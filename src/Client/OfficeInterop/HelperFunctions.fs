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


let createEmptyMatrixForTables (colCount:int) (rowCount:int) value =
    [|
        for i in 0 .. rowCount-1 do
            yield   [|
                for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
            |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

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
        | Some accession    -> sprintf "[%s] (#%i; #h; #t%s #u)" unitTermName id accession
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
                    Some (i + 1 |> float)
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
            newInd
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
                    /// DEPRECATED! For now we keep "x.StartsWith ColumnTags.UnitTag" instead of contains, as we once (>0.2.1) added accession number behind unit tag
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

let getCurrentCustomXml (customXmlParts:CustomXmlPartCollection) (context:RequestContext) =
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
            if xml = "" then "<customXml></customXml>" |> SimpleXml.parseElement
            elif isRootElement.IsSome then
                isRootElement.Value
            else
                let isManyRootElements = xml |> SimpleXml.tryParseManyElements
                if isManyRootElements.IsSome then
                    isManyRootElements.Value
                    |> List.tryFind (fun ele -> ele.Name = "customXml")
                    |> fun customXmlOpt -> if customXmlOpt.IsSome then customXmlOpt.Value else failwith "Swate could not find expected 'customXml' root tag."
                else
                    failwith "Swate could not parse Workbook Custom Xml Parts. Had neither one root nor many root elements. Please contact the developer."
        if xmlParsed.Name <> "customXml" then failwith (sprintf "Swate found unexpected root xml element: %s" xmlParsed.Name)

        return xmlParsed, xml
    }

let swateValidationOfXml (xmlParsed:XmlElement) (xml:string) =
    let v = SimpleXml.findElementsByName "Validation" xmlParsed
    if v.Length > 1 then failwith (sprintf "Swate found multiple 'Validation' xml elements. Please contact the developer.")
    if v.Length = 0 then
        None
    else
        Xml.ValidationTypes.SwateValidation.ofXml xml |> Some

let protocolGroupsOfXml (xmlParsed:XmlElement) (xml:string) =
    let protocolGroupTag = "ProtocolGroup"
    let v = SimpleXml.findElementsByName protocolGroupTag xmlParsed
    if v.Length > 1 then failwith (sprintf "Swate found multiple '%s' xml elements. Please contact the developer." protocolGroupTag)
    if v.Length = 0 then
        None
    else
        Xml.GroupTypes.ProtocolGroup.ofXml xml |> Some

let createGroupHeaderFormatForRange (range:Excel.Range) (context:RequestContext) =
    promise {

        let! range = context.sync().``then``(fun e ->
            range.load(U2.Case2 (ResizeArray(["format"])))
        )

        let! colorAndGetBorderItems = context.sync().``then``(fun e ->
    
            let f = range.format
    
            f.fill.color <- "#70AD47"
            f.font.color <- "white"
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
