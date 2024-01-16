import { Route__get_isExpert, Route } from "./Routing.js";
import { BuildingBlock_Model, TermSearch_Model, RequestBuildingBlockInfoStates, BuildingBlockDetailsState, ApiState_get_noCall, ApiState, ApiCallHistoryItem, ApiCallStatus, LogItem_ofStringNow, LogItem, DevState, LogItem_ofInteropLogginMsg_Z2252A316, PersistentStorageState, PageState } from "./Model.js";
import { BuildingBlockDetailsMsg, TermSearch_Msg, PersistentStorageMsg, AdvancedSearch_Msg, BuildingBlock_Msg, ApiMsg, ApiResponseMsg, Msg, DevMsg, curry, System_Exception__Exception_GetPropagatedError, Model } from "./Messages.js";
import { Cmd_map, Cmd_OfPromise_either, Cmd_ofEffect, Cmd_batch, Cmd_none } from "../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { Swatehost_ofQueryParam_71136F3F } from "./Host.js";
import { ofArray, singleton as singleton_1, cons, isEmpty, append, filter, map } from "../../fable_modules/fable-library.4.9.0/List.js";
import { map as map_2, empty, singleton, append as append_1, delay as delay_1, toList } from "../../fable_modules/fable-library.4.9.0/Seq.js";
import { renderModal } from "./Modals/Controller.js";
import { interopLoggingModal } from "./Modals/InteropLoggingModal.js";
import { now } from "../../fable_modules/fable-library.4.9.0/Date.js";
import { errorModal } from "./Modals/ErrorModal.js";
import { getTableMetaData } from "./OfficeInterop/OfficeInterop.js";
import { Cmd_OfAsync_start, Cmd_OfAsyncWith_either } from "../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { testAPIv1, serviceApi, api } from "./Api.js";
import { Msg as Msg_1, FillHiddenColsState } from "./OfficeInterop/OfficeInteropState.js";
import { substring, printf, toText } from "../../fable_modules/fable-library.4.9.0/String.js";
import { Msg as Msg_2 } from "./States/SpreadsheetInterface.js";
import { map as map_1 } from "../../fable_modules/fable-library.4.9.0/Array.js";
import { SorensenDice_createBigrams } from "../Shared/Shared.js";
import { buildingBlockDetailModal } from "./Modals/BuildingBlockDetailsModal.js";
import { toString } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { update as update_1, bounce } from "../../fable_modules/Thoth.Elmish.Debouncer.2.0.0/Debouncer.fs.js";
import { update as update_2 } from "./Update/OfficeInteropUpdate.js";
import { update as update_3 } from "./Update/SpreadsheetUpdate.js";
import { Interface_update } from "./Update/InterfaceUpdate.js";
import { update as update_4 } from "./Pages/TermSearch/TermSearchView.js";
import { update as update_5 } from "./SidebarComponents/AdvancedSearch.js";
import { update as update_6 } from "./Pages/FilePicker/FilePickerView.js";
import { update as update_7 } from "./Pages/BuildingBlock/BuildingBlockView.js";
import { update as update_8 } from "./Pages/ProtocolTemplates/ProtocolState.js";
import { update as update_9 } from "./Pages/Cytoscape/CytoscapeUpdate.js";
import { update as update_10 } from "./Pages/JsonExporter/JsonExporter.js";
import { update as update_11 } from "./Pages/TemplateMetadata/TemplateMetadata.js";
import { update as update_12 } from "./Pages/Dag/Dag.js";

export function urlUpdate(route, currentModel) {
    let bind$0040_1;
    if (route == null) {
        return [new Model(new PageState(new Route(1, []), false), currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), Cmd_none()];
    }
    else if (route.tag === 0) {
        const queryIntegerOption = route.fields[0];
        return [new Model(new PageState(new Route(1, []), false), (bind$0040_1 = currentModel.PersistentStorageState, new PersistentStorageState(bind$0040_1.SearchableOntologies, bind$0040_1.AppVersion, Swatehost_ofQueryParam_71136F3F(queryIntegerOption), bind$0040_1.HasOntologiesLoaded)), currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), Cmd_none()];
    }
    else {
        const page = route;
        return [new Model(new PageState(page, Route__get_isExpert(page)), currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), Cmd_none()];
    }
}

