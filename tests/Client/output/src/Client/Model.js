import { Record, Union } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { obj_type, int32_type, list_type, array_type, option_type, lambda_type, unit_type, record_type, bool_type, tuple_type, string_type, class_type, union_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";
import { toText, printf, toFail } from "../../fable_modules/fable-library.4.9.0/String.js";
import { toShortTimeString, utcNow } from "../../fable_modules/fable-library.4.9.0/Date.js";
import * as react from "react";
import { keyValueList } from "../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { TermTypes_TermSearchable_$reflection, TermTypes_Ontology_$reflection, TermTypes_TermMinimal_$reflection, TermTypes_Term_$reflection } from "../Shared/TermTypes.js";
import { AdvancedSearchOptions_init, AdvancedSearchOptions_$reflection } from "../Shared/AdvancedSearchTypes.js";
import { empty } from "../../fable_modules/fable-library.4.9.0/List.js";
import { Swatehost_$reflection } from "./Host.js";
import { Route, Route_$reflection } from "./Routing.js";
import { CompositeHeader_$reflection, IOType_$reflection } from "../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeHeader.fs.js";
import { CompositeCell, CompositeCell_$reflection } from "../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeCell.fs.js";
import { ARCtrl_ISA_CompositeHeader__CompositeHeader_get_ParameterEmpty_Static } from "../Shared/ARCtrl.Helper.js";
import { InsertBuildingBlock_$reflection, BuildingBlock_$reflection } from "../Shared/OfficeInteropTypes.js";
import { TableValidation_init_Z30026FB0, TableValidation_$reflection } from "./OfficeInterop/CustomXmlTypes.js";
import { Template_$reflection } from "../../fable_modules/ARCtrl.1.0.4/Templates/Template.fs.js";

export class WindowSize extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Mini", "MobileMini", "Mobile", "Tablet", "Desktop", "Widescreen"];
    }
}

export function WindowSize_$reflection() {
    return union_type("Model.WindowSize", [], WindowSize, () => [[], [], [], [], [], []]);
}

export function WindowSize__get_threshold(this$) {
    switch (this$.tag) {
        case 1:
            return 575;
        case 2:
            return 768;
        case 3:
            return 1023;
        case 4:
            return 1215;
        case 5:
            return 1407;
        default:
            return 0;
    }
}

export function WindowSize_ofWidth_Z524259A4(width) {
    if (width < WindowSize__get_threshold(new WindowSize(1, []))) {
        return new WindowSize(0, []);
    }
    else if (width < WindowSize__get_threshold(new WindowSize(2, []))) {
        return new WindowSize(1, []);
    }
    else if (width < WindowSize__get_threshold(new WindowSize(3, []))) {
        return new WindowSize(2, []);
    }
    else if (width < WindowSize__get_threshold(new WindowSize(4, []))) {
        return new WindowSize(3, []);
    }
    else if (width < WindowSize__get_threshold(new WindowSize(5, []))) {
        return new WindowSize(4, []);
    }
    else if (width >= WindowSize__get_threshold(new WindowSize(5, []))) {
        return new WindowSize(5, []);
    }
    else {
        return toFail(printf("\'%A\' triggered an unexpected error when calculating screen size from width."))(width);
    }
}

export class LogItem extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Debug", "Info", "Error", "Warning"];
    }
}

export function LogItem_$reflection() {
    return union_type("Model.LogItem", [], LogItem, () => [[["Item", tuple_type(class_type("System.DateTime"), string_type)]], [["Item", tuple_type(class_type("System.DateTime"), string_type)]], [["Item", tuple_type(class_type("System.DateTime"), string_type)]], [["Item", tuple_type(class_type("System.DateTime"), string_type)]]]);
}

export function LogItem_ofInteropLogginMsg_Z2252A316(msg) {
    const matchValue = msg.LogIdentifier;
    switch (matchValue.tag) {
        case 0:
            return new LogItem(0, [[utcNow(), msg.MessageTxt]]);
        case 3:
            return new LogItem(2, [[utcNow(), msg.MessageTxt]]);
        case 2:
            return new LogItem(3, [[utcNow(), msg.MessageTxt]]);
        default:
            return new LogItem(1, [[utcNow(), msg.MessageTxt]]);
    }
}

