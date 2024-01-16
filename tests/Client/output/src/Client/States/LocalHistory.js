import { SimpleJson_tryParse } from "../../../fable_modules/Fable.SimpleJson.3.24.0/SimpleJson.fs.js";
import { Convert_serialize, Convert_fromJson } from "../../../fable_modules/Fable.SimpleJson.3.24.0/Json.Converter.fs.js";
import { createTypeInfo } from "../../../fable_modules/Fable.SimpleJson.3.24.0/TypeInfo.Converter.fs.js";
import { int32_type, record_type, string_type, union_type, list_type, class_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { Record, Union } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { Model as Model_1, Model_init as Model_init_1, ActiveView_$reflection } from "./Spreadsheet.js";
import { ArcStudy_fromArcJsonString, ARCtrl_ISA_ArcStudy__ArcStudy_ToArcJsonString_71136F3F } from "../../../fable_modules/ARCtrl.ISA.Json.1.0.4/ArcTypes/ArcStudy.fs.js";
import { ArcAssay_fromArcJsonString, ARCtrl_ISA_ArcAssay__ArcAssay_ToArcJsonString_71136F3F } from "../../../fable_modules/ARCtrl.ISA.Json.1.0.4/ArcTypes/ArcAssay.fs.js";
import { Template_fromJsonString, Template_toJsonString } from "../../../fable_modules/ARCtrl.1.0.4/Templates/Template.Json.fs.js";
import { ArcInvestigation_fromArcJsonString, ARCtrl_ISA_ArcInvestigation__ArcInvestigation_ToArcJsonString_71136F3F } from "../../../fable_modules/ARCtrl.ISA.Json.1.0.4/ArcTypes/ArcInvestigation.fs.js";
import { iterate, isEmpty, append, cons, splitAt, length, tryItem, empty } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { ARCtrlHelper_ArcFiles } from "../../Shared/ARCtrl.Helper.js";
import { FSharpResult$2 } from "../../../fable_modules/fable-library.4.9.0/Choice.js";
import { value as value_1, map, flatten } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { parse } from "../../../fable_modules/fable-library.4.9.0/Int32.js";
import { newGuid as newGuid_1 } from "../../../fable_modules/fable-library.4.9.0/Guid.js";
import { printf, toConsole } from "../../../fable_modules/fable-library.4.9.0/String.js";

export function GeneralHelpers_tryGetSessionItem(key) {
    const v = sessionStorage.getItem(key);
    if (v == null) {
        return void 0;
    }
    else {
        return v;
    }
}

export function GeneralHelpers_tryGetLocalItem(key) {
    const v = localStorage.getItem(key);
    if (v == null) {
        return void 0;
    }
    else {
        return v;
    }
}

export function Keys_create_swate_session_history_table_key(tableGuid) {
    return "swate-table-history" + tableGuid;
}

export function HistoryOrder_ofJson(json) {
    const matchValue = SimpleJson_tryParse(json);
    if (matchValue != null) {
        return Convert_fromJson(matchValue, createTypeInfo(list_type(class_type("System.Guid"))));
    }
    else {
        throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
    }
}

export function HistoryOrder_tryFromSession() {
    const tryHistory = GeneralHelpers_tryGetSessionItem("swate_session_history_key");
    if (tryHistory == null) {
        return void 0;
    }
    else {
        return HistoryOrder_ofJson(tryHistory);
    }
}

export function HistoryOrder_toJson(history) {
    return Convert_serialize(history, createTypeInfo(list_type(class_type("System.Guid"))));
}

export class ConversionTypes_JsonArcFiles extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Investigation", "Study", "Assay", "Template", "None"];
    }
}

export function ConversionTypes_JsonArcFiles_$reflection() {
    return union_type("LocalHistory.ConversionTypes.JsonArcFiles", [], ConversionTypes_JsonArcFiles, () => [[], [], [], [], []]);
}

export class ConversionTypes_SessionStorage extends Record {
    constructor(JsonArcFiles, JsonString, ActiveView) {
        super();
        this.JsonArcFiles = JsonArcFiles;
        this.JsonString = JsonString;
        this.ActiveView = ActiveView;
    }
}

export function ConversionTypes_SessionStorage_$reflection() {
    return record_type("LocalHistory.ConversionTypes.SessionStorage", [], ConversionTypes_SessionStorage, () => [["JsonArcFiles", ConversionTypes_JsonArcFiles_$reflection()], ["JsonString", string_type], ["ActiveView", ActiveView_$reflection()]]);
}

