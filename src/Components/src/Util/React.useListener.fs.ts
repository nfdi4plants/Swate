import { bind, Option, map } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { class_type, TypeInfo } from "../fable_modules/fable-library-ts.4.24.0/Reflection.js";

export const Impl_allowsPassiveEvents: boolean = (() => {
    let passive = false;
    try {
        if ((typeof window !== 'undefined') && (typeof window.addEventListener === 'function')) {
            const options: any = {
                passive: true,
            };
            window.addEventListener("testPassiveEventSupport", (value: Event): void => {
            }, options);
            window.removeEventListener("testPassiveEventSupport", (value_1: Event): void => {
            });
        }
    }
    catch (matchValue: any) {
    }
    return passive;
})();

export const Impl_defaultPassive: any = {
    passive: true,
};

export function Impl_adjustPassive(maybeOptions: Option<any>): Option<any> {
    return map<any, any>((options: any): any => {
        if (options.passive && !Impl_allowsPassiveEvents) {
            return {
                capture: options.capture,
                once: options.once,
                passive: false,
            };
        }
        else {
            return options;
        }
    }, maybeOptions);
}

export function Impl_createRemoveOptions(maybeOptions: Option<any>): Option<any> {
    return bind<any, any>((options: any): Option<any> => {
        if (options.capture) {
            return {
                capture: true,
            };
        }
        else {
            return undefined;
        }
    }, maybeOptions);
}

export class React {
    constructor() {
    }
}

export function React_$reflection(): TypeInfo {
    return class_type("Swate.Components.ReactHelper.React", undefined, React);
}

