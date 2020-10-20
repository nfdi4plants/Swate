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


let createValueMatrix (colCount:int) (rowCount:int) value =
    ResizeArray([
        for outer in 0 .. rowCount-1 do
            let tmp = Array.zeroCreate colCount |> Seq.map (fun _ -> Some (value |> box))
            ResizeArray(tmp)
    ])

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

                (annotationTable.columns.getItemAt 0.).name <- "Source Name"

                sheet.getUsedRange().format.autofitColumns()
                sheet.getUsedRange().format.autofitRows()

                sprintf "Annotation Table created in [%s] with dimensions %.0f c x (%.0f + 1h)r" tableRange.address tableRange.columnCount (tableRange.rowCount - 1.) 

                )
            //.catch (fun e -> e |> unbox<System.Exception> |> fun x -> x.Message)
    )

let checkIfAnnotationTableIsPresent () =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let t = sheet.load(U2.Case1 "tables")
        let table = t.tables.getItemOrNullObject("annotationTable")
        context.sync()
            .``then``( fun _ ->
                not table.isNullObject 
        )
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
            //create an empty column to insert
            let testCol = createEmptyMatrixForTables 1 rowCount ""

            let createdCol =
                annotationTable.columns.add(
                    colCount,
                    values = U4.Case1 testCol, name=colName
                )
            let autofitRange = createdCol.getRange()

            autofitRange.format.autofitColumns()
            autofitRange.format.autofitRows()
            sprintf "%s column was added." colName
        )
    )

let changeTableColumnFormat (colName:string) (format:string) =
    Excel.run(fun context ->
       let sheet = context.workbook.worksheets.getActiveWorksheet()
       let annotationTable = sheet.tables.getItem("annotationTable")

       let colRange = (annotationTable.columns.getItem (U2.Case2 colName)).getDataBodyRange()
       colRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"]))) |> ignore
       
       context.sync().``then``( fun _ ->
           let rowCount = colRange.rowCount |> int
           //create an empty column to insert
           let formats = createValueMatrix 1 rowCount format

           colRange.numberFormat <- formats

           sprintf "format of %s was changed to %s" colName format
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

// Reform this to onSelectionChanged
let getParentOntology () =
    Excel.run (fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem("annotationTable")
        let tables = annotationTable.columns.load(propertyNames = U2.Case1 "items")
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let range = context.workbook.getSelectedRange()
        annoHeaderRange.load(U2.Case1 "columnIndex") |> ignore
        range.load(U2.Case1 "columnIndex") |> ignore
        context.sync()
            .``then``( fun _ ->
                let tableHeaderRangeColIndex = annoHeaderRange.columnIndex
                let selectColIndex = range.columnIndex
                let diff = selectColIndex - tableHeaderRangeColIndex |> int
                let vals =
                    tables.items
                let maxLength = vals.Count-1
                let value =
                    if diff < 0 || diff > maxLength then
                        None
                    else
                        let value1 = (vals.[diff].values.Item 0)
                        value1.Item 0
                //sprintf "%A::> %A : %A : %A" value diff tableHeaderRangeColIndex selectColIndex
                value
            )
    )

//let autoGetSelectedHeader () =
//    Excel.run (fun context ->
//        let sheet = context.workbook.worksheets.getActiveWorksheet()
//        let annotationTable = sheet.tables.getItem("annotationTable")
//        annotationTable.onSelectionChanged.add(fun e -> getParentOntology())
//        context.sync()
//    )

let syncContext (passthroughMessage : string) =
    Excel.run (fun context -> context.sync(passthroughMessage))