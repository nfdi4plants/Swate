module OfficeInterop

open Fable.Core
open ExcelJS.Fable
open Excel
open GlobalBindings

open Shared
open OfficeInteropTypes
open TermTypes

open OfficeInterop
open OfficeInterop.HelperFunctions
open OfficeInteropTypes
open OfficeInteropTypes.BuildingBlockTypes

/// Reoccuring Comment Defitinitions

/// 'annotationTables'      -> For a workbook (NOT! worksheet) all tables must have unique names. Therefore not all our tables can be called 'annotationTable'.
///                             Instead we add numbers to keep them unique. 'annotationTables' references all of those tables.

/// 'active annotationTable' -> The annotationTable present on the active worksheet. This is not trivial to access an is most of the time passed to a function by
///                             running 'tryFindActiveAnnotationTable()' in another message before.

/// 'TSR'/'TAN'             -> Term Source Ref - column / Term Accession Number - column

/// 'Reference Columns'     -> Meant are the hidden columns including TSR, TAN and Unit columns

/// 'Main Column'           -> Non hidden column of a building block. Each building block only contains one main column

/// 'Id Tag'                -> Column headers in Excel must be unique. Therefore Swate adds #integer to headers.

/// 'Unit column'           -> This references the unit block of a building block. It is a optional addition and not every building block must contain it. 

/// REFERENCES (often used functions with the same comment)

/// Ref. 1      -> Deactivate all events to prevent any crossreactions during our functions.      

/// Ref. 2      -> The next part loads relevant information from the excel objects and allows us to access them after 'context.sync()'

/// Ref. 3      -> Indices from a SelectedRange will return them on a worksheet perspective. E.g. C3 wll have col index 2.
///                 Indices from a table.getRange()/table.getHeaderRowRange() will be from a table perspective.
///                 The first col will have index 0 no matter at which worksheet column it is placed.
///                 Therefore we need to recalculate indices when working with selected range on the table. This is done multiple times throughout the office interop functions.


[<Emit("console.log($0)")>]
let consoleLog (message: string): unit = jsNative
        //ranges.format.fill.color <- "red"
        //let ranges = context.workbook.getSelectedRanges()
        //let x = ranges.load(U2.Case1 "address")

open System

open Fable.Core.JsInterop

/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
let exampleExcelFunction1 () =
    Excel.run(fun context ->

        let selectedRange = context.workbook.getSelectedRange().load(U2.Case2 (ResizeArray [|"dataValidation"|]))

        let selectedRangeValidation = selectedRange.dataValidation.load(U2.Case2 (ResizeArray [|"rule"|]))

        OfficeInterop.TermCollectionFunctions.addUpdateSelectedCellToQueryParamHandler context

        //promise {
            
            //let! termNamerange = OfficeInterop.TermCollectionFunctions.getSwateTermCollectionNameCol context

            //let! addValidation = context.sync().``then``(fun _ ->

            //    // https://fable.io/docs/communicate/js-from-fable.html
            //    let t1 = createEmpty<ListDataValidation>
            //    t1.inCellDropDown <- true
            //    t1.source <- U2.Case1 "=SwateTermCollection!$D$2#"

            //    let t2 = createEmpty<DataValidationRule>
            //    t2.list <- Some t1

            //    //https://stackoverflow.com/questions/37881457/how-to-implement-data-validation-in-excel-using-office-js-api
            //    //https://docs.microsoft.com/de-de/javascript/api/excel/excel.datavalidation?view=excel-js-preview#rule
            //    selectedRangeValidation.rule <- t2

            //    $"{selectedRangeValidation.rule.list.Value.inCellDropDown},{selectedRangeValidation.rule.list.Value.source}"
            //)

            //let! mySync = context.sync().``then``(fun _ -> ())

            //return addValidation
        //}

    )

/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
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

            //let protocolGroups = getAllSwateProtocolGroups xmlParsed

            //let tableValidations = getAllSwateTableValidation xmlParsed
            
            return (sprintf "%A"  allTables)
        }
    )

let swateSync (context:RequestContext) =
    context.sync().``then``(fun _ -> ()) |> Promise.start

