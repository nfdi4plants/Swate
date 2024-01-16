import { createElement } from "react";
import React from "react";
import { useFeliz_React__React_useState_Static_1505 } from "../../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { empty as empty_1, singleton, ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { BuildingBlock_DropdownPage, BuildingBlock_DropdownPage__get_toString, BuildingBlock_BuildingBlockUIState } from "../../Model.js";
import { empty, singleton as singleton_1, append, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { ARCtrl_ISA_CompositeHeader__CompositeHeader_get_OutputEmpty_Static, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_ComponentEmpty_Static, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_CharacteristicEmpty_Static, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_FactorEmpty_Static, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_ParameterEmpty_Static, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_InputEmpty_Static, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_AsButtonName, ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateDeepWith_Z331CE692 } from "../../../Shared/ARCtrl.Helper.js";
import { selectHeader } from "./Helper.js";
import { IOType, CompositeHeader } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeHeader.fs.js";
import { toString } from "../../../../fable_modules/fable-library.4.9.0/Types.js";

export function FreeTextInputElement(freeTextInputElementInputProps) {
    const onSubmit = freeTextInputElementInputProps.onSubmit;
    const patternInput = useFeliz_React__React_useState_Static_1505("");
    const children = ofArray([createElement("input", {
        onClick: (e) => {
            e.stopPropagation();
        },
        onChange: (ev) => {
            patternInput[1](ev.target.value);
        },
    }), createElement("button", {
        onClick: (e_1) => {
            e_1.stopPropagation();
            onSubmit(patternInput[0]);
        },
        children: "✅",
    })]);
    return createElement("span", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    });
}

const DropdownElements_itemTooltipStyle = ofArray([["fontSize", 1.1 + "rem"], ["paddingRight", 10 + "px"], ["textAlign", "center"], ["color", "#cc9a00"]]);

const DropdownElements_annotationsPrinciplesUrl = createElement("a", {
    href: "https://nfdi4plants.github.io/AnnotationPrinciples/",
    target: "_Blank",
    children: "info",
});