export function LogItem_get_toTableRow() {
    return (_arg) => {
        let children_8, arg_1, props_10, children_16, arg_2, props_18, children_24, arg_3, props_26, children, arg, props_2;
        switch (_arg.tag) {
            case 1: {
                const children_14 = [(children_8 = [(arg_1 = toShortTimeString(_arg.fields[0][0]), toText(printf("[%s]"))(arg_1))], react.createElement("td", {}, ...children_8)), (props_10 = [["style", {
                    color: "#1FC2A7",
                    fontWeight: "bold",
                }]], react.createElement("td", keyValueList(props_10, 1), "Info")), react.createElement("td", {}, _arg.fields[0][1])];
                return react.createElement("tr", {}, ...children_14);
            }
            case 2: {
                const children_22 = [(children_16 = [(arg_2 = toShortTimeString(_arg.fields[0][0]), toText(printf("[%s]"))(arg_2))], react.createElement("td", {}, ...children_16)), (props_18 = [["style", {
                    color: "#C21F3A",
                    fontWeight: "bold",
                }]], react.createElement("td", keyValueList(props_18, 1), "ERROR")), react.createElement("td", {}, _arg.fields[0][1])];
                return react.createElement("tr", {}, ...children_22);
            }
            case 3: {
                const children_30 = [(children_24 = [(arg_3 = toShortTimeString(_arg.fields[0][0]), toText(printf("[%s]"))(arg_3))], react.createElement("td", {}, ...children_24)), (props_26 = [["style", {
                    color: "#FFC000",
                    fontWeight: "bold",
                }]], react.createElement("td", keyValueList(props_26, 1), "Warning")), react.createElement("td", {}, _arg.fields[0][1])];
                return react.createElement("tr", {}, ...children_30);
            }
            default: {
                const children_6 = [(children = [(arg = toShortTimeString(_arg.fields[0][0]), toText(printf("[%s]"))(arg))], react.createElement("td", {}, ...children)), (props_2 = [["style", {
                    color: "#4FB3D9",
                    fontWeight: "bold",
                }]], react.createElement("td", keyValueList(props_2, 1), "Debug")), react.createElement("td", {}, _arg.fields[0][1])];
                return react.createElement("tr", {}, ...children_6);
            }
        }
    };
}

export function LogItem_ofStringNow(level, message) {
    switch (level) {
        case "Debug":
        case "debug":
            return new LogItem(0, [[utcNow(), message]]);
        case "Info":
        case "info":
            return new LogItem(1, [[utcNow(), message]]);
        case "Error":
        case "error":
            return new LogItem(2, [[utcNow(), message]]);
        case "Warning":
        case "warning":
            return new LogItem(3, [[utcNow(), message]]);
        default:
            return new LogItem(2, [[utcNow(), toText(printf("Swate found an unexpected log identifier: %s"))(level)]]);
    }
}

export class TermSearch_TermSearchUIState extends Record {
    constructor(SearchIsActive, SearchIsLoading) {
        super();
        this.SearchIsActive = SearchIsActive;
        this.SearchIsLoading = SearchIsLoading;
    }
}

export function TermSearch_TermSearchUIState_$reflection() {
    return record_type("Model.TermSearch.TermSearchUIState", [], TermSearch_TermSearchUIState, () => [["SearchIsActive", bool_type], ["SearchIsLoading", bool_type]]);
}

export function TermSearch_TermSearchUIState_init() {
    return new TermSearch_TermSearchUIState(false, false);
}

export class TermSearch_TermSearchUIController extends Record {
    constructor(state, setState) {
        super();
        this.state = state;
        this.setState = setState;
    }
}

export function TermSearch_TermSearchUIController_$reflection() {
    return record_type("Model.TermSearch.TermSearchUIController", [], TermSearch_TermSearchUIController, () => [["state", TermSearch_TermSearchUIState_$reflection()], ["setState", lambda_type(TermSearch_TermSearchUIState_$reflection(), unit_type)]]);
}

