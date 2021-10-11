module JSONExport

open System
open ISADotNet
open Shared.OfficeInteropTypes

type Column with
    member this.toMatrixElement() =
        let header = this.Header.SwateColumnHeader
        this.Cells
        |> Array.map (fun cell ->
            (header, cell.Index), Option.defaultValue "" cell.Value
        )

let parseBuildingBlockToProtocol (protocolName:string) (buildingBlocks:BuildingBlock []) =
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
    let matrixLength = matrixArr |> Array.maxBy (fst >> snd) |> (fst >> snd)
    let matrix = Collections.Generic.Dictionary<(string*int),string>(Map.ofArray matrixArr)
    (*let materials, factors, protocol, processes = *)
    ISADotNet.XLSX.AssayFile.AssayFile.fromSparseMatrix protocolName matrixHeaders matrixLength matrix
