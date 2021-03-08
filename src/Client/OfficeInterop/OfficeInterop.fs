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
let exampleExcelFunction1 () =
    Excel.run(fun context ->
              
        promise {
        
            return (sprintf "0" )
        }
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
                    |> Array.map (fun x -> Shared.AnnotationTable.create x.name x.worksheet.name)

                tableNames
            )

            let! xmlParsed = getCustomXml customXmlParts context

            let protocolGroups = getAllSwateProtocolGroups xmlParsed

            let tableValidations = getAllSwateTableValidation xmlParsed
            
            return (sprintf "%A, %A, %A" protocolGroups.Length tableValidations.Length allTables)
        }
    )

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

            let! allTableNames = getAllTableInfo()

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
                let newName = findNewTableName allTableNames 0
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

            let! sync = context.sync().``then``(fun e -> ())

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

/// This function is used to hide all '#h' tagged columns and to fit rows and columns to their values.
/// The main goal is to improve readability of the table with this function.
let autoFitTable () =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName()

            // Ref. 2
            let sheet = context.workbook.worksheets.getActiveWorksheet()

            let annotationTable = sheet.tables.getItem(annotationTable)
            let allCols = annotationTable.columns.load(propertyNames = U2.Case1 "items")
    
            let annoHeaderRange = annotationTable.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))

            let r = context.runtime.load(U2.Case1 "enableEvents")

            let! res = context.sync().``then``(fun _ ->

                // Ref. 1
                r.enableEvents <- false
                // Auto fit on all columns to fit cols and rows to their values.
                let allTableCols = allCols.items |> Array.ofSeq
                let _ =
                    allTableCols
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
                "Info","Autoformat Table"
            )

            return res
        }
    )

/// This is currently used to get information about the table for the table validation feature.
/// Might be necessary to redesign this to use the newer 'BuildingBlock' or get completly replaced by parts of 'getInsertTermsToFillHiddenCols'
/// As this function creates a complete representation of the table. Should we decide to keep the function then i will add more inline comments.
let getTableRepresentation() =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName()

            // Ref. 2
            let activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load(U2.Case1 "name")
            let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

            let! xmlParsed = getCustomXml customXmlParts context
            let currentTableValidation = getSwateValidationForCurrentTable annotationTable activeWorksheet.name xmlParsed

            let! buildingBlocks =
                context.sync().``then``( fun _ ->
                    let buildingBlocks = getBuildingBlocks annoHeaderRange annoBodyRange

                    buildingBlocks 
                )

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
                            activeWorksheet.name
                            annotationTable
                            (System.DateTime.Now.ToUniversalTime())
                            []
                            newColumnValidations
                updateTableValidation

            return updateCurrentTableValidationXml, buildingBlocks, "Update table representation."
        }
    )

