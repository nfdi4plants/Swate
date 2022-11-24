module Client.Tests

open Fable.Mocha
open Client

let client = testList "Client" [
    testCase "Hello received" <| fun _ ->
        let hello = sayHello "SAFE V3"

        Expect.equal hello "Hello SAFE V3" "Unexpected greeting"
]

let all =
    testList "All"
        [
#if FABLE_COMPILER // This preprocessor directive makes editor happy
            Shared.Tests.shared
#endif
            BuildingBlockFunctions.Tests.tests_buildingBlockFunctions
            BuildingBlockView.Tests.tests_BuildingBlockView
            FilePickerView.Tests.tests_FilePickerView_PathRerooting
            client
        ]

[<EntryPoint>]
let main _ = Mocha.runTests all