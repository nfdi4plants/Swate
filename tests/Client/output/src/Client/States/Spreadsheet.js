import { Union, Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { CompositeCell, CompositeCell_$reflection } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeCell.fs.js";
import { bool_type, uint8_type, array_type, string_type, class_type, tuple_type, union_type, int32_type, record_type, option_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { ARCtrlHelper_ArcFiles__Tables, ARCtrlHelper_ArcFiles_$reflection } from "../../Shared/ARCtrl.Helper.js";
import { toList, FSharpSet__get_IsEmpty, empty } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { comparePrimitives, compareArrays } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { ArcTables } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTables.fs.js";
import { ArcTable } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTable.fs.js";
import { CompositeHeader_$reflection, CompositeHeader } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeHeader.fs.js";
import { minBy } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { ArcAssay } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTypes.fs.js";
import { exists } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { CompositeColumn_$reflection } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeColumn.fs.js";
import { OntologyAnnotation_$reflection } from "../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologyAnnotation.fs.js";
import { TermTypes_TermSearchable_$reflection } from "../../Shared/TermTypes.js";

export class TableClipboard extends Record {
    constructor(Cell) {
        super();
        this.Cell = Cell;
    }
}

export function TableClipboard_$reflection() {
    return record_type("Spreadsheet.TableClipboard", [], TableClipboard, () => [["Cell", option_type(CompositeCell_$reflection())]]);
}

export function TableClipboard_init() {
    return new TableClipboard(void 0);
}

export class ActiveView extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Table", "Metadata"];
    }
}

export function ActiveView_$reflection() {
    return union_type("Spreadsheet.ActiveView", [], ActiveView, () => [[["index", int32_type]], []]);
}

/**
 * Returns -1 if no table index given
 */
export function ActiveView__get_TableIndex(this$) {
    if (this$.tag === 0) {
        return this$.fields[0] | 0;
    }
    else {
        return 0;
    }
}

export class Model extends Record {
    constructor(ActiveView, SelectedCells, ArcFile, Clipboard) {
        super();
        this.ActiveView = ActiveView;
        this.SelectedCells = SelectedCells;
        this.ArcFile = ArcFile;
        this.Clipboard = Clipboard;
    }
}

export function Model_$reflection() {
    return record_type("Spreadsheet.Model", [], Model, () => [["ActiveView", ActiveView_$reflection()], ["SelectedCells", class_type("Microsoft.FSharp.Collections.FSharpSet`1", [tuple_type(int32_type, int32_type)])], ["ArcFile", option_type(ARCtrlHelper_ArcFiles_$reflection())], ["Clipboard", TableClipboard_$reflection()]]);
}

export function Model_init() {
    return new Model(new ActiveView(1, []), empty({
        Compare: compareArrays,
    }), void 0, TableClipboard_init());
}

export function Model__get_Tables(this$) {
    const matchValue = this$.ArcFile;
    if (matchValue == null) {
        return new ArcTables([]);
    }
    else {
        return ARCtrlHelper_ArcFiles__Tables(matchValue);
    }
}

export function Model__get_ActiveTable(this$) {
    const matchValue = this$.ActiveView;
    if (matchValue.tag === 1) {
        const t = ArcTable.init("NULL_TABLE");
        t.AddColumn(new CompositeHeader(13, ["WARNING"]), [new CompositeCell(1, ["If you see this table view, pls contact a developer and report it."])]);
        return t;
    }
    else {
        return Model__get_Tables(this$).GetTableAt(matchValue.fields[0]);
    }
}

export function Model__get_getSelectedColumnHeader(this$) {
    if (FSharpSet__get_IsEmpty(this$.SelectedCells)) {
        return void 0;
    }
    else {
        const columnIndex = minBy((tuple) => tuple[0], toList(this$.SelectedCells), {
            Compare: comparePrimitives,
        })[0] | 0;
        return Model__get_ActiveTable(this$).GetColumn(columnIndex).Header;
    }
}

export function Model__GetAssay(this$) {
    const matchValue = this$.ArcFile;
    let matchResult, a;
    if (matchValue != null) {
        if (matchValue.tag === 3) {
            matchResult = 0;
            a = matchValue.fields[0];
        }
        else {
            matchResult = 1;
        }
    }
    else {
        matchResult = 1;
    }
    switch (matchResult) {
        case 0:
            return a;
        default:
            return ArcAssay.init("ASSAY_NULL");
    }
}

export function Model__get_headerIsSelected(this$) {
    if (!FSharpSet__get_IsEmpty(this$.SelectedCells)) {
        return exists((tupledArg) => (tupledArg[1] === 0), this$.SelectedCells);
    }
    else {
        return false;
    }
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["UpdateCell", "UpdateHeader", "UpdateActiveView", "UpdateSelectedCells", "RemoveTable", "RenameTable", "UpdateTableOrder", "UpdateHistoryPosition", "AddRows", "DeleteRow", "DeleteRows", "DeleteColumn", "CopySelectedCell", "CutSelectedCell", "PasteSelectedCell", "CopyCell", "CutCell", "PasteCell", "FillColumnWithTerm", "Reset", "SetArcFileFromBytes", "CreateAnnotationTable", "AddAnnotationBlock", "AddAnnotationBlocks", "UpdateArcFile", "InitFromArcFile", "InsertOntologyTerm", "InsertOntologyTerms", "UpdateTermColumns", "UpdateTermColumnsResponse", "ExportJsonTable", "ExportJsonTables", "ExportXlsx", "ExportXlsxDownload", "ParseTablesToDag"];
    }
}

export function Msg_$reflection() {
    return union_type("Spreadsheet.Msg", [], Msg, () => [[["Item1", tuple_type(int32_type, int32_type)], ["Item2", CompositeCell_$reflection()]], [["columIndex", int32_type], ["Item2", CompositeHeader_$reflection()]], [["Item", ActiveView_$reflection()]], [["Item", class_type("Microsoft.FSharp.Collections.FSharpSet`1", [tuple_type(int32_type, int32_type)])]], [["index", int32_type]], [["index", int32_type], ["name", string_type]], [["pre_index", int32_type], ["new_index", int32_type]], [["newPosition", int32_type]], [["Item", int32_type]], [["Item", int32_type]], [["Item", array_type(int32_type)]], [["Item", int32_type]], [], [], [], [["index", tuple_type(int32_type, int32_type)]], [["index", tuple_type(int32_type, int32_type)]], [["index", tuple_type(int32_type, int32_type)]], [["index", tuple_type(int32_type, int32_type)]], [], [["Item", array_type(uint8_type)]], [["tryUsePrevOutput", bool_type]], [["Item", CompositeColumn_$reflection()]], [["Item", array_type(CompositeColumn_$reflection())]], [["Item", ARCtrlHelper_ArcFiles_$reflection()]], [["Item", ARCtrlHelper_ArcFiles_$reflection()]], [["Item", OntologyAnnotation_$reflection()]], [["Item", array_type(OntologyAnnotation_$reflection())]], [], [["Item", array_type(TermTypes_TermSearchable_$reflection())]], [], [], [["Item", ARCtrlHelper_ArcFiles_$reflection()]], [["filename", string_type], ["Item2", array_type(uint8_type)]], []]);
}

//# sourceMappingURL=Spreadsheet.js.map
