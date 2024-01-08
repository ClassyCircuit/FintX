#region

using System.Reflection;
using System.Windows.Input;

using ReactiveUI;

using Tefin.Core.Reflection;
using Tefin.Features;
using Tefin.Grpc.Execution;

#endregion

namespace Tefin.ViewModels.Tabs.Grpc;

public class ClientStreamingReqViewModel : UnaryReqViewModel {
    private readonly ListJsonEditorViewModel _clientStreamJsonEditor;
    private readonly ListTreeEditorViewModel _clientStreamTreeEditor;
    private readonly Type _listType;
    private readonly Type _requestItemType;
    private ClientStreamingCallResponse _callResponse;
    private bool _canWrite;

    private IListEditorViewModel _clientStreamEditor;
    private bool _isShowingClientStreamTree;

    public ClientStreamingReqViewModel(MethodInfo methodInfo, bool generateFullTree, List<object?>? methodParameterInstances = null)
        : base(methodInfo, generateFullTree, methodParameterInstances) {
        this.WriteCommand = this.CreateCommand(this.OnWrite);
        this.EndWriteCommand = this.CreateCommand(this.OnEndWrite);
        this._callResponse = ClientStreamingCallResponse.Empty();

        var args = methodInfo.ReturnType.GetGenericArguments();
        this._requestItemType = args[0];
        var listType = typeof(List<>);
        this._listType = listType.MakeGenericType(this._requestItemType);

        this._clientStreamTreeEditor = new ListTreeEditorViewModel("Client Stream", this._listType);
        this._clientStreamJsonEditor = new ListJsonEditorViewModel("Client Stream", this._listType);
        this._isShowingClientStreamTree = true;
        this._clientStreamEditor = this._clientStreamTreeEditor;

        this.SubscribeTo(vm => ((ClientStreamingReqViewModel)vm).IsShowingClientStreamTree, this.OnIsShowingClientStreamTreeChanged);
    }

    public bool IsShowingClientStreamTree {
        get => this._isShowingClientStreamTree;
        set => this.RaiseAndSetIfChanged(ref this._isShowingClientStreamTree, value);
    }
    public ClientStreamingCallResponse CallResponse {
        get => this._callResponse;
        private set => this.RaiseAndSetIfChanged(ref this._callResponse, value);
    }

    public ICommand EndWriteCommand {
        get;
    }

    public ICommand WriteCommand {
        get;
    }

    public IListEditorViewModel ClientStreamEditor {
        get => this._clientStreamEditor;
        private set => this.RaiseAndSetIfChanged(ref this._clientStreamEditor, value);
    }
    public bool CanWrite {
        get => this._canWrite;
        set => this.RaiseAndSetIfChanged(ref this._canWrite, value);
    }

    public override string GetRequestContent() {
        var (ok, mParams) = this.GetMethodParameters();
        if (ok) {
            var (isValid, reqStream) = this.ClientStreamEditor.GetList();
            if (isValid) {
                var feature = new ExportFeature(this.MethodInfo, mParams, reqStream);
                var exportReqJson = feature.Export();
                if (exportReqJson.IsOk) {
                    return exportReqJson.ResultValue;
                }
            }
        }

        return "";
    }

    public override async Task ImportRequestFile(string file) {
        await GrpcUiUtils.ImportRequest(this.RequestEditor, this.ClientStreamEditor, this._listType, this.MethodInfo, file, this.Io);
    }

    public override async Task ImportRequest() {
        await GrpcUiUtils.ImportRequest(this.RequestEditor, this.ClientStreamEditor, this._listType, this.MethodInfo, this.Io);
    }

    public override async Task ExportRequest() {
        var (ok, mParams) = this.GetMethodParameters();
        if (ok) {
            var (isValid, reqStream) = this.ClientStreamEditor.GetList();
            if (!isValid) {
                this.Io.Log.Warn("Request stream is invalid. Content will not be saved to the request file");
            }

            await GrpcUiUtils.ExportRequest(mParams, reqStream, this.MethodInfo, this.Io);
        }
    }
    private void OnIsShowingClientStreamTreeChanged(ViewModelBase obj) {
        var vm = (ClientStreamingReqViewModel)obj;
        if (vm._isShowingClientStreamTree) {
            this.ShowAsTree();
        }
        else {
            this.ShowAsJson();
        }
    }

    private void ShowAsJson() {
        var (ok, list) = this._clientStreamEditor.GetList();
        this.ClientStreamEditor = this._clientStreamJsonEditor;
        if (ok)
            this.ClientStreamEditor.Show(list);
    }

    private void ShowAsTree() {
        var (ok, list) = this._clientStreamEditor.GetList();
        this.ClientStreamEditor = this._clientStreamTreeEditor;
        if (ok)
            this.ClientStreamEditor.Show(list);
    }

    public void SetupClientStream(ClientStreamingCallResponse response) {
        this._callResponse = response;
        var stream = Activator.CreateInstance(this._listType)!;
        var (ok, reqInstance) = TypeBuilder.getDefault(this._requestItemType, true, Core.Utils.none<object>(), 0);
        if (ok) {
            var add = this._listType.GetMethod("Add");
            add!.Invoke(stream, new[] {
                reqInstance
            });
        }
        else
            this.Io.Log.Error($"Unable to create an instance for {this._requestItemType}");

        this._clientStreamEditor.Show(stream!);
        this.CanWrite = true;
    }

    private async Task OnEndWrite() {
        try {
            var resp = this.CallResponse;
            this.IsBusy = true;
            var writer = new WriteClientStreamFeature();
            this.CallResponse = await writer.CompleteWrite(resp);
        }
        finally {
            this.CanWrite = false;
            //this._clientStreamEditor.Clear();
            this.IsBusy = false;
        }
    }

    private async Task OnWrite() {
        try {
            var resp = this.CallResponse;
            this.IsBusy = true;
            var writer = new WriteClientStreamFeature();

            foreach (var i in this.ClientStreamEditor.GetListItems())
                await writer.Write(resp, i);
        }
        catch (Exception exc) {
            this.Io.Log.Error(exc);
        }
        finally {
            this.IsBusy = false;
        }
    }
}