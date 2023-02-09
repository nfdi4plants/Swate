module Spreadsheet.Parser

open Shared
open OfficeInteropTypes
open Spreadsheet

type InsertBuildingBlock with
    member this.toSwateBuildingBlock(index:int) : SwateBuildingBlock =
        let header =
            let str = this.ColumnHeader.toAnnotationTableHeader()
            SwateColumnHeader.init(str, ?term = this.ColumnTerm)
        let unit = this.UnitTerm
        let rows =
            if this.HasValues then
                this.Rows |> Array.mapi (fun i t -> i + 1, BodyCell.create(t, ?unit = unit))
            else
                [||]
        SwateBuildingBlock.create(index, header, rows)

module SwateBuildingBlock =
    
    ///<summary> Parse column of index `index` from ActiveTableMap `m` to SwateBuildingBlock. </summary>
    let ofTableMap_byIndex (index: int) (m: Map<int*int,SwateCell>) =
        let column = Map.filter (fun k _ -> fst k = index ) m
        let header = column.[index, 0].Header
        let rows = [|
            for KeyValue ((_,rk),c) in column do
                if rk <> 0 then
                    yield
                        rk, c.Body
        |]
        SwateBuildingBlock.create(index, header, rows)

    let ofTableMap (m: Map<int*int,SwateCell>) : SwateBuildingBlock [] =
        let maxColIndex = m.Keys |> Seq.maxBy fst |> fst
        [|
            for i in 0 .. maxColIndex do
                yield ofTableMap_byIndex i m
        |]

    let toTableMap (buildingBlocks: SwateBuildingBlock []) : Map<int*int,SwateCell> =
        buildingBlocks
        |> Array.collect (fun bb ->
            let columnIndex = bb.Index
            let header = (columnIndex, 0), IsHeader bb.Header
            let rows =
                bb.Rows |> Array.map (fun (i,c) ->
                    (columnIndex, i), IsBody c
                )
            [|
                yield header
                yield! rows
            |]
        )
        |> Map.ofArray
        

