module OfficeInterop.Core

open System.Collections.Generic

open Fable.Core
open ExcelJS.Fable
open Excel
open GlobalBindings

open Shared
open OfficeInteropTypes
open TermTypes

open OfficeInterop
open OfficeInterop.HelperFunctions
open BuildingBlockFunctions

open ARCtrl
open ARCtrl.Spreadsheet

module OfficeInteropExtensions =

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

    /// <summary>
    /// Get the building block associated with the given column index
    /// </summary>
    /// <param name="table"></param>
    /// <param name="columnIndex"></param>
    /// <param name="context"></param>
    let getBuildingBlockByIndex (table: Table) (columnIndex: float) (context: RequestContext) =
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

    open ARCtrl.Spreadsheet.ArcTable

    type ExcelHelper =

        /// <summary>
        /// Get the excel table of the given context and name
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tableName"></param>
        static member tryGetTableByName (context: RequestContext) (tableName: string) =
            let _ = context.workbook.load(U2.Case1 "tables")
            let excelTable = context.workbook.tables.getItem(tableName)

            if tableName = null || tableName = "" then
                None
            else
                let annoHeaderRange = excelTable.getHeaderRowRange()
                let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
                let annoBodyRange = excelTable.getDataBodyRange()
                let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"; "numberFormat"|])) |> ignore
                Some (excelTable, annoHeaderRange, annoBodyRange)

        /// <summary>
        /// Swaps 'Rows with column values' to 'Columns with row values'
        /// </summary>
        /// <param name="rows"></param>
        static member viewRowsByColumns (rows: ResizeArray<ResizeArray<'a>>) =
            rows
            |> Seq.collect (fun row -> Seq.indexed row)
            |> Seq.groupBy fst
            |> Seq.map (snd >> Seq.map snd >> Seq.toArray)
            |> Seq.toArray

        /// <summary>
        /// Add a new column at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="excelTable"></param>
        /// <param name="name"></param>
        /// <param name="rowCount"></param>
        /// <param name="value"></param>
        static member addColumn (index: float) (excelTable: Table) name rowCount value =
            let col = createMatrixForTables 1 rowCount value

            excelTable.columns.add(
                index   = index,
                values  = U4.Case1 col,
                name    = name
            )

        /// <summary>
        /// Delete an existing column at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="excelTable"></param>
        /// <param name="name"></param>
        /// <param name="rowCount"></param>
        /// <param name="value"></param>
        static member deleteColumn (index: float) (excelTable: Table) =
            let col = excelTable.columns.getItemAt index
            col.delete()

        /// <summary>
        /// Add only the row values of the column you are adding
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="excelTable"></param>
        /// <param name="columnName"></param>
        /// <param name="rows"></param>
        static member addColumnAndRows (columnIndex: float) (excelTable: Table) columnName rows =
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
        static member addRows (index: float) (excelTable: Table) rowCount value =
            let col = createMatrixForTables 1 rowCount value
            excelTable.rows.add(
                index   = index,
                values  = U4.Case1 col
            )

        /// <summary>
        /// Adopt the tables, columns, and values
        /// </summary>
        /// <param name="table"></param>
        /// <param name="context"></param>
        /// <param name="shallHide"></param>
        static member adoptTableFormats (table: Table, context: RequestContext, shallHide: bool) =
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
        /// Converts a sequence of sequence of excel data into a resizearray, compatible with Excel.Range
        /// </summary>
        /// <param name="metadataValues"></param>
        static member convertToResizeArrays (metadataValues:seq<seq<string option>>) =

            //Selects the longest sequence of the metadata values
            //In the next step, determines the length of the longest metadata value sequence
            let maxLength = metadataValues |> Seq.maxBy Seq.length |> Seq.length

            //Adapts the length of the smaller sequences to the length of the longest sequence in order to avoid problems with the insertion into excel.Range
            let ra = ResizeArray()
            let parseToObj (input: string) : obj =
                box input
            for seq in metadataValues do
                //Parse string to obj option
                let ira = ResizeArray (seq |> Seq.map (fun column -> column |> Option.map parseToObj))
                if ira.Count < maxLength then
                    ira.AddRange (Array.create (maxLength - ira.Count) None)
                ra.Add ira
            ra

    type ArcTable with

        /// <summary>
        /// Creates ArcTable based on table name and collections of strings, representing columns and rows
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
                |> Some

            arcTable

        /// <summary>
        /// Transforms ArcTable to excel compatible "values", row major
        /// </summary>
        member this.ToExcelValues() =

            let table = this
            // Cancel if there are no columns
            if table.Columns.Length = 0 then
                ResizeArray()
            else
                let columns =
                    table.Columns
                    |> List.ofArray
                    |> List.sortBy classifyColumnOrder
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
        static member tryGetFromExcelTable (excelTable:Table, context:RequestContext) =
            promise {
                    //Get headers and body
                    let headerRange = excelTable.getHeaderRowRange()
                    let bodyRowRange = excelTable.getDataBodyRange()

                    let _ =
                        excelTable.load(U2.Case2 (ResizeArray [|"name"|])) |> ignore
                        bodyRowRange.load(U2.Case2 (ResizeArray [|"numberFormat"; "values";|])) |> ignore
                        headerRange.load(U2.Case2 (ResizeArray [|"columnCount"; "columnIndex"; "rowIndex"; "values"; |]))

                    let! inMemoryTable = context.sync().``then``(fun _ ->
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
                        ArcTable.fromStringSeqs(excelTable.name, headers, bodyRows)
                    )
                    return inMemoryTable
            }

        /// <summary>
        /// Checks whether the string seqs are part of a valid arc table or not and returns potential errors and index of them in the annotation table
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="rows"></param>
        static member validateColumns(headers:#seq<string>, rows:#seq<#seq<string>>) =

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
        static member validateExcelTable (excelTable:Table, context:RequestContext) =

            promise {
                    //Get headers and body
                    let headerRange = excelTable.getHeaderRowRange()
                    let bodyRowRange = excelTable.getDataBodyRange()

                    let _ =
                        headerRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
                        bodyRowRange.load(U2.Case2 (ResizeArray [|"values"; "numberFormat"|])) |> ignore

                    do! context.sync().``then``(fun _ -> ())

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

                        ArcTable.validateColumns(headers, bodyRows)

                    return inMemoryTable
            }

        /// <summary>
        /// Try to get a arc table from excel file based on excel table name
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="context"></param>
        static member tryGetFromExcelTableName (tableName:string, context:RequestContext) =

            let result = ExcelHelper.tryGetTableByName context tableName

            promise {
                if result.IsSome then
                    let table, _, _ = result.Value
                    let! inMemoryTable = ArcTable.tryGetFromExcelTable(table, context)
                    return inMemoryTable
                else return None
            }

        static member isTopLevelMetadataName (tableName:string) =
            match tableName with
            | name when ArcAssay.isMetadataSheetName name -> true
            | name when ArcInvestigation.isMetadataSheetName name -> true
            | name when ArcStudy.isMetadataSheetName name -> true
            | Template.metaDataSheetName
            | Template.obsoletemetaDataSheetName -> true
            | _ -> false

open OfficeInteropExtensions

// Reoccuring Comment Defitinitions

// 'annotationTables'      -> For a workbook (NOT! worksheet) all tables must have unique names. Therefore not all our tables can be called 'annotationTable'.
//                             Instead we add human readable ids to keep them unique. 'annotationTables' references all of those tables.

// 'active annotationTable' -> The annotationTable present on the active worksheet.

// 'TSR'/'TAN'             -> Term Source Ref - column / Term Accession Number - column

// 'Reference Columns'     -> The hidden columns including TSR, TAN and Unit columns

// 'Main Column'           -> Non hidden column of a building block. Each building block only contains one main column

// 'Id Tag'                -> Column headers in Excel must be unique. Therefore, Swate adds #integer to headers.

// 'Unit column'           -> This references the unit column of a building block. It is a optional addition and not every building block must contain it.

// 'Term column'            -> The name "TermColumn" refers to all columns with the syntax "Parameter/Factor/etc [TERM-NAME]"

// 'Featured column'        -> A featured column can be abstracted as a "term column" and is a pre-implemented usecase.
//                              Such a block will contain TSR and TAN and can be used for directed Term search.

open System

open Fable.Core.JsInterop

[<Literal>]
let AppendIndex = -1.

/// <summary>This is not used in production and only here for development. Its content is always changing to test functions for new features.</summary>
let exampleExcelFunction1 () =
    Excel.run(fun context ->
        promise {
            return "Hello World!"
        }
    )

/// <summary>This is not used in production and only here for development. Its content is always changing to test functions for new features.</summary>
let exampleExcelFunction2 () =
    Excel.run(fun context ->
        promise {

            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

            let tables = context.workbook.tables.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
            let _ = tables.load(propertyNames = U2.Case2 (ResizeArray [|"name";"worksheet"|]))

            let! allTables = context.sync().``then``( fun _ ->

                /// Get all names of all tables in the whole workbook.
                let tableNames =
                    tables.items
                    |> Seq.toArray
                    |> Array.map (fun x -> x.name, x.worksheet.name)

                tableNames
            )

            //let tableValidations = getAllSwateTableValidation xmlParsed

            return (sprintf "%A"  allTables)
        }
    )

let swateSync (context:RequestContext) =
    context.sync().``then``(fun _ -> ())

// I retrieve the index of the currently opened worksheet, here the new table should be created.
// I retrieve all annotationTables in the workbook. I filter out all annotationTables that are on a worksheet with a lower index than the index of the currently opened worksheet.
// I subtract from the index of the current worksheet the indices of the other found worksheets with annotationTable.
// I sort by the resulting lowest number (since the worksheet is then closest to the active one), I find the output column in the particular
// annotationTable and use the values it contains for the new annotationTable in the active worksheet.
/// <summary>
/// Will return Some tableName if any annotationTable exists in a worksheet before the active one
/// </summary>
/// <param name="context"></param>
let tryGetPrevAnnotationTableName (context:RequestContext) =
    promise {

        let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
        let activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load(U2.Case1 "position")
        let tables = context.workbook.tables
        let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items";"worksheet";"name"; "position"; "values"|]))

        let! prevTable = context.sync().``then``(fun _ ->
            let activeWorksheetPosition = activeWorksheet.position
            /// Get all names of all tables in the whole workbook.
            let prevTable =
                tables.items
                |> Seq.toArray
                |> Array.choose (fun x ->
                    if x.name.StartsWith("annotationTable") then
                        Some (x.worksheet.position ,x.name)
                    else
                        None
                )
                |> Array.filter(fun (wp, _) -> activeWorksheetPosition - wp > 0.)
                |> Array.sortBy(fun (wp, _) ->
                    activeWorksheetPosition - wp
                )
                |> Array.tryHead
            Option.bind (snd >> Some) prevTable
        )

        return prevTable
    }

/// <summary>
/// Checks whether the active excel table is valid or not
/// </summary>
let validateAnnotationTable excelTable context =
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
/// Try select the annotation table from the current active work sheet
/// </summary>
/// <param name="context"></param>
let tryGetActiveAnnotationTable (context: RequestContext) =
    promise {

        let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
        let activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load(U2.Case1 "position")
        let tables = context.workbook.tables
        let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items"; "worksheet"; "name"; "position"; "style"; "values"|]))

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

        match table with
        | None -> return Result.Error [InteropLogging.Msg.create InteropLogging.Error "The active worksheet must contain one annotation table to perform this action."]
        | Some table ->
            let! result = validateAnnotationTable table context
            return result
    }

/// <summary>
/// Will return Some tableName if any annotationTable exists in a worksheet before the active one
/// </summary>
/// <param name="context"></param>
/// <param name="name"></param>
let tryGetAnnotationTableByName (context:RequestContext) (name:string)=
    promise {

        let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
        let tables = context.workbook.tables
        let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items"; "worksheet"; "name"; "position"; "values"|]))

        let! activeTable = context.sync().``then``(fun _ ->
            tables.items
            |> Seq.toArray
            |> Array.tryFind (fun table ->
                table.name = name
            )
        )

        return activeTable
    }

/// <summary>
/// Get the previous arc table to the active worksheet
/// </summary>
/// <param name="context"></param>
let tryGetPrevTable (context:RequestContext) =
    promise {
        let! prevTableName = tryGetPrevAnnotationTableName context

        if prevTableName.IsSome then

            let! result = ArcTable.tryGetFromExcelTableName (prevTableName.Value, context)
            return result

        else

            return None
    }

/// <summary>
/// Get output column of arc excel table
/// </summary>
/// <param name="context"></param>
let tryGetPrevTableOutput (context:RequestContext) =
    promise {

        let! inMemoryTable = tryGetPrevTable context

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

/// <summary>
/// This function is used to create a new annotation table. 'isDark' refers to the current styling of excel (darkmode, or not).
/// </summary>
/// <param name="isDark"></param>
/// <param name="tryUseLastOutput"></param>
/// <param name="range"></param>
/// <param name="context"></param>
let private createAnnotationTableAtRange (isDark:bool, tryUseLastOutput:bool, range:Excel.Range, context: RequestContext) =

    // This function is used to create the "next" annotationTable name.
    // 'allTableNames' is passed from a previous function and contains a list of all annotationTables.
    let rec findNewTableName allTableNames =
        let id = HumanReadableIds.tableName()
        let newTestName = $"annotationTable{id}"
        let existsAlready = allTableNames |> Array.exists (fun tableName -> tableName = newTestName)
        if existsAlready then
            findNewTableName allTableNames
        else
            newTestName

    /// decide table style by input parameter
    let style =
        if isDark then
            "TableStyleMedium15"
        else
            "TableStyleMedium7"

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
            let annoTables =
                activeTables.items
                |> Seq.toArray
                |> Array.map (fun x -> x.name)
                |> Array.filter (fun x -> x.StartsWith "annotationTable")

            match annoTables.Length with
            //Create a new annotation table in the active worksheet
            | 0 -> ()
            //Create a mew worksheet with a new annotation table when the active worksheet already contains one
            | x when x = 1 ->
                //Create new worksheet and set it active
                context.workbook.worksheets.add() |> ignore
                let lastWorkSheet = context.workbook.worksheets.getLast()
                lastWorkSheet.activate()
                activeSheet <- lastWorkSheet
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

        let! allTableNames = getAllTableNames context

        let _ = activeSheet.load(propertyNames = U2.Case2 (ResizeArray[|"name"|])) |> ignore

        let newTableRange =
            if hasCreatedNewWorkSheet then
                activeSheet.getCell(tableRange.rowIndex, tableRange.columnIndex)
                    .load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount"; "address"; "isEntireColumn"; "worksheet"])))
            else tableRange

        // sync with proxy objects after loading values from excel
        let! table = context.sync().``then``( fun _ ->

            // Ref. 1
            r.enableEvents <- false

            // Create table in current worksheet

            // Create new annotationTable name
            let newName = findNewTableName allTableNames

            let inMemoryTable = ArcTable.init(newName)

            let newCells = Array.init (int tableRange.rowCount - 1) (fun _ -> CompositeCell.emptyFreeText)

            inMemoryTable.AddColumn(CompositeHeader.Input IOType.Source, newCells)

            let tableStrings = inMemoryTable.ToExcelValues()

            let excelTable = activeSheet.tables.add(U2.Case1 newTableRange, true)

            // Update annotationTable name
            excelTable.name <- newName

            newTableRange.values <- tableStrings

            // Update annotationTable style
            excelTable.style <- style
            excelTable
        )

        let _ = table.rows.load(propertyNames = U2.Case2 (ResizeArray[|"count"|]))

        let! table, logging = context.sync().``then``(fun _ ->

            //logic to compare size of previous table and current table and adapt size of inMemory table
            if prevTableOutput.IsSome then
                //Skip header because it is newly generated for inMemory table
                let newColValues =
                    prevTableOutput.Value.[1..]
                    |> Array.map (fun cell ->
                        [|cell|]
                        |> Array.map (box >> Some)
                        |> ResizeArray
                    ) |> ResizeArray

                let rowCount0 = int table.rows.count
                let diff = rowCount0 - newColValues.Count

                if diff > 0 then // table larger than values -> Delete rows to reduce excel table size to previous table size
                    table.rows?deleteRowsAt(newColValues.Count, diff)
                elif diff < 0 then // more values than table -> Add rows to increase excel table size to previous table size
                    let absolute = (-1) * diff
                    let nextvalues = createMatrixForTables 1 absolute ""
                    table.rows.add(-1, U4.Case1 nextvalues) |> ignore

                let body = (table.columns.getItemAt 0.).getDataBodyRange()
                body.values <- newColValues

            // Fit widths and heights of cols and rows to value size. (In this case the new column headers).
            activeSheet.getUsedRange().format.autofitColumns()
            activeSheet.getUsedRange().format.autofitRows()

            r.enableEvents <- true

            // Return info message
            let logging = InteropLogging.Msg.create InteropLogging.Info (sprintf "Annotation Table created in [%s] with dimensions 2c x (%.0f + 1h)r." newTableRange.address (newTableRange.rowCount - 1.))

            table, logging
        )

        return (table, logging)
    }

/// <summary>
/// This function is used to create a new annotation table. 'isDark' refers to the current styling of excel (darkmode, or not)
/// </summary>
/// <param name="isDark"></param>
/// <param name="tryUseLastOutput"></param>
let createAnnotationTable (isDark:bool, tryUseLastOutput:bool) =
    Excel.run (fun context ->
        let selectedRange = context.workbook.getSelectedRange()
        promise {
            let! newTableLogging = createAnnotationTableAtRange (isDark, tryUseLastOutput, selectedRange, context)

            // Interop logging expects list of logs
            return [snd newTableLogging]
        }
    )

/// <summary>
/// This function is used before most excel interop messages to get the active annotationTable
/// </summary>
let tryFindActiveAnnotationTable() =
    Excel.run(fun context ->

        // Ref. 2
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let t = sheet.load(U2.Case2 (ResizeArray[|"tables"|]))
        let tableItems = t.tables.load(propertyNames=U2.Case1 "items")

        context.sync()
            .``then``( fun _ ->
                // access names of all tables in the active worksheet.
                let tables =
                    tableItems.items
                    |> Seq.toArray
                    |> Array.map (fun x -> x.name)
                // filter all table names for tables starting with "annotationTable"
                let annoTables =
                    tables
                    |> Array.filter (fun x -> x.StartsWith "annotationTable")
                // Get the correct error message if we have <> 1 annotation table. Only returns success and the table name if annoTables.Length = 1
                let res = TryFindAnnoTableResult.exactlyOneAnnotationTable annoTables

                // return result
                res
        )
    )

/// <summary>
/// This function is used to hide all reference columns and to fit rows and columns to their values.
/// The main goal is to improve readability of the table with this function.
/// </summary>
/// <param name="hideRefCols"></param>
/// <param name="context"></param>
let autoFitTable (hideRefCols:bool) (context:RequestContext) =
    promise {
        let! excelTable = getActiveAnnotationTableName context

        // Ref. 2
        let sheet = context.workbook.worksheets.getActiveWorksheet()

        let excelTable = sheet.tables.getItem(excelTable)
        let allCols = excelTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"|]))

        let annoHeaderRange = excelTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))

        let r = context.runtime.load(U2.Case1 "enableEvents")

        let! res = context.sync().``then``(fun _ ->

            // Ref. 1
            r.enableEvents <- false
            // Auto fit on all columns to fit cols and rows to their values.
            let updateColumns =
                allCols.items
                |> Array.ofSeq
                |> Array.map (fun col ->
                    let r = col.getRange()
                    if (SwateColumnHeader.create col.name).isReference && hideRefCols then
                        r.columnHidden <- true
                    else
                        r.format.autofitColumns()
                        r.format.autofitRows()
                )

            r.enableEvents <- true

            // return message
            [InteropLogging.Msg.create InteropLogging.Info "Autoformat Table"]
        )
        return res
    }