export function Dev_update(devMsg, currentState) {
    switch (devMsg.tag) {
        case 2: {
            const parsedLogs = map(LogItem_ofInteropLogginMsg_Z2252A316, devMsg.fields[1]);
            const parsedDisplayLogs = filter((x) => {
                switch (x.tag) {
                    case 2:
                    case 3:
                        return true;
                    default:
                        return false;
                }
            }, parsedLogs);
            const nextState_1 = new DevState(append(parsedLogs, currentState.Log), append(parsedDisplayLogs, currentState.DisplayLogList));
            return [nextState_1, Cmd_batch(toList(delay_1(() => append_1(!isEmpty(parsedDisplayLogs) ? singleton(Cmd_ofEffect((dispatch) => {
                renderModal("GenericInteropLogs", (rmv) => interopLoggingModal(nextState_1, dispatch, rmv));
            })) : empty(), delay_1(() => singleton(devMsg.fields[0]))))))];
        }
        case 3: {
            const e = devMsg.fields[1];
            return [new DevState(cons(new LogItem(2, [[now(), System_Exception__Exception_GetPropagatedError(e)]]), currentState.Log), currentState.DisplayLogList), Cmd_batch(toList(delay_1(() => append_1(singleton(Cmd_ofEffect((_arg) => {
                renderModal("GenericError", (rmv_1) => errorModal(e, rmv_1));
            })), delay_1(() => singleton(devMsg.fields[0]))))))];
        }
        case 4:
            return [new DevState(currentState.Log, devMsg.fields[0]), Cmd_none()];
        case 0:
            return [currentState, Cmd_OfPromise_either(getTableMetaData, void 0, (arg) => (new Msg(3, [curry((tupledArg) => (new DevMsg(1, [tupledArg[0], tupledArg[1]])), Cmd_none(), arg)])), (arg_1) => (new Msg(3, [curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), Cmd_none(), arg_1)])))];
        default:
            return [new DevState(cons(LogItem_ofStringNow(devMsg.fields[1][0], devMsg.fields[1][1]), currentState.Log), currentState.DisplayLogList), devMsg.fields[0]];
    }
}

