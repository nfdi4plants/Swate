module OfficeInterop.HelperFunctions

open Fable.Core
open Fable.Core.JsInterop
open ExcelJS.Fable
open Excel
open GlobalBindings
open System.Collections.Generic
open System.Text.RegularExpressions

open OfficeInterop.Types
open BuildingBlockTypes
open Shared

// ExcelApi 1.1
let getActiveAnnotationTableName() =
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
                match res with
                | Success tableName -> tableName
                | TryFindAnnoTableResult.Error msg -> failwith msg
        )
    )

// ExcelApi 1.1
/// This function returns the names of all annotationTables in all worksheets.
/// This function is used to pass a list of all table names to e.g. the 'createAnnotationTable()' function. 
let getAllTableNames() =
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

let createMatrixForTables (colCount:int) (rowCount:int) value =
    [|
        for i in 0 .. rowCount-1 do
            yield [|
                for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
            |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

let createValueMatrix (colCount:int) (rowCount:int) value =
    ResizeArray([
        for outer in 0 .. rowCount-1 do
            let tmp = Array.zeroCreate colCount |> Seq.map (fun _ -> Some (value |> box))
            ResizeArray(tmp)
    ])

//let tryFindSpannedBuildingBlocks (currentProtocolGroup:Xml.GroupTypes.Protocol) (buildingBlocks: BuildingBlock []) =
//    let findAllSpannedBlocks =
//        currentProtocolGroup.SpannedBuildingBlocks
//        |> List.choose (fun spannedBlock ->
//            buildingBlocks
//            |> Array.tryFind (fun foundBuildingBlock ->
//                let isSameAccession =
//                    if spannedBlock.TermAccession <> "" || foundBuildingBlock.MainColumn.Header.Value.Ontology.IsSome then
//                        // As in the above only one option is that ontology is some we need a default in the next step.
//                        // We default to an empty termaccession, as 'spannedBlock' MUST be <> "" to trigger the default
//                        (Option.defaultValue (TermMinimal.create "" "") foundBuildingBlock.MainColumn.Header.Value.Ontology).TermAccession = spannedBlock.TermAccession
//                    else
//                        true
//                foundBuildingBlock.MainColumn.Header.Value.Header = spannedBlock.ColumnName
//                && isSameAccession
//            )
//        )
//    //let reduce = findAllSpannedBlocks |> List.map (fun x -> x.MainColumn.Header.Value.Header)
//    if findAllSpannedBlocks.Length = currentProtocolGroup.SpannedBuildingBlocks.Length then
//        Some findAllSpannedBlocks
//    else
//        None

open Indexing

//// ExcelApi 1.1 OR 1.4 (columns.add)
//let createUnitColumns (context:Excel.RequestContext) (annotationTable:Table) newBaseColIndex rowCount (format:string option) (unitAccessionOpt:string option) =
//    let col = createEmptyMatrixForTables 1 rowCount ""
//    if format.IsSome then
//        promise {
//            let! annoHeaderRange = context.sync().``then``(fun e ->
//                let annoHeaderRange = annotationTable.getHeaderRowRange()
//                let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"; "columnCount"; "rowIndex"|]))
//                annoHeaderRange
//            )
//            let! res = context.sync().``then``(fun e ->
//                let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
//                let allColHeaders =
//                    headerVals
//                    |> Array.choose id
//                    |> Array.map string

//                let newUnitId = Unit.findNewIdForUnit allColHeaders format
//                /// create unit main column
//                let createdUnitCol1 =
//                    annotationTable.columns.add(
//                        index = newBaseColIndex+3.,
//                        values = U4.Case1 col,
//                        name = sprintf "Unit %s" (Unit.createUnitColAttributes format.Value newUnitId)
//                    )

//                /// create unit TSR
//                let createdUnitCol2 =
//                    annotationTable.columns.add(
//                        index = newBaseColIndex+4.,
//                        values = U4.Case1 col,
//                        name = sprintf "Term Source REF %s" (Unit.createUnitColAttributes format.Value newUnitId)
//                    )

//                /// create unit TAN
//                let createdUnitCol3 =
//                    annotationTable.columns.add(
//                        index = newBaseColIndex+5.,
//                        values = U4.Case1 col,
//                        name = sprintf "Term Accession Number %s" (Unit.createUnitColAttributes format.Value newUnitId)
//                    )

//                Some (
//                    sprintf " Added specified unit: %s" (format.Value),
//                    sprintf "0.00 \"%s\"" (format.Value)
//                )
//            )

//            return res
//        }

//    else
//        promise {return None}

//let updateUnitColumns (context:RequestContext) (annotationTable:Table) newBaseColIndex (format:string option) (unitAccessionOpt:string option) =
//    let col v= createValueMatrix 1 1 v
//    if format.IsSome then
//        let annoHeaderRange = annotationTable.getHeaderRowRange()
//        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"; "columnCount"; "rowIndex"|]))

//        context.sync().``then``(fun e ->
//            let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq
            
//            let allColHeaders =
//                headerVals
//                |> Array.choose id
//                |> Array.map string
//            let newUnitId = Unit.findNewIdForUnit allColHeaders format unitAccessionOpt

//            /// create unit main column
//            let updateUnitCol1 =
//                annoHeaderRange.getColumn(newBaseColIndex+3.)
//                |> fun c1 -> c1.values <- sprintf "Unit %s" (Unit.unitColAttributes format.Value unitAccessionOpt newUnitId) |> col

//            /// create unit TSR
//            let createdUnitCol2 =
//                annoHeaderRange.getColumn(newBaseColIndex+4.)
//                |> fun c2 -> c2.values <- sprintf "Term Source REF %s" (Unit.unitColAttributes format.Value unitAccessionOpt newUnitId) |> col

//            /// create unit TAN
//            let createdUnitCol3 =
//                annoHeaderRange.getColumn(newBaseColIndex+5.)
//                |> fun c3 -> c3.values <- sprintf "Term Accession Number %s" (Unit.unitColAttributes format.Value unitAccessionOpt newUnitId) |> col

//            Some (
//                sprintf " Added specified unit: %s" (format.Value),
//                sprintf "0.00 \"%s\"" (format.Value)
//            )
//        )
//    else
//        promise {return None}

/// Swaps 'Rows with column values' to 'Columns with row values'.
let viewRowsByColumns (rows:ResizeArray<ResizeArray<'a>>) =
    rows
    |> Seq.collect (fun x -> Seq.indexed x)
    |> Seq.groupBy fst
    |> Seq.map (snd >> Seq.map snd >> Seq.toArray)
    |> Seq.toArray

/// This function needs an array of the column headers as input. Takes as such:
/// `let annoHeaderRange = annotationTable.getHeaderRowRange()`
/// `annoHeaderRange.load(U2.Case2 (ResizeArray[|"values";"columnIndex"|])) |> ignore`
/// `let headerVals = annoHeaderRange.values.[0] |> Array.ofSeq`
let findIndexNextNotHiddenCol (headerVals:obj option []) (startIndex:float) =
    let indexedHiddenCols =
        headerVals
        |> Array.indexed
        |> Array.choose (fun (i,x) ->
            let header = SwateColumnHeader.create (string x.Value) 
            if x.IsSome then
                let checkIsHidden = header.isReference
                if checkIsHidden then
                    Some (i|> float)
                else
                    None
            else
                None
        )
        //|> Array.filter (fun (i,x) -> x.TagArr.IsSome && Array.contains "#h" x.TagArr.Value)
    let rec loopingCheckSkipHiddenCols (newInd:float) =
        let nextIsHidden =
            Array.exists (fun i -> i = newInd + 1.) indexedHiddenCols
        if nextIsHidden then
            loopingCheckSkipHiddenCols (newInd + 1.)
        else
            newInd+1.
    //failwith (sprintf "START: %A ; FOUND NEW: %A" startIndex (loopingCheckSkipHiddenCols startIndex))
    loopingCheckSkipHiddenCols startIndex

module BuildingBlockTypes =

    // ExcelApi 1.1
    /// This function is part 1 to get a 'BuildingBlock []' representation of a Swate table.
    /// It should be used as follows: 'let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable'
    /// This function will load all necessery excel properties.
    let getBuildingBlocksPreSync (context:RequestContext) annotationTable =
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
        let annoBodyRange = annotationTable.getDataBodyRange()
        let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"; "numberFormat"|])) |> ignore
        annoHeaderRange, annoBodyRange

    // ExcelApi 1.1
    /// This function is part 2 to get a 'BuildingBlock []' representation of a Swate table.
    /// It's parameters are the output of 'getBuildingBlocksPreSync' and it will return a full 'BuildingBlock []'.
    /// It MUST be used either in or after 'context.sync().``then``(fun e -> ..)' after 'getBuildingBlocksPreSync'.
    let getBuildingBlocks (annoHeaderRange:Excel.Range) (annoBodyRange:Excel.Range) =

        /// Get the table by 'Columns [| Rows [|Values|] |]'
        let columnBodies = annoBodyRange.values |> viewRowsByColumns

        /// Get the table number formats (units) by 'Columns [| Rows [|Values|] |]'
        let numberFormats = annoBodyRange.numberFormat |> viewRowsByColumns

        /// Write columns into 'BuildingBlockTypes.Column'
        let columns =
            [|
                // iterate over n of columns
                for ind = 0 to (int annoHeaderRange.columnCount - 1) do
                    yield (
                        // Get column header and parse it
                        let header = annoHeaderRange.values.[0].[ind]
                        let swateHeader = SwateColumnHeader.create (string header.Value)
                        // Get column values and write them to 'BuildingBlockTypes.Cell'
                        let cells =
                            columnBodies.[ind]
                            |> Array.mapi (fun i cellVal ->
                                let cellValue = if cellVal.IsSome then Some (string cellVal.Value) else None
                                /// next extrract the number format for the cells, this will get - if existing - the unit term name.
                                /// The term name is enough in this case, as the unit ontology should be the only default ontology to be
                                /// used for this value and we can guarantee no duplicates inside of it, so no need for the unique identifier term accession.
                                let cellUnit =
                                    let unit = numberFormats.[ind].[i]
                                    if unit.IsSome && string unit.Value <> "General" then
                                        TermMinimal.ofNumberFormat (string unit.Value) |> Some
                                    else
                                        None
                                Cell.create i cellValue cellUnit
                            )
                        // Create column
                        Column.create ind swateHeader cells
                    )
            |]

        /// Failsafe (1): it should never happen, that the nextColumn is a reference column without an existing building block.
        let errorMsg1 (nextCol:Column) =
            failwith 
                $"Swate encountered an error while processing the active annotation table.
                Swate found a reference column ({nextCol.Header.SwateColumnHeader}) without a prior main column.."


        /// Hidden columns do only come with certain core names. The acceptable names can be found in OfficeInterop.Types.ColumnCoreNames.
        let errorMsg2 (nextCol:Column) =
            failwith 
                $"Swate encountered an error while processing the active annotation table.
                Swate found a reference column ({nextCol.Header.SwateColumnHeader}) with an unknown core name: {nextCol.Header.getColumnCoreName}"
                

        /// If a columns core name already exists for the current building block, then the block is faulty and needs userinput to be corrected.
        let errorMsg3 (nextCol:Column) (buildingBlock:BuildingBlock) assignedCol =
            failwith 
                $"Swate encountered an error while processing the active annotation table.
                Swate found a reference column ({nextCol.Header.SwateColumnHeader}) with a core name ({nextCol.Header.getColumnCoreName}), that is already assigned to the previous building block.
                Building block main column: {buildingBlock.MainColumn.Header.SwateColumnHeader}, already assigned column: {assignedCol}"

        /// Update current building block with new reference column. A ref col can be TSR, TAN and Unit.
        let checkForReferenceColumnType (currentBlock:BuildingBlock option) (nextCol:Column) =
            // Then we need to check if the nextCol is either a TSR, TAN or a unit column
            match nextCol.Header with
            | isTan when isTan.isTANCol ->
                // Build in fail safes.
                if currentBlock.IsNone then errorMsg1 nextCol
                if currentBlock.Value.TAN.IsSome then errorMsg3 nextCol currentBlock.Value currentBlock.Value.TAN.Value.Header.SwateColumnHeader
                // Update building block
                let updateCurrentBlock =
                    { currentBlock.Value with
                        TAN =  Some nextCol } |> Some
                updateCurrentBlock
            | isTSR when isTSR.isTSRCol ->
                // Build in fail safe.
                if currentBlock.IsNone then errorMsg1 nextCol
                if currentBlock.Value.TSR.IsSome then errorMsg3 nextCol currentBlock.Value currentBlock.Value.TSR.Value.Header.SwateColumnHeader
                // Update building block
                let updateCurrentBlock =
                    { currentBlock.Value with
                        TSR =  Some nextCol } |> Some
                updateCurrentBlock
            | isUnit when isUnit.isUnitCol ->
                // Build in fail safe.
                if currentBlock.IsNone then errorMsg1 nextCol
                if currentBlock.Value.Unit.IsSome then errorMsg3 nextCol currentBlock.Value currentBlock.Value.Unit.Value.Header.SwateColumnHeader
                // Create unit building block
                let updateCurrentBlock =
                    { currentBlock.Value with
                        Unit =  Some nextCol } |> Some
                updateCurrentBlock
            | _ ->
                // Build in fail safe.
                errorMsg2 nextCol currentBlock

        // Building blocks are defined by one visable column and an undefined number of hidden columns.
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
                // If the nextCol does is not a swate specific column header it is empty and therefore skipped.
                if
                    not nextCol.Header.isSwateColumnHeader
                then
                    sortColsIntoBuildingBlocks (index+1) currentBlock buildingBlockList
                // If the nextCol.Header has no tag array or its tag array does NOT contain a hidden tag then it starts a new building block
                elif
                    nextCol.Header.isMainColumn
                then
                    let newBuildingBlock = BuildingBlock.create nextCol None None None None |> Some
                    // If there is a 'currentBlock' we add it to the list of building blocks ('buildingBlockList').
                    // This is done because if a new block starts the previous one naturally is finished
                    if currentBlock.IsSome then
                        sortColsIntoBuildingBlocks (index+1) newBuildingBlock (currentBlock.Value::buildingBlockList)
                    // If there is no currentBuildingBlock, e.g. at the start of this function we replace the None with the first building block.
                    else
                        sortColsIntoBuildingBlocks (index+1) newBuildingBlock buildingBlockList
                // if the nextCol.Header is a reference column add it to the existing building block
                elif
                    nextCol.Header.isReference
                then
                    let updateCurrentBlock = checkForReferenceColumnType currentBlock nextCol
                    sortColsIntoBuildingBlocks (index+1) updateCurrentBlock buildingBlockList
                else
                    failwith $"""Unable to parse "{nextCol.Header}" into building blocks."""

        let extractTermToBuildingBlock (bb:BuildingBlock) =
            let termOpt =
                match bb.TSR, bb.TAN with
                | None, None            -> None
                | Some tsr, Some tan    ->
                    let tsrTermAccession = tsr.Header.tryGetTermAccession
                    let tanTermAccession = tan.Header.tryGetTermAccession
                    let termName        = bb.MainColumn.Header.tryGetOntologyTerm
                    match tsrTermAccession, tanTermAccession with
                    | Some accession1, Some accession2 ->
                        if accession1 <> accession2 then failwith $"Swate found mismatching term accession in building block {bb.MainColumn.Header}: {accession1}, {accession2}"
                        if termName.IsNone then failwith $"Swate found mismatching ontology term infor in building block {bb.MainColumn.Header}: Found term accession in reference columns, but no ontology ref in main column."
                        TermMinimal.create termName.Value accession1 |> Some
                    | None, None -> None
                    | _ -> failwith $"Swate found mismatching reference columns in building block {bb.MainColumn.Header}: Found TSR and TAN column but no complete term accessions."
                | _ -> failwith $"Swate found mismatching reference columns in building block {bb.MainColumn.Header}: Found only TSR or TAN."
            { bb with MainColumnTerm = termOpt }

            

        /// Sort all columns into building blocks.
        let buildingBlocks =
            sortColsIntoBuildingBlocks 0 None []
            |> List.rev
            |> Array.ofList
            |> Array.map extractTermToBuildingBlock

        buildingBlocks
        

//    open System

    //// ExcelApi 1.1
    //let findSelectedBuildingBlockPreSync (context:RequestContext) annotationTableName =
    //    let selectedRange = context.workbook.getSelectedRange()
    //    let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))

    //    // Ref. 2
    //    let annoHeaderRange, annoBodyRange = getBuildingBlocksPreSync context annotationTableName
    //    selectedRange, annoHeaderRange, annoBodyRange

    //// ExcelApi 1.1
    //let findSelectedBuildingBlock (selectedRange:Excel.Range) (annoHeaderRange:Excel.Range) (annoBodyRange:Excel.Range) (context:RequestContext) =
    //    context.sync().``then``( fun _ ->

    //        if selectedRange.columnCount <> 1. then
    //            failwith "To use this function please select a single column"

    //        let errorMsg = "To use this function please select a single column of a Swate table."

    //        let newSelectedColIndex =
    //            // recalculate the selected range index based on table
    //            let diff = selectedRange.columnIndex - annoHeaderRange.columnIndex
    //            // if index is smaller 0 it is outside of table range
    //            if diff < 0. then failwith errorMsg
    //            // if index is bigger than columnCount-1 then it is outside of tableRange
    //            elif diff > annoHeaderRange.columnCount-1. then failwith errorMsg
    //            else diff

    //        /// Sort all columns into building blocks.
    //        let buildingBlocks =
    //            getBuildingBlocks annoHeaderRange annoBodyRange

    //        /// find building block with the closest main column index from left
    //        let findLeftClosestBuildingBlock =
    //            buildingBlocks
    //            |> Array.filter (fun x -> x.MainColumn.Index <= int newSelectedColIndex)
    //            |> Array.minBy (fun x -> Math.Abs(x.MainColumn.Index - int newSelectedColIndex))

    //        findLeftClosestBuildingBlock
    //    )

//    let private sortMainColValuesToSearchTerms (buildingBlock:BuildingBlock) =
//        // get current col index
//        let tsrTanColIndices = [|buildingBlock.TSR.Value.Index; buildingBlock.TAN.Value.Index|]
//        let fillTermConstructsNoUnit bBlock =
//            // group cells by value so we don't get doubles.
//            bBlock.MainColumn.Cells
//            |> Array.groupBy (fun cell ->
//                cell.Value.Value
//            )
//            // create SearchTermI types that will be passed to the server to get filled with a term option.
//            |> Array.map (fun (searchStr,cellArr) ->
//                let rowIndices = cellArr |> Array.map (fun cell -> cell.Index)
//                Shared.SearchTermI.create tsrTanColIndices searchStr "" bBlock.MainColumn.Header.Value.Ontology rowIndices
//            )
//        /// We differentiate between building blocks with and without unit as unit building blocks will not contain terms as values but e.g. numbers.
//        /// In this case we do not want to search the database for the cell values but the parent ontology in the header.
//        /// This will then be used for TSR and TAN.
//        let fillTermConstructsWithUnit (bBlock:BuildingBlock) =
//            let searchStr       = bBlock.MainColumn.Header.Value.Ontology.Value.Name
//            let termAccession   = bBlock.MainColumn.Header.Value.Ontology.Value.TermAccession
//            let rowIndices =
//                bBlock.MainColumn.Cells
//                |> Array.map (fun x ->
//                    x.Index
//                )
//            [|Shared.SearchTermI.create tsrTanColIndices searchStr termAccession None rowIndices|]
//        if buildingBlock.Unit.IsSome then
//            fillTermConstructsWithUnit buildingBlock
//        else
//            fillTermConstructsNoUnit buildingBlock

//    let private sortUnitColToSearchTerm (buildingBlock:BuildingBlock) =
//        let unit = buildingBlock.Unit.Value
//        let searchString  = unit.MainColumn.Header.Value.Ontology.Value.Name
//        let termAccession = unit.MainColumn.Header.Value.Ontology.Value.TermAccession 
//        let colIndices = [|unit.MainColumn.Index; unit.TSR.Value.Index; unit.TAN.Value.Index|]
//        let rowIndices = unit.MainColumn.Cells |> Array.map (fun x -> x.Index)
//        Shared.SearchTermI.create colIndices searchString termAccession None rowIndices

//    let private sortHeaderToSearchTerm (buildingBlock:BuildingBlock) =
//        let isOntologyTerm = buildingBlock.MainColumn.Header.Value.Ontology.IsSome
//        let searchString  =
//            if isOntologyTerm then
//                buildingBlock.MainColumn.Header.Value.Ontology.Value.Name
//            else
//                buildingBlock.MainColumn.Header.Value.Header
//        let termAccession =
//            if isOntologyTerm then
//                buildingBlock.MainColumn.Header.Value.Ontology.Value.TermAccession
//            else
//                ""
//        let colIndices = [|
//            buildingBlock.MainColumn.Index;
//            if buildingBlock.TSR.IsSome then buildingBlock.TSR.Value.Index;
//            if buildingBlock.TAN.IsSome then buildingBlock.TAN.Value.Index
//        |]
//        let rowIndices = buildingBlock.MainColumn.Cells |> Array.map (fun x -> x.Index)
//        Shared.SearchTermI.create colIndices searchString termAccession None rowIndices

//    let sortBuildingBlockValuesToSearchTerm (buildingBlock:BuildingBlock) =

//        /// We need an array of all distinct cell.values and where they occur in col- and row-index
//        let terms() = sortMainColValuesToSearchTerms buildingBlock

//        /// Create search types for the unit building blocks.
//        let units() = sortUnitColToSearchTerm buildingBlock

//        match buildingBlock.TAN, buildingBlock.TSR with
//        | Some _, Some _ ->
//            /// Combine search types
//            [|
//                yield! terms()
//                if buildingBlock.Unit.IsSome then
//                    yield units()
//            |]
//        | None, None ->
//            let searchString  = ""
//            let termAccession = ""
//            let colIndices = [|buildingBlock.MainColumn.Index|]
//            let rowIndices = buildingBlock.MainColumn.Cells |> Array.map (fun x -> x.Index)
//            [| Shared.SearchTermI.create colIndices searchString termAccession None rowIndices |]
//        | _ -> failwith (sprintf "Encountered unknown reference column pattern. Building block (%s) can only contain both TSR and TAN or none." buildingBlock.MainColumn.Header.Value.Header)

//    let sortBuildingBlockToSearchTerm (buildingBlock:BuildingBlock) =
//        let bbValuesToSearchTerm = sortBuildingBlockValuesToSearchTerm buildingBlock
//        let bbHeaderToSearchTerm = sortHeaderToSearchTerm buildingBlock
//        [|yield! bbValuesToSearchTerm; bbHeaderToSearchTerm|] 

//    let sortBuildingBlocksValuesToSearchTerm (buildingBlocks:BuildingBlock []) =

//        buildingBlocks |> Array.collect (fun bb ->
//            sortBuildingBlockValuesToSearchTerm bb
//        )

open System
open Fable.SimpleXml
open Fable.SimpleXml.Generator

let xmlElementToXmlString (root:XmlElement) =
    let rec createChildren (child:XmlElement) =
        match child.SelfClosing with
        | true ->
            leaf child.Name [
                for cAttr in child.Attributes do
                    yield attr.value(cAttr.Key,cAttr.Value)
            ]
        | false ->
            node child.Name [
                for cAttr in child.Attributes do
                    yield attr.value(cAttr.Key,cAttr.Value)
            ][
                for grandChild in child.Children do
                    yield createChildren grandChild
                yield
                    text child.Content
            ]
    node root.Name [
        for rAttr in root.Attributes do
            yield attr.value(rAttr.Key,rAttr.Value)
    ] [
        for child in root.Children do
            yield createChildren child
        yield
            text root.Content
    ] |> serializeXml

let getCustomXml (customXmlParts:CustomXmlPartCollection) (context:RequestContext) =
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

        let xmlParsed =
            let isRootElement = xml |> SimpleXml.tryParseElement
            if xml = "" then
                "<customXml></customXml>" |> SimpleXml.parseElement
            elif isRootElement.IsSome then
                isRootElement.Value
            else
                let isManyRootElements = xml |> SimpleXml.tryParseManyElements
                if isManyRootElements.IsSome then
                    isManyRootElements.Value
                    |> List.tryFind (fun ele -> ele.Name = "customXml")
                    |> fun customXmlOpt -> if customXmlOpt.IsSome then customXmlOpt.Value else failwith "Swate could not find expected '<customXml>..</customXml>' root tag."
                else
                    failwith "Swate could not parse Workbook Custom Xml Parts. Had neither one root nor many root elements. Please contact the developer."
        if xmlParsed.Name <> "customXml" then failwith (sprintf "Swate found unexpected root xml element: %s" xmlParsed.Name)

        return xmlParsed
    }

let getActiveTableXml (tableName:string) (worksheetName:string) (completeCustomXmlParsed:XmlElement) =
    let tablexml=
        completeCustomXmlParsed
        |> SimpleXml.findElementsByName "SwateTable"
        |> List.tryFind (fun swateTableXml ->
            swateTableXml.Attributes.["Table"] = tableName
            && swateTableXml.Attributes.["Worksheet"] = worksheetName
        )
    if tablexml.IsSome then
        tablexml.Value |> Some
    else
        None


let getAllSwateTableValidation (xmlParsed:XmlElement) =
    let protocolGroups = SimpleXml.findElementsByName Xml.ValidationTypes.ValidationXmlRoot xmlParsed

    protocolGroups
    |> List.map (
        xmlElementToXmlString >> Xml.ValidationTypes.TableValidation.ofXml
    )

let getSwateValidationForCurrentTable tableName worksheetName (xmlParsed:XmlElement) =
    let activeTableXml = getActiveTableXml tableName worksheetName xmlParsed
    if activeTableXml.IsNone then
        None
    else
        let v = SimpleXml.findElementsByName Xml.ValidationTypes.ValidationXmlRoot activeTableXml.Value
        if v.Length > 1 then failwith (sprintf "Swate found multiple '<%s>' xml elements. Please contact the developer." Xml.ValidationTypes.ValidationXmlRoot)
        if v.Length = 0 then
            None
        else
            let tableXmlAsString = activeTableXml.Value |> xmlElementToXmlString
            Xml.ValidationTypes.TableValidation.ofXml tableXmlAsString |> Some

/// Use the 'remove' parameter to remove any Swate table validation xml for the worksheet annotation table name combination in 'tableValidation'
let private updateRemoveSwateValidation (tableValidation:Xml.ValidationTypes.TableValidation) (previousCompleteCustomXml:XmlElement) (remove:bool) =

    let currentTableXml = getActiveTableXml tableValidation.AnnotationTable.Name tableValidation.AnnotationTable.Worksheet previousCompleteCustomXml

    let nextTableXml =
        let newValidationXml = tableValidation.toXml |> SimpleXml.parseElement
        if currentTableXml.IsSome then
            let filteredChildren =
                currentTableXml.Value.Children
                |> List.filter (fun x -> x.Name <> Xml.ValidationTypes.ValidationXmlRoot )
            {currentTableXml.Value with
                Children =
                    if remove then
                        filteredChildren
                    else
                        newValidationXml::filteredChildren
            }
        else
            let initNewSwateTableXml =
                sprintf """<SwateTable Table="%s" Worksheet="%s"></SwateTable>""" tableValidation.AnnotationTable.Name tableValidation.AnnotationTable.Worksheet
            let swateTableXmlEle = initNewSwateTableXml |> SimpleXml.parseElement
            {swateTableXmlEle with
                Children = [newValidationXml]
            }
    let filterPrevTableFromRootChildren =
        previousCompleteCustomXml.Children
        |> List.filter (fun x ->
            let isExisting =
                x.Name = "SwateTable"
                && x.Attributes.["Table"] = tableValidation.AnnotationTable.Name
                && x.Attributes.["Worksheet"] = tableValidation.AnnotationTable.Worksheet
            isExisting |> not
        )
    {previousCompleteCustomXml with
        Children = nextTableXml::filterPrevTableFromRootChildren
    }

let removeSwateValidation (tableValidation:Xml.ValidationTypes.TableValidation) (previousCompleteCustomXml:XmlElement) =
    updateRemoveSwateValidation tableValidation previousCompleteCustomXml true

let updateSwateValidation (tableValidation:Xml.ValidationTypes.TableValidation) (previousCompleteCustomXml:XmlElement) =
    updateRemoveSwateValidation tableValidation previousCompleteCustomXml false

let replaceValidationByValidation tableVal1 tableVal2 previousCompleteCustomXml =
    let removeTableVal1 = removeSwateValidation tableVal1 previousCompleteCustomXml
    let addTableVal2 = updateSwateValidation tableVal2 removeTableVal1
    addTableVal2

let getAllSwateProtocolGroups (xmlParsed:XmlElement) =
    let protocolGroups = SimpleXml.findElementsByName Xml.GroupTypes.ProtocolGroupXmlRoot xmlParsed

    protocolGroups
    |> List.map (
        xmlElementToXmlString >> Xml.GroupTypes.ProtocolGroup.ofXml
    )

let getSwateProtocolGroupForCurrentTable tableName worksheetName (xmlParsed:XmlElement) =
    let activeTableXml = getActiveTableXml tableName worksheetName xmlParsed
    if activeTableXml.IsNone then
        None
    else
        let v = SimpleXml.findElementsByName Xml.GroupTypes.ProtocolGroupXmlRoot activeTableXml.Value
        if v.Length > 1 then failwith (sprintf "Swate found multiple '<%s>' xml elements. Please contact the developer." Xml.GroupTypes.ProtocolGroupXmlRoot)
        if v.Length = 0 then
            None
        else
            let tableXmlAsString = activeTableXml.Value |> xmlElementToXmlString
            Xml.GroupTypes.ProtocolGroup.ofXml tableXmlAsString |> Some

/// Use the 'remove' parameter to remove any Swate protocol group xml for the worksheet annotation table name combination in 'protocolGroup'
let updateRemoveSwateProtocolGroup (protocolGroup:Xml.GroupTypes.ProtocolGroup) (previousCompleteCustomXml:XmlElement) (remove:bool) =

    let currentTableXml = getActiveTableXml protocolGroup.AnnotationTable.Name protocolGroup.AnnotationTable.Worksheet previousCompleteCustomXml

    let nextTableXml =
        let newProtocolGroupXml = protocolGroup.toXml |> SimpleXml.parseElement
        if currentTableXml.IsSome then
            let filteredChildren =
                currentTableXml.Value.Children
                |> List.filter (fun x -> x.Name <> Xml.GroupTypes.ProtocolGroupXmlRoot )
            {currentTableXml.Value with
                Children =
                    if remove then
                        filteredChildren
                    else
                        if filteredChildren.IsEmpty then [newProtocolGroupXml] else newProtocolGroupXml::filteredChildren
            }
        else
            let initNewSwateTableXml =
                sprintf """<SwateTable Table="%s" Worksheet="%s"></SwateTable>""" protocolGroup.AnnotationTable.Name protocolGroup.AnnotationTable.Worksheet
            let swateTableXmlEle = initNewSwateTableXml |> SimpleXml.parseElement
            {swateTableXmlEle with
                Children = [newProtocolGroupXml]
            }

    let filterPrevTableFromRootChildren =
        previousCompleteCustomXml.Children
        |> List.filter (fun x ->
            let isExisting =
                x.Name = "SwateTable"
                && x.Attributes.["Table"] = protocolGroup.AnnotationTable.Name
                && x.Attributes.["Worksheet"] = protocolGroup.AnnotationTable.Worksheet
            isExisting |> not
        )

    {previousCompleteCustomXml with
        Children = nextTableXml::filterPrevTableFromRootChildren
    }

//let removeSwateProtocolGroup (protocolGroup:Xml.GroupTypes.ProtocolGroup) (previousCompleteCustomXml:XmlElement) =
//    updateRemoveSwateProtocolGroup protocolGroup previousCompleteCustomXml true

//let updateSwateProtocolGroup (protocolGroup:Xml.GroupTypes.ProtocolGroup) (previousCompleteCustomXml:XmlElement) =
//    updateRemoveSwateProtocolGroup protocolGroup previousCompleteCustomXml false

let replaceProtGroupByProtGroup protGroup1 protGroup2 (previousCompleteCustomXml:XmlElement) =
    let removeProtGroup1 = updateRemoveSwateProtocolGroup protGroup1 previousCompleteCustomXml true
    let addProtGroup2 = updateRemoveSwateProtocolGroup protGroup2 removeProtGroup1 false
    addProtGroup2

/// Use the 'remove' parameter to remove any Swate protocol xml for the worksheet annotation table name combination in 'protocolGroup'
let updateRemoveSwateProtocol (protocol:Xml.GroupTypes.Protocol) (previousCompleteCustomXml:XmlElement) (remove:bool)=

    let currentSwateProtocolGroup =
        let isExisting = getSwateProtocolGroupForCurrentTable protocol.AnnotationTable.Name protocol.AnnotationTable.Worksheet previousCompleteCustomXml
        if isExisting.IsNone then
            Xml.GroupTypes.ProtocolGroup.create protocol.SwateVersion protocol.AnnotationTable.Name protocol.AnnotationTable.Worksheet []
        else
            isExisting.Value

    let filteredProtocolChildren =
        currentSwateProtocolGroup.Protocols
        |> List.filter (fun x -> x.Id <> protocol.Id)

    let nextProtocolGroup =
        {currentSwateProtocolGroup with
            Protocols =
                if remove then
                    filteredProtocolChildren
                else
                    if filteredProtocolChildren.IsEmpty then [protocol] else protocol::filteredProtocolChildren
        }

    updateRemoveSwateProtocolGroup nextProtocolGroup previousCompleteCustomXml false

//let removeSwateProtocol (protocol:Xml.GroupTypes.Protocol) (previousCompleteCustomXml:XmlElement) =
//    updateRemoveSwateProtocol protocol previousCompleteCustomXml true

//let updateSwateProtocol (protocol:Xml.GroupTypes.Protocol) (previousCompleteCustomXml:XmlElement) =
//    updateRemoveSwateProtocol protocol previousCompleteCustomXml false

let updateProtocolFromXml (protocol:Xml.GroupTypes.Protocol) (remove:bool) =
    Excel.run(fun context ->

        let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

        promise {
            let! annotationTable = getActiveAnnotationTableName()

            let! xmlParsed = getCustomXml customXmlParts context

            // Not sure if this is necessary. Previously table and worksheet name were accessed at this point.
            // Then AnnotationTable was added to protocol. So now we refresh these values at this point.
            let securityUpdateForProtocol = {protocol with AnnotationTable = AnnotationTable.create annotationTable activeSheet.name}

            let nextCustomXml =
                updateRemoveSwateProtocol securityUpdateForProtocol xmlParsed remove

            let nextCustomXmlString = nextCustomXml |> xmlElementToXmlString

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
                    "%s ProtocolGroup Scheme with '%s - %s - %s' "
                    (if remove then "Remove Protocol from" else "Update")
                    activeSheet.name
                    annotationTable
                    protocol.Id
        }
    )

[<System.Obsolete>]
/// range -> 'let groupHeader = annoHeaderRange.getRowsAbove(1.)'
let formatGroupHeaderForRange (range:Excel.Range) (context:RequestContext) =
    promise {

        let! range = context.sync().``then``(fun e ->
            range.load(U2.Case2 (ResizeArray(["format"])))
        )

        let! colorAndGetBorderItems = context.sync().``then``(fun e ->
    
            let f = range.format
    
            f.fill.color <- "#70AD47"
            f.font.color <- "white"
            f.font.bold <- true
            f.horizontalAlignment <- U2.Case2 "center"
    
            let format = f.load(U2.Case2 (ResizeArray(["borders"])))
            format.borders.load(propertyNames = U2.Case2 (ResizeArray(["items"])))
        )
    
        let! borderItems = context.sync().``then``(fun e ->
            colorAndGetBorderItems.items |> Array.ofSeq |> Array.map (fun x -> x.load(propertyNames = U2.Case2 (ResizeArray(["sideIndex"; "color"]))) )
        )
        let! colorBorder = context.sync().``then``(fun e ->
            let color = borderItems |> Array.map (fun x ->
                if x.sideIndex = U2.Case2 "InsideVertical" then
                    x.color <- "white"
            )
            color
        )
        ()
    }

[<System.Obsolete>]
/// range -> 'let groupHeader = annoHeaderRange.getRowsAbove(1.)'
let cleanGroupHeaderFormat (range:Excel.Range) (context:RequestContext) =
    promise {
        // unmerge group header
        let! unmerge = context.sync().``then``(fun e ->
            range.unmerge()

        )
        // empty group header
        let! groupHeaderValues = context.sync().``then``(fun e ->
            range.load(U2.Case1 "values")
        )
        // empty group header part 2
        let! emptyGroupHeaderValues = context.sync().``then``(fun e ->
            let nV =
                groupHeaderValues.values
                |> Seq.map (fun innerArr ->
                    innerArr 
                    |> Seq.map (fun _ ->
                        "" |> box |> Some
                    ) |> ResizeArray
                ) |> ResizeArray
            groupHeaderValues.values <- nV
        )

        return ()
    }

