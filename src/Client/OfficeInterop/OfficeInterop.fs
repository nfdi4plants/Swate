module OfficeInterop

open Fable.Core
open Fable.Core.JsInterop
open OfficeJS
open Excel
open System.Collections.Generic
open System.Text.RegularExpressions

open Shared

open OfficeInterop.Regex
open OfficeInterop.Types
open Xml
open OfficeInterop.HelperFunctions
open OfficeInterop.EventHandlers
open BuildingBlockTypes

/// Reoccuring Comment Defitinitions

/// 'annotationTables'      -> For a workbook (NOT! worksheet) all tables must have unique names. Therefore not all our tables can be called 'annotationTable'.
///                             Instead we add numbers to keep them unique. 'annotationTables' references all of those tables.

/// 'active annotationTable' -> The annotationTable present on the active worksheet. This is not trivial to access an is most of the time passed to a function by
///                             running 'tryFindActiveAnnotationTable()' in another message before.

/// 'TSR'/'TAN'             -> Term Source Ref - column / Term Accession Number - column

/// 'Reference Columns'     -> Meant are the hidden columns including TSR, TAN and Unit columns

/// 'Main Column'           -> Non hidden column of a building block. Each building block only contains one main column

/// 'Tag Array'             -> Column headers can come with additional information. This info is currently saved
///                             in a list of tags starting with '#' in brackets. E.g. '(#id, #h)'

/// 'Unit col block'/       -> This references the unit block of a building block. It is a optional addition and not every
/// 'Unit cols'                 building block must contain it. It consists of a unit main column with the unit term
///                             and it's own TSR and TAN.

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
open Fable.Core

open Fable.SimpleXml
open Fable.SimpleXml.Generator

/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
let exampleExcelFunction () =
    Excel.run(fun context ->
        let annotationTableName = "annotationTable"
        let selectedRange = context.workbook.getSelectedRange()
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
   

        let annoHeaderRange,annoBodyRange = getBuildingBlocksPreSync context annotationTableName
        let groupHeader = annoHeaderRange.getRowsAbove(1.)

        promise {
            // https://docs.microsoft.com/de-de/office/dev/add-ins/excel/excel-add-ins-comments
            //let! format = createGroupHeaderFormatForRange groupHeader context
            let! xmlParsed, xml = getCurrentCustomXml customXmlParts context
            let! buildingBlocks = context.sync().``then``( fun e -> getBuildingBlocks annoHeaderRange annoBodyRange )
            return (sprintf "%A" buildingBlocks)
        }
    )


/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
let exampleExcelFunction2 () =
    Excel.run(fun context ->
        let selectedRange = context.workbook.getSelectedRange()
        let _ = selectedRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount";"address"; "isEntireColumn"])))

        promise {

            let! baseFunc =
                context.sync().``then``(fun e ->
                    sprintf "%A, %A" selectedRange.columnIndex selectedRange.rowIndex
            )

            return baseFunc
        }
    )

/// This function is used to create a new annotation table.
/// 'allTableNames' is a array of all currently existing annotationTables.
/// 'isDark' refers to the current styling of excel (darkmode, or not).
let createAnnotationTable ((allTableNames:String []),isDark:bool) =
    Excel.run(fun context ->

        /// This function is used to create the "next" annotationTable name.
        /// 'allTableNames' is passed from a previous function and contains a list of all annotationTables.
        /// The function then tests if the freshly created name already exists and if it does it rec executes itself againn with (ind+1)
        /// Due to how this function is written, the tables will not always count up. E.g. annotationTable2 gets deleted then the next table will not be
        /// annotationTable3 or higher but annotationTable2 again. This could in the future lead to problems if information is saved with the table name as identifier.
        let rec findNewTableName ind =
            let newTestName =
                if ind = 0 then "annotationTable" else sprintf "annotationTable%i" ind
            let existsAlready = allTableNames |> Array.exists (fun x -> x = newTestName)
            if existsAlready then
                findNewTableName (ind+1)
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

            ///// This is used to create the range in which we want to display the group header. (The row above the table)
            //let! groupDisplayRange = context.sync().``then``( fun _ ->
            //    activeSheet.getRangeByIndexes(tableRange.rowIndex,tableRange.columnIndex,1.,2.)
            //)

            ///// This will format the row above our table to have accent background, white font and inner vertical white borders
            //let! _ = createGroupHeaderFormatForRange groupDisplayRange context

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
                    let rowCount = if tableRange.isEntireColumn then 21. else (if tableRange.rowCount <= 1. then 1. else tableRange.rowCount-1.)
                    activeSheet.getRangeByIndexes(tableRange.rowIndex+1.,tableRange.columnIndex,rowCount,2.)

                /// Create table in current worksheet
                let annotationTable = activeSheet.tables.add(U2.Case1 adaptedRange,true)

                /// Update annotationTable column headers
                (annotationTable.columns.getItemAt 0.).name <- "Source Name"
                (annotationTable.columns.getItemAt 1.).name <- "Sample Name"

                /// Create new annotationTable name
                let newName = findNewTableName 0
                /// Update annotationTable name
                annotationTable.name <- newName

                /// Update annotationTable style
                annotationTable.style <- style

                /// Fit widths and heights of cols and rows to value size. (In this case the new column headers).
                activeSheet.getUsedRange().format.autofitColumns()
                activeSheet.getUsedRange().format.autofitRows()

                let annoTableName = allTableNames |> Array.filter (fun x -> x.StartsWith "annotationTable")

                r.enableEvents <- true

                /// Return info message
                TryFindAnnoTableResult.Success newName, sprintf "Annotation Table created in [%s] with dimensions 2c x (%.0f + 1h)r" tableRange.address (tableRange.rowCount - 1.)
            )
            //.catch (fun e -> e |> unbox<System.Exception> |> fun x -> x.Message)

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

