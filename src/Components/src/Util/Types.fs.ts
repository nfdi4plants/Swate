import { Record, Union } from "../fable_modules/fable-library-ts.4.24.0/Types.js";
import { obj_type, string_type, record_type, lambda_type, unit_type, class_type, bool_type, anonRecord_type, int32_type, union_type, TypeInfo } from "../fable_modules/fable-library-ts.4.24.0/Reflection.js";
import { int32 } from "../fable_modules/fable-library-ts.4.24.0/Int32.js";
import { map, orElse, unwrap, some, value as value_1, Option } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { Option_whereNot } from "../../../Shared/Extensions.fs.js";
import { tryFind, empty, singleton, append, delay, toList, isEmpty } from "../fable_modules/fable-library-ts.4.24.0/Seq.js";
import { Comment$ } from "../fable_modules/ARCtrl.Core.2.5.1/Comment.fs.js";
import { OntologyAnnotation } from "../fable_modules/ARCtrl.Core.2.5.1/OntologyAnnotation.fs.js";
import { SimpleJson_tryParse } from "../fable_modules/Fable.SimpleJson.3.24.0/./SimpleJson.fs.js";
import { Json_$union } from "../fable_modules/Fable.SimpleJson.3.24.0/Json.fs.js";
import { Convert_fromJson } from "../fable_modules/Fable.SimpleJson.3.24.0/./Json.Converter.fs.js";
import { createTypeInfo } from "../fable_modules/Fable.SimpleJson.3.24.0/./TypeInfo.Converter.fs.js";
import { isArrayLike, equals } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { join, isNullOrWhiteSpace } from "../fable_modules/fable-library-ts.4.24.0/String.js";

export type DaisyUIColors_$union = 
    | DaisyUIColors<0>
    | DaisyUIColors<1>
    | DaisyUIColors<2>
    | DaisyUIColors<3>
    | DaisyUIColors<4>
    | DaisyUIColors<5>
    | DaisyUIColors<6>

export type DaisyUIColors_$cases = {
    0: ["Primary", []],
    1: ["Secondary", []],
    2: ["Accent", []],
    3: ["Info", []],
    4: ["Success", []],
    5: ["Warning", []],
    6: ["Error", []]
}

export function DaisyUIColors_Primary() {
    return new DaisyUIColors<0>(0, []);
}

export function DaisyUIColors_Secondary() {
    return new DaisyUIColors<1>(1, []);
}

export function DaisyUIColors_Accent() {
    return new DaisyUIColors<2>(2, []);
}

export function DaisyUIColors_Info() {
    return new DaisyUIColors<3>(3, []);
}

export function DaisyUIColors_Success() {
    return new DaisyUIColors<4>(4, []);
}

export function DaisyUIColors_Warning() {
    return new DaisyUIColors<5>(5, []);
}

export function DaisyUIColors_Error() {
    return new DaisyUIColors<6>(6, []);
}

export class DaisyUIColors<Tag extends keyof DaisyUIColors_$cases> extends Union<Tag, DaisyUIColors_$cases[Tag][0]> {
    constructor(readonly tag: Tag, readonly fields: DaisyUIColors_$cases[Tag][1]) {
        super();
    }
    cases() {
        return ["Primary", "Secondary", "Accent", "Info", "Success", "Warning", "Error"];
    }
}

export function DaisyUIColors_$reflection(): TypeInfo {
    return union_type("Swate.Components.DaisyUIColors", [], DaisyUIColors, () => [[], [], [], [], [], [], []]);
}

