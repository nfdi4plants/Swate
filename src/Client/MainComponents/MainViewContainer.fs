module MainComponents.MainViewContainer


open Feliz
open Feliz.Bulma
open Spreadsheet
open Messages

let Main(minWidth: int, left: ReactElement seq) = 
    Html.div [
        prop.style [
            style.minWidth(minWidth)
            style.flexGrow 1
            style.flexShrink 1
            style.height(length.vh 100)
            style.width(length.perc 100)
        ]
        prop.children left
    ]