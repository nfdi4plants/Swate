module OfficeInterop.HelperFunctions

open Fable.Core
open Fable.Core.JsInterop
open OfficeJS
open Excel
open System.Collections.Generic
open System.Text.RegularExpressions

open OfficeInterop.Regex
open OfficeInterop.Types
open BuildingBlockTypes
open Shared

let getActiveAnnotationTableName (context:RequestContext)=
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

let createEmptyMatrixForTables (colCount:int) (rowCount:int) value =
    [|
        for i in 0 .. rowCount-1 do
            yield   [|
                for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
            |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

let tryFindSpannedBuildingBlocks (currentProtocolGroup:Xml.GroupTypes.Protocol) (buildingBlocks: BuildingBlock []) =
    let findAllSpannedBlocks =
        currentProtocolGroup.SpannedBuildingBlocks
        |> List.choose (fun spannedBlock ->
            buildingBlocks
            |> Array.tryFind (fun foundBuildingBlock ->
                let isSameAccession =
                    if spannedBlock.TermAccession <> "" && foundBuildingBlock.MainColumn.Header.Value.Ontology.IsSome then
                        foundBuildingBlock.MainColumn.Header.Value.Ontology.Value.TermAccession = spannedBlock.TermAccession
                    else
                        false
                foundBuildingBlock.MainColumn.Header.Value.Header = spannedBlock.ColumnName
                && isSameAccession
            )
        )
    //let reduce = findAllSpannedBlocks |> List.map (fun x -> x.MainColumn.Header.Value.Header)
    if findAllSpannedBlocks.Length = currentProtocolGroup.SpannedBuildingBlocks.Length then
        Some findAllSpannedBlocks
    else
        None

/// This will create the column header attributes for a unit block.
/// as unit always has to be a term and cannot be for example "Source" or "Sample", both of which have a differen format than for exmaple "Parameter [TermName]",
/// we only need one function to generate id and attributes and bring the unit term in the right format.
let unitColAttributes (unitTermName:string) (unitAccessionOptt:string option) (id:int) =
    match id with
    | 1 ->
        match unitAccessionOptt with
        | Some accession    -> sprintf "[%s] (#h; #t%s; #u)" unitTermName accession
        | None              -> sprintf "[%s] (#h; #u)" unitTermName
    | _ ->
        match unitAccessionOptt with
        | Some accession    -> sprintf "[%s] (#%i; #h; #t%s; #u)" unitTermName id accession
        | None              -> sprintf "[%s] (#%i; #h; #u)" unitTermName id

let createUnitColumns (allColHeaders:string []) (annotationTable:Table) newBaseColIndex rowCount (format:string option) (unitAccessionOpt:string option) =
    let col = createEmptyMatrixForTables 1 rowCount ""
    if format.IsSome then
        let findNewIdForUnit() =
            let rec loopingCheck int =
                let isExisting =
                    allColHeaders
                    // Should a column with the same name already exist, then count up the id tag.
                    |> Array.exists (fun existingHeader ->
                        // We don't need to check TSR or TAN, because the main column always starts with "Unit"
                        existingHeader = sprintf "Unit %s" (unitColAttributes format.Value unitAccessionOpt int)
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
                index = newBaseColIndex+3.,
                values = U4.Case1 col,
                name = sprintf "Unit %s" (unitColAttributes format.Value unitAccessionOpt newUnitId)
            )

        /// create unit TSR
        let createdUnitCol2 =
            annotationTable.columns.add(
                index = newBaseColIndex+4.,
                values = U4.Case1 col,
                name = sprintf "Term Source REF %s" (unitColAttributes format.Value unitAccessionOpt newUnitId)
            )

        /// create unit TAN
        let createdUnitCol3 =
            annotationTable.columns.add(
                index = newBaseColIndex+5.,
                values = U4.Case1 col,
                name = sprintf "Term Accession Number %s" (unitColAttributes format.Value unitAccessionOpt newUnitId)
            )

        Some (
            sprintf " Added specified unit: %s" (format.Value),
            sprintf "0.00 \"%s\"" (format.Value)
        )
    else
        None

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
            let prep = parseColHeader (string x.Value) 
            if x.IsSome && prep.TagArr.IsSome then
                let checkIsHidden =
                    prep.TagArr.Value |> Array.contains ColumnTags.HiddenTag
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
    
    /// This function is part 1 to get a 'BuildingBlock []' representation of a Swate table.
    /// It should be used as follows: 'let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable'
    /// This function will load all necessery excel properties.
    let getBuildingBlocksPreSync (context:RequestContext) annotationTable =
        let sheet = context.workbook.worksheets.getActiveWorksheet()
        let annotationTable = sheet.tables.getItem(annotationTable)
        let annoHeaderRange = annotationTable.getHeaderRowRange()
        let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
        let annoBodyRange = annotationTable.getDataBodyRange()
        let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"|])) |> ignore
        annoHeaderRange, annoBodyRange

    /// This function is part 2 to get a 'BuildingBlock []' representation of a Swate table.
    /// It's parameters are the output of 'getBuildingBlocksPreSync' and it will return a full 'BuildingBlock []'.
    /// It MUST be used either in or after 'context.sync().``then``(fun e -> ..)' after 'getBuildingBlocksPreSync'.
    let getBuildingBlocks (annoHeaderRange:OfficeJS.Excel.Range) (annoBodyRange:OfficeJS.Excel.Range) =

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
                    /// DEPRECATED! For now we keep "x.StartsWith ColumnTags.UnitTag" instead of contains, as we (>0.2.1) added accession number behind unit tag
                    if nextCol.Header.Value.TagArr.Value |> Array.exists (fun x -> x.StartsWith ColumnTags.UnitTag) |> not then
                        let updateCurrentBlock = checkForHiddenColType currentBlock nextCol
                        sortColsIntoBuildingBlocks (index+1) updateCurrentBlock buildingBlockList
                    /// Next we check for unit columns in the scheme of `Unit [Term] (#h; #u...) | TSR [Term] (#h; #u...) | TAN [Term] (#h; #u...)`
                    /// DEPRECATED! For now we keep "x.StartsWith ColumnTags.UnitTag" instead of contains, as we once (>0.2.1) added accession number behind unit tag
                    elif nextCol.Header.Value.TagArr.Value |> Array.exists (fun x -> x.StartsWith ColumnTags.UnitTag) then
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
        let buildingBlocksPre =
            sortColsIntoBuildingBlocks 0 None []
            |> List.rev
            |> Array.ofList

        // UPDATE IN > 0.2.0
        /// As we now add the TermAccession as "#txxx" tag in the reference columns we walk over all buildingBlock and update the maincolumn header accordingly.
        let buildingBlocks =
            buildingBlocksPre
            |> Array.map (fun buildingBlock ->
                match buildingBlock.TAN, buildingBlock.TSR with
                | Some tan, Some tsr ->
                    match tan.Header.Value.Ontology, tsr.Header.Value.Ontology with
                    | Some ont1, Some ont2 ->
                        let isSame = ont1.TermAccession = ont2.TermAccession
                        if isSame |> not then
                            failwith (sprintf "During BuildingBlock update with TermAccession found BuildingBlock (%s) with unknow TAN TSR pattern. (3)" buildingBlock.MainColumn.Header.Value.Header)
                        if ont1.TermAccession <> "" then
                            let nextMainColumn = {
                                buildingBlock.MainColumn with
                                    Header =  {
                                        buildingBlock.MainColumn.Header.Value with
                                            Ontology = {
                                                buildingBlock.MainColumn.Header.Value.Ontology.Value with
                                                    TermAccession = ont1.TermAccession
                                            } |> Some
                                    } |> Some
                            }
                            { buildingBlock with MainColumn = nextMainColumn }
                        else
                            buildingBlock
                    | None, None ->
                        buildingBlock
                    | _,_ ->
                        failwith (sprintf "During BuildingBlock update with TermAccession found BuildingBlock (%s) with unknow TAN TSR pattern. (2)" buildingBlock.MainColumn.Header.Value.Header)
                | None, None ->
                    buildingBlock
                | _, _ ->
                    failwith (sprintf "During BuildingBlock update with TermAccession found BuildingBlock (%s) with unknow TAN TSR pattern." buildingBlock.MainColumn.Header.Value.Header)
            )

        buildingBlocks

    open System

    let findSelectedBuildingBlockPreSync (context:RequestContext) annotationTableName =
        let selectedRange = context.workbook.getSelectedRange()
        let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))

        // Ref. 2
        let annoHeaderRange, annoBodyRange = getBuildingBlocksPreSync context annotationTableName
        selectedRange, annoHeaderRange, annoBodyRange

    let findSelectedBuildingBlock (selectedRange:Excel.Range) (annoHeaderRange:Excel.Range) (annoBodyRange:Excel.Range) (context:RequestContext) =
        context.sync().``then``( fun _ ->

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

            findLeftClosestBuildingBlock
        )

    let private sortMainColValuesToSearchTerms (buildingBlock:BuildingBlock) =
        // get current col index
        let tsrTanColIndices = [|buildingBlock.TSR.Value.Index; buildingBlock.TAN.Value.Index|]
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
        if buildingBlock.Unit.IsSome then
            fillTermConstructsWithUnit buildingBlock
        else
            fillTermConstructsNoUnit buildingBlock

    let private sortUnitColToSearchTerm (buildingBlock:BuildingBlock) =
        let unit = buildingBlock.Unit.Value
        let searchString  = unit.MainColumn.Header.Value.Ontology.Value.Name
        let termAccession = unit.MainColumn.Header.Value.Ontology.Value.TermAccession 
        let colIndices = [|unit.MainColumn.Index; unit.TSR.Value.Index; unit.TAN.Value.Index|]
        let rowIndices = unit.MainColumn.Cells |> Array.map (fun x -> x.Index)
        Shared.SearchTermI.create colIndices searchString termAccession None rowIndices

    let private sortHeaderToSearchTerm (buildingBlock:BuildingBlock) =
        let searchString  = buildingBlock.MainColumn.Header.Value.Ontology.Value.Name
        let termAccession = buildingBlock.MainColumn.Header.Value.Ontology.Value.TermAccession 
        let colIndices = [|buildingBlock.MainColumn.Index; buildingBlock.TSR.Value.Index; buildingBlock.TAN.Value.Index|]
        let rowIndices = buildingBlock.MainColumn.Cells |> Array.map (fun x -> x.Index)
        Shared.SearchTermI.create colIndices searchString termAccession None rowIndices

    let sortBuildingBlockValuesToSearchTerm (buildingBlock:BuildingBlock) =

        /// We need an array of all distinct cell.values and where they occur in col- and row-index
        let terms() = sortMainColValuesToSearchTerms buildingBlock

        /// Create search types for the unit building blocks.
        let units() = sortUnitColToSearchTerm buildingBlock

        match buildingBlock.TAN, buildingBlock.TSR with
        | Some _, Some _ ->
            /// Combine search types
            [|
                yield! terms()
                if buildingBlock.Unit.IsSome then
                    yield units()
            |]
        | None, None ->
            let searchString  = ""
            let termAccession = ""
            let colIndices = [|buildingBlock.MainColumn.Index|]
            let rowIndices = buildingBlock.MainColumn.Cells |> Array.map (fun x -> x.Index)
            [| Shared.SearchTermI.create colIndices searchString termAccession None rowIndices |]
        | _ -> failwith (sprintf "Encountered unknown reference column pattern. Building block (%s) can only contain both TSR and TAN or none." buildingBlock.MainColumn.Header.Value.Header)

    let sortBuildingBlockToSearchTerm (buildingBlock:BuildingBlock) =
        [|yield! sortBuildingBlockValuesToSearchTerm buildingBlock; sortHeaderToSearchTerm buildingBlock|] 

    let sortBuildingBlocksValuesToSearchTerm (buildingBlocks:BuildingBlock []) =

        buildingBlocks |> Array.collect (fun bb ->
            sortBuildingBlockValuesToSearchTerm bb
        )

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

        return xmlParsed, xml
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

