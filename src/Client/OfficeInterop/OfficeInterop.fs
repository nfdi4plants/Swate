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



[<Emit("console.log($0)")>]
let consoleLog (message: string): unit = jsNative

open System

open Fable.Core.JsInterop

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

            let! xmlParsed = getCustomXml customXmlParts context

            //let tableValidations = getAllSwateTableValidation xmlParsed
            
            return (sprintf "%A"  allTables)
        }
    )

let swateSync (context:RequestContext) =
    context.sync().``then``(fun _ -> ())

/// <summary>Will return Some tableName if any annotationTable exists in a worksheet before the active one.</summary>
let getPrevAnnotationTable (context:RequestContext) =
    promise {
    
        let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
        let activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load(U2.Case1 "position")
        let tables = context.workbook.tables
        let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items";"worksheet";"name"; "position"; "values"|]))

        let! prevTable = context.sync().``then``(fun e ->
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
                |> Array.filter(fun (wp,tableName) -> activeWorksheetPosition - wp > 0.)
                |> Array.sortBy(fun (wp,tableName) ->
                    activeWorksheetPosition - wp
                )
                |> Array.tryHead
            Option.bind (snd >> Some) prevTable
        )

        return prevTable
    }

// I retrieve the index of the currently opened worksheet, here the new table should be created.
// I retrieve all annotationTables in the workbook. I filter out all annotationTables that are on a worksheet with a lower index than the index of the currently opened worksheet.
// I subtract from the index of the current worksheet the indices of the other found worksheets with annotationTable.
// I sort by the resulting lowest number (since the worksheet is then closest to the active one), I find the output column in the particular
// annotationTable and use the values it contains for the new annotationTable in the active worksheet.
let getPrevTableOutput (context:RequestContext) =
    promise {
        let! prevTableName = getPrevAnnotationTable context

        if prevTableName.IsSome then
            // Ref. 2
            let! buildingBlocks = BuildingBlockFunctions.getBuildingBlocks context prevTableName.Value

            let outputCol = buildingBlocks |> Array.tryFind (fun x -> x.MainColumn.Header.isOutputCol)

            let values =
                if outputCol.IsSome then
                    outputCol.Value.MainColumn.Cells
                else [||]

            return values
        else
            return [||]
    }