/// <summary>
/// This function is used to hide all reference columns and to fit rows and columns to their values.
/// The main goal is to improve readability of the table with this function.
/// </summary>
/// <param name="context"></param>
let autoFitTableHide (context:RequestContext) =
    autoFitTable true context

/// <summary>
/// Autofit on all tables, columns, and their values for ExcelApi 1.2
/// </summary>
/// <param name="excelTable"></param>
/// <param name="context"></param>
let autoFitTableByTable (excelTable:Table) (context:RequestContext) =

    let allCols = excelTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"|]))

    let annoHeaderRange = excelTable.getHeaderRowRange()
    let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))

    context.sync().``then``(fun _ ->

        // Auto fit on all columns to fit cols and rows to their values.
        let updateColumns =
            allCols.items
            |> Array.ofSeq
            |> Array.map (fun col ->
                let r = col.getRange()
                if (SwateColumnHeader.create col.name).isReference then
                    r.columnHidden <- true
                else
                    r.format.autofitColumns()
                    r.format.autofitRows()
            )

        // return message
        [InteropLogging.Msg.create InteropLogging.Info "Autoformat Table"]
    )

let getBuildingBlocksAndSheet() =
    Excel.run(fun context ->
        promise {
            let! excelTable = getActiveAnnotationTableName(context)

            // Ref. 2
            let! buildingBlocks = BuildingBlockFunctions.getBuildingBlocks context excelTable

            let worksheet = context.workbook.worksheets.getActiveWorksheet()
            let _ = worksheet.load(U2.Case1 "name")

            let! name = context.sync().``then``(fun _ -> worksheet.name)
            return (name, buildingBlocks)
        }
    )

let getBuildingBlocksAndSheets() =
    Excel.run(fun context ->
        promise {

            let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
            let tables = context.workbook.tables
            let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items"; "worksheet"; "name"; "values"|]))

            let! worksheetAnnotationTableNames = context.sync().``then``(fun _ ->
                /// Get all names of all tables in the whole workbook.
                let worksheetTableNames =
                    tables.items
                    |> Seq.toArray
                    |> Array.choose (fun x ->
                        if x.name.StartsWith("annotationTable") then
                            Some (x.worksheet.name ,x.name)
                        else
                            None
                    )
                worksheetTableNames
            )

            // Ref. 2
            let! worksheetBuildingBlocks =
                worksheetAnnotationTableNames
                |> Array.map (fun (worksheetName,tableName) ->
                    // This function will not work without explicit calling Excel.run.
                    // My guess is, loading multiple values parallel on the same context will overwrite or cancel each other.
                    // By creating multiple instances of context this problem is circumvented.
                    // ONLY use multiple context instances when reading
                    Excel.run (fun context ->
                        let buildingBlocks = BuildingBlockFunctions.getBuildingBlocks context tableName
                        buildingBlocks |> Promise.map (fun res -> worksheetName, res)
                    )
                )
                |> Promise.all

            return worksheetBuildingBlocks
        }
    )

// ExcelApi 1.1
/// <summary>Selected ranged returns indices always from a worksheet perspective but we need the related table index. This is calculated here.</summary>
let private rebaseIndexToTable (selectedRange:Excel.Range) (annoHeaderRange:Excel.Range) =
    let diff = selectedRange.columnIndex - annoHeaderRange.columnIndex |> int
    let columnCount = annoHeaderRange.columnCount |> int
    let maxLength = columnCount-1
    if diff < 0 then
        maxLength
    elif diff > maxLength then
        maxLength
    else
        diff
    |> float

/// <summary>Check column type and term if combination already exists</summary>
let private checkIfBuildingBlockExisting (newBB:InsertBuildingBlock) (existingBuildingBlocks:BuildingBlock []) =
    let mainColumnPrints =
        existingBuildingBlocks
        |> Array.choose (fun x ->
            // reference columns are now allowed in duplicates (0.6.4)
            if x.MainColumn.Header.isMainColumn && not x.MainColumn.Header.isTermColumn then
                x.MainColumn.Header.toBuildingBlockNamePrePrint
            else
                None
        )
    if mainColumnPrints |> Array.contains newBB.ColumnHeader then failwith $"Swate table contains already building block \"{newBB.ColumnHeader.toAnnotationTableHeader()}\" in worksheet."


/// <summary>Check column type and term if combination already exists</summary>
// Issue #203: Don't Error, instead change output column
let private checkHasExistingOutput (newBB:InsertBuildingBlock) (existingBuildingBlocks:BuildingBlock []) =
    if newBB.ColumnHeader.isOutputColumn then
        let existingOutputOpt =
            existingBuildingBlocks
            |> Array.tryFind (fun x -> x.MainColumn.Header.isMainColumn && x.MainColumn.Header.isOutputCol)
        existingOutputOpt
    else
        None
        //if existingOutputOpt.IsSome then failwith $"Swate table contains already one output column \"{existingOutputOpt.Value.MainColumn.Header.SwateColumnHeader}\". Each Swate table can only contain exactly one output column type."

let private checkHasExistingInput (newBB:InsertBuildingBlock) (existingBuildingBlocks:BuildingBlock []) =
    if newBB.ColumnHeader.isInputColumn then
        let existingInputOpt =
            existingBuildingBlocks
            |> Array.tryFind (fun x -> x.MainColumn.Header.isMainColumn && x.MainColumn.Header.isInputCol)
        if existingInputOpt.IsSome then
            failwith $"Swate table contains already input building block \"{newBB.ColumnHeader.toAnnotationTableHeader()}\" in worksheet."


// ExcelApi 1.4
/// <summary>This function is used to add a new building block to the active annotationTable.</summary>
let addAnnotationBlock (newBB:InsertBuildingBlock) =
    Excel.run(fun context ->
        promise {

            let! excelTableName = getActiveAnnotationTableName context
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let excelTable = sheet.tables.getItem(excelTableName)

            // Ref. 2
            // This is necessary to place new columns next to selected col
            let annoHeaderRange = excelTable.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"; "columnIndex"; "columnCount"; "rowIndex"|]))
            let tableRange = excelTable.getRange()
            let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount"; "rowCount"])))
            let selectedRange = context.workbook.getSelectedRange()
            let _ = selectedRange.load(U2.Case1 "columnIndex")

            let! nextIndex, headerVals = context.sync().``then``(fun _ ->
                // This is necessary to place new columns next to selected col.
                let rebasedIndex = rebaseIndexToTable selectedRange annoHeaderRange

                // This is necessary to skip over hidden cols
                // Get an array of the headers
                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq

                // Here is the next col index, which is not hidden, calculated.
                let nextIndex = findIndexNextNotHiddenCol headerVals rebasedIndex
                nextIndex, headerVals
            )

            let rowCount = tableRange.rowCount |> int

            //create an empty column to insert
            let col value = createMatrixForTables 1 rowCount value

            let! mainColName, formatChangedMsg = context.sync().``then``( fun _ ->

                let allColHeaders =
                    headerVals
                    |> Array.choose id
                    |> Array.map string

                let columnNames = Indexing.createColumnNames newBB allColHeaders

                /// This logic will only work if there is only one format change
                let mutable formatChangedMsg : InteropLogging.Msg list = []

                let createAllCols =
                    let createCol index =
                        excelTable.columns.add(
                            index   = index,
                            values  = U4.Case1 (col "")
                        )
                    columnNames
                    |> Array.mapi (fun i colName ->
                        // create a single column
                        let col = createCol (nextIndex + float i)
                        // add column header name
                        col.name <- colName
                        let columnBody = col.getDataBodyRange()
                        // Fit column width to content
                        columnBody.format.autofitColumns()
                        // Update mainColumn body rows with number format IF building block has unit.
                        // Trim column name to
                        if newBB.UnitTerm.IsSome && colName = columnNames.[0] then
                            // create numberFormat for unit columns
                            let format = newBB.UnitTerm.Value.toNumberFormat
                            let formats = createValueMatrix 1 (rowCount-1) format
                            formatChangedMsg <- (InteropLogging.Msg.create InteropLogging.Info $"Added specified unit: {format}")::formatChangedMsg
                            columnBody.numberFormat <- formats
                        else
                            let format = createValueMatrix 1 (rowCount-1) "@"
                            columnBody.numberFormat <- format
                        // hide freshly created column if it is a reference column
                        if colName <> columnNames.[0] then
                            columnBody.columnHidden <- true
                        col
                    )


                // 'columnNames.[0]' should only be used for logging, so maybe trim whitespace?
                columnNames.[0], formatChangedMsg
            )

            let! fit = autoFitTableByTable excelTable context

            let createColsMsg = InteropLogging.Msg.create InteropLogging.Info $"{mainColName} was added."

            let loggingList = [
                if not formatChangedMsg.IsEmpty then yield! formatChangedMsg
                createColsMsg
            ]

            return loggingList
        }
    )

