import { defaultArg, map } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { tryFind, toList, tryFindBack } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { rangeDouble } from "../../../fable_modules/fable-library.4.9.0/Range.js";
import { Model__get_ActiveTable, ActiveView, Model_init as Model_init_1, Model, Model__get_Tables } from "../States/Spreadsheet.js";
import { Model_init, Model__ResetAll } from "../States/LocalHistory.js";
import { empty } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { compareArrays } from "../../../fable_modules/fable-library.4.9.0/Util.js";

export function ControllerTableAux_findEarlierTable(tableIndex, tables) {
    return map((i) => [i, tables.GetTableAt(i)], tryFindBack((k) => (k < tableIndex), toList(rangeDouble(0, 1, tables.TableCount - 1))));
}

export function ControllerTableAux_findLaterTable(tableIndex, tables) {
    return map((i) => [i, tables.GetTableAt(i)], tryFind((k) => (k > tableIndex), toList(rangeDouble(0, 1, tables.TableCount - 1))));
}

export function ControllerTableAux_findNeighborTables(tableIndex, tables) {
    return [ControllerTableAux_findEarlierTable(tableIndex, tables), ControllerTableAux_findLaterTable(tableIndex, tables)];
}

export function updateTableOrder(prevIndex, newIndex, state) {
    Model__get_Tables(state).MoveTable(prevIndex, newIndex);
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, state.Clipboard);
}

export function resetTableState() {
    return [Model__ResetAll(Model_init()), Model_init_1()];
}

export function renameTable(tableIndex, newName, state) {
    Model__get_Tables(state).RenameTableAt(tableIndex, newName);
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, state.Clipboard);
}

export function removeTable(removeIndex, state) {
    let neighbors;
    if (Model__get_Tables(state).TableCount === 0) {
        return state;
    }
    else {
        Model__get_Tables(state).RemoveTableAt(removeIndex);
        if (Model__get_Tables(state).TableCount === 0) {
            return Model_init_1();
        }
        else {
            const matchValue = state.ActiveView;
            if (matchValue.tag === 0) {
                if (matchValue.fields[0] === removeIndex) {
                    return new Model(new ActiveView(0, [(neighbors = ControllerTableAux_findNeighborTables(removeIndex, Model__get_Tables(state)), (neighbors[0] == null) ? ((neighbors[1] != null) ? neighbors[1][0] : 0) : neighbors[0][0])]), state.SelectedCells, state.ArcFile, state.Clipboard);
                }
                else {
                    return new Model(new ActiveView(0, [(matchValue.fields[0] > removeIndex) ? (matchValue.fields[0] - 1) : matchValue.fields[0]]), state.SelectedCells, state.ArcFile, state.Clipboard);
                }
            }
            else {
                return state;
            }
        }
    }
}

/**
 * Add `n` rows to active table.
 */
export function addRows(n, state) {
    Model__get_ActiveTable(state).AddRowsEmpty(n);
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, state.Clipboard);
}

export function deleteRow(index, state) {
    Model__get_ActiveTable(state).RemoveRow(index);
    return new Model(state.ActiveView, empty({
        Compare: compareArrays,
    }), state.ArcFile, state.Clipboard);
}

export function deleteRows(indexArr, state) {
    Model__get_ActiveTable(state).RemoveRows(indexArr);
    return new Model(state.ActiveView, empty({
        Compare: compareArrays,
    }), state.ArcFile, state.Clipboard);
}

export function deleteColumn(index, state) {
    Model__get_ActiveTable(state).RemoveColumn(index);
    return new Model(state.ActiveView, empty({
        Compare: compareArrays,
    }), state.ArcFile, state.Clipboard);
}

export function fillColumnWithCell(index_, index__1, state) {
    const index = [index_, index__1];
    let cell;
    const objectArg = Model__get_ActiveTable(state);
    const tupledArg = index;
    cell = objectArg.TryGetCellAt(tupledArg[0], tupledArg[1]);
    const columnIndex = index[0] | 0;
    Model__get_ActiveTable(state).IteriColumns((i, column_1) => {
        const cell_1 = defaultArg(cell, column_1.GetDefaultEmptyCell());
        if (i === columnIndex) {
            for (let cellRowIndex = 0; cellRowIndex <= (column_1.Cells.length - 1); cellRowIndex++) {
                Model__get_ActiveTable(state).UpdateCellAt(columnIndex, cellRowIndex, cell_1);
            }
        }
    });
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, state.Clipboard);
}

//# sourceMappingURL=Table.Controller.js.map
