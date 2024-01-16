import { createElement } from "react";
import * as react from "react";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Main } from "../../SidebarComponents/DarkmodeButton.js";
import { ofArray, singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { Msg } from "../../Messages.js";
import { Route } from "../../Routing.js";
import { singleton as singleton_1, append, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { pageHeader } from "../../SidebarComponents/LayoutHelper.js";

export function toggleDarkModeElement(model, dispatch) {
    let elems_1;
    return createElement("nav", createObj(Helpers_combineClasses("level", ofArray([["className", "is-mobile"], (elems_1 = [createElement("div", {
        className: "level-left",
        children: "Darkmode",
    }), createElement("div", createObj(Helpers_combineClasses("level-right", singleton(["children", createElement(Main, null)]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function swateExperts(model, dispatch) {
    let elems_1;
    return createElement("nav", createObj(Helpers_combineClasses("level", ofArray([["className", "is-mobile"], (elems_1 = [createElement("div", {
        className: "level-left",
        children: "Swate.Experts",
    }), createElement("div", createObj(Helpers_combineClasses("level-right", singleton(["children", createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-info"], ["className", "is-outlined"], ["onClick", (_arg) => {
        dispatch(new Msg(21, [[new Msg(20, [true]), new Msg(19, [new Route(8, [])])]]));
    }], ["children", "Swate.Experts"]]))))]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function swateCore(model, dispatch) {
    let elems_1;
    return createElement("nav", createObj(Helpers_combineClasses("level", ofArray([["className", "is-mobile"], (elems_1 = [createElement("div", {
        className: "level-left",
        children: "Swate.Core",
    }), createElement("div", createObj(Helpers_combineClasses("level-right", singleton(["children", createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-info"], ["className", "is-outlined"], ["onClick", (_arg) => {
        dispatch(new Msg(21, [[new Msg(20, [false]), new Msg(19, [new Route(1, [])])]]));
    }], ["children", "Swate.Core"]]))))]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function settingsViewComponent(model, dispatch) {
    const children = toList(delay(() => append(singleton_1(pageHeader("Swate Settings")), delay(() => append(singleton_1(createElement("label", {
        className: "label",
        children: "Customize Swate",
    })), delay(() => append(singleton_1(toggleDarkModeElement(model, dispatch)), delay(() => append(singleton_1(createElement("label", {
        className: "label",
        children: "Advanced Settings",
    })), delay(() => (model.PageState.IsExpert ? singleton_1(swateCore(model, dispatch)) : singleton_1(swateExperts(model, dispatch)))))))))))));
    return react.createElement("div", {}, ...children);
}

//# sourceMappingURL=SettingsView.js.map
