module OfficeInterop.Core

open System.Collections.Generic

open Fable.Core
open ExcelJS.Fable
open Excel
open GlobalBindings

open Shared
open Database
open DTO

open OfficeInterop
open OfficeInterop.ExcelUtil

open ARCtrl
open ARCtrl.Spreadsheet

[<AutoOpen>]
module ARCtrlUtil =

    /// <summary>
    /// Check whether the selected column is a reference column or not
    /// </summary>
    /// <param name="name"></param>
    let isReferenceColumn (name: string) =
        ARCtrl.Spreadsheet.ArcTable.helperColumnStrings
        |> Seq.exists (fun cName -> name.StartsWith cName)

    /// <summary>
    /// Group the columns to building blocks
    /// </summary>
    /// <param name="headers"></param>
    let groupToBuildingBlocks (headers: string []) =
        let ra: ResizeArray<ResizeArray<int*string>> = ResizeArray()
        headers
        |> Array.iteri (fun i header ->
            if isReferenceColumn header then
                ra.[ra.Count-1].Add(i, header)
            else
                ra.Add(ResizeArray([(i, header)]))
        )
        ra

    let isTopLevelMetadataSheet (worksheetName: string) =
        match worksheetName with
        | name when
            ArcAssay.isMetadataSheetName name
            || ArcInvestigation.isMetadataSheetName name
            || ArcStudy.isMetadataSheetName name
            || Template.isMetadataSheetName name -> true
        | _ -> false

    /// <summary>
    /// Get the associated CompositeColumn for the given column index. Returns raw column names and indices associated to all compartments of the CompositeColumn.
    /// </summary>
    /// <param name="table"></param>
    /// <param name="columnIndex"></param>
    /// <param name="context"></param>
    let getCompositeColumnInfoByIndex (table: Table) (columnIndex: float) (context: RequestContext) =
        promise {
            let headerRange = table.getHeaderRowRange()
            let _ = headerRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore

            return! context.sync().``then``(fun _ ->
                let rebasedIndex = columnIndex - headerRange.columnIndex |> int
                if rebasedIndex < 0 || rebasedIndex >= (int headerRange.columnCount) then
                    failwith "Cannot select building block outside of annotation table!"
                let headers: string [] = [|for v in headerRange.values.[0] do v.Value :?> string|]
                let selectedHeader = rebasedIndex, headers.[rebasedIndex]
                let buildingBlockGroups = groupToBuildingBlocks headers
                let selectedBuildingBlock =
                    buildingBlockGroups.Find(fun bb -> bb.Contains selectedHeader)
                selectedBuildingBlock
            )
        }