/// This function is used to add a new building block to the active annotationTable.
let addAnnotationBlock (buildingBlockInfo:MinimalBuildingBlock) =

    /// The following cols are currently always singles (cannot have TSR, TAN, unit cols). For easier refactoring these names are saved in OfficeInterop.Types.
    let isSingleCol =
        match buildingBlockInfo.MainColumnName with
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
    let hiddenColAttributes (parsedColHeader:ColHeader) (columnAccessionOpt: string option) (id:int) =
        let coreName =
            match parsedColHeader.Ontology, parsedColHeader.CoreName with
            | Some o , _ -> o.Name
            | None, Some cn -> cn
            | _ -> parsedColHeader.Header
        match id with
        | 1 ->
            match columnAccessionOpt with
            | Some accession    -> sprintf "[%s] (#h; #t%s)" coreName accession
            | None              -> sprintf "[%s] (#h)" coreName
        | _ ->
            match columnAccessionOpt with
            | Some accession    -> sprintf "[%s] (#%i; #h; #t%s)" coreName id accession
            | None              -> sprintf "[%s] (#%i; #h)" coreName id

    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName()

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

            let! newBaseColIndex,headerVals = context.sync().``then``(fun e ->
                // Ref. 3
                /// This is necessary to place new columns next to selected col.
                /// selected ranged returns indices always from a worksheet perspective but we need the related table index. This is calculated here.
                let newBaseColIndex =
                    let diff = range.columnIndex - annoHeaderRange.columnIndex |> int
                    let vals = annoHeaderRange.columnCount |> int
                    let maxLength = vals-1
                    if diff < 0 then
                        maxLength
                    elif diff > maxLength then
                        maxLength
                    else
                        diff
                    |> float

                // This is necessary to skip over hidden cols
                /// Get an array of the headers
                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq

                /// Here is the next col index, which is not hidden, calculated.
                let newBaseColIndex' = findIndexNextNotHiddenCol headerVals newBaseColIndex
                newBaseColIndex', headerVals
            )

            let! res = context.sync().``then``( fun _ ->

                r.enableEvents <- false


                let allColHeaders =
                    headerVals
                    |> Array.choose id
                    |> Array.map string

                /// This is necessary to check if the would be created col name already exists, to then tick up the id tag.
                let parsedBaseHeader = parseColHeader buildingBlockInfo.MainColumnName
                /// This function checks if the would be col names already exist. If they do it ticks up the id tag to keep col names unique.
                let findNewIdForName() =
                    let rec loopingCheck int =
                        let isExisting =
                            allColHeaders
                            // Should a column with the same name already exist, then count up the id tag.
                            |> Array.exists (fun existingHeader ->
                                if isSingleCol then
                                    existingHeader = mainColName buildingBlockInfo.MainColumnName int
                                else
                                    existingHeader = mainColName buildingBlockInfo.MainColumnName int
                                    // i think it is necessary to also check for "TSR" and "TAN" because of the following possibilities
                                    // Parameter [instrument model] | "Term Source REF [instrument model] (#h) | ...
                                    // Factor [instrument model] | "Term Source REF [instrument model] (#h) | ...
                                    // in the example above the mainColumn name is different but "TSR" and "TAN" would be the same.
                                    || existingHeader = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader buildingBlockInfo.MainColumnTermAccession int)
                                    || existingHeader = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader buildingBlockInfo.MainColumnTermAccession int)
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
                let col value = createEmptyMatrixForTables 1 rowCount value

                // create main column
                let createdCol1() =
                    let mainColVal = if buildingBlockInfo.Values.IsSome then buildingBlockInfo.Values.Value.Name else ""
                    annotationTable.columns.add(
                        index = newBaseColIndex,
                        values = U4.Case1 (col mainColVal),
                        name = mainColName buildingBlockInfo.MainColumnName newId
                    )

                // create TSR
                let createdCol2() =
                    let tsrColVal = if buildingBlockInfo.Values.IsSome && buildingBlockInfo.Values.Value.TermAccession <> "" then buildingBlockInfo.Values.Value.TermAccession.Split([|":"|],StringSplitOptions.None).[0] else ""
                    annotationTable.columns.add(
                        index = newBaseColIndex+1.,
                        values = U4.Case1 (col tsrColVal),
                        name = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader buildingBlockInfo.MainColumnTermAccession newId)
                    )

                // create TAN
                let createdCol3() =
                    let linkTermAccession() = buildingBlockInfo.Values.Value.TermAccession |> Shared.URLs.termAccessionUrlOfAccessionStr
                    let tanColVal = if buildingBlockInfo.Values.IsSome && buildingBlockInfo.Values.Value.TermAccession <> "" then linkTermAccession() else ""
                    annotationTable.columns.add(
                        index = newBaseColIndex+2.,
                        values = U4.Case1 (col tanColVal),
                        name = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader buildingBlockInfo.MainColumnTermAccession newId)
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
                    OfficeInterop.HelperFunctions.createUnitColumns allColHeaders annotationTable newBaseColIndex rowCount buildingBlockInfo.UnitName buildingBlockInfo.UnitTermAccession

                /// If unit block was added then return some msg information
                let unitColCreationMsg = if createUnitColsIfNeeded.IsSome then fst createUnitColsIfNeeded.Value else ""
                let unitColFormat = if createUnitColsIfNeeded.IsSome then snd createUnitColsIfNeeded.Value else "0.00"

                r.enableEvents <- true
                /// return main col names, unit column format and a message. The first two params are used in a follow up message (executing 'changeTableColumnFormat')
                mainColName buildingBlockInfo.MainColumnName newId, unitColFormat, sprintf "%s column was added. %s" buildingBlockInfo.MainColumnName unitColCreationMsg
            )

            return res
        }
    )

let addAnnotationBlocksAsProtocol (buildingBlockInfoList:MinimalBuildingBlock list, protocol:Xml.GroupTypes.Protocol) =
   
    let addBuildingBlock buildingBlockInfo =
        promise {
            let! res = addAnnotationBlock(buildingBlockInfo)
            return [res]
        }
    let chainBuildingBlocks buildingBlockInfoList =
        let promiseList = buildingBlockInfoList |> List.map (fun x -> addBuildingBlock x)

        let emptyPromise = promise {return []}

        let rec chain ind (promiseList:JS.Promise<(string*string*string) list> list ) resultPromise =
            if ind >= promiseList.Length then
                resultPromise
            elif ind = 0 then 
                let currentPromise = promiseList |> List.item ind
                chain 1 promiseList currentPromise
            else
                let currentPromise = promiseList |> List.item ind
                let nextPromise =
                    Promise.PromiseBuilder().Merge(currentPromise,resultPromise, (fun x y -> x@y))
                chain (ind+1) promiseList nextPromise

        chain 0 promiseList emptyPromise

    let infoProm =

        Excel.run(fun context ->
            promise {

                let! annotationTable = getActiveAnnotationTableName()

                let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
                let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
                let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))

                let annoHeaderRange,annoBodyRange = getBuildingBlocksPreSync context annotationTable

                let! xmlParsed = getCustomXml customXmlParts context
                
                let currentProtocolGroup = getSwateProtocolGroupForCurrentTable annotationTable activeSheet.name xmlParsed

                let! buildingBlocks = context.sync().``then``( fun e -> getBuildingBlocks annoHeaderRange annoBodyRange )

                if currentProtocolGroup.IsSome then
                    let existsAlready =
                        currentProtocolGroup.Value.Protocols
                        |> List.tryFind ( fun existingProtocol ->
                            if buildingBlockInfoList |> List.exists (fun x -> x.IsAlreadyExisting = true) then
                                existingProtocol.Id = protocol.Id
                            else
                                false
                        )
                    let isComplete =
                        if existsAlready.IsSome then
                            (tryFindSpannedBuildingBlocks existsAlready.Value buildingBlocks).IsSome
                        else
                            true
                    if existsAlready.IsSome then
                        if isComplete then
                            failwith ( sprintf "Protocol %s exists already in %s - %s." existsAlready.Value.Id currentProtocolGroup.Value.AnnotationTable.Name currentProtocolGroup.Value.AnnotationTable.Worksheet)

                /// filter out building blocks that are only passed to keep the colNames
                let onlyNonExistingBuildingBlocks = buildingBlockInfoList |> List.filter (fun x -> x.IsAlreadyExisting <> true)
                let alreadyExistingBlocks =
                    buildingBlockInfoList
                    |> List.filter (fun x -> x.IsAlreadyExisting = true)
                    |> List.map (fun x ->
                        x.MainColumnName, "0.00", ""
                    )

                let! chainProm = chainBuildingBlocks onlyNonExistingBuildingBlocks

                let updateProtocol = {protocol with AnnotationTable = AnnotationTable.create annotationTable activeSheet.name}

                return (chainProm@alreadyExistingBlocks,updateProtocol)
            }
        )

    promise {
        let! blockResults,info = infoProm
        let createSpannedBlocks =
            [
                for ind in 0 .. blockResults.Length-1 do
                    let colName                     = blockResults |> List.item ind |> (fun (x,_,_) -> x)
                    let relatedTermAccession        =
                        buildingBlockInfoList
                        |> List.tryFind ( fun x -> colName.Contains(x.MainColumnName) )
                        |> fun x ->
                            if x.IsNone then
                                failwith (
                                    sprintf
                                        "Could not find created building block information %s in given list: %A"
                                        colName
                                        (buildingBlockInfoList|> List.map (fun y -> y.MainColumnName))
                                )
                            else
                                if x.IsSome && x.Value.MainColumnTermAccession.IsSome then x.Value.MainColumnTermAccession.Value else ""
                    yield
                        Xml.GroupTypes.SpannedBuildingBlock.create colName relatedTermAccession
            ]
        let completeProtocolInfo = {info with SpannedBuildingBlocks = createSpannedBlocks}
        printfn "%A" completeProtocolInfo
        return (blockResults,completeProtocolInfo)
    }

