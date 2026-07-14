module ProcessCoreTests.Program

open Expecto

[<EntryPoint>]
let main argv =
    testList "ProcessCore provenance adapter" [
        ProcessCoreAdapterContractTests.tests
        ProcessCoreConverterTests.tests
        ProcessCoreWritebackTests.tests
    ]
    |> runTestsWithCLIArgs [] argv
