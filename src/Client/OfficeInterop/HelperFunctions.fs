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

let createEmptyMatrixForTables (colCount:int) (rowCount:int) value =
    [|
        for i in 0 .. rowCount-1 do
            yield   [|
                for i in 0 .. colCount-1 do yield U3<bool,string,float>.Case2 value
            |] :> IList<U3<bool,string,float>>
    |] :> IList<IList<U3<bool,string,float>>>

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

