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
open SwateInteropTypes
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


/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
let exampleExcelFunction () =
    Excel.run(fun context ->

        context.sync()
            .``then``( fun _ ->
                
                sprintf "Test output" 
            )
    )

/// This is not used in production and only here for development. Its content is always changing to test functions for new features.
let exampleExcelFunction2 () =
    Excel.run(fun context ->

        context.sync()
            .``then``( fun _ ->
                
                sprintf "Test output 2" 
            )
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
        let _ = tableRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount";"address"; ])))

        let r = context.runtime.load(U2.Case1 "enableEvents")

        //sync with proxy objects after loading values from excel
        context.sync()
            .``then``( fun _ ->

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
                let adaptedRange = activeSheet.getRangeByIndexes(tableRange.rowIndex,tableRange.columnIndex,tableRange.rowCount,2.)

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

                /// Should event handlers be active, then add them to the new table, otherwise don't.
                /// If the storage map is empty then eventhanderls should be deactivated.
                if EventHandlerStates.adaptHiddenColsHandlerList.IsEmpty then
                    ()
                else
                    EventHandlerStates.adaptHiddenColsHandlerList <-
                        EventHandlerStates.adaptHiddenColsHandlerList.Add (newName, annotationTable.onChanged.add(fun eventArgs -> adaptHiddenColsHandler (eventArgs,newName)) )

                r.enableEvents <- true

                /// Return info message
                SwateInteropTypes.Success newName, sprintf "Annotation Table created in [%s] with dimensions 2c x (%.0f + 1h)r" tableRange.address (tableRange.rowCount - 1.)
            )
            //.catch (fun e -> e |> unbox<System.Exception> |> fun x -> x.Message)
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
                let res = SwateInteropTypes.TryFindAnnoTableResult.exactlyOneAnnotationTable annoTables

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

/// This function is used to either add eventHandlers to all annotationTables or to remove all eventHanderls.
let toggleAdaptHiddenColsEventHandler () =
    /// Check if storage for eventHandlers is empty
    let isEmpty = EventHandlerStates.adaptHiddenColsHandlerList.IsEmpty
    /// If it is empty when the function is called then we want to add event handlers.
    if isEmpty then
        Excel.run(fun context ->

            /// This function recursevly adds eventHandlers to all elements of the 'tables' [] and stores the reference to the event handler in 'map'
            let rec addEventToTable (map:Map<string,OfficeExtension.EventHandlerResult<TableChangedEventArgs>>) ind (tables: Table []) =
                if ind > tables.Length-1 then
                    map
                else
                    let newMap = map.Add (tables.[ind].name, tables.[ind].onChanged.add(fun eventArgs -> adaptHiddenColsHandler (eventArgs,tables.[ind].name)))
                    addEventToTable newMap (ind+1) tables

            // Ref. 2

            let tableCollection = context.workbook.tables.load(propertyNames = U2.Case1 "items")

            context.sync()
                .``then``(fun _ ->

                    /// Get all annotationTables
                    let annoTables =
                        tableCollection.items
                        |> Seq.filter (fun x -> x.name.StartsWith "annotationTable")
                        |> Array.ofSeq

                    /// Add eventHandlers to all of them ...
                    let newHandlers = annoTables |> addEventToTable EventHandlerStates.adaptHiddenColsHandlerList 0
                    /// ... and store reference in event handler storage.
                    /// This is necessary as we need these objects to remove them (see 'removeHandler' below)
                    EventHandlerStates.adaptHiddenColsHandlerList <- newHandlers

                    /// Create message
                    let tableMessageStr = annoTables |> Seq.map (fun x -> x.name) |> String.concat ", " 

                    /// Return message in array due to how removing the handlers is structured.
                    /// (if .. then .. else needs same output.)
                    [|sprintf "Event handler added to tables: %s" tableMessageStr|]
                )
        )
    else
        /// creates a list of "Promises", which each remove one eventHandler and the reference from the event handler storage.
        let rec removeHandler ind promises (handlerArr:(string*OfficeExtension.EventHandlerResult<TableChangedEventArgs>) [])  =

            if ind > handlerArr.Length-1 then
                promises
            else
                /// get current handler from event handler storage
                let (name,handler):string*OfficeExtension.EventHandlerResult<TableChangedEventArgs> = handlerArr.[ind]

                /// Give handler.context as input for 'Excel.run' and remove it from the table and the event handler storage.
                let promise =
                    Excel.run(handler.context, fun context ->

                        let _ = handler.remove()

                        let newMap = EventHandlerStates.adaptHiddenColsHandlerList.Remove(name)

                        EventHandlerStates.adaptHiddenColsHandlerList <- newMap

                        context.sync()
                            .``then``(fun t ->
                                // As we will 'String.concat' these messages later we want the first message to give more context ...
                                if ind = 0 then
                                    sprintf "Event handler removed from tables: %s" name
                                // ... and every other message to just contain the table name.
                                else
                                    name
                            )
                    )

                // iterate through the whole event handler storage
                removeHandler (ind+1) (promise::promises) handlerArr

        // create all promises to remove event handlers
        removeHandler 0 [] (Map.toArray EventHandlerStates.adaptHiddenColsHandlerList)
        // this is done to create readable output.
        |> List.rev
        // execute all promises
        |> Promise.Parallel 

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
/// As this function creates a complete representation of the table and then searches on it. Should we decide to keep the function then i will add more inline comments.
let getTableRepresentation(annotationTable) =
    Excel.run(fun context ->
    let sheet = context.workbook.worksheets.getActiveWorksheet()
    let annotationTable = sheet.tables.getItem(annotationTable)
    let annoHeaderRange = annotationTable.getHeaderRowRange()
    let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))
    context.sync().``then``(
        fun _ ->
            let headerVals =
                annoHeaderRange.values.[0]
                |> Array.ofSeq
                |> Array.choose id
                |> Array.map string
            let parsedHeaders =
                headerVals |> Array.map parseColHeader
            let baseColRepresentation =
                parsedHeaders
                |> Array.map (fun header ->
                    let nColRep = SwateInteropTypes.ColumnRepresentation.init(header=header.Header)
                    { nColRep with
                        ParentOntology = header.Ontology
                        TagArray = if header.TagArr.IsSome then header.TagArr.Value else [||]
                    }
                )
            let filterOutHiddenCols (colRepArr:SwateInteropTypes.ColumnRepresentation []) =
                colRepArr
                |> Array.filter (fun x -> x.TagArray |> ((Array.contains ColumnTags.HiddenTag) >> not)  )
            let colReps =
                baseColRepresentation
                |> filterOutHiddenCols
            colReps, "Update table representation."
        )
    )