module ARCtrlExtensions =

    open System

    type ArcFiles with

        /// <summary>
        /// Output returns the expected sheetname and metadata values in string seqs form.
        /// </summary>
        member this.MetadataToExcelStringValues() =
            match this with
            | ArcFiles.Assay assay ->
                let metadataWorksheetName = ArcAssay.metadataSheetName
                let seqOfSeqs = ArcAssay.toMetadataCollection assay
                metadataWorksheetName, seqOfSeqs
            | ArcFiles.Investigation investigation ->
                let metadataWorksheetName = ArcInvestigation.metadataSheetName
                let seqOfSeqs = ArcInvestigation.toMetadataCollection investigation
                metadataWorksheetName, seqOfSeqs
            | ArcFiles.Study (study, assays) ->
                let metadataWorksheetName = ArcStudy.metadataSheetName
                let seqOfSeqs = ArcStudy.toMetadataCollection study (Option.whereNot List.isEmpty assays)
                metadataWorksheetName, seqOfSeqs
            | ArcFiles.Template template ->
                let metadataWorksheetName = Template.metaDataSheetName
                let seqOfSeqs = Template.toMetadataCollection template
                metadataWorksheetName, seqOfSeqs

    type ArcTable with

        /// <summary>
        /// Creates ArcTable based on table name and collections of strings, representing columns and rows.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="headers"></param>
        /// <param name="rows"></param>
        static member fromStringSeqs(name: string, headers: #seq<string>, rows: #seq<#seq<string>>) =

            let columns =
                Seq.append [headers] rows
                |> Seq.transpose

            let columnsList =
                columns
                |> Seq.toArray
                |> Array.map (Seq.toArray)

            let compositeColumns = ArcTable.composeColumns columnsList

            let arcTable =
                ArcTable.init name
                |> ArcTable.addColumns(compositeColumns, skipFillMissing = true)

            arcTable

        /// <summary>
        /// Transforms ArcTable to excel compatible "values", row major
        /// </summary>
        member this.ToStringSeqs() =

            // Cancel if there are no columns
            if this.Columns.Length = 0 then
                ResizeArray()
            else
                let columns =
                    this.Columns
                    |> List.ofArray
                    |> List.sortBy ArcTable.classifyColumnOrder
                    |> List.collect CompositeColumn.toStringCellColumns
                    |> Seq.transpose
                    |> Seq.map (fun column ->
                        column |> Seq.map (box >> Some)
                        |> ResizeArray
                    )
                    |> ResizeArray

                columns

        /// <summary>
        /// Try to create an arc table from an excel table
        /// </summary>
        /// <param name="excelTable"></param>
        /// <param name="context"></param>
        static member fromExcelTable (excelTable: Table, context: RequestContext) =
            promise {
                //Get headers and body
                let headerRange = excelTable.getHeaderRowRange()
                let bodyRowRange = excelTable.getDataBodyRange()

                let _ =
                    excelTable.load(U2.Case2 (ResizeArray [|"name"|])) |> ignore
                    headerRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore
                    bodyRowRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore
                    

                return! context.sync().``then``(fun _ ->
                    let headers =
                        headerRange.values.[0]
                        |> Seq.map (fun item ->
                            item
                            |> Option.map string
                            |> Option.defaultValue ""
                            |> (fun s -> s.TrimEnd())
                        )
                    let bodyRows =
                        bodyRowRange.values
                        |> Seq.map (fun items ->
                            items
                            |> Seq.map (fun item ->
                                item
                                |> Option.map string
                                |> Option.defaultValue ""
                            )
                        )
                    try
                        ArcTable.fromStringSeqs(excelTable.worksheet.name, headers, bodyRows) |> Result.Ok
                    with
                        | exn -> Result.Error exn
                )
            }

        /// <summary>
        /// Checks whether the string seqs are part of a valid arc table or not and returns potential errors and index of them in the annotation table
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="rows"></param>
        static member validate(headers: #seq<string>, rows: #seq<#seq<string>>) =

            let columns =
                Seq.append [headers] rows
                |> Seq.transpose

            let columnsArray =
                columns
                |> Seq.toArray
                |> Array.map (Seq.toArray)

            let groupedColumns = columnsArray |> ArcTable.groupColumnsByHeader

            let indexedError =
                groupedColumns
                |> Array.mapi (fun i c ->
                    try
                        let _ = CompositeColumn.fromStringCellColumns c
                        None
                    with
                    | ex ->
                        //The target index must be adapted depending on the position of the error column
                        //because the columns were group based on potential main columns
                        let hasMainColumn =
                            groupedColumns.[i]
                            |> Array.map (fun gc ->
                                CompositeHeader.Cases
                                |> Array.exists (fun (_, header) -> gc.[0].StartsWith(header))
                            )
                            |> Array.contains true

                        if hasMainColumn then Some (ex, i)
                        else Some (ex, i - 1)
                )
                |> Array.filter (fun i -> i.IsSome)

            let indexedError =
                if indexedError.Length > 0 then indexedError |> Array.map (fun i -> i.Value)
                else [||]

            let newHeaders = headers |> Array.ofSeq

            let errorIndices =
                indexedError
                |> Array.map (fun (ex, bi) ->
                    ex,
                    groupedColumns.[0..bi]
                    |> Array.map (fun bb -> bb.Length)
                    |> Array.sum
                    |> (fun i -> newHeaders.[i])
                )

            errorIndices

        /// <summary>
        /// Validate whether the selected excel table is a valid ARC / ISA table
        /// </summary>
        /// <param name="excelTable"></param>
        /// <param name="context"></param>
        static member validateExcelTable (excelTable: Table, context: RequestContext) =

            promise {
                //Get headers and body
                let headerRange = excelTable.getHeaderRowRange()
                let bodyRowRange = excelTable.getDataBodyRange()

                let _ =
                    headerRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore
                    bodyRowRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore

                do! context.sync()

                let inMemoryTable =

                    let headers =
                        headerRange.values.[0]
                        |> Seq.map (fun item ->
                            item
                            |> Option.map string
                            |> Option.defaultValue ""
                            |> (fun s -> s.TrimEnd())
                        )

                    let bodyRows =
                        bodyRowRange.values
                        |> Seq.map (fun items ->
                            items
                            |> Seq.map (fun item ->
                                item
                                |> Option.map string
                                |> Option.defaultValue ""
                            )
                        )

                    ArcTable.validate(headers, bodyRows)

                return inMemoryTable
            }

        /// <summary>
        /// Try to get a arc table from excel file based on excel table name
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="context"></param>
        static member fromExcelTableName (tableName: string, context: RequestContext) =

            promise {
                let! table = ExcelUtil.tryGetTableByName context tableName

                match table with
                | Some (table, _, _) ->
                    let! inMemoryTable = ArcTable.fromExcelTable(table, context)
                    return inMemoryTable
                | None ->
                    return Result.Error(exn $"Error. No table with the given name {tableName} found!")
            }

open System

open ARCtrlUtil
open ARCtrlExtensions

// I retrieve the index of the currently opened worksheet, here the new table should be created.
// I retrieve all annotationTables in the workbook. I filter out all annotationTables that are on a worksheet with a lower index than the index of the currently opened worksheet.
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
                |> Array.choose (fun x ->
                    if x.name.StartsWith(ARCtrl.Spreadsheet.ArcTable.annotationTablePrefix) then
                        Some (x.worksheet.position ,x.name)
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
/// Get the previous arc table to the active worksheet
/// </summary>
/// <param name="context"></param>
let tryGetPrevArcTable (context: RequestContext) : JS.Promise<ArcTable option> =
    promise {
        let! prevTableName = tryGetPrevAnnotationTableName context

        match prevTableName with
        | Some name ->

            let! result = ArcTable.fromExcelTableName (name, context)
            return
                match result with
                | Ok table -> Some table
                | Result.Error _ -> None

        | None ->
            return None
    }

/// <summary>
/// Get output column of arc excel table
/// </summary>
/// <param name="context"></param>
let tryGetPrevTableOutput (context: RequestContext) =
    promise {

        let! inMemoryTable = tryGetPrevArcTable context

        if inMemoryTable.IsSome then

            let outputColumns = inMemoryTable.Value.TryGetOutputColumn()

            if(outputColumns.IsSome) then

                let outputValues =
                    CompositeColumn.toStringCellColumns outputColumns.Value
                    |> (fun lists -> lists.Head.Head :: lists.Head.Tail)
                    |> Array.ofList

                if outputValues.Length > 0 then return Some outputValues
                else return None

            else

                return None

        else
            return None
    }


/// Annotation table refers to any excel table object starting with the ARCtrl certified annotation table prefix.
module AnnotationTable =

    /// <summary>
    /// Checks whether the active excel table is valid or not
    /// </summary>
    let validate excelTable context =
        promise {
            let! indexedErrors = ArcTable.validateExcelTable(excelTable, context)

            let messages =
                if indexedErrors.Length > 0 then
                    indexedErrors
                    |> List.ofArray
                    |> List.collect (fun (ex, header ) ->
                        [InteropLogging.Msg.create InteropLogging.Warning
                            $"Table is not a valid ARC table / ISA table: {ex.Message}. The column {header} is not valid! It needs further inspection what causes the error.";
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
    let tryGetActive (context: RequestContext) =
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
    /// This function is used to create a new annotation table. 'isDark' refers to the current styling of excel (darkmode, or not).
    /// </summary>
    /// <param name="isDark"></param>
    /// <param name="tryUseLastOutput"></param>
    /// <param name="range"></param>
    /// <param name="context"></param>
    let createAtRange (isDark: bool, tryUseLastOutput: bool, range: Excel.Range, context: RequestContext) =

        let newName = ExcelUtil.createNewTableName()

        /// decide table style by input parameter
        let style =
            if isDark then
                "TableStyleMedium15"
            else
                ExcelUtil.TableStyleLight

        // The next part loads relevant information from the excel objects and allows us to access them after 'context.sync()'
        let tableRange = range.getColumn(0)
        let _ = tableRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount"; "address"; "isEntireColumn"; "worksheet"])))

        let mutable activeSheet = tableRange.worksheet
        let _ = activeSheet.load(U2.Case2 (ResizeArray[|"tables"|]))
        let activeTables = activeSheet.tables.load(propertyNames=U2.Case1 "items")

        let r = context.runtime.load(U2.Case1 "enableEvents")

        //Required because a new tablerange is required for the new table range
        let mutable hasCreatedNewWorkSheet = false

        promise {
            // sync with proxy objects after loading values from excel
            do! context.sync().``then``( fun _ ->

                // Filter all names of tables on the active worksheet for names starting with "annotationTable".
                let annoTables = getAnnotationTables activeTables

                match annoTables.Length with
                //Create a new annotation table in the active worksheet
                | 0 -> ()
                //Create a mew worksheet with a new annotation table when the active worksheet already contains one
                | x when x = 1 ->
                    //Create new worksheet and set it active
                    let worksheet = context.workbook.worksheets.add()
                    worksheet.activate()
                    activeSheet <- worksheet
                    hasCreatedNewWorkSheet <- true

                // Fail the function if there are more than 1 annotation table in the active worksheet.
                // This check is done, to only have one annotationTable per workSheet.
                | x when x > 1 ->
                    failwith "The active worksheet contains more than one annotationTable. This should not happen. Please report this as a bug to the developers."
                | _ ->
                    failwith "The active worksheet contains a negative number of annotation tables. Obviously this cannot happen. Please report this as a bug to the developers."
            )

            // Is user input signals to try and find+reuse the output from the previous annotationTable do this, otherwise just return empty array
            let! prevTableOutput =
                if (tryUseLastOutput) then tryGetPrevTableOutput context
                else promise {return None}

            let _ = activeSheet.load(propertyNames = U2.Case2 (ResizeArray[|"name"|])) |> ignore

            let newTableRange =
                if hasCreatedNewWorkSheet then
                    let rowCount = match prevTableOutput with | Some rows -> float rows.Length | None -> tableRange.rowCount
                    activeSheet.getRangeByIndexes(tableRange.rowIndex, tableRange.columnIndex, rowCount, 1)
                        .load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount"; "address"; "isEntireColumn"; "worksheet"])))
                else tableRange

            // sync with proxy objects after loading values from excel
            let! table = context.sync().``then``( fun _ ->

                r.enableEvents <- false

                // Create table in current worksheet
                let inMemoryTable = ArcTable.init(newName)

                let newCells =
                    match prevTableOutput with
                    | Some rows -> rows |> Array.map CompositeCell.FreeText
                    | None -> Array.init (int tableRange.rowCount - 1) (fun _ -> CompositeCell.emptyFreeText)

                inMemoryTable.AddColumn(CompositeHeader.Input IOType.Source, newCells)

                let tableStrings = inMemoryTable.ToStringSeqs()

                let excelTable = activeSheet.tables.add(U2.Case1 newTableRange, true)

                // Update annotationTable name
                excelTable.name <- newName

                newTableRange.values <- tableStrings

                // Update annotationTable style
                excelTable.style <- style
                excelTable
            )

            let _ = table.rows.load(propertyNames = U2.Case2 (ResizeArray[|"count"|]))

            //let! table, logging = context.sync().``then``(fun _ ->

            //    //logic to compare size of previous table and current table and adapt size of inMemory table
            //    if prevTableOutput.IsSome then
            //        //Skip header because it is newly generated for inMemory table
            //        let newColValues =
            //            prevTableOutput.Value.[1..]
            //            |> Array.map (fun cell ->
            //                [|cell|]
            //                |> Array.map (box >> Some)
            //                |> ResizeArray
            //            ) |> ResizeArray

            //        let rowCount0 = int table.rows.count
            //        let diff = rowCount0 - newColValues.Count

            //        if diff > 0 then // table larger than values -> Delete rows to reduce excel table size to previous table size
            //            table.rows?deleteRowsAt(newColValues.Count, diff)
            //        elif diff < 0 then // more values than table -> Add rows to increase excel table size to previous table size
            //            let absolute = (-1) * diff
            //            let nextvalues = createMatrixForTables 1 absolute ""
            //            table.rows.add(-1, U4.Case1 nextvalues) |> ignore

            //        let body = (table.columns.getItemAt 0.).getDataBodyRange()
            //        body.values <- newColValues

            //    // Fit widths and heights of cols and rows to value size. (In this case the new column headers).
            //    activeSheet.getUsedRange().format.autofitColumns()
            //    activeSheet.getUsedRange().format.autofitRows()

            //    r.enableEvents <- true

            //    // Return info message

            //    table, logging
            //)
            let logging = InteropLogging.Msg.create InteropLogging.Info (sprintf "Annotation Table created in [%s] with dimensions 2c x (%.0f + 1h)r." newTableRange.address (newTableRange.rowCount - 1.))

            return (table, logging)
        }

    /// <summary>
    /// This function is used to create a new annotation table. 'isDark' refers to the current styling of excel (darkmode, or not)
    /// </summary>
    /// <param name="isDark"></param>
    /// <param name="tryUseLastOutput"></param>
    let create (isDark: bool, tryUseLastOutput: bool) =
        Excel.run (fun context ->
            let selectedRange = context.workbook.getSelectedRange()
            promise {
                let! newTableLogging = createAtRange (isDark, tryUseLastOutput, selectedRange, context)

                // Interop logging expects list of logs
                return [snd newTableLogging]
            }
        )

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
            let! table = tryGetActive context

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
/// Try find annotation table in active worksheet and parse to ArcTable
/// </summary>
/// <param name="context"></param>
let tryGetActiveArcTable context: JS.Promise<Result<ArcTable, exn>> =
    promise {
        let! excelTable = AnnotationTable.tryGetActive context
        match excelTable with
        | Some excelTable ->
            let! arcTable = ArcTable.fromExcelTable(excelTable, context)
            return arcTable
        | None ->
            return Result.Error (exn "Error! No active annotation table found!")
    }

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
/// Add a building block at the desired index to the given annotation table
/// </summary>
/// <param name="excelTable"></param>
/// <param name="arcTable"></param>
/// <param name="newBB"></param>
/// <param name="headerRange"></param>
/// <param name="selectedRange"></param>
let addCompositeColumn (excelTable: Table) (arcTable: ArcTable) (newColumn: CompositeColumn) (headerRange: Excel.Range) (selectedRange: Excel.Range) =

    let rowCount = arcTable.RowCount + 1

    let buildingBlockCells = Spreadsheet.CompositeColumn.toStringCellColumns newColumn

    let targetIndex =
        let excelIndex =
            let diff = selectedRange.columnIndex - headerRange.columnIndex |> int
            let columnCount = headerRange.columnCount |> int
            let maxLength = columnCount-1
            if diff < 0 then
                maxLength
            elif diff > maxLength then
                maxLength
            else
                diff
            |> float

        let headers = AnnotationTable.getHeaders (arcTable.ToStringSeqs())

        if excelIndex < float (headers.Length - 1) then

            //We want to start looking after the chosen column in order to ignore the current column because it could be a main column
            let headers = headers.[(int) excelIndex + 1..]

            let targetIndex =
                [|
                    for i in 0..headers.Length - 1 do
                        let header = headers.[i]
                        if ARCtrl.Spreadsheet.ArcTable.helperColumnStrings |> List.exists (fun cName -> header.StartsWith cName) then
                            ()
                        else
                            i
                |]
                |> Array.sortBy(fun index -> index)
                |> Array.tryHead

            if targetIndex.IsSome then
                //The +1 makes sure, we add the column after the column we have currently chosen
                excelIndex + (float) targetIndex.Value + 1.

            //The +1 makes sure, we add the column after the column we have currently chosen
            else AppendIndex
        else
            //Add the new building block to the end of the table
            AppendIndex

    let headers = AnnotationTable.getHeaders headerRange.values

    buildingBlockCells
    |> List.iteri(fun i bbCell ->
        //check and extend header to avoid duplicates
        let newHeader = extendName headers bbCell.Head
        let colIndex =
            if targetIndex >= 0 then targetIndex + (float) i
            else AppendIndex
        let value =
            if bbCell.Tail.IsEmpty then ""
            else bbCell.Tail.Head
        let column = ExcelUtil.Table.addColumn colIndex excelTable newHeader rowCount value

        column.getRange().format.autofitColumns()

        if ARCtrl.Spreadsheet.ArcTable.helperColumnStrings |> List.exists (fun cName -> newHeader.StartsWith cName) then
            column.getRange().columnHidden <- true
    )

    let msg = InteropLogging.Msg.create InteropLogging.Info $"Added new term column: {newColumn.Header}"

    let loggingList = [ msg ]

    loggingList

/// <summary>
/// Prepare the given table to be joined with the currently active annotation table
/// </summary>
/// <param name="tableToAdd"></param>
let prepareTemplateInMemory (table: Table) (tableToAdd: ArcTable) (context: RequestContext) =
    promise {
        let! originTableRes = ArcTable.fromExcelTable(table, context)

        match originTableRes with
        | Result.Error _ ->
            return failwith $"Failed to create arc table for table {table.name}"
        | Result.Ok originTable ->
            let finalTable = Table.selectiveTablePrepare originTable tableToAdd

            let selectedRange = context.workbook.getSelectedRange()

            let tableStartIndex = table.getRange()

            let _ =
                tableStartIndex.load(propertyNames=U2.Case2 (ResizeArray[|"columnIndex"|])) |> ignore
                selectedRange.load(propertyNames=U2.Case2 (ResizeArray[|"columnIndex"|]))

            // sync with proxy objects after loading values from excel
            do! context.sync().``then``( fun _ -> ())

            let targetIndex =
                let adaptedStartIndex = selectedRange.columnIndex - tableStartIndex.columnIndex
                if adaptedStartIndex > float (originTable.ColumnCount) then originTable.ColumnCount
                else int adaptedStartIndex + 1

            return finalTable, Some (targetIndex)
    }

/// <summary>
/// Add the given arc table to the active annotation table at the desired index
/// </summary>
/// <param name="tableToAdd"></param>
/// <param name="index"></param>
/// <param name="options"></param>
let joinTable (tableToAdd: ArcTable, options: TableJoinOptions option) =
    Excel.run(fun context ->
        promise {

            //When a name is available get the annotation and arctable for easy access of indices and value adaption
            //Annotation table enables a easy way to adapt the table, updating existing and adding new columns
            let! result = AnnotationTable.tryGetActive context

            match result with
            | Some excelTable ->
                let! (tableToAdd: ArcTable, index: int option) = prepareTemplateInMemory excelTable tableToAdd context

                //Arctable enables a fast check for the existence of input- and output-columns and their indices
                let! arcTableRes = ArcTable.fromExcelTable(excelTable, context)

                //When both tables could be accessed succesfully then check what kind of column shall be added an whether it is already there or not
                match arcTableRes with
                | Result.Ok arcTable ->
                    arcTable.Join(tableToAdd, ?index=index, ?joinOptions=options, forceReplace=true)

                    let newTableRange = excelTable.getRange()

                    let _ = newTableRange.load(propertyNames = U2.Case2 (ResizeArray["rowCount";]))

                    do! context.sync().``then``(fun _ ->
                        excelTable.delete()
                    )

                    let! (newTable, _) = AnnotationTable.createAtRange(false, false, newTableRange, context)

                    let _ = newTable.load(propertyNames = U2.Case2 (ResizeArray["name"; "values"; "columns";]))

                    do! context.sync().``then``(fun _ ->

                        newTable.name <- excelTable.name

                        let headerNames =
                            let names = AnnotationTable.getHeaders (arcTable.ToStringSeqs())
                            names
                            |> Array.map (fun name -> extendName names name)

                        headerNames
                        |> Array.iteri(fun i header ->
                            ExcelUtil.Table.addColumn i newTable header (int newTableRange.rowCount) "" |> ignore)
                    )

                    let bodyRange = newTable.getDataBodyRange()

                    let _ = bodyRange.load(propertyNames = U2.Case2 (ResizeArray["columnCount"; "rowCount"; "values"]))

                    do! context.sync().``then``(fun _ ->

                        //We delete the annotation table because we cannot overwrite an existing one
                        //As a result we create a new annotation table that has one column
                        //We delete the newly created column of the newly created table
                        newTable.columns.getItemAt(bodyRange.columnCount - 1.).delete()
                    )

                    let newBodyRange = newTable.getDataBodyRange()

                    let _ =
                        newTable.columns.load(propertyNames = U2.Case2 (ResizeArray["name"; "items"])) |> ignore
                        newBodyRange.load(propertyNames = U2.Case2 (ResizeArray["name"; "columnCount"; "values"]))

                    do! context.sync()

                    do! AnnotationTable.format(newTable, context, true)

                    return [InteropLogging.Msg.create InteropLogging.Warning $"Joined template {tableToAdd.Name} to table {excelTable.name}!"]
                | Result.Error _ ->
                    return [InteropLogging.Msg.create InteropLogging.Error "No arc table could be created! This should not happen at this stage! Please report this as a bug to the developers.!"]
            | None -> return [InteropLogging.NoActiveTableMsg]
        }
    )

let tryGetSelectedTableIndex (table: Table) (context: RequestContext) =
    promise {
        let selectedRange = context.workbook.getSelectedRange().load(U2.Case2 (ResizeArray[|"columnIndex"|]))
        let headerRange = table.getHeaderRowRange()
        let _ = headerRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "columnCount"|])) |> ignore

        return! context.sync().``then``(fun _ ->
            let rebasedIndex = selectedRange.columnIndex - headerRange.columnIndex |> int
            if rebasedIndex < 0 || rebasedIndex >= (int headerRange.columnCount) then
                None
            else
                Some rebasedIndex
                //failwith "Cannot select building block outside of annotation table!"
        )
    }

/// <summary>
/// Returns a ResizeArray of indices and header names for the selected building block
/// The indices are rebased to the excel annotation table.
/// </summary>
/// <param name="columns"></param>
/// <param name="selectedIndex"></param>
let getSelectedBuildingBlock (table: Table) (context: RequestContext) =
    promise {
        let selectedRange = context.workbook.getSelectedRange().load(U2.Case2 (ResizeArray[|"columnIndex"|]))
        let headerRange = table.getHeaderRowRange()
        let _ = headerRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore

        return! context.sync().``then``(fun _ ->
            let rebasedIndex = selectedRange.columnIndex - headerRange.columnIndex |> int
            if rebasedIndex < 0 || rebasedIndex >= (int headerRange.columnCount) then
                failwith "Cannot select building block outside of annotation table!"
            let headers: string [] = [|for v in headerRange.values.[0] do v.Value :?> string|]
            let selectedHeader = rebasedIndex, headers.[rebasedIndex]
            let buildingBlockGroups = groupToBuildingBlocks headers
            let selectedBuildingBlock =
                buildingBlockGroups.Find(fun bb -> bb.Contains selectedHeader)
            selectedBuildingBlock
        )
    }

/// <summary>
/// Returns a ResizeArray of indices and header names for the selected building block
/// The indices are rebased to the excel annotation table.
/// </summary>
/// <param name="columns"></param>
/// <param name="selectedIndex"></param>
let getBuildingBlockByColumnIndex (table: Table) (excelColumnIndex: float) (context: RequestContext) =
    promise {
        let headerRange = table.getHeaderRowRange()
        let _ = headerRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore

        return! context.sync().``then``(fun _ ->
            let rebasedIndex = excelColumnIndex - headerRange.columnIndex |> int
            if rebasedIndex < 0 || rebasedIndex >= (int headerRange.columnCount) then
                failwith "Cannot select building block outside of annotation table!"
            let headers: string [] = [|for v in headerRange.values.[0] do v.Value :?> string|]
            let selectedHeader = rebasedIndex, headers.[rebasedIndex]
            let buildingBlockGroups = groupToBuildingBlocks headers
            let selectedBuildingBlock =
                buildingBlockGroups.Find(fun bb -> bb.Contains selectedHeader)
            selectedBuildingBlock
        )
    }

/// <summary>
/// Get the main column of the arc table of the selected building block of the active annotation table
/// </summary>
let getArcIndex (excelTable: Table) (excelColumnIndex: float) (context: RequestContext) =
    promise {
        let! selectedBlock = getBuildingBlockByColumnIndex excelTable excelColumnIndex context

        let protoHeaders = excelTable.getHeaderRowRange()
        let _ = protoHeaders.load(U2.Case2 (ResizeArray(["values"])))

        do! context.sync()

        let headers = AnnotationTable.getHeaders protoHeaders.values

        let arcTableIndices = (groupToBuildingBlocks headers) |> Array.ofSeq |> Array.map (fun i -> i |> Array.ofSeq)

        let arcTableIndex =
            let potResult = arcTableIndices |> Array.mapi (fun i c -> i, c |> Array.tryFind (fun (_, s) -> s = snd selectedBlock.[0]))
            let result = potResult |> Array.filter (fun (_, c) -> c.IsSome) |> Array.map (fun (i, c) -> i, c.Value)
            Array.tryHead result

        let arcTableIndex =
            if arcTableIndex.IsSome then
                fst arcTableIndex.Value
            else failwith "Could not find a fitting arc table index"

        return arcTableIndex
    }

/// <summary>
/// Returns a ResizeArray of indices and header names for the selected building block
/// The indices are rebased to the excel annotation table.
/// </summary>
/// <param name="columns"></param>
/// <param name="selectedIndex"></param>
let getSelectedBuildingBlockCell (table: Table) (context: RequestContext) =
    promise {

        let selectedRange = context.workbook.getSelectedRange().load(U2.Case2 (ResizeArray[|"columnIndex"; "rowIndex";|]))

        let headerRange = table.getHeaderRowRange()
        let _ = headerRange.load(U2.Case2 (ResizeArray [|"columnCount"; "columnIndex"; "rowIndex"; "values";|])) |> ignore

        return! context.sync().``then``(fun _ ->
            let rebasedIndex = selectedRange.columnIndex - headerRange.columnIndex |> int
            if rebasedIndex < 0 || rebasedIndex >= (int headerRange.columnCount) then
                failwith "Cannot select building block outside of annotation table!"
            let headers: string [] = [|for v in headerRange.values.[0] do v.Value :?> string|]
            let selectedHeader = rebasedIndex, headers.[rebasedIndex]
            let buildingBlockGroups = groupToBuildingBlocks headers
            let selectedBuildingBlock =
                buildingBlockGroups.Find(fun bb -> bb.Contains selectedHeader)
            selectedBuildingBlock, selectedRange.rowIndex
        )
    }

/// <summary>
/// Get the main column of the arc table of the selected building block of the active annotation table
/// </summary>
let getArcMainColumn (excelTable: Table) (arcTable: ArcTable) (context: RequestContext) =
    promise {
        let! selectedBlock = getSelectedBuildingBlock excelTable context

        let protoHeaders = excelTable.getHeaderRowRange()
        let _ = protoHeaders.load(U2.Case2 (ResizeArray(["values"])))

        do! context.sync()

        let headers = AnnotationTable.getHeaders protoHeaders.values

        let arcTableIndices = (groupToBuildingBlocks headers) |> Array.ofSeq |> Array.map (fun i -> i |> Array.ofSeq)

        let arcTableIndex =
            let potResult = arcTableIndices |> Array.mapi (fun i c -> i, c |> Array.tryFind (fun (_, s) -> s = snd selectedBlock.[0]))
            let result = potResult |> Array.filter (fun (_, c) -> c.IsSome) |> Array.map (fun (i, c) -> i, c.Value)
            Array.tryHead result

        let arcTableIndex, columnName =
            if arcTableIndex.IsSome then
                fst arcTableIndex.Value, snd (snd arcTableIndex.Value)
            else failwith "Could not find a fitting arc table index"

        let targetColumn =
            let potColumn = arcTable.GetColumn arcTableIndex
            if columnName.Contains(potColumn.Header.ToString()) then potColumn
            else failwith "Could not find a fitting arc table index with matchin name"

        return (targetColumn, arcTableIndex)
    }

/// <summary>
/// Get the cell type of the selected cell
/// </summary>
let tryGetSelectedCellType () =
    Excel.run(fun context ->
        promise {
            let! result = AnnotationTable.tryGetActive context

            match result with
            | Some excelTable ->
                let! _, rowIndex = getSelectedBuildingBlockCell excelTable context
                let! arcTableRes = ArcTable.fromExcelTable(excelTable, context)

                match arcTableRes with
                | Result.Ok arcTable ->
                    let! (arcMainColumn, _) = getArcMainColumn excelTable arcTable context

                    if rowIndex > 0 then
                        return
                            match arcMainColumn with
                            | amc when amc.Cells.[(int rowIndex) - 1].isUnitized -> Some CompositeCellDiscriminate.Unitized
                            | amc when amc.Cells.[(int rowIndex) - 1].isTerm -> Some CompositeCellDiscriminate.Term
                            | amc when amc.Cells.[(int rowIndex) - 1].isData -> Some CompositeCellDiscriminate.Data
                            | amc when amc.Cells.[(int rowIndex) - 1].isFreeText -> Some CompositeCellDiscriminate.Text
                            | _ -> None
                    else return None
                | Result.Error _ -> return None
            | None -> return None
        }
    )

/// <summary>
/// Get the valid cell type for the conversion based on input cell type
/// </summary>
/// <param name="cellType"></param>
let tryGetValidConversionCellTypes () =
    Excel.run(fun context ->
        promise {
            let! result = AnnotationTable.tryGetActive context

            match result with
            | Some excelTable ->
                let! _, rowIndex = getSelectedBuildingBlockCell excelTable context
                let! arcTableRes = ArcTable.fromExcelTable(excelTable, context)

                match arcTableRes with
                | Result.Ok arcTable ->
                    let! (arcMainColumn, _) = getArcMainColumn excelTable arcTable context

                    if rowIndex > 0 then
                        return
                            match arcMainColumn with
                            | amc when amc.Cells.[(int rowIndex) - 1].isUnitized -> (Some CompositeCellDiscriminate.Unitized, Some CompositeCellDiscriminate.Term)
                            | amc when amc.Cells.[(int rowIndex) - 1].isTerm -> (Some CompositeCellDiscriminate.Term, Some CompositeCellDiscriminate.Unitized)
                            | amc when amc.Cells.[(int rowIndex) - 1].isData -> (Some CompositeCellDiscriminate.Data, Some CompositeCellDiscriminate.Text)
                            | amc when amc.Cells.[(int rowIndex) - 1].isFreeText ->
                                if (arcMainColumn.Header.isInput || arcMainColumn.Header.isOutput) && arcMainColumn.Header.IsDataColumn then
                                    (Some CompositeCellDiscriminate.Text, Some CompositeCellDiscriminate.Data)
                                else
                                    (Some CompositeCellDiscriminate.Data, None)
                            | _ -> (None, None)
                    else return (None, None)
                | Result.Error _ -> return (None, None)
            | None -> return (None, None)
        }
    )

/// <summary>
/// Select a building block, shifted by adaptedIndex from the selected building block
/// </summary>
/// <param name="table"></param>
/// <param name="adaptedIndex"></param>
/// <param name="context"></param>
let getAdaptedSelectedBuildingBlock (table: Table) (adaptedIndex: float) (context: RequestContext) =
    promise {

        let selectedRange = context.workbook.getSelectedRange().load(U2.Case2 (ResizeArray[|"columnIndex"|]))

        let headerRange = table.getHeaderRowRange()
        let _ = headerRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore

        return! context.sync().``then``(fun _ ->
            let rebasedIndex = selectedRange.columnIndex - headerRange.columnIndex + adaptedIndex |> int
            if rebasedIndex < 0 || rebasedIndex >= (int headerRange.columnCount) then
                failwith "Cannot select building block outside of annotation table!"
            let headers: string [] = [|for v in headerRange.values.[0] do v.Value :?> string|]
            let selectedHeader = rebasedIndex, headers.[rebasedIndex]
            let buildingBlockGroups = groupToBuildingBlocks headers
            let selectedBuildingBlock =
                buildingBlockGroups.Find(fun bb -> bb.Contains selectedHeader)
            selectedBuildingBlock
        )
    }

/// <summary>
/// Delete the annotation block of the selected column in excel
/// </summary>
let removeSelectedAnnotationBlock () =
    Excel.run(fun context ->
        promise {

            let! excelTableRes = AnnotationTable.tryGetActive context

            match excelTableRes with
            | Some excelTable ->
                let! selectedBuildingBlock = getSelectedBuildingBlock excelTable context

                // iterate DESCENDING to avoid index shift
                for i, _ in Seq.sortByDescending fst selectedBuildingBlock do
                    let column = excelTable.columns.getItemAt(i)
                    log $"delete column {i}"
                    column.delete()

                do! context.sync()

                do! AnnotationTable.format(excelTable, context, true)

                return [InteropLogging.Msg.create InteropLogging.Info $"The building block associated with column {snd (selectedBuildingBlock.Item 0)} has been deleted"]
            | None -> return [InteropLogging.NoActiveTableMsg]
        }
    )

/// <summary>
/// Get the main column of the arc table of the selected building block of the active annotation table
/// </summary>
let tryGetArcMainColumnFromFrontEnd () =
    Excel.run(fun context ->
        promise {
            let! excelTableRes = AnnotationTable.tryGetActive context

            match excelTableRes with
            | Some table ->
                let! arcTableRes = ArcTable.fromExcelTable(table, context)

                match arcTableRes with
                | Result.Ok arcTable ->
                    let! column = getArcMainColumn table arcTable context
                    return Some column
                | Result.Error _ -> return None
            | None -> return None
        }
    )

/// <summary>
/// Delete columns of at the given indices of the table
/// </summary>
/// <param name="selectedColumns"></param>
/// <param name="excelTable"></param>
let deleteSelectedExcelColumns (selectedColumns: seq<int>) (excelTable: Table) =
    // iterate DESCENDING to avoid index shift
    for i in Seq.sortByDescending (fun x -> x) selectedColumns do
        let column = excelTable.columns.getItemAt(i)
        log $"delete column {i}"
        column.delete()

/// <summary>
/// Convert the body of the given column into free text
/// </summary>
/// <param name="column"></param>
let convertToFreeTextCell (arcTable: ArcTable) (columnIndex: int) (column: CompositeColumn) rowIndex =
    let freeTextCell = column.Cells.[rowIndex].ToFreeTextCell()
    arcTable.UpdateCellAt(columnIndex, rowIndex, freeTextCell, true)

/// <summary>
/// Convert the body of the given column into data
/// </summary>
/// <param name="column"></param>
let convertToDataCell (arcTable: ArcTable) (columnIndex: int) (column: CompositeColumn) rowIndex =
    let header = column.Header
    let dataCell = column.Cells.[rowIndex].ToDataCell()
    let newHeader =
        match header with
        | header when header.isOutput -> CompositeHeader.Output IOType.Data
        | header when header.isInput -> CompositeHeader.Input IOType.Data
        | _ -> CompositeHeader.FreeText (header.ToString())
    arcTable.UpdateHeader(columnIndex, newHeader, false)
    arcTable.UpdateCellAt(columnIndex, rowIndex, dataCell, true)

/// <summary>
/// Convert the body of the given column into unit
/// </summary>
/// <param name="column"></param>
let convertToUnitCell (column: CompositeColumn) rowIndex =
    let unitCell = column.Cells.[rowIndex].ToUnitizedCell()
    column.Cells.[rowIndex] <- unitCell

/// <summary>
/// Convert the body of the given column into term
/// </summary>
/// <param name="column"></param>
let convertToTermCell (column: CompositeColumn) rowIndex =
    let termCell = column.Cells.[rowIndex].ToTermCell()
    column.Cells.[rowIndex] <- termCell

/// <summary>
/// This function is used to convert building blocks that can be converted. Data building blocks can be converted into free text, free text into data,
/// terms into units and units into terms
/// </summary>
let convertBuildingBlock () =
    Excel.run(fun context ->
        promise {
            let! excelTableRes = AnnotationTable.tryGetActive context

            match excelTableRes with
            | Some excelTable ->
                let! selectedBuildingBlock, rowIndex = getSelectedBuildingBlockCell excelTable context
                let! arcTableRes = ArcTable.fromExcelTable(excelTable, context)

                match arcTableRes with
                | Result.Ok arcTable ->

                    let! (arcMainColumn, arcIndex) = getArcMainColumn excelTable arcTable context

                    let msgText =
                        if rowIndex = 0 then "Headers cannot be converted"
                        else ""

                    match arcMainColumn with
                    | amc when amc.Cells.[(int rowIndex) - 1].isUnitized ->
                        convertToTermCell amc ((int rowIndex) - 1)
                        arcTable.UpdateColumn(arcIndex, arcMainColumn.Header, arcMainColumn.Cells)
                    | amc when amc.Cells.[(int rowIndex) - 1].isTerm ->
                        convertToUnitCell amc ((int rowIndex) - 1)
                        arcTable.UpdateColumn(arcIndex, arcMainColumn.Header, arcMainColumn.Cells)
                    | amc when amc.Cells.[(int rowIndex) - 1].isData -> convertToFreeTextCell arcTable arcIndex amc ((int rowIndex) - 1)
                    | amc when amc.Cells.[(int rowIndex) - 1].isFreeText -> convertToDataCell arcTable arcIndex amc ((int rowIndex) - 1)
                    | _ -> ()

                    let name = excelTable.name
                    let style = excelTable.style

                    let newTableRange = excelTable.getRange()

                    let newExcelTableValues = arcTable.ToStringSeqs()
                    let _ = newTableRange.load(propertyNames = U2.Case2 (ResizeArray["values"; "columnCount"; "rowCount"]))

                    do! context.sync().``then``(fun _ ->
                        let difference = (newExcelTableValues.Item 0).Count - (int newTableRange.columnCount)

                        match difference with
                        | diff when diff > 0 ->
                            for i = 0 to diff - 1 do
                                ExcelUtil.Table.addColumn (newTableRange.columnCount + (float i)) excelTable (i.ToString()) (int newTableRange.rowCount) "" |> ignore
                        | diff when diff < 0 ->
                            for i = 0 downto diff + 1 do
                                ExcelUtil.Table.deleteColumn 0 excelTable
                        | _ -> ()
                    )

                    let newTableRange = excelTable.getRange()
                    let _ = newTableRange.load(propertyNames = U2.Case2 (ResizeArray["values";  "columnCount"]))

                    do! context.sync().``then``(fun _ ->
                        excelTable.delete()
                        newTableRange.values <- newExcelTableValues
                    )

                    let _ = newTableRange.load(propertyNames = U2.Case2 (ResizeArray["values"; "worksheet"]))

                    do! context.sync()

                    let activeSheet = newTableRange.worksheet
                    let _ = activeSheet.load(U2.Case2 (ResizeArray[|"tables"|]))

                    do! context.sync().``then``(fun _ ->
                        let newTable = activeSheet.tables.add(U2.Case1 newTableRange, true)
                        newTable.name <- name
                        newTable.style <- style
                    )

                    let! newTable = AnnotationTable.tryGetActive context

                    match newTable with
                    | Some table -> do! AnnotationTable.format (table, context, true)
                    | _ -> ()

                    let msg =
                        if String.IsNullOrEmpty(msgText) then $"Converted building block of {snd selectedBuildingBlock.[0]} to unit"
                        else msgText

                    return [InteropLogging.Msg.create InteropLogging.Warning msg]
                | Result.Error ex -> return [InteropLogging.Msg.create InteropLogging.Error ex.Message]
            | None -> return [InteropLogging.NoActiveTableMsg]
        }
    )

/// <summary>
/// Validate the selected columns
/// </summary>
/// <param name="excelTable"></param>
/// <param name="selectedIndex"></param>
/// <param name="targetIndex"></param>
/// <param name="context"></param>
let validateSelectedColumns (headerRange: ExcelJS.Fable.Excel.Range, bodyRowRange: ExcelJS.Fable.Excel.Range, selectedIndex: int, targetIndex: int, context: RequestContext) =
    promise {
        let _ =
            headerRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
            bodyRowRange.load(U2.Case2 (ResizeArray [|"values"; "numberFormat"|])) |> ignore

        let! inMemoryTable = context.sync().``then``(fun _ ->

            let getRange (ti:int) (si:int) (range:array<'a>) =
                if ti < si then range.[ti..si]
                else range.[si..ti]

            let headerRow =
                headerRange.values.[0]
                |> Array.ofSeq
                |> getRange targetIndex selectedIndex
                |> Seq.map (fun item ->
                    item
                    |> Option.map string
                    |> Option.defaultValue ""
                    |> (fun s -> s.TrimEnd())
                )

            let bodyRows =
                bodyRowRange.values
                |> Seq.map (fun items ->
                    items
                    |> Array.ofSeq
                    |> getRange targetIndex selectedIndex
                    |> Seq.map (fun item ->
                        item
                        |> Option.map string
                        |> Option.defaultValue ""
                    )
                )

            ArcTable.validate(headerRow, bodyRows)
        )
        return inMemoryTable
    }

/// <summary>
/// Validate the selected building block and those next to it
/// </summary>
/// <param name="excelTable"></param>
/// <param name="context"></param>
let validateBuildingBlock (excelTable: Table, context: RequestContext) =
    promise {
        let columns = excelTable.columns
        let selectedRange = context.workbook.getSelectedRange()

        let _ =
            columns.load(propertyNames = U2.Case2 (ResizeArray[|"count"; "items"; "name"|])) |> ignore
            selectedRange.load(U2.Case2 (ResizeArray [|"values"; "columnIndex"|]))
    
        do! context.sync().``then``( fun _ -> ())

        let columnIndex = selectedRange.columnIndex
        let selectedColumns = columns.items |> Array.ofSeq
        let selectedColumn = selectedColumns.[int columnIndex]

        let isMainColumn =
            CompositeHeader.Cases
            |> Array.exists (fun (_, header) -> selectedColumn.name.StartsWith(header))

        let headerRange = excelTable.getHeaderRowRange()
        let bodyRowRange = excelTable.getDataBodyRange()

        let mutable errors:list<exn*string> = []

        if isMainColumn then
            let! selectedBuildingBlock = getSelectedBuildingBlock excelTable context
            let targetIndex = fst (selectedBuildingBlock.Item (selectedBuildingBlock.Count - 1))
            let! result = validateSelectedColumns(headerRange, bodyRowRange, int columnIndex, targetIndex, context)

            errors <- result |> List.ofArray

        if columnIndex > 0 && errors.IsEmpty then
            let! selectedBuildingBlock = getAdaptedSelectedBuildingBlock excelTable -1. context
            let targetIndex = selectedBuildingBlock |> Array.ofSeq
            let! result = validateSelectedColumns(headerRange, bodyRowRange, fst targetIndex.[0], fst targetIndex.[targetIndex.Length - 1], context)

            result
            |> Array.iter (fun r ->
                errors <- r :: errors
            )

        if columnIndex < columns.count && errors.IsEmpty then
            let! selectedBuildingBlock = getAdaptedSelectedBuildingBlock excelTable 1. context
            let targetIndex = selectedBuildingBlock |> Array.ofSeq
            let! result = validateSelectedColumns(headerRange, bodyRowRange, fst targetIndex.[0], fst targetIndex.[targetIndex.Length - 1], context)

            result
            |> Array.iter (fun r ->
                errors <- r :: errors
            )

        return (Array.ofList errors)
    }

/// <summary>
/// Checks whether the annotation table is a valid arc table or not
/// </summary>
let validateSelectedAndNeighbouringBuildingBlocks () =
    Excel.run(fun context ->
        promise {
            let! excelTableRes = AnnotationTable.tryGetActive context

            match excelTableRes with
            | Some excelTable -> return [InteropLogging.Msg.create InteropLogging.Info $"The annotation table {excelTable.name} is valid"]
            | None ->
                //have to try to get the table without check in order to obtain an indexed error of the building block
                let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
                let activeWorksheet = context.workbook.worksheets.getActiveWorksheet()
                let tables = context.workbook.tables
                let _ =
                    activeWorksheet.load(propertyNames=U2.Case2 (ResizeArray[|"position"|])) |> ignore
                    tables.load(propertyNames=U2.Case2 (ResizeArray[|"items"; "worksheet"; "name"; "position"; "values"|]))

                let! table = context.sync().``then``(fun _ ->
                    let activeWorksheetPosition = activeWorksheet.position
                    /// Get name of the table of currently active worksheet.
                    let activeTable =
                        tables.items
                        |> Seq.toArray
                        |> Array.tryFind (fun table ->
                            table.name.StartsWith("annotationTable") && table.worksheet.position = activeWorksheetPosition
                        )

                    activeTable
                )

                if table.IsNone then return [InteropLogging.NoActiveTableMsg]
                else
                    let! indexedErrors = validateBuildingBlock(table.Value, context)

                    let messages =
                        indexedErrors
                        |> List.ofArray
                        |> List.collect (fun (ex, header ) ->
                            [InteropLogging.Msg.create InteropLogging.Warning $"The building block is not valid for a ARC table / ISA table: {ex.Message}";
                             InteropLogging.Msg.create InteropLogging.Warning $"The column {header} is not valid! It needs further inspection what causes the error"])                    

                    return (List.append messages [InteropLogging.NoActiveTableMsg])
        }
    )

/// <summary>
/// Get term information from database based on names
/// </summary>
/// <param name="names"></param>
let searchTermsInDatabase names =
    promise {
        let terms =
            names
            |> List.map (fun name ->
                TermQuery.create(name, searchMode=Database.FullTextSearch.Exact)
            )
            |> Array.ofSeq
        let! result = Async.StartAsPromise (Api.ontology.searchTerms terms)

        return
            result
            |> Array.map (fun item -> Array.tryHead item.results)
    }

/// <summary>
/// Convert selected building block
/// </summary>
/// <param name="excelTable"></param>
/// <param name="arcTable"></param>
/// <param name="propertyColumns"></param>
/// <param name="indexedTerms"></param>
let updateSelectedBuildingBlocks (excelTable: Table) (arcTable: ArcTable) (propertyColumns: array<int*string []>) (indexedTerms: list<int*Term option>) =
    promise {
        let headers = ARCtrl.Spreadsheet.ArcTable.helperColumnStrings |> Array.ofSeq

        for pi in 0..propertyColumns.Length-1 do
            let pIndex, pcv = propertyColumns.[pi]
            let values = Array.create (arcTable.RowCount + 1) ""
            indexedTerms
            |> List.iter (fun (mainIndex, potTerm) ->
                match potTerm with
                | Some term ->
                    match pcv.[0] with
                    | header when header = headers.[2] -> //Unit
                        values.[mainIndex] <- term.Name
                    | header when header.Contains(headers.[0]) -> //Term Source REF
                        values.[mainIndex] <- term.FK_Ontology
                    | header when header.Contains(headers.[1]) -> //Term Accession Number
                        values.[mainIndex] <- term.Accession
                    | _ -> ()
                | None -> values.[mainIndex] <- pcv.[mainIndex]

                let bodyValues =
                    values
                    |> Array.map (box >> Some)
                    |> Array.map (fun c -> ResizeArray[c])
                    |> ResizeArray
                excelTable.columns.items.[pIndex].values <- bodyValues
            )
        }
/// <summary>
/// Validates the arc table of the currently selected work sheet
/// When the validations returns an error, an error is returned to the user
/// When the arc table is valid one or more of the following processes happen:
/// * When the main column of term or unit is empty, then the Term Source REF and Term Accession Number are emptied
/// * When the main column of term or unit contains a value, the Term Source REF and Term Accession Number are filled
/// with the correct value
/// The later is not implemented yet
/// </summary>
let rectifyTermColumns (context0, getTerms0) =
    excelRunWith context0 <| fun context ->
        promise {
            let! excelTableRes = AnnotationTable.tryGetActive context
            //When there are messages, then there is an error and further processes can be skipped because the annotation table is not valid
            match excelTableRes with
            | None -> return [InteropLogging.NoActiveTableMsg]
            | Some excelTable ->
                //Arctable enables a fast check for term and unit building blocks
                let! arcTableRes = ArcTable.fromExcelTable(excelTable, context)
                match arcTableRes with
                | Result.Ok arcTable ->
                    let columns = arcTable.Columns
                    let _ = excelTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "rowCount"; "values";|]))
                    do! context.sync()
                    let items = excelTable.columns.items
                    let termAndUnitHeaders = columns |> Array.choose (fun item -> if item.Header.IsTermColumn then Some (item.Header.ToString()) else None)
                    let columns =
                        items
                        |> Array.ofSeq
                        |> Array.map (fun c ->
                            c.values
                            |> Array.ofSeq
                            |> Array.collect (fun cc ->
                                cc
                                |> Array.ofSeq
                                |> Array.choose (fun r -> if r.IsSome then Some (r.ToString()) else None)
                            )
                        )
                    let body = excelTable.getDataBodyRange()
                    let _ = body.load(propertyNames = U2.Case2 (ResizeArray[|"values"|]))

                    do! context.sync()
                    for i in 0..columns.Length-1 do
                        let column = columns.[i]
                        if Array.contains (column.[0].Trim()) termAndUnitHeaders then
                            let! potBuildingBlock = getCompositeColumnInfoByIndex excelTable i context
                            let buildingBlock = potBuildingBlock |> Array.ofSeq

                            // Check whether building block is unit or not
                            // When it is unit, then delete the property column values only when the unit is empty, independent of the main column
                            // When it is a term, then delete the property column values when the main column is empty
                            let mainColumn =
                                if snd buildingBlock.[1] = "Unit" then
                                    excelTable.columns.items.Item (fst buildingBlock.[1])
                                else excelTable.columns.items.Item (fst buildingBlock.[0])
                            let potPropertyColumns =
                                buildingBlock.[1..]
                                |> Array.map (fun (index, _) -> index, excelTable.columns.items.Item index)

                            let propertyColumns =
                                potPropertyColumns
                                |> Array.map (fun (index, c) ->
                                    index,
                                    c.values
                                    |> Array.ofSeq
                                    |> Array.collect (fun pc ->
                                        pc
                                        |> Array.ofSeq
                                        |> Array.choose (fun pcv -> if pcv.IsSome then Some (pcv.ToString()) else None)
                                    )
                                )

                            let mainColumnHasValues =
                                mainColumn.values
                                |> Array.ofSeq
                                |> Array.collect (fun c ->
                                    c
                                    |> Array.ofSeq
                                    |> Array.choose (fun cc -> if cc.IsSome then Some (cc.ToString()) else None)
                                    |> Array.map (fun cv -> cv, String.IsNullOrEmpty(cv))
                                )

                            let mutable names = []
                            let mutable indices = []

                            //Check whether value of property colum is fitting for value of main column and adapt if not
                            //Delete values of property columns when main column is empty
                            for pi in 0..propertyColumns.Length-1 do
                                let pIndex, pcv = propertyColumns.[pi]
                                let values = Array.create (arcTable.RowCount + 1) ""
                                for mainIndex in 0..mainColumnHasValues.Length-1 do
                                    let mc, isNull = mainColumnHasValues.[mainIndex]
                                    if not isNull then
                                        names <- mc::names
                                        indices <- mainIndex::indices
                                        values.[mainIndex] <- pcv.[mainIndex]
                                let bodyValues =
                                    values
                                    |> Array.map (box >> Some)
                                    |> Array.map (fun c -> ResizeArray[c])
                                    |> ResizeArray
                                excelTable.columns.items.[pIndex].values <- bodyValues

                            let getTerms =
                                match getTerms0 with
                                | Some getTerms -> getTerms
                                | None -> searchTermsInDatabase
                            let! terms = getTerms names

                            let indexedTerms =
                                indices
                                |> List.mapi (fun ii index ->
                                    index, terms.[ii])

                            do! updateSelectedBuildingBlocks excelTable arcTable propertyColumns indexedTerms
                    do! AnnotationTable.format(excelTable, context, true)

                    return [InteropLogging.Msg.create InteropLogging.Warning $"The annotation table {excelTable.name} is valid"]

                | Result.Error ex -> return [InteropLogging.Msg.create InteropLogging.Error ex.Message]
        }

/// <summary>
/// Try to get the values of top level meta data from excel worksheet
/// </summary>
/// <param name="identifier"></param>
/// <param name="parseToMetadata"></param>
let parseToTopLevelMetadata<'T> identifier (parseToMetadata: string option seq seq -> 'T) (context: RequestContext) =
    promise {
        let workSheet = context.workbook.worksheets.getItem(identifier)
        let range = workSheet.getUsedRange true
        let _ =
            workSheet.load(propertyNames = U2.Case2 (ResizeArray[|"name"|])) |> ignore
            range.load(propertyNames = U2.Case2 (ResizeArray["values"; "rowCount"]))

        do! context.sync()

        if range.rowCount > 1 then

            let values =
                range.values
                |> Seq.map (fun row ->
                    row
                    |> Seq.map (fun column ->
                        //Have to check for empty strings becaue the parsing to arc file type causes problems with empty strings
                        if column.IsSome && not (String.IsNullOrEmpty(column.ToString())) then
                            Some (column.Value.ToString())
                        else None)
                )

            return Some (parseToMetadata values)
        else return None
    }

