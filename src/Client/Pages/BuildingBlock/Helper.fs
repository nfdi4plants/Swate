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

let selectHeader (uiState: BuildingBlockUIState) (setUiState: BuildingBlockUIState -> unit) (nextHeader: CompositeHeader) = 
    let nextState, updateBodyCellMsg =
        match nextHeader.IsTermColumn, uiState.BodyCellType with
        | true, BodyCellType.Term | true, BodyCellType.Unitized -> 
            { uiState with DropdownPage = DropdownPage.Main; DropdownIsActive = false },
            Msg.DoNothing
        | true, BodyCellType.Text -> 
            { BodyCellType = BodyCellType.Term; DropdownPage = DropdownPage.Main; DropdownIsActive = false },
            Msg.DoNothing
        | false, _ -> 
            { BodyCellType = BodyCellType.Text; DropdownPage = DropdownPage.Main; DropdownIsActive = false },
            Msg.DoNothing
    setUiState nextState
    Msg.Batch [
        BuildingBlock.Msg.SelectHeader nextHeader |> BuildingBlockMsg
        updateBodyCellMsg
    ]

let selectBody (body: CompositeCell) =
    BuildingBlock.Msg.SelectBodyCell (Some body) |> BuildingBlockMsg

let hasVerifiedTermHeader (header: CompositeHeader) = header.IsTermColumn && header.ToTerm().TermAccessionShort <> ""

let hasVerifiedCell (cell: CompositeCell) = (cell.isTerm || cell.isUnitized) && cell.ToTerm().TermAccessionShort <> ""

let isValidColumn (header : CompositeHeader) =
    header.IsFeaturedColumn 
    || (header.IsTermColumn && header.ToTerm().NameText.Length > 0)
    || header.IsSingleColumn 