/// This function returns the names of all annotationTables in all worksheets.
/// This function is used to pass a list of all table names to e.g. the 'createAnnotationTable()' function. 
let getTableInfoForAnnoTableCreation() =
    Excel.run(fun context ->

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
    )

/// This function is used to hide all '#h' tagged columns and to fit rows and columns to their values.
/// The main goal is to improve readability of the table with this function.
let autoFitTable (annotationTable) =
    Excel.run(fun context ->

        // Ref. 2
        
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
        let allCols = annotationTable.columns.load(propertyNames = U2.Case1 "items")
    
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))

        let r = context.runtime.load(U2.Case1 "enableEvents")

        context.sync().``then``(fun _ ->

            // Ref. 1
            r.enableEvents <- false

            // Auto fit on all columns to fit cols and rows to their values.
            let allCols = allCols.items |> Array.ofSeq
            let _ =
                allCols
                |> Array.map (fun col -> col.getRange())
                |> Array.map (fun x ->
                    // make all columns visible, we will later selectively hide all with '#h' tag
                    x.columnHidden <- false
                    x.format.autofitColumns()
                    x.format.autofitRows()
                )
            // Get all column headers
            let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
            // Get only column headers with values inside and map object to string
            let headerArr = headerVals |> Array.choose id |> Array.map string
            // Parse header elements into record type
            let parsedHeaderArr = headerArr |> Array.map parseColHeader
            // Find all columns to hide (with '#h' tag)
            let colsToHide =
                parsedHeaderArr
                |> Array.filter (fun header -> header.TagArr.IsSome && Array.contains ColumnTags.HiddenTag header.TagArr.Value)
            // Get all column ranges (necessary to change 'columnHidden' attribute) for all headers with '#h' tag.
            let ranges =
                colsToHide
                |> Array.map (fun header -> (annotationTable.columns.getItem (U2.Case2 header.Header)).getRange())
            // Hide columns
            let _ = ranges |> Array.map (fun x -> x.columnHidden <- true)

            r.enableEvents <- true

            // return message
            "Autoformat Table"
        )
    )