/// <summary>
/// Get all annotation tables of the excel file
/// </summary>
/// <param name="context"></param>
let getExcelAnnotationTables (context: RequestContext) =
    promise {
        let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))

        do! context.sync()

        let tables = context.workbook.tables
        let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items"; "name"|]))

        do! context.sync()

        let! annotationTables =
            context.sync().``then``(fun _ ->
                AnnotationTable.getAnnotationTables tables
            )

        let inMemoryTables = new ResizeArray<ArcTable>()
        let mutable msgs = List.Empty

        for i in 0 .. annotationTables.Length-1 do
            let! tableRes = AnnotationTable.validate annotationTables.[i] context
            match tableRes with
            | Result.Ok table ->
                let! resultRes = ArcTable.fromExcelTable(table, context)
                match resultRes with
                | Result.Ok result -> inMemoryTables.Add(result)
                | Result.Error message ->
                    let msg = InteropLogging.Msg.create InteropLogging.Error message.Message
                    msgs <- msg::msgs
            | Result.Error messages ->
                messages
                |> List.iter (fun message ->
                    msgs <- message::msgs)

        match msgs.Length with
        | 0 -> return Result.Ok inMemoryTables
        | _ -> return Result.Error msgs
    }

open FsSpreadsheet

/// <summary>
/// Delete excel worksheet that contains top level metadata
/// </summary>
/// <param name="identifier"></param>
let deleteTopLevelMetadata () =
    Excel.run(fun context ->
        promise {
            let worksheets = context.workbook.worksheets

            let _ = worksheets.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"; "values"|]))

            do! context.sync()

            worksheets.items
            |> Seq.iter (fun worksheet ->
                if isTopLevelMetadataSheet worksheet.name then
                    worksheet.delete()
            )

            return [InteropLogging.Msg.create InteropLogging.Warning $"The top level metadata work sheet has been deleted"]
        }
    )

