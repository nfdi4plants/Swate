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
open AutoFillTypes

[<Emit("console.log($0)")>]
let consoleLog (message: string): unit = jsNative
        //ranges.format.fill.color <- "red"
        //let ranges = context.workbook.getSelectedRanges()
        //let x = ranges.load(U2.Case1 "address")

open System
open Fable.Core

let exampleExcelFunction () =
    Excel.run(fun context ->

        context.sync().
            ``then``(fun _ ->
                sprintf "Current Event Handlers: %A" EventHandlerStates.adaptHiddenColsHandlerList 
            )
    )

let exampleExcelFunction2 () =
    Excel.run(fun context ->

        context.sync()
            .``then``( fun _ ->
                
                sprintf "Test output" 
            )
    )

let createAnnotationTable ((allTableNames:String []),isDark:bool) =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()

        let tableRange = context.workbook.getSelectedRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray(["rowIndex"; "columnIndex"; "rowCount";"address"; ])))

        let style =
            if isDark then
                "TableStyleMedium15"
            else
                "TableStyleMedium7"

        let rec findNewTableName ind =
            let newTestName =
                if ind = 0 then "annotationTable" else sprintf "annotationTable%i" ind
            let existsAlready = allTableNames |> Array.exists (fun x -> x = newTestName)
            if existsAlready then
                findNewTableName (ind+1)
            else
                newTestName

        let newName = findNewTableName 0

        let r = context.runtime.load(U2.Case1 "enableEvents")

        //sync with proxy objects after loading values from excel
        context.sync()
            .``then``( fun _ ->

                r.enableEvents <- false

                let adaptedRange = sheet.getRangeByIndexes(tableRange.rowIndex,tableRange.columnIndex,tableRange.rowCount,2.)
                let annotationTable = sheet.tables.add(U2.Case1 adaptedRange,true)

                (annotationTable.columns.getItemAt 0.).name <- "Source Name"
                (annotationTable.columns.getItemAt 1.).name <- "Sample Name"

                annotationTable.name <- newName

                annotationTable.style <- style

                sheet.getUsedRange().format.autofitColumns()
                sheet.getUsedRange().format.autofitRows()

                if EventHandlerStates.adaptHiddenColsHandlerList.IsEmpty then
                    ()
                else
                    EventHandlerStates.adaptHiddenColsHandlerList <-
                        EventHandlerStates.adaptHiddenColsHandlerList.Add (newName, annotationTable.onChanged.add(fun eventArgs -> adaptHiddenColsHandler (eventArgs,newName)) )

                r.enableEvents <- true
                SwateInteropTypes.Success newName, sprintf "Annotation Table created in [%s] with dimensions 2c x (%.0f + 1h)r" tableRange.address (tableRange.rowCount - 1.)
            )
            //.catch (fun e -> e |> unbox<System.Exception> |> fun x -> x.Message)
    )

let tryFindActiveAnnotationTable() =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let t = sheet.load(U2.Case2 (ResizeArray[|"tables"|]))
        let tableItems = t.tables.load(propertyNames=U2.Case1 "items")
        context.sync()
            .``then``( fun _ ->
                let tables =
                    tableItems.items
                    |> Seq.toArray
                    |> Array.map (fun x -> x.name)
                let annoTables =
                    tables
                    |> Array.filter (fun x -> x.StartsWith "annotationTable")
                let res = SwateInteropTypes.TryFindAnnoTableResult.exactlyOneAnnotationTable annoTables
                // add event to active table
                res
        )
    )

