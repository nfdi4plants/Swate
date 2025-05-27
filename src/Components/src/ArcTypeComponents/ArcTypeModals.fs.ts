import { class_type, TypeInfo } from "../fable_modules/fable-library-ts.4.24.0/Reflection.js";
import { Option, map, unwrap, defaultArg } from "../fable_modules/fable-library-ts.4.24.0/Option.js";
import { ReactElement, createElement } from "react";
import React from "react";
import * as react from "react";
import { equals, createObj } from "../fable_modules/fable-library-ts.4.24.0/Util.js";
import { ofArray } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";
import { TermSearch } from "../TermSearch/TermSearch.fs.js";
import { OntologyAnnotation } from "../fable_modules/ARCtrl.Core.2.5.1/OntologyAnnotation.fs.js";
import { TermModule_toOntologyAnnotation, TermModule_fromOntologyAnnotation } from "../Util/Types.fs.js";
import { CompositeHeader_$union } from "../fable_modules/ARCtrl.Core.2.5.1/Table/CompositeHeader.fs.js";
import { BaseModal } from "../GenericComponents/BaseModal.fs.js";
import { Option_whereNot } from "../../../Shared/Extensions.fs.js";
import { isNullOrWhiteSpace } from "../fable_modules/fable-library-ts.4.24.0/String.js";
import { Data } from "../fable_modules/ARCtrl.Core.2.5.1/Data.fs.js";
import { CompositeCell_Term, CompositeCell_Data, CompositeCell_FreeText, CompositeCell_Unitized, CompositeCell_$union } from "../fable_modules/ARCtrl.Core.2.5.1/Table/CompositeCell.fs.js";

export function ArcTypeModalsUtil_inputKeydownHandler(e: KeyboardEvent, submit: (() => void), cancel: (() => void)): void {
    const matchValue: string = e.code;
    switch (matchValue) {
        case "Enter": {
            if (e.ctrlKey ? true : e.metaKey) {
                e.preventDefault();
                e.stopPropagation();
                submit();
            }
            break;
        }
        case "Escape": {
            e.preventDefault();
            e.stopPropagation();
            cancel();
            break;
        }
        default:
            undefined;
    }
}

export class InputField {
    constructor() {
    }
}

export function InputField_$reflection(): TypeInfo {
    return class_type("Swate.Components.InputField", undefined, InputField);
}