/// <summary>This function is used to create a new annotation table.
/// 'isDark' refers to the current styling of excel (darkmode, or not).</summary>
let private createAnnotationTableAtRange (isDark:bool, tryUseLastOutput:bool, range:Excel.Range, context: RequestContext) =
    
    // This function is used to create the "next" annotationTable name.
    // 'allTableNames' is passed from a previous function and contains a list of all annotationTables.
    // The function then tests if the freshly created name already exists and if it does it rec executes itself againn with (ind+1)
    // Due to how this function is written, the tables will not always count up. E.g. annotationTable2 gets deleted then the next table will not be
    // annotationTable3 or higher but annotationTable2 again. This could in the future lead to problems if information is saved with the table name as identifier.
    let rec findNewTableName allTableNames =
        let id = HumanReadableIds.tableName()
        let newTestName = $"annotationTable{id}"
        let existsAlready = allTableNames |> Array.exists (fun x -> x = newTestName)
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
    
    let tableRange = range
    let _ = tableRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount";"address"; "isEntireColumn"; "worksheet"])))

    let activeSheet = tableRange.worksheet
    let _ = activeSheet.load(U2.Case2 (ResizeArray[|"tables"|]))
    let activeTables = activeSheet.tables.load(propertyNames=U2.Case1 "items")
    
    let r = context.runtime.load(U2.Case1 "enableEvents")
    
    promise {
    
        // Is user input signals to try and find+reuse the output from the previous annotationTable do this, otherwise just return empty array
        let! prevTableOutput = if tryUseLastOutput then getPrevTableOutput context else promise {return Array.empty}
    
        // If try to use last output check if we found some output in "prevTableOutput" by checking if the array is not empty.
        let useExistingPrevOutput = tryUseLastOutput && Array.isEmpty >> not <| prevTableOutput
    
        let! allTableNames = getAllTableNames context
    
        // sync with proxy objects after loading values from excel
        let! table,newTableLogging = context.sync().``then``( fun _ ->
    
            // Filter all names of tables on the active worksheet for names starting with "annotationTable".
            let annoTables =
                activeTables.items
                |> Seq.toArray
                |> Array.map (fun x -> x.name)
                |> Array.filter (fun x -> x.StartsWith "annotationTable")
    
            // Fail the function if there are not exactly 0 annotation tables in the active worksheet.
            // This check is done, to only have one annotationTable per workSheet.
            let _ =
                match annoTables.Length with
                | x when x > 0 ->
                    failwith "The active worksheet contains more than zero annotationTables. Please move to a new worksheet."
                | 0 ->
                    ()
                | _ ->
                    failwith "The active worksheet contains a negative number of annotation tables. Obviously this cannot happen. Please report this as a bug to the developers."
    
            // Ref. 1
            r.enableEvents <- false
    
            // We do not want to create annotation tables of any size. The recommended workflow is to use the addBuildingBlock functionality.
            // Therefore we recreate the tableRange but with a columncount of 2. The 2 Basic columns in any annotation table.
            // "Source Name" | "Sample Name"
            let adaptedRange =
                let rowCount =
                    if useExistingPrevOutput then
                        (float prevTableOutput.Length + 1.)
                    elif tableRange.isEntireColumn then
                        21.
                    elif tableRange.rowCount <= 2. then
                        2.
                    else
                        tableRange.rowCount
                activeSheet.getRangeByIndexes(tableRange.rowIndex,tableRange.columnIndex,rowCount,2.)
    
            // Create table in current worksheet
            let annotationTable = activeSheet.tables.add(U2.Case1 adaptedRange,true)
    
            // Update annotationTable column headers
            (annotationTable.columns.getItemAt 0.).name <- "Source Name"
            (annotationTable.columns.getItemAt 1.).name <- "Sample Name"
    
            if useExistingPrevOutput then
                let newColValues = prevTableOutput |> Array.map (fun cell -> ResizeArray[|Option.bind (box >> Some) cell.Value|] ) |> ResizeArray
                let col1 = (annotationTable.columns.getItemAt 0.)
                let body = col1.getDataBodyRange()
                body.values <- newColValues
    
            // Create new annotationTable name
            let newName = findNewTableName allTableNames
            // Update annotationTable name
            annotationTable.name <- newName
    
            // Update annotationTable style
            annotationTable.style <- style
    
            // Fit widths and heights of cols and rows to value size. (In this case the new column headers).
            activeSheet.getUsedRange().format.autofitColumns()
            activeSheet.getUsedRange().format.autofitRows()
    
            // let annoTableName = allTableNames |> Array.filter (fun x -> x.StartsWith "annotationTable")
    
            r.enableEvents <- true
    
            // Return info message
            let logging = InteropLogging.Msg.create InteropLogging.Info (sprintf "Annotation Table created in [%s] with dimensions 2c x (%.0f + 1h)r." tableRange.address (tableRange.rowCount - 1.))

            annotationTable, logging
        )
    
        return (table,newTableLogging)
    }

/// <summary>This function is used to create a new annotation table.
/// 'isDark' refers to the current styling of excel (darkmode, or not).</summary>
let createAnnotationTable (isDark:bool, tryUseLastOutput:bool) =
    Excel.run (fun context ->
        let selectedRange = context.workbook.getSelectedRange()
        promise {
            let! newTableLogging = createAnnotationTableAtRange (isDark,tryUseLastOutput,selectedRange,context)

            // Interop logging expects list of logs
            return [snd newTableLogging] 
        }
    )

/// <summary>This function is used before most excel interop messages to get the active annotationTable.</summary>
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