export function handleApiRequestMsg(reqMsg, currentState) {
    switch (reqMsg.tag) {
        case 1:
            return [new ApiState(new ApiCallHistoryItem("getTermSuggestionsByParentOntology", new ApiCallStatus(1, [])), currentState.callHistory), Cmd_OfAsyncWith_either((x_2) => {
                Cmd_OfAsync_start(x_2);
            }, api.getTermSuggestionsByParentTerm, {
                n: 5,
                parent_term: reqMsg.fields[1],
                query: reqMsg.fields[0],
            }, (arg_6) => (new Msg(2, [((arg_10) => (new ApiMsg(1, [new ApiResponseMsg(0, [arg_10])])))(arg_6)])), (arg_7) => (new Msg(2, [new ApiMsg(2, [arg_7])])))];
        case 2:
            return [new ApiState(new ApiCallHistoryItem("getUnitTermSuggestions", new ApiCallStatus(1, [])), currentState.callHistory), Cmd_OfAsyncWith_either((x_1) => {
                Cmd_OfAsync_start(x_1);
            }, api.getUnitTermSuggestions, {
                n: 5,
                query: reqMsg.fields[0],
            }, (arg_3) => (new Msg(2, [((arg_11) => (new ApiMsg(1, [new ApiResponseMsg(2, [arg_11])])))(arg_3)])), (arg_4) => (new Msg(2, [new ApiMsg(2, [arg_4])])))];
        case 3:
            return [new ApiState(new ApiCallHistoryItem("getTermsForAdvancedSearch", new ApiCallStatus(1, [])), currentState.callHistory), Cmd_OfAsyncWith_either((x_3) => {
                Cmd_OfAsync_start(x_3);
            }, api.getTermsForAdvancedSearch, reqMsg.fields[0], (arg_13) => (new Msg(2, [new ApiMsg(1, [new ApiResponseMsg(1, [arg_13])])])), (arg_14) => (new Msg(2, [new ApiMsg(2, [arg_14])])))];
        case 4:
            return [new ApiState(new ApiCallHistoryItem("getAllOntologies", new ApiCallStatus(1, [])), currentState.callHistory), Cmd_OfAsyncWith_either((x_4) => {
                Cmd_OfAsync_start(x_4);
            }, api.getAllOntologies, void 0, (arg_17) => (new Msg(2, [new ApiMsg(1, [new ApiResponseMsg(3, [arg_17])])])), (arg_18) => (new Msg(2, [new ApiMsg(2, [arg_18])])))];
        case 5:
            return [new ApiState(new ApiCallHistoryItem("getTermsByNames", new ApiCallStatus(1, [])), currentState.callHistory), Cmd_batch(ofArray([Cmd_OfAsyncWith_either((x_5) => {
                Cmd_OfAsync_start(x_5);
            }, api.getTermsByNames, reqMsg.fields[0], (arg_21) => (new Msg(2, [new ApiMsg(1, [new ApiResponseMsg(4, [arg_21])])])), (e) => (new Msg(21, [[new Msg(6, [new Msg_1(13, [new FillHiddenColsState(0, [])])]), new Msg(2, [new ApiMsg(2, [e])])]]))), singleton_1((dispatch) => {
                dispatch(new Msg(6, [new Msg_1(13, [new FillHiddenColsState(2, [])])]));
            })]))];
        case 6:
            return [new ApiState(new ApiCallHistoryItem("getAppVersion", new ApiCallStatus(1, [])), currentState.callHistory), Cmd_OfAsyncWith_either((x_6) => {
                Cmd_OfAsync_start(x_6);
            }, serviceApi.getAppVersion, void 0, (arg_24) => (new Msg(2, [new ApiMsg(1, [new ApiResponseMsg(5, [arg_24])])])), (arg_25) => (new Msg(2, [new ApiMsg(2, [arg_25])])))];
        default:
            return [new ApiState(new ApiCallHistoryItem("getTermSuggestions", new ApiCallStatus(1, [])), currentState.callHistory), Cmd_OfAsyncWith_either((x) => {
                Cmd_OfAsync_start(x);
            }, api.getTermSuggestions, {
                n: 5,
                query: reqMsg.fields[0],
            }, (arg) => (new Msg(2, [((arg_9) => (new ApiMsg(1, [new ApiResponseMsg(0, [arg_9])])))(arg)])), (arg_1) => (new Msg(2, [new ApiMsg(2, [arg_1])])))];
    }
}

