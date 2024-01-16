import { Cmd_OfAsync_start, Cmd_OfAsyncWith_either } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { api } from "../../Api.js";
import { AdvancedSearch_Msg, ApiMsg, ApiRequestMsg, DevMsg, curry, Msg, BuildingBlock_Msg } from "../../Messages.js";
import { Cmd_none } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { TermSearch_TermSearchUIState, BuildingBlock_Model } from "../../Model.js";
import { fromSeconds } from "../../../../fable_modules/fable-library.4.9.0/TimeSpan.js";
import { ofArray, singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { createAutocompleteSuggestions, autocompleteDropdownComponent, autocompleteTermSearchComponentInputComponent, AutocompleteParameters$1_ofAddBuildingBlockUnit2State_32E8F41A } from "../../SidebarComponents/AutocompleteSearch.js";
import { pageHeader, mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";
import { advancedSearchModal } from "../../SidebarComponents/AdvancedSearch.js";
import { createElement } from "react";
import * as react from "react";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { singleton as singleton_1, append, delay as delay_1, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { TermTypes_TermMinimal_create, TermTypes_TermMinimal_ofTerm_Z5E0A7659 } from "../../../Shared/TermTypes.js";
import { value as value_43 } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { Msg as Msg_1 } from "../../OfficeInterop/OfficeInteropState.js";
import { Main } from "./SearchComponent.js";
import { defaultOf } from "../../../../fable_modules/fable-library.4.9.0/Util.js";

export function update(addBuildingBlockMsg, state) {
    let msg;
    switch (addBuildingBlockMsg.tag) {
        case 1:
            return [state, Cmd_OfAsyncWith_either((x) => {
                Cmd_OfAsync_start(x);
            }, api.getTermSuggestions, {
                n: 5,
                query: addBuildingBlockMsg.fields[0],
            }, (t) => (new Msg(9, [new BuildingBlock_Msg(2, [t, addBuildingBlockMsg.fields[1]])])), (arg) => (new Msg(3, [curry((tupledArg) => (new DevMsg(3, [tupledArg[0], tupledArg[1]])), Cmd_none(), arg)])))];
        case 2: {
            const nextState_1 = new BuildingBlock_Model(state.Header, state.BodyCell, state.HeaderSearchText, addBuildingBlockMsg.fields[0], state.BodySearchText, state.BodySearchResults, state.Unit2TermSearchText, state.Unit2SelectedTerm, state.Unit2TermSuggestions, state.HasUnit2TermSuggestionsLoading, state.ShowUnit2TermSuggestions);
            addBuildingBlockMsg.fields[1].setState(new TermSearch_TermSearchUIState(true, false));
            return [nextState_1, Cmd_none()];
        }
        case 3:
            return [new BuildingBlock_Model(addBuildingBlockMsg.fields[0], state.BodyCell, state.HeaderSearchText, state.HeaderSearchResults, state.BodySearchText, state.BodySearchResults, state.Unit2TermSearchText, state.Unit2SelectedTerm, state.Unit2TermSuggestions, state.HasUnit2TermSuggestionsLoading, state.ShowUnit2TermSuggestions), Cmd_none()];
        case 4:
            return [new BuildingBlock_Model(state.Header, state.BodyCell, state.HeaderSearchText, state.HeaderSearchResults, addBuildingBlockMsg.fields[0], state.BodySearchResults, state.Unit2TermSearchText, state.Unit2SelectedTerm, state.Unit2TermSuggestions, state.HasUnit2TermSuggestionsLoading, state.ShowUnit2TermSuggestions), Cmd_none()];
        case 5:
            return [state, Cmd_OfAsyncWith_either((x_1) => {
                Cmd_OfAsync_start(x_1);
            }, api.getTermSuggestions, {
                n: 5,
                query: addBuildingBlockMsg.fields[0],
            }, (t_1) => (new Msg(9, [new BuildingBlock_Msg(8, [t_1, addBuildingBlockMsg.fields[1]])])), (arg_2) => (new Msg(3, [curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), Cmd_none(), arg_2)])))];
        case 6:
            return [state, Cmd_OfAsyncWith_either((x_2) => {
                Cmd_OfAsync_start(x_2);
            }, api.getTermSuggestionsByParentTerm, {
                n: 5,
                parent_term: addBuildingBlockMsg.fields[1],
                query: addBuildingBlockMsg.fields[0],
            }, (t_2) => (new Msg(9, [new BuildingBlock_Msg(8, [t_2, addBuildingBlockMsg.fields[2]])])), (arg_4) => (new Msg(3, [curry((tupledArg_2) => (new DevMsg(3, [tupledArg_2[0], tupledArg_2[1]])), Cmd_none(), arg_4)])))];
        case 7:
            return [state, Cmd_OfAsyncWith_either((x_3) => {
                Cmd_OfAsync_start(x_3);
            }, api.getAllTermsByParentTerm, addBuildingBlockMsg.fields[0], (t_3) => (new Msg(9, [new BuildingBlock_Msg(8, [t_3, addBuildingBlockMsg.fields[1]])])), (arg_6) => (new Msg(3, [curry((tupledArg_3) => (new DevMsg(3, [tupledArg_3[0], tupledArg_3[1]])), Cmd_none(), arg_6)])))];
        case 8: {
            const nextState_4 = new BuildingBlock_Model(state.Header, state.BodyCell, state.HeaderSearchText, state.HeaderSearchResults, state.BodySearchText, addBuildingBlockMsg.fields[0], state.Unit2TermSearchText, state.Unit2SelectedTerm, state.Unit2TermSuggestions, state.HasUnit2TermSuggestionsLoading, state.ShowUnit2TermSuggestions);
            addBuildingBlockMsg.fields[1].setState(new TermSearch_TermSearchUIState(true, false));
            return [nextState_4, Cmd_none()];
        }
        case 9:
            return [new BuildingBlock_Model(state.Header, addBuildingBlockMsg.fields[0], state.HeaderSearchText, state.HeaderSearchResults, state.BodySearchText, state.BodySearchResults, state.Unit2TermSearchText, state.Unit2SelectedTerm, state.Unit2TermSuggestions, state.HasUnit2TermSuggestionsLoading, state.ShowUnit2TermSuggestions), Cmd_none()];
        case 10: {
            const newTerm = addBuildingBlockMsg.fields[0];
            const triggerNewSearch = newTerm.length > 2;
            return [new BuildingBlock_Model(state.Header, state.BodyCell, state.HeaderSearchText, state.HeaderSearchResults, state.BodySearchText, state.BodySearchResults, newTerm, void 0, state.Unit2TermSuggestions, true, triggerNewSearch), (msg = (new Msg(0, [[fromSeconds(0.5), "GetNewUnitTermSuggestions", triggerNewSearch ? (new Msg(2, [new ApiMsg(0, [new ApiRequestMsg(2, [newTerm])])])) : (new Msg(25, []))]])), singleton((dispatch) => {
                dispatch(msg);
            }))];
        }
        case 12:
            return [new BuildingBlock_Model(state.Header, state.BodyCell, state.HeaderSearchText, state.HeaderSearchResults, state.BodySearchText, state.BodySearchResults, state.Unit2TermSearchText, state.Unit2SelectedTerm, addBuildingBlockMsg.fields[0], false, true), Cmd_none()];
        case 11: {
            const suggestion = addBuildingBlockMsg.fields[0];
            return [new BuildingBlock_Model(state.Header, state.BodyCell, state.HeaderSearchText, state.HeaderSearchResults, state.BodySearchText, state.BodySearchResults, suggestion.Name, suggestion, state.Unit2TermSuggestions, false, false), Cmd_none()];
        }
        default:
            return [new BuildingBlock_Model(state.Header, state.BodyCell, addBuildingBlockMsg.fields[0], state.HeaderSearchResults, state.BodySearchText, state.BodySearchResults, state.Unit2TermSearchText, state.Unit2SelectedTerm, state.Unit2TermSuggestions, state.HasUnit2TermSuggestionsLoading, state.ShowUnit2TermSuggestions), Cmd_none()];
    }
}

export function addUnitToExistingBlockElements(model, dispatch) {
    let elms_1, elms, s_1, elms_3, elems_5, elms_4;
    const autocompleteParamsUnit2 = AutocompleteParameters$1_ofAddBuildingBlockUnit2State_32E8F41A(model.AddBuildingBlockState);
    return mainFunctionContainer([advancedSearchModal(model, autocompleteParamsUnit2.ModalId, autocompleteParamsUnit2.InputId, dispatch, autocompleteParamsUnit2.OnAdvancedSearch), (elms_1 = singleton((elms = ofArray([react.createElement("b", {}, "Adds a unit to a complete building block."), (s_1 = " If the building block already has a unit assigned, the new unit is only applied to selected rows of the selected column.", s_1)]), createElement("p", {
        className: "help",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), (elms_3 = toList(delay_1(() => {
        let elems_3, elms_2;
        const changeUnitAutoCompleteParams = AutocompleteParameters$1_ofAddBuildingBlockUnit2State_32E8F41A(model.AddBuildingBlockState);
        return append(singleton_1(createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], (elems_3 = [(elms_2 = singleton(createElement("button", createObj(Helpers_combineClasses("button", ofArray([["className", "is-static"], ["className", "has-background-white"], ["children", "Add unit"]]))))), createElement("p", {
            className: "control",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        })), autocompleteTermSearchComponentInputComponent(dispatch, false, "Start typing to search", void 0, changeUnitAutoCompleteParams)], ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])]))))), delay_1(() => singleton_1(autocompleteDropdownComponent(dispatch, changeUnitAutoCompleteParams.DropDownIsVisible, changeUnitAutoCompleteParams.DropDownIsLoading, createAutocompleteSuggestions(dispatch, changeUnitAutoCompleteParams, model)))));
    })), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    })), createElement("p", createObj(Helpers_combineClasses("help", ofArray([["style", {
        display: "inline",
    }], (elems_5 = [createElement("a", {
        onClick: (e) => {
            e.preventDefault();
            dispatch(new Msg(5, [new AdvancedSearch_Msg(0, [AutocompleteParameters$1_ofAddBuildingBlockUnit2State_32E8F41A(model.AddBuildingBlockState).ModalId])]));
        },
        children: "Use advanced search",
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_5))])])))), (elms_4 = singleton(createElement("button", createObj(Helpers_combineClasses("button", toList(delay_1(() => {
        const isValid = model.AddBuildingBlockState.Unit2TermSearchText !== "";
        return append(singleton_1(["className", "is-success"]), delay_1(() => append(isValid ? singleton_1(["className", "is-active"]) : append(singleton_1(["className", "is-danger"]), delay_1(() => singleton_1(["disabled", true]))), delay_1(() => append(singleton_1(["className", "is-fullwidth"]), delay_1(() => append(singleton_1(["onClick", (_arg) => {
            const unitTerm = (model.AddBuildingBlockState.Unit2SelectedTerm != null) ? TermTypes_TermMinimal_ofTerm_Z5E0A7659(value_43(model.AddBuildingBlockState.Unit2SelectedTerm)) : void 0;
            if (model.AddBuildingBlockState.Unit2TermSearchText === "") {
                dispatch(new Msg(3, [curry((tupledArg) => (new DevMsg(1, [tupledArg[0], tupledArg[1]])), Cmd_none(), ["Error", "Cannot execute function with empty unit input"])]));
            }
            else if (model.AddBuildingBlockState.Unit2SelectedTerm != null) {
                dispatch(new Msg(6, [new Msg_1(8, [value_43(unitTerm)])]));
            }
            else {
                dispatch(new Msg(6, [new Msg_1(8, [TermTypes_TermMinimal_create(model.AddBuildingBlockState.Unit2TermSearchText, "")])]));
            }
        }]), delay_1(() => singleton_1(["children", "Update unit for cells"])))))))));
    })))))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_4)),
    }))]);
}

export function addBuildingBlockComponent(model, dispatch) {
    const children = toList(delay_1(() => append(singleton_1(pageHeader("Building Blocks")), delay_1(() => append(singleton_1(createElement("label", {
        className: "label",
        children: "Add annotation building blocks (columns) to the annotation table.",
    })), delay_1(() => append(singleton_1(createElement(Main, {
        model: model,
        dispatch: dispatch,
    })), delay_1(() => {
        const matchValue = model.PersistentStorageState.Host;
        let matchResult;
        if (matchValue != null) {
            if (matchValue.tag === 1) {
                matchResult = 0;
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
                return append(singleton_1(createElement("label", {
                    className: "label",
                    children: "Add/Update unit reference to existing building block.",
                })), delay_1(() => singleton_1(addUnitToExistingBlockElements(model, dispatch))));
            default:
                return singleton_1(defaultOf());
        }
    }))))))));
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

//# sourceMappingURL=BuildingBlockView.js.map
