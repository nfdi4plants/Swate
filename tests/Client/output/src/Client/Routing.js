import { Union } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { union_type, option_type, int32_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";
import { contains } from "../../fable_modules/fable-library.4.9.0/Array.js";
import { createObj, safeHash, equals } from "../../fable_modules/fable-library.4.9.0/Util.js";
import { createElement } from "react";
import { Helpers_combineClasses } from "../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Interop_reactApi } from "../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { collect, singleton, ofArray } from "../../fable_modules/fable-library.4.9.0/List.js";
import { oneOf, intParam, s, map } from "../../fable_modules/Fable.Elmish.UrlParser.1.0.2/parser.fs.js";
import { parsePath } from "../../fable_modules/Fable.Elmish.Browser.4.0.3/parser.fs.js";

export class Route extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Home", "BuildingBlock", "TermSearch", "FilePicker", "Info", "Protocol", "ProtocolSearch", "Dag", "JsonExport", "TemplateMetadata", "ActivityLog", "Settings", "NotFound"];
    }
}

export function Route_$reflection() {
    return union_type("Routing.Route", [], Route, () => [[["Item", option_type(int32_type)]], [], [], [], [], [], [], [], [], [], [], [], []]);
}

export function Route__get_toStringRdbl(this$) {
    switch (this$.tag) {
        case 2:
            return "Terms";
        case 3:
            return "File Picker";
        case 5:
            return "Templates";
        case 6:
            return "Template Search";
        case 7:
            return "Directed Acylclic Graph";
        case 8:
            return "Json Export";
        case 9:
            return "Template Metadata";
        case 4:
            return "Info";
        case 10:
            return "Activity Log";
        case 11:
            return "Settings";
        case 12:
            return "NotFound";
        default:
            return "Building Blocks";
    }
}

export function Route__get_isExpert(this$) {
    switch (this$.tag) {
        case 9:
        case 8:
            return true;
        default:
            return false;
    }
}

export function Route__isActive_Z5040D2F2(this$, currentRoute) {
    return contains(currentRoute, (this$.tag === 5) ? [new Route(5, []), new Route(6, [])] : [this$], {
        Equals: equals,
        GetHashCode: safeHash,
    });
}

export function Route_toIcon_Z5040D2F2(p) {
    const createElem = (icons, name) => createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["title", name], ["children", Interop_reactApi.Children.toArray(Array.from(icons))]]))));
    switch (p.tag) {
        case 2:
            return createElem(singleton(createElement("i", {
                className: "fa-solid fa-magnifying-glass-plus",
            })), Route__get_toStringRdbl(p));
        case 1:
            return createElem(ofArray([createElement("i", {
                className: "fa-solid fa-circle-plus",
            }), createElement("i", {
                className: "fa-solid fa-table-columns",
            })]), Route__get_toStringRdbl(p));
        case 5:
            return createElem(ofArray([createElement("i", {
                className: "fa-solid fa-circle-plus",
            }), createElement("i", {
                className: "fa-solid fa-table",
            })]), Route__get_toStringRdbl(p));
        case 6:
            return createElem(ofArray([createElement("i", {
                className: "fa-solid fa-table",
            }), createElement("i", {
                className: "fa-solid fa-magnifying-glass",
            })]), Route__get_toStringRdbl(p));
        case 7:
            return createElem(singleton(createElement("i", {
                className: "fa-solid fa-diagram-project",
            })), Route__get_toStringRdbl(p));
        case 8:
            return createElem(singleton(createElement("i", {
                className: "fa-solid fa-file-export",
            })), Route__get_toStringRdbl(p));
        case 9:
            return createElem(ofArray([createElement("i", {
                className: "fa-solid fa-circle-plus",
            }), createElement("i", {
                className: "fa-solid fa-table",
            })]), Route__get_toStringRdbl(p));
        case 3:
            return createElem(singleton(createElement("i", {
                className: "fa-solid fa-upload",
            })), Route__get_toStringRdbl(p));
        case 10:
            return createElem(singleton(createElement("i", {
                className: "fa-solid fa-timeline",
            })), Route__get_toStringRdbl(p));
        case 4:
            return createElem(singleton(createElement("i", {
                className: "fa-solid fa-question",
            })), Route__get_toStringRdbl(p));
        default:
            return createElement("i", {
                className: "fa-question",
            });
    }
}

export const Routing_route = (() => {
    let parser, queryParser, parseBefore, parseAfter, parseBefore_2, parseAfter_2, parseBefore_4, parseAfter_4;
    const parsers = ofArray([map((Item) => (new Route(0, [Item])), (parser = s(""), (queryParser = intParam("is_swatehost"), (state) => collect(queryParser, parser(state))))), map(new Route(2, []), s("TermSearch")), map(new Route(1, []), s("BuildingBlock")), map(new Route(3, []), s("FilePicker")), map(new Route(4, []), s("Info")), map(new Route(5, []), s("ProtocolInsert")), map(new Route(6, []), (parseBefore = s("Protocol"), (parseAfter = s("Search"), (state_2) => collect(parseAfter, parseBefore(state_2))))), map(new Route(7, []), s("Dag")), map(new Route(8, []), (parseBefore_2 = s("Experts"), (parseAfter_2 = s("JsonExport"), (state_4) => collect(parseAfter_2, parseBefore_2(state_4))))), map(new Route(9, []), (parseBefore_4 = s("Experts"), (parseAfter_4 = s("TemplateMetadata"), (state_6) => collect(parseAfter_4, parseBefore_4(state_6))))), map(new Route(10, []), s("ActivityLog")), map(new Route(11, []), s("Settings")), map(new Route(12, []), s("NotFound")), map(new Route(1, []), s("Core"))]);
    return (state_8) => oneOf(parsers, state_8);
})();

export function Routing_parsePath(location) {
    return parsePath(Routing_route, location);
}

//# sourceMappingURL=Routing.js.map
