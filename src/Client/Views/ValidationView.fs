module ValidationView

open Fable.React
open Fable.React.Props
open Fulma
open ExcelColors
open Model
open Messages
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions

open CustomComponents

let columnListElement (format:ValidationFormat) dispatch =
    tr [][
        td [][str format.ColumnHeader]
        td [][
            if format.Importance.IsSome then
                str (string format.Importance.Value)
            else
                str "X"
        ]
        td [][
            if format.ContentType.IsSome then
                str format.ContentType.Value.toString
            else
                str "X"
        ]
    ]

let validationComponent model dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Table Validation"]

        Field.div [Field.Props [Style [
            Width "100%"
        ]]] [
            Table.table [ Table.IsHoverable; Table.IsFullWidth ] [
                thead [ ] [
                    tr [ ] [
                        th [ ] [ str "Column Header" ]
                        th [ ] [ str "Importance" ]
                        th [ ] [ str "Content Type" ]
                    ]
                ]
                tbody [ ] [
                    for col in model.ValidationState.TableValidationScheme do
                        yield columnListElement col dispatch
                ]
            ]
        ]
    ]