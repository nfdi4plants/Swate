module Export

open System
open ISADotNet
open Shared.OfficeInteropTypes

//type Column with
//    member this.toMatrixElement() =
//        let header = this.Header.SwateColumnHeader
//        this.Cells
//        |> Array.map (fun cell ->
//            (cell.Index, header), Option.defaultValue "" cell.Value
//        )
//    member this.toMatrixElement(rebaseIndex:int) =
//        let header = this.Header.SwateColumnHeader
//        this.Cells
//        |> Array.map (fun cell ->
//            (cell.Index-rebaseIndex, header), Option.defaultValue "" cell.Value
//        )

//let parseBuildingBlockToMatrix (buildingBlocks:BuildingBlock []) =
//    let matrixHeaders =
//        buildingBlocks
//        |> Array.collect (fun bb -> [|
//            bb.MainColumn.Header.SwateColumnHeader
//            if bb.hasUnit then
//                bb.Unit.Value.Header.SwateColumnHeader
//            if bb.hasCompleteTSRTAN then
//                bb.TSR.Value.Header.SwateColumnHeader
//                bb.TAN.Value.Header.SwateColumnHeader
//        |])
//    let rebaseindex =
//        let getCellIndices (cellArr:Cell []) = cellArr |> Array.map (fun c -> c.Index)
//        buildingBlocks
//        |> Array.collect (fun bb -> [|
//            yield! bb.MainColumn.Cells |> getCellIndices
//            if bb.hasUnit then
//                yield! bb.Unit.Value.Cells |> getCellIndices
//            if bb.hasCompleteTSRTAN then
//                yield! bb.TSR.Value.Cells |> getCellIndices
//                yield! bb.TAN.Value.Cells |> getCellIndices
//        |]) |> Array.min
//    let matrixArr =
//        buildingBlocks
//        |> Array.collect (fun bb -> [|
//            yield! bb.MainColumn.toMatrixElement(rebaseindex)
//            if bb.hasUnit then
//                yield! bb.Unit.Value.toMatrixElement(rebaseindex)
//            if bb.hasCompleteTSRTAN then
//                yield! bb.TSR.Value.toMatrixElement(rebaseindex)
//                yield! bb.TAN.Value.toMatrixElement(rebaseindex)
//        |])
//    let matrix = Collections.Generic.Dictionary<(int*string),string>(Map.ofArray matrixArr)
//    matrixHeaders, matrix

//let parseBuildingBlockToAssay (templateName:string) (buildingBlocks:BuildingBlock []) =
//    let matrixHeaders, matrix = parseBuildingBlockToMatrix buildingBlocks
//    //printfn "%A" matrixHeaders // contains "Component [instrument model]"
//    ISADotNet.XLSX.AssayFile.Assay.fromSparseMatrix templateName matrixHeaders matrix

//let parseBuildingBlockSeqsToAssay (worksheetNameBuildingBlocks: (string*BuildingBlock []) []) =
//    let matrices =
//        worksheetNameBuildingBlocks
//        |> Array.map (fun (templateName, buildingBlocks) ->
//            let matrixHeaders, matrix = parseBuildingBlockToMatrix buildingBlocks
//            templateName, Seq.ofArray matrixHeaders, matrix
//        )
//    ISADotNet.XLSX.AssayFile.Assay.fromSparseMatrices matrices
