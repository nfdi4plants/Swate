import { Record, Union } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { uint8_type, int32_type, anonRecord_type, bool_type, record_type, option_type, array_type, union_type, class_type, tuple_type, string_type, list_type, lambda_type, unit_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";
import { Msg_$reflection as Msg_$reflection_1 } from "./OfficeInterop/InteropLogging.js";
import { TermSearch_TermSearchUIController_$reflection, AdvancedSearch_AdvancedSearchSubpages_$reflection, SettingsXml_Model_$reflection, BuildingBlockDetailsState_$reflection, Validation_Model_$reflection, BuildingBlock_Model_$reflection, Protocol_Model_$reflection, FilePicker_Model_$reflection, ApiState_$reflection, AdvancedSearch_Model_$reflection, TermSearch_Model_$reflection, DevState_$reflection, PersistentStorageState_$reflection, PageState_$reflection, RequestBuildingBlockInfoStates_$reflection, LogItem_$reflection } from "./Model.js";
import { TermTypes_Ontology_$reflection, TermTypes_Term_$reflection, TermTypes_TermSearchable_$reflection, TermTypes_TermMinimal_$reflection } from "../Shared/TermTypes.js";
import { AdvancedSearchOptions_$reflection } from "../Shared/AdvancedSearchTypes.js";
import { ColorMode_$reflection } from "./Colors/ExcelColors.js";
import { SelfMessage$1_$reflection, State_$reflection } from "../../fable_modules/Thoth.Elmish.Debouncer.2.0.0/Debouncer.fs.js";
import { Msg_$reflection as Msg_$reflection_2, Model_$reflection as Model_$reflection_1 } from "./OfficeInterop/OfficeInteropState.js";
import { Msg_$reflection as Msg_$reflection_3, Model_$reflection as Model_$reflection_2 } from "./States/JsonExporterState.js";
import { Msg_$reflection as Msg_$reflection_4, Model_$reflection as Model_$reflection_3 } from "./States/TemplateMetadataState.js";
import { Msg_$reflection as Msg_$reflection_7, Model_$reflection as Model_$reflection_4 } from "./States/DagState.js";
import { Msg_$reflection as Msg_$reflection_5, Model_$reflection as Model_$reflection_5 } from "./States/CytoscapeState.js";
import { Msg_$reflection as Msg_$reflection_6, Model_$reflection as Model_$reflection_6 } from "./States/Spreadsheet.js";
import { Model_$reflection as Model_$reflection_7 } from "./States/LocalHistory.js";
import { Msg_$reflection as Msg_$reflection_8 } from "./States/SpreadsheetInterface.js";
import { Route_$reflection } from "./Routing.js";
import { ProxyRequestException__get_ResponseText, ProxyRequestException } from "../../fable_modules/Fable.Remoting.Client.7.30.0/Types.fs.js";
import { SimpleJson_tryParse } from "../../fable_modules/Fable.SimpleJson.3.24.0/SimpleJson.fs.js";
import { Convert_fromJson } from "../../fable_modules/Fable.SimpleJson.3.24.0/Json.Converter.fs.js";
import { createTypeInfo } from "../../fable_modules/Fable.SimpleJson.3.24.0/TypeInfo.Converter.fs.js";
import { CompositeHeader_$reflection } from "../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeHeader.fs.js";
import { CompositeCell_$reflection } from "../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeCell.fs.js";
import { InsertBuildingBlock_$reflection } from "../Shared/OfficeInteropTypes.js";
import { Template_$reflection } from "../../fable_modules/ARCtrl.1.0.4/Templates/Template.fs.js";

export class DevMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["LogTableMetadata", "GenericLog", "GenericInteropLogs", "GenericError", "UpdateDisplayLogList"];
    }
}

export function DevMsg_$reflection() {
    return union_type("MessagesModule.DevMsg", [], DevMsg, () => [[], [["Item1", list_type(lambda_type(lambda_type(Msg_$reflection(), unit_type), unit_type))], ["Item2", tuple_type(string_type, string_type)]], [["Item1", list_type(lambda_type(lambda_type(Msg_$reflection(), unit_type), unit_type))], ["Item2", list_type(Msg_$reflection_1())]], [["Item1", list_type(lambda_type(lambda_type(Msg_$reflection(), unit_type), unit_type))], ["Item2", class_type("System.Exception")]], [["Item", list_type(LogItem_$reflection())]]]);
}