/// This function returns the names of all tables in all worksheets.
let getTableInfoForAnnoTableCreation() =
    Excel.run(fun context ->
        let tableCol = context.workbook.tables.load(propertyNames=U2.Case2 (ResizeArray[|"tables"|]))
        let tables = tableCol.load(propertyNames = U2.Case1 "name")
        let activeSheet = context.workbook.worksheets.getActiveWorksheet()
        let t = activeSheet.load(U2.Case2 (ResizeArray[|"tables"|]))
        let activeTables = t.tables.load(propertyNames=U2.Case1 "items")

        let r = context.runtime.load(U2.Case1 "enableEvents")

        context.sync()
            .``then``( fun _ ->

                r.enableEvents <- false

                let tableNames =
                    tables.items
                    |> Seq.toArray
                    |> Array.map (fun x -> x.name)

                let annoTables =
                    activeTables.items
                    |> Seq.toArray
                    |> Array.map (fun x -> x.name)
                    |> Array.filter (fun x -> x.StartsWith "annotationTable")

                // fail the function if there are not exactly 0 annotation tables in the active worksheet
                let _ =
                    match annoTables.Length with
                    | x when x > 0 ->
                        r.enableEvents <- true
                        failwith "The active worksheet contains more than zero annotationTables. Please move them to other worksheets."
                    | 0 ->
                        annoTables
                    | _ ->
                        r.enableEvents <- true
                        failwith "The active worksheet contains a negative number of annotation tables. Obviously this cannot happen. Please report this as a bug to the developers."

                r.enableEvents <- true

                tableNames
        )
    )

let toggleAdaptHiddenColsEventHandler () =
    let isEmpty = EventHandlerStates.adaptHiddenColsHandlerList.IsEmpty
    if isEmpty then
        Excel.run(fun context ->

            let tableCollection = context.workbook.tables.load(propertyNames = U2.Case1 "items")

            let rec addEventToTable (map:Map<string,OfficeExtension.EventHandlerResult<TableChangedEventArgs>>) ind (tables: Table []) =
                if ind > tables.Length-1 then
                    map
                else
                    let newMap = map.Add (tables.[ind].name, tables.[ind].onChanged.add(fun eventArgs -> adaptHiddenColsHandler (eventArgs,tables.[ind].name)))
                    addEventToTable newMap (ind+1) tables

            context.sync()
                .``then``(fun t ->

                    let annoTables =
                        tableCollection.items
                        |> Seq.filter (fun x -> x.name.StartsWith "annotationTable")
                        |> Array.ofSeq

                    let newHandlers = annoTables |> addEventToTable EventHandlerStates.adaptHiddenColsHandlerList 0

                    EventHandlerStates.adaptHiddenColsHandlerList <- newHandlers

                    let tableMessageStr = annoTables |> Seq.map (fun x -> x.name) |> String.concat ", " 

                    [|sprintf "Event handler added to tables: %s" tableMessageStr|]
                )
        )
    else
        let rec removeHandler ind promises (handlerArr:(string*OfficeExtension.EventHandlerResult<TableChangedEventArgs>) [])  =

            if ind > handlerArr.Length-1 then
                promises
            else
                let (name,handler):string*OfficeExtension.EventHandlerResult<TableChangedEventArgs> = handlerArr.[ind]

                let promise =
                    Excel.run(handler.context, fun context ->

                        let _ = handler.remove()

                        let newMap = EventHandlerStates.adaptHiddenColsHandlerList.Remove(name)

                        EventHandlerStates.adaptHiddenColsHandlerList <- newMap

                        context.sync()
                            .``then``(fun t ->
                                if ind = 0 then
                                    sprintf "Event handler removed from tables: %s" name
                                else
                                    name
                            )
                    )

                removeHandler (ind+1) (promise::promises) handlerArr

        removeHandler 0 [] (Map.toArray EventHandlerStates.adaptHiddenColsHandlerList)
        |> List.rev
        |> Promise.Parallel 

let autoFitTable (annotationTable) =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
        let allCols = annotationTable.columns.load(propertyNames = U2.Case1 "items")
    
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values"|]))

        let r = context.runtime.load(U2.Case1 "enableEvents")

        context.sync().``then``(
            fun _ ->
                r.enableEvents <- false
                let allCols = allCols.items |> Array.ofSeq
                let _ =
                    allCols
                    |> Array.map (fun col -> col.getRange())
                    |> Array.map (fun x ->
                        x.columnHidden <- false
                        x.format.autofitColumns()
                        x.format.autofitRows()
                    )
                // get all column headers
                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
                // get only column headers with values inside and map object to string
                let headerArr = headerVals |> Array.choose id |> Array.map string
                // parse header elements into record type
                let parsedHeaderArr = headerArr |> Array.map parseColHeader
                // find all columns to hide
                let colsToHide =
                    parsedHeaderArr
                    |> Array.filter (fun header -> header.TagArr.IsSome && Array.contains ColumnTags.HiddenTag header.TagArr.Value)
                let ranges =
                    colsToHide
                    |> Array.map (fun header -> (annotationTable.columns.getItem (U2.Case2 header.Header)).getRange())
                let hideCols = ranges |> Array.map (fun x -> x.columnHidden <- true)
                r.enableEvents <- true
                "Autoformat Table"
            )
    )

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