/// <summary>
/// Deletes the data contained in the selected worksheet and fills it afterwards with the given new data
/// </summary>
/// <param name="context"></param>
/// <param name="fsWorkSheet"></param>
/// <param name="seqOfSeqs"></param>
let private updateWorkSheet (context: RequestContext) (worksheetName: string) (seqOfSeqs: seq<seq<string option>>) =
    promise {
        let rowCount = seqOfSeqs |> Seq.length
        let columnCount = seqOfSeqs |> Seq.map Seq.length |> Seq.max

        let worksheet0 = context.workbook.worksheets.getItemOrNullObject(worksheetName)

        let _ = worksheet0.load(U2.Case2 (ResizeArray[|"isNullObject"|]))
        do! context.sync()

        let worksheet =
            if worksheet0.isNullObject then
                context.workbook.worksheets.add(worksheetName)
            else
                worksheet0

        do! context.sync()

        let range = worksheet.getUsedRange true        
        let _ = range.load(propertyNames = U2.Case2 (ResizeArray["values"]))
        do! context.sync()

        range.values <- null

        do! context.sync()

        let range = worksheet.getRangeByIndexes(0, 0, rowCount, columnCount)
        let _ = range.load(propertyNames = U2.Case2 (ResizeArray["values"]))

        do! context.sync()

        let values = ExcelUtil.convertSeqToExcelRangeInput(seqOfSeqs)

        range.values <- values

        range.format.autofitColumns()
        range.format.autofitRows()

        do! context.sync()

        return worksheet
    }



