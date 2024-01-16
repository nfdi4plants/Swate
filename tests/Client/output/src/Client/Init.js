import { DataTheme_GET, DataTheme_SET_EA9902F } from "./LocalStorage/Darkmode.js";
import { create } from "../../fable_modules/Thoth.Elmish.Debouncer.2.0.0/Debouncer.fs.js";
import { SettingsXml_Model_init, BuildingBlockDetailsState_init, Protocol_Model_init, Validation_Model_init, BuildingBlock_Model_init, FilePicker_Model_init, ApiState_init, AdvancedSearch_Model_init, TermSearch_Model_init, DevState_init, PersistentStorageState_init, PageState_init } from "./Model.js";
import { Model_init } from "./OfficeInterop/OfficeInteropState.js";
import { Model_init as Model_init_1 } from "./States/JsonExporterState.js";
import { Model_init as Model_init_2 } from "./States/TemplateMetadataState.js";
import { Model_init as Model_init_3 } from "./States/DagState.js";
import { Model_init_6DFDD678 } from "./States/CytoscapeState.js";
import { Model_init as Model_init_4, Model__UpdateFromSessionStorage, Spreadsheet_Model__Model_fromLocalStorage_Static } from "./States/LocalHistory.js";
import { Msg as Msg_1, Model } from "./Messages.js";
import { urlUpdate } from "./Update.js";
import { Cmd_batch } from "../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { value } from "../../fable_modules/fable-library.4.9.0/Option.js";
import { Msg } from "./States/SpreadsheetInterface.js";
import { singleton } from "../../fable_modules/fable-library.4.9.0/List.js";

export function initializeModel() {
    DataTheme_SET_EA9902F(DataTheme_GET());
    const DebouncerState = create();
    const PageState = PageState_init();
    const PersistentStorageState = PersistentStorageState_init();
    const DevState = DevState_init();
    const TermSearchState = TermSearch_Model_init();
    const AdvancedSearchState = AdvancedSearch_Model_init();
    const ExcelState = Model_init();
    const ApiState = ApiState_init();
    const FilePickerState = FilePicker_Model_init();
    const AddBuildingBlockState = BuildingBlock_Model_init();
    const ValidationState = Validation_Model_init();
    return new Model(PageState, PersistentStorageState, DebouncerState, DevState, TermSearchState, AdvancedSearchState, ExcelState, ApiState, FilePickerState, Protocol_Model_init(), AddBuildingBlockState, ValidationState, BuildingBlockDetailsState_init(), SettingsXml_Model_init(), Model_init_1(), Model_init_2(), Model_init_3(), Model_init_6DFDD678(), Spreadsheet_Model__Model_fromLocalStorage_Static(), Model__UpdateFromSessionStorage(Model_init_4()));
}

export function init(pageOpt) {
    let msg;
    const patternInput = urlUpdate(pageOpt, initializeModel());
    const initialModel = patternInput[0];
    return [initialModel, Cmd_batch([patternInput[1], (msg = (new Msg_1(17, [new Msg(0, [value(initialModel.PersistentStorageState.Host)])])), singleton((dispatch) => {
        dispatch(msg);
    }))])];
}

//# sourceMappingURL=Init.js.map