export class TableCellController extends Record {
    readonly Index: { x: int32, y: int32 };
    readonly IsActive: boolean;
    readonly IsSelected: boolean;
    readonly IsOrigin: boolean;
    readonly onKeyDown: ((arg0: KeyboardEvent) => void);
    readonly onBlur: ((arg0: FocusEvent) => void);
    readonly onClick: ((arg0: MouseEvent) => void);
    constructor(Index: { x: int32, y: int32 }, IsActive: boolean, IsSelected: boolean, IsOrigin: boolean, onKeyDown: ((arg0: KeyboardEvent) => void), onBlur: ((arg0: FocusEvent) => void), onClick: ((arg0: MouseEvent) => void)) {
        super();
        this.Index = Index;
        this.IsActive = IsActive;
        this.IsSelected = IsSelected;
        this.IsOrigin = IsOrigin;
        this.onKeyDown = onKeyDown;
        this.onBlur = onBlur;
        this.onClick = onClick;
    }
}

export function TableCellController_$reflection(): TypeInfo {
    return record_type("Swate.Components.TableCellController", [], TableCellController, () => [["Index", anonRecord_type(["x", int32_type], ["y", int32_type])], ["IsActive", bool_type], ["IsSelected", bool_type], ["IsOrigin", bool_type], ["onKeyDown", lambda_type(class_type("Browser.Types.KeyboardEvent", undefined), unit_type)], ["onBlur", lambda_type(class_type("Browser.Types.FocusEvent", undefined), unit_type)], ["onClick", lambda_type(class_type("Browser.Types.MouseEvent", undefined), unit_type)]]);
}

export function TableCellController_init_Z95CEA69(index: { x: int32, y: int32 }, isActive: boolean, isSelected: boolean, isOrigin: boolean, onKeyDown: ((arg0: KeyboardEvent) => void), onBlur: ((arg0: FocusEvent) => void), onClick: ((arg0: MouseEvent) => void)): TableCellController {
    return new TableCellController(index, isActive, isSelected, isOrigin, onKeyDown, onBlur, onClick);
}

export function TermModule_joinLeft(t1: Term, t2: Term): Term {
    let d2: any, d1_1: any, d2_1: any, d1: any;
    let data: Option<any>;
    const matchValue: Option<any> = t1.data;
    const matchValue_1: Option<any> = t2.data;
    data = ((matchValue == null) ? ((matchValue_1 == null) ? undefined : ((d2 = value_1(matchValue_1), some(d2)))) : ((matchValue_1 != null) ? ((d1_1 = value_1(matchValue), (d2_1 = value_1(matchValue_1), some(Object.assign({}, d1_1, d2_1))))) : ((d1 = value_1(matchValue), some(d1)))));
    return {
        name: unwrap(orElse(t1.name, t2.name)),
        id: unwrap(orElse(t1.id, t2.id)),
        description: unwrap(orElse(t1.description, t2.description)),
        source: unwrap(orElse(t1.source, t2.source)),
        href: unwrap(orElse(t1.href, t2.href)),
        isObsolete: unwrap(orElse(t1.isObsolete, t2.isObsolete)),
        data: data,
    };
}

export function TermModule_joinRight(t1: Term, t2: Term): Term {
    let d2: any, d1_1: any, d2_1: any, d1: any;
    let data: Option<any>;
    const matchValue: Option<any> = t1.data;
    const matchValue_1: Option<any> = t2.data;
    data = ((matchValue == null) ? ((matchValue_1 == null) ? undefined : ((d2 = value_1(matchValue_1), some(d2)))) : ((matchValue_1 != null) ? ((d1_1 = value_1(matchValue), (d2_1 = value_1(matchValue_1), some(Object.assign({}, d1_1, d2_1))))) : ((d1 = value_1(matchValue), some(d1)))));
    return {
        name: unwrap(orElse(t2.name, t1.name)),
        id: unwrap(orElse(t2.id, t1.id)),
        description: unwrap(orElse(t2.description, t1.description)),
        source: unwrap(orElse(t2.source, t1.source)),
        href: unwrap(orElse(t2.href, t1.href)),
        isObsolete: unwrap(orElse(t2.isObsolete, t1.isObsolete)),
        data: data,
    };
}