// https://github.com/nfdi4plants/Swate/issues/203
/// If an output column already exists it should be replaced by the new output column type.
let replaceOutputColumn (excelTableName: string) (existingOutputColumn: BuildingBlock) (newOutputcolumn: InsertBuildingBlock) =
    Excel.run(fun context ->
        promise {
            // Ref. 2
            // This is necessary to place new columns next to selected col
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let excelTable = sheet.tables.getItem(excelTableName)
            let annoHeaderRange = excelTable.getHeaderRowRange()
            let existingOutputColCell = annoHeaderRange.getCell(0., float existingOutputColumn.MainColumn.Index)
            let _ = existingOutputColCell.load(U2.Case2 (ResizeArray[|"values"|]))

            let newHeaderValues = ResizeArray[|ResizeArray [|newOutputcolumn.ColumnHeader.toAnnotationTableHeader() |> box |> Some|]|]
            do! context.sync().``then``(fun _ ->
                existingOutputColCell.values <- newHeaderValues
                ()
            )

            let! _ = autoFitTableByTable excelTable context

            let warningMsg = $"Found existing output column \"{existingOutputColumn.MainColumn.Header.SwateColumnHeader}\". Changed output column to \"{newOutputcolumn.ColumnHeader.toAnnotationTableHeader()}\"."

            let msg = InteropLogging.Msg.create InteropLogging.Warning warningMsg

            let loggingList = [ msg ]

            return loggingList
        }
    )

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

        let newColumn = ExcelHelper.addColumn AppendIndex excelTable (newBB.Header.ToString()) rowCount ""
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

        let newColumn = ExcelHelper.addColumn AppendIndex excelTable (newBB.Header.ToString()) rowCount ""
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
let addBuildingBlock (excelTable: Table) (arcTable: ArcTable) (newBB: CompositeColumn) (headerRange: Excel.Range) (selectedRange: Excel.Range) =

    let rowCount = arcTable.RowCount + 1

    let buildingBlockCells = Spreadsheet.CompositeColumn.toStringCellColumns newBB

    let targetIndex =
        let excelIndex = rebaseIndexToTable selectedRange headerRange

        let headers = arcTable.ToExcelValues().[0] |> Array.ofSeq |> Array.map (fun header -> header.ToString())

        if excelIndex < (float) (headers.Length - 1) then

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

    let headers =
        headerRange.values[0]
        |> List.ofSeq
        |> List.map (fun header -> header.ToString())

    buildingBlockCells
    |> List.iteri(fun i bbCell ->
        //check and extend header to avoid duplicates
        let newHeader = Indexing.extendName (headers |> List.toArray) bbCell.Head
        let calIndex =
            if targetIndex >= 0 then targetIndex + (float) i
            else AppendIndex
        let value =
            if bbCell.Tail.IsEmpty then ""
            else bbCell.Tail.Head
        let column = ExcelHelper.addColumn calIndex excelTable newHeader rowCount value
        newHeader::headers |> ignore
        column.getRange().format.autofitColumns()

        if ARCtrl.Spreadsheet.ArcTable.helperColumnStrings |> List.exists (fun cName -> newHeader.StartsWith cName) then
            column.getRange().columnHidden <- true
    )

    let msg = InteropLogging.Msg.create InteropLogging.Info $"Added new term column: {newBB.Header}"

    let loggingList = [ msg ]

    loggingList

/// <summary>
/// Prepare the given table to be joined with the currently active annotation table
/// </summary>
/// <param name="tableToAdd"></param>
let prepareTemplateInMemory (table:Table) (tableToAdd: ArcTable) (context: RequestContext) =
    promise {
        let! originTable = ArcTable.tryGetFromExcelTable(table, context)

        if originTable.IsNone then failwith $"Failed to create arc table for table {table.name}"

        let finalTable = Table.selectiveTablePrepare originTable.Value tableToAdd

        let selectedRange = context.workbook.getSelectedRange()

        let tableStartIndex = table.getRange()

        let _ =
            tableStartIndex.load(propertyNames=U2.Case2 (ResizeArray[|"columnIndex"|])) |> ignore
            selectedRange.load(propertyNames=U2.Case2 (ResizeArray[|"columnIndex"|]))

        // sync with proxy objects after loading values from excel
        do! context.sync().``then``( fun _ -> ())

        let targetIndex =
            let adaptedStartIndex = selectedRange.columnIndex - tableStartIndex.columnIndex
            if adaptedStartIndex > float (originTable.Value.ColumnCount) then originTable.Value.ColumnCount
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
            let! result = tryGetActiveAnnotationTable context

            match result with
            | Result.Ok excelTable ->
                let! (tableToAdd: ArcTable, index: int option) = prepareTemplateInMemory excelTable tableToAdd context

                //Arctable enables a fast check for the existence of input- and output-columns and their indices
                let! arcTable = ArcTable.tryGetFromExcelTable(excelTable, context)

                //When both tables could be accessed succesfully then check what kind of column shall be added an whether it is already there or not
                if arcTable.IsSome then

                    arcTable.Value.Join(tableToAdd, ?index=index, ?joinOptions=options, forceReplace=true)

                    let tableValues = arcTable.Value.ToExcelValues() |> Array.ofSeq
                    let (headers, _) = Array.ofSeq(tableValues.[0]), tableValues.[1..]

                    let newTableRange = excelTable.getRange()

                    let _ = newTableRange.load(propertyNames = U2.Case2 (ResizeArray["rowCount";]))

                    do! context.sync().``then``(fun _ ->
                        excelTable.delete()
                    )

                    let! (newTable, _) = createAnnotationTableAtRange(false, false, newTableRange, context)

                    let _ = newTable.load(propertyNames = U2.Case2 (ResizeArray["name"; "values"; "columns";]))

                    do! context.sync().``then``(fun _ ->

                        newTable.name <- excelTable.name

                        let headerNames =
                            let names = headers |> Array.map (fun item -> item.Value.ToString())
                            names
                            |> Array.map (fun name -> Indexing.extendName names name)

                        headerNames
                        |> Array.iteri(fun i header ->
                            ExcelHelper.addColumn i newTable header (int newTableRange.rowCount) "" |> ignore)
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

                    do! context.sync().``then``(fun _ -> ())

                    do! ExcelHelper.adoptTableFormats(newTable, context, true)

                    return [InteropLogging.Msg.create InteropLogging.Warning $"Joined template {tableToAdd.Name} to table {excelTable.name}!"]
                else
                    return [InteropLogging.Msg.create InteropLogging.Error "No arc table could be created! This should not happen at this stage! Please report this as a bug to the developers.!"]
            | Result.Error msgs -> return msgs
        }
    )

/// <summary>
/// Handle any diverging functionality here. This function is also used to make sure any new building blocks comply to the swate annotation-table definition
/// </summary>
/// <param name="newBB"></param>
let addAnnotationBlockHandler (newBB: CompositeColumn) =
    Excel.run(fun context ->
        promise {

            let! result = tryGetActiveAnnotationTable context

            match result with
            | Result.Ok excelTable ->
                //Arctable enables a fast check for the existence of input- and output-columns and their indices
                let! arcTable = ArcTable.tryGetFromExcelTable(excelTable, context)

                //When both tables could be accessed succesfully then check what kind of column shall be added an whether it is already there or not
                if arcTable.IsSome then

                    let selectedRange = context.workbook.getSelectedRange().getColumn(0)
                    let headerRange = excelTable.getHeaderRowRange()

                    let _ =
                        excelTable.rows.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"; "values"; "index"; "count"|])) |> ignore
                        selectedRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount"; "address"; "isEntireColumn"; "worksheet"; "columnCount"]))) |> ignore
                        headerRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount"; "address"; "isEntireColumn"; "worksheet"; "columnCount"; "values"]))) |> ignore
                        excelTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"; "values"; "index"; "count"|]))

                    let (|Input|_|) (newBuildingBlock:CompositeColumn) =
                        if newBuildingBlock.Header.isInput then
                            if arcTable.Value.TryGetInputColumn().IsSome then
                                Some (updateInputColumn excelTable arcTable.Value newBuildingBlock)
                            else Some (addInputColumn excelTable arcTable.Value newBuildingBlock)
                        else None

                    let (|Output|_|) (newBuildingBlock:CompositeColumn) =
                        if newBuildingBlock.Header.isOutput then
                            if arcTable.Value.TryGetOutputColumn().IsSome then
                                Some (updateOutputColumn excelTable arcTable.Value newBuildingBlock)
                            else Some (addOutputColumn excelTable arcTable.Value newBuildingBlock)
                        else None

                    let addBuildingBlock (newBuildingBlock:CompositeColumn) =
                        addBuildingBlock excelTable arcTable.Value newBuildingBlock headerRange selectedRange

                    let getResult (newBuildingBlock:CompositeColumn) =
                        match newBuildingBlock with
                        | Input msg -> msg
                        | Output msg -> msg
                        | _ -> addBuildingBlock newBuildingBlock

                    let! result = context.sync().``then``(fun _ ->
                        getResult newBB
                    )

                    do! ExcelHelper.adoptTableFormats(excelTable, context, true)

                    return result
                else
                    return [InteropLogging.Msg.create InteropLogging.Warning $"A table is missing! annotationTable: {excelTable.name}; arcTable: {arcTable.IsSome}"]
            | Result.Error msgs -> return msgs
        } 
    )

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

            let! result = tryGetActiveAnnotationTable context

            match result with
            | Result.Ok excelTable ->
                let! selectedBuildingBlock = getSelectedBuildingBlock excelTable context

                // iterate DESCENDING to avoid index shift
                for i, _ in Seq.sortByDescending fst selectedBuildingBlock do
                    let column = excelTable.columns.getItemAt(i)
                    log $"delete column {i}"
                    column.delete()

                do! context.sync().``then``(fun _ -> ())

                do! ExcelHelper.adoptTableFormats(excelTable, context, true)

                return [InteropLogging.Msg.create InteropLogging.Info $"The building block associated with column {snd (selectedBuildingBlock.Item 0)} has been deleted"]
            | Result.Error msgs -> return msgs
        }
    )