/// <summary>This function is used to hide all reference columns and to fit rows and columns to their values.
/// The main goal is to improve readability of the table with this function.</summary>
let autoFitTable (hideRefCols:bool) (context:RequestContext) =
    promise {
        let! annotationTable = getActiveAnnotationTableName context

        // Ref. 2
        let sheet = context.workbook.worksheets.getActiveWorksheet()

        let annotationTable = sheet.tables.getItem(annotationTable)
        let allCols = annotationTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"|]))
    
        let annoHeaderRange = annotationTable.getHeaderRowRange()
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

/// <summary>This function is used to hide all reference columns and to fit rows and columns to their values.
/// The main goal is to improve readability of the table with this function.</summary>
let autoFitTableHide (context:RequestContext) =
    autoFitTable true context

// ExcelApi 1.2
let autoFitTableByTable (annotationTable:Table) (context:RequestContext) =

    let allCols = annotationTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"|]))
    
    let annoHeaderRange = annotationTable.getHeaderRowRange()
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
    
/// <summary>This is currently used to get information about the table for the table validation feature.</summary>
let getTableRepresentation() =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName(context)

            // Ref. 2
            let! buildingBlocks = BuildingBlockFunctions.getBuildingBlocks context annotationTable

            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

            let! xmlParsed = getCustomXml customXmlParts context
            let currentTableValidation = CustomXmlFunctions.Validation.getSwateValidationForCurrentTable annotationTable xmlParsed

            // This function updates the current SwateValidation xml with all found building blocks.
            let updateCurrentTableValidationXml =
                // We start by transforming all building blocks into ColumnValidations
                let existingBuildingBlocks = buildingBlocks |> Array.map (fun buildingBlock -> CustomXmlTypes.Validation.ColumnValidation.ofBuildingBlock buildingBlock)
                // Map over all newColumnValidations and see if they exist in the currentTableValidation xml. If they do, then update them by their validation parameters.
                let updateTableValidation =
                    // Check if a TableValidation for the active table AND worksheet exists, else return the newly build colValidations.
                    if currentTableValidation.IsSome then
                        let updatedNewColumnValidations =
                            existingBuildingBlocks
                            |> Array.map (fun newColVal ->
                                let tryFindCurrentColVal = currentTableValidation.Value.ColumnValidations |> List.tryFind (fun x -> x.ColumnHeader = newColVal.ColumnHeader)
                                if tryFindCurrentColVal.IsSome then
                                    {newColVal with
                                        Importance = tryFindCurrentColVal.Value.Importance
                                        ValidationFormat = tryFindCurrentColVal.Value.ValidationFormat
                                    }
                                else
                                    newColVal
                            )
                            |> List.ofArray
                        // Update TableValidation with updated ColumnValidations
                        {currentTableValidation.Value with
                            ColumnValidations = updatedNewColumnValidations}
                    else
                        // Should no current TableValidation xml exist, create a new one
                        CustomXmlTypes.Validation.TableValidation.create
                            ""
                            annotationTable
                            (System.DateTime.Now.ToUniversalTime())
                            []
                            (List.ofArray existingBuildingBlocks)
                updateTableValidation

            return updateCurrentTableValidationXml, buildingBlocks
        }
    )

let getBuildingBlocksAndSheet() =
    Excel.run(fun context ->
        promise {
            let! annotationTable = getActiveAnnotationTableName(context)
            
            // Ref. 2
            let! buildingBlocks = BuildingBlockFunctions.getBuildingBlocks context annotationTable

            let worksheet = context.workbook.worksheets.getActiveWorksheet()
            let _ = worksheet.load(U2.Case1 "name")

            let! name = context.sync().``then``(fun _ -> worksheet.name)
            return (name, buildingBlocks)
        }
    )

open BuildingBlockFunctions

