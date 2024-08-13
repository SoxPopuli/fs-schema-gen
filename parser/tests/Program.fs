open Expecto
open Expecto.Flip
open Parser

open System.Runtime.CompilerServices

type ProjectPath =
    static member sourcePath([<CallerFilePath>] ?path: string) = Option.get path

module ProjectPath =
    open System.IO

    let projectDir () =
        ProjectPath.sourcePath () |> Directory.GetParent |> _.FullName

    let basic () =
        let root = projectDir ()

        Path.Join(root, "projects", "basic")

let getAllEntities dir =
    let loader = ProjectLoader dir
    let projects = loader.LoadUniqueProjects()

    projects
    |> Seq.map (fun p ->
        let el = EntityLoader.FromProjectOptions loader p
        let name = System.IO.FileInfo(p.ProjectFileName).Name
        name, el.FindAll()
    )

[<Tests>]
let projectTests =
    testList "Project Tests" [
        ftest "load basic project" {
            let loader = ProjectPath.basic () |> ProjectLoader

            let project = loader.LoadUniqueProjects() |> Seq.head

            let entityLoader = project |> EntityLoader.FromProjectOptions loader

            let record =
                entityLoader.FindEntityByPath [| "Program"; "RecordType" |] |> Option.get

            let ctx = FSharp.Compiler.Symbols.FSharpDisplayContext.Empty.WithShortTypeNames true

            //  for f in record.FSharpFields do
            //      let args =
            //          f.FieldType.GenericArguments
            //          |> Seq.map (fun f -> f.Format ctx)
            //          |> (fun x -> System.String.Join(", ", x))

            //      printfn $"{f.Name}: {f.FieldType.BasicQualifiedName}<{args}>"

            record |> ItemType.fromEntity |> printfn "%A"
        }

        test "balance tracker entities" {
            let home = System.Environment.GetEnvironmentVariable "HOME"

            let loader = $"{home}/Documents/Work/OpenBanking.BalanceTracker" |> ProjectLoader

            let el =
                loader.LoadUniqueProjects()
                |> Seq.find (fun p -> p.ProjectFileName.Contains "BalanceTracker")
                |> EntityLoader.FromProjectOptions loader

            let e =
                el.FindEntityByPath [| "Lambda"; "ResponseDTOs"; "MainPage"; "PreferredAccountDataV3DTO" |]
            //el.FindEntityByPath [| "Lambda"; "RequestDTOs"; "GetDashboardData" |]

            let e = Option.get e

            e.AsType() |> ItemType.get |> printfn "%A"

            ()

        //  let projectEntities = getAllEntities $"{home}/Documents/Work/OpenBanking.BalanceTracker"
        //  for (p, e) in projectEntities do
        //      printfn $"━━━ {p} ━━━━━━━━━━━━━"
        //      for entity in e do
        //          let entityName =
        //              entity.TryGetFullName ()
        //              |> Option.orElse (entity.TryGetFullDisplayName ())
        //              |> Option.defaultWith entity.ToString
        //          let entityType =
        //              entity.AsType ()
        //          printfn $"\t{entityName}: {entityType}"

        }
    ]

[<EntryPoint>]
let main args = runTestsInAssemblyWithCLIArgs [] args