export class ApiRequestMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["GetNewTermSuggestions", "GetNewTermSuggestionsByParentTerm", "GetNewUnitTermSuggestions", "GetNewAdvancedTermSearchResults", "FetchAllOntologies", "SearchForInsertTermsRequest", "GetAppVersion"];
    }
}

export function ApiRequestMsg_$reflection() {
    return union_type("MessagesModule.ApiRequestMsg", [], ApiRequestMsg, () => [[["Item", string_type]], [["Item1", string_type], ["Item2", TermTypes_TermMinimal_$reflection()]], [["Item", string_type]], [["Item", AdvancedSearchOptions_$reflection()]], [], [["Item", array_type(TermTypes_TermSearchable_$reflection())]], []]);
}

export class ApiResponseMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["TermSuggestionResponse", "AdvancedTermSearchResultsResponse", "UnitTermSuggestionResponse", "FetchAllOntologiesResponse", "SearchForInsertTermsResponse", "GetAppVersionResponse"];
    }
}

export function ApiResponseMsg_$reflection() {
    return union_type("MessagesModule.ApiResponseMsg", [], ApiResponseMsg, () => [[["Item", array_type(TermTypes_Term_$reflection())]], [["Item", array_type(TermTypes_Term_$reflection())]], [["Item", array_type(TermTypes_Term_$reflection())]], [["Item", array_type(TermTypes_Ontology_$reflection())]], [["Item", array_type(TermTypes_TermSearchable_$reflection())]], [["Item", string_type]]]);
}

export class ApiMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Request", "Response", "ApiError", "ApiSuccess"];
    }
}

export function ApiMsg_$reflection() {
    return union_type("MessagesModule.ApiMsg", [], ApiMsg, () => [[["Item", ApiRequestMsg_$reflection()]], [["Item", ApiResponseMsg_$reflection()]], [["Item", class_type("System.Exception")]], [["Item", tuple_type(string_type, string_type)]]]);
}

export class StyleChangeMsg extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["UpdateColorMode"];
    }
}

export function StyleChangeMsg_$reflection() {
    return union_type("MessagesModule.StyleChangeMsg", [], StyleChangeMsg, () => [[["Item", ColorMode_$reflection()]]]);
}

export class PersistentStorageMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["NewSearchableOntologies", "UpdateAppVersion"];
    }
}

export function PersistentStorageMsg_$reflection() {
    return union_type("MessagesModule.PersistentStorageMsg", [], PersistentStorageMsg, () => [[["Item", array_type(TermTypes_Ontology_$reflection())]], [["Item", string_type]]]);
}

export class BuildingBlockDetailsMsg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["GetSelectedBuildingBlockTermsRequest", "GetSelectedBuildingBlockTermsResponse", "UpdateBuildingBlockValues", "UpdateCurrentRequestState"];
    }
}

export function BuildingBlockDetailsMsg_$reflection() {
    return union_type("MessagesModule.BuildingBlockDetailsMsg", [], BuildingBlockDetailsMsg, () => [[["Item", array_type(TermTypes_TermSearchable_$reflection())]], [["Item", array_type(TermTypes_TermSearchable_$reflection())]], [["Item", array_type(TermTypes_TermSearchable_$reflection())]], [["Item", RequestBuildingBlockInfoStates_$reflection()]]]);
}

export class SettingsDataStewardMsg extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["UpdatePointerJson"];
    }
}

export function SettingsDataStewardMsg_$reflection() {
    return union_type("MessagesModule.SettingsDataStewardMsg", [], SettingsDataStewardMsg, () => [[["Item", option_type(string_type)]]]);
}

export class TopLevelMsg extends Union {
    constructor() {
        super();
        this.tag = 0;
        this.fields = [];
    }
    cases() {
        return ["CloseSuggestions"];
    }
}

