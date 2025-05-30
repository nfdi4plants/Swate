import { min } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Double.js";
import { int32 } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Int32.js";
import { take } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Array.js";
import { Option, some } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Option.js";

/**
 * Take "count" many items from array if existing. if not enough items return as many as possible
 */
export function Array_takeSafe<a>(count: int32, array: a[]): a[] {
    const count_1: int32 = min(count, array.length) | 0;
    return take<a>(count_1, array);
}

/**
 * If function returns `true` then return `Some x` otherwise `None`
 */
export function Option_where<$a>(f: ((arg0: $a) => boolean), x: $a): Option<$a> {
    if (f(x)) {
        return some(x);
    }
    else {
        return undefined;
    }
}

/**
 * If function return `true` then return `None` otherwise `Some x`
 */
export function Option_whereNot<$a>(f: ((arg0: $a) => boolean), x: $a): Option<$a> {
    if (f(x)) {
        return undefined;
    }
    else {
        return some(x);
    }
}

