import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, bool_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { map, empty, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { QuickAccessButton__toReactElement, QuickAccessButton_create_Z9F8EBC5 } from "../SharedComponents/QuickAccessButton.js";
import { createElement } from "react";
import React from "react";
import * as react from "react";
import { singleton as singleton_1, ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Msg } from "../States/SpreadsheetInterface.js";
import { Msg as Msg_1 } from "../Messages.js";
import { FillHiddenColsState__get_toReadableString, Msg as Msg_2 } from "../OfficeInterop/OfficeInteropState.js";
import { WindowSize, RequestBuildingBlockInfoStates__get_toStringMsg } from "../Model.js";
import { equals, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { join } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { keyValueList } from "../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { useFeliz_React__React_useState_Static_1505 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { Route } from "../Routing.js";
import { defaultOf } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpdesk_get_Url } from "../../Shared/URLs.js";

class NavbarState extends Record {
    constructor(BurgerActive, QuickAccessActive) {
        super();
        this.BurgerActive = BurgerActive;
        this.QuickAccessActive = QuickAccessActive;
    }
}

function NavbarState_$reflection() {
    return record_type("SidebarComponents.Navbar.NavbarState", [], NavbarState, () => [["BurgerActive", bool_type], ["QuickAccessActive", bool_type]]);
}

function NavbarState_get_init() {
    return new NavbarState(false, false);
}

function shortCutIconList(model, dispatch) {
    return toList(delay(() => append(singleton(QuickAccessButton_create_Z9F8EBC5("Create Annotation Table", ofArray([createElement("i", {
        className: "fa-solid fa-plus",
    }), createElement("i", {
        className: "fa-solid fa-table",
    })]), (e) => {
        e.preventDefault();
        dispatch(new Msg_1(17, [new Msg(1, [e.metaKey ? true : e.ctrlKey])]));
    })), delay(() => {
        let matchValue;
        return append((matchValue = model.PersistentStorageState.Host, (matchValue != null) ? ((matchValue.tag === 1) ? singleton(QuickAccessButton_create_Z9F8EBC5("Autoformat Table", singleton_1(createElement("i", {
            className: "fa-solid fa-rotate",
        })), (e_1) => {
            e_1.preventDefault();
            dispatch(new Msg_1(6, [new Msg_2(9, [!(e_1.metaKey ? true : e_1.ctrlKey)])]));
        })) : (empty())) : (empty())), delay(() => {
            let children;
            return append(singleton(QuickAccessButton_create_Z9F8EBC5("Update Ontology Terms", ofArray([createElement("i", {
                className: "fa-solid fa-spell-check",
            }), (children = [FillHiddenColsState__get_toReadableString(model.ExcelState.FillHiddenColsStateStore)], react.createElement("span", {}, ...children)), createElement("i", {
                className: "fa-solid fa-pen",
            })]), (_arg) => {
                dispatch(new Msg_1(17, [new Msg(12, [])]));
            })), delay(() => append(singleton(QuickAccessButton_create_Z9F8EBC5("Remove Building Block", ofArray([createElement("i", {
                className: "fa-solid fa-minus pr-1",
            }), createElement("i", {
                className: "fa-solid fa-table-columns",
            })]), (_arg_1) => {
                dispatch(new Msg_1(17, [new Msg(2, [])]));
            })), delay(() => {
                let children_2;
                return singleton(QuickAccessButton_create_Z9F8EBC5("Get Building Block Information", ofArray([createElement("i", {
                    className: "fa-solid fa-question pr-1",
                }), (children_2 = [RequestBuildingBlockInfoStates__get_toStringMsg(model.BuildingBlockDetailsState.CurrentRequestState)], react.createElement("span", {}, ...children_2)), createElement("i", {
                    className: "fa-solid fa-table-columns",
                })]), (_arg_2) => {
                    dispatch(new Msg_1(17, [new Msg(6, [])]));
                }));
            }))));
        }));
    }))));
}

function navbarShortCutIconList(model, dispatch) {
    return toList(delay(() => map(QuickAccessButton__toReactElement, shortCutIconList(model, dispatch))));
}

function quickAccessDropdownElement(model, dispatch, state, setState, isSndNavbar) {
    let elems_1, props_7, children_2, props_3, children;
    return createElement("div", createObj(Helpers_combineClasses("navbar-item", ofArray([["onClick", (_arg) => {
        setState(new NavbarState(state.BurgerActive, !state.QuickAccessActive));
    }], ["style", createObj(toList(delay(() => append(singleton(["padding", 0]), delay(() => (isSndNavbar ? singleton(["marginLeft", "auto"]) : empty()))))))], ["title", state.QuickAccessActive ? "Close quick access" : "Open quick access"], (elems_1 = [(props_7 = [["style", {
        width: "100%",
        height: "100%",
        position: "relative",
    }]], (children_2 = [createElement("a", createObj(Helpers_combineClasses("button", ofArray([["style", createObj(toList(delay(() => append(singleton(["backgroundColor", "transparent"]), delay(() => append(singleton(["height", 100 + "%"]), delay(() => (state.QuickAccessActive ? singleton(["color", "#FFC000"]) : empty()))))))))], ["className", "is-white"], ["className", "is-inverted"], ["children", (props_3 = [["style", {
        display: "inline-flex",
        position: "relative",
        justifyContent: "center",
    }]], (children = [createElement("i", {
        style: {
            position: "absolute",
            display: "block",
            transition: "opacity 0.25s, transform 0.25s",
            opacity: state.QuickAccessActive ? 1 : 0,
            transform: join(" ", state.QuickAccessActive ? singleton_1(("rotate(" + -180) + "deg)") : singleton_1(("rotate(" + 0) + "deg)")),
        },
        className: "fa-solid fa-times",
    }), createElement("i", {
        style: {
            position: "absolute",
            display: "block",
            transition: "opacity 0.25s, transform 0.25s",
            opacity: state.QuickAccessActive ? 0 : 1,
        },
        className: "fa-solid fa-ellipsis",
    }), createElement("i", {
        style: {
            display: "block",
            opacity: 0,
        },
        className: "fa-solid fa-ellipsis",
    })], react.createElement("div", keyValueList(props_3, 1), ...children)))]]))))], react.createElement("div", keyValueList(props_7, 1), ...children_2)))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

function quickAccessListElement(model, dispatch) {
    const props = [["style", {
        display: "flex",
        flexDirection: "row",
    }]];
    const children = toList(delay(() => navbarShortCutIconList(model, dispatch)));
    return react.createElement("div", keyValueList(props, 1), ...children);
}

export function NavbarComponent(navbarComponentInputProps) {
    let elems_10;
    const sidebarsize = navbarComponentInputProps.sidebarsize;
    const dispatch = navbarComponentInputProps.dispatch;
    const model = navbarComponentInputProps.model;
    const patternInput = useFeliz_React__React_useState_Static_1505(NavbarState_get_init());
    const state = patternInput[0];
    const setState = patternInput[1];
    return createElement("nav", createObj(Helpers_combineClasses("navbar", ofArray([["className", "myNavbarSticky"], ["id", "swate-mainNavbar"], ["role", join(" ", ["navigation"])], ["aria-label", "main navigation"], ["style", {
        flexWrap: "wrap",
    }], (elems_10 = toList(delay(() => {
        let elems_7, elems_3, elems_5, elems_4;
        return append(singleton(createElement("div", createObj(ofArray([["style", {
            flexBasis: 100 + "%",
        }], (elems_7 = [createElement("div", createObj(Helpers_combineClasses("navbar-brand", ofArray([["style", {
            width: 100 + "%",
        }], (elems_3 = toList(delay(() => append(singleton(createElement("div", createObj(Helpers_combineClasses("navbar-item", toList(delay(() => append(singleton(["id", "logo"]), delay(() => append(singleton(["onClick", (_arg) => {
            dispatch(new Msg_1(19, [new Route(1, [])]));
        }]), delay(() => append(singleton(["style", {
            width: 100,
            cursor: "pointer",
            padding: (0 + "px ") + (0.4 + "rem"),
        }]), delay(() => {
            let elms;
            const path = model.PageState.IsExpert ? "_e" : "";
            return singleton(["children", (elms = singleton_1(createElement("img", {
                style: {
                    maxHeight: 100 + "%",
                },
                src: `assets\\Swate_logo_for_excel${path}.svg`,
            })), createElement("figure", {
                className: "image",
                children: Interop_reactApi.Children.toArray(Array.from(elms)),
            }))]);
        })))))))))))), delay(() => {
            let matchValue;
            return append((matchValue = model.PersistentStorageState.Host, (sidebarsize.tag === 0) ? ((matchValue != null) ? ((matchValue.tag === 1) ? singleton(quickAccessDropdownElement(model, dispatch, state, setState, false)) : singleton(defaultOf())) : singleton(defaultOf())) : ((matchValue != null) ? ((matchValue.tag === 1) ? singleton(quickAccessListElement(model, dispatch)) : singleton(defaultOf())) : singleton(defaultOf()))), delay(() => singleton(createElement("a", createObj(Helpers_combineClasses("navbar-burger", toList(delay(() => append(state.BurgerActive ? singleton(["className", "is-active"]) : empty(), delay(() => append(singleton(["onClick", (_arg_1) => {
                setState(new NavbarState(!state.BurgerActive, state.QuickAccessActive));
            }]), delay(() => append(singleton(["className", "has-text-white"]), delay(() => append(singleton(["role", join(" ", ["button"])]), delay(() => append(singleton(["aria-label", "menu"]), delay(() => append(singleton(["aria-expanded", false]), delay(() => append(singleton(["style", {
                display: "block",
            }]), delay(() => {
                let elems_2;
                return singleton((elems_2 = [react.createElement("span", {
                    "aria-hidden": true,
                }), react.createElement("span", {
                    "aria-hidden": true,
                }), react.createElement("span", {
                    "aria-hidden": true,
                })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))]));
            }))))))))))))))))))))));
        })))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])])))), createElement("div", createObj(Helpers_combineClasses("navbar-menu", ofArray([["style", createObj(toList(delay(() => (state.BurgerActive ? singleton(["display", "block"]) : empty()))))], ["id", "navbarMenu"], ["className", state.BurgerActive ? "navbar-menu is-active" : "navbar-menu"], ["children", createElement("div", createObj(Helpers_combineClasses("navbar-dropdown", ofArray([["style", createObj(toList(delay(() => (state.BurgerActive ? singleton(["display", "block"]) : empty()))))], (elems_5 = [createElement("a", createObj(Helpers_combineClasses("navbar-item", ofArray([["href", "https://twitter.com/nfdi4plants"], ["target", "_Blank"], (elems_4 = [createElement("span", {
            children: ["News "],
        }), createElement("i", {
            className: "fa-brands fa-twitter",
            style: {
                color: "#1DA1F2",
                marginLeft: 2,
            },
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])])))), createElement("a", createObj(Helpers_combineClasses("navbar-item", ofArray([["href", "https://nfdi4plants.org/nfdi4plants.knowledgebase/docs/implementation/SwateManual/index.html"], ["target", "_Blank"], ["children", "How to use"]])))), createElement("a", createObj(Helpers_combineClasses("navbar-item", ofArray([["href", Helpdesk_get_Url()], ["target", "_Blank"], ["children", "Contact us!"]])))), createElement("a", createObj(Helpers_combineClasses("navbar-item", ofArray([["onClick", (_arg_2) => {
            setState(new NavbarState(!state.BurgerActive, state.QuickAccessActive));
            dispatch(new Msg_1(19, [new Route(11, [])]));
        }], ["children", "Settings"]])))), createElement("a", createObj(Helpers_combineClasses("navbar-item", ofArray([["onClick", (e) => {
            setState(new NavbarState(!state.BurgerActive, state.QuickAccessActive));
            dispatch(new Msg_1(19, [new Route(10, [])]));
        }], ["children", "Activity Log"]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])]))))]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_7))])])))), delay(() => {
            let elems_8;
            return (state.QuickAccessActive && equals(sidebarsize, new WindowSize(0, []))) ? singleton(createElement("div", createObj(Helpers_combineClasses("navbar-brand", ofArray([["style", {
                flexGrow: 1,
                display: "flex",
            }], (elems_8 = navbarShortCutIconList(model, dispatch), ["children", Interop_reactApi.Children.toArray(Array.from(elems_8))])]))))) : empty();
        }));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_10))])]))));
}

//# sourceMappingURL=Navbar.js.map
