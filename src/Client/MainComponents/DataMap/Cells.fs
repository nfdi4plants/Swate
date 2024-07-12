namespace MainComponents.DataMap

open Feliz
open Feliz.Bulma
open Spreadsheet
open Model
open Messages

open Fable.Core.JsInterop
open ARCtrl
open MainComponents.CellStyles

type Cells =

    static member Header(index:int, columnType: ColumnType, header: string) =
        let id = $"Datamap_Header_{header}_{index}"
        Html.th [
            if columnType.IsRefColumn then Bulma.color.hasBackgroundGreyLighter
            prop.key id
            prop.id id
            cellStyle []
            prop.className "main-contrast-bg"
            prop.children [
                Html.div [
                    cellInnerContainerStyle [style.custom("backgroundColor","inherit")]
                    prop.children [
                        basicValueDisplayCell header
                    ]
                ]
            ]
        ]