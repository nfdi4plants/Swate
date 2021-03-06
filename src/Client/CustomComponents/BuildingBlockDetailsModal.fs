module CustomComponents.BuildingBlockDetailsModal

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open ExcelColors
open Model
open Messages
open Shared
open CustomComponents

let buildingBlockDetailModal (model:Model) dispatch =
    let closeMsg = (fun e -> ToggleShowDetails |> BuildingBlockDetails |> dispatch)

    let baseArr =
        model.BuildingBlockDetailsState.BuildingBlockValues |> Array.sortBy (fun x -> x.ColIndices)

    let minColIndex = baseArr |> Array.collect (fun x -> x.ColIndices) |> Seq.min

    let mainColHeader =
        baseArr |> Array.find (fun t -> t.ColIndices |> Array.contains minColIndex)

    let unitHeaderOpt =
        baseArr |> Array.tryFind (fun t -> t.ColIndices |> Array.contains (minColIndex+3) )

    let valueArr =
        baseArr
        |> Array.except [mainColHeader; if unitHeaderOpt.IsSome then unitHeaderOpt.Value]

    let sprintableRowIndices (rowIndices:int [])=
        let splitArrToContinous (l: int []) =
            l 
            |> Array.indexed 
            |> Array.groupBy (fun (i,x) -> i-x)
            |> Array.map (fun (key,valArr) ->
                valArr |> Array.map snd
            )
        let separatedIndices = splitArrToContinous rowIndices
        let sprintedRowIndices =
            [for contRowIndices in separatedIndices do
                let isLong = contRowIndices.Length >= 3
                let sprinted =
                    if isLong then
                        let min = Array.min contRowIndices
                        let max = Array.max contRowIndices
                        sprintf "%i-%i" min max
                    else
                        contRowIndices
                        |> Array.map string |> String.concat ", "
                yield sprinted
            ]
        sprintedRowIndices |> String.concat ", "

        

    Modal.modal [ Modal.IsActive true ] [
        Modal.background [
            Props [ OnClick closeMsg ]
        ] [ ]
        Notification.notification [
            Notification.Props [Style [Width "80%"; MaxHeight "80%"; OverflowX OverflowOptions.Auto ]]
        ] [
            Notification.delete [Props [OnClick closeMsg]][]
            Table.table [
                Table.IsFullWidth
                Table.IsStriped
            ][
                thead [][
                    tr [][
                        th [Class "toExcelColor"][str "Name"]
                        th [Class "toExcelColor"][str "TAN"]
                        th [Class "toExcelColor"][str "ColIndex"]
                        th [Class "toExcelColor"][str "RowIndex"]
                    ]
                    tr [][
                        th [][
                            str mainColHeader.SearchQuery.Name
                            if mainColHeader.TermOpt.IsSome then
                                span [
                                    Style [Color NFDIColors.LightBlue.Base; MarginLeft ".5rem"; OverflowY OverflowOptions.Visible]
                                    Class (Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
                                    Tooltip.dataTooltip mainColHeader.TermOpt.Value.Definition
                                ][
                                    Fa.i [
                                        Fa.Solid.InfoCircle
                                    ][]
                                ]
                        ]
                        th [][
                            a [ Href (Shared.URLs.termAccessionUrlOfAccessionStr mainColHeader.SearchQuery.TermAccession)] [ str mainColHeader.SearchQuery.TermAccession ]
                        ]
                        th [][str (mainColHeader.ColIndices |> Seq.min |> string)]
                        th [][str "Header"]
                    ]
                    if unitHeaderOpt.IsSome then
                        let unitHeader = unitHeaderOpt.Value
                        tr [][
                            th [][
                                str unitHeader.SearchQuery.Name
                                if unitHeader.TermOpt.IsSome then
                                    span [
                                        Style [Color NFDIColors.LightBlue.Base; MarginLeft ".5rem"; OverflowY OverflowOptions.Visible]
                                        Class (Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
                                        Tooltip.dataTooltip unitHeader.TermOpt.Value.Definition
                                    ][
                                        Fa.i [
                                            Fa.Solid.InfoCircle
                                        ][]
                                    ]
                            ]
                            th [][
                                a [ Href (Shared.URLs.termAccessionUrlOfAccessionStr unitHeader.SearchQuery.TermAccession)] [ str unitHeader.SearchQuery.TermAccession]
                            ]
                            th [][str (unitHeader.ColIndices |> Seq.min |> string)]
                            th [][str "Unit"]
                        ]
                ]
                tbody [][
                    for t in valueArr do
                        yield
                            tr [] [
                                td [][
                                    str (if t.SearchQuery.Name = "" then "none" else t.SearchQuery.Name)
                                    if t.TermOpt.IsSome then
                                        span [
                                            Style [Color NFDIColors.LightBlue.Base; MarginLeft ".5rem"; OverflowY OverflowOptions.Visible]
                                            Class (Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
                                            Tooltip.dataTooltip t.TermOpt.Value.Definition
                                        ][
                                            Fa.i [
                                                Fa.Solid.InfoCircle
                                            ][]
                                        ]
                                ]
                                td [][
                                    if t.TermOpt.IsSome then
                                        a [ Href (Shared.URLs.termAccessionUrlOfAccessionStr t.TermOpt.Value.Accession)] [ str t.TermOpt.Value.Accession  ]
                                    else
                                        str "none"
                                ]
                                td [][str (mainColHeader.ColIndices |> Seq.min |> string)]
                                td [][str (sprintf "%A" (sprintableRowIndices t.RowIndices) ) ]
                        ]
                ]
            ]

        ]
    ]