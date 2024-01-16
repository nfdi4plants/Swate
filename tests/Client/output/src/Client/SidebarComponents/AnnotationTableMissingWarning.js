import { createElement } from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { TryFindAnnoTableResult } from "../../Shared/OfficeInteropTypes.js";
import { Msg } from "../OfficeInterop/OfficeInteropState.js";
import { Msg as Msg_1 } from "../Messages.js";
import { ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { Msg as Msg_2 } from "../States/SpreadsheetInterface.js";

export function annotationTableMissingWarningComponent(model, dispatch) {
    let elems_2, elms, s, elms_1;
    return createElement("div", createObj(Helpers_combineClasses("notification", ofArray([["className", "is-warning"], (elems_2 = [createElement("button", createObj(Helpers_combineClasses("delete", singleton(["onClick", (_arg) => {
        dispatch(new Msg_1(6, [new Msg(2, [new TryFindAnnoTableResult(0, ["Remove Warning Notification"])])]));
    }])))), createElement("h5", {
        children: ["Warning: No annotation table found in worksheet"],
    }), (elms = singleton((s = "Your worksheet seems to contain no annotation table. You can create one by pressing the button below.", s)), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), (elms_1 = singleton(createElement("button", createObj(Helpers_combineClasses("button", ofArray([["className", "is-fullwidth"], ["onClick", (e) => {
        dispatch(new Msg_1(17, [new Msg_2(1, [e.ctrlKey])]));
    }], ["children", "create annotation table"]]))))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

//# sourceMappingURL=AnnotationTableMissingWarning.js.map
