import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, int32_type, lambda_type, unit_type, class_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { createElement } from "react";
import { numberHash, arrayHash, equalArrays, curry2, createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { ofArray, empty, singleton as singleton_1 } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { toArray } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { map, contains } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { Array_distinct } from "../../../fable_modules/fable-library.4.9.0/Seq2.js";
import { Msg } from "../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../Messages.js";
import { renderModal } from "../Modals/Controller.js";

class ContextFunctions extends Record {
    constructor(DeleteRow, DeleteColumn, Copy, Cut, Paste, FillColumn, RowIndex, ColumnIndex) {
        super();
        this.DeleteRow = DeleteRow;
        this.DeleteColumn = DeleteColumn;
        this.Copy = Copy;
        this.Cut = Cut;
        this.Paste = Paste;
        this.FillColumn = FillColumn;
        this.RowIndex = (RowIndex | 0);
        this.ColumnIndex = (ColumnIndex | 0);
    }
}

function ContextFunctions_$reflection() {
    return record_type("MainComponents.ContextMenu.ContextFunctions", [], ContextFunctions, () => [["DeleteRow", lambda_type(lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type), lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type))], ["DeleteColumn", lambda_type(lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type), lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type))], ["Copy", lambda_type(lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type), lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type))], ["Cut", lambda_type(lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type), lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type))], ["Paste", lambda_type(lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type), lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type))], ["FillColumn", lambda_type(lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type), lambda_type(class_type("Browser.Types.MouseEvent", void 0), unit_type))], ["RowIndex", int32_type], ["ColumnIndex", int32_type]]);
}

function contextmenu(mousex, mousey, funcs, selectedCell, rmv) {
    let tupledArg_1, tupledArg_2, tupledArg_3, tupledArg_4, tupledArg_5, tupledArg_6, elems_2;
    const rmv_element = createElement("div", {
        onClick: rmv,
        onContextMenu: (e) => {
            e.preventDefault();
            rmv(e);
        },
        style: {
            position: "fixed",
            backgroundColor: "transparent",
            left: 0,
            top: 0,
            right: 0,
            bottom: 0,
            display: "block",
        },
    });
    const button = (tupledArg) => {
        const children_1 = singleton_1(createElement("button", createObj(Helpers_combineClasses("button", toList(delay(() => append(singleton(["style", {
            borderRadius: 0,
            justifyContent: "space-between",
        }]), delay(() => append(singleton(["onClick", tupledArg[2]]), delay(() => append(singleton(["className", "is-fullwidth"]), delay(() => append(singleton(["className", "is-black"]), delay(() => append(singleton(["className", "is-inverted"]), delay(() => append(tupledArg[3], delay(() => {
            let elems_1, elms;
            return singleton((elems_1 = [(elms = singleton_1(createElement("i", {
                className: tupledArg[1],
            })), createElement("span", {
                className: "icon",
                children: Interop_reactApi.Children.toArray(Array.from(elms)),
            })), createElement("span", {
                children: [tupledArg[0]],
            })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))]));
        }))))))))))))))))));
        return createElement("li", {
            children: Interop_reactApi.Children.toArray(Array.from(children_1)),
        });
    };
    let divider;
    const children_3 = singleton_1(createElement("div", {
        style: {
            border: (((2 + "px ") + "solid") + " ") + "#2D3E50",
            margin: ((2 + "px ") + 0) + "px",
        },
    }));
    divider = createElement("li", {
        children: Interop_reactApi.Children.toArray(Array.from(children_3)),
    });
    const buttonList = ofArray([(tupledArg_1 = ["Fill Column", "fa-solid fa-file-signature", curry2(funcs.FillColumn)(rmv), empty()], button([tupledArg_1[0], tupledArg_1[1], tupledArg_1[2], tupledArg_1[3]])), divider, (tupledArg_2 = ["Copy", "fa-solid fa-copy", curry2(funcs.Copy)(rmv), empty()], button([tupledArg_2[0], tupledArg_2[1], tupledArg_2[2], tupledArg_2[3]])), (tupledArg_3 = ["Cut", "fa-solid fa-scissors", curry2(funcs.Cut)(rmv), empty()], button([tupledArg_3[0], tupledArg_3[1], tupledArg_3[2], tupledArg_3[3]])), (tupledArg_4 = ["Paste", "fa-solid fa-paste", curry2(funcs.Paste)(rmv), singleton_1(["disabled", selectedCell == null])], button([tupledArg_4[0], tupledArg_4[1], tupledArg_4[2], tupledArg_4[3]])), divider, (tupledArg_5 = ["Delete Row", "fa-solid fa-delete-left", curry2(funcs.DeleteRow)(rmv), empty()], button([tupledArg_5[0], tupledArg_5[1], tupledArg_5[2], tupledArg_5[3]])), (tupledArg_6 = ["Delete Column", "fa-solid fa-delete-left fa-rotate-270", curry2(funcs.DeleteColumn)(rmv), empty()], button([tupledArg_6[0], tupledArg_6[1], tupledArg_6[2], tupledArg_6[3]]))]);
    return createElement("div", createObj(ofArray([["style", {
        backgroundColor: "white",
        position: "absolute",
        left: mousex,
        top: mousey - 40,
        width: 150,
        zIndex: 40,
        border: (((1 + "px ") + "solid") + " ") + "#2D3E50",
    }], (elems_2 = [rmv_element, createElement("ul", {
        children: Interop_reactApi.Children.toArray(Array.from(buttonList)),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])));
}

export function onContextMenu(index, model, dispatch, e) {
    e.stopPropagation();
    e.preventDefault();
    const mousePosition = [~~e.pageX, ~~e.pageY];
    const funcs = new ContextFunctions((rmv, e_1) => {
        rmv(e_1);
        const s = toArray(model.SpreadsheetModel.SelectedCells);
        if ((!(s.length === 0) && s.every((tupledArg) => (tupledArg[0] === index[0]))) && contains(index, s, {
            Equals: equalArrays,
            GetHashCode: arrayHash,
        })) {
            dispatch(new Msg_1(15, [new Msg(10, [Array_distinct(map((tuple) => tuple[1], s, Int32Array), {
                Equals: (x_1, y_1) => (x_1 === y_1),
                GetHashCode: numberHash,
            })])]));
        }
        else {
            dispatch(new Msg_1(15, [new Msg(9, [index[1]])]));
        }
    }, (rmv_1, e_2) => {
        rmv_1(e_2);
        dispatch(new Msg_1(15, [new Msg(11, [index[0]])]));
    }, (rmv_2, e_3) => {
        rmv_2(e_3);
        dispatch(new Msg_1(15, [new Msg(15, [index])]));
    }, (rmv_3, e_4) => {
        rmv_3(e_4);
        dispatch(new Msg_1(15, [new Msg(16, [index])]));
    }, (rmv_4, e_5) => {
        rmv_4(e_5);
        dispatch(new Msg_1(15, [new Msg(17, [index])]));
    }, (rmv_5, e_6) => {
        rmv_5(e_6);
        dispatch(new Msg_1(15, [new Msg(18, [index])]));
    }, index[1], index[0]);
    renderModal(`context_${mousePosition}`, (rmv_6) => contextmenu(mousePosition[0], mousePosition[1], funcs, model.SpreadsheetModel.Clipboard.Cell, rmv_6));
}

//# sourceMappingURL=ContextMenu.js.map
