import { sortBy, max as max_1, min as min_1, indexed, map, equalsWith, tryFind } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { Array_groupBy } from "../../../fable_modules/fable-library.4.9.0/Seq2.js";
import { createObj, comparePrimitives, numberHash } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { printf, toText, join } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { map as map_1, singleton, collect, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { HTMLAttr } from "../../../fable_modules/Fable.React.9.3.0/Fable.React.Props.fs.js";
import { createElement } from "react";
import * as react from "react";
import { ofArray, singleton as singleton_1 } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { keyValueList } from "../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { value as value_13 } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Msg, BuildingBlockDetailsMsg } from "../Messages.js";

function getBuildingBlockHeader(terms) {
    return tryFind((x) => equalsWith((x_1, y) => (x_1 === y), x.RowIndices, new Int32Array([0])), terms);
}

function getBodyRows(terms) {
    return terms.filter((x) => !equalsWith((x_1, y) => (x_1 === y), x.RowIndices, new Int32Array([0])));
}

function windowRowIndices(rowIndices) {
    const separatedIndices = map((tupledArg_1) => map((tuple) => tuple[1], tupledArg_1[1], Int32Array), Array_groupBy((tupledArg) => (tupledArg[0] - tupledArg[1]), indexed(rowIndices), {
        Equals: (x_1, y) => (x_1 === y),
        GetHashCode: numberHash,
    }));
    return join(", ", toList(delay(() => collect((contRowIndices) => {
        let min, max;
        return singleton((contRowIndices.length > 1) ? ((min = (min_1(contRowIndices, {
            Compare: comparePrimitives,
        }) | 0), (max = (max_1(contRowIndices, {
            Compare: comparePrimitives,
        }) | 0), toText(printf("%i-%i"))(min)(max)))) : (`${contRowIndices[0]}`));
    }, separatedIndices))));
}

function rowIndicesToReadable(rowIndices) {
    if (rowIndices.length > 1) {
        return windowRowIndices(rowIndices);
    }
    else if (equalsWith((x, y) => (x === y), rowIndices, new Int32Array([0]))) {
        return "Header";
    }
    else {
        return `${rowIndices[0]}`;
    }
}

function infoIcon(txt) {
    let elms;
    if (txt === "") {
        return "No defintion found";
    }
    else {
        const props_2 = [["style", {
            color: "#FFC000",
        }], new HTMLAttr(65, ["has-tooltip-right has-tooltip-multiline"]), ["data-tooltip", txt]];
        const children_1 = [(elms = singleton_1(createElement("i", {
            className: "fa-solid fa-circle-info",
        })), createElement("span", {
            className: "icon",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))];
        return react.createElement("span", keyValueList(props_2, 1), ...children_1);
    }
}

function searchResultTermToTableHeaderElement(term) {
    let isEmpty, props_4, children_6, children_10, props_11, children_12, children_14, children_16, props_18, props_20, children_22, children_26;
    if (term == null) {
        const children_38 = ofArray([react.createElement("th", {}, "-"), react.createElement("th", {}, "-"), react.createElement("th", {}, "-"), react.createElement("th", {}, "Header")]);
        return createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_38)),
        });
    }
    else if ((isEmpty = term, (isEmpty.Term.Name === "") && (isEmpty.Term.TermAccession === ""))) {
        const isEmpty_1 = term;
        const children_8 = ofArray([react.createElement("th", {}, "-"), react.createElement("th", {}, "-"), (props_4 = [["style", {
            textAlign: "center",
        }]], react.createElement("th", keyValueList(props_4, 1), "-")), (children_6 = [rowIndicesToReadable(isEmpty_1.RowIndices)], react.createElement("th", {}, ...children_6))]);
        return createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_8)),
        });
    }
    else if (term.SearchResultTerm != null) {
        const hasResult_1 = term;
        const children_18 = ofArray([(children_10 = [value_13(hasResult_1.SearchResultTerm).Name], react.createElement("th", {}, ...children_10)), (props_11 = [["style", {
            textAlign: "center",
        }]], (children_12 = [infoIcon(value_13(hasResult_1.SearchResultTerm).Description)], react.createElement("th", keyValueList(props_11, 1), ...children_12))), (children_14 = [value_13(hasResult_1.SearchResultTerm).Accession], react.createElement("th", {}, ...children_14)), (children_16 = [rowIndicesToReadable(hasResult_1.RowIndices)], react.createElement("th", {}, ...children_16))]);
        return createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_18)),
        });
    }
    else if (term.SearchResultTerm == null) {
        const hasNoResult_1 = term;
        const children_28 = ofArray([(props_18 = [["style", {
            color: "#CE4B61",
        }]], react.createElement("th", keyValueList(props_18, 1), hasNoResult_1.Term.Name)), (props_20 = [["style", {
            textAlign: "center",
        }]], (children_22 = [infoIcon("This Term was not found in the database.")], react.createElement("th", keyValueList(props_20, 1), ...children_22))), react.createElement("th", {}, hasNoResult_1.Term.TermAccession), (children_26 = [rowIndicesToReadable(hasNoResult_1.RowIndices)], react.createElement("th", {}, ...children_26))]);
        return createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_28)),
        });
    }
    else {
        throw new Error(`Swate encountered an error when trying to parse ${term} to search results.`);
    }
}

