open Expecto

[<Tests>]
let projectTests =
    testList "Project Tests" [
        test "a" {
            ()
        }

        test "b" {
            ()
        }
    ]

[<EntryPoint>]
let main args =
    runTestsInAssemblyWithCLIArgs [] args