export function ConversionTypes_SessionStorage_fromSpreadsheetModel_6DDB2EDA(model) {
    let al, s, a, t, i;
    let patternInput;
    const matchValue = model.ArcFile;
    patternInput = ((matchValue == null) ? [new ConversionTypes_JsonArcFiles(4, []), ""] : ((matchValue.tag === 2) ? ((al = matchValue.fields[1], (s = matchValue.fields[0], [new ConversionTypes_JsonArcFiles(1, []), ARCtrl_ISA_ArcStudy__ArcStudy_ToArcJsonString_71136F3F(s)]))) : ((matchValue.tag === 3) ? ((a = matchValue.fields[0], [new ConversionTypes_JsonArcFiles(2, []), ARCtrl_ISA_ArcAssay__ArcAssay_ToArcJsonString_71136F3F(a)])) : ((matchValue.tag === 0) ? ((t = matchValue.fields[0], [new ConversionTypes_JsonArcFiles(3, []), Template_toJsonString(0, t)])) : ((i = matchValue.fields[0], [new ConversionTypes_JsonArcFiles(0, []), ARCtrl_ISA_ArcInvestigation__ArcInvestigation_ToArcJsonString_71136F3F(i)]))))));
    return new ConversionTypes_SessionStorage(patternInput[0], patternInput[1], model.ActiveView);
}

export function ConversionTypes_SessionStorage__ToSpreadsheetModel(this$) {
    let matchValue;
    const init = Model_init_1();
    return new Model_1(this$.ActiveView, init.SelectedCells, (matchValue = this$.JsonArcFiles, (matchValue.tag === 1) ? (new ARCtrlHelper_ArcFiles(2, [ArcStudy_fromArcJsonString(this$.JsonString), empty()])) : ((matchValue.tag === 2) ? (new ARCtrlHelper_ArcFiles(3, [ArcAssay_fromArcJsonString(this$.JsonString)])) : ((matchValue.tag === 3) ? (new ARCtrlHelper_ArcFiles(0, [Template_fromJsonString(this$.JsonString)])) : ((matchValue.tag === 4) ? void 0 : (new ARCtrlHelper_ArcFiles(1, [ArcInvestigation_fromArcJsonString(this$.JsonString)])))))), init.Clipboard);
}

export function ConversionTypes_SessionStorage_toSpreadsheetModel_6F7B401E(sessionStorage) {
    return ConversionTypes_SessionStorage__ToSpreadsheetModel(sessionStorage);
}

export function Spreadsheet_Model__Model_fromJsonString_Static_Z721C83C5(json) {
    let matchValue;
    let conversionModel;
    try {
        conversionModel = (new FSharpResult$2(0, [(matchValue = SimpleJson_tryParse(json), (matchValue != null) ? Convert_fromJson(matchValue, createTypeInfo(ConversionTypes_SessionStorage_$reflection())) : (() => {
            throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
        })())]));
    }
    catch (ex) {
        conversionModel = (new FSharpResult$2(1, [ex.message]));
    }
    if (conversionModel.tag === 1) {
        console.log(["Error trying to read Spreadsheet.Model from local storage: ", conversionModel.fields[0]]);
        return Model_init_1();
    }
    else {
        return ConversionTypes_SessionStorage__ToSpreadsheetModel(conversionModel.fields[0]);
    }
}

export function Spreadsheet_Model__Model_ToJsonString(this$) {
    return Convert_serialize(ConversionTypes_SessionStorage_fromSpreadsheetModel_6DDB2EDA(this$), createTypeInfo(ConversionTypes_SessionStorage_$reflection()));
}

export function Spreadsheet_Model__Model_toJsonString_Static_6DDB2EDA(model) {
    return Spreadsheet_Model__Model_ToJsonString(model);
}

export function Spreadsheet_Model__Model_fromSessionStorage_Static_Z524259A4(position) {
    const guid = flatten(map((list) => tryItem(position, list), HistoryOrder_tryFromSession()));
    if (guid == null) {
        throw new Error("Not enough items in history list.");
    }
    const tryState = GeneralHelpers_tryGetSessionItem(Keys_create_swate_session_history_table_key(value_1(guid)));
    if (tryState == null) {
        throw new Error("Could not find any history.");
    }
    else {
        return Spreadsheet_Model__Model_fromJsonString_Static_Z721C83C5(tryState);
    }
}

