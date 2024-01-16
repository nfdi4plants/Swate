namespace FsSpreadsheet.Exceljs


module JsWorksheet =

    open Fable.Core

    [<Emit("console.log($0)")>]
    let private log (obj:obj) = jsNative

    open FsSpreadsheet
    open Fable.ExcelJs

    let writeFromFsWorksheet (wb: Workbook) (fsws:FsWorksheet) : unit =
        fsws.RescanRows()
        let rows = fsws.Rows |> Seq.map (fun x -> x.Cells)
        let ws = wb.addWorksheet(fsws.Name)
        // due to the design of fsspreadsheet this might overwrite some of the stuff from tables, 
        // but as it should be the same, this is only a performance sink.
        for row in rows do
            for fsCell in row do
                let jsCell = ws.getCell(fsCell.Address.Address)
                jsCell.value <- JsCell.writeFromFsCell fsCell
        let tables = fsws.Tables |> Seq.map (fun table -> JsTable.writeFromFsTable fsws.CellCollection table)
        for table in tables do
            ws.addTable(table) |> ignore

    let readToFsWorksheet (wb: FsWorkbook) (jsws: Worksheet) : unit =
        let fsws = FsWorksheet(jsws.name)
        jsws.eachRow(fun (row, rowIndex) ->
            row.eachCell(fun (c, columnIndex) ->
                if c.value.IsSome then
                    let fsCell = JsCell.readToFsCell jsws.name rowIndex columnIndex c
                    fsws.AddCell(fsCell) |> ignore
            )
        )
        for jstableref in jsws.getTables() do
            let table = JsTable.readToFsTable jstableref
            fsws.AddTable table |> ignore
        fsws.RescanRows()
        wb.AddWorksheet(fsws)