import { Msg } from "../OfficeInterop/OfficeInteropState.js";
import { curry, ApiMsg, ApiRequestMsg, DevMsg, Msg as Msg_1 } from "../Messages.js";
import { ofArray, singleton } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { Msg as Msg_2 } from "../States/Spreadsheet.js";
import { some } from "../../../fable_modules/fable-library.4.9.0/Option.js";
import { Cmd_OfPromise_either, Cmd_ofEffect, Cmd_batch, Cmd_none } from "../../../fable_modules/Fable.Elmish.4.1.0/cmd.fs.js";
import { toArray, isEmpty } from "../../../fable_modules/fable-library.4.9.0/Set.js";
import { Array_distinct } from "../../../fable_modules/fable-library.4.9.0/Seq2.js";
import { unzip } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { numberHash } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Operators_Failure } from "../../../fable_modules/fable-library.4.9.0/FSharp.Core.js";
import { Msg as Msg_3 } from "../States/JsonExporterState.js";
import { Msg as Msg_4 } from "../States/DagState.js";
import { singleton as singleton_1, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { addOnKeydownEvent } from "../MainComponents/KeyboardShortcuts.js";
import { send } from "../ARCitect/ARCitect.js";
import { Msg as Msg_5 } from "../States/ARCitect.js";
import { tryFindActiveAnnotationTable } from "../OfficeInterop/OfficeInterop.js";

function Helper_initializeAddIn() {
    return Office.onReady();
}

export function Interface_update(model, msg) {
    let msg_19, msg_20, msg_22;
    const host = model.PersistentStorageState.Host;
    switch (msg.tag) {
        case 1: {
            const usePrevOutput = msg.fields[0];
            let matchResult;
            if (host != null) {
                switch (host.tag) {
                    case 0:
                    case 2: {
                        matchResult = 1;
                        break;
                    }
                    default:
                        matchResult = 0;
                }
            }
            else {
                matchResult = 2;
            }
            switch (matchResult) {
                case 0:
                    return [model, singleton((dispatch_4) => {
                        dispatch_4(new Msg_1(6, [new Msg(0, [usePrevOutput])]));
                    })];
                case 1:
                    return [model, singleton((dispatch_5) => {
                        dispatch_5(new Msg_1(15, [new Msg_2(21, [usePrevOutput])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 3: {
            let matchResult_1;
            if (host != null) {
                switch (host.tag) {
                    case 0:
                    case 2: {
                        matchResult_1 = 0;
                        break;
                    }
                    default:
                        matchResult_1 = 1;
                }
            }
            else {
                matchResult_1 = 1;
            }
            switch (matchResult_1) {
                case 0:
                    return [model, singleton((dispatch_6) => {
                        dispatch_6(new Msg_1(15, [new Msg_2(22, [msg.fields[0]])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 4: {
            let matchResult_2;
            if (host != null) {
                switch (host.tag) {
                    case 0:
                    case 2: {
                        matchResult_2 = 0;
                        break;
                    }
                    default:
                        matchResult_2 = 1;
                }
            }
            else {
                matchResult_2 = 1;
            }
            switch (matchResult_2) {
                case 0:
                    return [model, singleton((dispatch_7) => {
                        dispatch_7(new Msg_1(15, [new Msg_2(23, [msg.fields[0]])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 5: {
            let matchResult_3;
            if (host != null) {
                switch (host.tag) {
                    case 1: {
                        matchResult_3 = 0;
                        break;
                    }
                    case 0: {
                        matchResult_3 = 1;
                        break;
                    }
                    default:
                        matchResult_3 = 2;
                }
            }
            else {
                matchResult_3 = 2;
            }
            switch (matchResult_3) {
                case 0: {
                    window.alert(some("Not implemented"));
                    return [model, Cmd_none()];
                }
                case 1:
                    return [model, singleton((dispatch_8) => {
                        dispatch_8(new Msg_1(15, [new Msg_2(24, [msg.fields[0]])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 7: {
            let matchResult_4;
            if (host != null) {
                switch (host.tag) {
                    case 0:
                    case 2: {
                        matchResult_4 = 0;
                        break;
                    }
                    default:
                        matchResult_4 = 1;
                }
            }
            else {
                matchResult_4 = 1;
            }
            switch (matchResult_4) {
                case 0:
                    return [model, singleton((dispatch_9) => {
                        dispatch_9(new Msg_1(15, [new Msg_2(26, [msg.fields[0]])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 8: {
            let matchResult_5;
            if (host != null) {
                switch (host.tag) {
                    case 1:
                    case 2: {
                        matchResult_5 = 0;
                        break;
                    }
                    default:
                        matchResult_5 = 1;
                }
            }
            else {
                matchResult_5 = 1;
            }
            switch (matchResult_5) {
                case 0:
                    return [model, singleton((dispatch_10) => {
                        dispatch_10(new Msg_1(6, [new Msg(15, [msg.fields[0]])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 2: {
            let matchResult_6;
            if (host != null) {
                switch (host.tag) {
                    case 0:
                    case 2: {
                        matchResult_6 = 1;
                        break;
                    }
                    default:
                        matchResult_6 = 0;
                }
            }
            else {
                matchResult_6 = 2;
            }
            switch (matchResult_6) {
                case 0:
                    return [model, singleton((dispatch_11) => {
                        dispatch_11(new Msg_1(6, [new Msg(7, [])]));
                    })];
                case 1: {
                    if (isEmpty(model.SpreadsheetModel.SelectedCells)) {
                        throw new Error("No column selected");
                    }
                    const distinct = Array_distinct(unzip(toArray(model.SpreadsheetModel.SelectedCells))[0], {
                        Equals: (x, y) => (x === y),
                        GetHashCode: numberHash,
                    });
                    return [model, (distinct.length !== 1) ? ((msg_19 = Operators_Failure("Please select one column only if you want to use `Remove Building Block`."), (msg_20 = (new Msg_1(3, [new DevMsg(3, [Cmd_none(), msg_19])])), singleton((dispatch_12) => {
                        dispatch_12(msg_20);
                    })))) : ((msg_22 = (new Msg_1(15, [new Msg_2(11, [distinct[0]])])), singleton((dispatch_13) => {
                        dispatch_13(msg_22);
                    })))];
                }
                default:
                    throw new Error("not implemented");
            }
        }
        case 9: {
            let matchResult_7;
            if (host != null) {
                switch (host.tag) {
                    case 1: {
                        matchResult_7 = 0;
                        break;
                    }
                    case 0: {
                        matchResult_7 = 1;
                        break;
                    }
                    default:
                        matchResult_7 = 2;
                }
            }
            else {
                matchResult_7 = 2;
            }
            switch (matchResult_7) {
                case 0:
                    return [model, singleton((dispatch_14) => {
                        dispatch_14(new Msg_1(11, [new Msg_3(8, [])]));
                    })];
                case 1:
                    return [model, singleton((dispatch_15) => {
                        dispatch_15(new Msg_1(15, [new Msg_2(30, [])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 10: {
            let matchResult_8;
            if (host != null) {
                switch (host.tag) {
                    case 1: {
                        matchResult_8 = 0;
                        break;
                    }
                    case 0: {
                        matchResult_8 = 1;
                        break;
                    }
                    default:
                        matchResult_8 = 2;
                }
            }
            else {
                matchResult_8 = 2;
            }
            switch (matchResult_8) {
                case 0:
                    return [model, singleton((dispatch_16) => {
                        dispatch_16(new Msg_1(11, [new Msg_3(11, [])]));
                    })];
                case 1:
                    return [model, singleton((dispatch_17) => {
                        dispatch_17(new Msg_1(15, [new Msg_2(31, [])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 11: {
            let matchResult_9;
            if (host != null) {
                switch (host.tag) {
                    case 0:
                    case 2: {
                        matchResult_9 = 1;
                        break;
                    }
                    default:
                        matchResult_9 = 0;
                }
            }
            else {
                matchResult_9 = 2;
            }
            switch (matchResult_9) {
                case 0:
                    return [model, singleton((dispatch_18) => {
                        dispatch_18(new Msg_1(16, [new Msg_4(1, [])]));
                    })];
                case 1:
                    return [model, singleton((dispatch_19) => {
                        dispatch_19(new Msg_1(15, [new Msg_2(34, [])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 6: {
            let matchResult_10;
            if (host != null) {
                if (host.tag === 1) {
                    matchResult_10 = 0;
                }
                else {
                    matchResult_10 = 1;
                }
            }
            else {
                matchResult_10 = 1;
            }
            switch (matchResult_10) {
                case 0:
                    return [model, singleton((dispatch_20) => {
                        dispatch_20(new Msg_1(6, [new Msg(14, [])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 12: {
            let matchResult_11;
            if (host != null) {
                switch (host.tag) {
                    case 0:
                    case 2: {
                        matchResult_11 = 1;
                        break;
                    }
                    default:
                        matchResult_11 = 0;
                }
            }
            else {
                matchResult_11 = 2;
            }
            switch (matchResult_11) {
                case 0:
                    return [model, singleton((dispatch_21) => {
                        dispatch_21(new Msg_1(6, [new Msg(11, [])]));
                    })];
                case 1:
                    return [model, singleton((dispatch_22) => {
                        dispatch_22(new Msg_1(15, [new Msg_2(28, [])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        case 13: {
            const terms = msg.fields[0];
            let matchResult_12;
            if (host != null) {
                switch (host.tag) {
                    case 0:
                    case 2: {
                        matchResult_12 = 1;
                        break;
                    }
                    default:
                        matchResult_12 = 0;
                }
            }
            else {
                matchResult_12 = 2;
            }
            switch (matchResult_12) {
                case 0:
                    return [model, singleton((dispatch_23) => {
                        dispatch_23(new Msg_1(6, [new Msg(12, [terms])]));
                    })];
                case 1:
                    return [model, singleton((dispatch_24) => {
                        dispatch_24(new Msg_1(15, [new Msg_2(29, [terms])]));
                    })];
                default:
                    throw new Error("not implemented");
            }
        }
        default:
            return [model, Cmd_batch(toList(delay(() => append(singleton_1(singleton((dispatch) => {
                dispatch(new Msg_1(2, [new ApiMsg(0, [new ApiRequestMsg(6, [])])]));
            })), delay(() => append(singleton_1(singleton((dispatch_1) => {
                dispatch_1(new Msg_1(2, [new ApiMsg(0, [new ApiRequestMsg(4, [])])]));
            })), delay(() => {
                const matchValue = msg.fields[0];
                return (matchValue.tag === 0) ? singleton_1(Cmd_batch(singleton(Cmd_ofEffect((dispatch_2) => {
                    addOnKeydownEvent(dispatch_2);
                })))) : ((matchValue.tag === 2) ? singleton_1(Cmd_batch(ofArray([Cmd_ofEffect((dispatch_3) => {
                    addOnKeydownEvent(dispatch_3);
                }), Cmd_ofEffect((_arg) => {
                    send(new Msg_5(0, []));
                })]))) : singleton_1(Cmd_OfPromise_either(tryFindActiveAnnotationTable, void 0, (arg) => (new Msg_1(6, [new Msg(2, [arg])])), (arg_1) => (new Msg_1(3, [curry((tupledArg) => (new DevMsg(3, [tupledArg[0], tupledArg[1]])), Cmd_none(), arg_1)])))));
            })))))))];
    }
}

//# sourceMappingURL=InterfaceUpdate.js.map