/// <summary>
/// Updates top level metadata excel worksheet of assays
/// </summary>
/// <param name="assay"></param>
let updateTopLevelMetadata (arcFiles: ArcFiles) =
    Excel.run(fun context ->
        promise {
            let worksheetName, seqOfSeqs = arcFiles.MetadataToExcelStringValues()

            let! updatedWorksheet = updateWorkSheet context worksheetName seqOfSeqs

            updatedWorksheet.activate()

            return [InteropLogging.Msg.create InteropLogging.Info $"The worksheet {worksheetName} has been updated"]
        }
    )


    

/// <summary>
/// Add new rows to the end of the given table
/// </summary>
/// <param name="table"></param>
/// <param name="tableRowCount"></param>
/// <param name="tableColumnCount"></param>
/// <param name="targetRowCount"></param>
/// <param name="context"></param>
let expandTableRowCount (table: Table) (tableRowCount: int) (tableColumnCount: int) (targetRowCount: int) (context: RequestContext) =
    promise {
        let diff = targetRowCount - tableRowCount

        ExcelUtil.Table.addRows -1. table tableColumnCount diff "" |> ignore

        do! context.sync()
    }    

/// <summary>
/// Insert the ontology information in the selected range independent of an annotation table
/// </summary>
/// <param name="selectedRange"></param>
/// <param name="ontology"></param>
/// <param name="context"></param>
let insertOntology (selectedRange: Excel.Range) (ontology: OntologyAnnotation) (context: RequestContext) =
    promise {
        let _ = selectedRange.load(U2.Case2 (ResizeArray[|"columnCount"; "rowCount"; "values"|]))

        do! context.sync()

        let selectedRowCount =   int selectedRange.rowCount
        let selectedColumnCount = int selectedRange.columnCount

        let selectedValues = selectedRange.values

        for rowIndex in 0..selectedRowCount-1 do
            for columnIndex in 0..selectedColumnCount-1 do
                match columnIndex%3 with
                | 0 -> selectedValues.[rowIndex].[columnIndex] <- (Option.map box ontology.Name)
                | 1 -> selectedValues.[rowIndex].[columnIndex] <- (Option.map box ontology.TermSourceREF)
                | 2 -> selectedValues.[rowIndex].[columnIndex] <- (ontology.TermAccessionOntobeeUrl |> Option.whereNot String.IsNullOrWhiteSpace |> Option.map box)
                | _ -> ()

        selectedRange.values <- selectedValues

        selectedRange.format.autofitColumns()
        selectedRange.format.autofitRows()

        do! context.sync()
    }

