module OfficeInterop

open Fable.Core
open Fable.Core.JsInterop
open OfficeJS
open Excel
open System.Collections.Generic
    
[<Global>]
let Office : Office.IExports = jsNative

[<Global>]
//[<CompiledName("Office.Excel")>]
let Excel : Excel.IExports = jsNative

[<Global>]
let RangeLoadOptions : Interfaces.RangeLoadOptions = jsNative

[<Emit("console.log($0)")>]
let consoleLog (message: string): unit = jsNative
    
let exampleExcelFunction () =
    Excel.run(fun context ->
        let ranges = context.workbook.getSelectedRanges()
        ranges.format.fill.color <- "red"
        let x = ranges.load(U2.Case1 "address")
        context.sync().``then``(
            fun _ -> x.address
        )
    )

let createEmptyMatrixForTables (colCount:int) (rowCount:int) value =
    [|
        for i in 0 .. rowCount-1 do
            yield   [|
                        for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
                    |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

let createAnnotationTable (isDark:bool) =
    Excel.run(fun context ->
        let tableRange = context.workbook.getSelectedRange()
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        //delete table with the same name if present because there can only be one chosen one <3
        sheet.tables.getItemOrNullObject("annotationTable").delete()

        let annotationTable = sheet.tables.add(U2.Case1 tableRange,true)
        annotationTable.name <- "annotationTable"

        tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount";"address"]))) |> ignore
        annotationTable.load(U2.Case1 "style") |> ignore

        //sync with proxy objects after loading values from excel   
        context.sync()
            .``then``( fun _ ->

                let style =
                    if isDark then
                        "TableStyleMedium15"
                    else
                        "TableStyleMedium7"

                annotationTable.style <- style

                if tableRange.columnCount < 2. then // only one column there, so add data col to end.

                    let dataCol = createEmptyMatrixForTables 1 (int tableRange.rowCount) ""

                    (annotationTable.columns.getItemAt 0.).name <- "Sample Name"
                    annotationTable.columns.add(-1.,U4.Case1 dataCol, "Data File Name") |> ignore

                    sheet.getUsedRange().format.autofitColumns()
                    sheet.getUsedRange().format.autofitRows()

                    sprintf "Annotation Table created in [%s] with dimensions %.0f + 1 mandatory c x (%.0f + 1h)r" tableRange.address tableRange.columnCount (tableRange.rowCount - 1.) 
                else

                    (annotationTable.columns.getItemAt 0.).name <- "Sample Name"
                    (annotationTable.columns.getItemAt (tableRange.columnCount - 1.)).name <- "Data File Name"

                    sheet.getUsedRange().format.autofitColumns()
                    sheet.getUsedRange().format.autofitRows()

                    sprintf "Annotation Table created in [%s] with dimensions %.0fc x (%.0f + 1h)r. Adapted style to %s" tableRange.address tableRange.columnCount (tableRange.rowCount - 1.) style
                        
                )
            //.catch (fun e -> e |> unbox<System.Exception> |> fun x -> x.Message)
    )



let addAnnotationColumn (colName:string) =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem("annotationTable")

        let tableRange = annotationTable.getRange()
        tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"]))) |> ignore

        context.sync().``then``( fun _ ->
            let colCount = tableRange.columnCount
            let rowCount = tableRange.rowCount |> int
            let testCol = createEmptyMatrixForTables 1 rowCount ""

            let _ =
                annotationTable.columns.add(
                    colCount - 1., //last column should always be the predefined results column
                    values = U4.Case1 testCol, name=colName
                )
            sprintf "%s column was added." colName
        )
    )

let fillValue (v:string) =
    Excel.run(fun context ->
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray(["address";"values"])))

        //sync with proxy objects after loading values from excel
        context.sync().``then``( fun _ ->
            let newVals = ResizeArray([
                for arr in range.values do
                    let tmp = arr |> Seq.map (fun _ -> Some (v |> box))
                    ResizeArray(tmp)
            ])
            range.values <- newVals
            sprintf "%s filled with %s" range.address v
        )
    )

let getTableMetaData () =
    Excel.run (fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem("annotationTable")
        annotationTable.columns.load(propertyNames = U2.Case1 "count") |> ignore
        annotationTable.rows.load(propertyNames = U2.Case1 "count")    |> ignore
        let rowRange = annotationTable.getRange()
        rowRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore
        let headerRange = annotationTable.getHeaderRowRange()
        headerRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore

        context.sync().``then``(fun _ ->
            let colCount,rowCount = annotationTable.columns.count, annotationTable.rows.count
            let rowRangeAddr, rowRangeColCount, rowRangeRowCount = rowRange.address,rowRange.columnCount,rowRange.rowCount
            let headerRangeAddr, headerRangeColCount, headerRangeRowCount = headerRange.address,headerRange.columnCount,headerRange.rowCount

            sprintf "Table Metadata: [Table] : %ic x %ir ; [TotalRange] : %s : %ic x %ir ; [HeaderRowRange] : %s : %ic x %ir "
                (colCount            |> int)
                (rowCount            |> int)
                (rowRangeAddr.Replace("Sheet",""))
                (rowRangeColCount    |> int)
                (rowRangeRowCount    |> int)
                (headerRangeAddr.Replace("Sheet",""))
                (headerRangeColCount |> int)
                (headerRangeRowCount |> int)
        )
    )

let syncContext (passthroughMessage : string) =
    Excel.run (fun context -> context.sync(passthroughMessage))