export function TopLevelMsg_$reflection() {
    return union_type("MessagesModule.TopLevelMsg", [], TopLevelMsg, () => [[]]);
}

export class Model extends Record {
    constructor(PageState, PersistentStorageState, DebouncerState, DevState, TermSearchState, AdvancedSearchState, ExcelState, ApiState, FilePickerState, ProtocolState, AddBuildingBlockState, ValidationState, BuildingBlockDetailsState, SettingsXmlState, JsonExporterModel, TemplateMetadataModel, DagModel, CytoscapeModel, SpreadsheetModel, History) {
        super();
        this.PageState = PageState;
        this.PersistentStorageState = PersistentStorageState;
        this.DebouncerState = DebouncerState;
        this.DevState = DevState;
        this.TermSearchState = TermSearchState;
        this.AdvancedSearchState = AdvancedSearchState;
        this.ExcelState = ExcelState;
        this.ApiState = ApiState;
        this.FilePickerState = FilePickerState;
        this.ProtocolState = ProtocolState;
        this.AddBuildingBlockState = AddBuildingBlockState;
        this.ValidationState = ValidationState;
        this.BuildingBlockDetailsState = BuildingBlockDetailsState;
        this.SettingsXmlState = SettingsXmlState;
        this.JsonExporterModel = JsonExporterModel;
        this.TemplateMetadataModel = TemplateMetadataModel;
        this.DagModel = DagModel;
        this.CytoscapeModel = CytoscapeModel;
        this.SpreadsheetModel = SpreadsheetModel;
        this.History = History;
    }
}

export function Model_$reflection() {
    return record_type("MessagesModule.Model", [], Model, () => [["PageState", PageState_$reflection()], ["PersistentStorageState", PersistentStorageState_$reflection()], ["DebouncerState", State_$reflection()], ["DevState", DevState_$reflection()], ["TermSearchState", TermSearch_Model_$reflection()], ["AdvancedSearchState", AdvancedSearch_Model_$reflection()], ["ExcelState", Model_$reflection_1()], ["ApiState", ApiState_$reflection()], ["FilePickerState", FilePicker_Model_$reflection()], ["ProtocolState", Protocol_Model_$reflection()], ["AddBuildingBlockState", BuildingBlock_Model_$reflection()], ["ValidationState", Validation_Model_$reflection()], ["BuildingBlockDetailsState", BuildingBlockDetailsState_$reflection()], ["SettingsXmlState", SettingsXml_Model_$reflection()], ["JsonExporterModel", Model_$reflection_2()], ["TemplateMetadataModel", Model_$reflection_3()], ["DagModel", Model_$reflection_4()], ["CytoscapeModel", Model_$reflection_5()], ["SpreadsheetModel", Model_$reflection_6()], ["History", Model_$reflection_7()]]);
}

export class Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Bounce", "DebouncerSelfMsg", "Api", "DevMsg", "TermSearchMsg", "AdvancedSearchMsg", "OfficeInteropMsg", "PersistentStorage", "FilePickerMsg", "BuildingBlockMsg", "ProtocolMsg", "JsonExporterMsg", "TemplateMetadataMsg", "BuildingBlockDetails", "CytoscapeMsg", "SpreadsheetMsg", "DagMsg", "InterfaceMsg", "TopLevelMsg", "UpdatePageState", "UpdateIsExpert", "Batch", "UpdateHistory", "TestMyAPI", "TestMyPostAPI", "DoNothing"];
    }
}

export function Msg_$reflection() {
    return union_type("MessagesModule.Msg", [], Msg, () => [[["Item", tuple_type(class_type("System.TimeSpan"), string_type, Msg_$reflection())]], [["Item", SelfMessage$1_$reflection(Msg_$reflection())]], [["Item", ApiMsg_$reflection()]], [["Item", DevMsg_$reflection()]], [["Item", TermSearch_Msg_$reflection()]], [["Item", AdvancedSearch_Msg_$reflection()]], [["Item", Msg_$reflection_2()]], [["Item", PersistentStorageMsg_$reflection()]], [["Item", FilePicker_Msg_$reflection()]], [["Item", BuildingBlock_Msg_$reflection()]], [["Item", Protocol_Msg_$reflection()]], [["Item", Msg_$reflection_3()]], [["Item", Msg_$reflection_4()]], [["Item", BuildingBlockDetailsMsg_$reflection()]], [["Item", Msg_$reflection_5()]], [["Item", Msg_$reflection_6()]], [["Item", Msg_$reflection_7()]], [["Item", Msg_$reflection_8()]], [["Item", TopLevelMsg_$reflection()]], [["Item", option_type(Route_$reflection())]], [["Item", bool_type]], [["Item", class_type("System.Collections.Generic.IEnumerable`1", [Msg_$reflection()])]], [["Item", Model_$reflection_7()]], [], [], []]);
}