/// <summary>
/// Get the main column of the arc table of the selected building block of the active annotation table
/// </summary>
let getArcMainColumn (excelTable: Table) (arcTable: ArcTable) (context: RequestContext)=
    promise {
        let! selectedBlock = getSelectedBuildingBlock excelTable context

        let protoHeaders = excelTable.getHeaderRowRange()
        let _ = protoHeaders.load(U2.Case2 (ResizeArray(["values"])))

        do! context.sync().``then``(fun _ -> ())

        let headers = protoHeaders.values.Item 0 |> Array.ofSeq |> Array.map (fun c -> c.ToString())

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
/// Get the main column of the arc table of the selected building block of the active annotation table
/// </summary>
let tryGetArcMainColumnFromFrontEnd () =
    Excel.run(fun context ->
        promise {
            let! result = tryGetActiveAnnotationTable context

            match result with
            | Result.Ok table ->
                let! arcTable = ArcTable.tryGetFromExcelTable(table, context)
                let! column = getArcMainColumn table arcTable.Value context
                return Some column
            | Result.Error _ -> return None
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
/// Add a building block at a specified position to the given table
/// </summary>
/// <param name="excelIndex"></param>
/// <param name="newBB"></param>
/// <param name="table"></param>
/// <param name="context"></param>
let addBuildingBlockAt (excelIndex: int) (newBB: CompositeColumn) (table: Table) (context: RequestContext) =
    promise {
        let headers = table.getHeaderRowRange()
        let _ = headers.load(U2.Case2 (ResizeArray [|"values"|]))

        do! context.sync().``then``(fun _ -> ())

        let stringHeaders = headers.values |> Array.ofSeq |> Array.collect (fun i -> Array.ofSeq i |> Array.map (fun ii -> ii.Value.ToString()))

        let buildingBlockCells = Spreadsheet.CompositeColumn.toStringCellColumns newBB

        let headers =
            buildingBlockCells
            |> List.map (fun bbc -> bbc.[0])
            |> List.map (fun s -> Indexing.extendName stringHeaders s)

        //Create a matrix for tabes that contains the right value for each cell
        let bodyValues =
            buildingBlockCells
            |> Array.ofSeq
            |> Array.map (fun bc ->
                bc
                |> Array.ofSeq
                |> Array.map (fun br -> [|U3<bool, string, float>.Case2 br|] :> IList<U3<bool, string, float>>))

        headers
        |> List.iteri (fun ci header -> ExcelHelper.addColumnAndRows (float (excelIndex + ci)) table header bodyValues.[ci] |> ignore)
    }

let getSelectedCellType () =
    Excel.run(fun context ->
        promise {
            let! result = tryGetActiveAnnotationTable context

            match result with
            | Result.Ok excelTable ->
                let! _, rowIndex = getSelectedBuildingBlockCell excelTable context
                let! arcTable = ArcTable.tryGetFromExcelTable(excelTable, context)
                let! (arcMainColumn, _) = getArcMainColumn excelTable arcTable.Value context

                if rowIndex > 0 then

                    let result =
                        match arcMainColumn with
                        | amc when amc.Cells.[(int rowIndex) - 1].isUnitized -> Some CompositeCellDiscriminate.Unitized
                        | amc when amc.Cells.[(int rowIndex) - 1].isTerm -> Some CompositeCellDiscriminate.Term
                        | amc when amc.Cells.[(int rowIndex) - 1].isData -> Some CompositeCellDiscriminate.Data
                        | amc when amc.Header.isInput && amc.Header.IsDataColumn && amc.Cells.[(int rowIndex) - 1].isFreeText -> Some CompositeCellDiscriminate.Text
                        | amc when amc.Header.isOutput && amc.Header.IsDataColumn && amc.Cells.[(int rowIndex) - 1].isFreeText -> Some CompositeCellDiscriminate.Text
                        | _ -> None

                    return result
                else return None

            | Result.Error _ -> return None
        }
    )

/// <summary>
/// This function is used to convert building blocks that can be converted. Data building blocks can be converted into free text, free text into data,
/// terms into units and units into terms
/// </summary>
let convertBuildingBlock () =
    Excel.run(fun context ->
        promise {
            let! result = tryGetActiveAnnotationTable context

            match result with
            | Result.Ok excelTable ->
                let! selectedBuildingBlock, rowIndex = getSelectedBuildingBlockCell excelTable context
                let! arcTable = ArcTable.tryGetFromExcelTable(excelTable, context)

                let! (arcMainColumn, arcIndex) = getArcMainColumn excelTable arcTable.Value context

                let msgText =
                    if rowIndex = 0 then "Headers cannot be converted"
                    else ""

                match arcMainColumn with
                | amc when amc.Cells.[(int rowIndex) - 1].isUnitized ->
                    convertToTermCell amc ((int rowIndex) - 1)
                    arcTable.Value.UpdateColumn(arcIndex, arcMainColumn.Header, arcMainColumn.Cells)
                | amc when amc.Cells.[(int rowIndex) - 1].isTerm ->
                    convertToUnitCell amc ((int rowIndex) - 1)
                    arcTable.Value.UpdateColumn(arcIndex, arcMainColumn.Header, arcMainColumn.Cells)
                | amc when amc.Cells.[(int rowIndex) - 1].isData -> convertToFreeTextCell arcTable.Value arcIndex amc ((int rowIndex) - 1)
                | amc when amc.Cells.[(int rowIndex) - 1].isFreeText -> convertToDataCell arcTable.Value arcIndex amc ((int rowIndex) - 1)
                | _ -> ()

                let name = excelTable.name
                let style = excelTable.style

                let newTableRange = excelTable.getRange()

                let newExcelTableValues = arcTable.Value.ToExcelValues()
                let _ = newTableRange.load(propertyNames = U2.Case2 (ResizeArray["values"; "columnCount"; "rowCount"]))

                do! context.sync().``then``(fun _ ->
                    let difference = (newExcelTableValues.Item 0).Count - (int newTableRange.columnCount)

                    match difference with
                    | diff when diff > 0 ->
                        for i = 0 to diff - 1 do
                            ExcelHelper.addColumn (newTableRange.columnCount + (float i)) excelTable (i.ToString()) (int newTableRange.rowCount) "" |> ignore
                    | diff when diff < 0 ->
                        for i = 0 downto diff + 1 do
                            ExcelHelper.deleteColumn 0 excelTable
                    | _ -> ()
                )

                let newTableRange = excelTable.getRange()
                let _ = newTableRange.load(propertyNames = U2.Case2 (ResizeArray["values";  "columnCount"]))

                do! context.sync().``then``(fun _ ->
                    excelTable.delete()
                    newTableRange.values <- newExcelTableValues
                )

                let _ = newTableRange.load(propertyNames = U2.Case2 (ResizeArray["values"; "worksheet"]))

                do! context.sync().``then``(fun _ -> ())

                let activeSheet = newTableRange.worksheet
                let _ = activeSheet.load(U2.Case2 (ResizeArray[|"tables"|]))

                do! context.sync().``then``(fun _ ->
                    let newTable = activeSheet.tables.add(U2.Case1 newTableRange, true)
                    newTable.name <- name
                    newTable.style <- style
                )

                let! newTable = tryGetActiveAnnotationTable context

                match newTable with
                | Result.Ok table -> do! ExcelHelper.adoptTableFormats(table, context, true)
                | _ -> ()

                let msg =
                    if String.IsNullOrEmpty(msgText) then $"Converted building block of {snd selectedBuildingBlock.[0]} to unit"
                    else msgText

                return [InteropLogging.Msg.create InteropLogging.Warning msg]
            | Result.Error msgs -> return msgs
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

            ArcTable.validateColumns(headerRow, bodyRows)
        )
        return inMemoryTable
    }

/// <summary>
/// Validate the selected building block and those next to it
/// </summary>
/// <param name="excelTable"></param>
/// <param name="context"></param>
let validateBuildingBlock (excelTable: Table, context: RequestContext) =

    let columns = excelTable.columns
    let selectedRange = context.workbook.getSelectedRange()

    let _ =
        columns.load(propertyNames = U2.Case2 (ResizeArray[|"count"; "items"; "name"|])) |> ignore
        selectedRange.load(U2.Case2 (ResizeArray [|"values"; "columnIndex"|]))

    promise {
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
            let! result = tryGetActiveAnnotationTable context
            match result with
            | Result.Ok excelTable -> return [InteropLogging.Msg.create InteropLogging.Warning $"The annotation table {excelTable.name} is valid"]
            | Result.Error msgs ->
                //have to try to get the table without check in order to obtain an indexed error of the building block
                let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
                let activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load(U2.Case1 "position")
                let tables = context.workbook.tables
                let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items"; "worksheet"; "name"; "position"; "values"|]))

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

                if table.IsNone then return msgs
                else
                    let! indexedErrors = validateBuildingBlock(table.Value, context)

                    let messages =
                        indexedErrors
                        |> List.ofArray
                        |> List.collect (fun (ex, header ) ->
                            [InteropLogging.Msg.create InteropLogging.Warning $"The building block is not valid for a ARC table / ISA table: {ex.Message}";
                             InteropLogging.Msg.create InteropLogging.Warning $"The column {header} is not valid! It needs further inspection what causes the error"])                    

                    return (List.append messages msgs)
        }
    )

/// <summary>
/// Validates the arc table of the currently selected work sheet
/// When the validations returns an error, an error is returned to the user
/// When the arc table is valid one or more of the following processes happen:
/// * When the main column of term or unit is empty, then the Term Source REF and Term Accession Number are emptied
/// * When the main column of term or unit contains a value, the Term Source REF and Term Accession Number are filled
/// with the correct value
/// The later is not implemented yet
/// </summary>
let rectifyTermColumns () =
    Excel.run(fun context ->
        promise {
            let! result = tryGetActiveAnnotationTable context

            //When there are messages, then there is an error and further processes can be skipped because the annotation table is not valid
            match result with
            | Result.Error messages -> return messages
            | Result.Ok excelTable ->
                //Arctable enables a fast check for term and unit building blocks
                let! arcTable = ArcTable.tryGetFromExcelTable(excelTable, context)

                let arcTable = arcTable.Value
                let columns = arcTable.Columns
                let _ = excelTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "values"; "rowCount"|]))

                do! context.sync().``then``(fun _ -> ())
                let items = excelTable.columns.items
                do! context.sync().``then``(fun _ -> ())

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

                do! context.sync().``then``(fun _ -> ())

                for i in 0..columns.Length-1 do
                    let column = columns.[i]
                    if Array.contains (column.[0].Trim()) termAndUnitHeaders then
                        let! potBuildingBlock = getBuildingBlockByIndex excelTable i context
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

                        //Check whether value of property colum is fitting for value of main column and adapt if not
                        //Delete values of property columns when main column is empty
                        propertyColumns
                        |> Array.iter (fun (pIndex, pcv) ->
                            let values = Array.create (arcTable.RowCount + 1) ""
                            mainColumnHasValues
                            |> Array.iteri (fun mainIndex (mc, isNull) ->
                                //if isNull for main column, then use empty string as value for properties
                                if not isNull then
                                    values.[mainIndex] <- pcv.[mainIndex]
                            )

                            let bodyValues =
                                values
                                |> Array.map (box >> Some)
                                |> Array.map (fun c -> ResizeArray[c])
                                |> ResizeArray
                            excelTable.columns.items.[pIndex].values <- bodyValues
                        )

                do! ExcelHelper.adoptTableFormats(excelTable, context, true)

                return [InteropLogging.Msg.create InteropLogging.Warning $"The annotation table {excelTable.name} is valid"]
        }
    )

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

        do! context.sync().``then``(fun _ -> ())

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
        let tables = context.workbook.tables
        let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items"; "worksheet"; "name"; "position"; "values"|]))

        let! annotationTables = context.sync().``then``(fun _ ->
            tables.items
            |> Seq.toArray
            |> Array.filter (fun table ->
                table.name.StartsWith("annotationTable")
            )
        )

        let inMemoryTables = new ResizeArray<ArcTable>()
        let mutable msgs = List.Empty

        for i in 0 .. annotationTables.Length-1 do
            let! potErrors = validateAnnotationTable annotationTables.[i] context
            match potErrors with
            | Result.Ok _ ->
                let! result = ArcTable.tryGetFromExcelTable(annotationTables.[i], context)
                inMemoryTables.Add(result.Value)
            | Result.Error messages ->
                messages
                |> List.iter (fun message ->
                    msgs <- message::msgs)

        match msgs.Length with
        | 0 -> return Result.Ok inMemoryTables
        | _ -> return Result.Error msgs
    }

/// <summary>
/// Use this function because template lacks isMetadataSheet method at the moment to avoid code duplication
/// </summary>
/// <param name="collection"></param>
let private parseTempatleToArcFile collection =
    let templateInfo, ers, tags, authors = Template.fromMetadataCollection collection
    Template.fromParts templateInfo ers tags authors (ArcTable.init "New Template") DateTime.Now

/// <summary>
/// Get template and avoid excel annotation tables
/// </summary>
/// <param name="worksheetName"></param>
/// <param name="context"></param>
let private handleTemplateParsingWithoutTable worksheetName context =
    promise {
        let! template = parseToTopLevelMetadata worksheetName parseTempatleToArcFile context
        match template with
        | Some template ->
            let result = ArcFiles.Template template
            return (result, worksheetName)
        | None ->
            let result = ArcFiles.Template (new Template(Guid.NewGuid(), new ArcTable("New ArcTable", new ResizeArray<CompositeHeader>(), new Dictionary<(int*int), CompositeCell>())))
            return (result, worksheetName)
    }

/// <summary>
/// Parse excel metadata into a arc file object withput parsing the table
/// </summary>
let tryParseExcelMetadataToArcFileWihoutTables () =
    Excel.run(fun context ->
        promise {
            let worksheets = context.workbook.worksheets

            let _ = worksheets.load(propertyNames = U2.Case2 (ResizeArray[|"values"; "name"|]))

            do! context.sync().``then``(fun _ -> ())

            let worksheetTopLevelMetadata =
                worksheets.items
                |> Seq.tryFind (fun item ->
                    ArcTable.isTopLevelMetadataName item.name)
            
            match worksheetTopLevelMetadata with
            | Some worksheet when ArcAssay.isMetadataSheetName worksheet.name ->
                let! assay = parseToTopLevelMetadata worksheet.name ArcAssay.fromMetadataCollection context
                match assay with
                | Some assay ->
                    let result = ArcFiles.Assay assay
                    return Result.Ok (result, worksheet.name)
                | None ->
                    let result = ArcFiles.Assay (new ArcAssay("New Assay"))
                    return Result.Ok (result, worksheet.name)
            | Some worksheet when ArcInvestigation.isMetadataSheetName worksheet.name ->
                let! investigation = parseToTopLevelMetadata worksheet.name ArcInvestigation.fromMetadataCollection context
                match investigation with
                | Some investigation ->
                    let result = ArcFiles.Investigation investigation
                    return Result.Ok (result, worksheet.name)
                | None ->
                    let result = ArcFiles.Investigation (new ArcInvestigation("New Investigation"))
                    return Result.Ok (result, worksheet.name)
            | Some worksheet when ArcStudy.isMetadataSheetName worksheet.name ->
                let! (studyCollection) = parseToTopLevelMetadata worksheet.name ArcStudy.fromMetadataCollection context
                match studyCollection with
                | Some (study, assays) ->
                    let result = ArcFiles.Study (study, assays)
                    return Result.Ok (result, worksheet.name)
                | None ->
                    let result = ArcFiles.Study (new ArcStudy("New Study"), [])
                    return Result.Ok (result, worksheet.name)
            | Some worksheet when Template.metaDataSheetName = worksheet.name ->
                let! result = handleTemplateParsingWithoutTable worksheet.name context
                return Result.Ok result
            | Some worksheet when Template.obsoletemetaDataSheetName  = worksheet.name ->
                let! result = handleTemplateParsingWithoutTable worksheet.name context
                return Result.Ok result
            | _ -> return Result.Error ()
        }
    )

/// <summary>
/// Handle parsing of template and check whether there are more than 1 annotation table in the excel file or not
/// Used to avoid code duplication because template lacks the isMetadatasheet method at the moment
/// </summary>
/// <param name="worksheetName"></param>
/// <param name="context"></param>
let private handleTemplateParsing worksheetName context =
    promise {
        let! template = parseToTopLevelMetadata worksheetName parseTempatleToArcFile context
        match template with
        | Some template ->
            let! tableCollection = getExcelAnnotationTables context
            match tableCollection with
            | Result.Ok tables ->
                if tables.Count > 1 then return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"Only one annotation table is allowed for an arc template but this one has {tables.Count} tables!"]
                else
                    template.Table <- (tables.[0])
                    let result = ArcFiles.Template template
                    return Result.Ok result
            | Result.Error msgs -> return Result.Error msgs
        | None -> return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"No top level metadata sheet is available!"]
    }

/// <summary>
/// Tries to get the name of the top level excel worksheet of top level metadata
/// </summary>
let tryParseExcelMetadataToArcFile () =
    Excel.run(fun context ->
        promise {
            let worksheets = context.workbook.worksheets

            let _ = worksheets.load(propertyNames = U2.Case2 (ResizeArray[|"values"; "name"|]))

            do! context.sync().``then``(fun _ -> ())

            let worksheetTopLevelMetadata =
                worksheets.items
                |> Seq.tryFind (fun item ->
                    ArcTable.isTopLevelMetadataName item.name)
            
            match worksheetTopLevelMetadata with
            | Some worksheet when ArcAssay.isMetadataSheetName worksheet.name ->
                let! assay = parseToTopLevelMetadata worksheet.name ArcAssay.fromMetadataCollection context
                let! tableCollection = getExcelAnnotationTables context
                match assay with
                | Some assay ->
                    match tableCollection with
                    | Result.Ok tables ->
                        assay.Tables <- (tables)
                        let result = ArcFiles.Assay assay
                        return Result.Ok result
                    | Result.Error msgs -> return Result.Error msgs
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
                | Some (study, assays) ->
                    let! tableCollection = getExcelAnnotationTables context
                    match tableCollection with
                    | Result.Ok tables ->
                        study.Tables <- (tables)
                        let result = ArcFiles.Study (study, assays)
                        return Result.Ok result
                    | Result.Error msgs -> return Result.Error msgs
                | None -> return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"No top level metadata sheet is available!"]
            | Some worksheet when Template.metaDataSheetName = worksheet.name ->
                let! result = handleTemplateParsing worksheet.name context
                return result
            | Some worksheet when Template.obsoletemetaDataSheetName  = worksheet.name ->
                let! result = handleTemplateParsing worksheet.name context
                return result
            | _ -> return Result.Error [InteropLogging.Msg.create InteropLogging.Error $"No top level metadata sheet is available that determines the type of data!"]
        }
    )

