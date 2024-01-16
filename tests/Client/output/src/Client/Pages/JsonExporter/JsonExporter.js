import { Model as Model_1, DevMsg, curry, Msg as Msg_1, Model__updateByJsonExporterModel_70759DCE } from "../../Messages.js";
import { Msg, Model } from "../../States/JsonExporterState.js";
import { Cmd_OfPromise_either, Cmd_none } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { getBuildingBlocksAndSheets, getBuildingBlocksAndSheet } from "../../OfficeInterop/OfficeInterop.js";
import { cons, head, map, ofArray, singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Cmd_OfAsync_start, Cmd_OfAsyncWith_either } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { isaDotNetCommonApi, swateJsonAPIv1 } from "../../Api.js";
import { toString } from "../../../../fable_modules/fable-library.4.9.0/Types.js";
import { now, toUniversalTime, toString as toString_1 } from "../../../../fable_modules/fable-library.4.9.0/Date.js";
import { bind, defaultArg } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { createElement } from "react";
import * as react from "react";
import { defaultOf, equals, createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { JsonExportType, JsonExportType__get_toExplanation } from "../../../Shared/Shared.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";
import { map as map_1, empty, singleton as singleton_1, append, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { keyValueList } from "../../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { Msg as Msg_2 } from "../../States/SpreadsheetInterface.js";
import { rangeDouble } from "../../../../fable_modules/fable-library.4.9.0/Range.js";
import { equalsWith, map as map_2 } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { parse } from "../../../../fable_modules/fable-library.4.9.0/Int32.js";
import { split } from "../../../../fable_modules/fable-library.4.9.0/String.js";

export function download(filename, text) {
    const element = document.createElement("a");
    element.setAttribute("href", "data:text/plain;charset=utf-8," + encodeURIComponent(text));
    element.setAttribute("download", filename);
    element.style.display = "None";
    document.body.appendChild(element);
    element.click();
    return document.body.removeChild(element);
}

export function update(msg, currentModel) {
    let bind$0040_1, bind$0040_2, bind$0040_3, bind$0040_4, bind$0040_5, bind$0040_6, bind$0040_7, matchValue, matchValue_1, bind$0040_11, bind$0040_12, matchValue_2, bind$0040_14, bind$0040;
    switch (msg.tag) {
        case 1:
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_1 = currentModel.JsonExporterModel, new Model(bind$0040_1.CurrentExportType, bind$0040_1.TableJsonExportType, bind$0040_1.WorkbookJsonExportType, bind$0040_1.XLSXParsingExportType, bind$0040_1.Loading, msg.fields[0], false, false, bind$0040_1.XLSXByteArray))), Cmd_none()];
        case 2:
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_2 = currentModel.JsonExporterModel, new Model(bind$0040_2.CurrentExportType, bind$0040_2.TableJsonExportType, bind$0040_2.WorkbookJsonExportType, bind$0040_2.XLSXParsingExportType, bind$0040_2.Loading, false, msg.fields[0], false, bind$0040_2.XLSXByteArray))), Cmd_none()];
        case 3:
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_3 = currentModel.JsonExporterModel, new Model(bind$0040_3.CurrentExportType, bind$0040_3.TableJsonExportType, bind$0040_3.WorkbookJsonExportType, bind$0040_3.XLSXParsingExportType, bind$0040_3.Loading, false, false, msg.fields[0], bind$0040_3.XLSXByteArray))), Cmd_none()];
        case 5:
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_4 = currentModel.JsonExporterModel, new Model(bind$0040_4.CurrentExportType, msg.fields[0], bind$0040_4.WorkbookJsonExportType, bind$0040_4.XLSXParsingExportType, bind$0040_4.Loading, false, bind$0040_4.ShowWorkbookExportTypeDropdown, bind$0040_4.ShowXLSXExportTypeDropdown, bind$0040_4.XLSXByteArray))), Cmd_none()];
        case 6:
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_5 = currentModel.JsonExporterModel, new Model(bind$0040_5.CurrentExportType, bind$0040_5.TableJsonExportType, msg.fields[0], bind$0040_5.XLSXParsingExportType, bind$0040_5.Loading, bind$0040_5.ShowTableExportTypeDropdown, false, bind$0040_5.ShowXLSXExportTypeDropdown, bind$0040_5.XLSXByteArray))), Cmd_none()];
        case 7:
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_6 = currentModel.JsonExporterModel, new Model(bind$0040_6.CurrentExportType, bind$0040_6.TableJsonExportType, bind$0040_6.WorkbookJsonExportType, msg.fields[0], bind$0040_6.Loading, bind$0040_6.ShowTableExportTypeDropdown, bind$0040_6.ShowWorkbookExportTypeDropdown, false, bind$0040_6.XLSXByteArray))), Cmd_none()];
        case 4:
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_7 = currentModel.JsonExporterModel, new Model(bind$0040_7.CurrentExportType, bind$0040_7.TableJsonExportType, bind$0040_7.WorkbookJsonExportType, bind$0040_7.XLSXParsingExportType, bind$0040_7.Loading, false, false, false, bind$0040_7.XLSXByteArray))), Cmd_none()];
        case 8: {
            let nextModel_8;
            const bind$0040_8 = currentModel.JsonExporterModel;
            nextModel_8 = (new Model(bind$0040_8.CurrentExportType, bind$0040_8.TableJsonExportType, bind$0040_8.WorkbookJsonExportType, bind$0040_8.XLSXParsingExportType, true, bind$0040_8.ShowTableExportTypeDropdown, bind$0040_8.ShowWorkbookExportTypeDropdown, bind$0040_8.ShowXLSXExportTypeDropdown, bind$0040_8.XLSXByteArray));
            const cmd = Cmd_OfPromise_either(getBuildingBlocksAndSheet, void 0, (arg) => {
                let tupledArg;
                return new Msg_1(11, [(tupledArg = arg, new Msg(9, [tupledArg[0], tupledArg[1]]))]);
            }, (arg_1) => (new Msg_1(3, [((b) => curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), singleton((dispatch) => {
                dispatch(new Msg_1(11, [new Msg(0, [false])]));
            }), b))(arg_1)])));
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, nextModel_8), cmd];
        }
        case 9: {
            let nextModel_9;
            const bind$0040_9 = currentModel.JsonExporterModel;
            nextModel_9 = (new Model(currentModel.JsonExporterModel.TableJsonExportType, bind$0040_9.TableJsonExportType, bind$0040_9.WorkbookJsonExportType, bind$0040_9.XLSXParsingExportType, true, bind$0040_9.ShowTableExportTypeDropdown, bind$0040_9.ShowWorkbookExportTypeDropdown, bind$0040_9.ShowXLSXExportTypeDropdown, bind$0040_9.XLSXByteArray));
            const cmd_1 = Cmd_OfAsyncWith_either((x) => {
                Cmd_OfAsync_start(x);
            }, (matchValue = currentModel.JsonExporterModel.TableJsonExportType, (matchValue.tag === 1) ? swateJsonAPIv1.parseAnnotationTableToAssayJson : ((matchValue.tag === 0) ? swateJsonAPIv1.parseAnnotationTableToProcessSeqJson : (() => {
                throw new Error(`Cannot parse "${toString(matchValue)}" with this endpoint.`);
            })())), [msg.fields[0], msg.fields[1]], (arg_2) => (new Msg_1(11, [new Msg(10, [arg_2])])), (arg_3) => (new Msg_1(3, [((b_1) => curry((tupledArg_2) => (new DevMsg(3, [tupledArg_2[0], tupledArg_2[1]])), singleton((dispatch_1) => {
                dispatch_1(new Msg_1(11, [new Msg(0, [false])]));
            }), b_1))(arg_3)])));
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, nextModel_9), cmd_1];
        }
        case 11:
            return [currentModel, Cmd_OfPromise_either(getBuildingBlocksAndSheets, void 0, (arg_5) => (new Msg_1(11, [new Msg(12, [arg_5])])), (arg_6) => (new Msg_1(3, [((b_2) => curry((tupledArg_3) => (new DevMsg(3, [tupledArg_3[0], tupledArg_3[1]])), singleton((dispatch_2) => {
                dispatch_2(new Msg_1(11, [new Msg(0, [false])]));
            }), b_2))(arg_6)])))];
        case 12: {
            let nextModel_10;
            const bind$0040_10 = currentModel.JsonExporterModel;
            nextModel_10 = (new Model(currentModel.JsonExporterModel.WorkbookJsonExportType, bind$0040_10.TableJsonExportType, bind$0040_10.WorkbookJsonExportType, bind$0040_10.XLSXParsingExportType, true, bind$0040_10.ShowTableExportTypeDropdown, bind$0040_10.ShowWorkbookExportTypeDropdown, bind$0040_10.ShowXLSXExportTypeDropdown, bind$0040_10.XLSXByteArray));
            const cmd_3 = Cmd_OfAsyncWith_either((x_1) => {
                Cmd_OfAsync_start(x_1);
            }, (matchValue_1 = currentModel.JsonExporterModel.WorkbookJsonExportType, (matchValue_1.tag === 0) ? swateJsonAPIv1.parseAnnotationTablesToProcessSeqJson : ((matchValue_1.tag === 1) ? swateJsonAPIv1.parseAnnotationTablesToAssayJson : (() => {
                throw new Error(`Cannot parse "${toString(matchValue_1)}" with this endpoint.`);
            })())), msg.fields[0], (arg_7) => (new Msg_1(11, [new Msg(10, [arg_7])])), (arg_8) => (new Msg_1(3, [((b_3) => curry((tupledArg_4) => (new DevMsg(3, [tupledArg_4[0], tupledArg_4[1]])), singleton((dispatch_3) => {
                dispatch_3(new Msg_1(11, [new Msg(0, [false])]));
            }), b_3))(arg_8)])));
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, nextModel_10), cmd_3];
        }
        case 10: {
            download(`${toString_1(toUniversalTime(now()), "yyyyMMdd_hhmmss")}${defaultArg(bind((x_2) => {
                let copyOfStruct_2;
                return "_" + ((copyOfStruct_2 = x_2, toString(copyOfStruct_2)));
            }, currentModel.JsonExporterModel.CurrentExportType), "")}.json`, msg.fields[0]);
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_11 = currentModel.JsonExporterModel, new Model(void 0, bind$0040_11.TableJsonExportType, bind$0040_11.WorkbookJsonExportType, bind$0040_11.XLSXParsingExportType, false, bind$0040_11.ShowTableExportTypeDropdown, bind$0040_11.ShowWorkbookExportTypeDropdown, bind$0040_11.ShowXLSXExportTypeDropdown, bind$0040_11.XLSXByteArray))), Cmd_none()];
        }
        case 13:
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_12 = currentModel.JsonExporterModel, new Model(bind$0040_12.CurrentExportType, bind$0040_12.TableJsonExportType, bind$0040_12.WorkbookJsonExportType, bind$0040_12.XLSXParsingExportType, bind$0040_12.Loading, bind$0040_12.ShowTableExportTypeDropdown, bind$0040_12.ShowWorkbookExportTypeDropdown, bind$0040_12.ShowXLSXExportTypeDropdown, msg.fields[0]))), Cmd_none()];
        case 14: {
            let nextModel_13;
            const bind$0040_13 = currentModel.JsonExporterModel;
            nextModel_13 = (new Model(currentModel.JsonExporterModel.XLSXParsingExportType, bind$0040_13.TableJsonExportType, bind$0040_13.WorkbookJsonExportType, bind$0040_13.XLSXParsingExportType, true, bind$0040_13.ShowTableExportTypeDropdown, bind$0040_13.ShowWorkbookExportTypeDropdown, bind$0040_13.ShowXLSXExportTypeDropdown, bind$0040_13.XLSXByteArray));
            const cmd_4 = Cmd_OfAsyncWith_either((x_3) => {
                Cmd_OfAsync_start(x_3);
            }, (matchValue_2 = currentModel.JsonExporterModel.XLSXParsingExportType, (matchValue_2.tag === 1) ? isaDotNetCommonApi.toAssayJsonStr : ((matchValue_2.tag === 2) ? isaDotNetCommonApi.toSwateTemplateJsonStr : isaDotNetCommonApi.toProcessSeqJsonStr)), msg.fields[0], (arg_10) => (new Msg_1(11, [new Msg(15, [arg_10])])), (arg_11) => (new Msg_1(3, [((b_4) => curry((tupledArg_5) => (new DevMsg(3, [tupledArg_5[0], tupledArg_5[1]])), singleton((dispatch_4) => {
                dispatch_4(new Msg_1(11, [new Msg(0, [false])]));
            }), b_4))(arg_11)])));
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, nextModel_13), cmd_4];
        }
        case 15: {
            download(`${toString_1(toUniversalTime(now()), "yyyyMMdd_hhmmss")}${defaultArg(bind((x_4) => {
                let copyOfStruct_5;
                return "_" + ((copyOfStruct_5 = x_4, toString(copyOfStruct_5)));
            }, currentModel.JsonExporterModel.CurrentExportType), "")}.json`, msg.fields[0]);
            return [Model__updateByJsonExporterModel_70759DCE(currentModel, (bind$0040_14 = currentModel.JsonExporterModel, new Model(bind$0040_14.CurrentExportType, bind$0040_14.TableJsonExportType, bind$0040_14.WorkbookJsonExportType, bind$0040_14.XLSXParsingExportType, false, bind$0040_14.ShowTableExportTypeDropdown, bind$0040_14.ShowWorkbookExportTypeDropdown, bind$0040_14.ShowXLSXExportTypeDropdown, bind$0040_14.XLSXByteArray))), Cmd_none()];
        }
        default:
            return [new Model_1(currentModel.PageState, currentModel.PersistentStorageState, currentModel.DebouncerState, currentModel.DevState, currentModel.TermSearchState, currentModel.AdvancedSearchState, currentModel.ExcelState, currentModel.ApiState, currentModel.FilePickerState, currentModel.ProtocolState, currentModel.AddBuildingBlockState, currentModel.ValidationState, currentModel.BuildingBlockDetailsState, currentModel.SettingsXmlState, (bind$0040 = currentModel.JsonExporterModel, new Model(bind$0040.CurrentExportType, bind$0040.TableJsonExportType, bind$0040.WorkbookJsonExportType, bind$0040.XLSXParsingExportType, msg.fields[0], bind$0040.ShowTableExportTypeDropdown, bind$0040.ShowWorkbookExportTypeDropdown, bind$0040.ShowXLSXExportTypeDropdown, bind$0040.XLSXByteArray)), currentModel.TemplateMetadataModel, currentModel.DagModel, currentModel.CytoscapeModel, currentModel.SpreadsheetModel, currentModel.History), Cmd_none()];
    }
}

