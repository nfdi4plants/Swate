import { Cmd_none, Cmd_OfPromise_either } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { createTemplateMetadataWorksheet } from "../../OfficeInterop/Functions/TemplateMetadataFunctions.js";
import { Msg, DevMsg, curry } from "../../Messages.js";
import { mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";
import { createElement } from "react";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { ofArray, singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Metadata_root } from "../../../Shared/TemplateTypes.js";
import { Msg as Msg_1 } from "../../States/TemplateMetadataState.js";

export function update(msg, currentModel) {
    return [currentModel, Cmd_OfPromise_either(createTemplateMetadataWorksheet, msg.fields[0], (arg) => (new Msg(3, [curry((tupledArg) => (new DevMsg(1, [tupledArg[0], tupledArg[1]])), Cmd_none(), arg)])), (arg_1) => (new Msg(3, [curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), Cmd_none(), arg_1)])))];
}

export function defaultMessageEle(model, dispatch) {
    let elms_1, elms_2;
    return mainFunctionContainer([(elms_1 = singleton(createElement("p", {
        className: "help",
        children: Interop_reactApi.Children.toArray(["Use this function to create a prewritten template metadata worksheet."]),
    })), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), (elms_2 = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["onClick", (e) => {
        dispatch(new Msg(12, [new Msg_1(Metadata_root)]));
    }], ["className", "is-fullwidth"], ["className", "is-info"], ["children", "Create metadata"]]))))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    }))]);
}

export function newNameMainElement(model, dispatch) {
    const elms = ofArray([createElement("label", {
        className: "label",
        children: "Template Metadata",
    }), createElement("label", {
        className: "label",
        children: "Create template metadata worksheet",
    }), defaultMessageEle(model, dispatch)]);
    return createElement("div", {
        className: "content",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

//# sourceMappingURL=TemplateMetadata.js.map
