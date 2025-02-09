#region

using System.Reflection;
using System.Threading;

using ReactiveUI;

using Tefin.Core.Reflection;
using Tefin.Grpc.Dynamic;

#endregion

namespace Tefin.ViewModels.Tabs;

public class JsonRequestEditorViewModel(MethodInfo methodInfo) : ViewModelBase, IRequestEditorViewModel {
    private string _json = "";

    public string Json {
        get => this._json;
        set => this.RaiseAndSetIfChanged(ref this._json, value);
    }

    public CancellationTokenSource? CtsReq {
        get;
        private set;
    }

    public MethodInfo MethodInfo {
        get;
    } = methodInfo;

    public (bool, object?[]) GetParameters() {
        var ret = DynamicTypes.fromJsonRequest(this.MethodInfo, this.Json);
        if (ret.IsOk & ret.ResultValue != null) {
            var val = ret.ResultValue;
            var mParams = val!.GetType().GetProperties()
                .Select(prop => prop.GetValue(val))
                .ToArray();
            var last = mParams.Last();
            this.CtsReq = null;
            if (last is CancellationToken) {
                this.CtsReq?.Dispose();
                this.CtsReq = null;
                this.CtsReq = new CancellationTokenSource();
                mParams[mParams.Length - 1] = this.CtsReq.Token;
            }

            return (true, mParams);
        }

        return (false, Array.Empty<object>());
    }

    public void Show(object?[] parameters) {
        var methodParams = this.MethodInfo.GetParameters();
        var hasValues = parameters.Length == methodParams.Length;

        if (!hasValues) {
            parameters = methodParams.Select(paramInfo => {
                var (ok, inst) = TypeBuilder.getDefault(paramInfo.ParameterType, true, Core.Utils.none<object>(), 0);
                return inst;
            }).ToArray();
        }

        var json = DynamicTypes.toJsonRequest(SerParam.Create(this.MethodInfo, parameters));
        if (json.IsOk)
            this.Json = json.ResultValue;
    }

    public void StartRequest() {
    }

    public void EndRequest() {
        this.CtsReq = null;
    }
}