export class TermSearch_Model extends Record {
    constructor(TermSearchText, SelectedTerm, TermSuggestions, ParentOntology, SearchByParentOntology, HasSuggestionsLoading, ShowSuggestions) {
        super();
        this.TermSearchText = TermSearchText;
        this.SelectedTerm = SelectedTerm;
        this.TermSuggestions = TermSuggestions;
        this.ParentOntology = ParentOntology;
        this.SearchByParentOntology = SearchByParentOntology;
        this.HasSuggestionsLoading = HasSuggestionsLoading;
        this.ShowSuggestions = ShowSuggestions;
    }
}

export function TermSearch_Model_$reflection() {
    return record_type("Model.TermSearch.Model", [], TermSearch_Model, () => [["TermSearchText", string_type], ["SelectedTerm", option_type(TermTypes_Term_$reflection())], ["TermSuggestions", array_type(TermTypes_Term_$reflection())], ["ParentOntology", option_type(TermTypes_TermMinimal_$reflection())], ["SearchByParentOntology", bool_type], ["HasSuggestionsLoading", bool_type], ["ShowSuggestions", bool_type]]);
}

export function TermSearch_Model_init() {
    return new TermSearch_Model("", void 0, [], void 0, true, false, false);
}

export class AdvancedSearch_AdvancedSearchSubpages extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["InputFormSubpage", "ResultsSubpage"];
    }
}

export function AdvancedSearch_AdvancedSearchSubpages_$reflection() {
    return union_type("Model.AdvancedSearch.AdvancedSearchSubpages", [], AdvancedSearch_AdvancedSearchSubpages, () => [[], []]);
}

export class AdvancedSearch_Model extends Record {
    constructor(ModalId, AdvancedSearchOptions, AdvancedSearchTermResults, AdvancedTermSearchSubpage, HasModalVisible, HasOntologyDropdownVisible, HasAdvancedSearchResultsLoading) {
        super();
        this.ModalId = ModalId;
        this.AdvancedSearchOptions = AdvancedSearchOptions;
        this.AdvancedSearchTermResults = AdvancedSearchTermResults;
        this.AdvancedTermSearchSubpage = AdvancedTermSearchSubpage;
        this.HasModalVisible = HasModalVisible;
        this.HasOntologyDropdownVisible = HasOntologyDropdownVisible;
        this.HasAdvancedSearchResultsLoading = HasAdvancedSearchResultsLoading;
    }
}

export function AdvancedSearch_Model_$reflection() {
    return record_type("Model.AdvancedSearch.Model", [], AdvancedSearch_Model, () => [["ModalId", string_type], ["AdvancedSearchOptions", AdvancedSearchOptions_$reflection()], ["AdvancedSearchTermResults", array_type(TermTypes_Term_$reflection())], ["AdvancedTermSearchSubpage", AdvancedSearch_AdvancedSearchSubpages_$reflection()], ["HasModalVisible", bool_type], ["HasOntologyDropdownVisible", bool_type], ["HasAdvancedSearchResultsLoading", bool_type]]);
}

export function AdvancedSearch_Model_init() {
    return new AdvancedSearch_Model("", AdvancedSearchOptions_init(), [], new AdvancedSearch_AdvancedSearchSubpages(0, []), false, false, false);
}

export function AdvancedSearch_Model_get_BuildingBlockHeaderId() {
    return "BuildingBlockHeader_ATS_Id";
}

export function AdvancedSearch_Model_get_BuildingBlockBodyId() {
    return "BuildingBlockBody_ATS_Id";
}

export class DevState extends Record {
    constructor(Log, DisplayLogList) {
        super();
        this.Log = Log;
        this.DisplayLogList = DisplayLogList;
    }
}

export function DevState_$reflection() {
    return record_type("Model.DevState", [], DevState, () => [["Log", list_type(LogItem_$reflection())], ["DisplayLogList", list_type(LogItem_$reflection())]]);
}

export function DevState_init() {
    return new DevState(empty(), empty());
}

export class PersistentStorageState extends Record {
    constructor(SearchableOntologies, AppVersion, Host, HasOntologiesLoaded) {
        super();
        this.SearchableOntologies = SearchableOntologies;
        this.AppVersion = AppVersion;
        this.Host = Host;
        this.HasOntologiesLoaded = HasOntologiesLoaded;
    }
}