/// This function is used to create a new annotation table.
/// 'allTableNames' is a array of all currently existing annotationTables.
/// 'isDark' refers to the current styling of excel (darkmode, or not).
let createAnnotationTable (isDark:bool) =
    Excel.run(fun context ->

        /// This function is used to create the "next" annotationTable name.
        /// 'allTableNames' is passed from a previous function and contains a list of all annotationTables.
        /// The function then tests if the freshly created name already exists and if it does it rec executes itself againn with (ind+1)
        /// Due to how this function is written, the tables will not always count up. E.g. annotationTable2 gets deleted then the next table will not be
        /// annotationTable3 or higher but annotationTable2 again. This could in the future lead to problems if information is saved with the table name as identifier.
        let rec findNewTableName allTableNames ind =
            let newTestName =
                if ind = 0 then "annotationTable" else sprintf "annotationTable%i" ind
            let existsAlready = allTableNames |> Array.exists (fun x -> x = newTestName)
            if existsAlready then
                findNewTableName allTableNames (ind+1)
            else
                newTestName

        /// decide table style by input parameter
        let style =
            if isDark then
                "TableStyleMedium15"
            else
                "TableStyleMedium7"

        // The next part loads relevant information from the excel objects and allows us to access them after 'context.sync()'
        let activeSheet = context.workbook.worksheets.getActiveWorksheet()
        let _ = activeSheet.load(U2.Case2 (ResizeArray[|"tables"|]))
        let activeTables = activeSheet.tables.load(propertyNames=U2.Case1 "items")

        let tableRange = context.workbook.getSelectedRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount";"address"; "isEntireColumn"])))

        let r = context.runtime.load(U2.Case1 "enableEvents")

        promise {


            let! allTableNames = getAllTableNames context

            //sync with proxy objects after loading values from excel
            let! r = context.sync().``then``( fun _ ->

                /// Filter all names of tables on the active worksheet for names starting with "annotationTable".
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

                /// We do not want to create annotation tables of any size. The recommended workflow is to use the addBuildingBlock functionality.
                /// Therefore we recreate the tableRange but with a columncount of 2. The 2 Basic columns in any annotation table.
                /// "Source Name" | "Sample Name"
                let adaptedRange =
                    let rowCount = if tableRange.isEntireColumn then 21. else (if tableRange.rowCount <= 1. then 1. else tableRange.rowCount)
                    activeSheet.getRangeByIndexes(tableRange.rowIndex,tableRange.columnIndex,rowCount,2.)

                /// Create table in current worksheet
                let annotationTable = activeSheet.tables.add(U2.Case1 adaptedRange,true)

                /// Update annotationTable column headers
                (annotationTable.columns.getItemAt 0.).name <- "Source Name"
                (annotationTable.columns.getItemAt 1.).name <- "Sample Name"

                /// Create new annotationTable name
                let newName = findNewTableName allTableNames 0
                /// Update annotationTable name
                annotationTable.name <- newName

                /// Update annotationTable style
                annotationTable.style <- style

                /// Fit widths and heights of cols and rows to value size. (In this case the new column headers).
                activeSheet.getUsedRange().format.autofitColumns()
                activeSheet.getUsedRange().format.autofitRows()

                //let annoTableName = allTableNames |> Array.filter (fun x -> x.StartsWith "annotationTable")

                r.enableEvents <- true

                /// Return info message
                sprintf "Annotation Table created in [%s] with dimensions 2c x (%.0f + 1h)r." tableRange.address (tableRange.rowCount - 1.)
            )

            return r
        }
    )

/// This function is used before most excel interop messages to get the active annotationTable.
let tryFindActiveAnnotationTable() =
    Excel.run(fun context ->

        // Ref. 2

        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let t = sheet.load(U2.Case2 (ResizeArray[|"tables"|]))
        let tableItems = t.tables.load(propertyNames=U2.Case1 "items")

        context.sync()
            .``then``( fun _ ->
                /// access names of all tables in the active worksheet.
                let tables =
                    tableItems.items
                    |> Seq.toArray
                    |> Array.map (fun x -> x.name)
                /// filter all table names for tables starting with "annotationTable"
                let annoTables =
                    tables
                    |> Array.filter (fun x -> x.StartsWith "annotationTable")
                /// Get the correct error message if we have <> 1 annotation table. Only returns success and the table name if annoTables.Length = 1
                let res = TryFindAnnoTableResult.exactlyOneAnnotationTable annoTables

                // return result
                res
        )
    )

/// This function is used to hide all reference columns and to fit rows and columns to their values.
/// The main goal is to improve readability of the table with this function.
let autoFitTable (context:RequestContext) =
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
                    if (SwateColumnHeader.create col.name).isReference then
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

let autoFitTableByTable (annotationTable:Table) (context:RequestContext) =

    let allCols = annotationTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items"; "name"|]))
    
    let annoHeaderRange = annotationTable.getHeaderRowRange()
    let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))

    let r = context.runtime.load(U2.Case1 "enableEvents")

    context.sync().``then``(fun _ ->

        // Ref. 1
        r.enableEvents <- false
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

        r.enableEvents <- true

        // return message
        [InteropLogging.Msg.create InteropLogging.Info "Autoformat Table"]
    )
    
/// This is currently used to get information about the table for the table validation feature.
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

            /// This function updates the current SwateValidation xml with all found building blocks.
            let updateCurrentTableValidationXml =
                /// We start by transforming all building blocks into ColumnValidations
                let existingBuildingBlocks = buildingBlocks |> Array.map (fun buildingBlock -> CustomXmlTypes.Validation.ColumnValidation.ofBuildingBlock buildingBlock)
                /// Map over all newColumnValidations and see if they exist in the currentTableValidation xml. If they do, then update them by their validation parameters.
                let updateTableValidation =
                    /// Check if a TableValidation for the active table AND worksheet exists, else return the newly build colValidations.
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
                        /// Update TableValidation with updated ColumnValidations
                        {currentTableValidation.Value with
                            ColumnValidations = updatedNewColumnValidations}
                    else
                        /// Should no current TableValidation xml exist, create a new one
                        CustomXmlTypes.Validation.TableValidation.create
                            ""
                            annotationTable
                            (System.DateTime.Now.ToUniversalTime())
                            []
                            (List.ofArray existingBuildingBlocks)
                updateTableValidation

            return updateCurrentTableValidationXml, buildingBlocks, "Update table representation."
        }
    )


/// selected ranged returns indices always from a worksheet perspective but we need the related table index. This is calculated here.
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

open BuildingBlockFunctions

/// Check column type and term if combination already exists
let private checkIfBuildingBlockExisting (newBB:InsertBuildingBlock) (existingBuildingBlocks:BuildingBlock []) =
    let mainColumnPrints =
        existingBuildingBlocks
        |> Array.choose (fun x ->
            if x.MainColumn.Header.isMainColumn then
                x.MainColumn.Header.toBuildingBlockNamePrePrint
            else
                None
        )
    if mainColumnPrints |> Array.contains newBB.Column then failwith $"Swate table contains already building block \"{newBB.Column.toAnnotationTableHeader()}\" in worksheet."