/// This is currently used to get information about the table for the table validation feature.
/// Might be necessary to redesign this to use the newer 'BuildingBlock' or get completly replaced by parts of 'getInsertTermsToFillHiddenCols'
/// As this function creates a complete representation of the table. Should we decide to keep the function then i will add more inline comments.
let getTableRepresentation(annotationTable) =
    Excel.run(fun context ->

        // Ref. 2
        let activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load(U2.Case1 "name")
        let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

        promise {
            let! xmlParsed, xml = getCurrentCustomXml customXmlParts context
            let currentSwateValidationXml = swateValidationOfXml xmlParsed xml

            let! worksheetName, buildingBlocks =
                context.sync().``then``( fun _ ->
                    let buildingBlocks = getBuildingBlocks annoHeaderRange annoBodyRange

                    let worksheetName = activeWorksheet.name
                    worksheetName, buildingBlocks 
                )

            let currentTableValidation =
                if currentSwateValidationXml.IsNone then
                    None
                else
                    let tryFindActiveTableValidation =
                        currentSwateValidationXml.Value.TableValidations
                        |> List.tryFind (fun tableVal -> tableVal.TableName = annotationTable && tableVal.WorksheetName = worksheetName)
                    tryFindActiveTableValidation

            /// This function updates the current SwateValidation xml with all found building blocks.
            let updateCurrentTableValidationXml =
                /// We start by transforming all building blocks into ColumnValidations
                let newColumnValidations = buildingBlocks |> Array.map (fun buildingBlock -> buildingBlock.toColumnValidation) |> List.ofArray
                /// Map over all newColumnValidations and see if they exist in the currentTableValidation xml. If they do, then update them by their validation parameters.
                let updateTableValidation =
                    /// Check if a TableValidation for the active table AND worksheet exists, else return the newly build colValidations.
                    if currentTableValidation.IsSome then
                        let updatedNewColumnValidations =
                            newColumnValidations
                            |> List.map (fun newColVal ->
                                let tryFindCurrentColVal = currentTableValidation.Value.ColumnValidations |> List.tryFind (fun x -> x.ColumnHeader = newColVal.ColumnHeader)
                                if tryFindCurrentColVal.IsSome then
                                    {newColVal with
                                        Importance = tryFindCurrentColVal.Value.Importance
                                        ValidationFormat = tryFindCurrentColVal.Value.ValidationFormat
                                    }
                                else
                                    newColVal
                            )
                        /// Update TableValidation with updated ColumnValidations
                        {currentTableValidation.Value with
                            ColumnValidations = updatedNewColumnValidations}
                    else
                        /// Should no current TableValidation xml exist, create a new one
                        ValidationTypes.TableValidation.create
                            ""
                            worksheetName
                            annotationTable
                            System.DateTime.Now
                            []
                            newColumnValidations
                updateTableValidation

            return updateCurrentTableValidationXml, buildingBlocks, "Update table representation."
        }
    )

