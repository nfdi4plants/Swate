module Client.Tests

open Fable.Mocha

let client = testList "Client" [
    testCase "Hello received" <| fun _ ->
        let hello = Index.sayHello "SAFE V3"

        Expect.equal hello "Hello SAFE V3" "Unexpected greeting"

]

let all =
    testList "All"
        [
#if FABLE_COMPILER // This preprocessor directive makes editor happy
            Swate.Components.Shared.Tests.shared
#endif
            OfficeAddIn.AnnotationTable.Successful.Main
            client
        ]

[<EntryPoint>]
let main _ = Mocha.runTests all