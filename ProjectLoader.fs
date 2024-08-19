namespace Parser

open Ionide
open System.IO
open FSharp.Compiler.CodeAnalysis

type ProjectLoader(projectPath, ?printNotifications) =
    let dirInfo = DirectoryInfo projectPath

    let toolsPath = ProjInfo.Init.init dirInfo None

    let workspaceLoader = ProjInfo.WorkspaceLoader.Create(toolsPath)

    let _subscription =
        match printNotifications with
        | Some true -> workspaceLoader.Notifications.Subscribe(fun msg -> printfn "%A" msg)
        | _ ->
            { new System.IDisposable with
                member _.Dispose() = ()
            }

    let checker = FSharpChecker.Create(keepAssemblyContents = true)

    let tryLoadProjectFile (info: FileInfo) =
        match info.Extension with
        | ".sln" -> workspaceLoader.LoadSln info.FullName
        | ".fsproj" -> workspaceLoader.LoadProjects [ info.FullName ]
        | _ -> Seq.empty

    let tryLoadProjectDir (info: DirectoryInfo) =
        let solutions = Directory.GetFiles(info.FullName, "*.sln")

        if Array.isEmpty solutions then
            Directory.GetFiles(dirInfo.FullName, "*.fsproj", SearchOption.AllDirectories)
            |> List.ofArray
            |> workspaceLoader.LoadProjects
        else
            solutions |> Array.map workspaceLoader.LoadSln |> Seq.concat

    member _.LoadProjects() =
        let fileInfo = FileInfo projectPath

        if fileInfo.Exists then
            tryLoadProjectFile fileInfo
        else if not dirInfo.Exists then
            failwith $"Directory or file '{projectPath}' does not exist"
        else
            let solutions = Directory.GetFiles(projectPath, "*.sln")

            match solutions with
            | [||] ->
                let projects =
                    Directory.GetFiles(dirInfo.FullName, "*.fsproj", SearchOption.AllDirectories)
                    |> List.ofArray
                    |> workspaceLoader.LoadProjects

                if Seq.isEmpty projects then
                    failwith $"No .fsproj files found in {dirInfo.FullName}"

                projects
            | slns -> slns |> Array.map workspaceLoader.LoadSln |> Seq.concat

    /// Load projects in `projectPath`, distinct by `ProjectId`
    member this.LoadUniqueProjects() =
        this.LoadProjects() |> Seq.distinctBy (fun p -> p.ProjectId)

    static member ConvertProjectOptions options =
        options |> ProjInfo.FCS.mapManyOptions |> Seq.toArray

    member _.ParseAndCheckProjectAsync(options: FSharpProjectOptions) = checker.ParseAndCheckProject(options)

    member this.ParseAndCheckProjectAsync(options: ProjInfo.Types.ProjectOptions, ?knownProjects) =
        let knownProjects = defaultArg knownProjects Seq.empty

        options
        |> (fun o -> ProjInfo.FCS.mapToFSharpProjectOptions o knownProjects)
        |> this.ParseAndCheckProjectAsync

    member this.ParseAndCheckProjectsAsync options =
        options
        |> ProjectLoader.ConvertProjectOptions
        |> Seq.map this.ParseAndCheckProjectAsync
        |> Async.Parallel
