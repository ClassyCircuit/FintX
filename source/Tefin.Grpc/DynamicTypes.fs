namespace Tefin.Grpc.Dynamic

open System
open System.Reflection
open Google.Protobuf.WellKnownTypes
open Tefin.Core
open Tefin.Core.Reflection
open Tefin.Grpc

type SerParam = {
    Method:MethodInfo
    RequestParams : obj array
    RequestStream :obj option
}
with
    static member Create(m, r) =
        {Method = m; RequestParams = r; RequestStream = None }
    static member WithStream (r:SerParam) (list) =
        {r with RequestStream = list }

module DynamicTypes =
    let private emitUnaryRequestClass methodInfo = RequestUtils.emitRequestClass "GrpcUnary" methodInfo
    let private emitClientStreamingRequestClass methodInfo = RequestUtils.emitRequestClass "GrpcClientStreaming" methodInfo
    let private emitDuplexStreamingRequestClass methodInfo = RequestUtils.emitRequestClass "GrpcDuplexStreaming" methodInfo
    let private emitServerStreamingRequestClass methodInfo = RequestUtils.emitRequestClass "GrpcServerStreaming" methodInfo
    
    let emitRequestClassForMethod (methodInfo : MethodInfo) =
        match GrpcMethod.getMethodType methodInfo with
        | MethodType.Duplex -> emitDuplexStreamingRequestClass methodInfo |> Res.ok
        | MethodType.Unary -> emitUnaryRequestClass methodInfo |> Res.ok
        | MethodType.ClientStreaming -> emitClientStreamingRequestClass methodInfo |> Res.ok
        | MethodType.ServerStreaming -> emitServerStreamingRequestClass methodInfo |> Res.ok
        | _ -> Res.failed (failwith "unknown grpc method type")
         
    let toJsonRequest (p:SerParam) =        
        let props =  CoreMethod.paramsToPropInfos p.Method p.RequestParams
        emitRequestClassForMethod(p.Method)
        |> Res.map (Instance.toJson props)
        |> Res.getValue
        
    let fromJsonRequest (method:MethodInfo) (json:string) =
        emitRequestClassForMethod(method)
        |> Res.map (Instance.fromJson json)
        |> Res.getValue
    
       
    
    

