module OfficeInterop.BuildingBlockFunctions

open Fable.Core
open ExcelJS.Fable
open Excel

open Shared.OfficeInteropTypes
open Shared.OfficeInteropTypes.BuildingBlockTypes
open Shared.TermTypes
open Indexing

/// Swaps 'Rows with column values' to 'Columns with row values'.
let private viewRowsByColumns (rows:ResizeArray<ResizeArray<'a>>) =
    rows
    |> Seq.collect (fun x -> Seq.indexed x)
    |> Seq.groupBy fst
    |> Seq.map (snd >> Seq.map snd >> Seq.toArray)
    |> Seq.toArray

// ExcelApi 1.1
/// This function is part 1 to get a 'BuildingBlock []' representation of a Swate table.
/// It should be used as follows: 'let annoHeaderRange, annoBodyRange = BuildingBlockTypes.getBuildingBlocksPreSync context annotationTable'
/// This function will load all necessery excel properties.
let private getBuildingBlocksPreSync (context:RequestContext) annotationTable =
    let sheet = context.workbook.worksheets.getActiveWorksheet()
    let annotationTable = sheet.tables.getItem(annotationTable)
    let annoHeaderRange = annotationTable.getHeaderRowRange()
    let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
    let annoBodyRange = annotationTable.getDataBodyRange()
    let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"; "numberFormat"|])) |> ignore
    annoHeaderRange, annoBodyRange

let private getBuildingBlocksPreSyncFromTable (annotationTable:Table) =
    let annoHeaderRange = annotationTable.getHeaderRowRange()
    let _ = annoHeaderRange.load(U2.Case2 (ResizeArray [|"columnIndex"; "values"; "columnCount"|])) |> ignore
    let annoBodyRange = annotationTable.getDataBodyRange()
    let _ = annoBodyRange.load(U2.Case2 (ResizeArray [|"values"; "numberFormat"|])) |> ignore
    annoHeaderRange, annoBodyRange

// ExcelApi 1.1
/// This function is part 2 to get a 'BuildingBlock []' representation of a Swate table.
/// It's parameters are the output of 'getBuildingBlocksPreSync' and it will return a full 'BuildingBlock []'.
/// It MUST be used either in or after 'context.sync().``then``(fun e -> ..)' after 'getBuildingBlocksPreSync'.
let private getBuildingBlocksPostSync (annoHeaderRange:Excel.Range) (annoBodyRange:Excel.Range) (context:RequestContext) =

    context.sync().``then``(fun _ ->
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
                                // start cell row index by 1, as 0 will later be used for the header
                                Cell.create (i+1) cellValue cellUnit
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
                    match termName, tsrTermAccession, tanTermAccession with
                    // complete term
                    | Some termName, Some accession1, Some accession2 ->
                        if accession1 <> accession2 then failwith $"Swate found mismatching term accession in building block {bb.MainColumn.Header}: {accession1}, {accession2}"
                        TermMinimal.create termName accession1 |> Some
                    // free text input term
                    | Some termName, None, None ->
                        TermMinimal.create termName "" |> Some
                    // this is a uncomplete column with no found term name but term accession. Column needs manual curation
                    | None, Some _, Some _ ->
                        failwith $"Swate found mismatching ontology term infor in building block {bb.MainColumn.Header}: Found term accession in reference columns, but no ontology ref in main column."
                    | None, None, None -> None
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
    )

type BuildingBlock with
    static member getFromContext(context:RequestContext,annotationTableName:string) =
        let annoHeaderRange, annoBodyRange = getBuildingBlocksPreSync context annotationTableName
        getBuildingBlocksPostSync annoHeaderRange annoBodyRange context

    static member getFromContext(context:RequestContext,annotationTable:Table) =
        let annoHeaderRange, annoBodyRange = getBuildingBlocksPreSyncFromTable annotationTable
        getBuildingBlocksPostSync annoHeaderRange annoBodyRange context

let getBuildingBlocks (context:RequestContext) (annotationTableName:string) =

    let annoHeaderRange, annoBodyRange = getBuildingBlocksPreSync context annotationTableName

    getBuildingBlocksPostSync annoHeaderRange annoBodyRange context


// ExcelApi 1.1
let findSelectedBuildingBlockPreSync (context:RequestContext) annotationTableName =
    let selectedRange = context.workbook.getSelectedRange()
    let _ = selectedRange.load(U2.Case2 (ResizeArray(["values";"columnIndex"; "columnCount"])))

    // Ref. 2
    let annoHeaderRange, annoBodyRange = getBuildingBlocksPreSync context annotationTableName
    selectedRange, annoHeaderRange, annoBodyRange

open ExcelJS.Fable

// ExcelApi 1.1
let findSelectedBuildingBlockPostSync (selectedRange:Excel.Range) (annoHeaderRange:Excel.Range) (annoBodyRange:Excel.Range) (context:RequestContext) =
    promise {

        /// Sort all columns into building blocks.
        let! buildingBlocks = getBuildingBlocksPostSync annoHeaderRange annoBodyRange context

        let! selectedBuildingBlock = context.sync().``then``( fun _ ->

            if selectedRange.columnCount <> 1. then
                failwith "To use this function please select a single column"

            let errorMsg = "To use this function please select a single column of a Swate table."

            let newSelectedColIndex =
                // recalculate the selected range index based on table
                let diff = selectedRange.columnIndex - annoHeaderRange.columnIndex
                // if index is smaller 0 it is outside of table range
                if diff < 0. then failwith errorMsg
                // if index is bigger than columnCount-1 then it is outside of tableRange
                elif diff > annoHeaderRange.columnCount-1. then failwith errorMsg
                else diff

            /// find building block with the closest main column index from left
            let findLeftClosestBuildingBlock =
                buildingBlocks
                |> Array.filter (fun x -> x.MainColumn.Index <= int newSelectedColIndex)
                |> Array.minBy (fun x -> System.Math.Abs(x.MainColumn.Index - int newSelectedColIndex))

            findLeftClosestBuildingBlock
        )

        return selectedBuildingBlock
    }

let findSelectedBuildingBlock (context:RequestContext) (annotationTableName:string) =

    let selectedRange, annoHeaderRange, annoBodyRange = findSelectedBuildingBlockPreSync context annotationTableName

    findSelectedBuildingBlockPostSync selectedRange annoHeaderRange annoBodyRange context

let toTermSearchable (buildingBlock:BuildingBlock) =
    let colIndex = buildingBlock.MainColumn.Index
    let parentTerm = buildingBlock.MainColumnTerm
    let allUnits =
        if buildingBlock.hasUnit then
            buildingBlock.MainColumn.Cells
            // get all units from cells
            |> Array.map (fun cell -> cell.Unit, cell.Index)
            // filter units to unique
            |> Array.choose (fun (unitName,rowInd) -> if unitName.IsSome then Some (unitName.Value,rowInd) else None)
            |> Array.groupBy fst
            // get only units where unit.isSome
            |> Array.map (fun (unitTerm,cellInfoArr) ->
                let cellRowIndices = cellInfoArr |> Array.map snd |> Array.distinct
                // will not contain termAccession
                TermSearchable.create unitTerm None true colIndex cellRowIndices
            )
        else
            [||]
    let allTermValues =
        if buildingBlock.hasCompleteTSRTAN && not buildingBlock.hasUnit then
            buildingBlock.MainColumn.Cells
            // get all units from cells
            |> Array.map (fun cell -> cell.Value, cell.Index)
            // filter units to unique
            |> Array.choose (fun (valueName,rowInd) -> if valueName.IsSome then Some (valueName.Value,rowInd) else None)
            |> Array.groupBy fst
            // get only values where value.isSome
            |> Array.map (fun (valueName,cellInfoArr) ->
                let cellRowIndices = cellInfoArr |> Array.map snd |> Array.distinct
                let term = TermMinimal.create valueName ""
                TermSearchable.create term parentTerm false colIndex cellRowIndices
            )
        else
            [||]

    [|
        if parentTerm.IsSome then TermSearchable.create parentTerm.Value None false colIndex [|0|]
        yield! allUnits
        yield! allTermValues
    |]
    |> Array.filter (fun x -> x.hasEmptyTerm |> not)