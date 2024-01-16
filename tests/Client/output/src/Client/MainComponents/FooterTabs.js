import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, string_type, bool_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { some, defaultArg } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { createElement } from "react";
import React from "react";
import { equals, curry2, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { empty as empty_1, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { cons, ofArray, empty, singleton as singleton_1 } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { FooterReorderData_create, FooterReorderData__toJson, FooterReorderData_ofJson_Z721C83C5 } from "../Spreadsheet/Types.js";
import { ActiveView, Msg } from "../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../Messages.js";
import { useFeliz_React__React_useState_Static_1505 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { renderModal } from "../Modals/Controller.js";
import { Msg as Msg_2 } from "../States/SpreadsheetInterface.js";

class FooterTab extends Record {
    constructor(IsEditable, IsDraggedOver, Name) {
        super();
        this.IsEditable = IsEditable;
        this.IsDraggedOver = IsDraggedOver;
        this.Name = Name;
    }
}

function FooterTab_$reflection() {
    return record_type("MainComponents.FooterTabs.FooterTab", [], FooterTab, () => [["IsEditable", bool_type], ["IsDraggedOver", bool_type], ["Name", string_type]]);
}

function FooterTab_init_6DFDD678(name) {
    return new FooterTab(false, false, defaultArg(name, ""));
}

function popup(x, y, renameMsg, deleteMsg, rmv) {
    let elems, children_2, tupledArg_1, tupledArg_2;
    const rmv_element = createElement("div", {
        onClick: rmv,
        onContextMenu: (e) => {
            e.preventDefault();
            rmv(e);
        },
        style: {
            position: "fixed",
            backgroundColor: "transparent",
            left: 0,
            top: 0,
            right: 0,
            bottom: 0,
            display: "block",
        },
    });
    const button = (tupledArg) => {
        const children = singleton_1(createElement("button", createObj(Helpers_combineClasses("button", toList(delay(() => append(singleton(["style", {
            borderRadius: 0,
        }]), delay(() => append(singleton(["onClick", tupledArg[1]]), delay(() => append(singleton(["className", "is-fullwidth"]), delay(() => append(singleton(["className", "is-small"]), delay(() => append(tupledArg[2], delay(() => singleton(["children", tupledArg[0]])))))))))))))))));
        return createElement("li", {
            children: Interop_reactApi.Children.toArray(Array.from(children)),
        });
    };
    return createElement("div", createObj(ofArray([["style", createObj(toList(delay(() => append(singleton(["backgroundColor", "white"]), delay(() => append(singleton(["position", "absolute"]), delay(() => append(singleton(["left", x]), delay(() => append(singleton(["top", y - 53]), delay(() => append(singleton(["zIndex", 20]), delay(() => append(singleton(["width", 70]), delay(() => singleton(["height", 53]))))))))))))))))], (elems = [rmv_element, (children_2 = ofArray([(tupledArg_1 = ["Delete", curry2(deleteMsg)((arg_3) => {
        rmv(arg_3);
    }), singleton_1(["className", "is-danger"])], button([tupledArg_1[0], tupledArg_1[1], tupledArg_1[2]])), (tupledArg_2 = ["Rename", curry2(renameMsg)((arg_7) => {
        rmv(arg_7);
    }), empty()], button([tupledArg_2[0], tupledArg_2[1], tupledArg_2[2]]))]), createElement("ul", {
        children: Interop_reactApi.Children.toArray(Array.from(children_2)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])));
}

function drag_preventdefault(e) {
    e.preventDefault();
    e.stopPropagation();
}

function drop_handler(eleOrder, state, setState, dispatch, e) {
    const getData = FooterReorderData_ofJson_Z721C83C5(e.dataTransfer.getData("text"));
    setState(new FooterTab(state.IsEditable, false, state.Name));
    if (getData.tag === 0) {
        const data_1 = getData.fields[0];
        console.log(some(data_1));
        dispatch(new Msg_1(15, [new Msg(6, [data_1.OriginOrder, eleOrder])]));
    }
}

function dragenter_handler(state, setState, e) {
    e.preventDefault();
    e.stopPropagation();
    return setState(new FooterTab(state.IsEditable, true, state.Name));
}

function dragleave_handler(state, setState, e) {
    e.preventDefault();
    e.stopPropagation();
    return setState(new FooterTab(state.IsEditable, false, state.Name));
}

export function Main(input) {
    const index = input.index | 0;
    const table = input.tables.GetTableAt(index);
    const patternInput = useFeliz_React__React_useState_Static_1505(FooterTab_init_6DFDD678(table.Name));
    const state = patternInput[0];
    const setState = patternInput[1];
    const dispatch = input.dispatch;
    const id = `ReorderMe_${index}_${table.Name}`;
    return createElement("li", createObj(Helpers_combineClasses("", toList(delay(() => append(state.IsDraggedOver ? singleton(["className", "dragover-footertab"]) : empty_1(), delay(() => append(singleton(["draggable", true]), delay(() => append(singleton(["onDrop", (e) => {
        drop_handler(index, state, setState, dispatch, e);
    }]), delay(() => append(singleton(["onDragLeave", (e_1) => {
        dragleave_handler(state, setState, e_1);
    }]), delay(() => append(singleton(["onDragStart", (e_2) => {
        e_2.dataTransfer.clearData();
        const dataJson = FooterReorderData__toJson(FooterReorderData_create(index, id));
        e_2.dataTransfer.setData("text", dataJson);
    }]), delay(() => append(singleton(["onDragEnter", (e_3) => {
        dragenter_handler(state, setState, e_3);
    }]), delay(() => append(singleton(["onDragOver", (e_4) => {
        drag_preventdefault(e_4);
    }]), delay(() => append(singleton(["style", {
        order: index,
    }]), delay(() => append(singleton(["key", id]), delay(() => append(singleton(["id", id]), delay(() => append(equals(input.model.SpreadsheetModel.ActiveView, new ActiveView(0, [index])) ? singleton(["className", "is-active"]) : empty_1(), delay(() => append(singleton(["onClick", (_arg) => {
        dispatch(new Msg_1(15, [new Msg(2, [new ActiveView(0, [index])])]));
    }]), delay(() => append(singleton(["onContextMenu", (e_5) => {
        e_5.stopPropagation();
        e_5.preventDefault();
        const mousePosition = [~~e_5.pageX, ~~e_5.pageY];
        renderModal(`popup_${mousePosition}`, (rmv_2) => popup(mousePosition[0], mousePosition[1], (rmv_1, e_7) => {
            rmv_1(e_7);
            setState(new FooterTab(true, state.IsDraggedOver, state.Name));
        }, (rmv, e_6) => {
            rmv(e_6);
            dispatch(new Msg_1(15, [new Msg(4, [index])]));
        }, rmv_2));
    }]), delay(() => append(singleton(["draggable", true]), delay(() => {
        let elems;
        return singleton((elems = toList(delay(() => {
            if (state.IsEditable) {
                const updateName = (e_8) => {
                    dispatch(new Msg_1(15, [new Msg(5, [index, state.Name])]));
                    setState(new FooterTab(false, state.IsDraggedOver, state.Name));
                };
                return singleton(createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", ofArray([["autoFocus", true], ["id", id + "input"], ["onChange", (ev) => {
                    setState(new FooterTab(state.IsEditable, state.IsDraggedOver, ev.target.value));
                }], ["onBlur", updateName], ["onKeyDown", (e_10) => {
                    const matchValue = e_10.which;
                    switch (matchValue) {
                        case 13: {
                            updateName(e_10);
                            break;
                        }
                        case 27: {
                            setState(new FooterTab(false, state.IsDraggedOver, table.Name));
                            break;
                        }
                        default:
                            0;
                    }
                }], ["defaultValue", table.Name]]))))));
            }
            else {
                return singleton(createElement("a", {
                    children: table.Name,
                }));
            }
        })), ["children", Interop_reactApi.Children.toArray(Array.from(elems))]));
    })))))))))))))))))))))))))))))))));
}

export function MainMetadata(input) {
    const nav = new ActiveView(1, []);
    return createElement("li", createObj(Helpers_combineClasses("", toList(delay(() => append(equals(input.model.SpreadsheetModel.ActiveView, nav) ? singleton(["className", "is-active"]) : empty_1(), delay(() => append(singleton(["key", "Metadata-Tab"]), delay(() => append(singleton(["id", "Metadata-Tab"]), delay(() => append(singleton(["onClick", (_arg) => {
        input.dispatch(new Msg_1(15, [new Msg(2, [nav])]));
    }]), delay(() => append(singleton(["style", {
        order: 0,
        height: 100 + "%",
        cursor: "pointer",
    }]), delay(() => {
        let elems;
        return singleton((elems = [createElement("a", {
            children: "Metadata",
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))]));
    })))))))))))))));
}

export function MainPlus(input) {
    const dispatch = input.dispatch;
    const patternInput = useFeliz_React__React_useState_Static_1505(FooterTab_init_6DFDD678());
    const state = patternInput[0];
    const setState = patternInput[1];
    return createElement("li", createObj(Helpers_combineClasses("", toList(delay(() => append(singleton(["key", "Add-Spreadsheet-Button"]), delay(() => append(singleton(["id", "Add-Spreadsheet-Button"]), delay(() => append(state.IsDraggedOver ? singleton(["className", "dragover-footertab"]) : empty_1(), delay(() => append(singleton(["onDragEnter", (e) => {
        dragenter_handler(state, setState, e);
    }]), delay(() => append(singleton(["onDragLeave", (e_1) => {
        dragleave_handler(state, setState, e_1);
    }]), delay(() => append(singleton(["onDragOver", (e_2) => {
        drag_preventdefault(e_2);
    }]), delay(() => append(singleton(["onDrop", (e_3) => {
        drop_handler(2147483647, state, setState, dispatch, e_3);
    }]), delay(() => append(singleton(["onClick", (e_4) => {
        dispatch(new Msg_1(17, [new Msg_2(1, [e_4.ctrlKey])]));
    }]), delay(() => append(singleton(["style", {
        order: 2147483647,
        height: 100 + "%",
        cursor: "pointer",
    }]), delay(() => {
        let elems_2, elems_1, elems;
        return singleton((elems_2 = [createElement("a", createObj(ofArray([["style", {
            height: "inherit",
            pointerEvents: "none",
        }], (elems_1 = [createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["className", "is-small"], (elems = [createElement("i", {
            className: "fa-solid fa-plus",
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))]));
    })))))))))))))))))))))));
}

//# sourceMappingURL=FooterTabs.js.map
