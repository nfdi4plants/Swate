import { Msg } from "../States/Spreadsheet.js";
import { DevMsg, curry, Msg as Msg_1 } from "../Messages.js";
import { max } from "../../../fable_modules/fable-library.4.9.0/Double.js";
import { createObj, defaultOf, equals } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { createElement } from "react";
import React from "react";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Cmd_none } from "../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { empty as empty_1, singleton as singleton_1, ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { empty, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { ArcAssay, ArcStudy, ArcInvestigation } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTypes.fs.js";
import { ARCtrlHelper_ArcFiles } from "../../Shared/ARCtrl.Helper.js";
import { Template } from "../../../fable_modules/ARCtrl.1.0.4/Templates/Template.fs.js";
import { ArcTable } from "../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTable.fs.js";
import { newGuid } from "../../../fable_modules/fable-library.4.9.0/Guid.js";
import { now } from "../../../fable_modules/fable-library.4.9.0/Date.js";
import { useFeliz_React__React_useState_Static_1505 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";

const buttonStyle = ["style", {
    margin: 1.5 + "rem",
}];

let UploadHandler_styleCounter = 0;

function UploadHandler_updateMsg(r) {
    return new Msg_1(15, [new Msg(20, [r])]);
}

function UploadHandler_setActive_DropArea() {
    UploadHandler_styleCounter = ((UploadHandler_styleCounter + 1) | 0);
    const ele = document.getElementById("droparea");
    ele.style.border = (`2px solid ${"#1FC2A7"}`);
}

function UploadHandler_setInActive_DropArea() {
    UploadHandler_styleCounter = (max(UploadHandler_styleCounter - 1, 0) | 0);
    if (UploadHandler_styleCounter <= 0) {
        const ele = document.getElementById("droparea");
        ele.style.border = "unset";
    }
}

function UploadHandler_ondrop(dispatch, e) {
    e.preventDefault();
    if (!equals(e.dataTransfer.items, defaultOf())) {
        const item = e.dataTransfer.items[0];
        if (item.kind === "file") {
            UploadHandler_setInActive_DropArea();
            UploadHandler_styleCounter = 0;
            const file = item.getAsFile();
            const reader = new FileReader();
            reader.onload = ((_arg) => {
                dispatch(UploadHandler_updateMsg(reader.result));
            });
            reader.readAsArrayBuffer(file);
        }
    }
}

function uploadNewTable(dispatch) {
    let elems_1, elems;
    return createElement("label", createObj(Helpers_combineClasses("label", ofArray([["style", {
        fontWeight: "normal",
    }], (elems_1 = [createElement("input", {
        id: "UploadFiles_MainWindowInit",
        type: "file",
        style: {
            display: "none",
        },
        onChange: (ev) => {
            const fileList = ev.target.files;
            if (fileList.length > 0) {
                let file;
                const f = fileList.item(0);
                file = f.slice();
                const reader = new FileReader();
                reader.onload = ((evt) => {
                    dispatch(new Msg_1(15, [new Msg(20, [evt.target.result])]));
                });
                reader.onerror = ((evt_1) => {
                    dispatch(new Msg_1(3, [curry((tupledArg) => (new DevMsg(1, [tupledArg[0], tupledArg[1]])), Cmd_none(), ["Error", evt_1.Value])]));
                });
                reader.readAsArrayBuffer(file);
            }
            const picker = document.getElementById("UploadFiles_MainWindowInit");
            picker.value = defaultOf();
        },
    }), createElement("span", createObj(Helpers_combineClasses("button", ofArray([["className", "is-large"], buttonStyle, ["className", "is-info"], ["onClick", (e) => {
        e.preventDefault();
        const getUploadElement = document.getElementById("UploadFiles_MainWindowInit");
        getUploadElement.click();
    }], (elems = [createElement("div", {
        children: ["Import File"],
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

function createNewTable(isActive, toggle, dispatch) {
    return createElement("div", createObj(Helpers_combineClasses("dropdown", toList(delay(() => append(isActive ? singleton(["className", "is-active"]) : empty(), delay(() => append(singleton(buttonStyle), delay(() => {
        let elems_4, elms, elems, elms_2, elms_1;
        return singleton((elems_4 = [(elms = singleton_1(createElement("span", createObj(Helpers_combineClasses("button", ofArray([["className", "is-large"], ["className", "is-link"], ["onClick", toggle], (elems = [createElement("div", {
            children: ["New File"],
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))), createElement("div", {
            className: "dropdown-trigger",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        })), (elms_2 = singleton_1((elms_1 = ofArray([createElement("a", createObj(Helpers_combineClasses("dropdown-item", ofArray([["onClick", (_arg) => {
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [ArcInvestigation.init("New Investigation")])])]));
        }], ["children", "Investigation"]])))), createElement("a", createObj(Helpers_combineClasses("dropdown-item", ofArray([["onClick", (_arg_1) => {
            const s = ArcStudy.init("New Study");
            const newTable = s.InitTable("New Study Table");
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(2, [s, empty_1()])])]));
        }], ["children", "Study"]])))), createElement("a", createObj(Helpers_combineClasses("dropdown-item", ofArray([["onClick", (_arg_2) => {
            const a = ArcAssay.init("New Assay");
            const newTable_1 = a.InitTable("New Assay Table");
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(3, [a])])]));
        }], ["children", "Assay"]])))), createElement("hr", createObj(Helpers_combineClasses("dropdown-divider", empty_1()))), createElement("a", createObj(Helpers_combineClasses("dropdown-item", ofArray([["onClick", (_arg_3) => {
            const template = Template.init("New Template");
            const table = ArcTable.init("New Table");
            template.Table = table;
            template.Version = "0.0.0";
            template.Id = newGuid();
            template.LastUpdated = now();
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
        }], ["children", "Template"]]))))]), createElement("div", {
            className: "dropdown-content",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        }))), createElement("div", {
            className: "dropdown-menu",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))]));
    })))))))));
}

export function Main(args) {
    let elems_1, elems;
    const patternInput = useFeliz_React__React_useState_Static_1505(true);
    const isActive = patternInput[0];
    return createElement("div", createObj(ofArray([["id", "droparea"], ["onDragEnter", (e) => {
        e.preventDefault();
        if (!equals(e.dataTransfer.items, defaultOf())) {
            const item = e.dataTransfer.items[0];
            if (item.kind === "file") {
                UploadHandler_setActive_DropArea();
            }
        }
    }], ["onDragLeave", (e_1) => {
        UploadHandler_setInActive_DropArea();
    }], ["onDragOver", (e_2) => {
        e_2.preventDefault();
    }], ["onDrop", (e_3) => {
        UploadHandler_ondrop(args.dispatch, e_3);
    }], ["style", {
        height: "inherit",
        width: "inherit",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
    }], (elems_1 = [createElement("div", createObj(ofArray([["style", {
        display: "flex",
        justifyContent: "space-between",
    }], (elems = [createNewTable(isActive, (_arg) => {
        patternInput[1](!isActive);
    }, args.dispatch), uploadNewTable(args.dispatch)], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])));
}

//# sourceMappingURL=NoTablesElement.js.map