/// This function removes a given building block from a given annotation table.
/// It returns the affected column indices.
let removeAnnotationBlock (tableName:string) (annotationBlock:BuildingBlock) =
    Excel.run(fun context ->
        promise {

            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let table = sheet.tables.getItem(tableName)
        
            // Ref. 2
        
            let _ = table.load(U2.Case1 "columns")
            let tableCols = table.columns.load(propertyNames = U2.Case1 "items")

            let targetedColIndices =
                let hasTSRAndTan =
                    if annotationBlock.hasCompleteTSRTAN then [|annotationBlock.TAN.Value.Index; annotationBlock.TSR.Value.Index|] else [||]
                let hasUnit =
                    if annotationBlock.hasCompleteUnitBlock then
                        [|annotationBlock.Unit.Value.MainColumn.Index;annotationBlock.Unit.Value.TSR.Value.Index;annotationBlock.Unit.Value.TAN.Value.Index|]
                    else
                        [||]
                [|  annotationBlock.MainColumn.Index
                    yield! hasTSRAndTan
                    yield! hasUnit
                |] |> Array.sort

            let! deleteCols =
                context.sync().``then``(fun e ->
                    targetedColIndices |> Array.map (fun targetIndex ->
                        tableCols.items.[targetIndex].delete()
                    )
                )

            return targetedColIndices
        }
    )

let removeAnnotationBlocks (tableName:string) (annotationBlocks:BuildingBlock [])  =
    annotationBlocks
    |> Array.sortByDescending (fun x -> x.MainColumn.Index)
    |> Array.map (removeAnnotationBlock tableName)
    |> Promise.all

let removeSelectedAnnotationBlock () =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName()

            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let table = sheet.tables.getItem(annotationTable)

            // Ref. 2

            let _ = table.load(U2.Case1 "columns")
            let tableCols = table.columns.load(propertyNames = U2.Case1 "items")

            // This is necessary to place new columns next to selected col
            let annoHeaderRange = table.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"rowIndex"|]))

            let tableRange = table.getRange()
            let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))

            let selectedRange = context.workbook.getSelectedRange()
            let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))

            // Ref. 2
            let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

            let! selectedBuildingBlock =
                BuildingBlockTypes.findSelectedBuildingBlock selectedRange annoHeaderRange annoBodyRange context

            let! deleteCols = removeAnnotationBlock annotationTable selectedBuildingBlock

            return sprintf "Delete Building Block %s (Cols: %A]" selectedBuildingBlock.MainColumn.Header.Value.Header deleteCols
        }
    )

let getAnnotationBlockDetails() =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName()

            let selectedRange = context.workbook.getSelectedRange()
            let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))
        
            // Ref. 2
            let annoHeaderRange, annoBodyRange = getBuildingBlocksPreSync context annotationTable

            let! selectedBuildingBlock =
                findSelectedBuildingBlock selectedRange annoHeaderRange annoBodyRange context

            let searchTerms = sortBuildingBlockToSearchTerm selectedBuildingBlock

            return searchTerms
        }
    )

let changeTableColumnFormat (colName:string) (format:string) =
    Excel.run(fun context ->
        
        promise {

            let! annotationTable = getActiveAnnotationTableName()

            // Ref. 2 
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTable)
            
            // get ranged of main column that was previously created
            let colBodyRange = (annotationTable.columns.getItem (U2.Case2 colName)).getDataBodyRange()
            let _ = colBodyRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
            
            
            // Ref. 1
            let r = context.runtime.load(U2.Case1 "enableEvents")

            let! res = context.sync().``then``( fun _ ->
                
                r.enableEvents <- false
                
                let rowCount = colBodyRange.rowCount |> int
                // create a format column to insert
                let formats = createValueMatrix 1 rowCount format
                
                // add unit format to previously created main column
                colBodyRange.numberFormat <- formats
                
                r.enableEvents <- true
                
                // return msg
                "Info",sprintf "Format of %s was changed to %s" colName format
            )

            return res
        }
        
        
    )

