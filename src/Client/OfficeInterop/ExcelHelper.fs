module OfficeInterop.ExcelHelper

open System
open System.Collections.Generic

open Fable.Core
open ExcelJS.Fable
open Excel

open OfficeInterop

open ARCtrl
open ARCtrlHelper
open GlobalBindings
open ARCtrl.Spreadsheet

[<Literal>]
let AppendIndex = -1.

[<Literal>]
let TableStyleLight = "TableStyleMedium7"

// ExcelApi 1.1
/// This function returns the names of all annotationTables in all worksheets.
let getAllTableNames (context: RequestContext) =
    // Ref. 2

    let tables = context.workbook.tables.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
    let _ = tables.load(propertyNames = U2.Case1 "name")

    context.sync()
        .``then``( fun _ ->

            /// Get all names of all tables in the whole workbook.
            let tableNames =
                tables.items
                |> Seq.toArray
                |> Array.map (fun x -> x.name)

            tableNames
    )

let createMatrixForTables (colCount: int) (rowCount: int) value =
    [|
        for i in 0 .. rowCount-1 do
            yield [|
                for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
            |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

let inline excelRunWith<'A> (context: RequestContext option) (promise: RequestContext -> JS.Promise<'A>) =
    match context with
    | Some ctx -> promise ctx
    | None -> Excel.run (fun ctx -> promise ctx)

/// This is based on a excel hack on how to add multiple header of the same name to an excel table.,
/// by just appending more whitespace to the name.
let extendName (existingNames: string []) (baseName: string) =
    /// https://unicode-table.com/en/200B/
    /// Play with Fire 🔥
    let zeroWidthChar = '​'
    let rec loop (baseName:string) =
        if existingNames |> Array.contains baseName then
            loop (baseName + " ")
        else
            baseName
    loop baseName

let createNewTableName() =
    let guid = System.Guid.NewGuid().ToString("N")
    ArcTable.annotationTablePrefix + guid

/// <summary>
/// Get the excel table of the given context and name
/// </summary>
/// <param name="context"></param>
/// <param name="tableName"></param>
let tryGetTableByName (context: RequestContext) (tableName: string) =
    promise {
        let _ = context.workbook.load(U2.Case1 "tables")
        let excelTable = context.workbook.tables.getItem(tableName)

        if String.IsNullOrWhiteSpace tableName then
            return None
        else
            let annoHeaderRange = excelTable.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
            let annoBodyRange = excelTable.getDataBodyRange()
            let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"; "numberFormat"|])) |> ignore
            return Some (excelTable, annoHeaderRange, annoBodyRange)
    }

/// <summary>
/// Swaps 'Rows with column values' to 'Columns with row values'
/// </summary>
/// <param name="rows"></param>
let viewRowsByColumns (rows: ResizeArray<ResizeArray<'a>>) =
    rows
    |> Seq.collect (fun row -> Seq.indexed row)
    |> Seq.groupBy fst
    |> Seq.map (snd >> Seq.map snd >> Seq.toArray)
    |> Seq.toArray

 /// <summary>
/// Converts a sequence of sequence of excel data into a resizearray, compatible with Excel.Range
/// </summary>
/// <param name="metadataValues"></param>
let convertSeqToExcelRangeInput (metadataValues: seq<seq<string option>>) =

    //Selects the longest sequence of the metadata values
    //In the next step, determines the length of the longest metadata value sequence
    let maxLength = metadataValues |> Seq.maxBy Seq.length |> Seq.length

    //Adapts the length of the smaller sequences to the length of the longest sequence in order to avoid problems with the insertion into excel.Range
    let ra = ResizeArray()
    for seq in metadataValues do
        //Parse string to obj option
        let ira = ResizeArray (seq |> Seq.map (fun cell -> cell |> Option.map box))
        if ira.Count < maxLength then
            ira.AddRange (Array.create (maxLength - ira.Count) None)
        ra.Add ira
    ra

/// <summary>
/// This function is used as minimal test function to ensure a working excel function test suit
/// </summary>
let getSelectedRangeAdress (context: RequestContext) =
    promise {
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case1 "address") |> ignore
        do! context.sync()

        return range.address
    }

[<AutoOpen>]
module Table =

    /// <summary>
    /// Add a new column at the index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="excelTable"></param>
    /// <param name="name"></param>
    /// <param name="rowCount"></param>
    /// <param name="value"></param>
    let addColumn (index: float) (excelTable: Table) name rowCount value =
        let col = createMatrixForTables 1 rowCount value

        excelTable.columns.add(
            index   = index,
            values  = U4.Case1 col,
            name    = name
        )

    /// <summary>
    /// Add a new column with the given values at the index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="excelTable"></param>
    /// <param name="values"></param>
    let addColumnAt (index: int) (excelTable: Table) (values: string []) =
        let columnValues =
            U4<Array<Array<U3<bool,string,float>>>,bool,string,float>.Case1 [|
                for row in values do
                    [|
                        U3.Case2 row
                    |] 
            |]

        excelTable.columns.add(
            index,
            columnValues
        )

    /// <summary>
    /// Delete an existing column at the given index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="excelTable"></param>
    /// <param name="name"></param>
    /// <param name="rowCount"></param>
    /// <param name="value"></param>
    let deleteColumn (index: float) (excelTable: Table) =
        let col = excelTable.columns.getItemAt index
        col.delete()

     /// <summary>
    /// Add only the row values of the column you are adding
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="excelTable"></param>
    /// <param name="columnName"></param>
    /// <param name="rows"></param>
    let addColumnAndRows (columnIndex: float) (excelTable: Table) columnName rows =
        excelTable.columns.add(
            index   = columnIndex,
            values  = U4.Case1 rows,
            name    = columnName
        )

    /// <summary>
    /// Add new rows with the same value to the table
    /// </summary>
    /// <param name="index"></param>
    /// <param name="excelTable"></param>
    /// <param name="rowCount"></param>
    /// <param name="value"></param>
    let addRows (index: float) (excelTable: Table) columnCount rowCount value =
        let col = createMatrixForTables columnCount rowCount value
        excelTable.rows.add(
            index   = index,
            values  = U4.Case1 col
        )

// I retrieve the index of the currently opened worksheet, here the new table should be created.
// I retrieve all annotationTables in the workbook.
// I filter out all annotationTables that are on a worksheet with a lower index than the index of the currently opened worksheet.
// I subtract from the index of the current worksheet the indices of the other found worksheets with annotationTable.
// I sort by the resulting lowest number (since the worksheet is then closest to the active one), I find the output column in the particular
// annotationTable and use the values it contains for the new annotationTable in the active worksheet.
/// <summary>
/// Will return Some tableName if any annotationTable exists in a worksheet before the active one
/// </summary>
/// <param name="context"></param>
let tryGetPrevAnnotationTableName (context: RequestContext) =
    promise {

        let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|" tables"|]))
        let activeWorksheet = context.workbook.worksheets.getActiveWorksheet()
        let tables = context.workbook.tables
        let _ =
            activeWorksheet.load(propertyNames=U2.Case2 (ResizeArray[|"position"|])) |> ignore
            tables.load(propertyNames=U2.Case2 (ResizeArray[|"items";"worksheet";"name"; "position"; "values"|]))

        let! prevTable = context.sync().``then``(fun _ ->
            /// Get all names of all tables in the whole workbook.
            let prevTable =
                tables.items
                |> Seq.toArray
                |> Array.choose (fun table ->
                    if table.name.StartsWith(ARCtrl.Spreadsheet.ArcTable.annotationTablePrefix) then
                        Some (table.worksheet.position ,table.name)
                    else
                        None
                )
                |> Array.filter(fun (wp, _) -> activeWorksheet.position - wp > 0.)
                |> Array.sortBy(fun (wp, _) ->
                    activeWorksheet.position - wp
                )
                |> Array.tryHead
            Option.bind (snd >> Some) prevTable
        )

        return prevTable
    }

