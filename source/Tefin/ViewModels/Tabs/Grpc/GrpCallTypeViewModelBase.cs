#region

using System.Reflection;

using Tefin.Core.Infra.Actors;
using Tefin.Core.Interop;

using static Tefin.Core.Interop.Messages;

#endregion

namespace Tefin.ViewModels.Tabs.Grpc;

public abstract class GrpCallTypeViewModelBase : ViewModelBase {
    protected GrpCallTypeViewModelBase(MethodInfo mi, ProjectTypes.ClientGroup cg) {
        this.MethodInfo = mi;
        this.Client = cg;
        GlobalHub.subscribe<MsgClientUpdated>(this.OnClientUpdated);
    }

    public ProjectTypes.ClientGroup Client { get; private set; }

    public MethodInfo MethodInfo { get; }

    public abstract void Init();

    public abstract string GetRequestContent();

    private void OnClientUpdated(MsgClientUpdated obj) {
        if (this.Client.Path == obj.Path || this.Client.Path == obj.PreviousPath) {
            this.Client = obj.Client;
        }
    }

    public abstract void ImportRequest(string requestFile);
}