open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open FSharp.Compiler.CodeAnalysis

open Ionide

open System
open System.IO
open FSharp.Compiler.Symbols

module DeeplyNested =
    module Nested2 =
        module Nested3 =
            module Nested4 =
                module Nested5 =
                    module Nested6 =
                        type DeeplyNestedRecord = { X: int }

type RecordType = {
    A: int
    B: float
    C: string
    Nested: NestedRecord
    DeeplyNested: DeeplyNested.Nested2.Nested3.Nested4.Nested5.Nested6.DeeplyNestedRecord
}

and NestedRecord = { X: int }

type ProjectLoader(projectDir) =
    let dirInfo = DirectoryInfo projectDir

    let toolsPath = ProjInfo.Init.init dirInfo None

    let workspaceLoader = ProjInfo.WorkspaceLoader.Create(toolsPath)

    let _subscription =
        workspaceLoader.Notifications.Subscribe(fun msg -> printfn "%A" msg)

    let checker = FSharpChecker.Create(keepAssemblyContents = true)

    member _.LoadProjects() =
        if not dirInfo.Exists then
            failwith $"Directory {projectDir} does not exist"

        let solutions = Directory.GetFiles(projectDir, "*.sln")

        match solutions with
        | [||] ->
            Directory.GetFiles(dirInfo.FullName, "*.fsproj", SearchOption.AllDirectories)
            |> List.ofArray
            |> workspaceLoader.LoadProjects
        | slns -> slns |> Array.map workspaceLoader.LoadSln |> Seq.concat

    static member ConvertProjectOptions options =
        options |> ProjInfo.FCS.mapManyOptions |> Seq.toArray

    member _.ParseAndCheckProject(options: FSharpProjectOptions) = checker.ParseAndCheckProject(options)

    member this.ParseAndCheckProject(options: ProjInfo.Types.ProjectOptions, ?knownProjects) =
        let knownProjects = defaultArg knownProjects Seq.empty

        options
        |> (fun o -> ProjInfo.FCS.mapToFSharpProjectOptions o knownProjects)
        |> this.ParseAndCheckProject

    member this.ParseAndCheckProjects options =
        options
        |> ProjectLoader.ConvertProjectOptions
        |> Seq.map this.ParseAndCheckProject
        |> Async.Parallel

type MultiValueDictionary<'Key, 'Value when 'Key: equality>() =
    inherit Collections.Generic.Dictionary<'Key, Collections.Generic.List<'Value>>()

    member this.Add(key, value) =
        if this.ContainsKey key then
            this[key].Add value
        else
            let list = Collections.Generic.List<'Value>()
            list.Add value
            this[key] <- list

type EntityLoader(checkData: FSharpCheckProjectResults) =
    class
        // Cache looked up entities to speed up recall
        let entityCache = MultiValueDictionary<string, FSharpEntity>()

        member _.FindEntityByPath(path: string seq) =
            checkData.AssemblySignature.FindEntityByPath(path |> Seq.toList)

        member this.FindEntityByPath([<ParamArray>] path) =
            path |> Array.toSeq |> this.FindEntityByPath
    end

let _ =
    let p = ProjectLoader("")
    let projects = p.LoadProjects() |> p.ParseAndCheckProjects |> Async.RunSynchronously

    let loaders = projects |> Array.map EntityLoader

    let loader = loaders[0]

    loader.FindEntityByPath("a", "b")

module Array =
    let mapSecond fn x = x |> Array.map (fun (a, b) -> a, fn b)

let getProjectOptions options =
    options |> ProjInfo.FCS.mapManyOptions |> Seq.toArray

let getAST dirPath =
    let checker = FSharpChecker.Create(keepAssemblyContents = true)

    let info = DirectoryInfo(dirPath)

    if info.Exists = false then
        [||]
    else
        let loader = ProjectLoader dirPath

        let options = loader.LoadProjects() |> ProjInfo.FCS.mapManyOptions |> Seq.toArray

        let projectResults =
            options
            |> Seq.map (fun o -> checker.ParseAndCheckProject o)
            |> Async.Parallel
            |> Async.RunSynchronously

        let fields =
            projectResults
            |> Array.map (fun x -> x.AssemblySignature.FindEntityByPath [ "Program"; "RecordType" ])
            |> Array.choose id
            |> Array.head
            |> (fun x -> x.FSharpFields)

        let parseResults, checkResults =
            projectResults
            |> Array.indexed
            |> Array.mapSecond (fun r ->
                let files = r.AssemblyContents.ImplementationFiles
                files |> List.map _.FileName
            )
            |> Seq.map (fun (i, names) ->
                names
                |> Seq.map (fun name -> checker.GetBackgroundCheckResultsForFileInProject(name, options[i]))
            )
            |> Seq.concat
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.unzip

        let fields =
            checkResults
            |> Array.map (fun x -> x.PartialAssemblySignature.FindEntityByPath [ "Program"; "RecordType" ])
            |> Array.choose id
            |> Array.head
            |> (fun x -> x.FSharpFields)

        let syntaxTrees = parseResults |> Array.map _.ParseTree

        syntaxTrees |> printfn "Syntax: %A"

        syntaxTrees

[<EntryPoint>]
let main _ =
    let asts = getAST "."
    printfn "%A" (asts[0])
    0
