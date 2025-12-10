namespace Pages

open Model

open Feliz

type ActivityLog =

    static member Main(model: Model) =
        Html.div [
            Html.table [
                prop.className "swt:table swt:table-xs"
                prop.children [
                    Html.tbody [
                        for logEntry in model.DevState.Log do
                            yield LogItem.toTableRow logEntry
                    ]
                ]
            ]
        ]