let changeTableColumnsFormat (colAndFormatList:(string*string) list) =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName()

            let colNames = colAndFormatList |> List.map fst
            let formats = colAndFormatList |> List.map snd
            // Ref. 2 
            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let annotationTable = sheet.tables.getItem(annotationTable)

            // get ranged of main column that was previously created
            let colBodyRanges =
                colNames
                |> List.map (fun colName ->
                    let colBodyRange = (annotationTable.columns.getItem (U2.Case2 colName)).getDataBodyRange()
                    let _ = colBodyRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
                    colBodyRange
                )

            // Ref. 1
            let r = context.runtime.load(U2.Case1 "enableEvents")

            let! res = context.sync().``then``( fun _ ->

                 r.enableEvents <- false

                 let formatCols() =
                    List.map2 (fun (colBodyRange:Excel.Range) format ->
                        let rowCount = colBodyRange.rowCount |> int
                        let formats = createValueMatrix 1 rowCount format
                        colBodyRange.numberFormat <- formats
                    ) colBodyRanges formats

                 // create a format column to insert

                 // add unit format to previously created main column

                 let _ = formatCols()

                 r.enableEvents <- true

                 // return msg
                 "Info",sprintf "Columns were updated with related format: %A" colAndFormatList
            )

            return res
        }

    )

// Reform this to onSelectionChanged (Even though we now know how to add eventHandlers we do not know how to pass info from handler to Swate app).
/// This function will parse the header of a selected column to check for a parent ontology, which will then be used for a isA-directed term search.
/// Any found parent ontology will also be displayed in a static field before the term search input field.
let getParentTerm () =
    Excel.run (fun context ->

        promise {
            let! annotationTable = getActiveAnnotationTableName()
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
            return res
        }
    )

/// This is used to insert terms into selected cells.
/// 'term' is the value that will be written into the main column.
/// 'termBackground' needs to be spearate from 'term' in case the user uses the fill function for a custom term.
/// Should the user write a real term with this function 'termBackground'.isSome and can be used to fill TSR and TAN.
let fillValue (term,termBackground:Shared.DbDomain.Term option) =
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
                "Info",sprintf "Insert %A %Ax" term nextColsRange.values.Count
            )

            return res
        }
    )

/// This is used to create a full representation of all building blocks in the table. This representation is then split into unit building blocks and regular building blocks.
/// These are then filtered for search terms and aggregated into an 'SearchTermI []', which is used to search the database for missing values.
/// 'annotationTable'' gets passed by 'tryFindActiveAnnotationTable'.
let createSearchTermsIFromTable () =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName()

            // Ref. 2
            let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

            let! res= context.sync().``then``( fun _ ->
                
                /// Sort all columns into building blocks.
                let buildingBlocks =
                    getBuildingBlocks annoHeaderRange annoBodyRange

                /// Filter for only building blocks with ontology (indicated by having a TSR and TAN).
                let buildingBlocksWithOntology =
                    buildingBlocks |> Array.filter (fun x -> x.TSR.IsSome && x.TAN.IsSome)

                /// Combine search types
                let allSearches = sortBuildingBlocksValuesToSearchTerm buildingBlocksWithOntology

                /// Return the name of the table and all search types
                annotationTable,allSearches
            )
            return res
        }

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
                                elif term.SearchQuery.Name = "" then
                                    let t = ""
                                    let ont = ""
                                    let accession = ""
                                    t, ont, accession
                                elif term.TermOpt = None then
                                    let t = term.SearchQuery.Name
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

            let! annotationTable = getActiveAnnotationTableName()
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

let writeProtocolToXml(protocol:GroupTypes.Protocol) =
    printfn "%A" protocol
    updateProtocolFromXml protocol false

let removeProtocolFromXml(protocol:GroupTypes.Protocol) =
    updateProtocolFromXml protocol true

