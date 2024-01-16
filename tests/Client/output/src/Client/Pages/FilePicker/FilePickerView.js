import { FilePicker_Model } from "../../Model.js";
import { Cmd_none } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { empty, sortByDescending, sortBy, map, contains, ofArray, singleton, mapIndexed } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Route } from "../../Routing.js";
import { FilePicker_Msg, Msg } from "../../Messages.js";
import { join, split, replace } from "../../../../fable_modules/fable-library.4.9.0/String.js";
import { equals, arrayHash, equalArrays, comparePrimitives, createObj, stringHash } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { last, take, tryFindIndexBack } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { createElement } from "react";
import * as react from "react";
import { empty as empty_1, append, singleton as singleton_1, collect, map as map_1, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { rangeDouble } from "../../../../fable_modules/fable-library.4.9.0/Range.js";
import { some } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { Msg as Msg_1 } from "../../States/SpreadsheetInterface.js";
import { responsiveReturnEle, triggerResponsiveReturnEle } from "../../SidebarComponents/ResponsiveFA.js";
import { List_except } from "../../../../fable_modules/fable-library.4.9.0/Seq2.js";
import { keyValueList } from "../../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { pageHeader, mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";

export function update(filePickerMsg, currentState) {
    if (filePickerMsg.tag === 1) {
        return [new FilePicker_Model(filePickerMsg.fields[0]), Cmd_none()];
    }
    else {
        return [new FilePicker_Model(mapIndexed((i, x) => [i + 1, x], filePickerMsg.fields[0])), singleton((dispatch) => {
            dispatch(new Msg(19, [new Route(3, [])]));
        })];
    }
}

function PathRerooting_normalizePath(path) {
    return replace(path, "\\", "/");
}

export const PathRerooting_listOfSupportedDirectories = ofArray(["studies", "assays", "workflows", "runs"]);

function PathRerooting_matchesSupportedDirectory(str) {
    return contains(str, PathRerooting_listOfSupportedDirectories, {
        Equals: (x, y) => (x === y),
        GetHashCode: stringHash,
    });
}

/**
 * Normalizes path and searches for 'listOfSupportedDirectories' (["studies"; "assays"; "workflows"; "runs"]) in path. reroots path to parent of supported directory if found
 * else returns only file name.
 */
export function PathRerooting_rerootPath(path) {
    const path_1 = PathRerooting_normalizePath(path);
    const splitPath = split(path_1, ["/"], void 0, 0);
    const tryFindLevel = tryFindIndexBack(PathRerooting_matchesSupportedDirectory, splitPath);
    if (tryFindLevel != null) {
        return replace(path_1, join("/", take(tryFindLevel, splitPath)) + "/", "");
    }
    else {
        return last(splitPath);
    }
}

export function uploadButton(model, dispatch, inputId) {
    const elms = ofArray([createElement("input", {
        style: {
            display: "none",
        },
        id: inputId,
        multiple: true,
        type: "file",
        onChange: (ev_1) => {
            const fileList = ev_1.target.files;
            if (!(fileList == null)) {
                const fileNames = map((f) => f.name, toList(delay(() => map_1((i) => fileList.item(i), rangeDouble(0, 1, fileList.length - 1)))));
                console.log(some(fileNames));
                dispatch(new Msg(8, [new FilePicker_Msg(0, [fileNames])]));
            }
        },
    }), createElement("button", createObj(Helpers_combineClasses("button", ofArray([["className", "is-info"], ["className", "is-fullwidth"], ["onClick", (e) => {
        const getUploadElement = document.getElementById(inputId);
        getUploadElement.click();
    }], ["children", "Pick file names"]]))))]);
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function insertButton(model, dispatch) {
    const elms = singleton(createElement("button", createObj(Helpers_combineClasses("button", ofArray([["className", "is-success"], ["className", "is-fullwidth"], ["onClick", (_arg) => {
        dispatch(new Msg(17, [new Msg_1(8, [map((tuple) => tuple[1], model.FilePickerState.FileNames)])]));
    }], ["children", "Insert file names"]])))));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function sortButton(icon, msg) {
    let elems_1, elms;
    return createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", msg], (elems_1 = [(elms = singleton(createElement("i", {
        className: join(" ", ["fa-lg", icon]),
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function fileSortElements(model, dispatch) {
    let elms, elems, elems_1;
    const elms_1 = singleton((elms = ofArray([createElement("a", createObj(Helpers_combineClasses("button", ofArray([["title", "Copy to Clipboard"], ["onClick", (e) => {
        triggerResponsiveReturnEle("clipboard_filepicker");
        const txt = join("\n", map((tuple) => tuple[1], model.FilePickerState.FileNames));
        const textArea = document.createElement("textarea");
        textArea.value = txt;
        textArea.style.top = "0";
        textArea.style.left = "0";
        textArea.style.position = "fixed";
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        const t = document.execCommand("copy");
        document.body.removeChild(textArea);
    }], (elems = [responsiveReturnEle("clipboard_filepicker", "fa-regular fa-clipboard", "fa-solid fa-check")], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))), createElement("div", createObj(Helpers_combineClasses("buttons", ofArray([["className", "has-addons"], ["style", {
        marginLeft: "auto",
    }], (elems_1 = [sortButton("fa-solid fa-arrow-down-a-z", (e_1) => {
        dispatch(new Msg(8, [new FilePicker_Msg(1, [mapIndexed((i, x_1) => [i + 1, x_1[1]], sortBy((tuple_1) => tuple_1[1], model.FilePickerState.FileNames, {
            Compare: comparePrimitives,
        }))])]));
    }), sortButton("fa-solid fa-arrow-down-z-a", (e_2) => {
        dispatch(new Msg(8, [new FilePicker_Msg(1, [mapIndexed((i_1, x_3) => [i_1 + 1, x_3[1]], sortByDescending((tuple_2) => tuple_2[1], model.FilePickerState.FileNames, {
            Compare: comparePrimitives,
        }))])]));
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))]), createElement("div", {
        className: "buttons",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    });
}

export function FileNameTable_deleteFromTable(id, fileName, model, dispatch) {
    return createElement("button", createObj(Helpers_combineClasses("delete", ofArray([["onClick", (_arg) => {
        dispatch(new Msg(8, [new FilePicker_Msg(1, [mapIndexed((i, tupledArg) => [i + 1, tupledArg[1]], List_except([[id, fileName]], model.FilePickerState.FileNames, {
            Equals: equalArrays,
            GetHashCode: arrayHash,
        }))])]));
    }], ["style", {
        marginRight: 2 + "rem",
    }]]))));
}

export function FileNameTable_moveUpButton(id, fileName, model, dispatch) {
    let elems_1, elms;
    return createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-small"], ["onClick", (_arg) => {
        dispatch(new Msg(8, [new FilePicker_Msg(1, [mapIndexed((i, v) => [i + 1, v[1]], sortBy((tuple) => tuple[0], map((tupledArg) => {
            const iterInd = tupledArg[0] | 0;
            const iterFileName = tupledArg[1];
            if (equalArrays([id, fileName], [iterInd, iterFileName])) {
                return [iterInd - 1.5, iterFileName];
            }
            else {
                return [iterInd, iterFileName];
            }
        }, model.FilePickerState.FileNames), {
            Compare: comparePrimitives,
        }))])]));
    }], (elems_1 = [(elms = singleton(createElement("i", {
        className: "fa-solid fa-arrow-up",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function FileNameTable_moveDownButton(id, fileName, model, dispatch) {
    let elems_1, elms;
    return createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-small"], ["onClick", (_arg) => {
        dispatch(new Msg(8, [new FilePicker_Msg(1, [mapIndexed((i, v) => [i + 1, v[1]], sortBy((tuple) => tuple[0], map((tupledArg) => {
            const iterInd = tupledArg[0] | 0;
            const iterFileName = tupledArg[1];
            if (equalArrays([id, fileName], [iterInd, iterFileName])) {
                return [iterInd + 1.5, iterFileName];
            }
            else {
                return [iterInd, iterFileName];
            }
        }, model.FilePickerState.FileNames), {
            Compare: comparePrimitives,
        }))])]));
    }], (elems_1 = [(elms = singleton(createElement("i", {
        className: "fa-solid fa-arrow-down",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function FileNameTable_moveButtonList(id, fileName, model, dispatch) {
    const elms = ofArray([FileNameTable_moveUpButton(id, fileName, model, dispatch), FileNameTable_moveDownButton(id, fileName, model, dispatch)]);
    return createElement("div", {
        className: "buttons",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function FileNameTable_table(model, dispatch) {
    let elems, children_12;
    return createElement("table", createObj(Helpers_combineClasses("table", ofArray([["className", "is-hoverable"], ["className", "is-striped"], ["className", "is-hoverable"], (elems = [(children_12 = toList(delay(() => collect((matchValue) => {
        let children_10, children_2, children_6, props_8, children_8;
        const index = matchValue[0] | 0;
        const fileName = matchValue[1];
        return singleton_1((children_10 = ofArray([(children_2 = [react.createElement("b", {}, `${index}`)], react.createElement("td", {}, ...children_2)), react.createElement("td", {}, fileName), (children_6 = [FileNameTable_moveButtonList(index, fileName, model, dispatch)], react.createElement("td", {}, ...children_6)), (props_8 = [["style", {
            textAlign: "right",
        }]], (children_8 = [FileNameTable_deleteFromTable(index, fileName, model, dispatch)], react.createElement("td", keyValueList(props_8, 1), ...children_8)))]), createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_10)),
        })));
    }, model.FilePickerState.FileNames))), react.createElement("tbody", {}, ...children_12))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

export function fileContainer(model, dispatch, inputId) {
    return mainFunctionContainer(toList(delay(() => append(singleton_1(createElement("p", {
        className: "help",
        children: "Choose one or multiple files, rearrange them and add their names to the Excel sheet.",
    })), delay(() => append(singleton_1(uploadButton(model, dispatch, inputId)), delay(() => (!equals(model.FilePickerState.FileNames, empty()) ? append(singleton_1(fileSortElements(model, dispatch)), delay(() => append(singleton_1(FileNameTable_table(model, dispatch)), delay(() => singleton_1(insertButton(model, dispatch)))))) : empty_1()))))))));
}

export function filePickerComponent(model, dispatch) {
    const elms = ofArray([pageHeader("File Picker"), createElement("label", {
        className: "label",
        children: "Select files from your computer and insert their names into Excel",
    }), fileContainer(model, dispatch, "filePicker_OnFilePickerMainFunc")]);
    return createElement("div", {
        className: "content",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

//# sourceMappingURL=FilePickerView.js.map