/// This function is used to add a new building block to the active annotationTable.
let addAnnotationBlock (annotationTable,colName:string,format:(string*(string option)) option) =

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
    let hiddenColAttributes (parsedColHeader:ColHeader) (id:int) =
        let coreName =
            match parsedColHeader.Ontology, parsedColHeader.CoreName with
            | Some o , _ -> o
            | None, Some cn -> cn
            | _ -> parsedColHeader.Header
        match id with
        | 1 ->
            (sprintf "[%s] (#h)" coreName)
        | _ ->
            (sprintf "[%s] (#%i; #h)" coreName id)

    /// This will create the column header attributes for a unit block.
    /// as unit always has to be a term and cannot be for example "Source" or "Sample", both of which have a differen format than for exmaple "Parameter [TermName]",
    /// we only need one function to generate id and attributes and bring the unit term in the right format.
    let unitColAttributes (unitTermInfo:string*string option) (id:int) =
        let unitTermName = fst unitTermInfo
        let unitAccession = if (snd unitTermInfo).IsNone then "" else (snd unitTermInfo).Value
        match id with
        | 1 ->
            sprintf "[%s] (#h; #u%s)" unitTermName unitAccession
        | _ ->
            sprintf "[%s] (#%i; #h; #u%s)" unitTermName id unitAccession

    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)

        // Ref. 2

        // This is necessary to place new columns next to selected col
        let tables = annotationTable.columns.load(propertyNames = U2.Case1 "items")
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"|]))
        let tableRange = annotationTable.getRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"])))
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case1 "columnIndex")

        // Ref. 1

        let r = context.runtime.load(U2.Case1 "enableEvents")

        context.sync().``then``( fun _ ->

            r.enableEvents <- false

            // Ref. 3
            /// This is necessary to place new columns next to selected col.
            /// selected ranged returns indices always from a worksheet perspective but we need the related table index. This is calculated here.
            let newBaseColIndex =
                let tableHeaderRangeColIndex = annoHeaderRange.columnIndex
                let selectColIndex = range.columnIndex
                let diff = selectColIndex - tableHeaderRangeColIndex |> int
                let vals = tables.items
                let maxLength = vals.Count-1
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
                                || existingHeader = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader int)
                                || existingHeader = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader int)
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
                    index = newBaseColIndex',
                    values = U4.Case1 col,
                    name = mainColName colName newId
                )

            // create TSR
            let createdCol2() =
                annotationTable.columns.add(
                    index = newBaseColIndex'+1.,
                    values = U4.Case1 col,
                    name = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader newId)
                )

            // create TAN
            let createdCol3() =
                annotationTable.columns.add(
                    index = newBaseColIndex'+2.,
                    values = U4.Case1 col,
                    name = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader newId)
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
                if format.IsSome then
                    let findNewIdForUnit() =
                        let rec loopingCheck int =
                            let isExisting =
                                allColHeaders
                                // Should a column with the same name already exist, then count up the id tag.
                                |> Array.exists (fun existingHeader ->
                                    // We don't need to check TSR or TAN, because the main column always starts with "Unit"
                                    existingHeader = sprintf "Unit %s" (unitColAttributes format.Value int)
                                )
                            if isExisting then
                                loopingCheck (int+1)
                            else
                                int
                        loopingCheck 1

                    let newUnitId = findNewIdForUnit()

                    /// create unit main column
                    let createdUnitCol1 =
                        annotationTable.columns.add(
                            index = newBaseColIndex'+3.,
                            values = U4.Case1 col,
                            name = sprintf "Unit %s" (unitColAttributes format.Value newUnitId)
                        )

                    /// create unit TSR
                    let createdUnitCol2 =
                        annotationTable.columns.add(
                            index = newBaseColIndex'+4.,
                            values = U4.Case1 col,
                            name = sprintf "Term Source REF %s" (unitColAttributes format.Value newUnitId)
                        )

                    /// create unit TAN
                    let createdUnitCol3 =
                        annotationTable.columns.add(
                            index = newBaseColIndex'+5.,
                            values = U4.Case1 col,
                            name = sprintf "Term Accession Number %s" (unitColAttributes format.Value newUnitId)
                        )

                    Some (
                        sprintf " Added specified unit: %s" (fst format.Value),
                        sprintf "0.00 \"%s\"" (fst format.Value)
                    )
                else
                    None

            /// If unit block was added then return some msg information
            let unitColCreationMsg = if createUnitColsIfNeeded.IsSome then fst createUnitColsIfNeeded.Value else ""
            let unitColFormat = if createUnitColsIfNeeded.IsSome then snd createUnitColsIfNeeded.Value else "0.00"

            r.enableEvents <- true
            /// return main col names, unit column format and a message. The first two params are used in a follow up message (executing 'changeTableColumnFormat')
            mainColName colName newId, unitColFormat, sprintf "%s column was added.%s" colName unitColCreationMsg
        )
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
                        tableRange.values.[0].[newColIndex]
                // return header of selected col
                value
            )
    )