export function dropdownItem(exportType, model, msg, isActive) {
    let elems_1;
    return createElement("a", createObj(Helpers_combineClasses("dropdown-item", ofArray([["tabIndex", 0], ["onClick", (e) => {
        e.stopPropagation();
        msg(exportType);
    }], ["onKeyDown", (k) => {
        if (~~k.which === 13) {
            msg(exportType);
        }
    }], (elems_1 = [createElement("span", {
        className: "has-tooltip-right has-tooltip-multiline",
        "data-tooltip": JsonExportType__get_toExplanation(exportType),
        style: {
            fontSize: 1.1 + "rem",
            paddingRight: 10,
            textAlign: "center",
            color: "#cc9a00",
        },
        children: createElement("i", {
            className: "fa-solid fa-circle-info",
        }),
    }), createElement("span", {
        children: [toString(exportType)],
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function parseTableToISAJsonEle(model, dispatch) {
    let elems_7, elms_3;
    return mainFunctionContainer([createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], (elems_7 = [(elms_3 = singleton(createElement("div", createObj(Helpers_combineClasses("dropdown", toList(delay(() => append(model.JsonExporterModel.ShowTableExportTypeDropdown ? singleton_1(["className", "is-active"]) : empty(), delay(() => {
        let elems_4, elms, elems, props, children, elms_2, elms_1;
        return singleton_1((elems_4 = [(elms = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e) => {
            e.stopPropagation();
            dispatch(new Msg_1(11, [new Msg(1, [!model.JsonExporterModel.ShowTableExportTypeDropdown])]));
        }], (elems = [(props = [["style", {
            marginRight: "5px",
        }]], (children = [toString(model.JsonExporterModel.TableJsonExportType)], react.createElement("span", keyValueList(props, 1), ...children))), createElement("i", {
            className: "fa-solid fa-angle-down",
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))), createElement("div", {
            className: "dropdown-trigger",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        })), (elms_2 = singleton((elms_1 = toList(delay(() => {
            const msg = (arg_1) => {
                dispatch(new Msg_1(11, [new Msg(5, [arg_1])]));
            };
            return append(singleton_1(dropdownItem(new JsonExportType(1, []), model, msg, equals(model.JsonExporterModel.TableJsonExportType, new JsonExportType(1, [])))), delay(() => singleton_1(dropdownItem(new JsonExportType(0, []), model, msg, equals(model.JsonExporterModel.TableJsonExportType, new JsonExportType(0, []))))));
        })), createElement("div", {
            className: "dropdown-content",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        }))), createElement("div", {
            className: "dropdown-menu",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))]));
    })))))))), createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    })), createElement("div", createObj(Helpers_combineClasses("control", ofArray([["className", "is-expanded"], ["children", createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-info"], ["className", "is-fullwidth"], ["onClick", (_arg) => {
        dispatch(new Msg_1(17, [new Msg_2(9, [])]));
    }], ["children", "Download as isa json"]]))))]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_7))])]))))]);
}