export function Spreadsheet_Model__Model_fromLocalStorage_Static() {
    const snapshotJsonString = GeneralHelpers_tryGetLocalItem("swate_local_spreadsheet_key");
    if (snapshotJsonString == null) {
        return Model_init_1();
    }
    else {
        return Spreadsheet_Model__Model_fromJsonString_Static_Z721C83C5(snapshotJsonString);
    }
}

export function Spreadsheet_Model__Model_SaveToLocalStorage(this$) {
    const snapshotJsonString = Spreadsheet_Model__Model_ToJsonString(this$);
    localStorage.setItem("swate_local_spreadsheet_key", snapshotJsonString);
}

export class Model extends Record {
    constructor(HistoryItemCountLimit, HistoryCurrentPosition, HistoryExistingItemCount, HistoryOrder) {
        super();
        this.HistoryItemCountLimit = (HistoryItemCountLimit | 0);
        this.HistoryCurrentPosition = (HistoryCurrentPosition | 0);
        this.HistoryExistingItemCount = (HistoryExistingItemCount | 0);
        this.HistoryOrder = HistoryOrder;
    }
}

export function Model_$reflection() {
    return record_type("LocalHistory.Model", [], Model, () => [["HistoryItemCountLimit", int32_type], ["HistoryCurrentPosition", int32_type], ["HistoryExistingItemCount", int32_type], ["HistoryOrder", list_type(class_type("System.Guid"))]]);
}

export function Model_init() {
    return new Model(31, 0, 0, empty());
}

export function Model__UpdateFromSessionStorage(this$) {
    const position = map((value) => parse(value, 511, false, 32), GeneralHelpers_tryGetSessionItem("swate_session_history_position"));
    const history = HistoryOrder_tryFromSession();
    let matchResult, h, p;
    if (position != null) {
        if (history != null) {
            matchResult = 0;
            h = history;
            p = position;
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
            return new Model(this$.HistoryItemCountLimit, p, length(h), h);
        default:
            return this$;
    }
}

export function Model__NextPositionIsValid_Z524259A4(this$, newPosition) {
    return !((((newPosition < 0) ? true : (newPosition === this$.HistoryCurrentPosition)) ? true : (newPosition >= this$.HistoryItemCountLimit)) ? true : (newPosition >= this$.HistoryExistingItemCount));
}

/**
 * Save the next table state to session storage for history control. Table state is stored with guid as key and order is stored as guid list.
 */
export function Model__SaveSessionSnapshot_6DDB2EDA(this$, model) {
    const generateNewGuid = () => {
        const g = newGuid_1();
        const key = Keys_create_swate_session_history_table_key(g);
        const try_key = GeneralHelpers_tryGetSessionItem(key);
        if (try_key == null) {
            return [g, key];
        }
        else {
            return generateNewGuid();
        }
    };
    const patternInput = generateNewGuid();
    const snapshotJsonString = Spreadsheet_Model__Model_ToJsonString(model);
    let nextState;
    let patternInput_1;
    if (this$.HistoryCurrentPosition !== 0) {
        toConsole(printf("[HISTORY] Rebranch to %i"))(this$.HistoryCurrentPosition);
        const tupledArg = splitAt(this$.HistoryCurrentPosition, this$.HistoryOrder);
        patternInput_1 = [tupledArg[1], tupledArg[0]];
    }
    else {
        patternInput_1 = [this$.HistoryOrder, empty()];
    }
    const newlist = cons(patternInput[0], patternInput_1[0]);
    const patternInput_2 = (length(newlist) > this$.HistoryItemCountLimit) ? splitAt(this$.HistoryItemCountLimit, newlist) : [newlist, empty()];
    const newlist_1 = patternInput_2[0];
    const toRemoveList = append(patternInput_1[1], patternInput_2[1]);
    if (!isEmpty(toRemoveList)) {
        iterate((guid) => {
            const rmvKey = Keys_create_swate_session_history_table_key(guid);
            sessionStorage.removeItem(rmvKey);
        }, toRemoveList);
    }
    nextState = (new Model(this$.HistoryItemCountLimit, 0, length(newlist_1), newlist_1));
    sessionStorage.setItem(patternInput[1], snapshotJsonString);
    sessionStorage.setItem("swate_session_history_key", HistoryOrder_toJson(nextState.HistoryOrder));
    sessionStorage.setItem("swate_session_history_position", "0");
    const arg_1 = length(nextState.HistoryOrder) | 0;
    toConsole(printf("[HISTORY] length: %i"))(arg_1);
    return nextState;
}

export function Model__ResetAll(this$) {
    localStorage.clear();
    sessionStorage.clear();
    return Model_init();
}

//# sourceMappingURL=LocalHistory.js.map