/// This function ist used to parse the protocol xml and the table to building blocks and assign the correct
/// table protocol-group-header to each building block.
let updateProtocolGroupHeader () =
    Excel.run(fun context ->

        promise {
            let! annotationTable = getActiveAnnotationTableName()
            let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))
            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
   

            let annoHeaderRange,annoBodyRange = getBuildingBlocksPreSync context annotationTable
            let _ = annoHeaderRange.load(U2.Case1 "rowIndex")
            let groupHeader = annoHeaderRange.getRowsAbove(1.)

            let! xmlParsed = getCustomXml customXmlParts context
            let! buildingBlocks = context.sync().``then``( fun e -> getBuildingBlocks annoHeaderRange annoBodyRange )
            let currentProtocolGroup = getSwateProtocolGroupForCurrentTable annotationTable activeSheet.name xmlParsed

            let! applyGroups = promise {

                let protocolsForCurrentTableSheet =
                    if currentProtocolGroup.IsSome then
                        currentProtocolGroup.Value.Protocols
                    else
                        []

                let getGroupHeaderIndicesForProtocol (buildingBlocks:BuildingBlock []) (protocol:Xml.GroupTypes.Protocol) =
                    let buildingBlockOpts = tryFindSpannedBuildingBlocks protocol buildingBlocks
                    // caluclate list of indices fro group blocks
                    if buildingBlockOpts.IsSome then
                        let getStartAndEnd (mainColIndices:int list) =
                            let startInd = List.min mainColIndices
                            let endInd   =
                                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
                                let max = List.max mainColIndices |> float
                                findIndexNextNotHiddenCol headerVals max |> int
                            startInd,endInd-1
                        let bbColNumberAndIndices =
                            buildingBlockOpts.Value
                            |> List.map (fun bb ->
                                let nOfCols =
                                    if bb.hasCompleteTSRTAN |> not then
                                        1
                                    elif bb.hasCompleteTSRTAN && bb.hasCompleteUnitBlock |> not then
                                        3
                                    elif bb.hasCompleteTSRTAN && bb.hasCompleteUnitBlock then
                                        6
                                    else failwith (sprintf "Swate encountered an unknown column pattern for building block: %s " bb.MainColumn.Header.Value.Header) 
                                bb.MainColumn.Index, nOfCols
                            )
                            |> List.sortBy fst
                        let rec sortIntoBlocks (iteration:int) (currentGroupIterator:int) (bbColNumberAndIndices:(int*int) list) (collector:(int*int*int) list) =
                            if iteration >= bbColNumberAndIndices.Length then
                                collector
                            elif iteration = 0 then
                                let currentInd, currentN = List.item iteration bbColNumberAndIndices
                                sortIntoBlocks 1 currentGroupIterator bbColNumberAndIndices ((currentGroupIterator,currentInd,currentN)::collector)
                            else
                                let currentColIndex, currentNOfCols = List.item iteration bbColNumberAndIndices
                                let lastGroup, lastColIndex, lastNOfCols =
                                    collector
                                    |> List.sortBy (fun (group,colIndex,nOfCols) -> colIndex)
                                    |> List.last
                                /// - 1 as the lastColIndex is already the mainColumn
                                let lastBBEndInd = lastColIndex + lastNOfCols
                                if lastBBEndInd = currentColIndex then
                                    sortIntoBlocks (iteration+1) currentGroupIterator bbColNumberAndIndices ((currentGroupIterator,currentColIndex,currentNOfCols)::collector)
                                elif lastBBEndInd > currentColIndex then
                                    failwith (sprintf "Swate encountered an unknown building block pattern (Error: SIB%i-%i)" lastBBEndInd currentColIndex)
                                else 
                                    let newGroupIterator = currentGroupIterator + 1 
                                    sortIntoBlocks (iteration+1) newGroupIterator bbColNumberAndIndices ((newGroupIterator,currentColIndex,currentNOfCols)::collector)
                        let mainColIndiceBlocks =
                            sortIntoBlocks 0 0 bbColNumberAndIndices []
                            |> List.groupBy (fun (group,colInd,nOfCols) -> group)
                            |> List.map (fun x ->
                                snd x |> List.map (fun (group,colInd,nOfCols) -> colInd)
                            )
                            |> List.map getStartAndEnd
                            |> List.sortByDescending fst
                        Some mainColIndiceBlocks
                    else
                        None

                /// 'tableStartIndex' -> let tableRange = annotationTable.getRange().columnIndex
                /// 'rangeStartIndex' -> range.columnIndex
                let recalculateColIndex tableStartIndex rangeStartIndex =
                    let tableRangeColIndex = tableStartIndex
                    let selectColIndex = rangeStartIndex
                    selectColIndex + tableRangeColIndex

                let recalculateColIndexToTable rangeStartIndex =
                    recalculateColIndex annoHeaderRange.columnIndex rangeStartIndex

                let! cleanGroupHeaderFormat = cleanGroupHeaderFormat groupHeader context

                let! group =
                    protocolsForCurrentTableSheet
                    |> List.map (fun protocol ->
                        let startEndIndices = getGroupHeaderIndicesForProtocol buildingBlocks protocol
                        promise {

                            if startEndIndices.IsSome then

                                let! startDiffList =
                                    startEndIndices.Value
                                    |> List.sortByDescending fst
                                    |> List.map (fun (startInd,endInd ) ->
                                        promise {

                                            let startInd,endInd = startInd |> float |> recalculateColIndexToTable, endInd |> float |> recalculateColIndexToTable
                                            let! diff =
                                                context.sync().``then``(fun e ->
                                                    /// +1 to also include the endcolumn, without it would end too early.
                                                    let diff = (endInd - startInd) + 1.
                                                    let getProtocolGroupHeaderRange = activeSheet.getRangeByIndexes(annoHeaderRange.rowIndex-1., startInd, 1., diff)
                                                    getProtocolGroupHeaderRange.merge()
                                                    diff
                                                )
                                            return (startInd, diff)
                                        }

                                    ) |> Promise.all

                                let! insertValue = promise {

                                    let! mergedHeaders =
                                        context.sync().``then``(fun e -> [|
                                            for start,diff in startDiffList do
                                                let range = activeSheet.getRangeByIndexes(annoHeaderRange.rowIndex-1., start, 1., diff)
                                                let _ = range.load (U2.Case1 "values")
                                                yield
                                                    int diff, range
                                                    
                                        |])
                                    
                                    let! insertValue =
                                        context.sync().``then``(fun e ->
                                            mergedHeaders
                                            |> Array.map (fun (n,mergedHeader) ->
                                                    let v = protocol.Id |> box |> Some
                                                    let nV =
                                                        [|
                                                            Array.init n (fun _ -> v) |> ResizeArray
                                                        |] |> ResizeArray
                                                    mergedHeader.values <- nV
                                                )
                                        )

                                    return sprintf "%A" protocol.Id
                                }

                                return ""
                                    
                            else
                                // REMOVE INCOMPLETE PROTOCOL
                                printfn "REMOVE!"
                                let! remove = removeProtocolFromXml protocol
                                return sprintf "%A" remove

                        }
                    ) |> Promise.Parallel
                return group
            }

            let! format = formatGroupHeaderForRange groupHeader context

            return ("info", "Update Protocol Header")
        }
    )

