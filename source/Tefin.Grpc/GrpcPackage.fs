﻿namespace Tefin.Grpc

open System.Collections.Generic
open System.Collections.ObjectModel
open System.IO
open Tefin.Core
open Tefin.Core.Interop

module GrpcPackage =
    
    let rootPath = Utils.getAppDataPath()
    let packagesPath = Path.Combine(rootPath, "packages")
    
    let packageName = "grpc"
    let grpcRoot = Path.Combine(packagesPath, packageName)
    let grpcProtos = Path.Combine(grpcRoot, "protos")
    let grpcTemp = Path.Combine(grpcRoot, "temp")
    let grpcConfig = Path.Combine(grpcRoot, "config")
    let grpcDb = Path.Combine(grpcRoot, "db")
    let grpcTools = Path.Combine(grpcRoot, "tools")
    let grpcProjects = Path.Combine(grpcRoot, "projects")
    let grpcProjectDefault = Path.Combine(grpcProjects, Project.DefaultName)
    
    let dbFile = $"{packageName}.db"
    let allPaths = [| grpcRoot; grpcProtos; grpcTemp; grpcConfig; grpcDb; grpcTools; grpcProjects; grpcProjectDefault |]
    
    let grpcConfigValues =
        let d = Dictionary<string, string>()
        d.Add("RootPath", grpcRoot)
        d.Add("ProtosPath", grpcProtos)
        d.Add("TempPath", grpcTemp)
        d.Add("Db", Path.Combine(grpcDb, dbFile))
        d.Add("ToolsPath", grpcTools)
        d.Add("ProjectsPath", grpcProjects)
        d.Add("DefaultProjectPath", grpcProjectDefault)
        ReadOnlyDictionary(d)
        
    type T() =
        interface IPackage with
        
            member x.Name = packageName
            member x.Init() = task {
               for d in allPaths do
                 ignore(Directory.CreateDirectory d)
            }
            
            member x.GetConfig() = grpcConfigValues
 
   

