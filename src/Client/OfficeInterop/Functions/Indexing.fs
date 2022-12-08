module OfficeInterop.Indexing

open Shared.OfficeInteropTypes

/// This is based on a excel hack on how to add multiple header of the same name to an excel table.,
/// by just appending more whitespace to the name.
let extendName (existingNames: string []) (baseName:string) =
    let rec loop (baseName:string) =
        if existingNames |> Array.contains baseName then
            loop (baseName + " ") 
        else
            baseName
    loop baseName

let createTSR (newBB:InsertBuildingBlock) =
    let termAccession = if newBB.ColumnTerm.IsSome then newBB.ColumnTerm.Value.TermAccession else ""
    $"{ColumnCoreNames.TermSourceRef.toString} ({termAccession})"

let createTAN (newBB:InsertBuildingBlock) =
    let termAccession = if newBB.ColumnTerm.IsSome then newBB.ColumnTerm.Value.TermAccession else ""
    $"{ColumnCoreNames.TermAccessionNumber.toString} ({termAccession})"  

let createUnit() =
    $"{ColumnCoreNames.Unit.toString}"

let createColumnNames (newBB:InsertBuildingBlock) (existingNames: string []) =
    let mainColumn = newBB.ColumnHeader.toAnnotationTableHeader()
    [|
        mainColumn
        if newBB.UnitTerm.IsSome then
            createUnit()
        if not newBB.ColumnHeader.Type.isSingleColumn then
            createTSR newBB
            createTAN newBB
    |]
    |> Array.map (extendName existingNames)