/// Check column type and term if combination already exists
let private checkHasExistingOutput (newBB:InsertBuildingBlock) (existingBuildingBlocks:BuildingBlock []) =
    if newBB.Column.isOutputColumn then
        let existingOutputOpt =
            existingBuildingBlocks
            |> Array.tryFind (fun x ->
                if x.MainColumn.Header.isMainColumn then 
                    let pp = x.MainColumn.Header.toBuildingBlockNamePrePrint 
                    pp.IsSome && pp.Value.isOutputColumn
                else
                    false
            )
        if existingOutputOpt.IsSome then failwith $"Swate table contains already one output column \"{existingOutputOpt.Value.MainColumn.Header.SwateColumnHeader}\". Each Swate table can only contain exactly one output column type."

/// This function is used to add a new building block to the active annotationTable.
let addAnnotationBlock (newBB:InsertBuildingBlock) =
    Excel.run(fun context ->

        promise {

            let! annotationTableName = getActiveAnnotationTableName context
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTableName)
            let! existingBuildingBlocks = BuildingBlock.getFromContext(context,annotationTable)

            checkIfBuildingBlockExisting newBB existingBuildingBlocks
            checkHasExistingOutput newBB existingBuildingBlocks
            

            // Ref. 2
            // This is necessary to place new columns next to selected col
            let annoHeaderRange = annotationTable.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"; "columnCount"; "rowIndex"|]))
            let tableRange = annotationTable.getRange()
            let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
            let selectedRange = context.workbook.getSelectedRange()
            let _ = selectedRange.load(U2.Case1 "columnIndex")


            let! nextIndex, headerVals = context.sync().``then``(fun e ->
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

            let rowCount = tableRange.rowCount |> int

            //create an empty column to insert
            let col value = createMatrixForTables 1 rowCount value

            let! mainColName, formatChangedMsg = context.sync().``then``( fun _ ->

                let allColHeaders =
                    headerVals
                    |> Array.choose id
                    |> Array.map string

                /// This function checks if the would be col names already exist. If they do it ticks up the id tag to keep col names unique.
                /// This function returns the id for the main column and related reference columns WHEN no unit is contained in the new building block
                let checkIdForMainCol() = OfficeInterop.Indexing.Column.findNewIdForColumn allColHeaders newBB

                let checkIdForUnitCol() = OfficeInterop.Indexing.Unit.findNewIdForUnit allColHeaders

                let mainColId = checkIdForMainCol()
                let unitColId = checkIdForUnitCol()

                let mainColName = OfficeInterop.Indexing.Column.createMainColName newBB mainColId
                let tsrColName() = OfficeInterop.Indexing.Column.createTSRColName newBB mainColId
                let tanColName() = OfficeInterop.Indexing.Column.createTANColName newBB mainColId
                let unitColName() = OfficeInterop.Indexing.Unit.createUnitColHeader unitColId

                let colNames = [|
                    mainColName
                    if newBB.UnitTerm.IsSome then
                        unitColName()
                    if not newBB.Column.Type.isSingleColumn then
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
                        // hide freshly created column if it is a reference column
                        if colName <> mainColName then
                            columnBody.columnHidden <- true
                        col
                    )

                mainColName, formatChangedMsg
            )

            let! fit = autoFitTableByTable annotationTable context

            let createColsMsg = InteropLogging.Msg.create InteropLogging.Info $"{mainColName} was added." 

            let logging = [
                if not formatChangedMsg.IsEmpty then yield! formatChangedMsg
                createColsMsg
            ]

            return logging
        } 
    )