/// This is used to insert terms into.
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
                                let replace = Shared.URLs.TermAccessionBaseUrl + "/" + term.Accession.Replace(@":",@"_")
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

/// This is used to create a full representation of all building blocks in the table and return it to the app.
/// 'annotationTable'' gets passed by 'tryFindActiveAnnotationTable'.
let createSearchTermsFromTable (annotationTable') =
    Excel.run(fun context ->

        // Ref. 2
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable')
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
        let annoBodyRange = annotationTable.getDataBodyRange()
        let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore

        context.sync()
            .``then``( fun _ ->
                /// Get the table by 'Columns [| Rows [|Values|] |]'
                let columnBodies =
                    annoBodyRange.values
                    |> viewRowsByColumns

                /// Write columns into 'BuildingBlockTypes.Column'
                let columns =
                    [|
                        // iterate over n of columns
                        for ind = 0 to (int annoHeaderRange.columnCount - 1) do
                            yield (
                                // Get column header and parse it
                                let header =
                                    annoHeaderRange.values.[0].[ind]
                                    |> fun x -> if x.IsSome then parseColHeader (string annoHeaderRange.values.[0].[ind].Value) |> Some else None
                                // Get column values and write them to 'BuildingBlockTypes.Cell'
                                let cells =
                                    columnBodies.[ind]
                                    |> Array.mapi (fun i cellVal ->
                                        let cellValue = if cellVal.IsSome then Some (string cellVal.Value) else None
                                        Cell.create i cellValue
                                    )
                                // Create column
                                Column.create ind header cells
                            )
                    |]

                /// Failsafe (1): it should never happen, that the nextColumn is a hidden column without an existing building block.
                let errorMsg1 (nextCol:Column) (buildingBlock:BuildingBlock option) =
                    failwith (
                        sprintf 
                            "Swate encountered an error while processing the active annotation table.
                            Swate found a hidden column (%s) without a prior main column (not hidden)."
                            nextCol.Header.Value.Header
                    )

                /// Hidden columns do only come with certain core names. The acceptable names can be found in OfficeInterop.Types.ColumnCoreNames.
                let errorMsg2 (nextCol:Column) (buildingBlock:BuildingBlock option) =
                    failwith (
                        sprintf
                            "Swate encountered an error while processing the active annotation table.
                            Swate found a hidden column (%s) with an unknown core name: %A"
                            nextCol.Header.Value.Header
                            nextCol.Header.Value.CoreName
                    )

                /// If a columns core name already exists for the current building block, then the block is faulty and needs userinput to be corrected.
                let errorMsg3 (nextCol:Column) (buildingBlock:BuildingBlock option) assignedCol =
                    failwith (
                        sprintf
                            "Swate encountered an error while processing the active annotation table.
                            Swate found a hidden column (%s) with a core name (%A) that is already assigned to the previous building block.
                            Building block main column: %s, already assigned column: %s"
                            nextCol.Header.Value.Header
                            nextCol.Header.Value.CoreName
                            buildingBlock.Value.MainColumn.Header.Value.Header
                            assignedCol
                    )

                /// Update current building block with new reference column. A ref col can be TSR, TAN and unit cols.
                let checkForHiddenColType (currentBlock:BuildingBlock option) (nextCol:Column) =
                    // Then we need to check if the nextCol is either a TSR, TAN or a unit column
                    match nextCol.Header.Value.CoreName.Value with
                    | ColumnCoreNames.Hidden.TermAccessionNumber ->
                        // Build in fail safes.
                        if currentBlock.IsNone then errorMsg1 nextCol currentBlock
                        if currentBlock.Value.TAN.IsSome then errorMsg3 nextCol currentBlock currentBlock.Value.TAN.Value.Header.Value.Header
                        // Update building block
                        let updateCurrentBlock =
                            { currentBlock.Value with
                                TAN =  Some nextCol } |> Some
                        updateCurrentBlock
                    | ColumnCoreNames.Hidden.TermSourceREF ->
                        // Build in fail safe.
                        if currentBlock.IsNone then errorMsg1 nextCol currentBlock
                        if currentBlock.Value.TSR.IsSome then errorMsg3 nextCol currentBlock currentBlock.Value.TSR.Value.Header.Value.Header
                        // Update building block
                        let updateCurrentBlock =
                            { currentBlock.Value with
                                TSR =  Some nextCol } |> Some
                        updateCurrentBlock
                    | ColumnCoreNames.Hidden.Unit ->
                        // Build in fail safe.
                        if currentBlock.IsSome then errorMsg3 nextCol currentBlock currentBlock.Value.MainColumn.Header.Value.Header
                        // Create unit building block
                        let newBlock = BuildingBlock.create nextCol None None None |> Some
                        newBlock 
                    | _ ->
                        // Build in fail safe.
                        errorMsg2 nextCol currentBlock

                // Building blocks are defined by one visuable column and an undefined number of hidden columns.
                // Therefore we iterate through the columns array and use every column without an `#h` tag as the start of a new building block.
                let rec sortColsIntoBuildingBlocks (index:int) (currentBlock:BuildingBlock option) (buildingBlockList:BuildingBlock list) =
                    // Exit case if we iterated through all columns
                    if index > (int annoHeaderRange.columnCount - 1) then
                        // Should we have a 'currentBuildingBlock' add it to the 'buildingBlockList' before returning it.
                        if currentBlock.IsSome then
                            currentBlock.Value::buildingBlockList
                        else
                            buildingBlockList
                    else
                        let nextCol = columns.[index]
                        // If the nextCol does not have an header it is empty and therefore skipped.
                        if
                            nextCol.Header.IsNone
                        then
                            sortColsIntoBuildingBlocks (index+1) currentBlock buildingBlockList
                        // If the nextCol.Header has no tag array or its tag array does NOT contain a hidden tag then it starts a new building block
                        elif
                            (nextCol.Header.Value.TagArr.IsSome && nextCol.Header.Value.TagArr.Value |> Array.contains ColumnTags.HiddenTag |> not)
                            || (nextCol.Header.IsSome && nextCol.Header.Value.TagArr.IsNone)
                        then
                            let newBuildingBlock = BuildingBlock.create nextCol None None None |> Some
                            // If there is a 'currentBlock' we add it to the list of building blocks ('buildingBlockList').
                            if currentBlock.IsSome then
                                sortColsIntoBuildingBlocks (index+1) newBuildingBlock (currentBlock.Value::buildingBlockList)
                            // If there is no currentBuildingBlock, e.g. at the start of this function we replace the None with the first building block.
                            else
                                sortColsIntoBuildingBlocks (index+1) newBuildingBlock buildingBlockList
                        // if the nextCol.Header has a tag array and it does contain a hidden tag then it is added to the currentBlock
                        elif
                            nextCol.Header.Value.TagArr.IsSome && nextCol.Header.Value.TagArr.Value |> Array.contains ColumnTags.HiddenTag
                        then
                            // There are multiple possibilities which column this is: TSR; TAN; Unit; Unit TSR; Unit TAN are the currently existing ones.
                            // We first check if there is NO unit tag in the header tag array
                            if nextCol.Header.Value.TagArr.Value |> Array.exists (fun x -> x.StartsWith ColumnTags.UnitTagStart) |> not then
                                let updateCurrentBlock = checkForHiddenColType currentBlock nextCol
                                sortColsIntoBuildingBlocks (index+1) updateCurrentBlock buildingBlockList
                            /// Next we check for unit columns in the scheme of `Unit [Term] (#h; #u...) | TSR [Term] (#h; #u...) | TAN [Term] (#h; #u...)`
                            elif nextCol.Header.Value.TagArr.Value |> Array.exists (fun x -> x.StartsWith ColumnTags.UnitTagStart) then
                                /// Please notice that we update the unit building block in the following function and not the core building block.
                                let updatedUnitBlock = checkForHiddenColType currentBlock.Value.Unit nextCol
                                /// Update the core building block with the updated unit building block.
                                let updateCurrentBlock = {currentBlock.Value with Unit = updatedUnitBlock} |> Some
                                sortColsIntoBuildingBlocks (index+1) updateCurrentBlock buildingBlockList
                            else
                                failwith "The tag array of the next column to process in 'sortColsIntoBuildingBlocks' can only contain a '#u' tag or not."
                        else
                            failwith (sprintf "The tag array of the next column to process in 'sortColsIntoBuildingBlocks' was not recognized as hidden or main column: %A." nextCol.Header)

                /// Sort all columns into building blocks.
                let buildingBlocks =
                    sortColsIntoBuildingBlocks 0 None []
                    |> List.rev
                    |> Array.ofList

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
                                cell.Value.IsSome, cell.Value.Value
                            )
                            // only keep cells with value and create InsertTerm types that will be passed to the server to get filled with a term option.
                            |> Array.choose (fun ((isSome,searchStr),cellArr) ->
                                if isSome && searchStr <> "" then
                                    let rowIndices = cellArr |> Array.map (fun cell -> cell.Index)
                                    Shared.SearchTermI.create tsrTanColIndices searchStr rowIndices
                                    |> Some
                                else
                                    None
                            )
                        /// We differentiate between building blocks with and without unit as unit building blocks will not contain terms as values but e.g. numbers.
                        /// In this case we do not want to search the database for the cell values but the parent ontology in the header.
                        /// This will then be used for TSR and TAN.
                        let fillTermConstructsWithUnit (bBlock:BuildingBlock) =
                            let searchStr = bBlock.MainColumn.Header.Value.Ontology.Value
                            let rowIndices =
                                bBlock.MainColumn.Cells
                                |> Array.map (fun x ->
                                   x.Index
                                )
                            [|Shared.SearchTermI.create tsrTanColIndices searchStr rowIndices|]
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
                            let searchString = unit.MainColumn.Header.Value.Ontology.Value
                            let colIndices = [|unit.MainColumn.Index; unit.TSR.Value.Index; unit.TAN.Value.Index|]
                            let rowIndices = unit.MainColumn.Cells |> Array.map (fun x -> x.Index)
                            Shared.SearchTermI.create colIndices searchString rowIndices
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
let UpdateTableBySearchTerms (annotationTable,insertTerms:SearchTermI []) =
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
                let foundTerms = insertTerms |> Array.filter (fun x -> x.TermOpt.IsSome)
                /// Insert terms into related cells for all stored row-/ col-indices
                let insert() =
                    foundTerms
                    // iterate over all found terms
                    |> Array.map (
                        fun insertTerm ->
                            /// Term search result from database
                            let t = insertTerm.TermOpt.Value
                            /// Get ontology and accession from Term.Accession
                            let ont, accession =
                                let a = t.Accession
                                let splitA = a.Split":"
                                let accession = Shared.URLs.TermAccessionBaseUrl + a.Replace(":","_")
                                splitA.[0], accession
                            /// Distinguish between core building blocks and unit buildingblocks.
                            let inputVals = [|
                                /// if the n of cols is 2 then it is a core building block.
                                if insertTerm.ColIndices.Length = 2 then
                                    createCellValueInput ont
                                    createCellValueInput accession
                                /// if the n of cols is 3 then it is a unit building block.
                                elif insertTerm.ColIndices.Length = 3 then
                                    createCellValueInput t.Name
                                    createCellValueInput ont
                                    createCellValueInput accession
                            |]

                            /// ATTENTION!! The following seems to be a strange interaction between office.js and fable.
                            /// In an example with 2 colIndices i had a mistake in the code to access: 'for i in 0 .. insertTerm.ColIndices.Length do'
                            /// so i actually accessed 3 colIndices which should have led to the classic 'System.IndexOutOfRangeException', but it didnt.
                            /// for 'inputVals.[2]' it returned 'undefined' and for 'insertTerm.ColIndices.[2]' it returned '0'.
                            /// This led to the first column to be erased for the same rows that were found to be replaced.

                            // iterate over all columns (in this case in form of the index of their array. as we need the index to access the correct 'inputVal' value
                            for i in 0 .. insertTerm.ColIndices.Length-1 do

                                // iterate over all rows and insert the correct inputVal
                                for rowInd in insertTerm.RowIndices do

                                    let cell = annoBodyRange.getCell(float rowInd, float insertTerm.ColIndices.[i])
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