export function System_Exception__Exception_GetPropagatedError(this$) {
    let matchValue;
    if (this$ instanceof ProxyRequestException) {
        const exn = this$;
        try {
            return ((matchValue = SimpleJson_tryParse(ProxyRequestException__get_ResponseText(exn)), (matchValue != null) ? Convert_fromJson(matchValue, createTypeInfo(anonRecord_type(["error", string_type], ["handled", bool_type], ["ignored", bool_type]))) : (() => {
                throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
            })())).error;
        }
        catch (ex) {
            return ex.message;
        }
    }
    else {
        return this$.message;
    }
}

export function curry(f, a, b) {
    return f([a, b]);
}

export class TermSearch_Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["ToggleSearchByParentOntology", "SearchTermTextChange", "TermSuggestionUsed", "NewSuggestions", "StoreParentOntologyFromOfficeInterop", "GetAllTermsByParentTermRequest", "GetAllTermsByParentTermResponse"];
    }
}

export function TermSearch_Msg_$reflection() {
    return union_type("MessagesModule.TermSearch.Msg", [], TermSearch_Msg, () => [[], [["searchString", string_type], ["parentTerm", option_type(TermTypes_TermMinimal_$reflection())]], [["Item", TermTypes_Term_$reflection()]], [["Item", array_type(TermTypes_Term_$reflection())]], [["Item", option_type(TermTypes_TermMinimal_$reflection())]], [["Item", TermTypes_TermMinimal_$reflection()]], [["Item", array_type(TermTypes_Term_$reflection())]]]);
}

export class AdvancedSearch_Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["ToggleModal", "ToggleOntologyDropdown", "UpdateAdvancedTermSearchSubpage", "ResetAdvancedSearchState", "UpdateAdvancedTermSearchOptions", "StartAdvancedSearch", "NewAdvancedSearchResults"];
    }
}

export function AdvancedSearch_Msg_$reflection() {
    return union_type("MessagesModule.AdvancedSearch.Msg", [], AdvancedSearch_Msg, () => [[["Item", string_type]], [], [["Item", AdvancedSearch_AdvancedSearchSubpages_$reflection()]], [], [["Item", AdvancedSearchOptions_$reflection()]], [], [["Item", array_type(TermTypes_Term_$reflection())]]]);
}

export class FilePicker_Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["LoadNewFiles", "UpdateFileNames"];
    }
}

export function FilePicker_Msg_$reflection() {
    return union_type("MessagesModule.FilePicker.Msg", [], FilePicker_Msg, () => [[["Item", list_type(string_type)]], [["newFileNames", list_type(tuple_type(int32_type, string_type))]]]);
}

export class BuildingBlock_Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["UpdateHeaderSearchText", "GetHeaderSuggestions", "GetHeaderSuggestionsResponse", "SelectHeader", "UpdateBodySearchText", "GetBodySuggestions", "GetBodySuggestionsByParent", "GetBodyTermsByParent", "GetBodySuggestionsResponse", "SelectBodyCell", "SearchUnitTermTextChange", "UnitTermSuggestionUsed", "NewUnitTermSuggestions"];
    }
}