/// <summary>
/// Checks whether the first selected cell is within the range of an annotation table or not
/// </summary>
/// <param name="tableRange"></param>
/// <param name="selectedRange"></param>
/// <param name="context"></param>
let private isSelectedOutsideAnnotationTable (tableRange: Excel.Range) (selectedRange: Excel.Range) (context: RequestContext) =
    promise {
        let _ =
            tableRange.load(U2.Case2 (ResizeArray[|"columnCount"; "rowCount";|])) |> ignore
            selectedRange.load(U2.Case2 (ResizeArray[|"columnIndex"; "rowIndex";|]))

        do! context.sync()

        let tableMaxRow = tableRange.rowCount
        let tableMaxColumn = tableRange.columnCount

        let selectedRow = selectedRange.rowIndex + 1.
        let selectedColumn = selectedRange.columnIndex + 1.

        if (selectedRow > tableMaxRow) || (selectedColumn > tableMaxColumn) then
            return true
        else return false
    }

/// <summary>
/// Fill the selected building blocks, or single columns, with the selected term
/// </summary>
/// <param name="ontologyAnnotation"></param>
let fillSelectedWithOntologyAnnotation (ontologyAnnotation: OntologyAnnotation) =
    Excel.run(fun context ->
        promise {
            let! result = AnnotationTable.tryGetActive context

            match result with
            | Some excelTable ->
                
                let selectedRange = context.workbook.getSelectedRange().load(U2.Case2 (ResizeArray[|"rowCount"; "rowIndex"|]))

                let mutable tableRange = excelTable.getRange()
                let _ = tableRange.load(U2.Case2 (ResizeArray[|"columnCount"; "rowCount"; "values"|]))

                do! context.sync()

                let! isAnnotationTableSelected = isSelectedOutsideAnnotationTable tableRange selectedRange context

                if isAnnotationTableSelected then

                    do! insertOntology selectedRange ontologyAnnotation context

                    return [InteropLogging.Msg.create InteropLogging.Info "Filled the columns with the selected term"]
                else

                    let firstRow = selectedRange.rowIndex
                    let selectedRowCount = selectedRange.rowCount

                    if firstRow + selectedRowCount > tableRange.rowCount then
                        let targetRowCount = int (firstRow + selectedRowCount)
                        do! expandTableRowCount excelTable (int tableRange.rowCount) (int tableRange.columnCount) targetRowCount context
                        let newTableRange = excelTable.getRange()
                        tableRange <- newTableRange

                    let _ = tableRange.load(U2.Case2 (ResizeArray[|"columnCount"; "rowCount"; "values"|]))

                    do! context.sync()

                    let tableValues =
                        tableRange.values
                        |> Array.ofSeq
                        |> Array.map (fun row ->
                            row
                            |> Array.ofSeq
                            |> Array.map (fun column ->
                                column
                                |> Option.map string
                                |> Option.defaultValue ""
                                |> (fun s -> s.TrimEnd())
                            )
                        )

                    let tableHeaders = tableValues.[0]

                    if firstRow = 0 && selectedRowCount = 1 then return [InteropLogging.Msg.create InteropLogging.Error "You cannot fill the headers of an annotation table with terms!"]
                    else
                        let selectedRowCount =
                            if firstRow = 0. then selectedRowCount - 1.
                            else selectedRowCount
                        let firstRow =
                            if firstRow = 0. then 1.
                            else firstRow

                        let! selectedBuildingBlock = getSelectedBuildingBlock excelTable context

                        let columnIndices = selectedBuildingBlock |> Array.ofSeq |> Array.map (fun (index, _) -> index)
                        let columnHeaders = ARCtrl.Spreadsheet.ArcTable.helperColumnStrings |> Array.ofSeq

                        let firstIndex = Array.head columnIndices
                        let lastIndex = Array.last columnIndices

                        let isUnit = Array.contains columnHeaders.[2] tableHeaders.[firstIndex..lastIndex] //Unit

                        for rowIndex in firstRow..(firstRow + selectedRowCount-1.) do
                            columnIndices
                            |> Array.iter (fun columnIndex ->
                                match tableHeaders.[columnIndex] with
                                | header when header = columnHeaders.[2] -> //Unit
                                    tableValues.[int rowIndex].[columnIndex] <- (if ontologyAnnotation.Name.IsSome then ontologyAnnotation.Name.Value else tableValues.[int rowIndex].[columnIndex])
                                | header when header.Contains(columnHeaders.[0]) -> //Term Source REF
                                    tableValues.[int rowIndex].[columnIndex] <- (if ontologyAnnotation.TermSourceREF.IsSome then ontologyAnnotation.TermSourceREF.Value else tableValues.[int rowIndex].[columnIndex])
                                | header when header.Contains(columnHeaders.[1]) -> //Term Accession Number
                                    tableValues.[int rowIndex].[columnIndex] <- (if ontologyAnnotation.TermAccessionNumber.IsSome then ontologyAnnotation.TermAccessionAndOntobeeUrlIfShort else tableValues.[int rowIndex].[columnIndex])
                                | _ -> //Only main column left
                                    if not isUnit then
                                        tableValues.[int rowIndex].[columnIndex] <- (if ontologyAnnotation.Name.IsSome then ontologyAnnotation.Name.Value else tableValues.[int rowIndex].[columnIndex])
                            )

                        let bodyValues =
                            tableValues
                            |> Array.map (fun row ->
                                row
                                |> Array.map (fun item -> box item |> Some)
                                |> ResizeArray
                            )
                            |> ResizeArray

                        tableRange.values <- bodyValues

                        do! context.sync()

                        do! AnnotationTable.format(excelTable, context, true)

                        return [InteropLogging.Msg.create InteropLogging.Info "Filled the columns with the selected term"]

            | _ ->
                let selectedRange = context.workbook.getSelectedRange().load(U2.Case2 (ResizeArray[|"rowCount"; "rowIndex"|]))

                do! context.sync()

                do! insertOntology selectedRange ontologyAnnotation context
                return [InteropLogging.Msg.create InteropLogging.Info "Filled the columns with the selected term"]
        }
    )


