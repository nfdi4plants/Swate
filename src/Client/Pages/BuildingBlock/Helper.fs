module BuildingBlock.Helper

open Shared
open OfficeInteropTypes
open Model
open Messages
open ARCtrl.ISA
open Model.BuildingBlock

let createCellFromUiStateAndOA (uiState: BuildingBlockUIState) (oa:OntologyAnnotation) =
    match uiState.BodyCellType with
    | BodyCellType.Text -> CompositeCell.createFreeText oa.NameText
    | BodyCellType.Term -> CompositeCell.createTerm oa
    | BodyCellType.Unitized -> CompositeCell.createUnitized("", oa)

let selectHeader (uiState: BuildingBlockUIState) (setUiState: BuildingBlockUIState -> unit) (header: CompositeHeader) = 
    let bodyType =
        match header.IsTermColumn, uiState.BodyCellType with
        | true, BodyCellType.Term | true, BodyCellType.Unitized -> { uiState with DropdownPage = DropdownPage.Main; DropdownIsActive = false }
        | true, BodyCellType.Text -> { BodyCellType = BodyCellType.Term; DropdownPage = DropdownPage.Main; DropdownIsActive = false }
        | false, _ -> { BodyCellType = BodyCellType.Text; DropdownPage = DropdownPage.Main; DropdownIsActive = false }
    setUiState bodyType
    BuildingBlock.Msg.SelectHeader header |> BuildingBlockMsg

let hasVerifiedTermHeader (header: CompositeHeader) = header.IsTermColumn && header.GetOA().TermAccessionShort <> ""

let hasVerifiedCell (cell: CompositeCell) = (cell.isTerm || cell.isUnitized) && cell.GetOA().TermAccessionShort <> ""

let isValidColumn (header : CompositeHeader) =
    header.IsFeaturedColumn 
    || (header.IsTermColumn && header.GetOA().NameText.Length > 0)
    || header.IsSingleColumn 