/// <summary>
/// Checks whether the active excel table is valid or not
/// </summary>
let getValidatedExcelTable excelTable context =
    promise {
        let! indexedErrors = validateExcelTable excelTable context

        let messages =
            if indexedErrors.Length > 0 then
                indexedErrors
                |> List.ofArray
                |> List.collect (fun (ex, header ) ->
                    [InteropLogging.Msg.create InteropLogging.Warning
                        $"Table is not a valid ARC table / ISA table: {ex.Message}. The column {header} is not valid! It needs further inspection what causes the error."
                    ])
            else
                []

        if messages.IsEmpty then
            return Result.Ok excelTable
        else
            return Result.Error messages
    }

/// <summary>
/// Get all annotation tables
/// </summary>
/// <param name="tables"></param>
let getAnnotationTables (tables: TableCollection) =
    tables.items
    |> Seq.toArray
    |> Array.filter (fun table ->
        table.name.StartsWith(ARCtrl.Spreadsheet.ArcTable.annotationTablePrefix))

/// <summary>
/// Try select the annotation table from the current active work sheet
/// </summary>
/// <param name="context"></param>
let tryGetActiveExcelTable (context: RequestContext) =
    promise {
        let activeWorksheet = context.workbook.worksheets.getActiveWorksheet()
        let tables = context.workbook.tables
        let _ =
            activeWorksheet.load(propertyNames=U2.Case2 (ResizeArray[|"position"|])) |> ignore
            context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|])) |> ignore
            tables.load(propertyNames=U2.Case2 (ResizeArray[|"items"; "worksheet"; "name"; "position"; "style"; "values"|]))
            
        return! context.sync().``then``(fun _ ->
            /// Get name of the table of currently active worksheet.
            let activeTable =
                tables.items
                |> Seq.toArray
                |> Array.tryFind (fun table ->                        
                    table.name.StartsWith(ARCtrl.Spreadsheet.ArcTable.annotationTablePrefix) && table.worksheet.position = activeWorksheet.position
                )
            activeTable
        )
    }