/// This function is used to add a new building block to the active annotationTable.
let addAnnotationBlock (annotationTable,colName:string,colTermOpt:DbDomain.Term option,format:string option, unitTermOpt:DbDomain.Term option) =

    /// The following cols are currently always singles (cannot have TSR, TAN, unit cols). For easier refactoring these names are saved in OfficeInterop.Types.
    let isSingleCol =
        match colName with
        | ColumnCoreNames.Shown.Sample | ColumnCoreNames.Shown.Source | ColumnCoreNames.Shown.Data -> true
        | _ -> false

    /// This function will create the mainColumn name from the base name (e.g. 'Parameter [instrument model]' -> Parameter [instrument model] (#1)).
    /// The possible addition of an id tag is needed, because column headers need to be unique in excel.
    let mainColName (colName:string) (id:int) =
        match id with
        | 1 ->
            colName
        | _ ->
            sprintf "%s (#%i)" colName id

    /// This is used to create the bracket information for reference (hidden) columns. Again this has two modi, one with id tag and one without.
    /// This time no core name is needed as this will always be TSR or TAN.
    let hiddenColAttributes (parsedColHeader:ColHeader) (columnTermOption: DbDomain.Term option) (id:int) =
        let coreName =
            match parsedColHeader.Ontology, parsedColHeader.CoreName with
            | Some o , _ -> o.Name
            | None, Some cn -> cn
            | _ -> parsedColHeader.Header
        match id with
        | 1 ->
            match columnTermOption with
            | Some t        -> sprintf "[%s] (#h; #t%s)" coreName t.Accession
            | None          -> sprintf "[%s] (#h)" coreName
        | _ ->
            match columnTermOption with
            | Some t        -> sprintf "[%s] (#%i; #h; #t%s)" coreName id t.Accession
            | None          -> sprintf "[%s] (#%i; #h)" coreName id

    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)

        // Ref. 2

        // This is necessary to place new columns next to selected col
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"; "columnCount"; "rowIndex"|]))
        let tableRange = annotationTable.getRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case1 "columnIndex")

        // Ref. 1
        let r = context.runtime.load(U2.Case1 "enableEvents")

        promise {

            let! newBaseColIndex,headerVals = context.sync().``then``(fun e ->
                // Ref. 3
                /// This is necessary to place new columns next to selected col.
                /// selected ranged returns indices always from a worksheet perspective but we need the related table index. This is calculated here.
                let newBaseColIndex =
                    let diff = range.columnIndex - annoHeaderRange.columnIndex |> int
                    let vals = annoHeaderRange.columnCount |> int
                    let maxLength = vals-1
                    if diff < 0 then
                        maxLength+1
                    elif diff > maxLength then
                        maxLength+1
                    else
                        diff+1
                    |> float

                // This is necessary to skip over hidden cols
                /// Get an array of the headers
                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq

                /// Here is the next col index, which is not hidden, calculated.
                let newBaseColIndex' = findIndexNextNotHiddenCol headerVals newBaseColIndex
                newBaseColIndex', headerVals
            )

            //let! extendGroupHeader = context.sync().``then``(fun _ ->
            //    let colCount =
            //        if isSingleCol then 1.
            //        elif format.IsSome then 6.
            //        else 3.
            //    let getHeaderRange = sheet.getRangeByIndexes(annoHeaderRange.rowIndex-1.,newBaseColIndex+1.,1.,colCount)
            //    let newRange = getHeaderRange.insert(InsertShiftDirection.Right)
            //    newRange.merge()
            //    newRange.load(U2.Case2 (ResizeArray(["values"])))
            //)

            //let! colorNewGroupHeader = createGroupHeaderFormatForRange extendGroupHeader context

            //let! insertValue = context.sync().``then``(fun e ->
            //    let nV =
            //        extendGroupHeader.values
            //        |> Seq.map (fun innerArr ->
            //            innerArr 
            //            |> Seq.map (fun _ ->
            //                "" |> box |> Some
            //            ) |> ResizeArray
            //        ) |> ResizeArray
            //    extendGroupHeader.values <- nV
            //)

            let! res = context.sync().``then``( fun _ ->

                r.enableEvents <- false


                let allColHeaders =
                    headerVals
                    |> Array.choose id
                    |> Array.map string

                /// This is necessary to check if the would be created col name already exists, to then tick up the id tag.
                let parsedBaseHeader = parseColHeader colName
                /// This function checks if the would be col names already exist. If they do it ticks up the id tag to keep col names unique.
                let findNewIdForName() =
                    let rec loopingCheck int =
                        let isExisting =
                            allColHeaders
                            // Should a column with the same name already exist, then count up the id tag.
                            |> Array.exists (fun existingHeader ->
                                if isSingleCol then
                                    existingHeader = mainColName colName int
                                else
                                    existingHeader = mainColName colName int
                                    // i think it is necessary to also check for "TSR" and "TAN" because of the following possibilities
                                    // Parameter [instrument model] | "Term Source REF [instrument model] (#h) | ...
                                    // Factor [instrument model] | "Term Source REF [instrument model] (#h) | ...
                                    // in the example above the mainColumn name is different but "TSR" and "TAN" would be the same.
                                    || existingHeader = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader colTermOpt int)
                                    || existingHeader = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader colTermOpt int)
                            )
                        if isExisting then
                            loopingCheck (int+1)
                        else
                            int
                    loopingCheck 1

                // The new id, which does not exist yet with the column name
                let newId = findNewIdForName()

                let rowCount = tableRange.rowCount |> int

                //create an empty column to insert
                let col = createEmptyMatrixForTables 1 rowCount ""

                // create main column
                let createdCol1() =
                    annotationTable.columns.add(
                        index = newBaseColIndex,
                        values = U4.Case1 col,
                        name = mainColName colName newId
                    )

                // create TSR
                let createdCol2() =
                    annotationTable.columns.add(
                        index = newBaseColIndex+1.,
                        values = U4.Case1 col,
                        name = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader colTermOpt newId)
                    )

                // create TAN
                let createdCol3() =
                    annotationTable.columns.add(
                        index = newBaseColIndex+2.,
                        values = U4.Case1 col,
                        name = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader colTermOpt newId)
                    )

                // Should the column be Data, Source or Sample then we do not add TSR and TAN
                let createCols =
                    if isSingleCol then
                        [|createdCol1()|]
                    
                    else [|
                        createdCol1()
                        createdCol2()
                        createdCol3()
                    |]

                /// if format.isSome then we need to also add unit columns in the following scheme:
                /// Unit [UnitTermName] (#id; #h; #u) | Term Source REF [UnitTermName] (#id; #h; #u) | Term Accession Number [UnitTermName] (#id; #h; #u)
                let createUnitColsIfNeeded =
                    OfficeInterop.HelperFunctions.createUnitColumns allColHeaders annotationTable newBaseColIndex rowCount format unitTermOpt

                /// If unit block was added then return some msg information
                let unitColCreationMsg = if createUnitColsIfNeeded.IsSome then fst createUnitColsIfNeeded.Value else ""
                let unitColFormat = if createUnitColsIfNeeded.IsSome then snd createUnitColsIfNeeded.Value else "0.00"

                r.enableEvents <- true
                /// return main col names, unit column format and a message. The first two params are used in a follow up message (executing 'changeTableColumnFormat')
                mainColName colName newId, unitColFormat, sprintf "%s column was added. %s" colName unitColCreationMsg
            )

            return res
        }
    )

