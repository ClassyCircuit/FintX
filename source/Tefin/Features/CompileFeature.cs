#region

using Tefin.Core;
using Tefin.Core.Build;
using Tefin.Grpc;

#endregion

namespace Tefin.Features;

public class CompileFeature(string serviceName, string clientName, string description, string[] protoFiles, string reflectionUrl, IOResolver io) {
    public async Task<(bool, CompileOutput)> CompileExisting(string[] csFiles) {
        CompileParameters? cParams = new(clientName, description, serviceName, protoFiles, Array.Empty<string>(), reflectionUrl, null);
        var com = await ServiceClient.compile(io, csFiles, cParams);
        if (com.IsOk) {
            return (true, com.ResultValue);
        }

        io.Log.Error(com.ErrorValue);
        return (false, com.ResultValue);
    }

    public async Task<(bool, CompileOutput)> Run() {
        var csFiles = Array.Empty<string>();
        var cParams = new CompileParameters(clientName, description, serviceName, protoFiles, csFiles, reflectionUrl, null);
        var csFilesRet = await ServiceClient.generateSourceFiles(io, cParams);
        var com = await ServiceClient.compile(Resolver.value, csFilesRet.ResultValue, cParams);
        if (com.IsOk) {
            return (true, com.ResultValue);
        }

        io.Log.Error(com.ErrorValue);
        return (false, com.ResultValue);
    }
}