export function handleApiResponseMsg(resMsg, currentState) {
    let msg_4, msg_6, msg_8, msg_12, msg_18, msg_20, msg, msg_2;
    switch (resMsg.tag) {
        case 2: {
            const finishedCall_1 = new ApiCallHistoryItem(currentState.currentCall.FunctionName, new ApiCallStatus(2, []));
            return [new ApiState(ApiState_get_noCall(), cons(finishedCall_1, currentState.callHistory)), Cmd_batch(ofArray([(msg_4 = (new Msg(2, [new ApiMsg(3, [["Debug", toText(printf("[ApiSuccess]: Call %s successfull."))(finishedCall_1.FunctionName)]])])), singleton_1((dispatch_2) => {
                dispatch_2(msg_4);
            })), (msg_6 = ((arg_3) => (new Msg(9, [new BuildingBlock_Msg(12, [arg_3])])))(resMsg.fields[0]), singleton_1((dispatch_3) => {
                dispatch_3(msg_6);
            }))]))];
        }
        case 1: {
            const finishedCall_2 = new ApiCallHistoryItem(currentState.currentCall.FunctionName, new ApiCallStatus(2, []));
            return [new ApiState(ApiState_get_noCall(), cons(finishedCall_2, currentState.callHistory)), Cmd_batch(ofArray([(msg_8 = (new Msg(2, [new ApiMsg(3, [["Debug", toText(printf("[ApiSuccess]: Call %s successfull."))(finishedCall_2.FunctionName)]])])), singleton_1((dispatch_4) => {
                dispatch_4(msg_8);
            })), singleton_1((dispatch_5) => {
                dispatch_5(new Msg(5, [new AdvancedSearch_Msg(6, [resMsg.fields[0]])]));
            })]))];
        }
        case 3: {
            const finishedCall_3 = new ApiCallHistoryItem(currentState.currentCall.FunctionName, new ApiCallStatus(2, []));
            return [new ApiState(ApiState_get_noCall(), cons(finishedCall_3, currentState.callHistory)), Cmd_batch(ofArray([(msg_12 = (new Msg(2, [new ApiMsg(3, [["Debug", toText(printf("[ApiSuccess]: Call %s successfull."))(finishedCall_3.FunctionName)]])])), singleton_1((dispatch_6) => {
                dispatch_6(msg_12);
            })), singleton_1((dispatch_7) => {
                dispatch_7(new Msg(7, [new PersistentStorageMsg(0, [resMsg.fields[0]])]));
            })]))];
        }
        case 4: {
            const finishedCall_4 = new ApiCallHistoryItem(currentState.currentCall.FunctionName, new ApiCallStatus(2, []));
            return [new ApiState(ApiState_get_noCall(), cons(finishedCall_4, currentState.callHistory)), Cmd_batch(ofArray([singleton_1((dispatch_8) => {
                dispatch_8(new Msg(17, [new Msg_2(13, [resMsg.fields[0]])]));
            }), (msg_18 = (new Msg(2, [new ApiMsg(3, [["Debug", toText(printf("[ApiSuccess]: Call %s successfull."))(finishedCall_4.FunctionName)]])])), singleton_1((dispatch_9) => {
                dispatch_9(msg_18);
            }))]))];
        }
        case 5: {
            const finishedCall_5 = new ApiCallHistoryItem(currentState.currentCall.FunctionName, new ApiCallStatus(2, []));
            return [new ApiState(ApiState_get_noCall(), cons(finishedCall_5, currentState.callHistory)), Cmd_batch(ofArray([(msg_20 = (new Msg(2, [new ApiMsg(3, [["Debug", toText(printf("[ApiSuccess]: Call %s successfull."))(finishedCall_5.FunctionName)]])])), singleton_1((dispatch_10) => {
                dispatch_10(msg_20);
            })), singleton_1((dispatch_11) => {
                dispatch_11(new Msg(7, [new PersistentStorageMsg(1, [resMsg.fields[0]])]));
            })]))];
        }
        default: {
            const finishedCall = new ApiCallHistoryItem(currentState.currentCall.FunctionName, new ApiCallStatus(2, []));
            return [new ApiState(ApiState_get_noCall(), cons(finishedCall, currentState.callHistory)), Cmd_batch(ofArray([(msg = (new Msg(2, [new ApiMsg(3, [["Debug", toText(printf("[ApiSuccess]: Call %s successfull."))(finishedCall.FunctionName)]])])), singleton_1((dispatch) => {
                dispatch(msg);
            })), (msg_2 = ((arg_2) => (new Msg(4, [new TermSearch_Msg(3, [arg_2])])))(resMsg.fields[0]), singleton_1((dispatch_1) => {
                dispatch_1(msg_2);
            }))]))];
        }
    }
}

export function handleApiMsg(apiMsg, currentState) {
    let msg_2;
    switch (apiMsg.tag) {
        case 3:
            return [currentState, (msg_2 = (new Msg(3, [curry((tupledArg_1) => (new DevMsg(1, [tupledArg_1[0], tupledArg_1[1]])), Cmd_none(), [apiMsg.fields[0][0], apiMsg.fields[0][1]])])), singleton_1((dispatch_1) => {
                dispatch_1(msg_2);
            }))];
        case 0:
            return handleApiRequestMsg(apiMsg.fields[0], currentState);
        case 1:
            return handleApiResponseMsg(apiMsg.fields[0], currentState);
        default: {
            const e = apiMsg.fields[0];
            const failedCall = new ApiCallHistoryItem(currentState.currentCall.FunctionName, new ApiCallStatus(3, [System_Exception__Exception_GetPropagatedError(e)]));
            return [new ApiState(ApiState_get_noCall(), cons(failedCall, currentState.callHistory)), Cmd_batch(toList(delay_1(() => append_1(singleton(Cmd_ofEffect((_arg) => {
                renderModal("GenericError", (rmv) => errorModal(e, rmv));
            })), delay_1(() => {
                let msg, arg_1;
                return singleton((msg = (new Msg(3, [curry((tupledArg) => (new DevMsg(1, [tupledArg[0], tupledArg[1]])), Cmd_none(), ["Error", (arg_1 = System_Exception__Exception_GetPropagatedError(e), toText(printf("[ApiError]: Call %s failed with: %s"))(failedCall.FunctionName)(arg_1))])])), singleton_1((dispatch) => {
                    dispatch(msg);
                })));
            })))))];
        }
    }
}