export function parseTablesToISAJsonEle(model, dispatch) {
    let elems_7, elms_3;
    return mainFunctionContainer([createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], (elems_7 = [(elms_3 = singleton(createElement("div", createObj(Helpers_combineClasses("dropdown", toList(delay(() => append(model.JsonExporterModel.ShowWorkbookExportTypeDropdown ? singleton_1(["className", "is-active"]) : empty(), delay(() => {
        let elems_4, elms, elems, props, children, elms_2, elms_1;
        return singleton_1((elems_4 = [(elms = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e) => {
            e.stopPropagation();
            dispatch(new Msg_1(11, [new Msg(2, [!model.JsonExporterModel.ShowWorkbookExportTypeDropdown])]));
        }], (elems = [(props = [["style", {
            marginRight: "5px",
        }]], (children = [toString(model.JsonExporterModel.WorkbookJsonExportType)], react.createElement("span", keyValueList(props, 1), ...children))), createElement("i", {
            className: "fa-solid fa-angle-down",
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))), createElement("div", {
            className: "dropdown-trigger",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        })), (elms_2 = singleton((elms_1 = toList(delay(() => {
            const msg = (arg_1) => {
                dispatch(new Msg_1(11, [new Msg(6, [arg_1])]));
            };
            return append(singleton_1(dropdownItem(new JsonExportType(1, []), model, msg, equals(model.JsonExporterModel.WorkbookJsonExportType, new JsonExportType(1, [])))), delay(() => singleton_1(dropdownItem(new JsonExportType(0, []), model, msg, equals(model.JsonExporterModel.WorkbookJsonExportType, new JsonExportType(0, []))))));
        })), createElement("div", {
            className: "dropdown-content",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        }))), createElement("div", {
            className: "dropdown-menu",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))]));
    })))))))), createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    })), createElement("div", createObj(Helpers_combineClasses("control", ofArray([["className", "is-expanded"], ["children", createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-info"], ["className", "is-fullwidth"], ["onClick", (_arg) => {
        dispatch(new Msg_1(17, [new Msg_2(10, [])]));
    }], ["children", "Download as isa json"]]))))]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_7))])]))))]);
}