function searchResultTermToTableElement(term) {
    let props_2, children_6, children_10, props_11, children_12, children_14, children_16, props_18, props_20, children_22, children_26;
    if ((term.Term.Name === "") && (term.Term.TermAccession === "")) {
        const children_8 = ofArray([react.createElement("td", {}, "-"), (props_2 = [["style", {
            textAlign: "center",
        }]], react.createElement("td", keyValueList(props_2, 1), "-")), react.createElement("td", {}, "-"), (children_6 = [rowIndicesToReadable(term.RowIndices)], react.createElement("td", {}, ...children_6))]);
        return createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_8)),
        });
    }
    else if (term.SearchResultTerm != null) {
        const hasResult_1 = term;
        const children_18 = ofArray([(children_10 = [value_13(hasResult_1.SearchResultTerm).Name], react.createElement("td", {}, ...children_10)), (props_11 = [["style", {
            textAlign: "center",
        }]], (children_12 = [infoIcon(value_13(hasResult_1.SearchResultTerm).Description)], react.createElement("td", keyValueList(props_11, 1), ...children_12))), (children_14 = [value_13(hasResult_1.SearchResultTerm).Accession], react.createElement("td", {}, ...children_14)), (children_16 = [rowIndicesToReadable(hasResult_1.RowIndices)], react.createElement("td", {}, ...children_16))]);
        return createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_18)),
        });
    }
    else if (term.SearchResultTerm == null) {
        const hasNoResult_1 = term;
        const children_28 = ofArray([(props_18 = [["style", {
            color: "#CE4B61",
        }]], react.createElement("td", keyValueList(props_18, 1), hasNoResult_1.Term.Name)), (props_20 = [["style", {
            textAlign: "center",
        }]], (children_22 = [infoIcon("This Term was not found in the database.")], react.createElement("td", keyValueList(props_20, 1), ...children_22))), react.createElement("td", {}, hasNoResult_1.Term.TermAccession), (children_26 = [rowIndicesToReadable(hasNoResult_1.RowIndices)], react.createElement("td", {}, ...children_26))]);
        return createElement("tr", {
            children: Interop_reactApi.Children.toArray(Array.from(children_28)),
        });
    }
    else {
        throw new Error(`Swate encountered an error when trying to parse ${term} to search results.`);
    }
}

function tableElement(terms) {
    let elems, children_10, children_8, props_2, children_12, children_14;
    const rowHeader = getBuildingBlockHeader(terms);
    const bodyRows = getBodyRows(terms);
    return createElement("table", createObj(Helpers_combineClasses("table", ofArray([["className", "is-fullwidth"], ["className", "is-striped"], (elems = [(children_10 = [(children_8 = ofArray([react.createElement("th", {
        className: "toExcelColor",
    }, "Name"), (props_2 = [new HTMLAttr(65, ["toExcelColor"]), ["style", {
        textAlign: "center",
    }]], react.createElement("th", keyValueList(props_2, 1), "Desc.")), react.createElement("th", {
        className: "toExcelColor",
    }, "TAN"), react.createElement("th", {
        className: "toExcelColor",
    }, "Row")]), createElement("tr", {
        children: Interop_reactApi.Children.toArray(Array.from(children_8)),
    }))], react.createElement("thead", {}, ...children_10)), (children_12 = [searchResultTermToTableHeaderElement(rowHeader)], react.createElement("thead", {}, ...children_12)), (children_14 = toList(delay(() => map_1(searchResultTermToTableElement, bodyRows))), react.createElement("tbody", {}, ...children_14))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
}

export function buildingBlockDetailModal(model, dispatch, rmv) {
    let elems_1, elems;
    const closeMsg = (e) => {
        rmv(e);
        dispatch(new Msg(13, [new BuildingBlockDetailsMsg(2, [[]])]));
    };
    const baseArr = sortBy((x) => min_1(x.RowIndices, {
        Compare: comparePrimitives,
    }), model.BuildingBlockValues, {
        Compare: comparePrimitives,
    });
    return createElement("div", createObj(Helpers_combineClasses("modal", ofArray([["className", "is-active"], (elems_1 = [createElement("div", createObj(Helpers_combineClasses("modal-background", singleton_1(["onClick", closeMsg])))), createElement("div", createObj(Helpers_combineClasses("notification", ofArray([["style", {
        width: 90 + "%",
        maxHeight: 80 + "%",
    }], (elems = [createElement("button", createObj(Helpers_combineClasses("delete", singleton_1(["onClick", closeMsg])))), tableElement(baseArr)], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

//# sourceMappingURL=BuildingBlockDetailsModal.js.map
