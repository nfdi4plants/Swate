module JSONExport

open System
open ISADotNet
open Shared.OfficeInteropTypes

type Column with
    member this.toMatrixElement() =
        let header = this.Header.SwateColumnHeader
        this.Cells
        |> Array.map (fun cell ->
            (cell.Index, header), Option.defaultValue "" cell.Value
        )

let parseBuildingBlockToMatrix (protocolName:string) (buildingBlocks:BuildingBlock []) =
    let matrixHeaders =
        buildingBlocks
        |> Array.collect (fun bb -> [|
            bb.MainColumn.Header.SwateColumnHeader
            if bb.hasUnit then
                bb.Unit.Value.Header.SwateColumnHeader
            if bb.hasCompleteTSRTAN then
                bb.TSR.Value.Header.SwateColumnHeader
                bb.TAN.Value.Header.SwateColumnHeader
        |])
    let matrixArr =
        buildingBlocks
        |> Array.collect (fun bb -> [|
            yield! bb.MainColumn.toMatrixElement()
            if bb.hasUnit then
                yield! bb.Unit.Value.toMatrixElement()
            if bb.hasCompleteTSRTAN then
                yield! bb.TSR.Value.toMatrixElement()
                yield! bb.TAN.Value.toMatrixElement()
        |])
    let matrix = Collections.Generic.Dictionary<(int*string),string>(Map.ofArray matrixArr)
    matrixHeaders, matrix

let parseBuildingBlockToAssay (protocolName:string) (buildingBlocks:BuildingBlock []) =
    let matrixHeaders, matrix = parseBuildingBlockToMatrix protocolName buildingBlocks
    ISADotNet.XLSX.AssayFile.Assay.fromSparseMatrix protocolName matrixHeaders matrix

let parseBuildingBlockSeqsToAssay (worksheetNameBuildingBlocks: (string*BuildingBlock []) []) =
    let matrices =
        worksheetNameBuildingBlocks
        |> Array.map (fun (protocolName, buildingBlocks) ->
            let matrixHeaders, matrix = parseBuildingBlockToMatrix protocolName buildingBlocks
            protocolName, Seq.ofArray matrixHeaders, matrix
        )
    ISADotNet.XLSX.AssayFile.Assay.fromSparseMatrices matrices