export function PersistentStorageState_$reflection() {
    return record_type("Model.PersistentStorageState", [], PersistentStorageState, () => [["SearchableOntologies", array_type(tuple_type(class_type("Microsoft.FSharp.Collections.FSharpSet`1", [string_type]), TermTypes_Ontology_$reflection()))], ["AppVersion", string_type], ["Host", option_type(Swatehost_$reflection())], ["HasOntologiesLoaded", bool_type]]);
}

export function PersistentStorageState_init() {
    return new PersistentStorageState([], "", void 0, false);
}

export class ApiCallStatus extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["IsNone", "Pending", "Successfull", "Failed"];
    }
}

export function ApiCallStatus_$reflection() {
    return union_type("Model.ApiCallStatus", [], ApiCallStatus, () => [[], [], [], [["Item", string_type]]]);
}

export class ApiCallHistoryItem extends Record {
    constructor(FunctionName, Status) {
        super();
        this.FunctionName = FunctionName;
        this.Status = Status;
    }
}

export function ApiCallHistoryItem_$reflection() {
    return record_type("Model.ApiCallHistoryItem", [], ApiCallHistoryItem, () => [["FunctionName", string_type], ["Status", ApiCallStatus_$reflection()]]);
}

export class ApiState extends Record {
    constructor(currentCall, callHistory) {
        super();
        this.currentCall = currentCall;
        this.callHistory = callHistory;
    }
}

export function ApiState_$reflection() {
    return record_type("Model.ApiState", [], ApiState, () => [["currentCall", ApiCallHistoryItem_$reflection()], ["callHistory", list_type(ApiCallHistoryItem_$reflection())]]);
}

export function ApiState_init() {
    return new ApiState(ApiState_get_noCall(), empty());
}

export function ApiState_get_noCall() {
    return new ApiCallHistoryItem("None", new ApiCallStatus(0, []));
}

export class PageState extends Record {
    constructor(CurrentPage, IsExpert) {
        super();
        this.CurrentPage = CurrentPage;
        this.IsExpert = IsExpert;
    }
}

export function PageState_$reflection() {
    return record_type("Model.PageState", [], PageState, () => [["CurrentPage", Route_$reflection()], ["IsExpert", bool_type]]);
}

export function PageState_init() {
    return new PageState(new Route(1, []), false);
}

export class FilePicker_Model extends Record {
    constructor(FileNames) {
        super();
        this.FileNames = FileNames;
    }
}

export function FilePicker_Model_$reflection() {
    return record_type("Model.FilePicker.Model", [], FilePicker_Model, () => [["FileNames", list_type(tuple_type(int32_type, string_type))]]);
}

export function FilePicker_Model_init() {
    return new FilePicker_Model(empty());
}

export class BuildingBlock_DropdownPage extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Main", "More", "IOTypes"];
    }
}

export function BuildingBlock_DropdownPage_$reflection() {
    return union_type("Model.BuildingBlock.DropdownPage", [], BuildingBlock_DropdownPage, () => [[], [], [["Item", tuple_type(lambda_type(IOType_$reflection(), CompositeHeader_$reflection()), string_type)]]]);
}

export function BuildingBlock_DropdownPage__get_toString(this$) {
    switch (this$.tag) {
        case 1:
            return "More";
        case 2:
            return this$.fields[0][1];
        default:
            return "Main Page";
    }
}

export function BuildingBlock_DropdownPage__get_toTooltip(this$) {
    switch (this$.tag) {
        case 1:
            return "More";
        case 2:
            return `Per table only one ${this$.fields[0][1]} is allowed. The value of this column must be a unique identifier.`;
        default:
            return "";
    }
}

export class BuildingBlock_BodyCellType extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Term", "Unitized", "Text"];
    }
}

export function BuildingBlock_BodyCellType_$reflection() {
    return union_type("Model.BuildingBlock.BodyCellType", [], BuildingBlock_BodyCellType, () => [[], [], []]);
}

