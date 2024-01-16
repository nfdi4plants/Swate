import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, bool_type, lambda_type, unit_type, list_type, class_type, string_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { defaultArg } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { ofArray, empty } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { createElement } from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { empty as empty_1, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export class QuickAccessButton extends Record {
    constructor(Description, FaList, Msg, IsActive, ButtonProps) {
        super();
        this.Description = Description;
        this.FaList = FaList;
        this.Msg = Msg;
        this.IsActive = IsActive;
        this.ButtonProps = ButtonProps;
    }
}

export function QuickAccessButton_$reflection() {
    return record_type("Components.QuickAccessButton.QuickAccessButton", [], QuickAccessButton, () => [["Description", string_type], ["FaList", list_type(class_type("Fable.React.ReactElement"))], ["Msg", lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type)], ["IsActive", bool_type], ["ButtonProps", list_type(class_type("Feliz.IReactProperty"))]]);
}

export function QuickAccessButton_create_Z9F8EBC5(description, faList, msg, isActive, buttonProps) {
    return new QuickAccessButton(description, faList, msg, defaultArg(isActive, true), defaultArg(buttonProps, empty()));
}

export function QuickAccessButton__toReactElement(this$) {
    let elems_2;
    const isDisabled = !this$.IsActive;
    return createElement("div", createObj(Helpers_combineClasses("navbar-item", ofArray([["title", this$.Description], ["style", createObj(toList(delay(() => append(singleton(["padding", 0]), delay(() => append(singleton(["minWidth", 45 + "px"]), delay(() => append(singleton(["display", "flex"]), delay(() => append(singleton(["alignItems", "center"]), delay(() => (isDisabled ? singleton(["cursor", "not-allowed"]) : empty_1()))))))))))))], (elems_2 = [createElement("a", createObj(Helpers_combineClasses("button", toList(delay(() => append(singleton(["tabIndex", isDisabled ? -1 : 0]), delay(() => append(this$.ButtonProps, delay(() => append(singleton(["disabled", isDisabled]), delay(() => append(singleton(["className", "myNavbarButton"]), delay(() => append(singleton(["onClick", this$.Msg]), delay(() => {
        let elems_1;
        return singleton((elems_1 = [createElement("div", {
            children: Interop_reactApi.Children.toArray(Array.from(this$.FaList)),
        })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))]));
    })))))))))))))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

//# sourceMappingURL=QuickAccessButton.js.map
