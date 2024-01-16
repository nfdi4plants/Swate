import { Model__get_ActiveTable, Model, TableClipboard } from "../States/Spreadsheet.js";
import { min } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { FSharpSet__get_IsEmpty, toArray } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { compareArrays } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { value } from "../../../fable_modules/fable-library.4.9.0/Option.js";

export function ClipboardAux_setClipboardCell(state, cell) {
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, new TableClipboard(cell));
}

export function copyCell(index_, index__1, state) {
    let objectArg, tupledArg;
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, new TableClipboard((objectArg = Model__get_ActiveTable(state), (tupledArg = [index_, index__1], objectArg.TryGetCellAt(tupledArg[0], tupledArg[1])))));
}

export function copySelectedCell(state) {
    const index = min(toArray(state.SelectedCells), {
        Compare: compareArrays,
    });
    return copyCell(index[0], index[1], state);
}

export function cutCell(index_, index__1, state) {
    const index = [index_, index__1];
    let cell;
    const objectArg = Model__get_ActiveTable(state);
    const tupledArg = index;
    cell = objectArg.TryGetCellAt(tupledArg[0], tupledArg[1]);
    const emptyCell = (cell != null) ? value(cell).GetEmptyCell() : Model__get_ActiveTable(state).GetColumn(index[0]).GetDefaultEmptyCell();
    Model__get_ActiveTable(state).UpdateCellAt(index[0], index[1], emptyCell);
    return ClipboardAux_setClipboardCell(state, cell);
}

export function cutSelectedCell(state) {
    const index = min(toArray(state.SelectedCells), {
        Compare: compareArrays,
    });
    return cutCell(index[0], index[1], state);
}

export function pasteCell(index_, index__1, state) {
    const index = [index_, index__1];
    const matchValue = state.Clipboard.Cell;
    if (matchValue != null) {
        const c = matchValue;
        Model__get_ActiveTable(state).UpdateCellAt(index[0], index[1], c);
        return state;
    }
    else {
        return state;
    }
}

export function pasteSelectedCell(state) {
    if (FSharpSet__get_IsEmpty(state.SelectedCells)) {
        return state;
    }
    else {
        const minIndex = min(toArray(state.SelectedCells), {
            Compare: compareArrays,
        });
        return pasteCell(minIndex[0], minIndex[1], state);
    }
}

//# sourceMappingURL=Clipboard.Controller.js.map