/// <summary>
/// Creates excel worksheet with name for top level metadata
/// </summary>
/// <param name="name"></param>
let createTopLevelMetadata workSheetName =
    Excel.run(fun context ->
        promise {
            let newWorkSheet = context.workbook.worksheets.add(workSheetName)

            try
                newWorkSheet.activate()
                do! context.sync().``then``(fun _ -> ())
                return [InteropLogging.Msg.create InteropLogging.Warning $"The work sheet {workSheetName} has been created"]
            with
                | err -> return [InteropLogging.Msg.create InteropLogging.Error err.Message]
        }
    )

open FsSpreadsheet

/// <summary>
/// Delete excel worksheet that contains top level metadata
/// </summary>
/// <param name="identifier"></param>
let deleteTopLevelMetadata () =
    Excel.run(fun context ->
        promise {
            let worksheets = context.workbook.worksheets

            let _ = worksheets.load(propertyNames = U2.Case2 (ResizeArray[|"values"; "name"|]))

            do! context.sync().``then``(fun _ -> ())

            worksheets.items
            |> Seq.iter (fun worksheet ->
                match worksheet.name with
                | name when ArcAssay.isMetadataSheetName name -> worksheet.delete()
                | name when ArcInvestigation.isMetadataSheetName name -> worksheet.delete()
                | name when ArcStudy.isMetadataSheetName name -> worksheet.delete()
                | Template.metaDataSheetName -> worksheet.delete()
                | Template.obsoletemetaDataSheetName  -> worksheet.delete()
                | _ -> ()
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
let private updateWorkSheet (context:RequestContext) (fsWorkSheet:FsWorksheet) (seqOfSeqs:seq<seq<string option>>) =
    promise {
        let worksheet = context.workbook.worksheets.getItem(fsWorkSheet.Name)
        let range = worksheet.getUsedRange true
        let _ = range.load(propertyNames = U2.Case2 (ResizeArray["values"]))

        do! context.sync().``then``(fun _ -> ())

        range.values <- null

        do! context.sync().``then``(fun _ -> ())

        let range = worksheet.getRangeByIndexes(0, 0, fsWorkSheet.MaxRowIndex, fsWorkSheet.MaxColumnIndex)
        let _ = range.load(propertyNames = U2.Case2 (ResizeArray["values"]))

        do! context.sync().``then``(fun _ -> ())

        let values = ExcelHelper.convertToResizeArrays(seqOfSeqs)

        range.values <- values

        range.format.autofitColumns()
        range.format.autofitRows()

        do! context.sync().``then``(fun _ -> ())
    }

/// <summary>
/// Updates top level metadata excel worksheet of assays
/// </summary>
/// <param name="assay"></param>
let updateTopLevelMetadata (arcFiles: ArcFiles) =
    Excel.run(fun context ->
        promise {
            let worksheet, seqOfSeqs =
                match arcFiles with
                | ArcFiles.Assay assay ->
                    let assayWorksheet = ArcAssay.toMetadataSheet assay
                    let seqOfSeqs = ArcAssay.toMetadataCollection assay
                    assayWorksheet, seqOfSeqs
                | ArcFiles.Investigation investigation ->
                    let investigationWorkbook = ArcInvestigation.toFsWorkbook investigation
                    let investigationWorksheet = investigationWorkbook.GetWorksheetByName(ArcInvestigation.metadataSheetName)
                    let seqOfSeqs = ArcInvestigation.toMetadataCollection investigation
                    investigationWorksheet, seqOfSeqs
                | ArcFiles.Study (study, assays) ->
                    let assays =
                        if assays.IsEmpty then None
                        else Some assays
                    let studyWorksheet = ArcStudy.toMetadataSheet study assays
                    let seqOfSeqs = ArcStudy.toMetadataCollection study assays
                    studyWorksheet, seqOfSeqs
                | ArcFiles.Template template ->
                    let templateWorksheet = Template.toMetadataSheet template
                    let seqOfSeqs = Template.toMetadataCollection template
                    templateWorksheet, seqOfSeqs

            do! updateWorkSheet context worksheet seqOfSeqs

            return [InteropLogging.Msg.create InteropLogging.Warning $"The worksheet {worksheet.Name} has been updated"]
        }
    )

// Old stuff, mostly deprecated

//let private createColumnBodyValues (insertBB:InsertBuildingBlock) (tableRowCount:int) =
//    let createList (rowCount:int) (values:string []) =
//        ResizeArray [|
//            // tableRowCount-2 because -1 to match index-level and -1 to substract header from count
//            for i in 0 .. tableRowCount-2 do
//                yield ResizeArray [|
//                    if i <= rowCount-1 then
//                        box values.[i] |> Some
//                    else
//                        None
//                |]
//        |]
//    match insertBB.HasValues with
//    | false -> [||]
//    | true ->
//        let rowCount = insertBB.Rows.Length
//        if insertBB.ColumnHeader.Type.isSingleColumn then
//            let values          = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.Name))
//            [|values|]
//        elif insertBB.HasUnit then
//            let unitTermRowArr  = Array.init rowCount (fun _ -> insertBB.UnitTerm.Value)
//            let values          = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.Name))
//            let unitTermNames   = createList rowCount (unitTermRowArr |> Array.map (fun tm -> tm.Name))
//            let tsrs            = createList rowCount (unitTermRowArr |> Array.map (fun tm -> tm.accessionToTSR))
//            let tans            = createList rowCount (unitTermRowArr |> Array.map (fun tm -> tm.accessionToTAN))
//            [|values; unitTermNames; tsrs; tans|]
//        else
//            let termNames = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.Name))
//            let tsrs      = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.accessionToTSR))
//            let tans      = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.accessionToTAN))
//            [|termNames; tsrs; tans|]

let addAnnotationBlocksToTable (buildingBlocks:InsertBuildingBlock [], table:Table, context:RequestContext) =
    promise {

        let excelTable = table
        let _ = excelTable.load(U2.Case1 "name")

        let! existingBuildingBlocks = BuildingBlock.getFromContext(context, excelTable)

        /// newBuildingBlocks -> will be added
        /// alreadyExistingBBs -> will be used for logging
        let newBuildingBlocks, alreadyExistingBBs =
            let newSet = buildingBlocks |> Array.map (fun x -> x.ColumnHeader) |> Set.ofArray
            let prevSet = existingBuildingBlocks |> Array.choose (fun x -> x.MainColumn.Header.toBuildingBlockNamePrePrint) |> Set.ofArray
            let bbsToAdd = Set.difference newSet prevSet |> Set.toArray
            // These building blocks do not exist in table and will be added
            let newBuildingBlocks =
                buildingBlocks
                |> Array.filter (fun buildingblock ->
                    // Not existing in annotation table
                    let isNotExisting = bbsToAdd |> Array.contains buildingblock.ColumnHeader
                    // Not input or output column
                    let isInputOutput = buildingblock.ColumnHeader.isOutputColumn && buildingblock.ColumnHeader.isInputColumn
                    isNotExisting && isInputOutput |> not
                )
            // These building blocks exist in table and are part of building block list. Keep them to push them as info msg.
            let existingBBs = Set.intersect newSet prevSet |> Set.toList
            newBuildingBlocks, existingBBs

        // Ref. 2
        // This is necessary to place new columns next to selected col
        let annoHeaderRange = excelTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"; "columnCount"; "rowIndex"|]))
        let tableRange = excelTable.getRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
        let selectedRange = context.workbook.getSelectedRange()
        let _ = selectedRange.load(U2.Case1 "columnIndex")

        let! startIndex, headerVals = context.sync().``then``(fun _ ->
            // Ref. 3
            /// This is necessary to place new columns next to selected col.
            let rebasedIndex = rebaseIndexToTable selectedRange annoHeaderRange

            // This is necessary to skip over hidden cols
            /// Get an array of the headers
            let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq

            /// Here is the next col index, which is not hidden, calculated.
            let nextIndex = findIndexNextNotHiddenCol headerVals rebasedIndex
            nextIndex, headerVals
        )

        let startColumnCount = tableRange.columnCount |> int
        /// Calculate if building blocks have values and if they have more values than currently rows exist for the table
        /// Return Some "n of missing rows" to match n of values from building blocks or None
        let expandByNRows =
            let maxNewRowCount =
                if Array.isEmpty newBuildingBlocks then
                    0
                else
                    newBuildingBlocks |> Array.map (fun x -> x.Rows.Length) |> Array.max
            let startRowCount = tableRange.rowCount |> int
            // substract one from table rowCount because of header row
            // maxNewRowCount refers to body rows
            let nOfMissingRows = maxNewRowCount - (startRowCount-1)
            if nOfMissingRows > 0 then
                Some nOfMissingRows
            else
                None

        // Expand table by min rows, only done if necessary
        let! expandedTable, expandedRowCount =
            if expandByNRows.IsSome then
                promise {
                    let! expandedTable,expandedTableRange = context.sync().``then``(fun _ ->
                        let newRowsValues = createMatrixForTables startColumnCount expandByNRows.Value ""
                        let newRows =
                            excelTable.rows.add(
                                values = U4.Case1 newRowsValues
                            )
                        let newTable = context.workbook.tables.getItem(excelTable.name)
                        let newTableRange = excelTable.getRange()
                        let _ = newTableRange.load(U2.Case2 (ResizeArray(["columnCount"; "rowCount"])))
                        excelTable,newTableRange
                    )
                    let! expandedRowCount = context.sync().``then``(fun _ -> int expandedTableRange.rowCount)
                    return (expandedTable, expandedRowCount)
                }
            else
                promise { return (excelTable, tableRange.rowCount |> int) }


        //create an empty column to insert
        let col value = createMatrixForTables 1 expandedRowCount value

        let mutable nextIndex = startIndex
        let mutable allColumnHeaders = headerVals |> Array.choose id |> Array.map string |> List.ofArray

        let addBuildingBlock (buildingBlock:InsertBuildingBlock) (currentNextIndex:float) (columnHeaders:string []) =

            let columnNames = Indexing.createColumnNames buildingBlock columnHeaders

            //printfn "%A" columnNames

            // Update storage for variables
            nextIndex <- currentNextIndex + float columnNames.Length
            allColumnHeaders <- (columnNames |> List.ofArray)@allColumnHeaders

            let createAllCols =
                let createCol index =
                    expandedTable.columns.add(
                        index   = index,
                        values  = U4.Case1 (col "")
                    )
                columnNames
                |> Array.mapi (fun i colName ->
                    // create a single column
                    let col = createCol (currentNextIndex + float i)
                    // add column header name
                    col.name <- colName
                    let columnBody = col.getDataBodyRange()
                    // Fit column width to content
                    columnBody.format.autofitColumns()
                    // Update mainColumn body rows with number format IF building block has unit.
                    if buildingBlock.UnitTerm.IsSome && colName = columnNames.[0] then
                        // create numberFormat for unit columns
                        let format = buildingBlock.UnitTerm.Value.toNumberFormat
                        let formats = createValueMatrix 1 (expandedRowCount-1) format
                        columnBody.numberFormat <- formats
                    else
                        let format = $"General"
                        let formats = createValueMatrix 1 (expandedRowCount-1) format
                        columnBody.numberFormat <- formats
                    //if buildingBlock.HasValues then
                    //    let values = createColumnBodyValues buildingBlock expandedRowCount
                    //    columnBody.values <- values.[i]
                    // hide freshly created column if it is a reference column
                    if colName <> columnNames.[0] then
                        columnBody.columnHidden <- true
                    col
                )

            columnNames

        let! addNewBuildingBlocks =
            context.sync().``then``(fun _ ->
                newBuildingBlocks
                |> Array.collect (fun (buildingBlock) ->
                    let colHeadersArr = allColumnHeaders |> Array.ofList
                    let addedBlockName = addBuildingBlock buildingBlock nextIndex colHeadersArr
                    addedBlockName
                )
            )

        let! fit = autoFitTableByTable expandedTable context

        let createColsMsg =
            let msg =
                if alreadyExistingBBs.IsEmpty
                    then $"Added protocol building blocks successfully."
                else
                    let skippedBBs = alreadyExistingBBs |> List.map (fun x -> x.toAnnotationTableHeader()) |> String.concat ", "
                    $"Insert completed successfully, but Swate found already existing building blocks in table. Building blocks must be unique. Skipped the following \"{skippedBBs}\"."
            InteropLogging.Msg.create InteropLogging.Info msg

        let logging = [
            yield! fit
            createColsMsg
        ]

        return logging
    }

let addAnnotationBlocks (buildingBlocks:CompositeColumn []) =
    Excel.run(fun context ->

        promise {

            //let! tryTable = tryFindActiveAnnotationTable()
            //let sheet = context.workbook.worksheets.getActiveWorksheet()

            //let! annotationTable, logging =
            //    match tryTable with
            //    | Success table ->
            //        (
            //            sheet.tables.getItem(table),
            //            InteropLogging.Msg.create InteropLogging.Info "Found annotation table for template insert!"
            //        )
            //        |> JS.Constructors.Promise.resolve
            //    | Error e ->
            //        let range =
            //            // not sure if this try...with is necessary as on creating a new worksheet it will autoselect the A1 cell.
            //            try
            //                context.workbook.getSelectedRange()
            //            with
            //                | e -> sheet.getUsedRange()
            //        createAnnotationTableAtRange(false, false, range, context)


            //let! addBlocksLogging = addAnnotationBlocksToTable(buildingBlocks,annotationTable,context)

            //return logging::addBlocksLogging
            return [InteropLogging.Msg.create InteropLogging.Warning "Stop!"]
        }
    )

let addAnnotationBlocksInNewSheet activateWorksheet (worksheetName:string,buildingBlocks:InsertBuildingBlock []) =
    Excel.run(fun context ->
        promise {

            let newWorksheet = context.workbook.worksheets.add(name=worksheetName)

            let worksheetRange = newWorksheet.getUsedRange()

            let! newTable, newTableLogging = createAnnotationTableAtRange (false, false, worksheetRange, context)

            let! addNewBuildingBlocksLogging = addAnnotationBlocksToTable(buildingBlocks, newTable, context)

            if activateWorksheet then newWorksheet.activate()

            let newSheetLogging = InteropLogging.Msg.create InteropLogging.Info $"Create new worksheet: {worksheetName}"

            return newSheetLogging::newTableLogging::addNewBuildingBlocksLogging
        }
    )

let addAnnotationBlocksInNewSheets (excelTablesToAdd: (string*InsertBuildingBlock []) []) =
    // Use different context instances for individual worksheet creation.
    // Does not work with Excel.run at the beginning and passing the related context to subfunctions.
    excelTablesToAdd
    |> Array.mapi (fun i x ->
        let acitvate = i = excelTablesToAdd.Length-1
        addAnnotationBlocksInNewSheet acitvate x

    )
    |> Promise.all
    |> Promise.map List.concat

///// This function is used to add unit reference columns to an existing building block without unit reference columns
//let updateUnitForCells (unitTerm:TermMinimal) =
//    Excel.run(fun context ->

//        promise {

//            let! excelTableName = getActiveAnnotationTableName context

//            let sheet = context.workbook.worksheets.getActiveWorksheet()
//            let excelTable = sheet.tables.getItem(excelTableName)
//            let _ = excelTable.columns.load(propertyNames = U2.Case2 (ResizeArray(["items"])))

//            let selectedRange = context.workbook.getSelectedRange()
//            let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"rowIndex"; "rowCount";])))

//            let annoHeaderRange = excelTable.getHeaderRowRange()
//            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))
//            let tableRange = excelTable.getRange()
//            let _ = tableRange.load(U2.Case2 (ResizeArray(["rowCount"])))

//            let! selectedBuildingBlock = OfficeInterop.BuildingBlockFunctions.findSelectedBuildingBlock context excelTableName

//            let! headerVals = context.sync().``then``(fun _ ->
//                // Get an array of the headers
//                annoHeaderRange.values.[0] |> Array.ofSeq
//            )

//            // Check if building block has existing unit
//            let! updateWithUnit =
//                // is unit column already exists we want to update selected cells
//                if selectedBuildingBlock.hasUnit then
//                    context.sync().``then``(fun _ ->

//                        let format = unitTerm.toNumberFormat
//                        let formats = createValueMatrix 1 (int selectedRange.rowCount) format
//                        selectedRange.numberFormat <- formats
//                        InteropLogging.Msg.create InteropLogging.Info $"Updated specified cells with unit: {format}."
//                    )
//                // if no unit column exists for the selected building block we want to add a unit column and update the whole main column with the unit.
//                elif selectedBuildingBlock.hasCompleteTSRTAN && selectedBuildingBlock.hasUnit |> not then
//                    context.sync().``then``(fun _ ->
//                        // Create unit column
//                        // Create unit column name
//                        let allColHeaders =
//                            headerVals
//                            |> Array.choose id
//                            |> Array.map string
//                        let unitColName = OfficeInterop.Indexing.createUnit() |> Indexing.extendName allColHeaders
//                        // add column at correct index
//                        let unitColumn =
//                            excelTable.columns.add(
//                                index = float selectedBuildingBlock.MainColumn.Index + 1.
//                            )
//                        // Add column name
//                        unitColumn.name <- unitColName
//                        // Change number format for main column
//                        // Get main column table body range
//                        let mainCol = excelTable.columns.items.[selectedBuildingBlock.MainColumn.Index].getDataBodyRange()
//                        // Create unitTerm number format
//                        let format = unitTerm.toNumberFormat
//                        let formats = createValueMatrix 1 (int tableRange.rowCount - 1) format
//                        mainCol.numberFormat <- formats

//                        InteropLogging.Msg.create InteropLogging.Info $"Created Unit Column {unitColName} for building block {selectedBuildingBlock.MainColumn.Header.SwateColumnHeader}."
//                    )
//                else
//                    failwith $"You can only add unit to building blocks of the type: {BuildingBlockType.TermColumns}"
//            let! _ = autoFitTable true context
//            return [updateWithUnit]
//        }

//    )

/// <summary>This function removes a given building block from a given annotation table.
/// It returns the affected column indices.</summary>
let removeAnnotationBlock (tableName:string) (annotationBlock:BuildingBlock) (context:RequestContext) =
    promise {

        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let table = sheet.tables.getItem(tableName)

        // Ref. 2
        let _ = table.load(U2.Case1 "columns")
        let tableCols = table.columns.load(propertyNames = U2.Case1 "items")

        let targetedColIndices =
            let refColIndices =
                if annotationBlock.hasUnit then
                    [| annotationBlock.Unit.Value.Index; annotationBlock.TAN.Value.Index; annotationBlock.TSR.Value.Index |]
                elif annotationBlock.hasCompleteTSRTAN then
                    [| annotationBlock.TAN.Value.Index; annotationBlock.TSR.Value.Index |]
                else
                    [| |]
            [|  annotationBlock.MainColumn.Index
                yield! refColIndices
            |] |> Array.sort

        do! context.sync().``then``(fun _ ->
                targetedColIndices |> Array.map (fun targetIndex ->
                    tableCols.items.[targetIndex].delete()
                ) |> ignore
            )

        return targetedColIndices
    }

//let removeAnnotationBlocks (tableName:string) (annotationBlocks:BuildingBlock [])  =
//    annotationBlocks
//    |> Array.sortByDescending (fun x -> x.MainColumn.Index)
//    |> Array.map (removeAnnotationBlock tableName)
//    |> Promise.all

//let removeSelectedAnnotationBlock () =
//    Excel.run(fun context ->

//        promise {

//            let! excelTable = getActiveAnnotationTableName context

//            let! selectedBuildingBlock = OfficeInterop.BuildingBlockFunctions.findSelectedBuildingBlock context excelTable

//            let! deleteCols = removeAnnotationBlock excelTable selectedBuildingBlock context

//            let resultMsg = InteropLogging.Msg.create InteropLogging.Info $"Delete Building Block {selectedBuildingBlock.MainColumn.Header.SwateColumnHeader} (Cols: {deleteCols})"

//            let! format = autoFitTableHide context

//            return [resultMsg]
//        }
//    )

let getAnnotationBlockDetails() =
    Excel.run(fun context ->
        promise {

            let! excelTable = getActiveAnnotationTableName context

            let! selectedBuildingBlock = OfficeInterop.BuildingBlockFunctions.findSelectedBuildingBlock context excelTable

            let searchTerms = selectedBuildingBlock |> fun bb -> OfficeInterop.BuildingBlockFunctions.toTermSearchable bb

            return searchTerms
        }
    )

let checkForDeprecation (buildingBlocks:BuildingBlock [])  =
    let mutable msgList = []
    // https://github.com/nfdi4plants/Swate/issues/201
    // Output column "Data File Name" Shared.OfficeInteropTypes.BuildingBlockType.Data was deprecated in version 0.6.0
    let deprecated_DataFileName (buildingBlocks:BuildingBlock []) =
        let hasDataFileNameCol = buildingBlocks |> Array.tryFind (fun x -> x.MainColumn.Header.SwateColumnHeader = Shared.OfficeInteropTypes.BuildingBlockType.Data.toString)
        match hasDataFileNameCol with
        | Some _ ->
            let m =
                $"""Found deprecated output column "{Shared.OfficeInteropTypes.BuildingBlockType.Data.toString}". Obsolete since v0.6.0.
                It is recommend to replace "{Shared.OfficeInteropTypes.BuildingBlockType.Data.toString}" with "{Shared.OfficeInteropTypes.BuildingBlockType.RawDataFile.toString}"
                or "{Shared.OfficeInteropTypes.BuildingBlockType.DerivedDataFile.toString}"."""
            let message = InteropLogging.Msg.create InteropLogging.Warning m
            msgList <- message::msgList
            buildingBlocks
        | None -> buildingBlocks
    // Chain all deprecation checks
    buildingBlocks
    |> deprecated_DataFileName
    |> ignore
    // Only return msg list. Messages with InteropLogging.Warning will be pushed to user.
    msgList

let getAllAnnotationBlockDetails() =
    Excel.run(fun context ->
        promise {
            let! excelTableName = getActiveAnnotationTableName context

            let! buildingBlocks = OfficeInterop.BuildingBlockFunctions.getBuildingBlocks context excelTableName

            let deprecationMsgs = checkForDeprecation buildingBlocks

            let searchTerms = buildingBlocks |> Array.collect OfficeInterop.BuildingBlockFunctions.toTermSearchable

            return (searchTerms, deprecationMsgs)
        }
    )

/// <summary>This function is used to parse a selected header to a TermMinimal type, used for relationship directed term search.</summary>
let getTermFromHeaderValues (headerValues: ResizeArray<obj option>) (selectedHeaderColIndex: int) =
    // is selected range is in table then take header value from selected column
    let header = headerValues.[selectedHeaderColIndex] |> Option.get |> string |> SwateColumnHeader.create
    // as the reference columns also contain a accession tag we want to return the first reference column header
    // instead of the main column header, if the main column header does include an ontology
    match header with
    | notUsedToDirectedTermSearch when header.isSingleCol || header.isTANCol || header.isTSRCol || header.isUnitCol -> None
    | isFeaturedColumn when header.isFeaturedCol ->
        let termAccession = header.getFeaturedColAccession
        let parentTerm = TermMinimal.create header.SwateColumnHeader termAccession |> Some
        parentTerm
    | isTermCol when header.tryGetOntologyTerm.IsSome ->
        let termName = header.tryGetOntologyTerm.Value
        let termAccession =
            let headerIndexPlus1 = SwateColumnHeader.create ( Option.defaultValue (box "") headerValues.[selectedHeaderColIndex+1] |> string )
            let headerIndexPlus2 = SwateColumnHeader.create ( Option.defaultValue (box "") headerValues.[selectedHeaderColIndex+2] |> string )
            if not headerIndexPlus1.isUnitCol && headerIndexPlus1.isTSRCol then
                headerIndexPlus1.tryGetTermAccession
            elif headerIndexPlus1.isUnitCol && headerIndexPlus2.isTSRCol then
                headerIndexPlus2.tryGetTermAccession
            else
                None

        let parentTerm = TermMinimal.create termName (Option.defaultValue "" termAccession) |> Some
        parentTerm
    | _ -> None

// Reform this to onSelectionChanged (Even though we now know how to add eventHandlers we do not know how to pass info from handler to Swate app).
/// <summary>This function will parse the header of a selected column to check for a parent ontology, which will then be used for a isA-directed term search.
/// Any found parent ontology will also be displayed in a static field before the term search input field.</summary>
let getParentTerm () =
    Excel.run (fun context ->

        promise {
            try
                let! excelTable = getActiveAnnotationTableName context
                // Ref. 2
                let sheet = context.workbook.worksheets.getActiveWorksheet()
                let excelTable = sheet.tables.getItem(excelTable)
                let tableRange = excelTable.getRange()
                let _ = tableRange.load(U2.Case2 (ResizeArray[|"columnIndex"; "rowIndex"; "values"|]))
                let range = context.workbook.getSelectedRange()
                let _ = range.load(U2.Case2 (ResizeArray[|"columnIndex"; "rowIndex"|]))

                let! res = context.sync().``then``( fun _ ->

                    // Ref. 3
                    // recalculate the selected col index from a worksheet perspective to the table perspective.
                    let newColIndex =
                        let tableRangeColIndex = tableRange.columnIndex
                        let selectColIndex = range.columnIndex
                        selectColIndex - tableRangeColIndex |> int

                    let newRowIndex =
                        let tableRangeRowIndex = tableRange.rowIndex
                        let selectedRowIndex = range.rowIndex
                        selectedRowIndex - tableRangeRowIndex |> int

                    // Get all values from the table range
                    let colHeaderVals = tableRange.values.[0]
                    let rowVals = tableRange.values
                    // Get the index of the last column in the table
                    let lastColInd = colHeaderVals.Count-1
                    // Get the index of the last row in the table
                    let lastRowInd = rowVals.Count-1
                    let value =
                        // check if selected range is inside table
                        if
                            newColIndex < 0
                            || newColIndex > lastColInd
                            || newRowIndex < 0
                            || newRowIndex > lastRowInd
                        then
                            None
                        else
                            getTermFromHeaderValues colHeaderVals newColIndex
                    // return parent term of selected col
                    value
                )
                return res
            with
                | exn -> return None
        }
    )

/// <summary>This is used to insert terms into selected cells.
/// 'term' is the value that will be written into the main column.
/// 'termBackground' needs to be spearate from 'term' in case the user uses the fill function for a custom term.
/// Should the user write a real term with this function 'termBackground'.isSome and can be used to fill TSR and TAN.</summary>
let insertOntologyTerm (term:OntologyAnnotation) =
    Excel.run(fun context ->
        // Ref. 2
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "rowIndex"; "columnCount"; "rowCount"])))
        // This is for TSR and TAN
        let nextColsRange = range.getColumnsAfter 2.
        let _ = nextColsRange.load(U2.Case2 (ResizeArray(["values";"columnIndex";"columnCount"])))

        // Ref. 1
        let r = context.runtime.load(U2.Case1 "enableEvents")

        promise {

            let! tryTable = tryFindActiveAnnotationTable()

            // This function checks multiple scenarios destroying Swate table formatting through the insert ontology term function
            do! match tryTable with
                | Success table ->
                    promise {
                        // Input column also affects the next 2 columns so [range.columnIndex; range.columnIndex+1.; range.columnIndex+2.]
                        let sheet = context.workbook.worksheets.getActiveWorksheet()
                        let table = sheet.tables.getItem(table)
                        let tableRange = table.getRange()
                        let _ = tableRange.load(U2.Case2 (ResizeArray(["rowIndex"; "rowCount"; "columnIndex"; "columnCount"])))
                        // sync load to receive values
                        let! inputRow, lastInputRow, inputColumn, lastInputColumn = context.sync().``then``(fun _ -> range.rowIndex, range.rowIndex+range.rowCount, range.columnIndex, range.columnIndex + 2.)
                        //printfn $"inputRow: {inputRow}, lastInputRow: {lastInputRow}"
                        //printfn $"inputColumn: {inputColumn}, lastInputColumn: {lastInputColumn}"
                        let lastColumnIndex = tableRange.columnIndex + tableRange.columnCount
                        let lastRowIndex = tableRange.rowIndex + tableRange.rowCount
                        //printfn "rowIndex: %A, lastRowIndex: %A" tableRange.rowIndex lastRowIndex
                        //printfn "columnIndex: %A, lastColumnIndex: %A" tableRange.columnIndex lastColumnIndex
                        let isInBodyRows = (inputRow >= tableRange.rowIndex || lastInputRow >= tableRange.rowIndex) && (inputRow <= lastRowIndex || lastInputRow <= lastRowIndex)
                        let isInBodyColumns = (inputColumn >= tableRange.columnIndex || lastInputColumn >= tableRange.columnIndex) && (inputColumn <= lastColumnIndex || lastInputColumn <= lastColumnIndex)
                        //printfn "isInBodyRows: %A; isInBodyColumns: %A" isInBodyRows isInBodyColumns
                        // Never add ontology terms inside annotation table header (this will also prevent adding terms right next to the table,
                        // which is good, as excel would extend the table around the inserted term, destroying the annotation table format
                        if [inputRow .. lastInputRow] |> List.contains tableRange.rowIndex then
                            if isInBodyColumns then
                                failwith "Cannot insert ontology term into annotation table header row. If you want to create new building blocks, please use the Add Building Block function."
                        // Never add ontology terms right next to the table as excel would extend the table around the inserted term, destroying the annotation table format.
                        // Check row below table
                        if inputRow = lastRowIndex then
                            if isInBodyColumns then failwith "Cannot insert ontology term directly underneath an annotation table!"
                        // Check column to the right side of the table AND check the two columns left of the table (function fills 3 columns)
                        if inputColumn = lastColumnIndex || inputColumn = tableRange.columnIndex - 2. || inputColumn = tableRange.columnIndex - 1. then
                            if isInBodyRows then failwith "Cannot insert ontology term directly next to an annotation table!"
                        let! buildingblocks = BuildingBlock.getFromContext(context,table)
                        // Never add ontology terms to input/output columns and Only to main columns
                        let mainColumnIndices =
                            buildingblocks
                            // cannot be added to input/output columns
                            |> Array.filter(fun x -> not x.MainColumn.Header.isOutputCol && not x.MainColumn.Header.isInputCol )
                            |> Array.map(fun x -> x.MainColumn.Index)
                        // check if 'inputColumn' = any of the maincolumn indices
                        let isInsideTable = isInBodyRows && isInBodyColumns
                        if isInsideTable then
                            printfn "mainColumnIndices: %A" mainColumnIndices
                            /// indices start at table begin, so we need to rebase our inputcolumn index to table start
                            let rebasedIndex = inputColumn - tableRange.columnIndex |> int
                            if mainColumnIndices |> Array.contains rebasedIndex = false then failwith "Cannot insert ontology term to input/output/reference columns of an annotation table!"
                        return ()
                    }
                | Error e       -> JS.Constructors.Promise.resolve(())

            //sync with proxy objects after loading values from excel
            let! res = context.sync().``then``( fun _ ->

                // failwith if the number of selected columns is > 1. This is done due to hidden columns
                // and an overlapping reaction as we add values to the columns next to the selected one
                if range.columnCount > 1. then failwith "Cannot insert Terms in more than one column at a time."

                r.enableEvents <- false

                // create new values for selected range
                let newVals = ResizeArray([
                    for arr in range.values do
                        let tmp = arr |> Seq.map (fun _ -> Some (term.Name |> box))
                        ResizeArray(tmp)
                ])

                // create values for TSR and TAN
                let nextNewVals = ResizeArray([
                    // iterate over rows
                    for ind in 0 .. nextColsRange.values.Count-1 do
                        let tmp =
                            nextColsRange.values.[ind]
                            // iterate over cols
                            |> Seq.mapi (fun i _ ->
                                match i, term.TermAccessionShort = String.Empty with
                                | 0, true | 1, true ->
                                    None
                                | 0, false ->
                                    //add "Term Source REF"
                                    term.TermSourceREF |> Option.map box
                                | 1, false ->
                                    //add "Term Accession Number"
                                    Some ( term.TermAccessionOntobeeUrl |> box )
                                | _, _ ->
                                    r.enableEvents <- true
                                    failwith "The insert should never add more than two extra columns."
                            )
                        ResizeArray(tmp)
                ])
                // fill selected range with new values
                range.values <- newVals
                // fill TSR and TAN with new values
                nextColsRange.values <- nextNewVals

                r.enableEvents <- true

                // return print msg
                "Info",sprintf "Insert %A %Ax" term nextColsRange.values.Count
            )

            let! fit =
                match tryTable with
                | Success table -> autoFitTableHide context
                | Error e       -> JS.Constructors.Promise.resolve([])

            return res
        }
    )