export function handlePersistenStorageMsg(persistentStorageMsg, currentState) {
    if (persistentStorageMsg.tag === 1) {
        return [new PersistentStorageState(currentState.SearchableOntologies, persistentStorageMsg.fields[0], currentState.Host, currentState.HasOntologiesLoaded), Cmd_none()];
    }
    else {
        return [new PersistentStorageState(map_1((ont) => [SorensenDice_createBigrams(ont.Name), ont], persistentStorageMsg.fields[0]), currentState.AppVersion, currentState.Host, true), Cmd_none()];
    }
}

export function handleBuildingBlockDetailsMsg(topLevelMsg, currentState) {
    switch (topLevelMsg.tag) {
        case 3:
            return [new BuildingBlockDetailsState(topLevelMsg.fields[0], currentState.BuildingBlockValues), Cmd_none()];
        case 0:
            return [new BuildingBlockDetailsState(new RequestBuildingBlockInfoStates(2, []), currentState.BuildingBlockValues), Cmd_OfAsyncWith_either((x_1) => {
                Cmd_OfAsync_start(x_1);
            }, api.getTermsByNames, topLevelMsg.fields[0], (arg) => (new Msg(13, [new BuildingBlockDetailsMsg(1, [arg])])), (x) => (new Msg(21, [[new Msg(3, [curry((tupledArg) => (new DevMsg(3, [tupledArg[0], tupledArg[1]])), Cmd_none(), x)]), new Msg(13, [new BuildingBlockDetailsMsg(3, [new RequestBuildingBlockInfoStates(0, [])])])]])))];
        case 1: {
            const nextState_3 = new BuildingBlockDetailsState(new RequestBuildingBlockInfoStates(0, []), topLevelMsg.fields[0]);
            return [nextState_3, Cmd_ofEffect((dispatch) => {
                renderModal("BuildingBlockDetails", (rmv) => buildingBlockDetailModal(nextState_3, dispatch, rmv));
            })];
        }
        default:
            return [new BuildingBlockDetailsState(currentState.CurrentRequestState, topLevelMsg.fields[0]), Cmd_none()];
    }
}

export function handleTopLevelMsg(topLevelMsg, currentModel) {
    let bind$0040, bind$0040_1;
    return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, (bind$0040 = currentModel.TermSearchState, new TermSearch_Model(bind$0040.TermSearchText, bind$0040.SelectedTerm, bind$0040.TermSuggestions, bind$0040.ParentOntology, bind$0040.SearchByParentOntology, bind$0040.HasSuggestionsLoading, false)), currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, (bind$0040_1 = currentModel.AddBuildingBlockState, new BuildingBlock_Model(bind$0040_1.Header, bind$0040_1.BodyCell, bind$0040_1.HeaderSearchText, bind$0040_1.HeaderSearchResults, bind$0040_1.BodySearchText, bind$0040_1.BodySearchResults, bind$0040_1.Unit2TermSearchText, bind$0040_1.Unit2SelectedTerm, bind$0040_1.Unit2TermSuggestions, bind$0040_1.HasUnit2TermSuggestionsLoading, false)), currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), Cmd_none()];
}