/// <summary>This function is used to insert file names into the selected range.</summary>
let insertFileNamesFromFilePicker (fileNameList: string list) =
    Excel.run(fun context ->

        // Ref. 2
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray(["values"; "columnIndex"; "columnCount"])))

        // Ref. 1
        let r = context.runtime.load(U2.Case1 "enableEvents")

        // sync with proxy objects after loading values from excel
        context.sync().``then``( fun _ ->

            if range.columnCount > 1. then failwith "Cannot insert Terms in more than one column at a time."

            r.enableEvents <- false

            // create new values for selected Range.
            let newVals = ResizeArray([
                // iterate over the rows of the selected range (there can only be one column as we fail if more are selected)
                for rowInd in 0 .. range.values.Count - 1 do
                    let tmp =
                        // Iterate over col values (1).
                        range.values.[rowInd] |> Seq.map (
                            // Ignore prevValue as it will be replaced anyways.
                            fun _ ->
                                // This part is a design choice.
                                // Should the user select less cells than we have items in the 'fileNameList' then we only fill the selected cells.
                                // Should the user select more cells than we have items in the 'fileNameList' then we fill the leftover cells with none.
                                let fileName = if fileNameList.Length - 1 < rowInd then None else List.item rowInd fileNameList |> box |> Some
                                fileName
                        )
                    ResizeArray(tmp)
            ])

            range.values <- newVals
            range.format.autofitColumns()
            r.enableEvents <- true
            //sprintf "%s filled with %s; ExtraCols: %s" range.address v nextColsRange.address

            // return print msg
            "Info",sprintf "%A, %A" range.values.Count newVals
        )
    )

