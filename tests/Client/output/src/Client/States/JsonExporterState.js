import { Union, Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { JsonExportType, JsonExportType_$reflection } from "../../Shared/Shared.js";
import { union_type, tuple_type, string_type, record_type, array_type, uint8_type, bool_type, option_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { BuildingBlock_$reflection } from "../../Shared/OfficeInteropTypes.js";

export class Model extends Record {
    constructor(CurrentExportType, TableJsonExportType, WorkbookJsonExportType, XLSXParsingExportType, Loading, ShowTableExportTypeDropdown, ShowWorkbookExportTypeDropdown, ShowXLSXExportTypeDropdown, XLSXByteArray) {
        super();
        this.CurrentExportType = CurrentExportType;
        this.TableJsonExportType = TableJsonExportType;
        this.WorkbookJsonExportType = WorkbookJsonExportType;
        this.XLSXParsingExportType = XLSXParsingExportType;
        this.Loading = Loading;
        this.ShowTableExportTypeDropdown = ShowTableExportTypeDropdown;
        this.ShowWorkbookExportTypeDropdown = ShowWorkbookExportTypeDropdown;
        this.ShowXLSXExportTypeDropdown = ShowXLSXExportTypeDropdown;
        this.XLSXByteArray = XLSXByteArray;
    }
}

export function Model_$reflection() {
    return record_type("Model.JsonExporter.Model", [], Model, () => [["CurrentExportType", option_type(JsonExportType_$reflection())], ["TableJsonExportType", JsonExportType_$reflection()], ["WorkbookJsonExportType", JsonExportType_$reflection()], ["XLSXParsingExportType", JsonExportType_$reflection()], ["Loading", bool_type], ["ShowTableExportTypeDropdown", bool_type], ["ShowWorkbookExportTypeDropdown", bool_type], ["ShowXLSXExportTypeDropdown", bool_type], ["XLSXByteArray", array_type(uint8_type)]]);
}

export function Model_init() {
    return new Model(void 0, new JsonExportType(1, []), new JsonExportType(1, []), new JsonExportType(1, []), false, false, false, false, new Uint8Array(0));
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["UpdateLoading", "UpdateShowTableExportTypeDropdown", "UpdateShowWorkbookExportTypeDropdown", "UpdateShowXLSXExportTypeDropdown", "CloseAllDropdowns", "UpdateTableJsonExportType", "UpdateWorkbookJsonExportType", "UpdateXLSXParsingExportType", "ParseTableOfficeInteropRequest", "ParseTableServerRequest", "ParseTableServerResponse", "ParseTablesOfficeInteropRequest", "ParseTablesServerRequest", "StoreXLSXByteArray", "ParseXLSXToJsonRequest", "ParseXLSXToJsonResponse"];
    }
}

export function Msg_$reflection() {
    return union_type("Model.JsonExporter.Msg", [], Msg, () => [[["Item", bool_type]], [["Item", bool_type]], [["Item", bool_type]], [["Item", bool_type]], [], [["Item", JsonExportType_$reflection()]], [["Item", JsonExportType_$reflection()]], [["Item", JsonExportType_$reflection()]], [], [["worksheetName", string_type], ["Item2", array_type(BuildingBlock_$reflection())]], [["Item", string_type]], [], [["Item", array_type(tuple_type(string_type, array_type(BuildingBlock_$reflection())))]], [["Item", array_type(uint8_type)]], [["Item", array_type(uint8_type)]], [["Item", string_type]]]);
}

//# sourceMappingURL=JsonExporterState.js.map
