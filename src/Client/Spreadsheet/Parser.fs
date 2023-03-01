module Spreadsheet.Parser

open Shared
open OfficeInteropTypes
open Spreadsheet

type InsertBuildingBlock with
    member this.toSwateBuildingBlock(index:int) : SwateBuildingBlock =
        let header = HeaderCell.create(this.ColumnHeader.Type, ?term = this.ColumnTerm, hasUnit = this.HasUnit)
        let rows =
            match this.HasValues, header.isTermColumn, this.HasUnit with
            | true, true, true    -> this.Rows |> Array.mapi (fun i t -> i + 1, SwateCell.create(t.Name, ?unit = this.UnitTerm))
            | true, true, false   -> this.Rows |> Array.mapi (fun i t -> i + 1, SwateCell.create(t))
            | true, false, _      -> this.Rows |> Array.mapi (fun i t -> i + 1, SwateCell.create(t.Name))
            | false, _, _         -> [||]
        SwateBuildingBlock.create(index, header, rows)

module SwateBuildingBlock =
    
    ///<summary> Parse column of index `index` from ActiveTableMap `m` to SwateBuildingBlock. </summary>
    let ofTableMap_byIndex (index: int) (m: Map<int*int,SwateCell>) =
        let column = Map.filter (fun k _ -> fst k = index ) m
        let header = column.[index, 0].Header
        let rows = [|
            for KeyValue ((_,rk),c) in column do
                if rk <> 0 then
                    yield rk, c
        |]
        SwateBuildingBlock.create(index, header, rows)

    let ofTableMap_list (m: Map<int*int,SwateCell>) : SwateBuildingBlock list =
        let maxColIndex = m.Keys |> Seq.maxBy fst |> fst
        [
            for i in 0 .. maxColIndex do
                yield ofTableMap_byIndex i m
        ]

    let ofTableMap (m: Map<int*int,SwateCell>) : SwateBuildingBlock [] =
        let maxColIndex = m.Keys |> Seq.maxBy fst |> fst
        [|
            for i in 0 .. maxColIndex do
                yield ofTableMap_byIndex i m
        |]

    let toTableMap (buildingBlocks: seq<SwateBuildingBlock>) : Map<int*int,SwateCell> =
        buildingBlocks
        |> Seq.collect (fun bb ->
            let columnIndex = bb.Index
            let header = (columnIndex, 0), IsHeader bb.Header
            let rows =
                bb.Rows |> Array.map (fun (i,c) ->
                    (columnIndex, i), c
                )
            [|
                yield header
                yield! rows
            |]
        )
        |> Map.ofSeq
        