export class BuildingBlock_BuildingBlockUIState extends Record {
    constructor(DropdownIsActive, DropdownPage, BodyCellType) {
        super();
        this.DropdownIsActive = DropdownIsActive;
        this.DropdownPage = DropdownPage;
        this.BodyCellType = BodyCellType;
    }
}

export function BuildingBlock_BuildingBlockUIState_$reflection() {
    return record_type("Model.BuildingBlock.BuildingBlockUIState", [], BuildingBlock_BuildingBlockUIState, () => [["DropdownIsActive", bool_type], ["DropdownPage", BuildingBlock_DropdownPage_$reflection()], ["BodyCellType", BuildingBlock_BodyCellType_$reflection()]]);
}

export function BuildingBlock_BuildingBlockUIState_init() {
    return new BuildingBlock_BuildingBlockUIState(false, new BuildingBlock_DropdownPage(0, []), new BuildingBlock_BodyCellType(0, []));
}

export class BuildingBlock_Model extends Record {
    constructor(Header, BodyCell, HeaderSearchText, HeaderSearchResults, BodySearchText, BodySearchResults, Unit2TermSearchText, Unit2SelectedTerm, Unit2TermSuggestions, HasUnit2TermSuggestionsLoading, ShowUnit2TermSuggestions) {
        super();
        this.Header = Header;
        this.BodyCell = BodyCell;
        this.HeaderSearchText = HeaderSearchText;
        this.HeaderSearchResults = HeaderSearchResults;
        this.BodySearchText = BodySearchText;
        this.BodySearchResults = BodySearchResults;
        this.Unit2TermSearchText = Unit2TermSearchText;
        this.Unit2SelectedTerm = Unit2SelectedTerm;
        this.Unit2TermSuggestions = Unit2TermSuggestions;
        this.HasUnit2TermSuggestionsLoading = HasUnit2TermSuggestionsLoading;
        this.ShowUnit2TermSuggestions = ShowUnit2TermSuggestions;
    }
}

export function BuildingBlock_Model_$reflection() {
    return record_type("Model.BuildingBlock.Model", [], BuildingBlock_Model, () => [["Header", CompositeHeader_$reflection()], ["BodyCell", CompositeCell_$reflection()], ["HeaderSearchText", string_type], ["HeaderSearchResults", array_type(TermTypes_Term_$reflection())], ["BodySearchText", string_type], ["BodySearchResults", array_type(TermTypes_Term_$reflection())], ["Unit2TermSearchText", string_type], ["Unit2SelectedTerm", option_type(TermTypes_Term_$reflection())], ["Unit2TermSuggestions", array_type(TermTypes_Term_$reflection())], ["HasUnit2TermSuggestionsLoading", bool_type], ["ShowUnit2TermSuggestions", bool_type]]);
}

export function BuildingBlock_Model_init() {
    return new BuildingBlock_Model(ARCtrl_ISA_CompositeHeader__CompositeHeader_get_ParameterEmpty_Static(), CompositeCell.emptyTerm, "", [], "", [], "", void 0, [], false, false);
}

export class Validation_Model extends Record {
    constructor(ActiveTableBuildingBlocks, TableValidationScheme, DisplayedOptionsId) {
        super();
        this.ActiveTableBuildingBlocks = ActiveTableBuildingBlocks;
        this.TableValidationScheme = TableValidationScheme;
        this.DisplayedOptionsId = DisplayedOptionsId;
    }
}

export function Validation_Model_$reflection() {
    return record_type("Model.Validation.Model", [], Validation_Model, () => [["ActiveTableBuildingBlocks", array_type(BuildingBlock_$reflection())], ["TableValidationScheme", TableValidation_$reflection()], ["DisplayedOptionsId", option_type(int32_type)]]);
}

export function Validation_Model_init() {
    return new Validation_Model([], TableValidation_init_Z30026FB0(), void 0);
}

export class Protocol_CuratedCommunityFilter extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Both", "OnlyCurated", "OnlyCommunity"];
    }
}

export function Protocol_CuratedCommunityFilter_$reflection() {
    return union_type("Model.Protocol.CuratedCommunityFilter", [], Protocol_CuratedCommunityFilter, () => [[], [], []]);
}

