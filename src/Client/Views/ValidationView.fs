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

let columnListElement ind (format:ValidationFormat) (model:Model) dispatch =
    let isActive =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            true
        | _ ->
            false
    tr [
        Class "nonSelectText"
        Style [
            Cursor "pointer"
            UserSelect UserSelectOptions.None
        ]
        OnClick (fun e ->
            e.preventDefault()
            if isActive then
                UpdateDisplayedOptionsId None |> Validation |> dispatch
            else
                UpdateDisplayedOptionsId (Some ind) |> Validation |> dispatch
        )
    ][
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

let optionsElement ind (format:ValidationFormat) (model:Model) dispatch =
    let isVisible =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            DisplayOptions.Block
        | _ ->
            DisplayOptions.None
    tr [][
        td [
            ColSpan 3
            Style [Padding "0"]
        ][
            Box.box' [
                Props [
                    ColSpan 3
                    Style [
                        Display isVisible
                        Width "100%"
                    ]
                ]
            ][
                Column.column [ ] [

                ]
            ]
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
                    for i in 0 .. model.ValidationState.TableValidationScheme.Length-1 do
                        let f = model.ValidationState.TableValidationScheme.[i]
                        yield
                            columnListElement i f model dispatch
                        yield
                            optionsElement i f model dispatch
                        
                ]
            ]
        ]
    ]