export function update(msg, model) {
    let msg_12;
    const msg_1 = msg;
    let currentModel;
    const msg_13 = msg;
    const model_1 = model;
    if ((msg_12 = msg_13, (msg_12.tag === 0) ? false : ((msg_12.tag === 3) ? false : (!(msg_12.tag === 19))))) {
        const txt = `${toString(msg_13)}`;
        currentModel = (new Model(model_1.PageState, model_1.PersistentStorageState, model_1.DebouncerState, new DevState(cons(LogItem_ofStringNow("Info", (txt.length > 62) ? (substring(txt, 0, 62) + "..") : txt), model_1.DevState.Log), model_1.DevState.DisplayLogList), model_1.TermSearchState, model_1.AdvancedSearchState, model_1.ExcelState, model_1.ApiState, model_1.FilePickerState, model_1.ProtocolState, model_1.AddBuildingBlockState, model_1.ValidationState, model_1.BuildingBlockDetailsState, model_1.SettingsXmlState, model_1.JsonExporterModel, model_1.TemplateMetadataModel, model_1.DagModel, model_1.CytoscapeModel, model_1.SpreadsheetModel, model_1.History));
    }
    else {
        currentModel = model_1;
    }
    switch (msg_1.tag) {
        case 22:
            return [new Model(model.PageState, model.PersistentStorageState, model.DebouncerState, model.DevState, model.TermSearchState, model.AdvancedSearchState, model.ExcelState, model.ApiState, model.FilePickerState, model.ProtocolState, model.AddBuildingBlockState, model.ValidationState, model.BuildingBlockDetailsState, model.SettingsXmlState, model.JsonExporterModel, model.TemplateMetadataModel, model.DagModel, model.CytoscapeModel, model.SpreadsheetModel, msg_1.fields[0]), Cmd_none()];
        case 23:
            return [currentModel, Cmd_map((Item_4) => (new Msg(3, [Item_4])), Cmd_OfAsyncWith_either((x) => {
                Cmd_OfAsync_start(x);
            }, testAPIv1.test, void 0, (b) => curry((tupledArg) => (new DevMsg(1, [tupledArg[0], tupledArg[1]])), Cmd_none(), b), (b_1) => curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), Cmd_none(), b_1)))];
        case 24:
            return [currentModel, Cmd_map((Item_9) => (new Msg(3, [Item_9])), Cmd_OfAsyncWith_either((x_1) => {
                Cmd_OfAsync_start(x_1);
            }, testAPIv1.postTest, "instrument Mod", (b_2) => curry((tupledArg_2) => (new DevMsg(1, [tupledArg_2[0], tupledArg_2[1]])), Cmd_none(), b_2), (b_3) => curry((tupledArg_3) => (new DevMsg(3, [tupledArg_3[0], tupledArg_3[1]])), Cmd_none(), b_3)))];
        case 21:
            return [currentModel, Cmd_batch(toList(delay_1(() => map_2((msg_2) => singleton_1((dispatch) => {
                dispatch(msg_2);
            }), msg_1.fields[0]))))];
        case 19: {
            const pageOpt = msg_1.fields[0];
            return [new Model((pageOpt == null) ? (new PageState(new Route(1, []), currentModel.PageState.IsExpert)) : (new PageState(pageOpt, currentModel.PageState.IsExpert)), currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), Cmd_none()];
        }
        case 20:
            return [new Model(new PageState(currentModel.PageState.CurrentPage, msg_1.fields[0]), currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), Cmd_none()];
        case 0: {
            const patternInput = bounce(msg_1.fields[0][0], msg_1.fields[0][1], msg_1.fields[0][2], currentModel.DebouncerState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, patternInput[0], currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), Cmd_map((Item_10) => (new Msg(1, [Item_10])), patternInput[1])];
        }
        case 1: {
            const patternInput_1 = update_1(msg_1.fields[0], currentModel.DebouncerState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, patternInput_1[0], currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_1[1]];
        }
        case 6: {
            const patternInput_2 = update_2(currentModel, msg_1.fields[0]);
            return [patternInput_2[0], patternInput_2[1]];
        }
        case 15: {
            const patternInput_3 = update_3(currentModel.SpreadsheetModel, currentModel, msg_1.fields[0]);
            const nextModel_5 = patternInput_3[1];
            return [new Model(nextModel_5.PageState, nextModel_5.PersistentStorageState, nextModel_5.DebouncerState, nextModel_5.DevState, nextModel_5.TermSearchState, nextModel_5.AdvancedSearchState, nextModel_5.ExcelState, nextModel_5.ApiState, nextModel_5.FilePickerState, nextModel_5.ProtocolState, nextModel_5.AddBuildingBlockState, nextModel_5.ValidationState, nextModel_5.BuildingBlockDetailsState, nextModel_5.SettingsXmlState, nextModel_5.JsonExporterModel, nextModel_5.TemplateMetadataModel, nextModel_5.DagModel, nextModel_5.CytoscapeModel, patternInput_3[0], nextModel_5.History), patternInput_3[2]];
        }
        case 17:
            return Interface_update(currentModel, msg_1.fields[0]);
        case 4: {
            const patternInput_4 = update_4(msg_1.fields[0], currentModel.TermSearchState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, patternInput_4[0], currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_4[1]];
        }
        case 5: {
            const patternInput_5 = update_5(msg_1.fields[0], currentModel.AdvancedSearchState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, patternInput_5[0], currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_5[1]];
        }
        case 3: {
            const patternInput_6 = Dev_update(msg_1.fields[0], currentModel.DevState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, patternInput_6[0], currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_6[1]];
        }
        case 2: {
            const patternInput_7 = handleApiMsg(msg_1.fields[0], currentModel.ApiState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, patternInput_7[0], currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_7[1]];
        }
        case 7: {
            const patternInput_8 = handlePersistenStorageMsg(msg_1.fields[0], currentModel.PersistentStorageState);
            return [new Model(currentModel.PageState, patternInput_8[0], currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_8[1]];
        }
        case 8: {
            const patternInput_9 = update_6(msg_1.fields[0], currentModel.FilePickerState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, patternInput_9[0], currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_9[1]];
        }
        case 9: {
            const patternInput_10 = update_7(msg_1.fields[0], currentModel.AddBuildingBlockState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, patternInput_10[0], currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_10[1]];
        }
        case 10: {
            const patternInput_11 = update_8(msg_1.fields[0], currentModel.ProtocolState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, patternInput_11[0], currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_11[1]];
        }
        case 13: {
            const patternInput_12 = handleBuildingBlockDetailsMsg(msg_1.fields[0], currentModel.BuildingBlockDetailsState);
            return [new Model(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, patternInput_12[0], currentModel.SettingsXmlState, currentModel.JsonExporterModel, currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), patternInput_12[1]];
        }
        case 14: {
            const patternInput_13 = update_9(msg_1.fields[0], currentModel.CytoscapeModel, currentModel);
            const nextModel0 = patternInput_13[1];
            return [new Model(nextModel0.PageState, nextModel0.PersistentStorageState, nextModel0.DebouncerState, nextModel0.DevState, nextModel0.TermSearchState, nextModel0.AdvancedSearchState, nextModel0.ExcelState, nextModel0.ApiState, nextModel0.FilePickerState, nextModel0.ProtocolState, nextModel0.AddBuildingBlockState, nextModel0.ValidationState, nextModel0.BuildingBlockDetailsState, nextModel0.SettingsXmlState, nextModel0.JsonExporterModel, nextModel0.TemplateMetadataModel, nextModel0.DagModel, patternInput_13[0], nextModel0.SpreadsheetModel, nextModel0.History), patternInput_13[2]];
        }
        case 11: {
            const patternInput_14 = update_10(msg_1.fields[0], currentModel);
            return [patternInput_14[0], patternInput_14[1]];
        }
        case 12: {
            const patternInput_15 = update_11(msg_1.fields[0], currentModel);
            return [patternInput_15[0], patternInput_15[1]];
        }
        case 16: {
            const patternInput_16 = update_12(msg_1.fields[0], currentModel);
            return [patternInput_16[0], patternInput_16[1]];
        }
        case 18: {
            const patternInput_17 = handleTopLevelMsg(msg_1.fields[0], currentModel);
            return [patternInput_17[0], patternInput_17[1]];
        }
        default:
            return [currentModel, Cmd_none()];
    }
}

//# sourceMappingURL=Update.js.map
