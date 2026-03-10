module Client.Tests

open Fable.Mocha

let client =
    testList "Client" [
        Components.Tests.Table.ContextMenu.Main
        Components.Tests.Table.KeyboardNavigation.Main
        OfficeAddIn.AnnotationTable.Successful.Main
        Electron.Tests.DataAnnotatorWidget.Main
    ]

let all =
    testList "All" [
#if FABLE_COMPILER // This preprocessor directive makes editor happy
        Swate.Components.Shared.Tests.shared
#endif
        client
    ]

[<EntryPoint>]
let main _ = Mocha.runTests all