export function TermModule_toOntologyAnnotation(term: Term): OntologyAnnotation {
    const comments: Option<Comment$[]> = Option_whereNot<Comment$[]>(isEmpty, Array.from(toList<Comment$>(delay<Comment$>((): Iterable<Comment$> => append<Comment$>((term.description != null) ? singleton<Comment$>(new Comment$("description", JSON.stringify(value_1(term.description)))) : empty<Comment$>(), delay<Comment$>((): Iterable<Comment$> => append<Comment$>((term.data != null) ? singleton<Comment$>(new Comment$("data", JSON.stringify(value_1(term.data)))) : empty<Comment$>(), delay<Comment$>((): Iterable<Comment$> => append<Comment$>((term.source != null) ? singleton<Comment$>(new Comment$("source", JSON.stringify(value_1(term.source)))) : empty<Comment$>(), delay<Comment$>((): Iterable<Comment$> => ((term.isObsolete != null) ? singleton<Comment$>(new Comment$("isObsolete", JSON.stringify(value_1(term.isObsolete)))) : empty<Comment$>())))))))))));
    return new OntologyAnnotation(unwrap(term.name), unwrap(term.source), unwrap(term.id), unwrap(comments));
}

export function TermModule_fromOntologyAnnotation(oa: OntologyAnnotation): Term {
    const description: Option<string> = map<Comment$, string>((c_1: Comment$): string => {
        const matchValue: Option<Json_$union> = SimpleJson_tryParse(value_1(c_1.Value));
        if (matchValue != null) {
            return Convert_fromJson<string>(value_1(matchValue), createTypeInfo(string_type));
        }
        else {
            throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
        }
    }, tryFind<Comment$>((c: Comment$): boolean => equals(c.Name, "description"), oa.Comments));
    const data: Option<any> = map<Comment$, any>((c_3: Comment$): any => {
        const matchValue_1: Option<Json_$union> = SimpleJson_tryParse(value_1(c_3.Value));
        if (matchValue_1 != null) {
            return Convert_fromJson<any>(value_1(matchValue_1), createTypeInfo(obj_type));
        }
        else {
            throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
        }
    }, tryFind<Comment$>((c_2: Comment$): boolean => equals(c_2.Name, "data"), oa.Comments));
    const source_3: Option<string> = map<Comment$, string>((c_5: Comment$): string => {
        const matchValue_2: Option<Json_$union> = SimpleJson_tryParse(value_1(c_5.Value));
        if (matchValue_2 != null) {
            return Convert_fromJson<string>(value_1(matchValue_2), createTypeInfo(string_type));
        }
        else {
            throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
        }
    }, tryFind<Comment$>((c_4: Comment$): boolean => equals(c_4.Name, "source"), oa.Comments));
    const isObsolete: Option<boolean> = map<Comment$, boolean>((c_7: Comment$): boolean => {
        const matchValue_3: Option<Json_$union> = SimpleJson_tryParse(value_1(c_7.Value));
        if (matchValue_3 != null) {
            return Convert_fromJson<boolean>(value_1(matchValue_3), createTypeInfo(bool_type));
        }
        else {
            throw new Error("Couldn\'t parse the input JSON string because it seems to be invalid");
        }
    }, tryFind<Comment$>((c_6: Comment$): boolean => equals(c_6.Name, "isObsolete"), oa.Comments));
    return {
        name: unwrap(oa.Name),
        id: unwrap(oa.TermAccessionNumber),
        description: unwrap(description),
        source: unwrap(source_3),
        href: unwrap(Option_whereNot<string>(isNullOrWhiteSpace, oa.TermAccessionOntobeeUrl)),
        isObsolete: unwrap(isObsolete),
        data: data,
    };
}

export function TermSearchStyleModule_resolveStyle(style: string | string[]): string {
    if (isArrayLike(style)) {
        return join(" ", style as Iterable<string>);
    }
    else {
        return style;
    }
}

