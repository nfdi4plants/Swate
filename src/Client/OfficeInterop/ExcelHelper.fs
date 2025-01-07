module ExcelHelper

open Fable.Core
open ExcelJS.Fable
open Excel

open OfficeInterop
open OfficeInterop.ExcelUtil

open ARCtrl
open ArcCtrlHelper

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

        let newColumn = ExcelUtil.Table.addColumn AppendIndex excelTable (newBB.Header.ToString()) rowCount ""
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

        let newColumn = ExcelUtil.Table.addColumn AppendIndex excelTable (newBB.Header.ToString()) rowCount ""
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
