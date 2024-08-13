module Parser

open Parser

open FSharp.Compiler.CodeAnalysis
open Ionide
open System.IO


let _ =
    let p = ProjectLoader("")

    let projects =
        p.LoadProjects() |> p.ParseAndCheckProjectsAsync |> Async.RunSynchronously

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