let addAnnotationBlocks (newBuildingBlocks:InsertBuildingBlock list) =
    Excel.run(fun context ->

        promise {
    
            let! annotationTableName = getActiveAnnotationTableName context
    
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTableName)

            let! existingBuildingBlocks = BuildingBlock.getFromContext(context,annotationTable) 

            let newBBs, alreadyExistingBBs =
                let newSet = newBuildingBlocks |> List.map (fun x -> x.Column) |> Set.ofList
                let prevSet = existingBuildingBlocks |> Array.choose (fun x -> x.MainColumn.Header.toBuildingBlockNamePrePrint )|> Set.ofArray
                let bbsToAdd = Set.difference newSet prevSet |> Set.toArray
                // These building blocks do not exist in table and will be added
                newBuildingBlocks |> List.filter (fun x -> bbsToAdd |> Array.contains x.Column) |> List.filter (fun x -> not x.Column.isOutputColumn && not x.Column.isInputColumn)
                ,
                // These building blocks exist in table and are part of building block list. Keep them to push them as info msg.
                Set.intersect newSet prevSet |> Set.toList

            printfn $"{newBBs.Length}"

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
    
            let rowCount = tableRange.rowCount |> int

            //create an empty column to insert
            let col value = createMatrixForTables 1 rowCount value

            let mutable nextIndex = startIndex
            let mutable allColumnHeaders = headerVals |> Array.choose id |> Array.map string |> List.ofArray

            let addBuildingBlock (bb:InsertBuildingBlock) (currentNextIndex:float) (columnHeaders:string []) =
            
                /// This function checks if the would be col names already exist. If they do it ticks up the id tag to keep col names unique.
                /// This function returns the id for the main column and related reference columns WHEN no unit is contained in the new building block
                let checkIdForMainCol() = OfficeInterop.Indexing.Column.findNewIdForColumn columnHeaders bb
            
                let checkIdForUnitCol() = OfficeInterop.Indexing.Unit.findNewIdForUnit columnHeaders
            
                let mainColId = checkIdForMainCol()
                let unitColId = checkIdForUnitCol()
            
                let mainColName = OfficeInterop.Indexing.Column.createMainColName bb mainColId
                let tsrColName() = OfficeInterop.Indexing.Column.createTSRColName bb mainColId
                let tanColName() = OfficeInterop.Indexing.Column.createTANColName bb mainColId
                let unitColName() = OfficeInterop.Indexing.Unit.createUnitColHeader unitColId

                let colNames = [|
                    mainColName
                    if bb.UnitTerm.IsSome then OfficeInterop.Indexing.Unit.createUnitColHeader unitColId
                    if not bb.Column.Type.isSingleColumn then
                        tsrColName()
                        tanColName()
                |]
        
                /// Update storage for variables
                nextIndex <- currentNextIndex + float colNames.Length
                let updatedHeaderList =
                    if bb.UnitTerm.IsSome then
                        unitColName()::mainColName::tsrColName()::tanColName()::allColumnHeaders
                    else
                        mainColName::tsrColName()::tanColName()::allColumnHeaders 
                allColumnHeaders <-  updatedHeaderList
            
                let createAllCols =
                    let createCol index =
                        annotationTable.columns.add(
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
                        if bb.UnitTerm.IsSome && colName = mainColName then
                            // create numberFormat for unit columns
                            let format = bb.UnitTerm.Value.toNumberFormat
                            let formats = createValueMatrix 1 (rowCount-1) format
                            columnBody.numberFormat <- formats
                        // hide freshly created column if it is a reference column
                        if colName <> mainColName then
                            columnBody.columnHidden <- true
                        col
                    )
            
                colNames

            let! addBuildingBlocks = 
                context.sync().``then``(fun _ ->
                    newBBs
                    |> List.collect (fun bb ->
                        let colHeadersArr = allColumnHeaders |> Array.ofList
                        let addedBlockName = addBuildingBlock bb nextIndex colHeadersArr
                        addedBlockName |> List.ofArray
                    )
                )

            let! fit = autoFitTableByTable annotationTable context

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
    )

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
                /// Get an array of the headers
                annoHeaderRange.values.[0] |> Array.ofSeq
            )

            /// Check if building block has existing unit
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
                        /// Create unit column name
                        let allColHeaders =
                            headerVals
                            |> Array.choose id
                            |> Array.map string
                        let checkIdForUnitCol() = OfficeInterop.Indexing.Unit.findNewIdForUnit allColHeaders
                        let unitColId = checkIdForUnitCol()
                        let unitColName = OfficeInterop.Indexing.Unit.createUnitColHeader unitColId
                        /// add column at correct index
                        let unitColumn =
                            annotationTable.columns.add(
                                index = float selectedBuildingBlock.MainColumn.Index + 1.
                            )
                        /// Add column name
                        unitColumn.name <- unitColName
                        // Change number format for main column
                        /// Get main column table body range
                        let mainCol = annotationTable.columns.items.[selectedBuildingBlock.MainColumn.Index].getDataBodyRange()
                        // Create unitTerm number format
                        let format = unitTerm.toNumberFormat
                        let formats = createValueMatrix 1 (int tableRange.rowCount - 1) format
                        mainCol.numberFormat <- formats
                        InteropLogging.Msg.create InteropLogging.Info $"Created Unit Column {unitColName} for building block {selectedBuildingBlock.MainColumn.Header.SwateColumnHeader}."
                    )
                else
                    failwith $"You can only add unit to building blocks of the type: {BuildingBlockType.Parameter}, {BuildingBlockType.Characteristics}, {BuildingBlockType.Factor}"

            return [updateWithUnit]
        }
    )

//let addAnnotationBlocksAsProtocol (buildingBlockInfoList:MinimalBuildingBlock list, protocol:Xml.GroupTypes.Protocol) =
  
//    let chainBuildingBlocks (buildingBlockInfoList:MinimalBuildingBlock list) =
//        let state bb = promise {
//            let! baseAsync = bb |> addAnnotationBlock
//            return [baseAsync]
//        }
//        /// include isEmpty check to avoid errors during protocol update without any building blocks to add
//        if buildingBlockInfoList.IsEmpty then
//            promise {return []}
//        else
//            buildingBlockInfoList.Tail
//            |> List.fold (fun (previousPromise:JS.Promise<(string*string*string) list>) nextID ->
//                promise {
//                    let! prev,nextPromise =
//                        previousPromise.``then``(fun e ->
//                            e,state nextID
//                        )
//                    let! next = nextPromise
//                    return next@prev
//                }            
//            ) (state buildingBlockInfoList.Head)

//    let infoProm =

//        Excel.run(fun context ->
//            promise {

//                let! annotationTable = getActiveAnnotationTableName()

//                let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//                let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
//                let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))

//                let annoHeaderRange,annoBodyRange = getBuildingBlocksPreSync context annotationTable

//                let! xmlParsed = getCustomXml customXmlParts context
                
//                let currentProtocolGroup = getSwateProtocolGroupForCurrentTable annotationTable activeSheet.name xmlParsed

//                let! buildingBlocks = context.sync().``then``( fun e -> getBuildingBlocks annoHeaderRange annoBodyRange )

