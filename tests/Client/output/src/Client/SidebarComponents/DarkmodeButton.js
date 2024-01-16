import { createElement } from "react";
import React from "react";
import { useReact_useContext_37FA55CF } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { DataTheme__get_toIcon, State, DataTheme_SET_EA9902F, DataTheme, themeContext } from "../LocalStorage/Darkmode.js";
import { equals, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function Main() {
    let elems;
    const state = useReact_useContext_37FA55CF(themeContext);
    return createElement("a", createObj(Helpers_combineClasses("navbar-item", ofArray([["onClick", (e) => {
        e.preventDefault();
        const next = equals(state.Theme, new DataTheme(0, [])) ? (new DataTheme(1, [])) : (new DataTheme(0, []));
        DataTheme_SET_EA9902F(next);
        state.SetTheme(new State(next, state.SetTheme));
    }], (elems = [createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["className", "is-medium"], ["children", DataTheme__get_toIcon(state.Theme)]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

//# sourceMappingURL=DarkmodeButton.js.map
