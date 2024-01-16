namespace FsSpreadsheet.Exceljs

module JsTable =

    open Fable.Core
    open FsSpreadsheet
    open Fable.ExcelJs
    open Fable.Core.JsInterop

    [<Emit("console.log($0)")>]
    let private log (obj:obj) = jsNative

    let writeFromFsTable (fscellcollection: FsCellsCollection) (fsTable: FsTable) : Table =
        let fsColumns = fsTable.GetColumns fscellcollection
        let columns = 
            if fsTable.ShowHeaderRow then
                [| for headerCell in fsTable.GetHeaderRow(fscellcollection) do
                    yield TableColumn(headerCell.ValueAsString()) |]
            else
                [|
                    for i in 1 .. Seq.length fsColumns do yield TableColumn(string i)
                |] 
        let rows = 
            [| for col in fsColumns do
                let cells = 
                    if fsTable.ShowHeaderRow then col.Cells |> Seq.tail else col.Cells 
                yield! cells |> Seq.map (fun c -> 
                    let rowValue = JsCell.writeFromFsCell c |> Option.get
                    c.Address.RowNumber, (c.Address.ColumnNumber, rowValue)
                ) 
            |]
            |> Array.groupBy fst
            |> Array.sortBy fst
            |> Array.map (fun (_,arr) ->
                let m = arr |> Array.map snd |> Map
                let row = [|fsTable.RangeAddress.FirstAddress.ColumnNumber .. fsTable.RangeAddress.FirstAddress.ColumnNumber + (columns.Length-1)|] 
                let row = row |> Array.map (fun i -> m.TryFind i |> box)
                row
            )
        let defaultStyle = {|
            showRowStripes = true
        |}
        Table(fsTable.Name,fsTable.RangeAddress.Range,columns,rows,fsTable.Name,headerRow = fsTable.ShowHeaderRow, style = defaultStyle)

    let readToFsTable(table:ITableRef) =
        let table = table.table.Value
        let tableRef = table.tableRef |> FsRangeAddress
        let tableName = if isNull table.displayName then table.name else table.displayName
        let table = FsTable(tableName, tableRef, table.totalsRow, table.headerRow)
        table