let changeTableColumnFormat annotationTable (colName:string) (format:string) =
    Excel.run(fun context ->

       // Ref. 2 
       let sheet = context.workbook.worksheets.getActiveWorksheet()
       let annotationTable = sheet.tables.getItem(annotationTable)

       // get ranged of main column that was previously created
       let colBodyRange = (annotationTable.columns.getItem (U2.Case2 colName)).getDataBodyRange()
       let _ = colBodyRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))


       // Ref. 1
       let r = context.runtime.load(U2.Case1 "enableEvents")

       context.sync().``then``( fun _ ->

            r.enableEvents <- false

            let rowCount = colBodyRange.rowCount |> int
            // create a format column to insert
            let formats = createValueMatrix 1 rowCount format

            // add unit format to previously created main column
            colBodyRange.numberFormat <- formats

            r.enableEvents <- true

            // return msg
            sprintf "format of %s was changed to %s" colName format
       )
    )

// Reform this to onSelectionChanged (Even though we now know how to add eventHandlers we do not know how to pass info from handler to Swate app).
/// This function will parse the header of a selected column to check for a parent ontology, which will then be used for a isA-directed term search.
/// Any found parent ontology will also be displayed in a static field before the term search input field.
let getParentTerm (annotationTable) =
    Excel.run (fun context ->

        // Ref. 2
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
        let tableRange = annotationTable.getRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray[|"columnIndex"; "rowIndex"; "values"|]))
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray[|"columnIndex"; "rowIndex"|]))

        context.sync()
            .``then``( fun _ ->

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
                        let parsedHeader = parseColHeader (string header.Value)
                        /// as the reference columns also contain a accession tag we want to return the first reference column header
                        /// instead of the main column header, if the main column header does include an ontology
                        if parsedHeader.Ontology.IsSome then
                            tableRange.values.[0].[newColIndex+1]
                        else
                            None
                // return header of selected col
                value
            )
    )

