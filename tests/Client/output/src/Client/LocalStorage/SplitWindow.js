import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { record_type, float64_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { Convert_fromJson, Convert_serialize } from "../../../fable_modules/Fable.SimpleJson.3.24.0/Json.Converter.fs.js";
import { createTypeInfo } from "../../../fable_modules/Fable.SimpleJson.3.24.0/TypeInfo.Converter.fs.js";
import { printf, toConsole } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { SimpleJson_tryParse } from "../../../fable_modules/Fable.SimpleJson.3.24.0/SimpleJson.fs.js";

export class SplitWindow extends Record {
    constructor(ScrollbarWidth, RightWindowWidth) {
        super();
        this.ScrollbarWidth = ScrollbarWidth;
        this.RightWindowWidth = RightWindowWidth;
    }
}

export function SplitWindow_$reflection() {
    return record_type("LocalStorage.SplitWindow.SplitWindow", [], SplitWindow, () => [["ScrollbarWidth", float64_type], ["RightWindowWidth", float64_type]]);
}

function LocalStorage_write(m) {
    const jsonString = Convert_serialize(m, createTypeInfo(SplitWindow_$reflection()));
    localStorage.setItem("SplitWindow", jsonString);
}

function LocalStorage_load() {
    let matchValue;
    try {
        return (matchValue = SimpleJson_tryParse(localStorage.getItem("SplitWindow")), (matchValue != null) ? Convert_fromJson(matchValue, createTypeInfo(SplitWindow_$reflection())) : (() => {
            throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
        })());
    }
    catch (matchValue_1) {
        localStorage.removeItem("SplitWindow");
        toConsole(printf("Could not find %s"))("SplitWindow");
        return void 0;
    }
}

export function SplitWindow_init() {
    const tryFromStorage = LocalStorage_load();
    if (tryFromStorage == null) {
        let InitScrollbarWidth = 0;
        let setInitWidth;
        const scrollDiv = document.createElement("div");
        scrollDiv.className = "scrollbar-measure";
        document.body.appendChild(scrollDiv);
        const sw = scrollDiv.offsetWidth - scrollDiv.clientWidth;
        InitScrollbarWidth = sw;
        setInitWidth = scrollDiv.remove();
        return new SplitWindow(InitScrollbarWidth, 400);
    }
    else {
        return tryFromStorage;
    }
}

export function SplitWindow__WriteToLocalStorage(this$) {
    LocalStorage_write(this$);
}

//# sourceMappingURL=SplitWindow.js.map