let getBuildingBlocksAndSheets() =
    Excel.run(fun context ->
        promise {

            let _ = context.workbook.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
            let tables = context.workbook.tables
            let _ = tables.load(propertyNames=U2.Case2 (ResizeArray[|"items";"worksheet";"name"; "values"|]))

            let! worksheetAnnotationTableNames = context.sync().``then``(fun e ->
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
let rebaseIndexToTable (selectedRange:Excel.Range) (annoHeaderRange:Excel.Range) =
    let diff = selectedRange.columnIndex - annoHeaderRange.columnIndex |> int
    let vals = annoHeaderRange.columnCount |> int
    let maxLength = vals-1
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
            if x.MainColumn.Header.isMainColumn then
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

// ExcelApi 1.4
/// <summary>This function is used to add a new building block to the active annotationTable.</summary>
let addAnnotationBlock (newBB:InsertBuildingBlock) =
    Excel.run(fun context ->
        promise {

            let! annotationTableName = getActiveAnnotationTableName context
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTableName)

            // Ref. 2
            // This is necessary to place new columns next to selected col
            let annoHeaderRange = annotationTable.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"; "columnCount"; "rowIndex"|]))
            let tableRange = annotationTable.getRange()
            let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
            let selectedRange = context.workbook.getSelectedRange()
            let _ = selectedRange.load(U2.Case1 "columnIndex")

            let! nextIndex, headerVals = context.sync().``then``(fun e ->
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

                // This function checks if the would be col names already exist. If they do it ticks up the id tag to keep col names unique.
                // This function returns the id for the main column and related reference columns WHEN no unit is contained in the new building block
                let checkIdForRefCols() = OfficeInterop.Indexing.RefColumns.findNewIdForReferenceColumns allColHeaders newBB
                let checkIdForUnitCol() = OfficeInterop.Indexing.Unit.findNewIdForUnit allColHeaders

                let mainColName = newBB.ColumnHeader.toAnnotationTableHeader()
                let tsrColName() = OfficeInterop.Indexing.RefColumns.createTSRColName newBB (checkIdForRefCols())
                let tanColName() = OfficeInterop.Indexing.RefColumns.createTANColName newBB (checkIdForRefCols())
                let unitColName() = OfficeInterop.Indexing.Unit.createUnitColHeader (checkIdForUnitCol())

                let colNames = [|
                    mainColName
                    if newBB.UnitTerm.IsSome then
                        unitColName()
                    if not newBB.ColumnHeader.Type.isSingleColumn then
                        tsrColName()
                        tanColName()
                |]

                /// This logic will only work if there is only one format change
                let mutable formatChangedMsg : InteropLogging.Msg list = []

                let createAllCols =
                    let createCol index =
                        annotationTable.columns.add(
                            index   = index,
                            values  = U4.Case1 (col "")
                        )
                    colNames
                    |> Array.mapi (fun i colName ->
                        // create a single column
                        let col = createCol (nextIndex + float i)
                        // add column header name
                        col.name <- colName
                        let columnBody = col.getDataBodyRange()
                        // Fit column width to content
                        columnBody.format.autofitColumns()
                        // Update mainColumn body rows with number format IF building block has unit.
                        if newBB.UnitTerm.IsSome && colName = mainColName then
                            // create numberFormat for unit columns
                            let format = newBB.UnitTerm.Value.toNumberFormat
                            let formats = createValueMatrix 1 (rowCount-1) format
                            formatChangedMsg <- (InteropLogging.Msg.create InteropLogging.Info $"Added specified unit: {format}")::formatChangedMsg
                            columnBody.numberFormat <- formats
                        else
                            let format = createValueMatrix 1 (rowCount-1) "@"
                            columnBody.numberFormat <- format
                        // hide freshly created column if it is a reference column
                        if colName <> mainColName then
                            columnBody.columnHidden <- true
                        col
                    )

                mainColName, formatChangedMsg
            )

            let! fit = autoFitTableByTable annotationTable context

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
let replaceOutputColumn (annotationTableName:string) (existingOutputColumn: BuildingBlock) (newOutputcolumn: InsertBuildingBlock) =
    Excel.run(fun context ->
        promise {
            // Ref. 2
            // This is necessary to place new columns next to selected col
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTableName)
            let annoHeaderRange = annotationTable.getHeaderRowRange()
            let existingOutputColCell = annoHeaderRange.getCell(0., float existingOutputColumn.MainColumn.Index)
            let _ = existingOutputColCell.load(U2.Case2 (ResizeArray[|"values"|]))

            let newHeaderValues = ResizeArray[|ResizeArray [|newOutputcolumn.ColumnHeader.toAnnotationTableHeader() |> box |> Some|]|]
            let! change = context.sync().``then``(fun e ->
                existingOutputColCell.values <- newHeaderValues
                ()
            )

            let! fit = autoFitTableByTable annotationTable context
            let warningMsg = $"Found existing output column \"{existingOutputColumn.MainColumn.Header.SwateColumnHeader}\". Changed output column to \"{newOutputcolumn.ColumnHeader.toAnnotationTableHeader()}\"."

            let msg = InteropLogging.Msg.create InteropLogging.Warning warningMsg

            let loggingList = [
                msg
            ]

            return loggingList
        }
    )

/// Handle any diverging functionality here. This function is also used to make sure any new building blocks comply to the swate annotation-table definition.
let addAnnotationBlockHandler (newBB:InsertBuildingBlock) =
    Excel.run(fun context ->
        promise {

            let! annotationTableName = getActiveAnnotationTableName context
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTableName)

            let! existingBuildingBlocks = BuildingBlock.getFromContext(context,annotationTable)

            checkIfBuildingBlockExisting newBB existingBuildingBlocks

            // if newBB is output column and output column already exists in table this returns (Some outputcolumn-building-block), else None.
            let outputColOpt = checkHasExistingOutput newBB existingBuildingBlocks

            let! res = 
                match outputColOpt with
                | Some existingOutputColumn -> replaceOutputColumn annotationTableName existingOutputColumn newBB 
                | None -> addAnnotationBlock newBB

            return res
        } 
    )

let private createColumnBodyValues (insertBB:InsertBuildingBlock) (tableRowCount:int) =
    let createList (rowCount:int) (values:string []) =
        ResizeArray [|
            // tableRowCount-2 because -1 to match index-level and -1 to substract header from count
            for i in 0 .. tableRowCount-2 do
                yield ResizeArray [|
                    if i <= rowCount-1 then
                        box values.[i] |> Some
                    else
                        None
                |]
        |] 
    match insertBB.HasValues with
    | false -> [||]
    | true ->
        let rowCount = insertBB.Rows.Length
        if insertBB.ColumnHeader.Type.isSingleColumn then
            let values          = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.Name))
            [|values|]
        elif insertBB.HasUnit then
            let unitTermRowArr  = Array.init rowCount (fun _ -> insertBB.UnitTerm.Value) 
            let values          = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.Name))
            let unitTermNames   = createList rowCount (unitTermRowArr |> Array.map (fun tm -> tm.Name))
            let tsrs            = createList rowCount (unitTermRowArr |> Array.map (fun tm -> tm.accessionToTSR))
            let tans            = createList rowCount (unitTermRowArr |> Array.map (fun tm -> tm.accessionToTAN))
            [|values; unitTermNames; tsrs; tans|]
        else
            let termNames = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.Name))
            let tsrs      = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.accessionToTSR))
            let tans      = createList rowCount (insertBB.Rows |> Array.map (fun tm -> tm.accessionToTAN))
            [|termNames; tsrs; tans|]