/// This is used to insert terms into selected cells.
/// 'term' is the value that will be written into the main column.
/// 'termBackground' needs to be spearate from 'term' in case the user uses the fill function for a custom term.
/// Should the user write a real term with this function 'termBackground'.isSome and can be used to fill TSR and TAN.
let fillValue (annotationTable,term,termBackground:Shared.DbDomain.Term option) =
    Excel.run(fun context ->

        // Ref. 2
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))
        /// This is for TSR and TAN
        let nextColsRange = range.getColumnsAfter 2.
        let _ = nextColsRange.load(U2.Case2 (ResizeArray(["values";"columnIndex";"columnCount"])))

        // Ref. 1
        let r = context.runtime.load(U2.Case1 "enableEvents")

        //sync with proxy objects after loading values from excel
        context.sync().``then``( fun _ ->

            /// failwith if the number of selected columns is > 1. This is done due to hidden columns
            /// and an overlapping reaction as we add values to the columns next to the selected one
            if range.columnCount > 1. then failwith "Cannot insert Terms in more than one column at a time."

            r.enableEvents <- false

            // create new values for selected range 
            let newVals = ResizeArray([
                for arr in range.values do
                    let tmp = arr |> Seq.map (fun _ -> Some (term |> box))
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
                            match i, termBackground with
                            | 0, None | 1, None ->
                                Some ("user-specific" |> box)
                            | 1, Some term ->
                                //add "Term Accession Number"
                                let replace = Shared.URLs.TermAccessionBaseUrl + term.Accession.Replace(@":",@"_")
                                Some ( replace |> box )
                            | 0, Some term ->
                                //add "Term Source REF"
                                Some (term.Accession.Split(@":").[0] |> box)
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
            sprintf "%A, %A" nextColsRange.values.Count nextNewVals
        )
    )

/// This is used to create a full representation of all building blocks in the table. This representation is then split into unit building blocks and regular building blocks.
/// These are then filtered for search terms and aggregated into an 'SearchTermI []', which is used to search the database for missing values.
/// 'annotationTable'' gets passed by 'tryFindActiveAnnotationTable'.
let createSearchTermsIFromTable (annotationTable') =
    Excel.run(fun context ->

        // Ref. 2
        let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable'

        context.sync()
            .``then``( fun _ ->
                
                /// Sort all columns into building blocks.
                let buildingBlocks =
                    getBuildingBlocks annoHeaderRange annoBodyRange

                /// Filter for only building blocks with ontology (indicated by having a TSR and TAN).
                let buildingBlocksWithOntology =
                    buildingBlocks |> Array.filter (fun x -> x.TSR.IsSome && x.TAN.IsSome)

                /// We need an array of all distinct cell.values and where they occur in col- and row-index
                let terms =
                    buildingBlocksWithOntology
                    |> Array.collect (fun bBlock ->
                        // get current col index
                        let tsrTanColIndices = [|bBlock.TSR.Value.Index; bBlock.TAN.Value.Index|]
                        let fillTermConstructsNoUnit bBlock =
                            // group cells by value so we don't get doubles.
                            bBlock.MainColumn.Cells
                            |> Array.groupBy (fun cell ->
                                cell.Value.Value
                            )
                            // create SearchTermI types that will be passed to the server to get filled with a term option.
                            |> Array.map (fun (searchStr,cellArr) ->
                                let rowIndices = cellArr |> Array.map (fun cell -> cell.Index)
                                Shared.SearchTermI.create tsrTanColIndices searchStr "" bBlock.MainColumn.Header.Value.Ontology rowIndices
                            )
                        /// We differentiate between building blocks with and without unit as unit building blocks will not contain terms as values but e.g. numbers.
                        /// In this case we do not want to search the database for the cell values but the parent ontology in the header.
                        /// This will then be used for TSR and TAN.
                        let fillTermConstructsWithUnit (bBlock:BuildingBlock) =
                            let searchStr       = bBlock.MainColumn.Header.Value.Ontology.Value.Name
                            let termAccession   = bBlock.MainColumn.Header.Value.Ontology.Value.TermAccession
                            let rowIndices =
                                bBlock.MainColumn.Cells
                                |> Array.map (fun x ->
                                   x.Index
                                )
                            [|Shared.SearchTermI.create tsrTanColIndices searchStr termAccession None rowIndices|]
                        if bBlock.Unit.IsSome then
                            fillTermConstructsWithUnit bBlock
                        else
                            fillTermConstructsNoUnit bBlock
                    )

                /// Create search types for the unit building blocks.
                let units =
                    buildingBlocksWithOntology
                    |> Array.filter (fun bBlock -> bBlock.Unit.IsSome)
                    |> Array.map (
                        fun bBlock ->
                            let unit = bBlock.Unit.Value
                            let searchString  = unit.MainColumn.Header.Value.Ontology.Value.Name
                            let termAccession = unit.MainColumn.Header.Value.Ontology.Value.TermAccession 
                            let colIndices = [|unit.MainColumn.Index; unit.TSR.Value.Index; unit.TAN.Value.Index|]
                            let rowIndices = unit.MainColumn.Cells |> Array.map (fun x -> x.Index)
                            Shared.SearchTermI.create colIndices searchString termAccession None rowIndices
                    )

                /// Combine search types
                let allSearches = [|
                    yield! terms
                    yield! units
                |]

                /// Return the name of the table and all search types
                annotationTable',allSearches
            )
    )

/// This function will be executed after the SearchTerm types from 'createSearchTermsFromTable' where send to the server to search the database for them.
/// Here the results will be written into the table by the stored col and row indices.
let UpdateTableBySearchTermsI (annotationTable,terms:SearchTermI []) =
    Excel.run(fun context ->

        /// This will create a single cell value arr
        let createCellValueInput str =
            ResizeArray([
                ResizeArray([
                    str |> box |> Some
                ])
            ])

        // Ref. 2
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
        let annoBodyRange = annotationTable.getDataBodyRange()
        let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore

        // Ref. 1
        let r = context.runtime.load(U2.Case1 "enableEvents")

        context.sync().
            ``then``(fun _ ->
                r.enableEvents <- false
                /// Filter for only terms which returned a result and therefore were not custom user input.
                let foundTerms = terms |> Array.filter (fun x -> x.TermOpt.IsSome)
                /// Insert terms into related cells for all stored row-/ col-indices
                let insert() =
                    terms
                    // iterate over all found terms
                    |> Array.map (
                        fun term ->
                            let t,ont,accession =
                                if term.TermOpt.IsSome then
                                    /// Term search result from database
                                    let t = term.TermOpt.Value
                                    /// Get ontology and accession from Term.Accession
                                    let ont, accession =
                                        let a = t.Accession
                                        let splitA = a.Split":"
                                        let accession = Shared.URLs.TermAccessionBaseUrl + a.Replace(":","_")
                                        splitA.[0], accession
                                    t.Name,ont,accession
                                elif term.SearchString = "" then
                                    let t = ""
                                    let ont = ""
                                    let accession = ""
                                    t, ont, accession
                                elif term.TermOpt = None then
                                    let t = term.SearchString
                                    let ont = "user-specific"
                                    let accession = "user-specific"
                                    t, ont, accession
                                else
                                    failwith "Swate encountered an error in (UpdateTableBySearchTermsI.insert()) trying to parse database search results to Swate table."
                            /// Distinguish between core building blocks and unit buildingblocks.
                            let inputVals = [|
                                /// if the n of cols is 2 then it is a core building block.
                                if term.ColIndices.Length = 2 then
                                    createCellValueInput ont
                                    createCellValueInput accession
                                /// if the n of cols is 3 then it is a unit building block.
                                elif term.ColIndices.Length = 3 then
                                    createCellValueInput t
                                    createCellValueInput ont
                                    createCellValueInput accession
                            |]

                            /// ATTENTION!! The following seems to be a strange interaction between office.js and fable.
                            /// In an example with 2 colIndices i had a mistake in the code to access: 'for i in 0 .. insertTerm.ColIndices.Length do'
                            /// so i actually accessed 3 colIndices which should have led to the classic 'System.IndexOutOfRangeException', but it didnt.
                            /// for 'inputVals.[2]' it returned 'undefined' and for 'insertTerm.ColIndices.[2]' it returned '0'.
                            /// This led to the first column to be erased for the same rows that were found to be replaced.

                            // iterate over all columns (in this case in form of the index of their array. as we need the index to access the correct 'inputVal' value
                            for i in 0 .. term.ColIndices.Length-1 do

                                // iterate over all rows and insert the correct inputVal
                                for rowInd in term.RowIndices do

                                    let cell = annoBodyRange.getCell(float rowInd, float term.ColIndices.[i])
                                    cell.values <- inputVals.[i]
                    )

                let _ = insert()

                r.enableEvents <- true

                // return print msg
                sprintf "Filled information for terms: %s" (foundTerms |> Array.map (fun x -> x.TermOpt.Value.Name) |> String.concat ", ")
            )
    )

/// This function is used to insert file names into the selected range.
let insertFileNamesFromFilePicker (annotationTable, fileNameList:string list) =
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

let getTableMetaData (annotationTable) =
    Excel.run (fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
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

let writeProtocolToXml(protocol:GroupTypes.Protocol,currentSwateVersion:string) =
    Excel.run(fun context ->

        let newProtocol = {
            protocol with
                SwateVersion = if protocol.SwateVersion = "" then currentSwateVersion else protocol.SwateVersion
        }

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

        promise {
            let! xmlParsed, xml = getCurrentCustomXml customXmlParts context

            let currentProtocolGroup =
                let previousProtocolGroup = protocolGroupsOfXml xmlParsed xml
                if previousProtocolGroup.IsNone then GroupTypes.ProtocolGroup.create currentSwateVersion [] else previousProtocolGroup.Value

            let nextProtocolGroup =
                let newProtocols =
                    currentProtocolGroup.Protocols
                    |> List.filter (fun x -> x.TableName <> newProtocol.TableName || x.WorksheetName <> newProtocol.WorksheetName || x.Id <> newProtocol.Id)
                    |> fun filteredProtocols -> newProtocol::filteredProtocols
                { currentProtocolGroup with
                    SwateVersion = currentSwateVersion
                    Protocols = newProtocols
                }

            let nextCustomXml =
                let nextAsXmlFormat = nextProtocolGroup.toXml |> SimpleXml.parseElement
                let childrenWithoutProtocolGroup = xmlParsed.Children |> List.filter (fun child ->
                    child.Name <> "ProtocolGroup"
                )
                let nextChildren = nextAsXmlFormat::childrenWithoutProtocolGroup
                { xmlParsed with
                    Children = nextChildren
                } |> OfficeInterop.HelperFunctions.xmlElementToXmlString

            let! deleteXml =
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.delete() )
                    
                    xmls |> Array.ofSeq
                )

            let! addNext =
                context.sync().``then``(fun e ->
                    customXmlParts.add(nextCustomXml)
                )

            // This will be displayed in activity log
            return
                "Info",
                sprintf
                    "Update ProtocolGroup Scheme with '%s - %s - %s' "
                    newProtocol.WorksheetName
                    newProtocol.TableName
                    newProtocol.Id
        }
    )

let writeTableValidationToXml(tableValidation:ValidationTypes.TableValidation,currentSwateVersion:string) =
    Excel.run(fun context ->

        // Update DateTime 
        let newTableValidation = {
            tableValidation with
                // This line is used to give freshly created TableValidations the current Swate Version
                SwateVersion = if tableValidation.SwateVersion = "" then currentSwateVersion else tableValidation.SwateVersion
                DateTime = System.DateTime.Now
            }

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
    
        promise {
    
            let! xmlParsed, xml = getCurrentCustomXml customXmlParts context

            let currentSwateValidationXml =
                let previousValidation = swateValidationOfXml xmlParsed xml
                if previousValidation.IsNone then ValidationTypes.SwateValidation.init (currentSwateVersion) else previousValidation.Value
    
            let nextSwateValidationXml =
                let newTableValidations =
                    currentSwateValidationXml.TableValidations
                    |> List.filter (fun x -> x.TableName <> newTableValidation.TableName || x.WorksheetName <> newTableValidation.WorksheetName)
                    |> fun filteredValidations -> newTableValidation::filteredValidations
                { currentSwateValidationXml with
                    SwateVersion = currentSwateVersion
                    TableValidations = newTableValidations
                }
    
            let nextCustomXml =
                let nextAsXmlFormat = nextSwateValidationXml.toXml |> SimpleXml.parseElement
                let childrenWithoutValidation = xmlParsed.Children |> List.filter (fun child ->
                    child.Name <> "Validation"
                )
                let nextChildren = nextAsXmlFormat::childrenWithoutValidation
                { xmlParsed with
                        Children = nextChildren
                } |> OfficeInterop.HelperFunctions.xmlElementToXmlString
                        
            let! deleteXml =
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Seq.map (fun x -> x.delete() )
    
                    xmls |> Array.ofSeq
                )
    
            let! addNext =
                context.sync().``then``(fun e ->
                    customXmlParts.add(nextCustomXml)
                )

            // This will be displayed in activity log
            return
                "Info",
                sprintf
                    "Update Validation Scheme with '%s - %s' @%s"
                    newTableValidation.WorksheetName
                    newTableValidation.TableName
                    ( newTableValidation.DateTime.ToString("yyyy-MM-dd HH:mm") )
        }
    )

