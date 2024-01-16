import { CompositeCell } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeCell.fs.js";
import { BuildingBlock_BodyCellType, BuildingBlock_BuildingBlockUIState, BuildingBlock_DropdownPage } from "../../Model.js";
import { BuildingBlock_Msg, Msg } from "../../Messages.js";
import { ARCtrl_ISA_CompositeCell__CompositeCell_ToTerm } from "../../../Shared/ARCtrl.Helper.js";

export function createCellFromUiStateAndOA(uiState, oa) {
    const matchValue = uiState.BodyCellType;
    switch (matchValue.tag) {
        case 0:
            return CompositeCell.createTerm(oa);
        case 1:
            return CompositeCell.createUnitized("", oa);
        default:
            return CompositeCell.createFreeText(oa.NameText);
    }
}

export function selectHeader(uiState, setUiState, nextHeader) {
    let patternInput;
    const matchValue = nextHeader.IsTermColumn;
    const matchValue_1 = uiState.BodyCellType;
    let matchResult;
    if (matchValue) {
        switch (matchValue_1.tag) {
            case 2: {
                matchResult = 1;
                break;
            }
            default:
                matchResult = 0;
        }
    }
    else {
        matchResult = 2;
    }
    switch (matchResult) {
        case 0: {
            patternInput = [new BuildingBlock_BuildingBlockUIState(false, new BuildingBlock_DropdownPage(0, []), uiState.BodyCellType), new Msg(25, [])];
            break;
        }
        case 1: {
            patternInput = [new BuildingBlock_BuildingBlockUIState(false, new BuildingBlock_DropdownPage(0, []), new BuildingBlock_BodyCellType(0, [])), new Msg(9, [new BuildingBlock_Msg(9, [CompositeCell.emptyTerm])])];
            break;
        }
        default:
            patternInput = [new BuildingBlock_BuildingBlockUIState(false, new BuildingBlock_DropdownPage(0, []), new BuildingBlock_BodyCellType(2, [])), new Msg(9, [new BuildingBlock_Msg(9, [CompositeCell.emptyFreeText])])];
    }
    setUiState(patternInput[0]);
    return new Msg(21, [[new Msg(9, [new BuildingBlock_Msg(3, [nextHeader])]), patternInput[1]]]);
}

export function selectBody(body) {
    return new Msg(9, [new BuildingBlock_Msg(9, [body])]);
}

export function hasVerifiedTermHeader(header) {
    if (header.IsTermColumn) {
        return header.ToTerm().TermAccessionShort !== "";
    }
    else {
        return false;
    }
}

export function hasVerifiedCell(cell) {
    if (cell.isTerm ? true : cell.isUnitized) {
        return ARCtrl_ISA_CompositeCell__CompositeCell_ToTerm(cell).TermAccessionShort !== "";
    }
    else {
        return false;
    }
}

export function isValidColumn(header) {
    if (header.IsFeaturedColumn ? true : (header.IsTermColumn && (header.ToTerm().NameText.length > 0))) {
        return true;
    }
    else {
        return header.IsSingleColumn;
    }
}

//# sourceMappingURL=Helper.js.map