/// <summary>This function will be executed after the SearchTerm types from 'createSearchTermsFromTable' where send to the server to search the database for them.
/// Here the results will be written into the table by the stored col and row indices.</summary>
let UpdateTableByTermsSearchable (terms:TermSearchable []) =
    Excel.run(fun context ->

        // This will create a single cell value arr
        let createCellValueInput str =
            ResizeArray([
                ResizeArray([
                    str |> box |> Some
                ])
            ])

        let getBodyRows (terms:TermSearchable []) =
            terms |> Array.filter (fun x -> x.RowIndices <> [|0|])

        let getHeaderRows (terms:TermSearchable []) =
            terms |> Array.filter (fun x -> x.RowIndices = [|0|])

        let createMainColName coreName searchResultName columnHeaderId = $"{coreName} [{searchResultName}{columnHeaderId}]"
        let createTSRColName searchResultTermAccession columnHeaderId = $"{ColumnCoreNames.TermSourceRef.toString} ({searchResultTermAccession}{columnHeaderId})"
        let createTANColName searchResultTermAccession columnHeaderId = $"{ColumnCoreNames.TermAccessionNumber.toString} ({searchResultTermAccession}{columnHeaderId})"

        promise {
            let! excelTableName = getActiveAnnotationTableName context
            // Ref. 2
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let excelTable = sheet.tables.getItem(excelTableName)
            // Ref. 2
            let tableBodyRange = excelTable.getDataBodyRange()
            let _ = tableBodyRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore

            let tableHeaderRange = excelTable.getHeaderRowRange()
            let _ = tableHeaderRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore

            // Ref. 1
            let r = context.runtime.load(U2.Case1 "enableEvents")

            let bodyTerms = terms |> getBodyRows
            let headerTerms = terms |> getHeaderRows

            let! buildingBlocks = OfficeInterop.BuildingBlockFunctions.getBuildingBlocks context excelTableName

            let! resultMsg =
                context.sync().``then``(fun _ ->
                    r.enableEvents <- false
                    /// Update only table column headers
                    let numberOfUpdatedHeaderTerms =
                        headerTerms
                        |> Array.map (fun headerTerm ->
                            let relBuildingBlock = buildingBlocks |> Array.find (fun bb -> bb.MainColumn.Index = headerTerm.ColIndex)
                            let columnHeaderId =
                                let opt = relBuildingBlock.MainColumn.Header.tryGetHeaderId
                                match opt with
                                | Some id   -> id
                                | None      -> ""
                            let columnCoreName =
                                let opt = relBuildingBlock.MainColumn.Header.getColumnCoreName
                                match opt with
                                | Some t    -> t
                                | None      -> failwith $"Could not get Swate compatible column name from {relBuildingBlock.MainColumn.Header.SwateColumnHeader}."
                            match headerTerm with
                            | hasSearchResult when headerTerm.SearchResultTerm.IsSome ->
                                let searchResult = TermMinimal.ofTerm hasSearchResult.SearchResultTerm.Value
                                let mainColName = createMainColName columnCoreName searchResult.Name columnHeaderId
                                let tsrColName = createTSRColName searchResult.TermAccession columnHeaderId
                                let tanColName = createTANColName searchResult.TermAccession columnHeaderId
                                // if building block has unit, then tsr and tan are one index further to the right
                                let baseShift = if relBuildingBlock.hasUnit then float headerTerm.ColIndex + 1. else float headerTerm.ColIndex + 0.
                                if relBuildingBlock.MainColumnTerm.IsSome && relBuildingBlock.MainColumnTerm.Value = searchResult then
                                    // return 0 as nothing needs be to be changed
                                    0
                                elif relBuildingBlock.MainColumnTerm.IsSome && relBuildingBlock.MainColumnTerm.Value <> searchResult then
                                    let mainColHeader= tableHeaderRange.getCell(0., float headerTerm.ColIndex)
                                    let tsrColHeader= tableHeaderRange.getCell(0., baseShift + 1.)
                                    let tanColHeader= tableHeaderRange.getCell(0., baseShift + 2.)
                                    mainColHeader.values   <- createCellValueInput mainColName
                                    tsrColHeader.values    <- createCellValueInput tsrColName
                                    tanColHeader.values    <- createCellValueInput tanColName
                                    1
                                else
                                    failwith $"Swate enocuntered an unknown term pattern. Found term {hasSearchResult.Term} for building block {relBuildingBlock.MainColumn.Header.SwateColumnHeader}"
                            // This is hit when free text input is entered as building block
                            | hasNoSearchResult when headerTerm.SearchResultTerm.IsNone ->
                                if
                                    relBuildingBlock.MainColumnTerm.IsSome
                                    && relBuildingBlock.MainColumnTerm.Value.Name = hasNoSearchResult.Term.Name
                                    && relBuildingBlock.MainColumnTerm.Value.Name <> ""
                                    && relBuildingBlock.MainColumnTerm.Value.TermAccession = ""
                                then
                                    0
                                elif
                                    relBuildingBlock.MainColumnTerm.IsSome
                                    && relBuildingBlock.MainColumnTerm.Value.Name = hasNoSearchResult.Term.Name
                                    && relBuildingBlock.MainColumnTerm.Value.Name <> ""
                                then
                                    let mainColName = createMainColName columnCoreName hasNoSearchResult.Term.Name columnHeaderId
                                    let tsrColName = createTSRColName "" columnHeaderId
                                    let tanColName = createTANColName "" columnHeaderId
                                    let baseShift = if relBuildingBlock.hasUnit then float headerTerm.ColIndex + 1. else float headerTerm.ColIndex + 0.
                                    let mainColHeader= tableHeaderRange.getCell(0., float headerTerm.ColIndex)
                                    let tsrColHeader= tableHeaderRange.getCell(0., baseShift + 1.)
                                    let tanColHeader= tableHeaderRange.getCell(0., baseShift + 2.)
                                    mainColHeader.values   <- createCellValueInput mainColName
                                    tsrColHeader.values    <- createCellValueInput tsrColName
                                    tanColHeader.values    <- createCellValueInput tanColName
                                    1
                                else
                                    failwith $"Swate enocuntered an unknown term pattern. Found no term in database for building block {relBuildingBlock.MainColumn.Header.SwateColumnHeader}"
                            | anythingElse -> failwith $"Swate encountered an unknown term pattern. Search result: {anythingElse} for buildingBlock {relBuildingBlock.MainColumn.Header.SwateColumnHeader}"

                        )

                    // Insert table body terms into related cells for all stored row-/ col-indices
                    let numberOfUpdatedBodyTerms =
                        bodyTerms
                        // iterate over all found terms
                        |> Array.map (
                            fun term ->
                                let t,tsr,tan=
                                    if term.SearchResultTerm.IsSome then
                                        // Term search result from database
                                        let t = term.SearchResultTerm.Value
                                        // Get ontology and accession from Term.Accession
                                        let tsr, tan =
                                            let splitAccession = t.Accession.Split":"
                                            let tan = OntologyAnnotation(tan=t.Accession).TermAccessionOntobeeUrl
                                            splitAccession.[0], tan
                                        t.Name, tsr, tan
                                    elif term.Term.Name = "" then
                                        let t = ""
                                        let ont = ""
                                        let accession = ""
                                        t, ont, accession
                                    elif term.SearchResultTerm = None then
                                        let t = term.Term.Name
                                        let ont = FreeTextInput
                                        let accession = FreeTextInput
                                        t, ont, accession
                                    else
                                        failwith $"Swate could not parse database search results for term: {term.Term.Name}."
                                // iterate over all rows and insert the correct inputVal
                                for rowInd in term.RowIndices do
                                    // ColIndex saves the column index of the main column. In case of a unit term the term gets inserted at maincolumn index + 1.
                                    // TSR and TAN are also shifted to the right by 1.
                                    let termNameIndex = if term.IsUnit then float term.ColIndex + 1. else float term.ColIndex
                                    // Terms are saved based on rowIndex for the whole table. As the following works on the TableBodyRange we need to reduce the indices by 1.
                                    let tableBodyRowIndex = float rowInd - 1.
                                    let mainColumnCell = tableBodyRange.getCell(tableBodyRowIndex, termNameIndex)
                                    let tsrColumnCell = tableBodyRange.getCell(tableBodyRowIndex, termNameIndex + 1.)
                                    let tanColumnCell = tableBodyRange.getCell(tableBodyRowIndex, termNameIndex + 2.)
                                    mainColumnCell.values   <- createCellValueInput t
                                    tsrColumnCell.values    <- createCellValueInput tsr
                                    tanColumnCell.values    <- createCellValueInput tan
                                1
                        )

                    r.enableEvents <- true

                    InteropLogging.Msg.create InteropLogging.Info $"Updated {numberOfUpdatedBodyTerms |> Array.sum} terms in table body. Updated {numberOfUpdatedHeaderTerms |> Array.sum} terms in table column headers."
                )
            return [resultMsg]
        }
    )