let writeTableValidationToXml(tableValidation:ValidationTypes.TableValidation,currentSwateVersion:string) =
    Excel.run(fun context ->

        // Update DateTime 
        let newTableValidation = {
            tableValidation with
                // This line is used to give freshly created TableValidations the current Swate Version
                SwateVersion = if tableValidation.SwateVersion = "" then currentSwateVersion else tableValidation.SwateVersion
                DateTime = System.DateTime.Now.ToUniversalTime()
            }

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
    
        promise {
    
            let! xmlParsed = getCustomXml customXmlParts context
    
            let nextCustomXml = updateSwateValidation newTableValidation xmlParsed

            let nextCustomXmlString = nextCustomXml |> OfficeInterop.HelperFunctions.xmlElementToXmlString
                        
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
                    "Update Validation Scheme with '%s - %s' @%s"
                    newTableValidation.AnnotationTable.Worksheet
                    newTableValidation.AnnotationTable.Name
                    ( newTableValidation.DateTime.ToString("yyyy-MM-dd HH:mm") )
        }
    )

let addTableValidationToExisting (tableValidation:ValidationTypes.TableValidation, colNames: string list) =
    Excel.run(fun context ->
        printfn "START ADDING TABLEVALIDATION"
        let getBaseName (colHeader:string) =
            let parsedHeader = parseColHeader colHeader
            let ont = if parsedHeader.Ontology.IsSome then sprintf " [%s]" parsedHeader.Ontology.Value.Name else ""
            sprintf "%s%s" parsedHeader.CoreName.Value ont

        let newColNameMap =
            colNames |> List.map (fun x ->
                getBaseName x, x
            )
            |> Map.ofList
        printfn "%A" newColNameMap
        printfn "%A" tableValidation
        //failwith (sprintf "%A" tableValidation)

        let updateColumnValidationColNames =
            tableValidation.ColumnValidations
            |> List.filter (fun x -> x.ColumnHeader <> ColumnCoreNames.Shown.Source && x.ColumnHeader <> ColumnCoreNames.Shown.Sample)
            |> List.map (fun previousColVal ->
                let baseName = getBaseName previousColVal.ColumnHeader
                let newName =
                    newColNameMap.[baseName]
                {previousColVal with ColumnHeader = newName}
            )

        // Update DateTime 
        let newTableValidation = {
            tableValidation with
                DateTime = System.DateTime.Now.ToUniversalTime()
                ColumnValidations = updateColumnValidationColNames
            }

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
    
        promise {
    
            let! xmlParsed = getCustomXml customXmlParts context

            let currentTableValidationOpt = getSwateValidationForCurrentTable newTableValidation.AnnotationTable.Name newTableValidation.AnnotationTable.Worksheet xmlParsed

            let updatedTableValidation =
                if currentTableValidationOpt.IsSome then
                    let previousTableValidation = currentTableValidationOpt.Value
                    {previousTableValidation with
                        ColumnValidations = newTableValidation.ColumnValidations@previousTableValidation.ColumnValidations |> List.sortBy (fun x -> x.ColumnAdress)
                        SwateVersion = newTableValidation.SwateVersion
                        DateTime = newTableValidation.DateTime
                        
                    }
                else
                    newTableValidation

            let nextCustomXml = updateSwateValidation updatedTableValidation xmlParsed

            let nextCustomXmlString = nextCustomXml |> OfficeInterop.HelperFunctions.xmlElementToXmlString

            let! deleteXml =
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Array.ofSeq |> Array.map (fun x -> x.delete() )
                    xmls
                )

            let! reloadedCustomXml =
                context.sync().``then``(fun e ->
                    let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
                    workbook.customXmlParts
                )

            let! addNext =
                context.sync().``then``(fun e ->
                    reloadedCustomXml.add(nextCustomXmlString)
                )

            // This will be displayed in activity log
            return
                "Info",
                sprintf
                    "Update Validation Scheme with '%s - %s' @%s"
                    newTableValidation.AnnotationTable.Worksheet
                    newTableValidation.AnnotationTable.Name
                    ( newTableValidation.DateTime.ToString("yyyy-MM-dd HH:mm") )
        }
    )