let addAnnotationBlocksToTable (buildingBlocks:InsertBuildingBlock [], table:Table,context:RequestContext) =
    promise {
        
        let annotationTable = table
        let _ = annotationTable.load(U2.Case1 "name")

        let! existingBuildingBlocks = BuildingBlock.getFromContext(context,annotationTable) 

        /// newBuildingBlocks -> will be added
        /// alreadyExistingBBs -> will be used for logging
        let newBuildingBlocks, alreadyExistingBBs =
            let newSet = buildingBlocks |> Array.map (fun x -> x.ColumnHeader) |> Set.ofArray
            let prevSet = existingBuildingBlocks |> Array.choose (fun x -> x.MainColumn.Header.toBuildingBlockNamePrePrint )|> Set.ofArray
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
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"; "columnCount"; "rowIndex"|]))
        let tableRange = annotationTable.getRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
        let selectedRange = context.workbook.getSelectedRange()
        let _ = selectedRange.load(U2.Case1 "columnIndex")
        
        let! startIndex, headerVals = context.sync().``then``(fun e ->
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
        let! expandedTable,expandedRowCount =
            if expandByNRows.IsSome then
                promise {
                    let! expandedTable,expandedTableRange = context.sync().``then``(fun e ->
                        let newRowsValues = createMatrixForTables startColumnCount expandByNRows.Value ""
                        let newRows =
                            annotationTable.rows.add(
                                values = U4.Case1 newRowsValues
                            )
                        let newTable = context.workbook.tables.getItem(annotationTable.name)
                        let newTableRange = annotationTable.getRange()
                        let _ = newTableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
                        annotationTable,newTableRange
                    )
                    let! expandedRowCount = context.sync().``then``(fun _ -> int expandedTableRange.rowCount)
                    return (expandedTable, expandedRowCount)
                }
            else
                promise { return (annotationTable,tableRange.rowCount |> int) }
    
    
        //create an empty column to insert
        let col value = createMatrixForTables 1 expandedRowCount value
    
        let mutable nextIndex = startIndex
        let mutable allColumnHeaders = headerVals |> Array.choose id |> Array.map string |> List.ofArray
    
        let addBuildingBlock (buildingBlock:InsertBuildingBlock) (currentNextIndex:float) (columnHeaders:string []) =
            /// This function checks if the would be col names already exist. If they do it ticks up the id tag to keep col names unique.
            let checkIdForRefCols() = OfficeInterop.Indexing.RefColumns.findNewIdForReferenceColumns columnHeaders buildingBlock
            let checkIdForUnitCol() = OfficeInterop.Indexing.Unit.findNewIdForUnit columnHeaders
                
            let mainColName = buildingBlock.ColumnHeader.toAnnotationTableHeader()
            let tsrColName() = OfficeInterop.Indexing.RefColumns.createTSRColName buildingBlock (checkIdForRefCols())
            let tanColName() = OfficeInterop.Indexing.RefColumns.createTANColName buildingBlock (checkIdForRefCols())
            let unitColName() = OfficeInterop.Indexing.Unit.createUnitColHeader (checkIdForUnitCol())

            let colNames = [|
                mainColName
                if buildingBlock.UnitTerm.IsSome then
                    unitColName()
                if not buildingBlock.ColumnHeader.Type.isSingleColumn then
                    tsrColName()
                    tanColName()
            |]

            printfn "%A" colNames

            // Update storage for variables
            nextIndex <- currentNextIndex + float colNames.Length
            allColumnHeaders <- (colNames |> List.ofArray)@allColumnHeaders
                
            let createAllCols =
                let createCol index =
                    expandedTable.columns.add(
                        index   = index,
                        values  = U4.Case1 (col "")
                    )
                colNames
                |> Array.mapi (fun i colName ->
                    // create a single column
                    let col = createCol (currentNextIndex + float i)
                    // add column header name
                    col.name <- colName
                    let columnBody = col.getDataBodyRange()
                    // Fit column width to content
                    columnBody.format.autofitColumns()
                    // Update mainColumn body rows with number format IF building block has unit.
                    if buildingBlock.UnitTerm.IsSome && colName = mainColName then
                        // create numberFormat for unit columns
                        let format = buildingBlock.UnitTerm.Value.toNumberFormat
                        let formats = createValueMatrix 1 (expandedRowCount-1) format
                        columnBody.numberFormat <- formats
                    else
                        let format = $"General"
                        let formats = createValueMatrix 1 (expandedRowCount-1) format
                        columnBody.numberFormat <- formats
                    if buildingBlock.HasValues then
                        let values = createColumnBodyValues buildingBlock expandedRowCount
                        columnBody.values <- values.[i]
                    // hide freshly created column if it is a reference column
                    if colName <> mainColName then
                        columnBody.columnHidden <- true
                    col
                )
                
            colNames
    
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

let addAnnotationBlocks (buildingBlocks:InsertBuildingBlock []) =
    Excel.run(fun context ->

        promise {

            let! tryTable = tryFindActiveAnnotationTable()
            let sheet = context.workbook.worksheets.getActiveWorksheet()

            let! annotationTable, logging =
                match tryTable with
                | Success table ->
                    (
                        sheet.tables.getItem(table),
                        InteropLogging.Msg.create InteropLogging.Info "Found annotation table for template insert!"
                    )
                    |> JS.Constructors.Promise.resolve
                | Error e ->
                    let range =
                        // not sure if this try...with is necessary as on creating a new worksheet it will autoselect the A1 cell.
                        try
                            context.workbook.getSelectedRange()
                        with
                            | e -> sheet.getUsedRange()
                    createAnnotationTableAtRange(false,false,range,context)

            
            let! addBlocksLogging = addAnnotationBlocksToTable(buildingBlocks,annotationTable,context)

            return logging::addBlocksLogging
        } 
    )

let addAnnotationBlocksInNewSheet activateWorksheet (worksheetName:string,buildingBlocks:InsertBuildingBlock []) =
    Excel.run(fun context ->
        promise {

            let newWorksheet = context.workbook.worksheets.add(name=worksheetName)

            let worksheetRange = newWorksheet.getUsedRange()

            let! newTable,newTableLogging = createAnnotationTableAtRange (false,false,worksheetRange,context)

            let! addNewBuildingBlocksLogging = addAnnotationBlocksToTable(buildingBlocks, newTable, context)

            if activateWorksheet then newWorksheet.activate()

            let newSheetLogging = InteropLogging.Msg.create InteropLogging.Info $"Create new worksheet: {worksheetName}"

            return newSheetLogging::newTableLogging::addNewBuildingBlocksLogging
        }
    )

let addAnnotationBlocksInNewSheets (annotationTablesToAdd: (string*InsertBuildingBlock []) []) =
    // Use different context instances for individual worksheet creation.
    // Does not work with Excel.run at the beginning and passing the related context to subfunctions.
    annotationTablesToAdd
    |> Array.mapi (fun i x ->
        let acitvate = i = annotationTablesToAdd.Length-1
        addAnnotationBlocksInNewSheet acitvate x
            
    )
    |> Promise.all
    |> Promise.map List.concat

/// This function is used to add unit reference columns to an existing building block without unit reference columns
let updateUnitForCells (unitTerm:TermMinimal) =
    Excel.run(fun context ->

        promise {

            let! annotationTableName = getActiveAnnotationTableName context
            
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTableName)
            let _ = annotationTable.columns.load(propertyNames = U2.Case2 (ResizeArray(["items"])))

            let selectedRange = context.workbook.getSelectedRange()
            let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"rowIndex"; "rowCount";])))

            let annoHeaderRange = annotationTable.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))
            let tableRange = annotationTable.getRange()
            let _ = tableRange.load(U2.Case2 (ResizeArray(["rowCount"])))

            let! selectedBuildingBlock = OfficeInterop.BuildingBlockFunctions.findSelectedBuildingBlock context annotationTableName

            let! headerVals = context.sync().``then``(fun e ->
                // Get an array of the headers
                annoHeaderRange.values.[0] |> Array.ofSeq
            )

            // Check if building block has existing unit
            let! updateWithUnit =
                // is unit column already exists we want to update selected cells
                if selectedBuildingBlock.hasUnit then
                    context.sync().``then``(fun _ ->
                        
                        let format = unitTerm.toNumberFormat
                        let formats = createValueMatrix 1 (int selectedRange.rowCount) format
                        selectedRange.numberFormat <- formats
                        InteropLogging.Msg.create InteropLogging.Info $"Updated specified cells with unit: {format}."
                    )
                // if no unit column exists for the selected building block we want to add a unit column and update the whole main column with the unit.
                elif selectedBuildingBlock.hasCompleteTSRTAN && selectedBuildingBlock.hasUnit |> not then
                    context.sync().``then``(fun _ ->
                        // Create unit column
                        // Create unit column name
                        let allColHeaders =
                            headerVals
                            |> Array.choose id
                            |> Array.map string
                        let checkIdForUnitCol() = OfficeInterop.Indexing.Unit.findNewIdForUnit allColHeaders
                        let unitColId = checkIdForUnitCol()
                        let unitColName = OfficeInterop.Indexing.Unit.createUnitColHeader unitColId
                        // add column at correct index
                        let unitColumn =
                            annotationTable.columns.add(
                                index = float selectedBuildingBlock.MainColumn.Index + 1.
                            )
                        // Add column name
                        unitColumn.name <- unitColName
                        // Change number format for main column
                        // Get main column table body range
                        let mainCol = annotationTable.columns.items.[selectedBuildingBlock.MainColumn.Index].getDataBodyRange()
                        // Create unitTerm number format
                        let format = unitTerm.toNumberFormat
                        let formats = createValueMatrix 1 (int tableRange.rowCount - 1) format
                        mainCol.numberFormat <- formats
                        
                        InteropLogging.Msg.create InteropLogging.Info $"Created Unit Column {unitColName} for building block {selectedBuildingBlock.MainColumn.Header.SwateColumnHeader}."
                    )
                else
                    failwith $"You can only add unit to building blocks of the type: {BuildingBlockType.TermColumns}"
            let! _ = autoFitTable true context
            return [updateWithUnit]
        }

    )

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

        let! deleteCols =
            context.sync().``then``(fun e ->
                targetedColIndices |> Array.map (fun targetIndex ->
                    tableCols.items.[targetIndex].delete()
                )
            )

        return targetedColIndices
    }