let updateSwateValidation (tableValidation:Xml.ValidationTypes.TableValidation) (previousCompleteCustomXml:XmlElement) =

    let currentTableXml = getActiveTableXml tableValidation.TableName tableValidation.WorksheetName previousCompleteCustomXml

    let nextTableXml =
        let newValidationXml = tableValidation.toXml |> SimpleXml.parseElement
        if currentTableXml.IsSome then
            let filteredChildren =
                currentTableXml.Value.Children
                |> List.filter (fun x -> x.Name <> Xml.ValidationTypes.ValidationXmlRoot )
            {currentTableXml.Value with
                Children = newValidationXml::filteredChildren
            }
        else
            let initNewSwateTableXml =
                sprintf """<SwateTable Table="%s" Worksheet="%s"></SwateTable>""" tableValidation.TableName tableValidation.WorksheetName
            let swateTableXmlEle = initNewSwateTableXml |> SimpleXml.parseElement
            {swateTableXmlEle with
                Children = [newValidationXml]
            }
    let filterPrevTableFromRootChildren =
        previousCompleteCustomXml.Children
        |> List.filter (fun x ->
            let isExisting =
                x.Name = "SwateTable"
                && x.Attributes.["Table"] = tableValidation.TableName
                && x.Attributes.["Worksheet"] = tableValidation.WorksheetName
            isExisting |> not
        )
    {previousCompleteCustomXml with
        Children = nextTableXml::filterPrevTableFromRootChildren
    }

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