export function InputField_Input_Z7357B228(v: string, setter: ((arg0: string) => void), label: string, rmv: (() => void), submit: (() => void), autofocus?: boolean): ReactElement {
    let elems: Iterable<ReactElement>, value_10: string;
    const autofocus_1: boolean = defaultArg<boolean>(autofocus, false);
    return createElement<any>("div", createObj(ofArray([["className", "swt:flex swt:flex-col swt:gap-2"] as [string, any], (elems = [createElement<any>("label", {
        className: "swt:label",
        children: label,
    }), createElement<any>("input", createObj(ofArray([["className", "swt:input"] as [string, any], ["autoFocus", autofocus_1] as [string, any], (value_10 = v, ["ref", (e: Element): void => {
        if (!(e == null) && !equals(e.value, value_10)) {
            e.value = value_10;
        }
    }] as [string, any]), ["onChange", (ev: Event): void => {
        setter(ev.target.value);
    }] as [string, any], ["onKeyDown", (e_1: KeyboardEvent): void => {
        ArcTypeModalsUtil_inputKeydownHandler(e_1, submit, rmv);
    }] as [string, any]])))], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

export function InputField_TermCombi_85508A(v: Option<Term>, setter: ((arg0: Option<Term>) => void), label: string, rmv: (() => void), submit: (() => void), autofocus?: boolean, parentOa?: OntologyAnnotation): ReactElement {
    let elems: Iterable<ReactElement>;
    const autofocus_1: boolean = defaultArg<boolean>(autofocus, false);
    return createElement<any>("div", createObj(ofArray([["className", "swt:flex swt:flex-col swt:gap-2"] as [string, any], (elems = [createElement<any>("label", {
        className: "swt:label",
        children: label,
    }), createElement(TermSearch, {
        onTermSelect: setter,
        term: unwrap(v),
        parentId: unwrap(map<OntologyAnnotation, string>((oa: OntologyAnnotation): string => oa.TermAccessionShort, parentOa)),
        advancedSearch: true,
        onKeyDown: (e: KeyboardEvent): void => {
            ArcTypeModalsUtil_inputKeydownHandler(e, submit, rmv);
        },
        showDetails: true,
        portalModals: document.body,
        autoFocus: autofocus_1,
        classNames: {
            inputLabel: "swt:border-current",
        },
    })], ["children", reactApi.Children.toArray(Array.from(elems))] as [string, any])])));
}

export class FooterButtons {
    constructor() {
    }
}

export function FooterButtons_$reflection(): TypeInfo {
    return class_type("Swate.Components.FooterButtons", undefined, FooterButtons);
}

export function FooterButtons_Cancel_3A5B6456(rmv: (() => void)): ReactElement {
    return createElement<any>("button", {
        className: "swt:btn swt:btn-outline",
        children: "Cancel",
        onClick: (_arg: MouseEvent): void => {
            rmv();
        },
    });
}

export function FooterButtons_Submit_3A5B6456(submitOnClick: (() => void)): ReactElement {
    return createElement<any>("button", {
        className: "swt:btn swt:btn-primary swt:ml-auto",
        children: "Submit",
        onClick: (e: MouseEvent): void => {
            submitOnClick();
        },
    });
}

/**
 * pr is required to make indicators on termsearch not overflow
 * pl is required to make the input ouline when focused not cut of
 */
export function get_BaseModalContentClassOverride(): string {
    return "swt:overflow-y-auto swt:space-y-2 swt:pl-1 swt:pr-4 swt:py-1";
}

export function TermModal(oa: OntologyAnnotation, setOa: ((arg0: OntologyAnnotation) => void), rmv: (() => void), relevantCompositeHeader?: CompositeHeader_$union): ReactElement {
    let xs: Iterable<ReactElement>, xs_1: Iterable<ReactElement>;
    const initTerm: Term = TermModule_fromOntologyAnnotation(oa);
    const patternInput: [Term, ((arg0: Term) => void)] = reactApi.useState<Term, Term>(initTerm);
    const tempTerm: Term = patternInput[0];
    const setTempTerm: ((arg0: Term) => void) = patternInput[1];
    const submit = (): void => {
        setOa(TermModule_toOntologyAnnotation(tempTerm));
        rmv();
    };
    const parentOa: Option<OntologyAnnotation> = map<CompositeHeader_$union, OntologyAnnotation>((h: CompositeHeader_$union): OntologyAnnotation => h.ToTerm(), relevantCompositeHeader);
    return createElement(BaseModal, {
        rmv: (_arg: MouseEvent): void => {
            rmv();
        },
        header: createElement<any>("div", {
            children: ["Term"],
        }),
        content: (xs = [InputField_TermCombi_85508A(tempTerm, (t: Option<Term>): void => {
            setTempTerm(defaultArg(t, {}));
        }, "Term Name", rmv, submit, true, unwrap(parentOa)), InputField_Input_Z7357B228(defaultArg(tempTerm.source, ""), (input: string): void => {
            tempTerm.source = Option_whereNot<string>(isNullOrWhiteSpace, input);
            setTempTerm(tempTerm);
        }, "Source", rmv, submit), InputField_Input_Z7357B228(defaultArg(tempTerm.id, ""), (input_1: string): void => {
            tempTerm.id = Option_whereNot<string>(isNullOrWhiteSpace, input_1);
            setTempTerm(tempTerm);
        }, "Accession Number", rmv, submit)], react.createElement(react.Fragment, {}, ...xs)),
        contentClassInfo: get_BaseModalContentClassOverride(),
        footer: (xs_1 = [FooterButtons_Cancel_3A5B6456(rmv), FooterButtons_Submit_3A5B6456(submit)], react.createElement(react.Fragment, {}, ...xs_1)),
    });
}

export function UnitizedModal(v: string, oa: OntologyAnnotation, setUnitized: ((arg0: string, arg1: OntologyAnnotation) => void), rmv: (() => void), relevantCompositeHeader?: CompositeHeader_$union): ReactElement {
    let xs: Iterable<ReactElement>, xs_1: Iterable<ReactElement>;
    const initTerm: Term = TermModule_fromOntologyAnnotation(oa);
    const patternInput: [string, ((arg0: string) => void)] = reactApi.useState<string, string>(v);
    const tempValue: string = patternInput[0];
    const patternInput_1: [Term, ((arg0: Term) => void)] = reactApi.useState<Term, Term>(initTerm);
    const tempTerm: Term = patternInput_1[0];
    const setTempTerm: ((arg0: Term) => void) = patternInput_1[1];
    const submit = (): void => {
        setUnitized(tempValue, TermModule_toOntologyAnnotation(tempTerm));
        rmv();
    };
    const parentOa: Option<OntologyAnnotation> = map<CompositeHeader_$union, OntologyAnnotation>((h: CompositeHeader_$union): OntologyAnnotation => h.ToTerm(), relevantCompositeHeader);
    return createElement(BaseModal, {
        rmv: (_arg: MouseEvent): void => {
            rmv();
        },
        header: createElement<any>("div", {
            children: ["Unitized"],
        }),
        content: (xs = [InputField_Input_Z7357B228(tempValue, (input: string): void => {
            patternInput[1](input);
        }, "Value", rmv, submit, true), InputField_TermCombi_85508A(tempTerm, (t: Option<Term>): void => {
            setTempTerm(defaultArg(t, {}));
        }, "Term Name", rmv, submit, undefined, unwrap(parentOa)), InputField_Input_Z7357B228(defaultArg(tempTerm.source, ""), (input_1: string): void => {
            tempTerm.source = Option_whereNot<string>(isNullOrWhiteSpace, input_1);
            setTempTerm(tempTerm);
        }, "Source", rmv, submit), InputField_Input_Z7357B228(defaultArg(tempTerm.id, ""), (input_2: string): void => {
            tempTerm.id = Option_whereNot<string>(isNullOrWhiteSpace, input_2);
            setTempTerm(tempTerm);
        }, "Accession Number", rmv, submit)], react.createElement(react.Fragment, {}, ...xs)),
        contentClassInfo: get_BaseModalContentClassOverride(),
        footer: (xs_1 = [FooterButtons_Cancel_3A5B6456(rmv), FooterButtons_Submit_3A5B6456(submit)], react.createElement(react.Fragment, {}, ...xs_1)),
    });
}

export function FreeTextModal(v: string, setV: ((arg0: string) => void), rmv: (() => void), relevantCompositeHeader?: CompositeHeader_$union): ReactElement {
    let xs: Iterable<ReactElement>, xs_1: Iterable<ReactElement>;
    const patternInput: [string, ((arg0: string) => void)] = reactApi.useState<string, string>(v);
    const tempValue: string = patternInput[0];
    const submit = (): void => {
        setV(tempValue);
        rmv();
    };
    return createElement(BaseModal, {
        rmv: (_arg: MouseEvent): void => {
            rmv();
        },
        header: createElement<any>("div", {
            children: ["Freetext"],
        }),
        content: (xs = [InputField_Input_Z7357B228(tempValue, (input: string): void => {
            patternInput[1](input);
        }, "Value", rmv, submit, true)], react.createElement(react.Fragment, {}, ...xs)),
        contentClassInfo: get_BaseModalContentClassOverride(),
        footer: (xs_1 = [FooterButtons_Cancel_3A5B6456(rmv), FooterButtons_Submit_3A5B6456(submit)], react.createElement(react.Fragment, {}, ...xs_1)),
    });
}

export function DataModal(v: Data, setData: ((arg0: Data) => void), rmv: (() => void), relevantCompositeHeader?: CompositeHeader_$union): ReactElement {
    let xs: Iterable<ReactElement>, xs_1: Iterable<ReactElement>;
    const patternInput: [Data, ((arg0: Data) => void)] = reactApi.useState<Data, Data>(v);
    const tempData: Data = patternInput[0];
    const setTempData: ((arg0: Data) => void) = patternInput[1];
    const submit = (): void => {
        setData(tempData);
        rmv();
    };
    return createElement(BaseModal, {
        rmv: (_arg: MouseEvent): void => {
            rmv();
        },
        header: createElement<any>("div", {
            children: ["Data"],
        }),
        content: (xs = [InputField_Input_Z7357B228(defaultArg(tempData.FilePath, ""), (input: string): void => {
            tempData.FilePath = Option_whereNot<string>(isNullOrWhiteSpace, input);
            setTempData(tempData);
        }, "File Path", rmv, submit, true), InputField_Input_Z7357B228(defaultArg(tempData.Selector, ""), (input_1: string): void => {
            tempData.Selector = Option_whereNot<string>(isNullOrWhiteSpace, input_1);
            setTempData(tempData);
        }, "Selector", rmv, submit), InputField_Input_Z7357B228(defaultArg(tempData.SelectorFormat, ""), (input_2: string): void => {
            tempData.SelectorFormat = Option_whereNot<string>(isNullOrWhiteSpace, input_2);
            setTempData(tempData);
        }, "Selector Format", rmv, submit)], react.createElement(react.Fragment, {}, ...xs)),
        contentClassInfo: get_BaseModalContentClassOverride(),
        footer: (xs_1 = [FooterButtons_Cancel_3A5B6456(rmv), FooterButtons_Submit_3A5B6456(submit)], react.createElement(react.Fragment, {}, ...xs_1)),
    });
}

export function CompositeCellModal(compositeCellModalInputProps: any): ReactElement {
    const relevantCompositeHeader: Option<CompositeHeader_$union> = compositeCellModalInputProps.relevantCompositeHeader;
    const rmv: (() => void) = compositeCellModalInputProps.rmv;
    const setCell: ((arg0: CompositeCell_$union) => void) = compositeCellModalInputProps.setCell;
    const compositeCell: CompositeCell_$union = compositeCellModalInputProps.compositeCell;
    switch (compositeCell.tag) {
        case /* Unitized */ 2:
            return UnitizedModal(compositeCell.fields[0], compositeCell.fields[1], (v_1: string, oa_3: OntologyAnnotation): void => {
                setCell(CompositeCell_Unitized(v_1, oa_3));
            }, rmv, unwrap(relevantCompositeHeader));
        case /* FreeText */ 1:
            return FreeTextModal(compositeCell.fields[0], (v_3: string): void => {
                setCell(CompositeCell_FreeText(v_3));
            }, rmv, unwrap(relevantCompositeHeader));
        case /* Data */ 3:
            return DataModal(compositeCell.fields[0], (v_5: Data): void => {
                setCell(CompositeCell_Data(v_5));
            }, rmv, unwrap(relevantCompositeHeader));
        default:
            return TermModal(compositeCell.fields[0], (oa_1: OntologyAnnotation): void => {
                setCell(CompositeCell_Term(oa_1));
            }, rmv, unwrap(relevantCompositeHeader));
    }
}

