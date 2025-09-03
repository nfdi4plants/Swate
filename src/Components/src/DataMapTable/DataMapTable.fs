namespace Swate.Components

open Feliz
open Fable.Core
open ARCtrl


[<Erase; Mangle(false)>]
type DataMapTable =

    [<ReactComponent(true)>]
    static member DataMapTable(datamap: DataMap, setDatamap: DataMap -> unit, ?height) =
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

        Table.Table(datamap.RowCount + 1, datamap.ColumnCount, renderCell, renderActiveCell, ?height = height)


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