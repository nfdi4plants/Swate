import { nonSeeded } from "../../../fable_modules/fable-library.4.9.0/Random.js";
import { createObj, createAtom } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { CSSProp, HTMLAttr } from "../../../fable_modules/Fable.React.9.3.0/Fable.React.Props.fs.js";
import { keyValueList } from "../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { empty, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { createElement } from "react";
import * as react from "react";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";

export const rnd = nonSeeded();

export let order = createAtom(!(rnd.Next2(0, 10) > 5));

export function mainFunctionContainer(children) {
    const props = [new HTMLAttr(65, ["mainFunctionContainer"]), ["style", keyValueList(toList(delay(() => {
        const rndVal = rnd.Next2(30, 70) | 0;
        const colorArr = ["#61bbdd", "#35c8b0"];
        return append(singleton(new CSSProp(55, [`linear-gradient(${colorArr[order() ? 0 : 1]} ${100 - rndVal}%, ${colorArr[order() ? 1 : 0]})`])), delay(() => {
            order(!order());
            return empty();
        }));
    })), 1)]];
    return react.createElement("div", keyValueList(props, 1), ...children);
}

export function pageHeader(header) {
    return createElement("h1", createObj(Helpers_combineClasses("title", ofArray([["className", "is-5"], ["children", header]]))));
}

//# sourceMappingURL=LayoutHelper.js.map
