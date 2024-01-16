import { createElement } from "react";
import * as react from "react";
import { equalArrays, defaultOf, createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { empty, singleton, append, map, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { rangeDouble } from "../../../../fable_modules/fable-library.4.9.0/Range.js";
import { singleton as singleton_1, cons, ofArray, item, length } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { DevMsg, curry, Msg, Protocol_Msg } from "../../Messages.js";
import { Cmd_none } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { equalsWith } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { value as value_36, some } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { pageHeader, mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";
import { keyValueList } from "../../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { Route } from "../../Routing.js";
import { toString } from "../../../../fable_modules/fable-library.4.9.0/Types.js";

export function TemplateFromJsonFile_fileUploadButton(model, dispatch) {
    const elms = ofArray([createElement("input", createObj(cons(["type", "file"], Helpers_combineClasses("file-input", ofArray([["id", "UploadFiles_ElementId"], ["type", "file"], ["style", {
        display: "none",
    }], ["onChange", (ev_1) => {
        const fileList_1 = ev_1.target.files;
        if (!(fileList_1 == null)) {
            const fileList = toList(delay(() => map((i) => fileList_1.item(i), rangeDouble(0, 1, fileList_1.length - 1))));
            if (length(fileList) > 0) {
                let file;
                const f = item(0, fileList);
                file = f.slice();
                const reader = new FileReader();
                reader.onload = ((evt) => {
                    dispatch(new Msg(10, [new Protocol_Msg(0, [evt.target.result])]));
                });
                reader.onerror = ((evt_1) => {
                    dispatch(new Msg(3, [curry((tupledArg) => (new DevMsg(1, [tupledArg[0], tupledArg[1]])), Cmd_none(), ["Error", evt_1.target.value])]));
                });
                reader.readAsArrayBuffer(file);
            }
            const picker = document.getElementById("UploadFiles_ElementId");
            picker.value = defaultOf();
        }
    }]]))))), createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-info"], ["className", "is-fullwidth"], ["onClick", (e) => {
        e.preventDefault();
        const getUploadElement = document.getElementById("UploadFiles_ElementId");
        getUploadElement.click();
    }], ["children", "Upload protocol"]]))))]);
    return createElement("label", {
        className: "label",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function TemplateFromJsonFile_fileUploadEle(model, dispatch) {
    let elems_2;
    const hasData = !equalsWith(equalArrays, model.ProtocolState.UploadedFileParsed, new Array(0));
    return createElement("div", createObj(Helpers_combineClasses("columns", ofArray([["className", "is-mobile"], (elems_2 = toList(delay(() => {
        let elms;
        return append(singleton((elms = singleton_1(TemplateFromJsonFile_fileUploadButton(model, dispatch)), createElement("div", {
            className: "column",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))), delay(() => (hasData ? singleton(createElement("div", createObj(Helpers_combineClasses("column", ofArray([["className", "is-narrow"], ["children", createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e) => {
            dispatch(new Msg(10, [new Protocol_Msg(2, [])]));
        }], ["className", "is-danger"], ["children", createElement("i", {
            className: "fa-solid fa-times",
        })]]))))]]))))) : empty())));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

export function TemplateFromJsonFile_importToTableEle(model, dispatch) {
    const hasData = !equalsWith(equalArrays, model.ProtocolState.UploadedFileParsed, new Array(0));
    return createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], ["children", createElement("div", createObj(Helpers_combineClasses("control", ofArray([["className", "is-expanded"], ["children", createElement("a", createObj(Helpers_combineClasses("button", toList(delay(() => append(singleton(["className", "is-info"]), delay(() => append(hasData ? singleton(["className", "is-active"]) : append(singleton(["className", "is-danger"]), delay(() => singleton(["disabled", true]))), delay(() => append(singleton(["className", "is-fullwidth"]), delay(() => append(singleton(["onClick", (_arg) => {
        window.alert(some("\'SpreadsheetInterface.ImportFile\' is not implemented"));
    }]), delay(() => singleton(["children", "Insert json"]))))))))))))))]]))))]]))));
}

