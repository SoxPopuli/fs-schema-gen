open Expecto
open Expecto.Flip
open Parser

open System.Runtime.CompilerServices

open type ItemType.ItemType

type DataPath =
    static member sourcePath([<CallerFilePath>] ?path: string) = Option.get path

module DataPath =
    open System.IO

    let rootDir () =
        DataPath.sourcePath () |> Directory.GetParent |> _.FullName

    let basic () =
        let root = rootDir ()

        Path.Join(root, "projects", "basic")

    let testConfig () =
        let root = rootDir ()
        Path.Join(root, "projects", "test_config.toml")

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
        test "load basic project" {
            let loader = DataPath.basic () |> ProjectLoader

            let project = loader.LoadUniqueProjects() |> Seq.head

            let entityLoader = project |> EntityLoader.FromProjectOptions loader

            let expected =
                Record(
                    "RecordType",
                    [
                        ("A", Int32)
                        ("B", Float)
                        ("C", String)
                        ("D", Float)
                        ("E", Decimal)
                        ("F", Bool)
                        ("List", List Int32)
                        ("Array", Array Int32)
                        ("Tuple", Tuple [ Int32; Float ])
                        ("Dictionary", Dictionary(Int32, String))
                        ("Opt", Option Int32)
                        ("Res", Result(Int32, String))
                        ("Nested", Option(Record("NestedRecord", [ ("X", Int32) ])))
                        ("DeeplyNested", Record("DeeplyNestedRecord", [ ("X", Int32) ]))
                        ("Anonymous", AnonymousRecord [ ("Anon", Int32); ("B", String); ("Second", Float) ])
                    ]
                )

            let record =
                entityLoader.FindEntityByPath [| "Program"; "RecordType" |] |> Option.get

            record |> ItemType.fromEntity |> Expect.equal "" expected
        }
    ]

[<Tests>]
let configTests =
    testList "Config Tests" [
        test "load test config" {
            let config = DataPath.testConfig()
            let configText = System.IO.File.ReadAllText config

            let endpoints = Config.parseToml configText |> Seq.toArray

            printfn "%A" endpoints
            ()
        }
    ]

[<EntryPoint>]
let main args = runTestsInAssemblyWithCLIArgs [] args
