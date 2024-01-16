import { createElement } from "react";
import React from "react";
import { defaultOf } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Main as Main_1 } from "../MainComponents/Metadata/Study.js";
import { Main as Main_2 } from "../MainComponents/Metadata/Investigation.js";
import { Main as Main_3 } from "../MainComponents/Metadata/Template.js";
import { Main as Main_4 } from "../MainComponents/Metadata/Assay.js";
import { Main as Main_5 } from "../MainComponents/SpreadsheetView.js";

export function Main(x) {
    const model = x.model;
    const dispatch = x.dispatch;
    if (model.SpreadsheetModel.ActiveView.tag === 1) {
        const matchValue_3 = model.SpreadsheetModel.ArcFile;
        if (matchValue_3 == null) {
            return defaultOf();
        }
        else {
            switch (matchValue_3.tag) {
                case 2: {
                    const aArr = matchValue_3.fields[1];
                    const s = matchValue_3.fields[0];
                    return Main_1(s, aArr, model, dispatch);
                }
                case 1: {
                    const inv = matchValue_3.fields[0];
                    return Main_2(inv, model, dispatch);
                }
                case 0: {
                    const t = matchValue_3.fields[0];
                    return Main_3(t, model, dispatch);
                }
                default: {
                    const a = matchValue_3.fields[0];
                    return Main_4(a, model, dispatch);
                }
            }
        }
    }
    else {
        return createElement(Main_5, {
            model: model,
            dispatch: dispatch,
        });
    }
}

//# sourceMappingURL=XlsxFileView.js.map
