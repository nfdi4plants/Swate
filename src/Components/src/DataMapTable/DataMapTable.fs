namespace Swate.Components

open Feliz
open Fable.Core
open ARCtrl
open Fable.Core.JsInterop

[<Erase; Mangle(false)>]
type DataMapTable =

    static member private ContextMenu
        (datamap: DataMap, setDatamap: DataMap -> unit, tableRef: IRefValue<TableHandle>, containerRef, ?debug: bool)
        =
        let deleteRow =
            fun (index: CellCoordinate) ->
                let isSelected = tableRef.current.SelectHandle.contains index
                let start = index.y - 1

                if not isSelected then
                    datamap.DataContexts.RemoveAt(start)
                else
                    let range = tableRef.current.SelectHandle.getSelectedCellRange().Value
                    let count = range.yEnd - range.yStart + 1
                    datamap.DataContexts.RemoveRange(start, count)

                setDatamap datamap

        ContextMenu.ContextMenu(
            (fun data ->
                let index = data |> unbox<CellCoordinate>

                [
                    ContextMenuItem(
                        text = Html.div "Delete Row",
                        icon = Icons.DeleteLeft(),
                        kbdbutton = AnnotationTableContextMenu.ATCMC.KbdHint("DelR"),
                        onClick = (fun x -> deleteRow index)
                    )
                ]
            ),
            ref = containerRef,
            onSpawn =
                (fun e ->
                    let target = e.target :?> Browser.Types.HTMLElement

                    match target.closest ("[data-row][data-column]"), containerRef.current with
                    | Some cell, Some container when container.contains (cell) ->
                        let cell = cell :?> Browser.Types.HTMLElement
                        let row = int cell?dataset?row
                        let col = int cell?dataset?column
                        let indices: CellCoordinate = {| y = row; x = col |}
                        if col > 0 && row > 0 then Some indices else None // disable context menu on index column
                    | _ ->
                        console.log ("No table cell found")
                        None
                ),
            ?debug = debug
        )

    [<ReactComponent(true)>]
    static member DataMapTable(datamap: DataMap, setDatamap: DataMap -> unit, ?height, ?debug: bool) =

        let tableRef = React.useRef<TableHandle> (unbox null)
        let containerRef = React.useElementRef ()

        let renderCell =
            React.memo (
                (fun (index: CellCoordinate) ->
                    match index with
                    | _ when index.x > 0 && index.y > 0 ->
                        let cell = datamap.GetCell(index.x - 1, index.y - 1).ToString()
                        TableCell.StringInactiveCell(index, cell)
                    | _ when index.x > 0 && index.y = 0 ->
                        let header = datamap.GetHeader(index.x - 1).ToString()
                        TableCell.StringInactiveCell(index, header, disableActivation = true)
                    | _ ->
                        TableCell.BaseCell(
                            index.y,
                            index.x,
                            Html.text index.y,
                            className =
                                "swt:rounded-0 swt:px-2 swt:py-1 swt:flex swt:items-center swt:justify-center swt:cursor-not-allowed swt:w-full swt:h-full swt:bg-base-200"
                        )
                )
            )

        let renderActiveCell =
            React.memo (
                (fun (index: CellCoordinate) ->
                    match index with
                    | _ when index.x > 0 && index.y > 0 ->
                        let cell = datamap.GetCell(index.x - 1, index.y - 1)

                        let setCell =
                            fun newValue ->
                                datamap.SetCell(index.x - 1, index.y - 1, newValue)
                                setDatamap datamap

                        AnnotationTable.CompositeCellActiveRender(index, cell, setCell)

                    | _ when index.x > 0 && index.y = 0 -> Html.div "when index.x > 0 && index.y = 0"
                    | _ -> Html.div "unknown table pattern"
                )
            )

        Html.div [
            if debug.IsSome && debug.Value then
                prop.testId "datamap_table"
                prop.custom ("data-columncount", datamap.ColumnCount)
                prop.custom ("data-rowcount", datamap.RowCount)
            prop.className "swt:overflow-auto swt:flex swt:flex-col swt:h-full"
            prop.ref containerRef

            prop.children [
                DataMapTable.ContextMenu(datamap, setDatamap, tableRef, containerRef, ?debug = debug)
                Table.Table(
                    datamap.RowCount + 1,
                    datamap.ColumnCount,
                    renderCell,
                    renderActiveCell,
                    ref = tableRef,
                    ?height = height
                )
            ]
        ]


    [<ReactComponent>]
    static member Entry() =

        let datamap, setDatamap =
            React.useState (
                DataMap(
                    ResizeArray [
                        for i in 0..100 do
                            DataContext(
                                name = sprintf "Name %d" i,
                                dataType = DataFile.RawDataFile,
                                format = sprintf "Format %A" i,
                                selectorFormat = sprintf "Selector %A" i,
                                explication = OntologyAnnotation("Explication", "EXP", "EXP:21309813"),
                                unit = OntologyAnnotation("Unit", "UNIT", "UNIT:0000001"),
                                objectType = OntologyAnnotation("ObjectType", "OT", "OT:0000001"),
                                label = sprintf "Label: %d" i,
                                description = sprintf "Description: %d" i,
                                generatedBy = "Kevin F",
                                comments =
                                    ResizeArray(
                                        [
                                            for i in 0..5 do
                                                Comment(sprintf "Comment %d" i, sprintf "Value %d" i)
                                        ]
                                    )
                            )
                    ]
                )
            )

        DataMapTable.DataMapTable(datamap, setDatamap, 400)