/// <summary>This function is used to insert file names into the selected range.</summary>
let insertFileNamesFromFilePicker (fileNameList:string list) =
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

            let! excelTable = getActiveAnnotationTableName context
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let excelTable = sheet.tables.getItem(excelTable)
            let _ =excelTable.columns.load(propertyNames = U2.Case1 "count") |> ignore
            let _ =excelTable.rows.load(propertyNames = U2.Case1 "count")    |> ignore
            let rowRange = excelTable.getRange()
            let _ = rowRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore
            let headerRange = excelTable.getHeaderRowRange()
            let _ = headerRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore

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

let deleteAllCustomXml() =
    Excel.run(fun context ->

        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
        // https://docs.microsoft.com/en-us/javascript/api/excel/excel.customxmlpartcollection?view=excel-js-preview

        promise {

            do! context.sync().``then``(fun _ ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.delete() )

                    xmls |> Array.ofSeq |> ignore
                )

            return "Info","Deleted All Custom Xml!"
        }
    )

let getSwateCustomXml() =
    Excel.run(fun context ->

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

        promise {

            let! getXml =
                context.sync().``then``(fun _ ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.getXml() )

                    xmls |> Array.ofSeq
                )

            let! xml =
                context.sync().``then``(fun _ ->

                    //let nOfItems = customXmlParts.items.Count
                    let vals = getXml |> Array.map (fun x -> x.value)
                    //sprintf "N = %A; items: %A" nOfItems vals
                    let xml = vals |> String.concat Environment.NewLine
                    xml
                )

            return xml
        }
    )

