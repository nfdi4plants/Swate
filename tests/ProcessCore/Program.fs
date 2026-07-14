module ProcessCoreTests.Program

open Expecto

[<EntryPoint>]
let main argv =
    testList "ProcessCore provenance adapter" [ ProcessCoreAdapterContractTests.tests ]
    |> runTestsWithCLIArgs [] argv