//                if currentProtocolGroup.IsSome then
//                    let existsAlready =
//                        currentProtocolGroup.Value.Protocols
//                        |> List.tryFind ( fun existingProtocol ->
//                            if buildingBlockInfoList |> List.exists (fun x -> x.IsAlreadyExisting = true) then
//                                existingProtocol.Id = protocol.Id && existingProtocol.ProtocolVersion = protocol.ProtocolVersion
//                            else
//                                false
//                        )
//                    let isComplete =
//                        if existsAlready.IsSome then
//                            (tryFindSpannedBuildingBlocks existsAlready.Value buildingBlocks).IsSome
//                        else
//                            true
//                    if existsAlready.IsSome then
//                        if isComplete then
//                            failwith ( sprintf "Protocol %s exists already in %s - %s." existsAlready.Value.Id currentProtocolGroup.Value.AnnotationTable.Name currentProtocolGroup.Value.AnnotationTable.Worksheet)

//                /// filter out building blocks that are only passed to keep the colNames
//                let onlyNonExistingBuildingBlocks = buildingBlockInfoList |> List.filter (fun x -> x.IsAlreadyExisting <> true)
//                let alreadyExistingBlocks =
//                    buildingBlockInfoList
//                    |> List.filter (fun x -> x.IsAlreadyExisting = true)
//                    |> List.map (fun x ->
//                        x.MainColumnName, "0.00", ""
//                    )

//                let! chainProm = chainBuildingBlocks onlyNonExistingBuildingBlocks

//                let updateProtocol = {protocol with AnnotationTable = AnnotationTable.create annotationTable activeSheet.name}

//                return (chainProm@alreadyExistingBlocks,updateProtocol)
//            }
//        )

//    promise {
//        let! blockResults,info = infoProm
//        let createSpannedBlocks =
//            [
//                for ind in 0 .. blockResults.Length-1 do
//                    let colName                     = blockResults |> List.item ind |> (fun (x,_,_) -> x)
//                    let relatedTermAccession        =
//                        buildingBlockInfoList
//                        |> List.tryFind ( fun x -> colName.Contains(x.MainColumnName) )
//                        |> fun x ->
//                            if x.IsNone then
//                                failwith (
//                                    sprintf
//                                        "Could not find created building block information %s in given list: %A"
//                                        colName
//                                        (buildingBlockInfoList|> List.map (fun y -> y.MainColumnName))
//                                )
//                            else
//                                if x.IsSome && x.Value.MainColumnTermAccession.IsSome then x.Value.MainColumnTermAccession.Value else ""
//                    yield
//                        Xml.GroupTypes.SpannedBuildingBlock.create colName relatedTermAccession
//            ]
//        let completeProtocolInfo = {info with SpannedBuildingBlocks = createSpannedBlocks}
//        return (blockResults,completeProtocolInfo)
//    }

/// This function removes a given building block from a given annotation table.
/// It returns the affected column indices.
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

            let! format = autoFitTable context

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

let getAllAnnotationBlockDetails() =
    Excel.run(fun context ->

        promise {

            let! annotationTableName = getActiveAnnotationTableName context

            let! buildingBlocks = OfficeInterop.BuildingBlockFunctions.getBuildingBlocks context annotationTableName

            let searchTerms = buildingBlocks |> Array.collect OfficeInterop.BuildingBlockFunctions.toTermSearchable 

            return searchTerms
        }
    )

//let changeTableColumnFormat (colName:string) (format:string) =
//    Excel.run(fun context ->
        
//        promise {

//            let! annotationTable = getActiveAnnotationTableName()

//            // Ref. 2 
//            let sheet = context.workbook.worksheets.getActiveWorksheet()
//            let annotationTable = sheet.tables.getItem(annotationTable)
            
//            // get ranged of main column that was previously created
//            let colBodyRange = (annotationTable.columns.getItem (U2.Case2 colName)).getDataBodyRange()
//            let _ = colBodyRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
            
            
//            // Ref. 1
//            let r = context.runtime.load(U2.Case1 "enableEvents")

//            let! res = context.sync().``then``( fun _ ->
                
//                r.enableEvents <- false
                
//                let rowCount = colBodyRange.rowCount |> int
//                // create a format column to insert
//                let formats = createValueMatrix 1 rowCount format
                
//                // add unit format to previously created main column
//                colBodyRange.numberFormat <- formats
                
//                r.enableEvents <- true
                
//                // return msg
//                "Info",sprintf "Format of %s was changed to %s" colName format
//            )

//            return res
//        }        
//    )

//let changeTableColumnsFormat (colAndFormatList:(string*string) list) =
//    Excel.run(fun context ->

//        promise {

//            let! annotationTable = getActiveAnnotationTableName()

//            let colNames = colAndFormatList |> List.map fst
//            let formats = colAndFormatList |> List.map snd
//            // Ref. 2 
//            let sheet = context.workbook.worksheets.getActiveWorksheet()
//            let annotationTable = sheet.tables.getItem(annotationTable)

//            // get ranged of main column that was previously created
//            let colBodyRanges =
//                colNames
//                |> List.map (fun colName ->
//                    let colBodyRange = (annotationTable.columns.getItem (U2.Case2 colName)).getDataBodyRange()
//                    let _ = colBodyRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
//                    colBodyRange
//                )

//            // Ref. 1
//            let r = context.runtime.load(U2.Case1 "enableEvents")

//            let! res = context.sync().``then``( fun _ ->

//                 r.enableEvents <- false

//                 let formatCols() =
//                    List.map2 (fun (colBodyRange:Excel.Range) format ->
//                        let rowCount = colBodyRange.rowCount |> int
//                        let formats = createValueMatrix 1 rowCount format
//                        colBodyRange.numberFormat <- formats
//                    ) colBodyRanges formats

//                 // create a format column to insert

//                 // add unit format to previously created main column

//                 let _ = formatCols()

//                 r.enableEvents <- true

//                 // return msg
//                 "Info",sprintf "Columns were updated with related format: %A" colAndFormatList
//            )

