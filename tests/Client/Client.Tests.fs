module Client.Tests

open Fable.Mocha

let client = testList "Client" [
    Components.Tests.Table.KeyboardNavigation.Main
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