let updateSwateProtocolGroup (protocolGroup:Xml.GroupTypes.ProtocolGroup) (previousCompleteCustomXml:XmlElement) =

    let currentTableXml = getActiveTableXml protocolGroup.TableName protocolGroup.WorksheetName previousCompleteCustomXml

    let nextTableXml =
        let newProtocolGroupXml = protocolGroup.toXml |> SimpleXml.parseElement
        if currentTableXml.IsSome then
            let filteredChildren =
                currentTableXml.Value.Children
                |> List.filter (fun x -> x.Name <> Xml.GroupTypes.ProtocolGroupXmlRoot )
            {currentTableXml.Value with
                Children = newProtocolGroupXml::filteredChildren
            }
        else
            let initNewSwateTableXml =
                sprintf """<SwateTable Table="%s" Worksheet="%s"></SwateTable>""" protocolGroup.TableName protocolGroup.WorksheetName
            let swateTableXmlEle = initNewSwateTableXml |> SimpleXml.parseElement
            {swateTableXmlEle with
                Children = [newProtocolGroupXml]
            }
    let filterPrevTableFromRootChildren =
        previousCompleteCustomXml.Children
        |> List.filter (fun x ->
            let isExisting =
                x.Name = "SwateTable"
                && x.Attributes.["Table"] = protocolGroup.TableName
                && x.Attributes.["Worksheet"] = protocolGroup.WorksheetName
            isExisting |> not
        )
    {previousCompleteCustomXml with
        Children = nextTableXml::filterPrevTableFromRootChildren
    }