/// This function is used to add unit reference columns to an existing building block without unit reference columns
let addUnitToExistingBuildingBlock (annotationTable:string,format:string option,unitTermOpt:DbDomain.Term option) =
    Excel.run(fun context ->

        let annotationTableName = annotationTable

        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTableName)

        // Ref. 2

        // This is necessary to place new columns next to selected col
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"rowIndex"|]))

        let tableRange = annotationTable.getRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))

        let selectedRange = context.workbook.getSelectedRange()
        let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))

        // Ref. 2
        let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTableName

        context.sync()
            .``then``( fun _ ->

                if selectedRange.columnCount <> 1. then
                    failwith "To add a unit please select a single column"

                let newSelectedColIndex =
                    // recalculate the selected range index based on table
                    let diff = selectedRange.columnIndex - annoHeaderRange.columnIndex
                    // if index is smaller 0 it is outside of table range
                    if diff <= 0. then 0.
                    // if index is bigger than columnCount-1 then it is outside of tableRange
                    elif diff >= annoHeaderRange.columnCount-1. then annoHeaderRange.columnCount-1.
                    else diff

                /// Sort all columns into building blocks.
                let buildingBlocks =
                    getBuildingBlocks annoHeaderRange annoBodyRange

                /// find building block with the closest main column index from left
                let findLeftClosestBuildingBlock =
                    buildingBlocks
                    |> Array.filter (fun x -> x.MainColumn.Index <= int newSelectedColIndex)
                    |> Array.minBy (fun x -> Math.Abs(x.MainColumn.Index - int newSelectedColIndex))

                if findLeftClosestBuildingBlock.TAN.IsNone || findLeftClosestBuildingBlock.TSR.IsNone then
                    failwith (
                        sprintf
                            "Swate can only add a unit to columns of the type: %s, %s, %s."
                            OfficeInterop.Types.ColumnCoreNames.Shown.Parameter
                            OfficeInterop.Types.ColumnCoreNames.Shown.Characteristics
                            OfficeInterop.Types.ColumnCoreNames.Shown.Factor
                    )

                if findLeftClosestBuildingBlock.Unit.IsSome then
                    failwith (
                        sprintf
                            "Swate cannot add a unit to a building block already containing a unit: %A"
                            findLeftClosestBuildingBlock.Unit.Value.MainColumn.Header.Value.CoreName
                    )

                // This is necessary to skip over hidden cols
                /// Get an array of the headers
                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq

                let allColHeaders =
                    headerVals
                    |> Array.choose id
                    |> Array.map string

                let unitColumnResult =
                    createUnitColumns allColHeaders annotationTable (float findLeftClosestBuildingBlock.MainColumn.Index) (int tableRange.rowCount) format unitTermOpt

                let maincolName = findLeftClosestBuildingBlock.MainColumn.Header.Value.Header

                /// If unit block was added then return some msg information
                let unitColCreationMsg = if unitColumnResult.IsSome then fst unitColumnResult.Value else ""
                let unitColFormat = if unitColumnResult.IsSome then snd unitColumnResult.Value else "0.00"

                maincolName, unitColFormat, unitColCreationMsg
        )
    )