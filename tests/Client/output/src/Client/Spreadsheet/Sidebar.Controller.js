import { stringHash, int32ToString } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { contains } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { Model, ActiveView, Model__get_ActiveTable, ActiveView__get_TableIndex, Model__get_Tables } from "../States/Spreadsheet.js";
import { toArray, FSharpSet__get_IsEmpty } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { head } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { ARCtrl_ISA_CompositeCell__CompositeCell_UpdateWithOA_Z4C0FE73C, ARCtrlHelper_ArcFiles__Tables } from "../../Shared/ARCtrl.Helper.js";
import { value } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { ArcTable } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTable.fs.js";
import { CompositeHeader } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeHeader.fs.js";
import { toString } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { renderModal } from "../Modals/Controller.js";
import { warningModalSimple } from "../Modals/WarningModal.js";
import { CompositeColumn } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeColumn.fs.js";

export function SidebarControllerAux_createNewTableName(ind_mut, names_mut) {
    SidebarControllerAux_createNewTableName:
    while (true) {
        const ind = ind_mut, names = names_mut;
        const name = "NewTable" + int32ToString(ind);
        if (contains(name, names, {
            Equals: (x, y) => (x === y),
            GetHashCode: stringHash,
        })) {
            ind_mut = (ind + 1);
            names_mut = names;
            continue SidebarControllerAux_createNewTableName;
        }
        else {
            return name;
        }
        break;
    }
}

/**
 * Uses current `ActiveTableIndex` to return next `ActiveTableIndex` whenever a new table is added and we want to
 * switch to the new table.
 */
export function SidebarControllerAux_getNextActiveTableIndex(state) {
    if (Model__get_Tables(state).TableCount === 0) {
        return 0;
    }
    else {
        return (ActiveView__get_TableIndex(state.ActiveView) + 1) | 0;
    }
}

/**
 * Uses the first selected columnIndex from `state.SelectedCells` to determine if new column should be inserted or appended.
 */
export function SidebarControllerAux_getNextColumnIndex(state) {
    if (!FSharpSet__get_IsEmpty(state.SelectedCells)) {
        return head(toArray(state.SelectedCells))[0] | 0;
    }
    else {
        return Model__get_ActiveTable(state).ColumnCount | 0;
    }
}

/**
 * Make sure only one column is selected
 */
export function SanityChecks_verifyOnlyOneColumnSelected(selectedCells) {
    let columnIndex;
    if (!((columnIndex = (selectedCells[0][0] | 0), selectedCells.every((x) => (x[0] === columnIndex))))) {
        throw new Error("Can only paste term in one column at a time!");
    }
}

/**
 * This is the basic function to create new Tables from an array of SwateBuildingBlocks
 */
export function addTable(newTable, state) {
    const tables = Model__get_Tables(state);
    const newIndex = SidebarControllerAux_getNextActiveTableIndex(state) | 0;
    tables.AddTable(newTable, newIndex);
    return new Model(new ActiveView(0, [newIndex]), state.SelectedCells, state.ArcFile, state.Clipboard);
}

/**
 * This function is used to create multiple tables at once.
 */
export function addTables(tables, state) {
    const newIndex = SidebarControllerAux_getNextActiveTableIndex(state) | 0;
    Model__get_Tables(state).AddTables(tables, newIndex);
    return new Model(new ActiveView(0, [newIndex + tables.length]), state.SelectedCells, state.ArcFile, state.Clipboard);
}

/**
 * Adds the most basic empty Swate table with auto generated name.
 */
export function createTable(usePrevOutput, state) {
    const tables = ARCtrlHelper_ArcFiles__Tables(value(state.ArcFile));
    const newName = SidebarControllerAux_createNewTableName(0, tables.TableNames);
    const newTable = ArcTable.init(newName);
    if (usePrevOutput && ((tables.TableCount - 1) >= ActiveView__get_TableIndex(state.ActiveView))) {
        const table = tables.GetTableAt(ActiveView__get_TableIndex(state.ActiveView));
        const output = table.GetOutputColumn();
        const newInput = new CompositeHeader(11, [value(output.Header.TryOutput())]);
        newTable.AddColumn(newInput, output.Cells, void 0, true);
    }
    return addTable(newTable, new Model(state.ActiveView, state.SelectedCells, state.ArcFile, state.Clipboard));
}

export function addBuildingBlock(newColumn, state) {
    let Cells, Cells_1;
    const table = Model__get_ActiveTable(state);
    let nextIndex = SidebarControllerAux_getNextColumnIndex(state);
    let newColumn_1 = newColumn;
    if (newColumn_1.Header.isOutput) {
        const hasOutput = table.TryGetOutputColumn();
        if (hasOutput != null) {
            const msg = `Found existing output column. Changed output column to "${toString(newColumn_1.Header)}".`;
            renderModal("ColumnReplaced", (rmv) => warningModalSimple(msg, rmv));
            newColumn_1 = ((Cells = value(hasOutput).Cells, new CompositeColumn(newColumn_1.Header, Cells)));
        }
    }
    if (newColumn_1.Header.isInput) {
        const hasInput = table.TryGetInputColumn();
        if (hasInput != null) {
            const msg_1 = `Found existing input column. Changed input column to "${toString(newColumn_1.Header)}".`;
            renderModal("ColumnReplaced", (rmv_1) => warningModalSimple(msg_1, rmv_1));
            newColumn_1 = ((Cells_1 = value(hasInput).Cells, new CompositeColumn(newColumn_1.Header, Cells_1)));
        }
    }
    table.AddColumn(newColumn_1.Header, newColumn_1.Cells, nextIndex, true);
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, state.Clipboard);
}

export function addBuildingBlocks(newColumns, state) {
    const table = Model__get_ActiveTable(state);
    let newColumns_1 = newColumns;
    let nextIndex = SidebarControllerAux_getNextColumnIndex(state);
    table.AddColumns(newColumns_1, nextIndex);
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, state.Clipboard);
}

export function insertTerm_IntoSelected(term, state) {
    const table = Model__get_ActiveTable(state);
    const selected = toArray(state.SelectedCells);
    SanityChecks_verifyOnlyOneColumnSelected(selected);
    const column = table.GetColumn(selected[0][0]);
    for (let idx = 0; idx <= (selected.length - 1); idx++) {
        const forLoopVar = selected[idx];
        const rowIndex = forLoopVar[1] | 0;
        const colIndex = forLoopVar[0] | 0;
        const c = table.TryGetCellAt(colIndex, rowIndex);
        const newCell = (c == null) ? ARCtrl_ISA_CompositeCell__CompositeCell_UpdateWithOA_Z4C0FE73C(column.GetDefaultEmptyCell(), term) : c;
        table.UpdateCellAt(colIndex, rowIndex, newCell);
    }
    return new Model(state.ActiveView, state.SelectedCells, state.ArcFile, state.Clipboard);
}

//# sourceMappingURL=Sidebar.Controller.js.map