export function TemplateFromJsonFile_protocolInsertElement(model, dispatch) {
    let elms_1, elms, props_2, elms_2;
    return mainFunctionContainer([(elms_1 = singleton_1((elms = ofArray([react.createElement("b", {}, "Insert tables via ISA-JSON files."), " You can use Swate.Experts to create these files from existing Swate tables. ", (props_2 = [["style", {
        color: "#C21F3A",
    }]], react.createElement("span", keyValueList(props_2, 1), "Only missing building blocks will be added."))]), createElement("p", {
        className: "help",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), (elms_2 = singleton_1(TemplateFromJsonFile_fileUploadEle(model, dispatch)), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    })), TemplateFromJsonFile_importToTableEle(model, dispatch)]);
}

export function TemplateFromDB_toProtocolSearchElement(model, dispatch) {
    return createElement("span", createObj(Helpers_combineClasses("button", ofArray([["onClick", (_arg) => {
        dispatch(new Msg(19, [new Route(6, [])]));
    }], ["className", "is-info"], ["className", "is-fullwidth"], ["style", {
        margin: ((1 + "rem") + " ") + (0 + "px"),
    }], ["children", "Browse database"]]))));
}

export function TemplateFromDB_addFromDBToTableButton(model, dispatch) {
    let elems_5;
    return createElement("div", createObj(Helpers_combineClasses("columns", ofArray([["className", "is-mobile"], (elems_5 = toList(delay(() => {
        let elms_2, elms_1, elms;
        return append(singleton((elms_2 = singleton_1((elms_1 = singleton_1((elms = singleton_1(createElement("a", createObj(Helpers_combineClasses("button", toList(delay(() => append(singleton(["className", "is-success"]), delay(() => append((model.ProtocolState.ProtocolSelected != null) ? singleton(["className", "is-active"]) : append(singleton(["className", "is-danger"]), delay(() => singleton(["disabled", true]))), delay(() => append(singleton(["className", "is-fullwidth"]), delay(() => append(singleton(["onClick", (e) => {
            const p = value_36(model.ProtocolState.ProtocolSelected);
            window.alert(some("Protocol AddAnnotationBlocks is not implemented. Replace template logic first"));
        }]), delay(() => singleton(["children", "Add template"]))))))))))))))), createElement("div", {
            className: "control",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))), createElement("div", {
            className: "field",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        }))), createElement("div", {
            className: "column",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))), delay(() => ((model.ProtocolState.ProtocolSelected != null) ? singleton(createElement("div", createObj(Helpers_combineClasses("column", ofArray([["className", "is-narrow"], ["children", createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e_1) => {
            dispatch(new Msg(10, [new Protocol_Msg(7, [])]));
        }], ["className", "is-danger"], ["children", createElement("i", {
            className: "fa-solid fa-times",
        })]]))))]]))))) : empty())));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])]))));
}

export function TemplateFromDB_displaySelectedProtocolEle(model, dispatch) {
    let props_14, children_12, elems, children_2, children, children_10;
    return ofArray([(props_14 = [["style", {
        overflowX: "auto",
        marginBottom: "1rem",
    }]], (children_12 = [createElement("table", createObj(Helpers_combineClasses("table", ofArray([["className", "is-fullwidth"], ["className", "is-bordered"], (elems = [(children_2 = [(children = ofArray([createElement("th", {
        children: ["Column"],
    }), createElement("th", {
        children: ["Column TAN"],
    })]), createElement("tr", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    }))], react.createElement("thead", {}, ...children_2)), (children_10 = toList(delay(() => map((column) => {
        let children_4, children_6;
        const children_8 = ofArray([(children_4 = [toString(column.Header)], react.createElement("td", {}, ...children_4)), (children_6 = [column.Header.IsTermColumn ? column.Header.ToTerm().TermAccessionShort : "-"], react.createElement("td", {}, ...children_6))]);
        return createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_8)),
        });
    }, value_36(model.ProtocolState.ProtocolSelected).Table.Columns))), react.createElement("tbody", {}, ...children_10))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))], react.createElement("div", keyValueList(props_14, 1), ...children_12))), TemplateFromDB_addFromDBToTableButton(model, dispatch)]);
}

export function TemplateFromDB_showDatabaseProtocolTemplate(model, dispatch) {
    return mainFunctionContainer(toList(delay(() => {
        let elms_1, elms, props_2;
        return append(singleton((elms_1 = singleton_1((elms = ofArray([react.createElement("b", {}, "Search the database for templates."), " The building blocks from these templates can be inserted into the Swate table. ", (props_2 = [["style", {
            color: "#C21F3A",
        }]], react.createElement("span", keyValueList(props_2, 1), "Only missing building blocks will be added."))]), createElement("p", {
            className: "help",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))), createElement("div", {
            className: "field",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        }))), delay(() => {
            let elms_2;
            return append(singleton((elms_2 = singleton_1(TemplateFromDB_toProtocolSearchElement(model, dispatch)), createElement("div", {
                className: "field",
                children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
            }))), delay(() => {
                let elms_3;
                return append(singleton((elms_3 = singleton_1(TemplateFromDB_addFromDBToTableButton(model, dispatch)), createElement("div", {
                    className: "field",
                    children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
                }))), delay(() => {
                    let elms_4;
                    return (model.ProtocolState.ProtocolSelected != null) ? singleton((elms_4 = toList(delay(() => TemplateFromDB_displaySelectedProtocolEle(model, dispatch))), createElement("div", {
                        className: "field",
                        children: Interop_reactApi.Children.toArray(Array.from(elms_4)),
                    }))) : empty();
                }));
            }));
        }));
    })));
}

export function fileUploadViewComponent(model, dispatch) {
    const children = [pageHeader("Templates"), createElement("label", {
        className: "label",
        children: "Add template from database.",
    }), TemplateFromDB_showDatabaseProtocolTemplate(model, dispatch), createElement("label", {
        className: "label",
        children: "Add template(s) from file.",
    }), TemplateFromJsonFile_protocolInsertElement(model, dispatch)];
    return react.createElement("div", {
        onSubmit: (e) => {
            e.preventDefault();
        },
        onKeyDown: (k) => {
            if (k.key === "Enter") {
                k.preventDefault();
            }
        },
    }, ...children);
}

//# sourceMappingURL=ProtocolView.js.map