let addAnnotationBlock (annotationTable,colName:string,format:(string*(string option)) option) =

    // The following cols are useful with TSR and TAN hidden cols and cannot have unit cols
    let isSingleCol =
        match colName with
        | "Sample Name" | "Source Name" | "Data File Name" -> true
        | _ -> false

    let mainColName (colName:string) (id:int) =
        match id with
        | 1 ->
            colName
        | _ ->
            sprintf "%s (#%i)" colName id
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
    // as unit always has to be a term and cannot be for example "Source" or "Sample", both of which have a differen format than for exmaple "Parameter [TermName]",
    // we only need one function to generate id and attributes and bring the unit term in the right format.
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

        /// This is necessary to place new columns next to selected col
        let tables = annotationTable.columns.load(propertyNames = U2.Case1 "items")
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let range = context.workbook.getSelectedRange()
        annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"|])) |> ignore
        range.load(U2.Case1 "columnIndex") |> ignore

        ///
        let tableRange = annotationTable.getRange()
        tableRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"]))) |> ignore

        let r = context.runtime.load(U2.Case1 "enableEvents")

        context.sync().``then``( fun _ ->

            r.enableEvents <- false

            /// This is necessary to place new columns next to selected col
            let tableHeaderRangeColIndex = annoHeaderRange.columnIndex
            let selectColIndex = range.columnIndex
            let diff = selectColIndex - tableHeaderRangeColIndex |> int
            let vals =
                tables.items
            let maxLength = vals.Count-1
            let newBaseColIndex =
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
            // This is necessary to prevent trying to create a column with an already existing name
            let parsedBaseHeader = parseColHeader colName
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
                                // i think it is necessary to also check for "T S REF" and "T A N" because of the following possibilities
                                // Parameter [instrument model] | "Term Source REF [instrument model] (#h) | ...
                                // Factor [instrument model] | "Term Source REF [instrument model] (#h) | ...
                                // in the example above the coreColName is different but "T S REF" and "T A N" would be the same.
                                || existingHeader = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader int)
                                || existingHeader = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader int)
                        )
                    if isExisting then
                        loopingCheck (int+1)
                    else
                        int
                loopingCheck 1
            let newId = findNewIdForName()

            let rowCount = tableRange.rowCount |> int

            //create an empty column to insert
            let col =
                createEmptyMatrixForTables 1 rowCount ""
            let createdCol1() =
                annotationTable.columns.add(
                    index = newBaseColIndex',
                    values = U4.Case1 col,
                    name = mainColName colName newId
                )

            let createdCol2() =
                annotationTable.columns.add(
                    index = newBaseColIndex'+1.,
                    values = U4.Case1 col,
                    name = sprintf "Term Source REF %s" (hiddenColAttributes parsedBaseHeader newId)
                )
            let createdCol3() =
                annotationTable.columns.add(
                    index = newBaseColIndex'+2.,
                    values = U4.Case1 col,
                    name = sprintf "Term Accession Number %s" (hiddenColAttributes parsedBaseHeader newId)
                )

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

                    let createdUnitCol1 =
                        annotationTable.columns.add(
                            index = newBaseColIndex'+3.,
                            values = U4.Case1 col,
                            name = sprintf "Unit %s" (unitColAttributes format.Value newUnitId)
                        )

                    let createdUnitCol2 =
                        annotationTable.columns.add(
                            index = newBaseColIndex'+4.,
                            values = U4.Case1 col,
                            name = sprintf "Term Source REF %s" (unitColAttributes format.Value newUnitId)
                        )
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

            let unitColCreationMsg = if createUnitColsIfNeeded.IsSome then fst createUnitColsIfNeeded.Value else ""
            let unitColFormat = if createUnitColsIfNeeded.IsSome then snd createUnitColsIfNeeded.Value else "0.00"

            r.enableEvents <- true
            mainColName colName newId, unitColFormat, sprintf "%s column was added.%s" colName unitColCreationMsg
        )
    )

