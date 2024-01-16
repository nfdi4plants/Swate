import { Msg, Model } from "../../States/DagState.js";
import { Cmd_none, Cmd_OfPromise_either } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { getBuildingBlocksAndSheets } from "../../OfficeInterop/OfficeInterop.js";
import { Model__updateByDagModel_Z220F769A, DevMsg, curry, Msg as Msg_1 } from "../../Messages.js";
import { ofArray, singleton } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Cmd_OfAsync_start, Cmd_OfAsyncWith_either } from "../../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { dagApi } from "../../Api.js";
import { pageHeader, mainFunctionContainer } from "../../SidebarComponents/LayoutHelper.js";
import { empty, singleton as singleton_1, append, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { createElement } from "react";
import * as react from "react";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Msg as Msg_2 } from "../../States/SpreadsheetInterface.js";
import { value as value_22 } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { HTMLAttr } from "../../../../fable_modules/Fable.React.9.3.0/Fable.React.Props.fs.js";
import { keyValueList } from "../../../../fable_modules/fable-library.4.9.0/MapUtil.js";

export function update(msg, currentModel) {
    switch (msg.tag) {
        case 1: {
            const nextModel_1 = new Model(true, currentModel.DagModel.DagHtml);
            const cmd = Cmd_OfPromise_either(getBuildingBlocksAndSheets, void 0, (arg) => (new Msg_1(16, [new Msg(2, [arg])])), (arg_1) => (new Msg_1(3, [((b) => curry((tupledArg) => (new DevMsg(3, [tupledArg[0], tupledArg[1]])), singleton((dispatch) => {
                dispatch(new Msg_1(16, [new Msg(0, [false])]));
            }), b))(arg_1)])));
            return [Model__updateByDagModel_Z220F769A(currentModel, nextModel_1), cmd];
        }
        case 2:
            return [currentModel, Cmd_OfAsyncWith_either((x) => {
                Cmd_OfAsync_start(x);
            }, dagApi.parseAnnotationTablesToDagHtml, msg.fields[0], (arg_2) => (new Msg_1(16, [new Msg(3, [arg_2])])), (arg_3) => (new Msg_1(3, [((b_1) => curry((tupledArg_1) => (new DevMsg(3, [tupledArg_1[0], tupledArg_1[1]])), singleton((dispatch_1) => {
                dispatch_1(new Msg_1(16, [new Msg(0, [false])]));
            }), b_1))(arg_3)])))];
        case 3:
            return [Model__updateByDagModel_Z220F769A(currentModel, new Model(false, msg.fields[0])), Cmd_none()];
        default:
            return [Model__updateByDagModel_Z220F769A(currentModel, new Model(msg.fields[0], currentModel.DagModel.DagHtml)), Cmd_none()];
    }
}

export function defaultMessageEle(model, dispatch) {
    return mainFunctionContainer(toList(delay(() => {
        let elms_2, elms, s_6;
        return append(singleton_1((elms_2 = ofArray([(elms = ofArray(["A ", react.createElement("b", {}, "D"), "irected ", react.createElement("b", {}, "A"), "cyclic ", react.createElement("b", {}, "G"), (s_6 = "raph represents the chain of applied protocols to samples. Within are all intermediate products as well as protocols displayed.", s_6)]), createElement("p", {
            className: "help",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        })), createElement("p", {
            className: "help",
            children: Interop_reactApi.Children.toArray(["This only works if your input and output columns have values."]),
        })]), createElement("div", {
            className: "field",
            children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
        }))), delay(() => {
            let elms_3;
            return append(singleton_1((elms_3 = singleton(createElement("a", createObj(Helpers_combineClasses("button", ofArray([["className", "is-fullwidth"], ["className", "is-info"], ["onClick", (_arg) => {
                dispatch(new Msg_1(17, [new Msg_2(11, [])]));
            }], ["children", "Display dag"]]))))), createElement("div", {
                className: "field",
                children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
            }))), delay(() => {
                let elms_4, props_12;
                return (model.DagModel.DagHtml != null) ? singleton_1((elms_4 = singleton((props_12 = [new HTMLAttr(150, [value_22(model.DagModel.DagHtml)]), ["style", {
                    width: "100%",
                    height: "400px",
                }]], react.createElement("iframe", keyValueList(props_12, 1)))), createElement("div", {
                    className: "field",
                    children: Interop_reactApi.Children.toArray(Array.from(elms_4)),
                }))) : empty();
            }));
        }));
    })));
}

export function mainElement(model, dispatch) {
    let elems;
    return createElement("div", createObj(Helpers_combineClasses("content", ofArray([["onSubmit", (e) => {
        e.preventDefault();
    }], ["onKeyDown", (k) => {
        if (~~k.which === 13) {
            k.preventDefault();
        }
    }], (elems = [pageHeader("Visualize Protocol Flow"), createElement("label", {
        className: "label",
        children: "Display directed acyclic graph",
    }), defaultMessageEle(model, dispatch)], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

//# sourceMappingURL=Dag.js.map
