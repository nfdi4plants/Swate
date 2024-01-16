import { createElement } from "react";
import * as react from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { singleton as singleton_1, ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { printf, toText, join } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { keyValueList } from "../../../fable_modules/fable-library.4.9.0/MapUtil.js";

export function responsiveFaElement(toggle, fa, faToggled) {
    let elms, elms_1, elms_2;
    const props_6 = [["style", {
        position: "relative",
    }]];
    const children_3 = [(elms = singleton_1(createElement("i", createObj(ofArray([["style", createObj(toList(delay(() => append(singleton(["position", "absolute"]), delay(() => append(singleton(["top", 0]), delay(() => append(singleton(["left", 0]), delay(() => append(singleton(["display", "block"]), delay(() => append(singleton(["transitionProperty", "opacity 0.25s, transform 0.25s"]), delay(() => (toggle ? singleton(["opacity", 0]) : singleton(["opacity", 1])))))))))))))))], fa])))), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), (elms_1 = singleton_1(createElement("i", createObj(ofArray([["style", createObj(toList(delay(() => append(singleton(["position", "absolute"]), delay(() => append(singleton(["top", 0]), delay(() => append(singleton(["left", 0]), delay(() => append(singleton(["display", "block"]), delay(() => append(singleton(["transitionProperty", "opacity 0.25s, transform 0.25s"]), delay(() => append(toggle ? singleton(["opacity", 1]) : singleton(["opacity", 0]), delay(() => (toggle ? singleton(["transform", join(" ", [("rotate(" + -180) + "deg)"])]) : singleton(["transform", join(" ", [("rotate(" + 0) + "deg)"])])))))))))))))))))], faToggled])))), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), (elms_2 = singleton_1(createElement("i", createObj(ofArray([["style", {
        display: "block",
        opacity: 0,
    }], fa])))), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    }))];
    return react.createElement("div", keyValueList(props_6, 1), ...children_3);
}

function createTriggeredId(id) {
    return toText(printf("%s_triggered"))(id);
}

function createNonTriggeredId(id) {
    return toText(printf("%s_triggered_not"))(id);
}

export function triggerResponsiveReturnEle(id) {
    const notTriggeredId = createNonTriggeredId(id);
    const triggeredId = createTriggeredId(id);
    const ele = document.getElementById(notTriggeredId);
    const triggeredEle = document.getElementById(triggeredId);
    ele.style.opacity = "0";
    triggeredEle.style.opacity = "1";
    triggeredEle.style.transform = "rotate(-360deg)";
}

export function responsiveReturnEle(id, fa, faToggled) {
    const notTriggeredId = createNonTriggeredId(id);
    const triggeredId = createTriggeredId(id);
    const props_3 = [["style", {
        position: "relative",
    }]];
    const children = [createElement("i", {
        style: {
            position: "absolute",
            top: 0,
            left: 0,
            display: "block",
            transition: "opacity 0.25s, transform 0.25s",
            opacity: 1,
        },
        id: notTriggeredId,
        onTransitionEnd: (e) => {
            setTimeout(() => {
                const ele = document.getElementById(notTriggeredId);
                ele.style.opacity = "1";
            }, 1500);
        },
        className: fa,
    }), createElement("i", {
        style: {
            position: "absolute",
            top: 0,
            left: 0,
            display: "block",
            transition: "opacity 0.25s, transform 0.25s",
            opacity: 0,
            transform: join(" ", [("rotate(" + 0) + "deg)"]),
        },
        id: triggeredId,
        onTransitionEnd: (e_1) => {
            setTimeout(() => {
                const triggeredEle = document.getElementById(triggeredId);
                triggeredEle.style.opacity = "0";
                triggeredEle.style.transform = "rotate(0deg)";
            }, 1500);
        },
        className: faToggled,
    }), createElement("i", {
        style: {
            position: "absolute",
            opacity: 0,
        },
        className: fa,
    })];
    return react.createElement("div", keyValueList(props_3, 1), ...children);
}

//# sourceMappingURL=ResponsiveFA.js.map
