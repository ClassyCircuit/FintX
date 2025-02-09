namespace Tefin.Core

open System.Collections.Generic
open System.IO
open System.Reflection
open System.Runtime.InteropServices.JavaScript
open Tefin.Core.Interop

module AutoSave =

    type FileParam =
        { Json: string
          Header: string
          FullPath: string option }

        static member Empty() =
            { Json = ""
              Header = ""
              FullPath = None }

        member x.WithJson j = { x with Json = j }
        member x.WithHeader h = { x with Header = h }

        member x.WithFullPath p =
            { x with
                FullPath =
                    if (System.String.IsNullOrWhiteSpace p) then
                        None
                    else
                        Some p }

    type MethodParam =
        { Name: string
          Files: FileParam array }

        static member Empty() = { Name = ""; Files = Array.empty }
        member x.WithName n = { x with Name = n }
        member x.WithFiles f = { x with Files = f }

    type ClientParam =
        { Project: Project
          Client: ClientGroup
          Methods: MethodParam array }

        static member Empty() =
            { Project = Unchecked.defaultof<Project>
              Client = Unchecked.defaultof<ClientGroup>
              Methods = Array.empty }

        member x.WithProject p = { x with Project = p }
        member x.WithClient c = { x with Client = c }
        member x.WithMethods m = { x with Methods = m }

    type Writer =
        { Write: IOResolver -> string -> string -> unit
          Remove: string -> unit }

    let writer =
        let cache = Dictionary<string, string>()

        let doWrite (io: IOResolver) (file: string) (json: string) =
            try
                let found, content = cache.TryGetValue file

                if not found then
                    cache[file] <- json
                    io.File.WriteAllText file json
                else if not (content = json) then
                    cache[file] <- json
                    io.File.WriteAllText file json
            with exc ->
                io.Log.Warn($"Unable to auto-save {Path.GetFileName(file)}. {exc.Message}")

        let doRemove (file: string) =
            if (cache.ContainsKey file) then
                ignore <| cache.Remove file

        { Write = doWrite; Remove = doRemove }


    let rec private saveFile (io: IOResolver) (methodName: string) (autoSavePath: string) (f: FileParam) (writer: Writer) =
        match f.FullPath with
        | Some file ->
            if not (System.String.IsNullOrWhiteSpace f.Json) then
                writer.Write io file f.Json

            f
        | None ->
            // if the json content was provided but no full path, we save it with the
            // next available file name
            let fileName = Utils.getAvailableFileName autoSavePath f.Header Ext.requestFileExt
            let fullPath = Path.Combine(autoSavePath, fileName)
            writer.Write io fullPath f.Json

            { Json = f.Json
              Header = f.Header
              FullPath = Some fullPath }

    let private saveMethod (io: IOResolver) (clientPath: string) (m: MethodParam) (writer: Writer) =
        let autoSavePath = Project.getAutoSavePath clientPath m.Name            
        io.Dir.CreateDirectory autoSavePath

        let autoSavedFiles =
            m.Files
            |> Array.map (fun fileParam -> saveFile io m.Name autoSavePath fileParam writer)
            |> Array.map (fun p -> p.FullPath.Value)

        //Delete any existing files that were not auto-saved
        let existingFiles = io.Dir.GetFiles autoSavePath

        for e in existingFiles do
            if not (Array.contains e autoSavedFiles) then
                io.File.Delete e
                writer.Remove e

    let getAutoSavedFiles (io: IOResolver) (clientPath: string) =
        Project.getMethodsPath clientPath
        |> fun path -> io.Dir.GetFiles(path, "*" + Ext.requestFileExt, SearchOption.AllDirectories)
        |> Array.filter (fun fp -> fp.Contains(Project.autoSaveFolderName))
        |> Array.sortBy id
        
    let private saveClient (io: IOResolver) (clientParam: ClientParam) (writer: Writer) =
        if (Directory.Exists clientParam.Client.Path) then
            if (clientParam.Methods.Length = 0) then
                getAutoSavedFiles io clientParam.Client.Path
                |> Array.iter File.Delete
            
            for m in clientParam.Methods do
                saveMethod io clientParam.Client.Path m writer

    let getAutoSaveLocation (io: IOResolver) (methodInfo: MethodInfo) (clientPath: string) =
        let methodName = methodInfo.Name
        let autoSavePath = Project.getAutoSavePath (clientPath) methodName            

        io.Dir.CreateDirectory autoSavePath
        let fileName = Utils.getAvailableFileName autoSavePath methodName Ext.requestFileExt
        let fullPath = Path.Combine(autoSavePath, fileName)

        if not (io.File.Exists fullPath) then
            io.File.WriteAllText fullPath ""

        fullPath

    let run =
        let timer = new System.Timers.Timer()
        timer.AutoReset <- true
        timer.Interval <- 5000 //5 sec
      
        let io = Resolver.value
        let w = writer

        fun (getParam: System.Func<ClientParam array>) ->
            if not timer.Enabled then
                timer.Enabled <- true
                timer.Elapsed
                |> Observable.add (fun args ->
                    let clientParams = getParam.Invoke()

                    for clientParam in clientParams do
                        saveClient io clientParam w

                )