export function fileUploadButton(model, dispatch, id) {
    let elems;
    return createElement("label", createObj(Helpers_combineClasses("label", ofArray([["className", "mb-2 has-text-weight-normal"], (elems = [createElement("input", createObj(cons(["type", "file"], Helpers_combineClasses("file-input", ofArray([["id", id], ["type", "file"], ["style", {
        display: "none",
    }], ["onChange", (ev_1) => {
        const fileList = ev_1.target.files;
        if (!(fileList == null)) {
            const blobs = map((f) => f.slice(), toList(delay(() => map_1((i) => fileList.item(i), rangeDouble(0, 1, fileList.length - 1)))));
            const reader = new FileReader();
            reader.onload = ((evt) => {
                let arraybuffer;
                dispatch(new Msg_1(11, [new Msg(13, [(arraybuffer = evt.target.result, map_2((byteStr) => parse(byteStr, 511, true, 8), split(toString(new Uint8Array(arraybuffer)), [","], void 0, 1), Uint8Array))])]));
            });
            reader.onerror = ((evt_1) => {
                dispatch(new Msg_1(3, [curry((tupledArg) => (new DevMsg(1, [tupledArg[0], tupledArg[1]])), Cmd_none(), ["Error", evt_1.target.value])]));
            });
            reader.readAsArrayBuffer(head(blobs));
            const picker = document.getElementById(id);
            picker.value = defaultOf();
        }
    }]]))))), createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-info"], ["className", "is-fullwidth"], ["children", "Upload Excel file"]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

export function xlsxUploadAndParsingMainElement(model, dispatch) {
    let elems_7, elms_3;
    return mainFunctionContainer([fileUploadButton(model, dispatch, "xlsxConverter_uploadButton"), createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], (elems_7 = [(elms_3 = singleton(createElement("div", createObj(Helpers_combineClasses("dropdown", toList(delay(() => append(model.JsonExporterModel.ShowXLSXExportTypeDropdown ? singleton_1(["className", "is-active"]) : empty(), delay(() => {
        let elems_4, elms, elems, props, children, elms_2, elms_1;
        return singleton_1((elems_4 = [(elms = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e) => {
            e.stopPropagation();
            dispatch(new Msg_1(11, [new Msg(3, [!model.JsonExporterModel.ShowXLSXExportTypeDropdown])]));
        }], (elems = [(props = [["style", {
            marginRight: "5px",
        }]], (children = [toString(model.JsonExporterModel.XLSXParsingExportType)], react.createElement("span", keyValueList(props, 1), ...children))), createElement("i", {
            className: "fa-solid fa-angle-down",
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))), createElement("div", {
            className: "dropdown-trigger",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        })), (elms_2 = singleton((elms_1 = toList(delay(() => {
            const msg = (arg_1) => {
                dispatch(new Msg_1(11, [new Msg(7, [arg_1])]));
            };
            return append(singleton_1(dropdownItem(new JsonExportType(1, []), model, msg, equals(model.JsonExporterModel.XLSXParsingExportType, new JsonExportType(1, [])))), delay(() => append(singleton_1(dropdownItem(new JsonExportType(0, []), model, msg, equals(model.JsonExporterModel.XLSXParsingExportType, new JsonExportType(0, [])))), delay(() => singleton_1(dropdownItem(new JsonExportType(2, []), model, msg, equals(model.JsonExporterModel.XLSXParsingExportType, new JsonExportType(2, []))))))));
        })), createElement("div", {
            className: "dropdown-content",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        }))), createElement("div", {
            className: "dropdown-menu",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))]));
    })))))))), createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    })), createElement("div", createObj(Helpers_combineClasses("control", ofArray([["className", "is-expanded"], ["children", createElement("a", createObj(Helpers_combineClasses("button", toList(delay(() => {
        const hasContent = !equalsWith((x, y) => (x === y), model.JsonExporterModel.XLSXByteArray, new Uint8Array(0));
        return append(singleton_1(["className", "is-info"]), delay(() => append(hasContent ? singleton_1(["className", "is-active"]) : append(singleton_1(["className", "is-danger"]), delay(() => singleton_1(["disabled", true]))), delay(() => append(singleton_1(["className", "is-fullwidth"]), delay(() => append(singleton_1(["onClick", (_arg) => {
            if (hasContent) {
                dispatch(new Msg_1(11, [new Msg(14, [model.JsonExporterModel.XLSXByteArray])]));
            }
        }]), delay(() => singleton_1(["children", "Download as isa json"])))))))));
    })))))]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_7))])]))))]);
}

export function jsonExporterMainElement(model, dispatch) {
    let elems_1, elms;
    return createElement("div", createObj(Helpers_combineClasses("content", ofArray([["onSubmit", (e) => {
        e.preventDefault();
    }], ["onKeyDown", (k) => {
        if (~~k.which === 13) {
            k.preventDefault();
        }
    }], ["onClick", (e_1) => {
        dispatch(new Msg_1(11, [new Msg(4, [])]));
    }], ["style", {
        minHeight: 100 + "vh",
    }], (elems_1 = [createElement("label", {
        className: "label",
        children: "Json Exporter",
    }), (elms = ofArray(["Export swate annotation tables to ", react.createElement("a", {
        href: "https://en.wikipedia.org/wiki/JSON",
    }, "JSON"), " format. Official ISA-JSON types can be found ", react.createElement("a", {
        href: "https://isa-specs.readthedocs.io/en/latest/isajson.html#",
    }, "here"), "."]), createElement("p", {
        className: "help",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), createElement("label", {
        className: "label",
        children: "Export active table",
    }), parseTableToISAJsonEle(model, dispatch), createElement("label", {
        className: "label",
        children: "Export workbook",
    }), parseTablesToISAJsonEle(model, dispatch), createElement("label", {
        className: "label",
        children: "Export Swate conform xlsx file.",
    }), xlsxUploadAndParsingMainElement(model, dispatch)], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

//# sourceMappingURL=JsonExporter.js.map