export class Protocol_Model extends Record {
    constructor(Loading, UploadedFileParsed, ProtocolSelected, ProtocolsAll) {
        super();
        this.Loading = Loading;
        this.UploadedFileParsed = UploadedFileParsed;
        this.ProtocolSelected = ProtocolSelected;
        this.ProtocolsAll = ProtocolsAll;
    }
}

export function Protocol_Model_$reflection() {
    return record_type("Model.Protocol.Model", [], Protocol_Model, () => [["Loading", bool_type], ["UploadedFileParsed", array_type(tuple_type(string_type, array_type(InsertBuildingBlock_$reflection())))], ["ProtocolSelected", option_type(Template_$reflection())], ["ProtocolsAll", array_type(Template_$reflection())]]);
}

export function Protocol_Model_init() {
    return new Protocol_Model(false, [], void 0, []);
}

export class RequestBuildingBlockInfoStates extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Inactive", "RequestExcelInformation", "RequestDataBaseInformation"];
    }
}

export function RequestBuildingBlockInfoStates_$reflection() {
    return union_type("Model.RequestBuildingBlockInfoStates", [], RequestBuildingBlockInfoStates, () => [[], [], []]);
}

export function RequestBuildingBlockInfoStates__get_toStringMsg(this$) {
    switch (this$.tag) {
        case 1:
            return "Check Columns";
        case 2:
            return "Search Database ";
        default:
            return "";
    }
}

export class BuildingBlockDetailsState extends Record {
    constructor(CurrentRequestState, BuildingBlockValues) {
        super();
        this.CurrentRequestState = CurrentRequestState;
        this.BuildingBlockValues = BuildingBlockValues;
    }
}

export function BuildingBlockDetailsState_$reflection() {
    return record_type("Model.BuildingBlockDetailsState", [], BuildingBlockDetailsState, () => [["CurrentRequestState", RequestBuildingBlockInfoStates_$reflection()], ["BuildingBlockValues", array_type(TermTypes_TermSearchable_$reflection())]]);
}

export function BuildingBlockDetailsState_init() {
    return new BuildingBlockDetailsState(new RequestBuildingBlockInfoStates(0, []), []);
}

export class SettingsXml_Model extends Record {
    constructor(ActiveSwateValidation, NextAnnotationTableForActiveValidation, ActiveProtocolGroup, NextAnnotationTableForActiveProtGroup, ActiveProtocol, NextAnnotationTableForActiveProtocol, RawXml, NextRawXml, FoundTables, ValidationXmls) {
        super();
        this.ActiveSwateValidation = ActiveSwateValidation;
        this.NextAnnotationTableForActiveValidation = NextAnnotationTableForActiveValidation;
        this.ActiveProtocolGroup = ActiveProtocolGroup;
        this.NextAnnotationTableForActiveProtGroup = NextAnnotationTableForActiveProtGroup;
        this.ActiveProtocol = ActiveProtocol;
        this.NextAnnotationTableForActiveProtocol = NextAnnotationTableForActiveProtocol;
        this.RawXml = RawXml;
        this.NextRawXml = NextRawXml;
        this.FoundTables = FoundTables;
        this.ValidationXmls = ValidationXmls;
    }
}

export function SettingsXml_Model_$reflection() {
    return record_type("Model.SettingsXml.Model", [], SettingsXml_Model, () => [["ActiveSwateValidation", option_type(obj_type)], ["NextAnnotationTableForActiveValidation", option_type(string_type)], ["ActiveProtocolGroup", option_type(obj_type)], ["NextAnnotationTableForActiveProtGroup", option_type(string_type)], ["ActiveProtocol", option_type(obj_type)], ["NextAnnotationTableForActiveProtocol", option_type(string_type)], ["RawXml", option_type(string_type)], ["NextRawXml", option_type(string_type)], ["FoundTables", array_type(string_type)], ["ValidationXmls", array_type(obj_type)]]);
}

export function SettingsXml_Model_init() {
    return new SettingsXml_Model(void 0, void 0, void 0, void 0, void 0, void 0, void 0, void 0, [], []);
}

//# sourceMappingURL=Model.js.map
