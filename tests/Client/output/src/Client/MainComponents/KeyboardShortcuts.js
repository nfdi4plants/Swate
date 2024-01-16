import { Msg } from "../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../Messages.js";

function onKeydownEvent(dispatch, e) {
    const e_1 = e;
    const matchValue = e_1.ctrlKey;
    const matchValue_1 = e_1.which;
    if (matchValue) {
        const matchValue_3 = e_1.ctrlKey;
        const matchValue_4 = e_1.which;
        let matchResult;
        if (matchValue_3) {
            switch (matchValue_4) {
                case 67: {
                    matchResult = 0;
                    break;
                }
                case 88: {
                    matchResult = 1;
                    break;
                }
                case 86: {
                    matchResult = 2;
                    break;
                }
                default:
                    matchResult = 3;
            }
        }
        else {
            matchResult = 3;
        }
        switch (matchResult) {
            case 0: {
                dispatch(new Msg_1(15, [new Msg(12, [])]));
                break;
            }
            case 1: {
                dispatch(new Msg_1(15, [new Msg(13, [])]));
                break;
            }
            case 2: {
                dispatch(new Msg_1(15, [new Msg(14, [])]));
                break;
            }
            case 3: {
                break;
            }
        }
    }
}

/**
 * These events only get reapplied on reload, not during hot reload
 */
export function addOnKeydownEvent(dispatch) {
    document.body.removeEventListener("keydown", (e) => {
        onKeydownEvent(dispatch, e);
    });
    document.body.addEventListener("keydown", (e_1) => {
        onKeydownEvent(dispatch, e_1);
    });
}

//# sourceMappingURL=KeyboardShortcuts.js.map
