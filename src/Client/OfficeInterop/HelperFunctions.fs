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

//module BuildingBlockTypes =

//    open System

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

