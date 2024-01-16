import { some } from "../../fable_modules/fable-library.4.9.0/Option.js";
import { printf, toConsole, toText } from "../../fable_modules/fable-library.4.9.0/String.js";
import { tryGetValue } from "../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { FSharpRef } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { Dictionary } from "../../fable_modules/fable-library.4.9.0/MutableMap.js";
import { HashIdentity_Structural } from "../../fable_modules/fable-library.4.9.0/FSharp.Collections.js";

export function log(a) {
    console.log(some(a));
}

export function logf(a, b) {
    log(toText(a)(b));
}

export function debounce(storage, key, timeout, fn, value) {
    const key_1 = key;
    let matchValue;
    let outArg = 0;
    matchValue = [tryGetValue(storage, key_1, new FSharpRef(() => outArg, (v) => {
        outArg = (v | 0);
    })), outArg];
    if (matchValue[0]) {
        toConsole(printf("CLEAR"));
        clearTimeout(matchValue[1]);
    }
    else {
        toConsole(printf("Not clear"));
    }
    const timeoutId_1 = setTimeout(() => {
        storage.delete(key_1);
        fn(value);
    }, timeout) | 0;
    storage.set(key_1, timeoutId_1);
}

export function debouncel(storage, key, timeout, setLoading, fn, value) {
    const key_1 = key;
    let matchValue;
    let outArg = 0;
    matchValue = [tryGetValue(storage, key_1, new FSharpRef(() => outArg, (v) => {
        outArg = (v | 0);
    })), outArg];
    if (matchValue[0]) {
        clearTimeout(matchValue[1]);
    }
    else {
        setLoading(true);
    }
    const timeoutId_1 = setTimeout(() => {
        storage.delete(key_1);
        setLoading(false);
        fn(value);
    }, timeout) | 0;
    storage.set(key_1, timeoutId_1);
}

export function newDebounceStorage() {
    return new Dictionary([], HashIdentity_Structural());
}

//# sourceMappingURL=Helper.js.map