//            return res
//        }
//    )

// Reform this to onSelectionChanged (Even though we now know how to add eventHandlers we do not know how to pass info from handler to Swate app).
/// This function will parse the header of a selected column to check for a parent ontology, which will then be used for a isA-directed term search.
/// Any found parent ontology will also be displayed in a static field before the term search input field.
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
                    /// recalculate the selected col index from a worksheet perspective to the table perspective.
                    let newColIndex =
                        let tableRangeColIndex = tableRange.columnIndex
                        let selectColIndex = range.columnIndex
                        selectColIndex - tableRangeColIndex |> int

                    let newRowIndex =
                        let tableRangeRowIndex = tableRange.rowIndex
                        let selectedRowIndex = range.rowIndex
                        selectedRowIndex - tableRangeRowIndex |> int

                    /// Get all values from the table range
                    let colHeaderVals = tableRange.values.[0]
                    let rowVals = tableRange.values
                    /// Get the index of the last column in the table
                    let lastColInd = colHeaderVals.Count-1
                    /// Get the index of the last row in the table
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
                            // is selected range is in table then take header value from selected column
                            let header = tableRange.values.[0].[newColIndex]
                            let parsedHeader = SwateColumnHeader.create (string header.Value)
                            /// as the reference columns also contain a accession tag we want to return the first reference column header
                            /// instead of the main column header, if the main column header does include an ontology
                            if not parsedHeader.isSingleCol || not parsedHeader.isTANCol || not parsedHeader.isTSRCol || not parsedHeader.isUnitCol then
                                if parsedHeader.tryGetOntologyTerm.IsSome then
                                    let termName = parsedHeader.tryGetOntologyTerm.Value
                                    let termAccession =
                                        let headerIndexPlus1 = SwateColumnHeader.create ( Option.defaultValue (box "") tableRange.values.[0].[newColIndex+1] |> string )
                                        let headerIndexPlus2 = SwateColumnHeader.create ( Option.defaultValue (box "") tableRange.values.[0].[newColIndex+2] |> string )
                                        if not headerIndexPlus1.isUnitCol && headerIndexPlus1.isTSRCol then
                                            headerIndexPlus1.tryGetTermAccession
                                        elif headerIndexPlus1.isUnitCol && headerIndexPlus2.isTSRCol then
                                            headerIndexPlus2.tryGetTermAccession
                                        else
                                            None

                                    let parentTerm = TermMinimal.create termName (Option.defaultValue "" termAccession) |> Some
                                    parentTerm
                                else
                                    None
                            else
                                None
                    // return parent term of selected col
                    value
                )
                return res
            with
                | exn -> return None
        }
    )

/// This is used to insert terms into selected cells.
/// 'term' is the value that will be written into the main column.
/// 'termBackground' needs to be spearate from 'term' in case the user uses the fill function for a custom term.
/// Should the user write a real term with this function 'termBackground'.isSome and can be used to fill TSR and TAN.
let insertOntologyTerm (term:TermMinimal) =
    Excel.run(fun context ->

        // Ref. 2
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))
        /// This is for TSR and TAN
        let nextColsRange = range.getColumnsAfter 2.
        let _ = nextColsRange.load(U2.Case2 (ResizeArray(["values";"columnIndex";"columnCount"])))

        // Ref. 1
        let r = context.runtime.load(U2.Case1 "enableEvents")

        promise {

            //sync with proxy objects after loading values from excel
            let! res = context.sync().``then``( fun _ ->

                /// failwith if the number of selected columns is > 1. This is done due to hidden columns
                /// and an overlapping reaction as we add values to the columns next to the selected one
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

            return res
        }
    )

/// This is used to create a full representation of all building blocks in the table. This representation is then split into unit building blocks and regular building blocks.
/// These are then filtered for search terms and aggregated into an 'SearchTermI []', which is used to search the database for missing values.
/// 'annotationTable'' gets passed by 'tryFindActiveAnnotationTable'.
//let createSearchTermsIFromTable () =
//    Excel.run(fun context ->

//        promise {

//            let! annotationTable = getActiveAnnotationTableName()

//            // Ref. 2
//            let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

//            let! res= context.sync().``then``( fun _ ->
                
//                /// Sort all columns into building blocks.
//                let buildingBlocks =
//                    getBuildingBlocks annoHeaderRange annoBodyRange

//                /// Filter for only building blocks with ontology (indicated by having a TSR and TAN).
//                let buildingBlocksWithOntology =
//                    buildingBlocks |> Array.filter (fun x -> x.TSR.IsSome && x.TAN.IsSome)

//                /// Combine search types
//                let allSearches = sortBuildingBlocksValuesToSearchTerm buildingBlocksWithOntology

//                /// Return the name of the table and all search types
//                annotationTable,allSearches
//            )
//            return res
//        }

//    )