//let removeAnnotationBlocks (tableName:string) (annotationBlocks:BuildingBlock [])  =
//    annotationBlocks
//    |> Array.sortByDescending (fun x -> x.MainColumn.Index)
//    |> Array.map (removeAnnotationBlock tableName)
//    |> Promise.all

let removeSelectedAnnotationBlock () =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName context

            let! selectedBuildingBlock = OfficeInterop.BuildingBlockFunctions.findSelectedBuildingBlock context annotationTable

            let! deleteCols = removeAnnotationBlock annotationTable selectedBuildingBlock context

            let resultMsg = InteropLogging.Msg.create InteropLogging.Info $"Delete Building Block {selectedBuildingBlock.MainColumn.Header.SwateColumnHeader} (Cols: {deleteCols})"  

            let! format = autoFitTableHide context

            return [resultMsg]
        }
    )

let getAnnotationBlockDetails() =
    Excel.run(fun context ->
        promise {

            let! annotationTable = getActiveAnnotationTableName context

            let! selectedBuildingBlock = OfficeInterop.BuildingBlockFunctions.findSelectedBuildingBlock context annotationTable

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
        | None ->
            buildingBlocks
    // Chain all deprecation checks
    buildingBlocks
    |> deprecated_DataFileName
    |> ignore
    // Only return msg list. Messages with InteropLogging.Warning will be pushed to user.
    msgList

let getAllAnnotationBlockDetails() =
    Excel.run(fun context ->
        promise {
            let! annotationTableName = getActiveAnnotationTableName context

            let! buildingBlocks = OfficeInterop.BuildingBlockFunctions.getBuildingBlocks context annotationTableName

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
                let! annotationTable = getActiveAnnotationTableName context
                // Ref. 2
                let sheet = context.workbook.worksheets.getActiveWorksheet()
                let annotationTable = sheet.tables.getItem(annotationTable)
                let tableRange = annotationTable.getRange()
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
let insertOntologyTerm (term:TermMinimal) =
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
            let! checkCorrectInsertInSwateTable =
                match tryTable with
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
                                match i, term.TermAccession = String.Empty with
                                | 0, true | 1, true ->
                                    Some ("user-specific" |> box)
                                | 0, false ->
                                    //add "Term Source REF"
                                    Some (term.accessionToTSR |> box)
                                | 1, false ->
                                    //add "Term Accession Number"
                                    Some ( term.accessionToTAN |> box )
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
            let! annotationTableName = getActiveAnnotationTableName context
            // Ref. 2
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTableName)
            // Ref. 2
            let tableBodyRange = annotationTable.getDataBodyRange()
            let _ = tableBodyRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore

            let tableHeaderRange = annotationTable.getHeaderRowRange()
            let _ = tableHeaderRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore

            // Ref. 1
            let r = context.runtime.load(U2.Case1 "enableEvents")

            let bodyTerms = terms |> getBodyRows
            let headerTerms = terms |> getHeaderRows

            let! buildingBlocks = OfficeInterop.BuildingBlockFunctions.getBuildingBlocks context annotationTableName

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
                                            let tan = Shared.URLs.termAccessionUrlOfAccessionStr t.Accession
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
        let _ = range.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))

        // Ref. 1
        let r = context.runtime.load(U2.Case1 "enableEvents")

        // sync with proxy objects after loading values from excel
        context.sync().``then``( fun _ ->

            if range.columnCount > 1. then failwith "Cannot insert Terms in more than one column at a time."

            r.enableEvents <- false

            // create new values for selected Range.
            let newVals = ResizeArray([
                // iterate over the rows of the selected range (there can only be one column as we fail if more are selected)
                for rowInd in 0 .. range.values.Count-1 do
                    let tmp =
                        // Iterate over col values (1).
                        range.values.[rowInd] |> Seq.map (
                            // Ignore prevValue as it will be replaced anyways.
                            fun prevValue ->
                                // This part is a design choice.
                                // Should the user select less cells than we have items in the 'fileNameList' then we only fill the selected cells.
                                // Should the user select more cells than we have items in the 'fileNameList' then we fill the leftover cells with none.
                                let fileName = if fileNameList.Length-1 < rowInd then None else List.item rowInd fileNameList |> box |> Some
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

            let! annotationTable = getActiveAnnotationTableName context
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTable)
            let _ =annotationTable.columns.load(propertyNames = U2.Case1 "count") |> ignore
            let _ =annotationTable.rows.load(propertyNames = U2.Case1 "count")    |> ignore
            let rowRange = annotationTable.getRange()
            let _ = rowRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore
            let headerRange = annotationTable.getHeaderRowRange()
            let _ = headerRange.load(U2.Case2 (ResizeArray(["address";"columnCount";"rowCount"]))) |> ignore

            let! res = context.sync().``then``(fun _ ->
                let colCount,rowCount = annotationTable.columns.count, annotationTable.rows.count
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
    
            let! getXml =
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.delete() )
    
                    xmls |> Array.ofSeq
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
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.getXml() )

                    xmls |> Array.ofSeq
                )

            let! xml =
                context.sync().``then``(fun e ->

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

            let! deleteXml =
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.delete() )
            
                    xmls |> Array.ofSeq
                )

            let! addNext =
                context.sync().``then``(fun e ->
                    customXmlParts.add(newXmlString)
                )

            return "Info", "Custom xml update successful" 
        }
    )