let updateSwateCustomXml(newXmlString:String) =
    Excel.run(fun context ->

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

        promise {

            do! context.sync().``then``(fun _ ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.delete() )

                    xmls |> Array.ofSeq |> ignore
                )

            do! context.sync().``then``(fun _ ->
                    customXmlParts.add(newXmlString) |> ignore
                )

            return "Info", "Custom xml update successful"
        }
    )

//let addTableValidationToExisting (tableValidation:ValidationTypes.TableValidation, colNames: string list) =
//    Excel.run(fun context ->
//        let getBaseName (colHeader:string) =
//            let parsedHeader = parseColHeader colHeader
//            let ont = if parsedHeader.Ontology.IsSome then sprintf " [%s]" parsedHeader.Ontology.Value.Name else ""
//            sprintf "%s%s" parsedHeader.CoreName.Value ont

//        let newColNameMap =
//            colNames |> List.map (fun x ->
//                getBaseName x, x
//            )
//            |> Map.ofList
//        //failwith (sprintf "%A" tableValidation)

//        let updateColumnValidationColNames =
//            tableValidation.ColumnValidations
//            |> List.filter (fun x -> x.ColumnHeader <> BuildingBlockType.Source.toString && x.ColumnHeader <> BuildingBlockType.Sample.toString)
//            |> List.map (fun previousColVal ->
//                let baseName = getBaseName previousColVal.ColumnHeader
//                let newName =
//                    newColNameMap.[baseName]
//                {previousColVal with ColumnHeader = newName}
//            )

//        // Update DateTime
//        let newTableValidation = {
//            tableValidation with
//                DateTime = System.DateTime.Now.ToUniversalTime()
//                ColumnValidations = updateColumnValidationColNames
//            }

//        // The first part accesses current CustomXml
//        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

//        promise {

//            let! xmlParsed = getCustomXml customXmlParts context

//            let currentTableValidationOpt = getSwateValidationForCurrentTable newTableValidation.AnnotationTable.Name newTableValidation.AnnotationTable.Worksheet xmlParsed

//            let updatedTableValidation =
//                if currentTableValidationOpt.IsSome then
//                    let previousTableValidation = currentTableValidationOpt.Value
//                    {previousTableValidation with
//                        ColumnValidations = newTableValidation.ColumnValidations@previousTableValidation.ColumnValidations |> List.sortBy (fun x -> x.ColumnAdress)
//                        SwateVersion = newTableValidation.SwateVersion
//                        DateTime = newTableValidation.DateTime

//                    }
//                else
//                    newTableValidation

//            let nextCustomXml = updateSwateValidation updatedTableValidation xmlParsed

//            let nextCustomXmlString = nextCustomXml |> OfficeInterop.HelperFunctions.xmlElementToXmlString

//            let! deleteXml =
//                context.sync().``then``(fun e ->
//                    let items = customXmlParts.items
//                    let xmls = items |> Array.ofSeq |> Array.map (fun x -> x.delete() )
//                    xmls
//                )

//            let! reloadedCustomXml =
//                context.sync().``then``(fun e ->
//                    let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//                    workbook.customXmlParts
//                )

//            let! addNext =
//                context.sync().``then``(fun e ->
//                    reloadedCustomXml.add(nextCustomXmlString)
//                )

//            // This will be displayed in activity log
//            return
//                "Info",
//                sprintf
//                    "Update Validation Scheme with '%s - %s' @%s"
//                    newTableValidation.AnnotationTable.Worksheet
//                    newTableValidation.AnnotationTable.Name
//                    ( newTableValidation.DateTime.ToString("yyyy-MM-dd HH:mm") )
//        }
//    )


//let getAllValidationXmlParsed() =
//    Excel.run(fun context ->

//        promise {

//            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

//            let tables = context.workbook.tables.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
//            let _ = tables.load(propertyNames = U2.Case2 (ResizeArray [|"name";"worksheet"|]))

//            let! allTables = context.sync().``then``( fun _ ->

//                /// Get all names of all tables in the whole workbook.
//                let tableNames =
//                    tables.items
//                    |> Seq.toArray
//                    |> Array.map (fun x -> Shared.AnnotationTable.create x.name x.worksheet.name)

//                tableNames
//            )

//            let! xmlParsed = getCustomXml customXmlParts context

//            let tableValidations = getAllSwateTableValidation xmlParsed

//            return (tableValidations, allTables)
//        }
//    )

//let getActiveProtocolGroupXmlParsed() =
//    Excel.run(fun context ->

//        promise {

//            let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))
//            let! annotationTable = getActiveAnnotationTableName()

//            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

//            let! xmlParsed = getCustomXml customXmlParts context

//            let protocolGroup = getSwateProtocolGroupForCurrentTable annotationTable activeSheet.name xmlParsed

//            return protocolGroup

//        }
//    )

//let getAllProtocolGroupXmlParsed() =
//    Excel.run(fun context ->

//        promise {

//            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

//            let tables = context.workbook.tables.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
//            let _ = tables.load(propertyNames = U2.Case2 (ResizeArray [|"name";"worksheet"|]))

//            let! allTables = context.sync().``then``( fun _ ->

//                /// Get all names of all tables in the whole workbook.
//                let tableNames =
//                    tables.items
//                    |> Seq.toArray
//                    |> Array.map (fun x -> Shared.AnnotationTable.create x.name x.worksheet.name)

//                tableNames
//            )

//            let! xmlParsed = getCustomXml customXmlParts context

//            let protocolGroups = getAllSwateProtocolGroups xmlParsed

//            return (protocolGroups, allTables)
//        }
//    )


///// This function aims to update a protocol with a newer version from the db. To do this with minimum user friction we want the following:
///// Keep all already existing building blocks that still exist in the new version. By doing this we keep already filled in values.
///// Remove all building blocks that are not part of the new version.
///// Add all new building blocks.
//// Of couse this is best be done by using already existing functions. Therefore we try the following. Return information necessary to use:
//// Msg 'AddAnnotationBlocks' -> this will add all new blocks that are mentioned in 'minimalBuildingBlocks', add validationXml to existing and also add protocol xml.
//// 'Remove building block' functionality by passing the correct indices
//let updateProtocolByNewVersion (prot:OfficeInterop.Types.Xml.GroupTypes.Protocol, dbTemplate:Shared.ProtocolTemplate) =
//    Excel.run(fun context ->

//        promise {

//            let! annotationTable = getActiveAnnotationTableName()

//            // Ref. 2
//            let activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load(U2.Case1 "name")
//            let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

//            let! allBuildingBlocks =
//                context.sync().``then``( fun _ ->
//                    getBuildingBlocks annoHeaderRange annoBodyRange
//                )

//            /// Filter all annotation blocks for those that are part of existing protocol
//            let filterBuildingBlocksForProtocol =
//                allBuildingBlocks |> Array.filter (fun bb ->
//                    prot.SpannedBuildingBlocks
//                    |> List.exists (fun spannedBB ->
//                        spannedBB.ColumnName = bb.MainColumn.Header.Value.Header
//                    )
//                )

//            let minBuildingBlocksInfoDB = dbTemplate.TableXml |> MinimalBuildingBlock.ofExcelTableXml |> snd

//            let minimalBuildingBlocksToAdd =
//                minBuildingBlocksInfoDB
//                |> List.filter (fun minimalBB ->
//                    filterBuildingBlocksForProtocol
//                    |> Array.exists (fun bb -> minimalBB = MinimalBuildingBlock.ofBuildingBlockWithoutValues false bb)
//                    |> not
//                )

//            let buildingBlocksToRemove =
//                filterBuildingBlocksForProtocol
//                |> Array.filter (fun x ->
//                    minBuildingBlocksInfoDB
//                    |> List.exists (fun minimalBB -> minimalBB = MinimalBuildingBlock.ofBuildingBlockWithoutValues false x)
//                    |> not
//                )

//            let alreadyExistingBuildingBlocks =
//                filterBuildingBlocksForProtocol
//                |> Array.filter (fun bb ->
//                    buildingBlocksToRemove
//                    |> Array.contains bb
//                    |> not
//                )
//                |> Array.map (fun bb ->
//                     MinimalBuildingBlock.ofBuildingBlockWithoutValues true bb
//                     |> fun minBB -> {minBB with MainColumnName = bb.MainColumn.Header.Value.Header}
//                )
//                |> List.ofArray

//            let! remove =
//                removeAnnotationBlocks annotationTable buildingBlocksToRemove

//            let! reloadBuildingBlocks =
//                let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

//                let allBuildingBlocks =
//                    context.sync().``then``( fun _ ->
//                        let buildingBlocks = getBuildingBlocks annoHeaderRange annoBodyRange

//                        buildingBlocks
//                    )

//                allBuildingBlocks

//            let filterReloadedBuildingBlocksForProtocol =
//                reloadBuildingBlocks |> Array.filter (fun bb ->
//                    prot.SpannedBuildingBlocks |> List.exists (fun spannedBB -> spannedBB.ColumnName = bb.MainColumn.Header.Value.Header)
//                )

//            let table = activeWorksheet.tables.getItem(annotationTable)

//            //Auto select place to add new building blocks.
//            let! selectCorrectIndex = context.sync().``then``(fun e ->
//                let lastInd = filterReloadedBuildingBlocksForProtocol |> Array.map (fun bb -> bb.MainColumn.Index) |> Array.max |> float

//                table.getDataBodyRange().getColumn(lastInd).select()
//            )

//            let validationType =
//                dbTemplate.CustomXml
//                |> ValidationTypes.TableValidation.ofXml
//                |> Some

//            let protocol =
//                let id = dbTemplate.Name
//                let version = dbTemplate.Version
//                /// This could be outdated and needs to be updated during Msg-handling
//                let swateVersion = prot.SwateVersion
//                GroupTypes.Protocol.create id version swateVersion [] annotationTable activeWorksheet.name

//            /// Need to connect both again. 'alreadyExistingBuildingBlocks' is marked as already existing and is only passed to remain info about
//            let minimalBuildingBlockInfo =
//                minimalBuildingBlocksToAdd@alreadyExistingBuildingBlocks

//            return minimalBuildingBlockInfo, protocol, validationType
//        }
//    )


//let removeXmlType(xmlType:XmlTypes) =
//    Excel.run(fun context ->

//        promise {

//            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

//            let! xmlParsed = getCustomXml customXmlParts context

//            let nextCustomXml =
//                match xmlType with
//                | ValidationType tableValidation ->
//                    removeSwateValidation tableValidation xmlParsed
//                | GroupType protGroup ->
//                    updateRemoveSwateProtocolGroup protGroup xmlParsed true
//                | ProtocolType protocol ->
//                    updateRemoveSwateProtocol protocol xmlParsed true

//            let nextCustomXmlString = nextCustomXml |> OfficeInterop.HelperFunctions.xmlElementToXmlString

//            let! deleteXml =
//                context.sync().``then``(fun e ->
//                    let items = customXmlParts.items
//                    let xmls = items |> Array.ofSeq |> Array.map (fun x -> x.delete() )
//                    xmls
//                )

//            let! reloadedCustomXml =
//                context.sync().``then``(fun e ->
//                    let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//                    workbook.customXmlParts
//                )

//            let! addNext =
//                context.sync().``then``(fun e ->
//                    reloadedCustomXml.add(nextCustomXmlString)
//                )

//            return (sprintf "Removed %s" xmlType.toStringRdb)
//        }
//    )

//let updateAnnotationTableByXmlType(prevXmlType:XmlTypes, nextXmlType:XmlTypes) =

//    Excel.run(fun context ->

//        promise {

//            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

//            let! xmlParsed = getCustomXml customXmlParts context

//            let nextCustomXml =
//                match prevXmlType,nextXmlType with
//                | XmlTypes.ValidationType prevV, XmlTypes.ValidationType nextV ->
//                    replaceValidationByValidation prevV nextV xmlParsed
//                | XmlTypes.GroupType prevV, XmlTypes.GroupType nextV ->
//                    replaceProtGroupByProtGroup prevV nextV xmlParsed
//                | XmlTypes.ProtocolType prevV, XmlTypes.ProtocolType nextV ->
//                    failwith "Not coded yet"
//                | anyElse1, anyElse2 -> failwith "Swate encountered different XmlTypes while trying to reassign custom xml to new annotation table - worksheet combination."

//            let nextCustomXmlString = nextCustomXml |> OfficeInterop.HelperFunctions.xmlElementToXmlString

//            let! deleteXml =
//                context.sync().``then``(fun e ->
//                    let items = customXmlParts.items
//                    let xmls = items |> Array.ofSeq |> Array.map (fun x -> x.delete() )
//                    xmls
//                )

//            let! reloadedCustomXml =
//                context.sync().``then``(fun e ->
//                    let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//                    workbook.customXmlParts
//                )

//            let! addNext =
//                context.sync().``then``(fun e ->
//                    reloadedCustomXml.add(nextCustomXmlString)
//                )

//            return (sprintf "Updated %s BY %s" prevXmlType.toStringRdb nextXmlType.toStringRdb)
//        }
//    )