/// <summary>
/// Adopt the tables, columns, and values
/// </summary>
/// <param name="table"></param>
/// <param name="context"></param>
/// <param name="shallHide"></param>
let format(table: Table, context: RequestContext, shallHide: bool) =
    promise {
        let _ = table.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"|]))

        do! context.sync().``then``( fun _ -> ())

        let range = table.getRange()

        range.format.autofitColumns()
        range.format.autofitRows()

        table.columns.items
        |> Array.ofSeq
        |> Array.iter (fun column ->
            if ARCtrl.Spreadsheet.ArcTable.helperColumnStrings |> List.exists (fun cName -> column.name.StartsWith cName) then
                column.getRange().columnHidden <- shallHide)
    }

/// <summary>
/// This function is used to hide all reference columns and to fit rows and columns to their values.
/// The main goal is to improve readability of the table with this function.
/// </summary>
/// <param name="hideRefCols"></param>
/// <param name="context"></param>
let formatActive (context: RequestContext) (shallHide: bool) =
    promise {
        let! table = tryGetActiveExcelTable context

        match table with
        | Some table -> do! format(table, context, shallHide)
        | None -> ()
    }

/// <summary>
/// Get headers from range
/// </summary>
/// <param name="range"></param>
let getHeaders (range: ResizeArray<ResizeArray<obj option>>) =
    range.Item 0
    |> Array.ofSeq
    |> Array.map (fun header -> header.ToString())

/// <summary>
/// Get headers from range
/// </summary>
/// <param name="range"></param>
let getBody (range: ResizeArray<ResizeArray<obj option>>) =
    range
    |> Array.ofSeq
    |> (fun items -> items.[1..])
    |> ResizeArray

/// <summary>
/// Update an existing inputcolumn of an annotation table
/// </summary>
/// <param name="excelTable"></param>
/// <param name="arcTable"></param>
/// <param name="newBB"></param>
let updateInputColumn (excelTable: Table) (arcTable: ArcTable) (newBB: CompositeColumn) =

    let possibleInputColumn = arcTable.TryGetInputColumn()

    if possibleInputColumn.IsSome then

        let inputColumnName = possibleInputColumn.Value.Header.ToString()

        let columns = excelTable.columns
        let inputColumn =
            columns.items
            |> Array.ofSeq
            |> Array.tryFind(fun col -> col.name = inputColumnName)

        if inputColumn.IsSome then

            //Only update the input column when it has a new value
            if inputColumnName <> newBB.Header.ToString() then
                excelTable.columns.items.[(int) inputColumn.Value.index].name <- newBB.Header.ToString()

            let warningMsg =
                if inputColumnName = newBB.Header.ToString() then
                    $"Found existing input column \"{inputColumnName}\". Did not change the column because the new input column is the same \"{excelTable.columns.items.[(int) inputColumn.Value.index].name}\"."
                else
                    $"Found existing input column \"{inputColumnName}\". Changed input column to \"{excelTable.columns.items.[(int) inputColumn.Value.index].name}\"."

            let msg = InteropLogging.Msg.create InteropLogging.Warning warningMsg

            let loggingList = [ msg ]

            loggingList
        else
            failwith "Something went wrong! The update input column is not filled with data! Please report this as a bug to the developers."
    else
        failwith "Something went wrong! The update input column does not exist! Please report this as a bug to the developers."