let changeTableColumnFormat annotationTable (colName:string) (format:string) =
    Excel.run(fun context ->
       let sheet = context.workbook.worksheets.getActiveWorksheet()
       let annotationTable = sheet.tables.getItem(annotationTable)

       let colBodyRange = (annotationTable.columns.getItem (U2.Case2 colName)).getDataBodyRange()
       colBodyRange.load(U2.Case2 (ResizeArray(["columnCount";"rowCount"]))) |> ignore

       let r = context.runtime.load(U2.Case1 "enableEvents")

       context.sync().``then``( fun _ ->

            r.enableEvents <- false

            let rowCount = colBodyRange.rowCount |> int
            //create an empty column to insert
            let formats = createValueMatrix 1 rowCount format

            colBodyRange.numberFormat <- formats

            r.enableEvents <- true
            sprintf "format of %s was changed to %s" colName format
       )
    )

// Reform this to onSelectionChanged
let getParentTerm (annotationTable) =
    Excel.run (fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
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

let fillValue (annotationTable,v,fillTerm:Shared.DbDomain.Term option) =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
        //let annoRange = annotationTable.getDataBodyRange()
        //let _ = annoRange.load(U2.Case2 (ResizeArray(["address";"values";"columnIndex"; "columnCount"])))
        let range = context.workbook.getSelectedRange()
        let _ = range.load(U2.Case2 (ResizeArray(["address";"values";"columnIndex"; "columnCount"])))
        let nextColsRange = range.getColumnsAfter 2.
        let _ = nextColsRange.load(U2.Case2 (ResizeArray(["address";"values";"columnIndex";"columnCount"])))

        let r = context.runtime.load(U2.Case1 "enableEvents")

        //sync with proxy objects after loading values from excel
        context.sync().``then``( fun _ ->
            if range.columnCount > 1. then failwith "Cannot insert Terms in more than one column at a time."

            r.enableEvents <- false

            let newVals = ResizeArray([
                for arr in range.values do
                    let tmp = arr |> Seq.map (fun _ -> Some (v |> box))
                    ResizeArray(tmp)
            ])

            let nextNewVals = ResizeArray([
                for ind in 0 .. nextColsRange.values.Count-1 do
                    let tmp =
                        nextColsRange.values.[ind]
                        |> Seq.mapi (fun i _ ->
                            match i, fillTerm with
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

            range.values <- newVals
            nextColsRange.values <- nextNewVals
            r.enableEvents <- true
            //sprintf "%s filled with %s; ExtraCols: %s" range.address v nextColsRange.address
            sprintf "%A, %A" nextColsRange.values.Count nextNewVals
        )
    )
   
let getInsertTermsToFillHiddenCols (annotationTable') =
    Excel.run(fun context ->
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable')
        let tables = annotationTable.columns.load(propertyNames = U2.Case2 (ResizeArray[|"items";"count"|]))
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"|])) |> ignore
        let annoBodyRange = annotationTable.getDataBodyRange()
        let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore
        context.sync()
            .``then``( fun _ ->
                let columnBodies =
                    annoBodyRange.values
                    |> viewRowsByColumns
                let columns =
                    [|
                        for i = 0 to (int tables.count - 1) do
                            yield (
                                let ind = i
                                let header =
                                    annoHeaderRange.values.[0].[ind]
                                    |> fun x -> if x.IsSome then parseColHeader (string annoHeaderRange.values.[0].[ind].Value) |> Some else None
                                let cells =
                                    columnBodies.[ind]
                                    |> Array.mapi (fun i cellVal ->
                                        let cellValue = if cellVal.IsSome then Some (string cellVal.Value) else None
                                        Cell.create i cellValue
                                    )
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

                /// Hidden columns do only come with certain core names. The acceptable names can be found in OfficeInterop.ColumnCoreNames.
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

                let checkForHiddenColType (currentBlock:BuildingBlock option) (nextCol:Column) =
                    // Then we need to check if the nextCol is either a TSR or a TAN column
                    match nextCol.Header.Value.CoreName.Value with
                    | ColumnCoreNames.Hidden.TermAccessionNumber ->
                        // Build in fail safes.
                        if currentBlock.IsNone then errorMsg1 nextCol currentBlock
                        if currentBlock.Value.TAN.IsSome then errorMsg3 nextCol currentBlock currentBlock.Value.TAN.Value.Header.Value.Header
                        let updateCurrentBlock =
                            { currentBlock.Value with
                                TAN =  Some nextCol } |> Some
                        updateCurrentBlock
                    | ColumnCoreNames.Hidden.TermSourceREF ->
                        // Build in fail safe.
                        if currentBlock.IsNone then errorMsg1 nextCol currentBlock
                        if currentBlock.Value.TSR.IsSome then errorMsg3 nextCol currentBlock currentBlock.Value.TSR.Value.Header.Value.Header
                        let updateCurrentBlock =
                            { currentBlock.Value with
                                TSR =  Some nextCol } |> Some
                        updateCurrentBlock
                    | ColumnCoreNames.Hidden.Unit ->
                        // Build in fail safe.
                        if currentBlock.IsSome then errorMsg3 nextCol currentBlock currentBlock.Value.MainColumn.Header.Value.Header
                        let newBlock = BuildingBlock.create nextCol None None None |> Some
                        newBlock 
                    | _ ->
                        // Build in fail safe.
                        errorMsg2 nextCol currentBlock

                // building block are defined by one visuable column and an undefined number of hidden columns.
                // Therefore we iterate through the columns array and use every column without an `#h` tag as the start of a new building block.
                let rec sortColsIntoBuildingBlocks (index:int) (currentBlock:BuildingBlock option) (buildingBlockList:BuildingBlock list) =
                    if index > (int tables.count - 1) then
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
                        // if the nextCol.Header has a tag array and it does NOT contain a hidden tag then it starts a new building block
                        elif
                            (nextCol.Header.Value.TagArr.IsSome && nextCol.Header.Value.TagArr.Value |> Array.contains ColumnTags.HiddenTag |> not)
                            || (nextCol.Header.IsSome && nextCol.Header.Value.TagArr.IsNone)
                        then
                            let newBuildingBlock = BuildingBlock.create nextCol None None None |> Some
                            // If there is a currentBlock we add it to the list of building blocks.
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
                                let updatedUnitBlock = checkForHiddenColType currentBlock.Value.Unit nextCol
                                let updateCurrentBlock = {currentBlock.Value with Unit = updatedUnitBlock} |> Some
                                sortColsIntoBuildingBlocks (index+1) updateCurrentBlock buildingBlockList
                            else
                                failwith "The tag array of the next column to process in 'sortColsIntoBuildingBlocks' can only contain a '#u' tag or not."
                        else
                            failwith (sprintf "The tag array of the next column to process in 'sortColsIntoBuildingBlocks' was not recognized as hidden or main column: %A." nextCol.Header)

                // sort all columns into building blocks
                let buildingBlocks =
                    sortColsIntoBuildingBlocks 0 None []
                    |> List.rev
                    |> Array.ofList

                let buildingBlocksWithOntology =
                    buildingBlocks |> Array.filter (fun x -> x.TSR.IsSome && x.TAN.IsSome)

                /// We need an array of all distinct cell.values and where they occur in col- and row-index
                let terms =
                    buildingBlocksWithOntology
                    |> Array.collect (fun bBlock ->
                        // get current col index
                        let tsrTanColIndices = [|bBlock.TSR.Value.Index; bBlock.TAN.Value.Index|]
                        let fillTermConstructsNoUnit bBlock=
                            // group cells by value so we don't get doubles
                            bBlock.MainColumn.Cells
                            |> Array.groupBy (fun cell ->
                                cell.Value.IsSome, cell.Value.Value
                            )
                            // only keep cells with value and create InsertTerm types that will be passed to the server to get filled with a term option.
                            |> Array.choose (fun ((isSome,searchStr),cellArr) ->
                                if isSome && searchStr <> "" then
                                    let rowIndices = cellArr |> Array.map (fun cell -> cell.Index)
                                    Shared.InsertTerm.create tsrTanColIndices searchStr rowIndices
                                    |> Some
                                else
                                    None
                            )
                        let fillTermConstructsWithUnit (bBlock:BuildingBlock) =
                            let searchStr = bBlock.MainColumn.Header.Value.Ontology.Value
                            let rowIndices =
                                bBlock.MainColumn.Cells
                                |> Array.map (fun x ->
                                   x.Index
                                )
                            [|Shared.InsertTerm.create tsrTanColIndices searchStr rowIndices|]
                        if bBlock.Unit.IsSome then
                            fillTermConstructsWithUnit bBlock
                        else
                            fillTermConstructsNoUnit bBlock
                    )

                let units =
                    buildingBlocksWithOntology
                    |> Array.filter (fun bBlock -> bBlock.Unit.IsSome)
                    |> Array.map (
                        fun bBlock ->
                            let unit = bBlock.Unit.Value
                            let searchString = unit.MainColumn.Header.Value.Ontology.Value
                            let colIndices = [|unit.MainColumn.Index; unit.TSR.Value.Index; unit.TAN.Value.Index|]
                            let rowIndices = unit.MainColumn.Cells |> Array.map (fun x -> x.Index)
                            Shared.InsertTerm.create colIndices searchString rowIndices
                    )

                let allSearches = [|
                    yield! terms
                    yield! units
                |]

                annotationTable',allSearches
            )
    )

let fillHiddenColsByInsertTerm (annotationTable,insertTerms:InsertTerm []) =
    Excel.run(fun context ->

        let createCellValueInput str=
            ResizeArray([
                ResizeArray([
                    str |> box |> Some
                ])
            ])

        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
        let annoBodyRange = annotationTable.getDataBodyRange()
        let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore

        let r = context.runtime.load(U2.Case1 "enableEvents")

        context.sync().
            ``then``(fun _ ->
                r.enableEvents <- false

                let foundTerms = insertTerms |> Array.filter (fun x -> x.TermOpt.IsSome)
                let insert() =
                    foundTerms
                    |> Array.map (
                        fun insertTerm ->
                            let t = insertTerm.TermOpt.Value
                            let ont, accession =
                                let a = t.Accession
                                let splitA = a.Split":"
                                let accession = Shared.URLs.TermAccessionBaseUrl + a.Replace(":","_")
                                splitA.[0], accession
                            let inputVals = [|
                                if insertTerm.ColIndices.Length = 2 then
                                    createCellValueInput ont
                                    createCellValueInput accession
                                elif insertTerm.ColIndices.Length = 3 then
                                    createCellValueInput t.Name
                                    createCellValueInput ont
                                    createCellValueInput accession
                            |]
                            printfn "insert term: %A, for cols: %A, for rows: %A" t insertTerm.ColIndices insertTerm.RowIndices
                            // iterate over all columns (in this case in form of the index of their array. as we need the index to access the correct 'inputVal' value
                            for i in 0 .. insertTerm.ColIndices.Length do

                                // iterate over all rows and insert the correct inputVal
                                for rowInd in insertTerm.RowIndices do

                                    let cell = annoBodyRange.getCell(float rowInd, float insertTerm.ColIndices.[i])
                                    cell.values <- inputVals.[i]
                    )

                let _ = insert()

                r.enableEvents <- true
                sprintf "Filled information for terms: %s" (foundTerms |> Array.map (fun x -> x.TermOpt.Value.Name) |> String.concat ", ")
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

//let autoGetSelectedHeader () =
//    Excel.run (fun context ->
//        let sheet = context.workbook.worksheets.getActiveWorksheet()
//        let annotationTable = sheet.tables.getItem("annotationTable")
//        annotationTable.onSelectionChanged.add(fun e -> getParentOntology())
//        context.sync()
//    )

let syncContext (passthroughMessage : string) =
    Excel.run (fun context -> context.sync(passthroughMessage))