function DropdownElements_createSubBuildingBlockDropdownLink(state, setState, subpage) {
    let elems_1, children;
    return createElement("a", createObj(Helpers_combineClasses("dropdown-item", ofArray([["tabIndex", 0], ["onClick", (e) => {
        e.preventDefault();
        e.stopPropagation();
        setState(new BuildingBlock_BuildingBlockUIState(state.DropdownIsActive, subpage, state.BodyCellType));
    }], ["style", {
        paddingRight: 1 + "rem",
        display: "inline-flex",
        justifyContent: "space-between",
    }], (elems_1 = [createElement("span", {
        children: [BuildingBlock_DropdownPage__get_toString(subpage)],
    }), createElement("span", {
        style: {
            width: 20 + "px",
            alignSelf: "flex-end",
            lineHeight: 1.5 + "px",
            fontSize: 1.1 + "rem",
        },
        children: (children = singleton(createElement("i", {
            className: "fa-solid fa-arrow-right",
        })), createElement("span", {
            className: "icon",
            children: Interop_reactApi.Children.toArray(Array.from(children)),
        })),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

function DropdownElements_backToMainDropdownButton(state, setState) {
    let elems_2, elems_1, elms;
    return createElement("div", createObj(Helpers_combineClasses("dropdown-item", ofArray([["style", {
        textAlign: "right",
    }], (elems_2 = [createElement("button", createObj(Helpers_combineClasses("button", ofArray([["style", {
        float: "left",
        width: 20 + "px",
        height: 20 + "px",
        borderRadius: 4 + "px",
        border: "unset",
    }], ["onClick", (e) => {
        e.preventDefault();
        e.stopPropagation();
        setState(new BuildingBlock_BuildingBlockUIState(state.DropdownIsActive, new BuildingBlock_DropdownPage(0, []), state.BodyCellType));
    }], ["className", "is-inverted"], ["className", "is-black"], (elems_1 = [(elms = singleton(createElement("i", {
        className: "fa-solid fa-arrow-left",
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))), DropdownElements_annotationsPrinciplesUrl], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

function DropdownElements_createBuildingBlockDropdownItem(model, dispatch, uiState, setUiState, header) {
    const isDeepFreeText = (header.tag === 13) ? true : ((header.tag === 11) ? (header.fields[0].tag === 6) : ((header.tag === 12) && (header.fields[0].tag === 6)));
    return createElement("a", createObj(Helpers_combineClasses("dropdown-item", toList(delay(() => {
        let nextHeader;
        return append(!isDeepFreeText ? ((nextHeader = ((header.IsTermColumn && !header.IsFeaturedColumn) ? ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateDeepWith_Z331CE692(header, model.AddBuildingBlockState.Header) : header), append(singleton_1(["onClick", (e) => {
            e.stopPropagation();
            dispatch(selectHeader(uiState, setUiState, nextHeader));
        }]), delay(() => singleton_1(["onKeyDown", (k) => {
            if (~~k.which === 13) {
                dispatch(selectHeader(uiState, setUiState, nextHeader));
            }
        }]))))) : empty(), delay(() => {
            let elems;
            return singleton_1((elems = toList(delay(() => {
                let matchResult, io;
                switch (header.tag) {
                    case 0:
                    case 3:
                    case 2:
                    case 1: {
                        matchResult = 1;
                        break;
                    }
                    case 12: {
                        matchResult = 0;
                        io = header.fields[0];
                        break;
                    }
                    case 11: {
                        matchResult = 0;
                        io = header.fields[0];
                        break;
                    }
                    default:
                        matchResult = 2;
                }
                switch (matchResult) {
                    case 0: {
                        const matchValue = io;
                        if (matchValue.tag === 6) {
                            const ch = header.isOutput ? ((Item) => (new CompositeHeader(12, [Item]))) : ((Item_1) => (new CompositeHeader(11, [Item_1])));
                            return singleton_1(createElement(FreeTextInputElement, {
                                onSubmit: (v) => {
                                    dispatch(selectHeader(uiState, setUiState, ch(new IOType(6, [v]))));
                                },
                            }));
                        }
                        else {
                            return singleton_1(createElement("span", {
                                children: [toString(matchValue)],
                            }));
                        }
                    }
                    case 1:
                        return singleton_1(createElement("span", {
                            children: [ARCtrl_ISA_CompositeHeader__CompositeHeader_get_AsButtonName(header)],
                        }));
                    default:
                        return singleton_1(createElement("span", {
                            children: [toString(header)],
                        }));
                }
            })), ["children", Interop_reactApi.Children.toArray(Array.from(elems))]));
        }));
    })))));
}

function DropdownElements_dropdownContentMain(state, setState, model, dispatch) {
    return ofArray([DropdownElements_createSubBuildingBlockDropdownLink(state, setState, new BuildingBlock_DropdownPage(2, [[(Item) => (new CompositeHeader(11, [Item])), ARCtrl_ISA_CompositeHeader__CompositeHeader_get_AsButtonName(ARCtrl_ISA_CompositeHeader__CompositeHeader_get_InputEmpty_Static())]])), createElement("hr", createObj(Helpers_combineClasses("dropdown-divider", empty_1()))), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_ParameterEmpty_Static()), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_FactorEmpty_Static()), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_CharacteristicEmpty_Static()), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, ARCtrl_ISA_CompositeHeader__CompositeHeader_get_ComponentEmpty_Static()), DropdownElements_createSubBuildingBlockDropdownLink(state, setState, new BuildingBlock_DropdownPage(1, [])), createElement("hr", createObj(Helpers_combineClasses("dropdown-divider", empty_1()))), DropdownElements_createSubBuildingBlockDropdownLink(state, setState, new BuildingBlock_DropdownPage(2, [[(Item_1) => (new CompositeHeader(12, [Item_1])), ARCtrl_ISA_CompositeHeader__CompositeHeader_get_AsButtonName(ARCtrl_ISA_CompositeHeader__CompositeHeader_get_OutputEmpty_Static())]])), createElement("div", createObj(Helpers_combineClasses("dropdown-item", ofArray([["style", {
        textAlign: "right",
    }], ["children", DropdownElements_annotationsPrinciplesUrl]]))))]);
}

function DropdownElements_dropdownContentProtocolTypeColumns(state, setState, state_search, setState_search, model, dispatch) {
    return ofArray([DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, new CompositeHeader(10, [])), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, new CompositeHeader(9, [])), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, new CompositeHeader(5, [])), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, new CompositeHeader(8, [])), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, new CompositeHeader(4, [])), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, new CompositeHeader(6, [])), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, new CompositeHeader(7, [])), DropdownElements_backToMainDropdownButton(state, setState)]);
}

function DropdownElements_dropdownContentIOTypeColumns(createHeaderFunc, state, setState, model, dispatch) {
    return ofArray([DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, createHeaderFunc(new IOType(0, []))), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, createHeaderFunc(new IOType(1, []))), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, createHeaderFunc(new IOType(5, []))), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, createHeaderFunc(new IOType(2, []))), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, createHeaderFunc(new IOType(3, []))), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, createHeaderFunc(new IOType(4, []))), DropdownElements_createBuildingBlockDropdownItem(model, dispatch, state, setState, createHeaderFunc(new IOType(6, [""]))), DropdownElements_backToMainDropdownButton(state, setState)]);
}

export function Main(state, setState, state_search, setState_search, model, dispatch) {
    const elms_3 = singleton(createElement("div", createObj(Helpers_combineClasses("dropdown", toList(delay(() => append(state.DropdownIsActive ? singleton_1(["className", "is-active"]) : empty(), delay(() => {
        let elems_5, elms_1, elems_1, elms, elms_2, content, matchValue;
        return singleton_1((elems_5 = [(elms_1 = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e) => {
            e.stopPropagation();
            setState(new BuildingBlock_BuildingBlockUIState(!state.DropdownIsActive, state.DropdownPage, state.BodyCellType));
        }], (elems_1 = [createElement("span", {
            style: {
                marginRight: 5,
            },
            children: ARCtrl_ISA_CompositeHeader__CompositeHeader_get_AsButtonName(model.AddBuildingBlockState.Header),
        }), (elms = singleton(createElement("i", {
            className: "fa-solid fa-angle-down",
        })), createElement("span", {
            className: "icon",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))))), createElement("div", {
            className: "dropdown-trigger",
            children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
        })), (elms_2 = singleton((content = ((matchValue = state.DropdownPage, (matchValue.tag === 1) ? DropdownElements_dropdownContentProtocolTypeColumns(state, setState, state_search, setState_search, model, dispatch) : ((matchValue.tag === 2) ? DropdownElements_dropdownContentIOTypeColumns(matchValue.fields[0][0], state, setState, model, dispatch) : DropdownElements_dropdownContentMain(state, setState, model, dispatch)))), createElement("div", createObj(Helpers_combineClasses("dropdown-content", singleton(["children", Interop_reactApi.Children.toArray(Array.from(content))])))))), createElement("div", {
            className: "dropdown-menu",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))]));
    }))))))));
    return createElement("div", {
        className: "control",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    });
}

//# sourceMappingURL=Dropdown.js.map