/// <summary>
/// Add a new inputcolumn to an annotation table
/// </summary>
/// <param name="excelTable"></param>
/// <param name="arcTable"></param>
/// <param name="newBB"></param>
let addInputColumn (excelTable: Table) (arcTable: ArcTable) (newBB: CompositeColumn) =

    if arcTable.TryGetInputColumn().IsSome then
        failwith "Something went wrong! The add input column is filled with data! Please report this as a bug to the developers."

    else

        let rowCount = arcTable.RowCount + 1

        let newColumn = Table.addColumn AppendIndex excelTable (newBB.Header.ToString()) rowCount ""
        let columnBody = newColumn.getDataBodyRange()
        // Fit column width to content
        columnBody.format.autofitColumns()

        let msg = InteropLogging.Msg.create InteropLogging.Info $"Added new input column: {newBB.Header}"

        let loggingList = [ msg ]

        loggingList

/// <summary>
/// Update an existing outputcolumn of an annotation table
/// </summary>
/// <param name="excelTable"></param>
/// <param name="arcTable"></param>
/// <param name="newBB"></param>
let updateOutputColumn (excelTable: Table) (arcTable: ArcTable) (newBB: CompositeColumn) =

    let possibleOutputColumn = arcTable.TryGetOutputColumn()

    if possibleOutputColumn.IsSome then

        let outputColumnName = possibleOutputColumn.Value.Header.ToString()

        let columns = excelTable.columns
        let outputColumn =
            columns.items
            |> Array.ofSeq
            |> Array.tryFind(fun col -> col.name = outputColumnName)

        if outputColumn.IsSome then

            //Only update the output column when it has a new value
            if outputColumnName <> newBB.Header.ToString() then
                excelTable.columns.items.[(int) outputColumn.Value.index].name <- newBB.Header.ToString()

            let warningMsg =
                if outputColumnName = newBB.Header.ToString() then
                    $"Found existing output column \"{outputColumnName}\". Did not change the column because the new output column is the same \"{excelTable.columns.items.[(int) outputColumn.Value.index].name}\"."
                else
                    $"Found existing output column \"{outputColumnName}\". Changed output column to \"{excelTable.columns.items.[(int) outputColumn.Value.index].name}\"."

            let msg = InteropLogging.Msg.create InteropLogging.Warning warningMsg

            let loggingList = [ msg ]

            loggingList
        else
            failwith "Something went wrong! The update output column is not filled with data! Please report this as a bug to the developers."
    else
        failwith "Something went wrong! The update output column does not exist! Please report this as a bug to the developers."

/// <summary>
/// Add a new outputcolumn to an annotation table
/// </summary>
/// <param name="excelTable"></param>
/// <param name="arcTable"></param>
/// <param name="newBB"></param>
let addOutputColumn (excelTable: Table) (arcTable: ArcTable) (newBB: CompositeColumn) =

    if arcTable.TryGetOutputColumn().IsSome then
        failwith "Something went wrong! The add output column is filled with data! Please report this as a bug to the developers."

    else

        let rowCount = arcTable.RowCount + 1

        let newColumn = Table.addColumn AppendIndex excelTable (newBB.Header.ToString()) rowCount ""
        let columnBody = newColumn.getDataBodyRange()
        // Fit column width to content
        columnBody.format.autofitColumns()

        let msg = InteropLogging.Msg.create InteropLogging.Info $"Added new output column: {newBB.Header}"

        let loggingList = [ msg ]

        loggingList

/// <summary>
/// Checks whether the given worksheet name exists or not and updates it, when it already exists by adding a number
/// </summary>
/// <param name="templateName"></param>
/// <param name="context"></param>
let getNewActiveWorkSheetName (worksheetName: string) (context: RequestContext) =
    promise {
        let worksheets = context.workbook.worksheets
        let activeWorksheet = context.workbook.worksheets.getActiveWorksheet()
        let _ =
            activeWorksheet.load(propertyNames = U2.Case2 (ResizeArray["name"])) |> ignore
            worksheets.load(propertyNames = U2.Case2 (ResizeArray["items"; "name"]))

        do! context.sync()

        let worksheetNames = worksheets.items |> Seq.map (fun item -> item.name) |> Array.ofSeq
        let worksheetName = System.Text.RegularExpressions.Regex.Replace(worksheetName, "\W", "")

        if (Array.contains worksheetName worksheetNames) then
            let nameCount =
                worksheetNames
                |> Array.filter(fun item -> item.Contains(worksheetName))
                |> Array.length
            return (worksheetName + (nameCount.ToString()))
        else
            return worksheetName
    }