/// This function is used to add unit reference columns to an existing building block without unit reference columns
let addUnitToExistingBuildingBlock (format:string option,unitAccessionOpt:string option) =
    Excel.run(fun context ->

        promise {

            let! annotationTable = getActiveAnnotationTableName()

            let sheet = context.workbook.worksheets.getActiveWorksheet()
            let table = sheet.tables.getItem(annotationTable)

            // Ref. 2

            // This is necessary to place new columns next to selected col
            let annoHeaderRange = table.getHeaderRowRange()
            let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"rowIndex"|]))

            let tableRange = table.getRange()
            let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))

            let selectedRange = context.workbook.getSelectedRange()
            let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))

            // Ref. 2
            let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

            let! selectedBuildingBlock =
                BuildingBlockTypes.findSelectedBuildingBlock selectedRange annoHeaderRange annoBodyRange context

            let! res = context.sync().``then``( fun _ ->

                    if selectedBuildingBlock.TAN.IsNone || selectedBuildingBlock.TSR.IsNone then
                        failwith (
                            sprintf
                                "Swate can only add a unit to columns of the type: %s, %s, %s."
                                OfficeInterop.Types.ColumnCoreNames.Shown.Parameter
                                OfficeInterop.Types.ColumnCoreNames.Shown.Characteristics
                                OfficeInterop.Types.ColumnCoreNames.Shown.Factor
                        )
                        
                    // This is necessary to skip over hidden cols
                    /// Get an array of the headers
                    let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq

                    let allColHeaders =
                        headerVals
                        |> Array.choose id
                        |> Array.map string

                    let unitColumnResult =
                        if selectedBuildingBlock.Unit.IsSome then
                            updateUnitColumns allColHeaders annoHeaderRange (float selectedBuildingBlock.MainColumn.Index) format unitAccessionOpt
                        else
                            createUnitColumns allColHeaders table (float selectedBuildingBlock.MainColumn.Index) (int tableRange.rowCount) format unitAccessionOpt

                    let maincolName = selectedBuildingBlock.MainColumn.Header.Value.Header

                    /// If unit block was added then return some msg information
                    //let unitColCreationMsg = if unitColumnResult.IsSome then fst unitColumnResult.Value else ""
                    let unitColFormat = if unitColumnResult.IsSome then snd unitColumnResult.Value else "0.00"

                    maincolName, unitColFormat //, unitColCreationMsg
            )
            return res
        }
    )

let getAllValidationXmlParsed() =
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
                    |> Array.map (fun x -> Shared.AnnotationTable.create x.name x.worksheet.name)

                tableNames
            )

            let! xmlParsed = getCustomXml customXmlParts context

            let tableValidations = getAllSwateTableValidation xmlParsed
            
            return (tableValidations, allTables)
        }
    )

let getActiveProtocolGroupXmlParsed() =
    Excel.run(fun context ->
    
        promise {

            let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))
            let! annotationTable = getActiveAnnotationTableName()

            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

            let! xmlParsed = getCustomXml customXmlParts context

            let protocolGroup = getSwateProtocolGroupForCurrentTable annotationTable activeSheet.name xmlParsed

            return protocolGroup

        }
    )

let getAllProtocolGroupXmlParsed() =
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
                    |> Array.map (fun x -> Shared.AnnotationTable.create x.name x.worksheet.name)

                tableNames
            )

            let! xmlParsed = getCustomXml customXmlParts context

            let protocolGroups = getAllSwateProtocolGroups xmlParsed
            
            return (protocolGroups, allTables)
        }
    )


/// This function aims to update a protocol with a newer version from the db. To do this with minimum user friction we want the following:
/// Keep all already existing building blocks that still exist in the new version. By doing this we keep already filled in values.
/// Remove all building blocks that are not part of the new version.
/// Add all new building blocks.
// Of couse this is best be done by using already existing functions. Therefore we try the following. Return information necessary to use:
// Msg 'AddAnnotationBlocks' -> this will add all new blocks that are mentioned in 'minimalBuildingBlocks', add validationXml to existing and also add protocol xml.
// 'Remove building block' functionality by passing the correct indices
let updateProtocolByNewVersion (prot:OfficeInterop.Types.Xml.GroupTypes.Protocol, dbTemplate:Shared.ProtocolTemplate) =
    Excel.run(fun context ->
    
        promise {
    
            let! annotationTable = getActiveAnnotationTableName()
    
            // Ref. 2
            let activeWorksheet = context.workbook.worksheets.getActiveWorksheet().load(U2.Case1 "name")
            let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable
    
            //let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
            //let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))
    
            //let! xmlParsed = getCustomXml customXmlParts context
            //let currentProtocolGroup = getSwateValidationForCurrentTable annotationTable activeWorksheet.name xmlParsed
    
            let! allBuildingBlocks =
                context.sync().``then``( fun _ ->
                    let buildingBlocks = getBuildingBlocks annoHeaderRange annoBodyRange
    
                    buildingBlocks 
                )

            let filterBuildingBlocksForProtocol =
                allBuildingBlocks |> Array.filter (fun bb ->
                    prot.SpannedBuildingBlocks |> List.exists (fun spannedBB -> spannedBB.ColumnName = bb.MainColumn.Header.Value.Header)
                )

            let minBuildingBlocksInfoDB = dbTemplate.TableXml |> MinimalBuildingBlock.ofExcelTableXml |> snd

            let minimalBuildingBlocksToAdd =
                minBuildingBlocksInfoDB
                |> List.filter (fun minimalBB ->
                    filterBuildingBlocksForProtocol
                    |> Array.exists (fun bb -> minimalBB = MinimalBuildingBlock.ofBuildingBlockWithoutValues false bb)
                    |> not
                )

            let buildingBlocksToRemove =
                filterBuildingBlocksForProtocol
                |> Array.filter (fun x ->
                    minBuildingBlocksInfoDB
                    |> List.exists (fun minimalBB -> minimalBB = MinimalBuildingBlock.ofBuildingBlockWithoutValues false x)
                    |> not
                )

            let alreadyExistingBuildingBlocks =
                filterBuildingBlocksForProtocol
                |> Array.filter (fun bb ->
                    buildingBlocksToRemove
                    |> Array.contains bb
                    |> not
                )
                |> Array.map (fun bb ->
                     MinimalBuildingBlock.ofBuildingBlockWithoutValues true bb
                     |> fun minBB -> {minBB with MainColumnName = bb.MainColumn.Header.Value.Header}
                )
                |> List.ofArray

            let! remove =
                removeAnnotationBlocks annotationTable buildingBlocksToRemove

            let! reloadBuildingBlocks =
                let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable

                let allBuildingBlocks =
                    context.sync().``then``( fun _ ->
                        let buildingBlocks = getBuildingBlocks annoHeaderRange annoBodyRange
    
                        buildingBlocks 
                    )

                allBuildingBlocks

            let filterReloadedBuildingBlocksForProtocol =
                reloadBuildingBlocks |> Array.filter (fun bb ->
                    prot.SpannedBuildingBlocks |> List.exists (fun spannedBB -> spannedBB.ColumnName = bb.MainColumn.Header.Value.Header)
                )

            let table = activeWorksheet.tables.getItem(annotationTable)

            //Auto select place to add new building blocks.
            let! selectCorrectIndex = context.sync().``then``(fun e ->
                let lastInd = filterReloadedBuildingBlocksForProtocol |> Array.map (fun bb -> bb.MainColumn.Index) |> Array.max |> float

                table.getDataBodyRange().getColumn(lastInd).select()
            )

            let validationType =
                dbTemplate.CustomXml
                |> ValidationTypes.TableValidation.ofXml
                |> Some

            let protocol =
                let id = dbTemplate.Name
                let version = dbTemplate.Version
                /// This could be outdated and needs to be updated during Msg-handling
                let swateVersion = prot.SwateVersion
                GroupTypes.Protocol.create id version swateVersion [] annotationTable activeWorksheet.name

            /// Need to connect both again. 'alreadyExistingBuildingBlocks' is marked as already existing and is only passed to remain info about 
            let minimalBuildingBlockInfo =
                minimalBuildingBlocksToAdd@alreadyExistingBuildingBlocks

            return minimalBuildingBlockInfo, protocol, validationType
        }
    )