let writeTableValidationToXml(tableValidation:CustomXmlTypes.Validation.TableValidation,currentSwateVersion:string) =
    Excel.run(fun context ->

        // Update DateTime 
        let newTableValidation = {
            tableValidation with
                // This line is used to give freshly created TableValidations the current Swate Version
                SwateVersion = currentSwateVersion
                DateTime = System.DateTime.Now.ToUniversalTime()
            }

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
    
        promise {
    
            let! xmlParsed = getCustomXml customXmlParts context
    
            let nextCustomXml = CustomXmlFunctions.Validation.updateSwateValidation newTableValidation xmlParsed

            let nextCustomXmlString = nextCustomXml |> Fable.SimpleXml.Generator.ofXmlElement |> Fable.SimpleXml.Generator.serializeXml
                        
            let! deleteXml =
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.delete() )
    
                    xmls |> Array.ofSeq
                )
    
            let! addNext =
                context.sync().``then``(fun e ->
                    customXmlParts.add(nextCustomXmlString)
                )

            // This will be displayed in activity log
            return
                "Info",
                sprintf
                    "Update Validation Scheme with '%s' @%s"
                    newTableValidation.AnnotationTable
                    ( newTableValidation.DateTime.ToString("yyyy-MM-dd HH:mm") )
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