let getTableMetaData () =
    Excel.run (fun context ->

        promise {
            let! excelTable = AnnotationTable.tryGetActive context
            if excelTable.IsNone then failwith "Error. No active table found!"
            let excelTable = excelTable.Value
            let rowRange = excelTable.getRange()
            let headerRange = excelTable.getHeaderRowRange()
            let _ =
                headerRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore
                rowRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore
                excelTable.columns.load(propertyNames = U2.Case1 "count") |> ignore
                excelTable.rows.load(propertyNames = U2.Case1 "count")    |> ignore

            let! res = context.sync().``then``(fun _ ->
                let colCount,rowCount = excelTable.columns.count, excelTable.rows.count
                let rowRangeAddr, rowRangeColCount, rowRangeRowCount = rowRange.address,rowRange.columnCount,rowRange.rowCount
                let headerRangeAddr, headerRangeColCount, headerRangeRowCount = headerRange.address,headerRange.columnCount,headerRange.rowCount

                "info",
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

            return res
        }
    )

type Main =

    /// <summary>
    /// Reads all excel information and returns ArcFiles object, with metadata and tables.
    /// </summary>
    static member tryParseToArcFile (?getTables, ?context0) =
        let getTables = defaultArg getTables true
        excelRunWith context0 <| fun context ->
            promise {
                let _ = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"worksheets"|]))
                do! context.sync()
                let worksheets = context.workbook.worksheets
                let _ = worksheets.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"|]))
                do! context.sync()
                let worksheetTopLevelMetadata =
                    worksheets.items
                    |> Seq.tryFind (fun item -> isTopLevelMetadataSheet item.name)

                match worksheetTopLevelMetadata with
                | Some worksheet when ArcAssay.isMetadataSheetName worksheet.name ->
                    let! assay = parseToTopLevelMetadata worksheet.name ArcAssay.fromMetadataCollection context
                    match assay with
                    | Some assay when getTables->
                        let! tableCollection = getExcelAnnotationTables context
                        match tableCollection with
                        | Result.Ok tables ->
                            assay.Tables <- (tables)
                            let result = ArcFiles.Assay assay
                            return Result.Ok result
                        | Result.Error msgs -> return Result.Error msgs
                    | Some assay ->
                        return ArcFiles.Assay assay |> Result.Ok
                    | None -> return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"No top level metadata sheet is available!"]
                | Some worksheet when ArcInvestigation.isMetadataSheetName worksheet.name ->
                    let! investigation = parseToTopLevelMetadata worksheet.name ArcInvestigation.fromMetadataCollection context
                    match investigation with
                    | Some investigation ->
                        let result = ArcFiles.Investigation investigation
                        return Result.Ok result
                    | None -> return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"No top level metadata sheet is available!"]
                | Some worksheet when ArcStudy.isMetadataSheetName worksheet.name ->
                    let! studyCollection = parseToTopLevelMetadata worksheet.name ArcStudy.fromMetadataCollection context
                    match studyCollection with
                    | Some (study, assays) when getTables ->
                        let! tableCollection = getExcelAnnotationTables context
                        match tableCollection with
                        | Result.Ok tables ->
                            study.Tables <- tables
                            let result = ArcFiles.Study (study, assays)
                            return Result.Ok result
                        | Result.Error msgs -> return Result.Error msgs
                    | Some (study, assays) ->
                        return ArcFiles.Study (study, assays) |> Result.Ok
                    | None -> return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"No top level metadata sheet is available!"]
                | Some worksheet when Template.isMetadataSheetName worksheet.name ->
                    let! result = parseToTopLevelMetadata worksheet.name Template.fromMetadataCollection context
                    match result with
                    | Some (templateInfo, ers, tags, authors) when getTables ->
                        let! tableCollection = getExcelAnnotationTables context
                        let mkTemplate table = Template.fromParts templateInfo ers tags authors table DateTime.Now |> ArcFiles.Template
                        match tableCollection with
                        | Result.Ok tables when tables.Count = 1 ->
                            let table = tables.[0]
                            let template = mkTemplate table
                            return Result.Ok template
                        | Result.Ok tables when tables.Count > 1 -> //when not exactly 1 table is found throw
                            return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"Only one annotation table is allowed for an arc template but this one has {tables.Count} tables!"]
                        | Result.Error _ | Result.Ok _ ->
                            let table = ArcTable.init("New Template Table")
                            let template = mkTemplate table
                            return Result.Ok template
                    | Some (templateInfo, ers, tags, authors) ->
                        let template = Template.fromParts templateInfo ers tags authors (ArcTable.init("New Template Table")) DateTime.Now |> ArcFiles.Template
                        return Result.Ok template
                    | None ->
                        return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"No top level metadata sheet is available!"]
                    
                | _ -> return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"No top level metadata sheet is available that determines the type of data!"]
            }

    /// <summary>
    /// Create a new annotation table based on an arcTable 
    /// </summary>
    /// <param name="arcTable"></param>
    /// <param name="context0"></param>
    static member createNewAnnotationTable(arcTable: ArcTable, ?context0) =
        excelRunWith context0 <| fun context ->
            promise {
                let worksheetName = arcTable.Name
                //delete existing worksheet with the same name
                let worksheet0 = context.workbook.worksheets.getItemOrNullObject(worksheetName)
                worksheet0.delete()
                // create new worksheet
                let worksheet = context.workbook.worksheets.add(worksheetName)
                do! context.sync()
                let tableValues = arcTable.ToStringSeqs()
                let rowCount = tableValues.Count
                let columnCount = tableValues |> Seq.map Seq.length |> Seq.max
                let tableRange = worksheet.getRangeByIndexes(0, 0, rowCount, columnCount)
                let table = worksheet.tables.add(U2.Case1 tableRange, true)
                tableRange.values <- tableValues
                table.name <- createNewTableName()
                table.style <- TableStyleLight
                // Only activate the worksheet if this function is specifically called
                if context0.IsNone then worksheet.activate()
                do! context.sync()
                return worksheet, table
            }

    /// <summary>
    /// This function deletes all existing arc tables in the excel file and metadata sheets, and writes a new ArcFile to excel
    /// </summary>
    /// <param name="arcFiles"></param>
    static member updateArcFile (arcFiles: ArcFiles, ?context0) =
        excelRunWith context0 <| fun context ->
            promise {
                let worksheetName, seqOfSeqs = arcFiles.MetadataToExcelStringValues()
                let! updatedWorksheet = updateWorkSheet context worksheetName seqOfSeqs
                let tables = arcFiles.Tables()
                for table in tables do
                    do! Main.createNewAnnotationTable(table, context).``then``(fun _ -> ())
                updatedWorksheet.activate()
                return [InteropLogging.Msg.create InteropLogging.Info $"Replaced existing Swate information! Added {tables.TableCount} tables!"]
            }

    /// <summary>
    /// Handle any diverging functionality here. This function is also used to make sure any new building blocks comply to the swate annotation-table definition
    /// </summary>
    /// <param name="newColumn"></param>
    static member addCompositeColumn (newColumn: CompositeColumn,(*newHeader: CompositeHeader, ?newValues: U2<CompositeCell, CompositeCell[]>,*) ?table: Excel.Table, ?context0: RequestContext) =
        excelRunWith context0 <| fun context ->
            promise {
                let! excelTable =
                    match table with
                    | Some table -> promise {return Some table}
                    | None -> AnnotationTable.tryGetActive context

                if excelTable.IsNone then failwith "Error. No active table found!"

                let excelTable = excelTable.Value

                let! arcTableRes = ArcTable.fromExcelTable(excelTable, context)

                match arcTableRes with
                | Ok arcTable ->
                    let values =
                        if newColumn.Cells.Length < arcTable.RowCount && newColumn.Cells.Length > 0 then
                            Array.create arcTable.RowCount newColumn.Cells.[0]
                        else
                            newColumn.Cells
                    
                    arcTable.AddColumn(newColumn.Header, values, forceReplace=true, skipFillMissing=false)

                    let! _ = Main.createNewAnnotationTable(arcTable)

                    let! newTable = AnnotationTable.tryGetActive(context)

                    if newTable.IsSome then
                        do! AnnotationTable.format(newTable.Value, context, true)

                    return []

                    //let selectedRange = context.workbook.getSelectedRange().getColumn(0)
                    //let headerRange = excelTable.getHeaderRowRange()

                    //let _ =
                    //    excelTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"; "values"; "index"; "count"|])) |> ignore
                    //    excelTable.rows.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"; "values"; "index"; "count"|])) |> ignore
                    //    selectedRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount"; "address"; "isEntireColumn"; "worksheet"; "columnCount"]))) |> ignore
                    //    headerRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount"; "address"; "isEntireColumn"; "worksheet"; "columnCount"; "values"])))

                    //do! context.sync()


                    ////Work in progress
                    //let excelTableRowCount = excelTable.rows.count

                    //let columns =
                    //    let values =
                    //        match newValues with
                    //        | Some (U2.Case1 cell) -> [|cell|]
                    //        | Some (U2.Case2 cells) -> cells
                    //        | None -> [||]
                            
                    //    //Adapt rowCount of composite column that shall be added
                    //    let column = CompositeColumn.create(newHeader, values)                        

                    //    [|
                    //        for column in Spreadsheet.CompositeColumn.toStringCellColumns column do
                    //            let c = ResizeArray column

                    //            if excelTableRowCount > c.Count then
                    //                let diff = int excelTableRowCount - c.Count
                    //                let ex = Array.init diff (fun _ ->
                    //                    match newValues with
                    //                    | Some (U2.Case1 cell) -> c.[0]
                    //                    | _ -> ""
                    //                )
                    //                c.AddRange ex
                    //            c |> Array.ofSeq
                    //    |]

                    //let! selectedIndex = tryGetSelectedTableIndex excelTable context |> _.``then``(fun i ->
                    //    match i with | Some i -> i | None -> int ExcelUtil.AppendIndex)

                    ////When single column replace existing column with same name or add at index                    
                    //if newHeader.IsUnique then

                    //    let existingColumnRes = arcTable.TryGetColumnByHeader newHeader

                    //    match existingColumnRes with
                    //    | Some existingColumn ->
                    //        let toUpdateColumn =
                    //            excelTable.columns.items
                    //            |> Seq.find (fun item ->
                    //                item.name.Contains(existingColumn.Header.ToString()))
                    //        excelTable.columns.items.[(int toUpdateColumn.index)].name <- columns.[0].[0]
                    //    | None ->
                    //        let isInputOrOutputAndExistingRes =
                    //            match newHeader with
                    //            | header when header.isOutput -> arcTable.TryGetOutputColumn()
                    //            | header when header.isInput ->  arcTable.TryGetInputColumn()
                    //            | _ -> None

                    //        match isInputOrOutputAndExistingRes with
                    //        | Some column ->
                    //            let startString = if column.Header.isInput then "Input" else "Output"
                    //            let toUpdateColumn =
                    //                excelTable.columns.items
                    //                |> Seq.find (fun item ->
                    //                    item.name.StartsWith(startString))
                    //            excelTable.columns.items.[(int toUpdateColumn.index)].name <- columns.[0].[0]
                    //        | None -> ExcelUtil.Table.addColumnAt selectedIndex excelTable columns.[0] |> ignore                            
                    //else
                    //    for i in 0 .. (columns.Length-1) do
                    //        let column = columns.[i]
                    //        let index = selectedIndex + i                        
                    //        ExcelUtil.Table.addColumnAt index excelTable column |> ignore

                    //do! context.sync()

                    //do! AnnotationTable.format(excelTable, context, true)

                    //return []
                        
                | Result.Error exn ->
                    return [InteropLogging.Msg.create InteropLogging.Error exn.Message]
            } 