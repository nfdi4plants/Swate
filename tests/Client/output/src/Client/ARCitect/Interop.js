import { tryFind } from "../../../fable_modules/fable-library.4.9.0/Array.js";
import { getValue, getRecordElements, name } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { IEventHandler_$reflection } from "../States/ARCitect.js";
import { value, some } from "../../../fable_modules/fable-library.4.9.0/Option.js";

export function verifyARCitectMsg(e) {
    const content = e.data;
    const source = e.source;
    if (content.swate) {
        return content;
    }
    else {
        return void 0;
    }
}

/**
 * Returns a function to remove the event listener
 */
export function initEventListener(eventHandler) {
    const handle = (e) => {
        const matchValue = verifyARCitectMsg(e);
        if (matchValue == null) {
        }
        else {
            const content = matchValue;
            let func;
            const matchValue_1 = tryFind((t) => (name(t) === content.api), getRecordElements(IEventHandler_$reflection()));
            func = ((matchValue_1 == null) ? void 0 : some(getValue(matchValue_1, eventHandler)));
            if (func == null) {
            }
            else {
                value(func)(content.data);
            }
        }
    };
    window.addEventListener("message", handle);
    return () => {
        window.removeEventListener("message", handle);
    };
}

//# sourceMappingURL=Interop.js.map
