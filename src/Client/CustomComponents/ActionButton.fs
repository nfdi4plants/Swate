module ActionButton

open Fulma
open Fable.React
open Fable.React.Props
open Fable.FontAwesome

let private buttonCol (text:string) (disabled:bool) (onClickMsg:Browser.Types.MouseEvent -> unit) =
    Column.column [][
        Field.div [] [
            Control.div [] [
                Button.a [
                    if disabled
                    then
                        Button.IsActive true
                    else
                        Button.Color Color.IsDanger
                        Button.Props [Disabled true]
                    Button.IsFullWidth
                    Button.Color IsSuccess
                    Button.OnClick onClickMsg
                ] [
                    str text
                ]
            ]
        ]
    ]

let private removeButton (removeMsg:Browser.Types.MouseEvent -> unit) = 
    Column.column [Column.Width(Screen.All, Column.IsNarrow)][
        Button.a [
            Button.OnClick removeMsg
            Button.Color IsDanger
        ][
            Fa.i [Fa.Solid.Times][]
        ]
    ]

let button (text:string) (disabled:bool) (onClickMsg:Browser.Types.MouseEvent -> unit) =
    Columns.columns [Columns.IsMobile][
        buttonCol text disabled onClickMsg
    ]

let buttonWithRemove (text:string) (disabled:bool) (onClickMsg:Browser.Types.MouseEvent -> unit) (showRemoveButton:bool) (removeMsg:Browser.Types.MouseEvent -> unit) =
    Columns.columns [Columns.IsMobile][
        buttonCol text disabled onClickMsg
        if showRemoveButton then
            removeButton removeMsg
    ]

