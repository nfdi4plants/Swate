module BuildingBlock.Helper

open Shared
open OfficeInteropTypes
open Model
open Messages
open ARCtrl
open Model.BuildingBlock

let isSameMajorHeaderCellType (hct1: BuildingBlock.HeaderCellType) (hct2: BuildingBlock.HeaderCellType) =
    (hct1.IsTermColumn() = hct2.IsTermColumn())
    && (hct1.HasIOType() = hct2.HasIOType())
    
let selectHeaderCellType (hct: BuildingBlock.HeaderCellType) setUiState dispatch =
    BuildingBlock.UpdateHeaderCellType hct |> BuildingBlockMsg |> dispatch
    { DropdownPage = DropdownPage.Main; DropdownIsActive = false }|> setUiState
    
open Fable.Core

let createCompositeHeaderFromState (state: BuildingBlock.Model) =
    let getOA() = state.TryHeaderOA() |> Option.defaultValue OntologyAnnotation.empty
    let getIOType() = state.TryHeaderIO() |> Option.defaultValue (IOType.FreeText "")
    match state.HeaderCellType with
    | HeaderCellType.Component -> CompositeHeader.Component <| getOA()
    | HeaderCellType.Characteristic -> CompositeHeader.Characteristic <| getOA()
    | HeaderCellType.Factor -> CompositeHeader.Factor <| getOA()
    | HeaderCellType.Parameter -> CompositeHeader.Parameter <| getOA()
    | HeaderCellType.ProtocolType -> CompositeHeader.ProtocolType
    | HeaderCellType.ProtocolDescription -> CompositeHeader.ProtocolDescription
    | HeaderCellType.ProtocolUri -> CompositeHeader.ProtocolUri
    | HeaderCellType.ProtocolVersion -> CompositeHeader.ProtocolVersion
    | HeaderCellType.ProtocolREF -> CompositeHeader.ProtocolREF
    | HeaderCellType.Performer -> CompositeHeader.Performer
    | HeaderCellType.Date -> CompositeHeader.Date
    | HeaderCellType.Input -> CompositeHeader.Input <| getIOType()
    | HeaderCellType.Output -> CompositeHeader.Output <| getIOType()
   
let tryCreateCompositeCellFromState (state: BuildingBlock.Model) =
    match state.BodyCellType, state.BodyArg with 
    | BodyCellType.Term, Some (U2.Case2 oa) -> CompositeCell.createTerm (oa) |> Some
    | BodyCellType.Unitized, Some (U2.Case2 oa) -> CompositeCell.createUnitized ("", oa) |> Some
    | BodyCellType.Text, Some (U2.Case1 s) -> CompositeCell.createFreeText s |> Some
    | _ -> None

let isValidColumn (header : CompositeHeader) =
    header.IsFeaturedColumn 
    || (header.IsTermColumn && header.ToTerm().NameText.Length > 0)
    || header.IsSingleColumn