export function BuildingBlock_Msg_$reflection() {
    return union_type("MessagesModule.BuildingBlock.Msg", [], BuildingBlock_Msg, () => [[["Item", string_type]], [["Item1", string_type], ["Item2", TermSearch_TermSearchUIController_$reflection()]], [["Item1", array_type(TermTypes_Term_$reflection())], ["Item2", TermSearch_TermSearchUIController_$reflection()]], [["Item", CompositeHeader_$reflection()]], [["Item", string_type]], [["Item1", string_type], ["Item2", TermSearch_TermSearchUIController_$reflection()]], [["Item1", string_type], ["Item2", TermTypes_TermMinimal_$reflection()], ["Item3", TermSearch_TermSearchUIController_$reflection()]], [["Item1", TermTypes_TermMinimal_$reflection()], ["Item2", TermSearch_TermSearchUIController_$reflection()]], [["Item1", array_type(TermTypes_Term_$reflection())], ["Item2", TermSearch_TermSearchUIController_$reflection()]], [["Item", CompositeCell_$reflection()]], [["searchString", string_type]], [["unitTerm", TermTypes_Term_$reflection()]], [["Item", array_type(TermTypes_Term_$reflection())]]]);
}

export class Protocol_Msg extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["ParseUploadedFileRequest", "ParseUploadedFileResponse", "RemoveUploadedFileParsed", "GetAllProtocolsRequest", "GetAllProtocolsResponse", "SelectProtocol", "ProtocolIncreaseTimesUsed", "RemoveSelectedProtocol", "UpdateLoading"];
    }
}

export function Protocol_Msg_$reflection() {
    return union_type("MessagesModule.Protocol.Msg", [], Protocol_Msg, () => [[["raw", array_type(uint8_type)]], [["Item", array_type(tuple_type(string_type, array_type(InsertBuildingBlock_$reflection())))]], [], [], [["Item", array_type(string_type)]], [["Item", Template_$reflection()]], [["protocolName", string_type]], [], [["Item", bool_type]]]);
}

export function Model__updateByExcelState_Z26496641(this$, s) {
    return new Model(this$.PageState, this$.PersistentStorageState, this$.DebouncerState, this$.DevState, this$.TermSearchState, this$.AdvancedSearchState, s, this$.ApiState, this$.FilePickerState, this$.ProtocolState, this$.AddBuildingBlockState, this$.ValidationState, this$.BuildingBlockDetailsState, this$.SettingsXmlState, this$.JsonExporterModel, this$.TemplateMetadataModel, this$.DagModel, this$.CytoscapeModel, this$.SpreadsheetModel, this$.History);
}

export function Model__updateByJsonExporterModel_70759DCE(this$, m) {
    return new Model(this$.PageState, this$.PersistentStorageState, this$.DebouncerState, this$.DevState, this$.TermSearchState, this$.AdvancedSearchState, this$.ExcelState, this$.ApiState, this$.FilePickerState, this$.ProtocolState, this$.AddBuildingBlockState, this$.ValidationState, this$.BuildingBlockDetailsState, this$.SettingsXmlState, m, this$.TemplateMetadataModel, this$.DagModel, this$.CytoscapeModel, this$.SpreadsheetModel, this$.History);
}

export function Model__updateByTemplateMetadataModel_Z686248C7(this$, m) {
    return new Model(this$.PageState, this$.PersistentStorageState, this$.DebouncerState, this$.DevState, this$.TermSearchState, this$.AdvancedSearchState, this$.ExcelState, this$.ApiState, this$.FilePickerState, this$.ProtocolState, this$.AddBuildingBlockState, this$.ValidationState, this$.BuildingBlockDetailsState, this$.SettingsXmlState, this$.JsonExporterModel, m, this$.DagModel, this$.CytoscapeModel, this$.SpreadsheetModel, this$.History);
}

export function Model__updateByDagModel_Z220F769A(this$, m) {
    return new Model(this$.PageState, this$.PersistentStorageState, this$.DebouncerState, this$.DevState, this$.TermSearchState, this$.AdvancedSearchState, this$.ExcelState, this$.ApiState, this$.FilePickerState, this$.ProtocolState, this$.AddBuildingBlockState, this$.ValidationState, this$.BuildingBlockDetailsState, this$.SettingsXmlState, this$.JsonExporterModel, this$.TemplateMetadataModel, m, this$.CytoscapeModel, this$.SpreadsheetModel, this$.History);
}

//# sourceMappingURL=Messages.js.map