let removeXmlType(xmlType:XmlTypes) =
    Excel.run(fun context ->

        promise {

            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

            let! xmlParsed = getCustomXml customXmlParts context

            let nextCustomXml =
                match xmlType with
                | ValidationType tableValidation ->
                    removeSwateValidation tableValidation xmlParsed
                | GroupType protGroup ->
                    removeSwateProtocolGroup protGroup xmlParsed
                | ProtocolType protocol ->
                    removeSwateProtocol protocol xmlParsed

            let nextCustomXmlString = nextCustomXml |> OfficeInterop.HelperFunctions.xmlElementToXmlString

            let! deleteXml =
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Array.ofSeq |> Array.map (fun x -> x.delete() )
                    xmls
                )

            let! reloadedCustomXml =
                context.sync().``then``(fun e ->
                    let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
                    workbook.customXmlParts
                )

            let! addNext =
                context.sync().``then``(fun e ->
                    reloadedCustomXml.add(nextCustomXmlString)
                )

            return (sprintf "Removed %s" xmlType.toStringRdb)
        }
    )

let updateAnnotationTableByXmlType(prevXmlType:XmlTypes, nextXmlType:XmlTypes) =

    Excel.run(fun context ->

        promise {

            let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
            let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

            let! xmlParsed = getCustomXml customXmlParts context

            let nextCustomXml =
                match prevXmlType,nextXmlType with
                | XmlTypes.ValidationType prevV, XmlTypes.ValidationType nextV ->
                    replaceValidationByValidation prevV nextV xmlParsed
                | XmlTypes.GroupType prevV, XmlTypes.GroupType nextV ->
                    replaceProtGroupByProtGroup prevV nextV xmlParsed
                | XmlTypes.ProtocolType prevV, XmlTypes.ProtocolType nextV ->
                    failwith "Not coded yet"
                | anyElse1, anyElse2 -> failwith "Swate encountered different XmlTypes while trying to reassign custom xml to new annotation table - worksheet combination."

            let nextCustomXmlString = nextCustomXml |> OfficeInterop.HelperFunctions.xmlElementToXmlString

            let! deleteXml =
                context.sync().``then``(fun e ->
                    let items = customXmlParts.items
                    let xmls = items |> Array.ofSeq |> Array.map (fun x -> x.delete() )
                    xmls
                )

            let! reloadedCustomXml =
                context.sync().``then``(fun e ->
                    let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
                    workbook.customXmlParts
                )

            let! addNext =
                context.sync().``then``(fun e ->
                    reloadedCustomXml.add(nextCustomXmlString)
                )

            return (sprintf "Updated %s BY %s" prevXmlType.toStringRdb nextXmlType.toStringRdb)
        }
    )

let createPointerJson() =
    Excel.run(fun context ->

    let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))
        
    promise {
        let! annotationTable = getActiveAnnotationTableName()
        let workbook = context.workbook.load(U2.Case1 "name")

        let! json = context.sync().``then``(fun e -> 
            [
                "name"          , Fable.SimpleJson.JString  ""
                "version"       , Fable.SimpleJson.JString  ""     
                "author"        , Fable.SimpleJson.JString  ""
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