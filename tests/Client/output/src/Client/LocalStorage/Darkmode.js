import { toString, Record, Union } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, lambda_type, unit_type, union_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { toText, printf, toConsole } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { equals } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { createElement } from "react";
import { React_createContext_Z10F951C2 } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";

export class DataTheme extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Dark", "Light"];
    }
}

export function DataTheme_$reflection() {
    return union_type("LocalStorage.Darkmode.DataTheme", [], DataTheme, () => [[], []]);
}

export class State extends Record {
    constructor(Theme, SetTheme) {
        super();
        this.Theme = Theme;
        this.SetTheme = SetTheme;
    }
}

export function State_$reflection() {
    return record_type("LocalStorage.Darkmode.State", [], State, () => [["Theme", DataTheme_$reflection()], ["SetTheme", lambda_type(State_$reflection(), unit_type)]]);
}

function Attribute_getDataTheme() {
    const v = document.documentElement.getAttribute("data-theme");
    if (v == null) {
        return void 0;
    }
    else {
        return DataTheme_ofString_Z721C83C5(v);
    }
}

function Attribute_setDataTheme(theme) {
    document.documentElement.setAttribute("data-theme", theme.toLocaleLowerCase());
}

function BrowserSetting_getDefault() {
    if ((window.matchMedia("(prefers-color-scheme: dark)")).matches) {
        return new DataTheme(0, []);
    }
    else {
        return new DataTheme(1, []);
    }
}

function LocalStorage_write(dt) {
    const s = toString(dt);
    localStorage.setItem("DataTheme", s);
}

function LocalStorage_load() {
    try {
        return DataTheme_ofString_Z721C83C5(localStorage.getItem("DataTheme"));
    }
    catch (matchValue) {
        localStorage.removeItem("DataTheme");
        toConsole(printf("Could not find %s"))("DataTheme");
        return void 0;
    }
}

export function DataTheme_ofString_Z721C83C5(str) {
    const matchValue = str.toLocaleLowerCase();
    switch (matchValue) {
        case "dark":
            return new DataTheme(0, []);
        default:
            return new DataTheme(1, []);
    }
}

export function DataTheme_SET_EA9902F(theme) {
    Attribute_setDataTheme(toString(theme));
    LocalStorage_write(theme);
}

export function DataTheme_GET() {
    const localStorage = LocalStorage_load();
    const dataTheme = Attribute_getDataTheme();
    if (dataTheme != null) {
        return dataTheme;
    }
    else if (localStorage != null) {
        return localStorage;
    }
    else {
        return BrowserSetting_getDefault();
    }
}

export function DataTheme__get_isDark(this$) {
    return equals(this$, new DataTheme(0, []));
}

export function DataTheme__get_toIcon(this$) {
    const c = (this$.tag === 0) ? "fa-solid fa-moon" : "fa-solid fa-lightbulb";
    return createElement("i", {
        className: toText(printf("%s fa-xl"))(c),
    });
}

export function State_init() {
    const dt = DataTheme_GET();
    DataTheme_SET_EA9902F(dt);
    return new State(dt, (state) => {
        throw new Error("This is not implemented and serves as placeholder");
    });
}

export const themeContext = React_createContext_Z10F951C2("Theme", State_init());

//# sourceMappingURL=Darkmode.js.map