/// This function will be executed after the SearchTerm types from 'createSearchTermsFromTable' where send to the server to search the database for them.
/// Here the results will be written into the table by the stored col and row indices.
let UpdateTableByTermsSearchable (terms:TermSearchable []) =
    Excel.run(fun context ->

        /// This will create a single cell value arr
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
                                let opt = relBuildingBlock.MainColumn.Header.tryGetMainColumnHeaderId
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
                                /// if building block has unit, then tsr and tan are one index further to the right
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
                            /// This is hit when free text input is entered as building block
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

                    /// Insert table body terms into related cells for all stored row-/ col-indices
                    let numberOfUpdatedBodyTerms =
                        bodyTerms
                        // iterate over all found terms
                        |> Array.map (
                            fun term ->
                                let t,tsr,tan=
                                    if term.SearchResultTerm.IsSome then
                                        /// Term search result from database
                                        let t = term.SearchResultTerm.Value
                                        /// Get ontology and accession from Term.Accession
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
                                    /// ColIndex saves the column index of the main column. In case of a unit term the term gets inserted at maincolumn index + 1.
                                    /// TSR and TAN are also shifted to the right by 1.
                                    let termNameIndex = if term.IsUnit then float term.ColIndex + 1. else float term.ColIndex
                                    /// Terms are saved based on rowIndex for the whole table. As the following works on the TableBodyRange we need to reduce the indices by 1.
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

/// This function is used to insert file names into the selected range.
let insertFileNamesFromFilePicker (fileNameList:string list) =
    Excel.run(fun context ->

        // Ref. 2
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))

        // Ref. 1
        let r = context.runtime.load(U2.Case1 "enableEvents")

        //sync with proxy objects after loading values from excel
        context.sync().``then``( fun _ ->

            if range.columnCount > 1. then failwith "Cannot insert Terms in more than one column at a time."

            r.enableEvents <- false

            /// create new values for selected Range.
            let newVals = ResizeArray([
                // iterate over the rows of the selected range (there can only be one column as we fail if more are selected)
                for rowInd in 0 .. range.values.Count-1 do
                    let tmp =
                        // Iterate over col values (1).
                        range.values.[rowInd] |> Seq.map (
                            // Ignore prevValue as it will be replaced anyways.
                            fun prevValue ->
                                /// This part is a design choice.
                                /// Should the user select less cells than we have items in the 'fileNameList' then we only fill the selected cells.
                                /// Should the user select more cells than we have items in the 'fileNameList' then we fill the leftover cells with none.
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
            sprintf "%A, %A" range.values.Count newVals
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

            //let! xmlParsed, currentSwateValidationXml = getCurrentValidationXml customXmlParts context

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

            return "Info",sprintf "%A" xml
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

//let writeProtocolToXml(protocol:GroupTypes.Protocol) =
//    updateProtocolFromXml protocol false

//let removeProtocolFromXml(protocol:GroupTypes.Protocol) =
//    updateProtocolFromXml protocol true

///// This function ist used to parse the protocol xml and the table to building blocks and assign the correct
///// table protocol-group-header to each building block.
//let updateProtocolGroupHeader () =
//    Excel.run(fun context ->

//        promise {
//            let! annotationTable = getActiveAnnotationTableName()
//            let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))
//            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
//            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
   

//            let annoHeaderRange,annoBodyRange = getBuildingBlocksPreSync context annotationTable
//            let _ = annoHeaderRange.load(U2.Case1 "rowIndex")
//            let groupHeader = annoHeaderRange.getRowsAbove(1.)

//            let! xmlParsed = getCustomXml customXmlParts context
//            let! buildingBlocks = context.sync().``then``( fun e -> getBuildingBlocks annoHeaderRange annoBodyRange )
//            let currentProtocolGroup = getSwateProtocolGroupForCurrentTable annotationTable activeSheet.name xmlParsed

//            let! applyGroups = promise {

//                let protocolsForCurrentTableSheet =
//                    if currentProtocolGroup.IsSome then
//                        currentProtocolGroup.Value.Protocols
//                    else
//                        []

//                let getGroupHeaderIndicesForProtocol (buildingBlocks:BuildingBlock []) (protocol:Xml.GroupTypes.Protocol) =
//                    let buildingBlockOpts = tryFindSpannedBuildingBlocks protocol buildingBlocks
//                    // caluclate list of indices for group blocks
//                    if buildingBlockOpts.IsSome then
//                        let getStartAndEnd (mainColIndices:int list) =
//                            let startInd = List.min mainColIndices
//                            let endInd   =
//                                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
//                                let max = List.max mainColIndices |> float
//                                findIndexNextNotHiddenCol headerVals max |> int
//                            startInd,endInd-1
//                        let bbColNumberAndIndices =
//                            buildingBlockOpts.Value
//                            |> List.map (fun bb ->
//                                let nOfCols =
//                                    if bb.hasCompleteTSRTAN |> not then
//                                        1
//                                    elif bb.hasCompleteTSRTAN && bb.hasCompleteUnitBlock |> not then
//                                        3
//                                    elif bb.hasCompleteTSRTAN && bb.hasCompleteUnitBlock then
//                                        6
//                                    else failwith (sprintf "Swate encountered an unknown column pattern for building block: %s " bb.MainColumn.Header.Value.Header) 
//                                bb.MainColumn.Index, nOfCols
//                            )
//                            |> List.sortBy fst
//                        let rec sortIntoBlocks (iteration:int) (currentGroupIterator:int) (bbColNumberAndIndices:(int*int) list) (collector:(int*int*int) list) =
//                            if iteration >= bbColNumberAndIndices.Length then
//                                collector
//                            elif iteration = 0 then
//                                let currentInd, currentN = List.item iteration bbColNumberAndIndices
//                                sortIntoBlocks 1 currentGroupIterator bbColNumberAndIndices ((currentGroupIterator,currentInd,currentN)::collector)
//                            else
//                                let currentColIndex, currentNOfCols = List.item iteration bbColNumberAndIndices
//                                let lastGroup, lastColIndex, lastNOfCols =
//                                    collector
//                                    |> List.sortBy (fun (group,colIndex,nOfCols) -> colIndex)
//                                    |> List.last
//                                /// - 1 as the lastColIndex is already the mainColumn
//                                let lastBBEndInd = lastColIndex + lastNOfCols
//                                if lastBBEndInd = currentColIndex then
//                                    sortIntoBlocks (iteration+1) currentGroupIterator bbColNumberAndIndices ((currentGroupIterator,currentColIndex,currentNOfCols)::collector)
//                                elif lastBBEndInd > currentColIndex then
//                                    failwith (sprintf "Swate encountered an unknown building block pattern (Error: SIB%i-%i)" lastBBEndInd currentColIndex)
//                                else 
//                                    let newGroupIterator = currentGroupIterator + 1 
//                                    sortIntoBlocks (iteration+1) newGroupIterator bbColNumberAndIndices ((newGroupIterator,currentColIndex,currentNOfCols)::collector)
//                        let mainColIndiceBlocks =
//                            sortIntoBlocks 0 0 bbColNumberAndIndices []
//                            |> List.groupBy (fun (group,colInd,nOfCols) -> group)
//                            |> List.map (fun x ->
//                                snd x |> List.map (fun (group,colInd,nOfCols) -> colInd)
//                            )
//                            |> List.map getStartAndEnd
//                            |> List.sortByDescending fst
//                        Some mainColIndiceBlocks
//                    else
//                        None

//                /// 'tableStartIndex' -> let tableRange = annotationTable.getRange().columnIndex
//                /// 'rangeStartIndex' -> range.columnIndex
//                let recalculateColIndex tableStartIndex rangeStartIndex =
//                    let tableRangeColIndex = tableStartIndex
//                    let selectColIndex = rangeStartIndex
//                    selectColIndex + tableRangeColIndex

//                let recalculateColIndexToTable rangeStartIndex =
//                    recalculateColIndex annoHeaderRange.columnIndex rangeStartIndex

//                let! cleanGroupHeaderFormat = cleanGroupHeaderFormat groupHeader context

//                let! group =
//                    protocolsForCurrentTableSheet
//                    |> List.map (fun protocol ->
//                        let startEndIndices = getGroupHeaderIndicesForProtocol buildingBlocks protocol
//                        promise {

//                            if startEndIndices.IsSome then

//                                let! startDiffList =
//                                    startEndIndices.Value
//                                    |> List.sortByDescending fst
//                                    |> List.map (fun (startInd,endInd ) ->
//                                        promise {

//                                            let startInd,endInd = startInd |> float |> recalculateColIndexToTable, endInd |> float |> recalculateColIndexToTable
//                                            let! diff =
//                                                context.sync().``then``(fun e ->
//                                                    /// +1 to also include the endcolumn, without it would end too early.
//                                                    let diff = (endInd - startInd) + 1.
//                                                    let getProtocolGroupHeaderRange = activeSheet.getRangeByIndexes(annoHeaderRange.rowIndex-1., startInd, 1., diff)
//                                                    getProtocolGroupHeaderRange.merge()
//                                                    diff
//                                                )
//                                            return (startInd, diff)
//                                        }

//                                    ) |> Promise.all

//                                let! insertValue = promise {

//                                    let! mergedHeaders =
//                                        context.sync().``then``(fun e -> [|
//                                            for start,diff in startDiffList do
//                                                let range = activeSheet.getRangeByIndexes(annoHeaderRange.rowIndex-1., start, 1., diff)
//                                                let _ = range.load (U2.Case1 "values")
//                                                yield
//                                                    int diff, range
                                                    
//                                        |])
                                    
//                                    let! insertValue =
//                                        context.sync().``then``(fun e ->
//                                            mergedHeaders
//                                            |> Array.map (fun (n,mergedHeader) ->
//                                                    let v = protocol.Id |> box |> Some
//                                                    let nV =
//                                                        [|
//                                                            Array.init n (fun _ -> v) |> ResizeArray
//                                                        |] |> ResizeArray
//                                                    mergedHeader.values <- nV
//                                                )
//                                        )

//                                    return sprintf "%A" protocol.Id
//                                }

//                                return ""
                                    
//                            else
//                                // REMOVE INCOMPLETE PROTOCOL
//                                let! remove = removeProtocolFromXml protocol
//                                return sprintf "%A" remove

//                        }
//                    ) |> Promise.Parallel
//                return group
//            }

//            let! format = formatGroupHeaderForRange groupHeader context

//            return ("info", "Update Protocol Header")
//        }
//    )

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

let createPointerJson() =
    Excel.run(fun context ->

    let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))
        
    promise {
        let! annotationTable = getActiveAnnotationTableName context
        let workbook = context.workbook.load(U2.Case1 "name")

        let! json = context.sync().``then``(fun e -> 
            [
                "name"          , Fable.SimpleJson.JString  ""
                "version"       , Fable.SimpleJson.JString  ""     
                "author"        , Fable.SimpleJson.JArray   []
                "description"   , Fable.SimpleJson.JString  ""
                "docslink"      , Fable.SimpleJson.JString  ""
                "tags"          , Fable.SimpleJson.JArray   []        
                "Workbook"      , Fable.SimpleJson.JString  workbook.name
                "Worksheet"     , Fable.SimpleJson.JString  activeSheet.name
                "Table"         , Fable.SimpleJson.JString  annotationTable
            ]
            |> List.map (fun x ->
                [x]
                |> Map.ofList
                |> Fable.SimpleJson.JObject
                |> Fable.SimpleJson.SimpleJson.toString
                |> fun x -> "  " + x.Replace("{","").Replace("}","")
            )
            |> String.concat (sprintf ",%s" System.Environment.NewLine)
            |> fun jsonbody ->
                sprintf "{%s%s%s}" System.Environment.NewLine jsonbody System.Environment.NewLine
        )

        return json
        }
    )