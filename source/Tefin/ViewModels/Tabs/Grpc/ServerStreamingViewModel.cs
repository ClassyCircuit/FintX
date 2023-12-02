#region

using System.Reflection;
using System.Windows.Input;

using ReactiveUI;

using Tefin.Core.Interop;
using Tefin.Features;
using Tefin.Grpc.Execution;

using static Tefin.Core.Utils;

#endregion

namespace Tefin.ViewModels.Tabs.Grpc;

public class ServerStreamingViewModel : GrpCallTypeViewModelBase {
    private string _statusText;

    public ServerStreamingViewModel(MethodInfo mi, ProjectTypes.ClientGroup cg) : base(mi, cg) {
        this.ReqViewModel = new ServerStreamingReqViewModel(mi, true);
        this.RespViewModel = new ServerStreamingRespViewModel(mi);
        this.StartCommand = this.CreateCommand(this.OnStart);
        this.StatusText = "";
    }

    public ServerStreamingReqViewModel ReqViewModel { get; set; }
    public ServerStreamingRespViewModel RespViewModel { get; }
    public ICommand StartCommand { get; }

    public string StatusText {
        get => this._statusText;
        private set => this.RaiseAndSetIfChanged(ref this._statusText, value);
    }

    public override void Dispose() {
        base.Dispose();
        this.ReqViewModel.Dispose();
        this.RespViewModel.Dispose();
    }

    public override void Init() {
        this.ReqViewModel.Init();
    }

    private async Task OnStart() {
        this.IsBusy = true;
        try {
            var mi = this.ReqViewModel.MethodInfo;
            var mParams = this.ReqViewModel.GetMethodParameters();
            var cfg = new CallConfig("http://localhost:5000", false, "", none<Cert>(), this.Io);

            var feature = new CallServerStreamingFeature(mi, mParams, cfg, this.Io);
            var (ok, resp) = await feature.Run();
            var (_, response, context) = resp.OkayOrFailed();

            this.RespViewModel.Show(ok, response, context);
            await this.RespViewModel.SetupServerStreamNode(response);

            //await this.RespViewModel.Complete(stdResp, readServerStream.CompleteRead);
            await this.RespViewModel.Complete(typeof(StandardResponseViewModel.GrpcStandardResponse), CompleteRead);
            this.IsBusy = false;

            var elapsed = DateTime.Now - context.StartTime;
            this.StatusText = $"Elapsed: {printTimeSpan(elapsed)}";

            async Task<object> CompleteRead() {
                var readServerStream = new ReadServerStreamFeature();
                var resp = (ServerStreamingCallResponse)response;
                resp = await readServerStream.CompleteRead(resp);
                var model = new StandardResponseViewModel.GrpcStandardResponse() {
                    Headers = resp.Headers.Value,
                    Trailers = resp.Trailers.Value,
                    Status = resp.Status.Value
                };
                return model;
            }
        }
        finally {
            this.IsBusy = false;
        }
    }
}