let updateSwateProtocolGroupByProtocol tableName worksheetName (protocol:Xml.GroupTypes.Protocol) (previousCompleteCustomXml:XmlElement) =

    let currentSwateProtocolGroup =
        let isExisting = getSwateProtocolGroupForCurrentTable tableName worksheetName previousCompleteCustomXml
        if isExisting.IsNone then
            Xml.GroupTypes.ProtocolGroup.create protocol.SwateVersion tableName worksheetName []
        else
            isExisting.Value

    let filteredProtocolChildren =
        currentSwateProtocolGroup.Protocols
        |> List.filter (fun x -> x.Id <> protocol.Id)

    let nextProtocolGroup =
        {currentSwateProtocolGroup with
            Protocols = protocol::filteredProtocolChildren
        }

    updateSwateProtocolGroup nextProtocolGroup previousCompleteCustomXml

let updateProtocolFromXml (protocol:Xml.GroupTypes.Protocol) (remove:bool) =
    Excel.run(fun context ->

        let activeSheet = context.workbook.worksheets.getActiveWorksheet().load(propertyNames = U2.Case2 (ResizeArray[|"name"|]))

        // The first part accesses current CustomXml
        let workbook = context.workbook.load(propertyNames = U2.Case2 (ResizeArray[|"customXmlParts"|]))
        let customXmlParts = workbook.customXmlParts.load (propertyNames = U2.Case2 (ResizeArray[|"items"|]))

        promise {
            let! annotationTable = getActiveAnnotationTableName context

            let! xmlParsed, xml = getCustomXml customXmlParts context

            let nextCustomXml = updateSwateProtocolGroupByProtocol annotationTable activeSheet.name protocol xmlParsed

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

let createValueMatrix (colCount:int) (rowCount:int) value =
    ResizeArray([
        for outer in 0 .. rowCount-1 do
            let tmp = Array.zeroCreate colCount |> Seq.map (fun _ -> Some (value |> box))
            ResizeArray(tmp)
    ])

/// Not used currently
let createEmptyAnnotationMatrixForTables (rowCount:int) value (header:string) =
    [|
        for ind in 0 .. rowCount-1 do
            yield   [|
                for i in 0 .. 2 do
                    yield
                        match ind, i with
                        | 0, 0 ->
                            U3<bool,string,float>.Case2 header
                        | 0, 1 ->
                            U3<bool,string,float>.Case2 "Term Source REF"
                        | 0, 2 ->
                            U3<bool,string,float>.Case2 "Term Accession Number"
                        | _, _ ->
                            U3<bool,string,float>.Case2 value
            |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>
