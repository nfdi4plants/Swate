import { min } from "../../../fable_modules/fable-library.4.9.0/Double.js";
import { SplitWindow__WriteToLocalStorage, SplitWindow_init, SplitWindow } from "../LocalStorage/SplitWindow.js";
import { createElement } from "react";
import React from "react";
import { TermTypes_createTerm } from "../../Shared/TermTypes.js";
import { useReact_useEffectOnce_3A5B6456, useReact_useEffect_311B4086, useReact_useState_FCFD9EF } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";

const minWidth = 400;

function calculateNewSideBarSize(model, pos) {
    return min((window.innerWidth - pos) - model.ScrollbarWidth, (~~window.innerWidth - minWidth) - ~~model.ScrollbarWidth);
}

function onResize_event(model, setModel, e) {
    const sidebarWindow = document.getElementById("sidebar_window").clientWidth;
    setModel(new SplitWindow(model.ScrollbarWidth, calculateNewSideBarSize(model, window.innerWidth - sidebarWindow)));
}

function mouseMove_event(model, setModel, e) {
    setModel(new SplitWindow(model.ScrollbarWidth, calculateNewSideBarSize(model, e.clientX)));
}

function mouseUp_event(mouseMove, e) {
    document.removeEventListener("mousemove", mouseMove);
    document.removeEventListener("mouseup", (e_1) => {
        mouseUp_event(mouseMove, e_1);
    });
}

function mouseDown_event(mouseMove, e) {
    e.preventDefault();
    document.addEventListener("mousemove", mouseMove);
    document.addEventListener("mouseup", (e_1) => {
        mouseUp_event(mouseMove, e_1);
    }, {
        capture: false,
        once: true,
        passive: false,
    });
}

function dragbar(model, setModel, dispatch) {
    return createElement("div", {
        style: {
            minWidth: 3 + "px",
            width: 3 + "px",
            height: 100 + "%",
            backgroundColor: "#A9A9A9",
            cursor: "col-resize",
            zIndex: 39,
        },
        onMouseDown: (e_1) => {
            mouseDown_event((e) => {
                mouseMove_event(model, setModel, e);
            }, e_1);
        },
    });
}

export const exampleTerm = TermTypes_createTerm("MS:1023810", "instrument model", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.", false, "MS");

/**
 * Splits screen into two parts. Left and right, with a dragbar in between to change size of right side.
 */
export function Main(mainInputProps) {
    let elems_2, elems_1;
    const dispatch = mainInputProps.dispatch;
    const right = mainInputProps.right;
    const left = mainInputProps.left;
    const patternInput = useReact_useState_FCFD9EF(SplitWindow_init);
    const setModel = patternInput[1];
    const model = patternInput[0];
    useReact_useEffect_311B4086(() => {
        SplitWindow__WriteToLocalStorage(model);
    }, [model]);
    useReact_useEffectOnce_3A5B6456(() => {
        window.addEventListener("resize", (e) => {
            onResize_event(model, setModel, e);
        });
    });
    return createElement("div", createObj(ofArray([["style", {
        display: "flex",
    }], (elems_2 = [createElement("div", {
        style: {
            minWidth: minWidth,
            flexGrow: 1,
            flexShrink: 1,
            height: 100 + "vh",
            width: 100 + "%",
        },
        children: Interop_reactApi.Children.toArray(Array.from(left)),
    }), createElement("div", createObj(ofArray([["id", "sidebar_window"], ["style", {
        float: "right",
        minWidth: minWidth,
        flexBasis: model.RightWindowWidth + "px",
        flexShrink: 0,
        flexGrow: 0,
        height: 100 + "vh",
        width: 100 + "%",
        overflow: "auto",
        display: "flex",
    }], (elems_1 = toList(delay(() => append(singleton(dragbar(model, setModel, dispatch)), delay(() => right)))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])));
}

//# sourceMappingURL=SplitWindowView.js.map
