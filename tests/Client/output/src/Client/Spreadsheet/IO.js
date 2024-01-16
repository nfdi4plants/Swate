import { fromFsWorkbook } from "../../../fable_modules/ARCtrl.ISA.Spreadsheet.1.0.4/ArcAssay.fs.js";
import { ARCtrlHelper_ArcFiles } from "../../Shared/ARCtrl.Helper.js";
import { ARCtrl_ISA_ArcStudy__ArcStudy_fromFsWorkbook_Static_32154C9D } from "../../../fable_modules/ARCtrl.ISA.Spreadsheet.1.0.4/ArcStudy.fs.js";
import { fromFsWorkbook as fromFsWorkbook_1 } from "../../../fable_modules/ARCtrl.ISA.Spreadsheet.1.0.4/ArcInvestigation.fs.js";
import { Template_fromFsWorkbook } from "../../../fable_modules/ARCtrl.1.0.4/Templates/Template.Spreadsheet.fs.js";
import { tail, head, isEmpty, ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { value } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { PromiseBuilder__Delay_62FBFDE1, PromiseBuilder__Run_212F1D4B } from "../../../fable_modules/Fable.Promise.3.2.0/Promise.fs.js";
import { promise } from "../../../fable_modules/Fable.Promise.3.2.0/PromiseImpl.fs.js";
import { Xlsx } from "../../../fable_modules/FsSpreadsheet.Exceljs.5.0.2/Xlsx.fs.js";

function tryToConvertAssay(fswb) {
    try {
        return new ARCtrlHelper_ArcFiles(3, [fromFsWorkbook(fswb)]);
    }
    catch (matchValue) {
        return void 0;
    }
}

function tryToConvertStudy(fswb) {
    let tupledArg;
    try {
        return (tupledArg = ARCtrl_ISA_ArcStudy__ArcStudy_fromFsWorkbook_Static_32154C9D(fswb), new ARCtrlHelper_ArcFiles(2, [tupledArg[0], tupledArg[1]]));
    }
    catch (matchValue) {
        return void 0;
    }
}

function tryToConvertInvestigation(fswb) {
    try {
        return new ARCtrlHelper_ArcFiles(1, [fromFsWorkbook_1(fswb)]);
    }
    catch (matchValue) {
        return void 0;
    }
}

function tryToConvertTemplate(fswb) {
    try {
        return new ARCtrlHelper_ArcFiles(0, [Template_fromFsWorkbook(fswb)]);
    }
    catch (matchValue) {
        return void 0;
    }
}

function converters() {
    return ofArray([tryToConvertAssay, tryToConvertStudy, tryToConvertInvestigation, tryToConvertTemplate]);
}

export function readFromBytes(bytes) {
    const tryConvert = (converters_1_mut, json_mut) => {
        tryConvert:
        while (true) {
            const converters_1 = converters_1_mut, json = json_mut;
            if (!isEmpty(converters_1)) {
                const matchValue = head(converters_1)(json);
                if (matchValue == null) {
                    converters_1_mut = tail(converters_1);
                    json_mut = json;
                    continue tryConvert;
                }
                else {
                    return value(matchValue);
                }
            }
            else {
                throw new Error("Unable to parse json to supported isa file.");
            }
            break;
        }
    };
    return PromiseBuilder__Run_212F1D4B(promise, PromiseBuilder__Delay_62FBFDE1(promise, () => (Xlsx.fromBytes(bytes).then((_arg) => {
        const arcFile = tryConvert(converters(), _arg);
        return Promise.resolve(arcFile);
    }))));
}

//# sourceMappingURL=IO.js.map
