import { class_type } from "../../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { map, toArray, value as value_36, defaultArg, unwrap } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { Person } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/Person.fs.js";
import { OntologyAnnotation } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologyAnnotation.fs.js";
import { Publication } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/Publication.fs.js";
import { Factor } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/Factor.fs.js";
import { OntologySourceReference } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologySourceReference.fs.js";
import { createElement } from "react";
import React from "react";
import { equals, createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { printf, toConsole, join } from "../../../../fable_modules/fable-library.4.9.0/String.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { choose, length, cons, singleton, ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { DateParsing_parse, Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { useReact_useEffect_311B4086, useReact_useState_FCFD9EF, useFeliz_React__React_useState_Static_1505 } from "../../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { log, debouncel, newDebounceStorage } from "../../Helper.js";
import { iterate, collect, append, empty as empty_1, singleton as singleton_1, delay, toList } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";
import { rangeDouble } from "../../../../fable_modules/fable-library.4.9.0/Range.js";
import { parse, toString } from "../../../../fable_modules/fable-library.4.9.0/Date.js";
import { isMatch } from "../../../../fable_modules/fable-library.4.9.0/RegExp.js";
import { append as append_1, removeAt, mapIndexed, equalsWith } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { Comment$ } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/Comment.fs.js";

export class Helper_PersonMutable {
    constructor(firstname, lastname, midinitials, orcid, address, affiliation, email, phone, fax, roles) {
        this["FirstName@"] = firstname;
        this["LastName@"] = lastname;
        this["MidInitials@"] = midinitials;
        this["ORCID@"] = orcid;
        this["Address@"] = address;
        this["Affiliation@"] = affiliation;
        this["EMail@"] = email;
        this["Phone@"] = phone;
        this["Fax@"] = fax;
        this["Roles@"] = roles;
    }
}

export function Helper_PersonMutable_$reflection() {
    return class_type("MainComponents.Metadata.Helper.PersonMutable", void 0, Helper_PersonMutable);
}

export function Helper_PersonMutable_$ctor_D022634(firstname, lastname, midinitials, orcid, address, affiliation, email, phone, fax, roles) {
    return new Helper_PersonMutable(firstname, lastname, midinitials, orcid, address, affiliation, email, phone, fax, roles);
}

export function Helper_PersonMutable__get_FirstName(__) {
    return __["FirstName@"];
}

export function Helper_PersonMutable__set_FirstName_6DFDD678(__, v) {
    __["FirstName@"] = v;
}

export function Helper_PersonMutable__get_LastName(__) {
    return __["LastName@"];
}

export function Helper_PersonMutable__set_LastName_6DFDD678(__, v) {
    __["LastName@"] = v;
}

export function Helper_PersonMutable__get_MidInitials(__) {
    return __["MidInitials@"];
}

export function Helper_PersonMutable__set_MidInitials_6DFDD678(__, v) {
    __["MidInitials@"] = v;
}

export function Helper_PersonMutable__get_ORCID(__) {
    return __["ORCID@"];
}

export function Helper_PersonMutable__set_ORCID_6DFDD678(__, v) {
    __["ORCID@"] = v;
}

export function Helper_PersonMutable__get_Address(__) {
    return __["Address@"];
}

export function Helper_PersonMutable__set_Address_6DFDD678(__, v) {
    __["Address@"] = v;
}

export function Helper_PersonMutable__get_Affiliation(__) {
    return __["Affiliation@"];
}

export function Helper_PersonMutable__set_Affiliation_6DFDD678(__, v) {
    __["Affiliation@"] = v;
}

export function Helper_PersonMutable__get_EMail(__) {
    return __["EMail@"];
}

export function Helper_PersonMutable__set_EMail_6DFDD678(__, v) {
    __["EMail@"] = v;
}

export function Helper_PersonMutable__get_Phone(__) {
    return __["Phone@"];
}

export function Helper_PersonMutable__set_Phone_6DFDD678(__, v) {
    __["Phone@"] = v;
}

export function Helper_PersonMutable__get_Fax(__) {
    return __["Fax@"];
}

export function Helper_PersonMutable__set_Fax_6DFDD678(__, v) {
    __["Fax@"] = v;
}

export function Helper_PersonMutable__get_Roles(__) {
    return __["Roles@"];
}

export function Helper_PersonMutable__set_Roles_7FEAC74C(__, v) {
    __["Roles@"] = v;
}

export function Helper_PersonMutable_fromPerson_Z22779EAF(person) {
    return Helper_PersonMutable_$ctor_D022634(unwrap(person.FirstName), unwrap(person.LastName), unwrap(person.MidInitials), unwrap(person.ORCID), unwrap(person.Address), unwrap(person.Affiliation), unwrap(person.EMail), unwrap(person.Phone), unwrap(person.Fax), unwrap(person.Roles));
}

export function Helper_PersonMutable__ToPerson(this$) {
    return Person.create(void 0, Helper_PersonMutable__get_ORCID(this$), Helper_PersonMutable__get_LastName(this$), Helper_PersonMutable__get_FirstName(this$), Helper_PersonMutable__get_MidInitials(this$), Helper_PersonMutable__get_EMail(this$), Helper_PersonMutable__get_Phone(this$), Helper_PersonMutable__get_Fax(this$), Helper_PersonMutable__get_Address(this$), Helper_PersonMutable__get_Affiliation(this$), Helper_PersonMutable__get_Roles(this$));
}

export class Helper_OntologyAnnotationMutable {
    constructor(name, tsr, tan) {
        this["Name@"] = name;
        this["TSR@"] = tsr;
        this["TAN@"] = tan;
    }
}

export function Helper_OntologyAnnotationMutable_$reflection() {
    return class_type("MainComponents.Metadata.Helper.OntologyAnnotationMutable", void 0, Helper_OntologyAnnotationMutable);
}

export function Helper_OntologyAnnotationMutable_$ctor_250E0578(name, tsr, tan) {
    return new Helper_OntologyAnnotationMutable(name, tsr, tan);
}

export function Helper_OntologyAnnotationMutable__get_Name(__) {
    return __["Name@"];
}

export function Helper_OntologyAnnotationMutable__set_Name_6DFDD678(__, v) {
    __["Name@"] = v;
}

export function Helper_OntologyAnnotationMutable__get_TSR(__) {
    return __["TSR@"];
}

export function Helper_OntologyAnnotationMutable__set_TSR_6DFDD678(__, v) {
    __["TSR@"] = v;
}

export function Helper_OntologyAnnotationMutable__get_TAN(__) {
    return __["TAN@"];
}

export function Helper_OntologyAnnotationMutable__set_TAN_6DFDD678(__, v) {
    __["TAN@"] = v;
}

export function Helper_OntologyAnnotationMutable_fromOntologyAnnotation_Z4C0FE73C(oa) {
    return Helper_OntologyAnnotationMutable_$ctor_250E0578(unwrap((oa.NameText === "") ? void 0 : oa.NameText), unwrap(oa.TermSourceREF), unwrap(oa.TermAccessionNumber));
}

export function Helper_OntologyAnnotationMutable__ToOntologyAnnotation(this$) {
    return OntologyAnnotation.fromString(Helper_OntologyAnnotationMutable__get_Name(this$), Helper_OntologyAnnotationMutable__get_TSR(this$), Helper_OntologyAnnotationMutable__get_TAN(this$));
}

export class Helper_PublicationMutable {
    constructor(pubmedid, doi, authors, title, status, comments) {
        this["PubmedId@"] = pubmedid;
        this["Doi@"] = doi;
        this["Authors@"] = authors;
        this["Title@"] = title;
        this["Status@"] = status;
        this["Comments@"] = comments;
    }
}

export function Helper_PublicationMutable_$reflection() {
    return class_type("MainComponents.Metadata.Helper.PublicationMutable", void 0, Helper_PublicationMutable);
}

export function Helper_PublicationMutable_$ctor_496BCAB8(pubmedid, doi, authors, title, status, comments) {
    return new Helper_PublicationMutable(pubmedid, doi, authors, title, status, comments);
}

export function Helper_PublicationMutable__get_PubmedId(__) {
    return __["PubmedId@"];
}

export function Helper_PublicationMutable__set_PubmedId_6DFDD678(__, v) {
    __["PubmedId@"] = v;
}

export function Helper_PublicationMutable__get_Doi(__) {
    return __["Doi@"];
}

export function Helper_PublicationMutable__set_Doi_6DFDD678(__, v) {
    __["Doi@"] = v;
}

export function Helper_PublicationMutable__get_Authors(__) {
    return __["Authors@"];
}

export function Helper_PublicationMutable__set_Authors_6DFDD678(__, v) {
    __["Authors@"] = v;
}

export function Helper_PublicationMutable__get_Title(__) {
    return __["Title@"];
}

export function Helper_PublicationMutable__set_Title_6DFDD678(__, v) {
    __["Title@"] = v;
}

export function Helper_PublicationMutable__get_Status(__) {
    return __["Status@"];
}

export function Helper_PublicationMutable__set_Status_Z41EC1759(__, v) {
    __["Status@"] = v;
}

export function Helper_PublicationMutable__get_Comments(__) {
    return __["Comments@"];
}

export function Helper_PublicationMutable__set_Comments_Z1781AAC1(__, v) {
    __["Comments@"] = v;
}

export function Helper_PublicationMutable_fromPublication_Z3279EA88(pub) {
    return Helper_PublicationMutable_$ctor_496BCAB8(unwrap(pub.PubMedID), unwrap(pub.DOI), unwrap(pub.Authors), unwrap(pub.Title), unwrap(pub.Status), unwrap(pub.Comments));
}

export function Helper_PublicationMutable__ToPublication(this$) {
    return Publication.create(unwrap(Helper_PublicationMutable__get_PubmedId(this$)), unwrap(Helper_PublicationMutable__get_Doi(this$)), unwrap(Helper_PublicationMutable__get_Authors(this$)), unwrap(Helper_PublicationMutable__get_Title(this$)), unwrap(Helper_PublicationMutable__get_Status(this$)), unwrap(Helper_PublicationMutable__get_Comments(this$)));
}

export class Helper_FactorMutable {
    constructor(name, factortype, comments) {
        this["Name@"] = name;
        this["FactorType@"] = factortype;
        this["Comments@"] = comments;
    }
}

export function Helper_FactorMutable_$reflection() {
    return class_type("MainComponents.Metadata.Helper.FactorMutable", void 0, Helper_FactorMutable);
}

export function Helper_FactorMutable_$ctor_Z1E94A380(name, factortype, comments) {
    return new Helper_FactorMutable(name, factortype, comments);
}

export function Helper_FactorMutable__get_Name(__) {
    return __["Name@"];
}

export function Helper_FactorMutable__set_Name_6DFDD678(__, v) {
    __["Name@"] = v;
}

export function Helper_FactorMutable__get_FactorType(__) {
    return __["FactorType@"];
}

export function Helper_FactorMutable__set_FactorType_Z41EC1759(__, v) {
    __["FactorType@"] = v;
}

export function Helper_FactorMutable__get_Comments(__) {
    return __["Comments@"];
}

export function Helper_FactorMutable__set_Comments_Z1781AAC1(__, v) {
    __["Comments@"] = v;
}

export function Helper_FactorMutable_fromFactor_Z55333BD7(f) {
    return Helper_FactorMutable_$ctor_Z1E94A380(unwrap(f.Name), unwrap(f.FactorType), unwrap(f.Comments));
}

export function Helper_FactorMutable__ToFactor(this$) {
    return Factor.create(void 0, unwrap(Helper_FactorMutable__get_Name(this$)), unwrap(Helper_FactorMutable__get_FactorType(this$)), unwrap(Helper_FactorMutable__get_Comments(this$)));
}

export class Helper_OntologySourceReferenceMutable {
    constructor(name, description, file, version, comments) {
        this["Name@"] = name;
        this["Description@"] = description;
        this["File@"] = file;
        this["Version@"] = version;
        this["Comments@"] = comments;
    }
}

export function Helper_OntologySourceReferenceMutable_$reflection() {
    return class_type("MainComponents.Metadata.Helper.OntologySourceReferenceMutable", void 0, Helper_OntologySourceReferenceMutable);
}

export function Helper_OntologySourceReferenceMutable_$ctor_Z61E08C1(name, description, file, version, comments) {
    return new Helper_OntologySourceReferenceMutable(name, description, file, version, comments);
}

export function Helper_OntologySourceReferenceMutable__get_Name(__) {
    return __["Name@"];
}

export function Helper_OntologySourceReferenceMutable__set_Name_6DFDD678(__, v) {
    __["Name@"] = v;
}

export function Helper_OntologySourceReferenceMutable__get_Description(__) {
    return __["Description@"];
}

export function Helper_OntologySourceReferenceMutable__set_Description_6DFDD678(__, v) {
    __["Description@"] = v;
}

export function Helper_OntologySourceReferenceMutable__get_File(__) {
    return __["File@"];
}

export function Helper_OntologySourceReferenceMutable__set_File_6DFDD678(__, v) {
    __["File@"] = v;
}

export function Helper_OntologySourceReferenceMutable__get_Version(__) {
    return __["Version@"];
}

export function Helper_OntologySourceReferenceMutable__set_Version_6DFDD678(__, v) {
    __["Version@"] = v;
}

export function Helper_OntologySourceReferenceMutable__get_Comments(__) {
    return __["Comments@"];
}

export function Helper_OntologySourceReferenceMutable__set_Comments_Z1781AAC1(__, v) {
    __["Comments@"] = v;
}

export function Helper_OntologySourceReferenceMutable_fromOntologySourceReference_Z79C650B(o) {
    return Helper_OntologySourceReferenceMutable_$ctor_Z61E08C1(unwrap(o.Name), unwrap(o.Description), unwrap(o.File), unwrap(o.Version), unwrap(o.Comments));
}

export function Helper_OntologySourceReferenceMutable__ToOntologySourceReference(this$) {
    return OntologySourceReference.create(unwrap(Helper_OntologySourceReferenceMutable__get_Description(this$)), unwrap(Helper_OntologySourceReferenceMutable__get_File(this$)), unwrap(Helper_OntologySourceReferenceMutable__get_Name(this$)), unwrap(Helper_OntologySourceReferenceMutable__get_Version(this$)), unwrap(Helper_OntologySourceReferenceMutable__get_Comments(this$)));
}

export function Helper_addButton(clickEvent) {
    let elems;
    return createElement("div", createObj(ofArray([["className", join(" ", ["is-flex", "is-justify-content-center"])], (elems = [createElement("button", createObj(Helpers_combineClasses("button", ofArray([["className", "is-ghost"], ["children", "+"], ["onClick", clickEvent]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])));
}

export function Helper_deleteButton(clickEvent) {
    let elems;
    return createElement("div", createObj(ofArray([["className", join(" ", ["is-flex", "is-justify-content-flex-end"])], (elems = [createElement("button", createObj(Helpers_combineClasses("button", ofArray([["className", "is-outlined"], ["className", "is-danger"], ["children", "Delete"], ["onClick", clickEvent]]))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])));
}

export function Helper_cardFormGroup(formComponents) {
    const elms = singleton(createElement("div", {
        className: join(" ", ["form-container"]),
        children: Interop_reactApi.Children.toArray(Array.from(formComponents)),
    }));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export class FormComponents {
    constructor() {
    }
}

export function FormComponents_$reflection() {
    return class_type("MainComponents.Metadata.FormComponents", void 0, FormComponents);
}

export function FormComponents_TextInput_468641B7(formComponents_TextInput_468641B7InputProps) {
    let elems_3;
    const isarea = formComponents_TextInput_468641B7InputProps.isarea;
    const removebutton = formComponents_TextInput_468641B7InputProps.removebutton;
    const fullwidth = formComponents_TextInput_468641B7InputProps.fullwidth;
    const setter = formComponents_TextInput_468641B7InputProps.setter;
    const label = formComponents_TextInput_468641B7InputProps.label;
    const input = formComponents_TextInput_468641B7InputProps.input;
    const inputFormElement = defaultArg(isarea, false) ? ((elm) => createElement("textarea", createObj(Helpers_combineClasses("textarea", elm)))) : ((props_2) => createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", props_2)))));
    const fullwidth_1 = defaultArg(fullwidth, false);
    const patternInput = useFeliz_React__React_useState_Static_1505(false);
    const patternInput_1 = useFeliz_React__React_useState_Static_1505(input);
    const setState = patternInput_1[1];
    const patternInput_2 = useReact_useState_FCFD9EF(newDebounceStorage);
    useReact_useEffect_311B4086(() => {
        setState(input);
    }, [input]);
    return createElement("div", createObj(Helpers_combineClasses("field", ofArray([["style", createObj(toList(delay(() => (fullwidth_1 ? singleton_1(["flexGrow", 1]) : empty_1()))))], (elems_3 = toList(delay(() => append((label !== "") ? singleton_1(createElement("label", {
        className: "label",
        children: label,
    })) : empty_1(), delay(() => {
        let elems_2;
        return singleton_1(createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "has-addons"], (elems_2 = toList(delay(() => append(singleton_1(createElement("div", createObj(Helpers_combineClasses("control", toList(delay(() => append(patternInput[0] ? singleton_1(["className", "is-loading"]) : empty_1(), delay(() => append(singleton_1(["style", createObj(toList(delay(() => (fullwidth_1 ? singleton_1(["flexGrow", 1]) : empty_1()))))]), delay(() => {
            let elems, value_16;
            return singleton_1((elems = [inputFormElement(ofArray([(value_16 = patternInput_1[0], ["ref", (e) => {
                if (!(e == null) && !equals(e.value, value_16)) {
                    e.value = value_16;
                }
            }]), ["onChange", (ev) => {
                const e_1 = ev.target.value;
                setState(e_1);
                debouncel(patternInput_2[0], label, 1000, patternInput[1], setter, e_1);
            }]]))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))]));
        })))))))))), delay(() => {
            let elms;
            return (removebutton != null) ? singleton_1((elms = singleton(createElement("span", createObj(Helpers_combineClasses("button", ofArray([["children", "X"], ["onClick", value_36(removebutton)], ["className", "is-danger"], ["className", "is-outlined"]]))))), createElement("div", {
                className: "control",
                children: Interop_reactApi.Children.toArray(Array.from(elms)),
            }))) : empty_1();
        })))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])])))));
    })))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_3))])]))));
}

export function FormComponents_InputSequence_6A786D4E(formComponents_InputSequence_6A786D4EInputProps) {
    let elms, children_2;
    const inputComponent = formComponents_InputSequence_6A786D4EInputProps.inputComponent;
    const setter = formComponents_InputSequence_6A786D4EInputProps.setter;
    const label = formComponents_InputSequence_6A786D4EInputProps.label;
    const empty = formComponents_InputSequence_6A786D4EInputProps.empty;
    const inputs$0027 = formComponents_InputSequence_6A786D4EInputProps["inputs\'"];
    const patternInput = useFeliz_React__React_useState_Static_1505(Array.from(inputs$0027));
    const state = patternInput[0];
    useReact_useEffect_311B4086(() => {
        patternInput[1](Array.from(inputs$0027));
    }, [inputs$0027]);
    const elms_1 = ofArray([createElement("label", {
        className: "label",
        children: label,
    }), (elms = singleton((children_2 = toList(delay(() => collect((i) => {
        let children;
        return singleton_1((children = singleton(inputComponent([state[i], "", (oa) => {
            state[i] = oa;
            setter(Array.from(state));
        }, (_arg_1) => {
            state.splice(i, 1);
            setter(Array.from(state));
        }])), createElement("li", {
            children: Interop_reactApi.Children.toArray(Array.from(children)),
        })));
    }, rangeDouble(0, 1, state.length - 1)))), createElement("ol", {
        children: Interop_reactApi.Children.toArray(Array.from(children_2)),
    }))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), Helper_addButton((_arg_2) => {
        void (state.push(empty));
        setter(Array.from(state));
    })]);
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    });
}

export function FormComponents_TextInputs_Z2C75BD80(formComponents_TextInputs_Z2C75BD80InputProps) {
    const fullwidth = formComponents_TextInputs_Z2C75BD80InputProps.fullwidth;
    const placeholder = formComponents_TextInputs_Z2C75BD80InputProps.placeholder;
    const setter = formComponents_TextInputs_Z2C75BD80InputProps.setter;
    const label = formComponents_TextInputs_Z2C75BD80InputProps.label;
    const texts = formComponents_TextInputs_Z2C75BD80InputProps.texts;
    return createElement(FormComponents_InputSequence_6A786D4E, {
        "inputs\'": texts,
        empty: "",
        label: label,
        setter: setter,
        inputComponent: (tupledArg) => createElement(FormComponents_TextInput_468641B7, {
            input: tupledArg[0],
            label: tupledArg[1],
            setter: tupledArg[2],
            fullwidth: true,
            removebutton: tupledArg[3],
        }),
    });
}

export function FormComponents_DateTimeInput_Z7B9B1480(formComponents_DateTimeInput_Z7B9B1480InputProps) {
    let elems_1;
    const fullwidth = formComponents_DateTimeInput_Z7B9B1480InputProps.fullwidth;
    const placeholder = formComponents_DateTimeInput_Z7B9B1480InputProps.placeholder;
    const setter = formComponents_DateTimeInput_Z7B9B1480InputProps.setter;
    const label = formComponents_DateTimeInput_Z7B9B1480InputProps.label;
    const input = formComponents_DateTimeInput_Z7B9B1480InputProps.input;
    const fullwidth_1 = defaultArg(fullwidth, false);
    const patternInput = useFeliz_React__React_useState_Static_1505(false);
    const patternInput_1 = useFeliz_React__React_useState_Static_1505(input);
    const setState = patternInput_1[1];
    const patternInput_2 = useReact_useState_FCFD9EF(newDebounceStorage);
    useReact_useEffect_311B4086(() => {
        setState(input);
    }, [input]);
    return createElement("div", createObj(Helpers_combineClasses("field", ofArray([["style", createObj(toList(delay(() => (fullwidth_1 ? singleton_1(["flexGrow", 1]) : empty_1()))))], (elems_1 = toList(delay(() => append((label !== "") ? singleton_1(createElement("label", {
        className: "label",
        children: label,
    })) : empty_1(), delay(() => singleton_1(createElement("div", createObj(Helpers_combineClasses("control", toList(delay(() => append(patternInput[0] ? singleton_1(["className", "is-loading"]) : empty_1(), delay(() => {
        let elems, value_10;
        return singleton_1((elems = [createElement("input", createObj(cons(["type", "datetime-local"], Helpers_combineClasses("input", ofArray([(value_10 = patternInput_1[0], ["ref", (e) => {
            if (!(e == null) && !equals(e.value, value_10)) {
                e.value = value_10;
            }
        }]), ["onChange", (ev) => {
            iterate((e_1) => {
                const dtString = toString(e_1, "yyyy-MM-ddThh:mm");
                log(["LOOK AT ME", dtString]);
                setState(dtString);
                debouncel(patternInput_2[0], label, 1000, patternInput[1], setter, dtString);
            }, toArray(DateParsing_parse(ev.target.value)));
        }]])))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems))]));
    })))))))))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function FormComponents_DateTimeInput_Z478DEF28(input, label, setter, fullwidth) {
    return createElement(FormComponents_DateTimeInput_Z7B9B1480, {
        input: toString(input, "yyyy-MM-ddThh:mm"),
        label: label,
        setter: (s) => {
            setter(parse(s));
        },
        fullwidth: unwrap(fullwidth),
    });
}

export function FormComponents_GUIDInput_25667B6A(formComponents_GUIDInput_25667B6AInputProps) {
    let elems_2;
    const fullwidth = formComponents_GUIDInput_25667B6AInputProps.fullwidth;
    const placeholder = formComponents_GUIDInput_25667B6AInputProps.placeholder;
    const setter = formComponents_GUIDInput_25667B6AInputProps.setter;
    const label = formComponents_GUIDInput_25667B6AInputProps.label;
    const input = formComponents_GUIDInput_25667B6AInputProps.input;
    const fullwidth_1 = defaultArg(fullwidth, false);
    const patternInput = useFeliz_React__React_useState_Static_1505(false);
    const patternInput_1 = useFeliz_React__React_useState_Static_1505(input);
    const setState = patternInput_1[1];
    const patternInput_2 = useFeliz_React__React_useState_Static_1505(true);
    const isValid = patternInput_2[0];
    const patternInput_3 = useReact_useState_FCFD9EF(newDebounceStorage);
    useReact_useEffect_311B4086(() => {
        setState(input);
    }, [input]);
    const regex = /^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$/gu;
    return createElement("div", createObj(Helpers_combineClasses("field", ofArray([["style", createObj(toList(delay(() => (fullwidth_1 ? singleton_1(["flexGrow", 1]) : empty_1()))))], (elems_2 = toList(delay(() => append((label !== "") ? singleton_1(createElement("label", {
        className: "label",
        children: label,
    })) : empty_1(), delay(() => append(singleton_1(createElement("div", createObj(Helpers_combineClasses("control", toList(delay(() => append(singleton_1(["className", "has-icons-right"]), delay(() => append(patternInput[0] ? singleton_1(["className", "is-loading"]) : empty_1(), delay(() => {
        let elems_1;
        return singleton_1((elems_1 = toList(delay(() => append(singleton_1(createElement("input", createObj(cons(["type", "text"], Helpers_combineClasses("input", toList(delay(() => append(singleton_1(["pattern", regex]), delay(() => append(singleton_1(["required", true]), delay(() => append(!isValid ? singleton_1(["className", "is-danger"]) : empty_1(), delay(() => append((placeholder != null) ? singleton_1(["placeholder", value_36(placeholder)]) : empty_1(), delay(() => {
            let value_20;
            return append(singleton_1((value_20 = patternInput_1[0], ["ref", (e) => {
                if (!(e == null) && !equals(e.value, value_20)) {
                    e.value = value_20;
                }
            }])), delay(() => singleton_1(["onChange", (ev) => {
                const s_1 = ev.target.value;
                const nextValid = isMatch(regex, s_1.trim());
                patternInput_2[1](nextValid);
                setState(s_1);
                if (nextValid) {
                    debouncel(patternInput_3[0], label, 200, patternInput[1], setter, s_1);
                }
            }])));
        }))))))))))))))), delay(() => {
            let elems;
            return isValid ? singleton_1(createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["className", "is-right"], ["className", "is-small"], ["className", "is-success"], (elems = [createElement("i", {
                className: "fas fa-check",
            })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))))) : empty_1();
        })))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))]));
    })))))))))), delay(() => {
        let value_34;
        return singleton_1((value_34 = "Guid should contain 32 digits with 4 dashes following: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx. Allowed are a-f, A-F and numbers.", createElement("small", {
            children: [value_34],
        })));
    })))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

export function FormComponents_OntologyAnnotationInput_154CF37E(formComponents_OntologyAnnotationInput_154CF37EInputProps) {
    const removebutton = formComponents_OntologyAnnotationInput_154CF37EInputProps.removebutton;
    const showTextLabels = formComponents_OntologyAnnotationInput_154CF37EInputProps.showTextLabels;
    const setter = formComponents_OntologyAnnotationInput_154CF37EInputProps.setter;
    const label = formComponents_OntologyAnnotationInput_154CF37EInputProps.label;
    const input = formComponents_OntologyAnnotationInput_154CF37EInputProps.input;
    const showTextLabels_1 = defaultArg(showTextLabels, true);
    const patternInput = useFeliz_React__React_useState_Static_1505(Helper_OntologyAnnotationMutable_fromOntologyAnnotation_Z4C0FE73C(input));
    const state = patternInput[0];
    useReact_useEffect_311B4086(() => {
        patternInput[1](Helper_OntologyAnnotationMutable_fromOntologyAnnotation_Z4C0FE73C(input));
    }, [input]);
    const hasLabel = label !== "";
    const elms = toList(delay(() => append(hasLabel ? singleton_1(createElement("label", {
        className: "label",
        children: label,
    })) : empty_1(), delay(() => {
        let elems_1;
        return singleton_1(createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", join(" ", ["is-flex", "is-flex-direction-row", "is-justify-content-space-between"])], (elems_1 = toList(delay(() => {
            let elems;
            return append(singleton_1(createElement("div", createObj(ofArray([["className", join(" ", toList(delay(() => append(singleton_1("form-container"), delay(() => ((removebutton != null) ? singleton_1("pr-2") : empty_1()))))))], (elems = [createElement(FormComponents_TextInput_468641B7, {
                input: defaultArg(Helper_OntologyAnnotationMutable__get_Name(state), ""),
                label: showTextLabels_1 ? "Term Name" : "",
                setter: (s_1) => {
                    const s_2 = (s_1 === "") ? void 0 : s_1;
                    toConsole(printf("INNER SET"));
                    Helper_OntologyAnnotationMutable__set_Name_6DFDD678(state, s_2);
                    setter(Helper_OntologyAnnotationMutable__ToOntologyAnnotation(state));
                },
                fullwidth: true,
            }), createElement(FormComponents_TextInput_468641B7, {
                input: defaultArg(Helper_OntologyAnnotationMutable__get_TSR(state), ""),
                label: showTextLabels_1 ? "TSR" : "",
                setter: (s_3) => {
                    Helper_OntologyAnnotationMutable__set_TSR_6DFDD678(state, (s_3 === "") ? void 0 : s_3);
                    setter(Helper_OntologyAnnotationMutable__ToOntologyAnnotation(state));
                },
                fullwidth: true,
            }), createElement(FormComponents_TextInput_468641B7, {
                input: defaultArg(Helper_OntologyAnnotationMutable__get_TAN(state), ""),
                label: showTextLabels_1 ? "TAN" : "",
                setter: (s_5) => {
                    Helper_OntologyAnnotationMutable__set_TAN_6DFDD678(state, (s_5 === "") ? void 0 : s_5);
                    setter(Helper_OntologyAnnotationMutable__ToOntologyAnnotation(state));
                },
                fullwidth: true,
            })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))), delay(() => {
                let children;
                return (removebutton != null) ? singleton_1((children = toList(delay(() => append(showTextLabels_1 ? singleton_1(createElement("label", createObj(Helpers_combineClasses("label", ofArray([["style", {
                    color: "transparent",
                }], ["children", "rmv"]]))))) : empty_1(), delay(() => singleton_1(createElement("button", createObj(Helpers_combineClasses("button", ofArray([["children", "X"], ["onClick", value_36(removebutton)], ["className", "is-danger"], ["className", "is-outlined"]]))))))))), createElement("div", {
                    children: Interop_reactApi.Children.toArray(Array.from(children)),
                }))) : empty_1();
            }));
        })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])])))));
    }))));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function FormComponents_OntologyAnnotationsInput_532A28D8(formComponents_OntologyAnnotationsInput_532A28D8InputProps) {
    const showTextLabels = formComponents_OntologyAnnotationsInput_532A28D8InputProps.showTextLabels;
    const setter = formComponents_OntologyAnnotationsInput_532A28D8InputProps.setter;
    const label = formComponents_OntologyAnnotationsInput_532A28D8InputProps.label;
    const oas = formComponents_OntologyAnnotationsInput_532A28D8InputProps.oas;
    return createElement(FormComponents_InputSequence_6A786D4E, {
        "inputs\'": oas,
        empty: OntologyAnnotation.empty,
        label: label,
        setter: setter,
        inputComponent: (tupledArg) => createElement(FormComponents_OntologyAnnotationInput_154CF37E, {
            input: tupledArg[0],
            label: tupledArg[1],
            setter: tupledArg[2],
            showTextLabels: unwrap(showTextLabels),
            removebutton: tupledArg[3],
        }),
    });
}

export function FormComponents_PersonInput_Z4DA5A514(formComponents_PersonInput_Z4DA5A514InputProps) {
    let elms_1, elems, children, fields, all, elems_2, elms, elems_4;
    const deletebutton = formComponents_PersonInput_Z4DA5A514InputProps.deletebutton;
    const setter = formComponents_PersonInput_Z4DA5A514InputProps.setter;
    const input = formComponents_PersonInput_Z4DA5A514InputProps.input;
    const patternInput = useFeliz_React__React_useState_Static_1505(false);
    const isExtended = patternInput[0];
    const patternInput_1 = useFeliz_React__React_useState_Static_1505(Helper_PersonMutable_fromPerson_Z22779EAF(input));
    const state = patternInput_1[0];
    useReact_useEffect_311B4086(() => {
        patternInput_1[1](Helper_PersonMutable_fromPerson_Z22779EAF(input));
    }, [input]);
    const fn = defaultArg(Helper_PersonMutable__get_FirstName(state), "");
    const ln = defaultArg(Helper_PersonMutable__get_LastName(state), "");
    const mi = defaultArg(Helper_PersonMutable__get_MidInitials(state), "");
    let nameStr;
    const x = (`${fn} ${mi} ${ln}`).trim();
    nameStr = ((x === "") ? "<name>" : x);
    const orcid = defaultArg(Helper_PersonMutable__get_ORCID(state), "<orcid>");
    const createPersonFieldTextInput = (tupledArg) => createElement(FormComponents_TextInput_468641B7, {
        input: defaultArg(tupledArg[0], ""),
        label: tupledArg[1],
        setter: (s) => {
            tupledArg[2]((s === "") ? void 0 : s);
            setter(Helper_PersonMutable__ToPerson(state));
        },
        fullwidth: true,
    });
    const elms_2 = ofArray([(elms_1 = ofArray([createElement("div", createObj(Helpers_combineClasses("card-header-title", singleton((elems = [(children = ofArray([createElement("h5", {
        className: "title is-5",
        children: nameStr,
    }), createElement("h6", {
        className: "subtitle is-6",
        children: orcid,
    })]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    })), createElement("div", {
        style: {
            marginLeft: "auto",
        },
        children: (fields = ofArray([Helper_PersonMutable__get_FirstName(state), Helper_PersonMutable__get_LastName(state), Helper_PersonMutable__get_MidInitials(state), Helper_PersonMutable__get_ORCID(state), Helper_PersonMutable__get_Address(state), Helper_PersonMutable__get_Affiliation(state), Helper_PersonMutable__get_EMail(state), Helper_PersonMutable__get_Phone(state), Helper_PersonMutable__get_Fax(state), map((_arg_1) => "", Helper_PersonMutable__get_Roles(state))]), (all = (length(fields) | 0), `${length(choose((x_1) => x_1, fields))}/${all}`)),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))]))))), createElement("a", createObj(Helpers_combineClasses("card-header-icon", ofArray([["onClick", (_arg_3) => {
        patternInput[1](!isExtended);
    }], (elems_2 = [(elms = singleton(createElement("i", {
        className: join(" ", ["fas", "fa-angle-down"]),
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))]), createElement("header", {
        className: "card-header",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), createElement("div", createObj(Helpers_combineClasses("card-content", ofArray([["className", join(" ", toList(delay(() => (!isExtended ? singleton_1("is-hidden") : empty_1()))))], (elems_4 = toList(delay(() => append(singleton_1(Helper_cardFormGroup(ofArray([createPersonFieldTextInput([Helper_PersonMutable__get_FirstName(state), "First Name", (s_4) => {
        Helper_PersonMutable__set_FirstName_6DFDD678(state, s_4);
    }]), createPersonFieldTextInput([Helper_PersonMutable__get_LastName(state), "Last Name", (s_5) => {
        Helper_PersonMutable__set_LastName_6DFDD678(state, s_5);
    }])]))), delay(() => append(singleton_1(Helper_cardFormGroup(ofArray([createPersonFieldTextInput([Helper_PersonMutable__get_MidInitials(state), "Mid Initials", (s_6) => {
        Helper_PersonMutable__set_MidInitials_6DFDD678(state, s_6);
    }]), createPersonFieldTextInput([Helper_PersonMutable__get_ORCID(state), "ORCID", (s_7) => {
        Helper_PersonMutable__set_ORCID_6DFDD678(state, s_7);
    }])]))), delay(() => append(singleton_1(Helper_cardFormGroup(ofArray([createPersonFieldTextInput([Helper_PersonMutable__get_Affiliation(state), "Affiliation", (s_8) => {
        Helper_PersonMutable__set_Affiliation_6DFDD678(state, s_8);
    }]), createPersonFieldTextInput([Helper_PersonMutable__get_Address(state), "Address", (s_9) => {
        Helper_PersonMutable__set_Address_6DFDD678(state, s_9);
    }])]))), delay(() => append(singleton_1(Helper_cardFormGroup(ofArray([createPersonFieldTextInput([Helper_PersonMutable__get_EMail(state), "Email", (s_10) => {
        Helper_PersonMutable__set_EMail_6DFDD678(state, s_10);
    }]), createPersonFieldTextInput([Helper_PersonMutable__get_Phone(state), "Phone", (s_11) => {
        Helper_PersonMutable__set_Phone_6DFDD678(state, s_11);
    }]), createPersonFieldTextInput([Helper_PersonMutable__get_Fax(state), "Fax", (s_12) => {
        Helper_PersonMutable__set_Fax_6DFDD678(state, s_12);
    }])]))), delay(() => append(singleton_1(createElement(FormComponents_OntologyAnnotationsInput_532A28D8, {
        oas: defaultArg(Helper_PersonMutable__get_Roles(state), []),
        label: "Roles",
        setter: (oas) => {
            Helper_PersonMutable__set_Roles_7FEAC74C(state, equalsWith(equals, oas, []) ? void 0 : oas);
            setter(Helper_PersonMutable__ToPerson(state));
        },
        showTextLabels: false,
    })), delay(() => ((deletebutton != null) ? singleton_1(Helper_deleteButton(value_36(deletebutton))) : empty_1()))))))))))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))]);
    return createElement("div", {
        className: "card",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    });
}

export function FormComponents_PersonsInput_4FAF87F1(persons, label, setter) {
    let elms;
    const elms_1 = ofArray([createElement("label", {
        className: "label",
        children: label,
    }), (elms = toList(delay(() => mapIndexed((i, person) => createElement(FormComponents_PersonInput_Z4DA5A514, {
        input: person,
        setter: (p) => {
            persons[i] = p;
            setter(persons);
        },
        deletebutton: (_arg) => {
            setter(removeAt(i, persons));
        },
    }), persons))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), Helper_addButton((_arg_1) => {
        setter(append_1(persons, [Person.create()]));
    })]);
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    });
}

export function FormComponents_CommentInput_3913CA7E(comment, label, setter, showTextLabels, removebutton) {
    const showTextLabels_1 = defaultArg(showTextLabels, true);
    const elms = toList(delay(() => append((label !== "") ? singleton_1(createElement("label", {
        className: "label",
        children: label,
    })) : empty_1(), delay(() => {
        let elems;
        return singleton_1(createElement("div", createObj(ofArray([["className", join(" ", ["form-container"])], (elems = toList(delay(() => append(singleton_1(createElement(FormComponents_TextInput_468641B7, {
            input: defaultArg(comment.Name, ""),
            label: showTextLabels_1 ? "Term Name" : "",
            setter: (s_1) => {
                setter(new Comment$(comment.ID, (s_1 === "") ? void 0 : s_1, comment.Value));
            },
            fullwidth: true,
        })), delay(() => append(singleton_1(createElement(FormComponents_TextInput_468641B7, {
            input: defaultArg(comment.Value, ""),
            label: showTextLabels_1 ? "TSR" : "",
            setter: (s_2) => {
                setter(new Comment$(comment.ID, comment.Name, (s_2 === "") ? void 0 : s_2));
            },
            fullwidth: true,
        })), delay(() => ((removebutton != null) ? singleton_1(createElement("button", createObj(Helpers_combineClasses("button", ofArray([["children", "X"], ["onClick", value_36(removebutton)], ["className", "is-danger"], ["className", "is-outlined"]]))))) : empty_1()))))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems))])]))));
    }))));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

export function FormComponents_CommentsInput_2201E571(comments, label, setter) {
    const elms_1 = toList(delay(() => append((label !== "") ? singleton_1(createElement("label", {
        className: "label",
        children: label,
    })) : empty_1(), delay(() => {
        let elms;
        return append(singleton_1((elms = toList(delay(() => mapIndexed((i, comment) => FormComponents_CommentInput_3913CA7E(comment, "", (c) => {
            comments[i] = c;
            setter(comments);
        }, false, (_arg) => {
            setter(removeAt(i, comments));
        }), comments))), createElement("div", {
            className: "field",
            children: Interop_reactApi.Children.toArray(Array.from(elms)),
        }))), delay(() => singleton_1(Helper_addButton((_arg_1) => {
            setter(append_1(comments, [Comment$.create()]));
        }))));
    }))));
    return createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    });
}

export function FormComponents_PublicationInput_Z3FC1A874(formComponents_PublicationInput_Z3FC1A874InputProps) {
    let elms_1, elems, children, fields, all, elems_2, elms, elems_4;
    const deletebutton = formComponents_PublicationInput_Z3FC1A874InputProps.deletebutton;
    const setter = formComponents_PublicationInput_Z3FC1A874InputProps.setter;
    const input = formComponents_PublicationInput_Z3FC1A874InputProps.input;
    const patternInput = useFeliz_React__React_useState_Static_1505(false);
    const isExtended = patternInput[0];
    const patternInput_1 = useFeliz_React__React_useState_Static_1505(Helper_PublicationMutable_fromPublication_Z3279EA88(input));
    const state = patternInput_1[0];
    useReact_useEffect_311B4086(() => {
        patternInput_1[1](Helper_PublicationMutable_fromPublication_Z3279EA88(input));
    }, [input]);
    const title = defaultArg(Helper_PublicationMutable__get_Title(state), "<title>");
    const doi = defaultArg(Helper_PublicationMutable__get_Doi(state), "<doi>");
    const createPersonFieldTextInput = (tupledArg) => createElement(FormComponents_TextInput_468641B7, {
        input: defaultArg(tupledArg[0], ""),
        label: tupledArg[1],
        setter: (s) => {
            tupledArg[2]((s === "") ? void 0 : s);
            setter(Helper_PublicationMutable__ToPublication(state));
        },
        fullwidth: true,
    });
    const elms_2 = ofArray([(elms_1 = ofArray([createElement("div", createObj(Helpers_combineClasses("card-header-title", singleton((elems = [(children = ofArray([createElement("h5", {
        className: "title is-5",
        children: title,
    }), createElement("h6", {
        className: "subtitle is-6",
        children: doi,
    })]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    })), createElement("div", {
        style: {
            marginLeft: "auto",
        },
        children: (fields = ofArray([Helper_PublicationMutable__get_PubmedId(state), Helper_PublicationMutable__get_Doi(state), Helper_PublicationMutable__get_Title(state), Helper_PublicationMutable__get_Authors(state), map((_arg_1) => "", Helper_PublicationMutable__get_Comments(state)), map((_arg_2) => "", Helper_PublicationMutable__get_Status(state))]), (all = (length(fields) | 0), `${length(choose((x) => x, fields))}/${all}`)),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))]))))), createElement("a", createObj(Helpers_combineClasses("card-header-icon", ofArray([["onClick", (_arg_4) => {
        patternInput[1](!isExtended);
    }], (elems_2 = [(elms = singleton(createElement("i", {
        className: join(" ", ["fas", "fa-angle-down"]),
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))]), createElement("header", {
        className: "card-header",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), createElement("div", createObj(Helpers_combineClasses("card-content", ofArray([["className", join(" ", toList(delay(() => (!isExtended ? singleton_1("is-hidden") : empty_1()))))], (elems_4 = toList(delay(() => append(singleton_1(createPersonFieldTextInput([Helper_PublicationMutable__get_Title(state), "Title", (s_4) => {
        Helper_PublicationMutable__set_Title_6DFDD678(state, s_4);
    }])), delay(() => append(singleton_1(Helper_cardFormGroup(ofArray([createPersonFieldTextInput([Helper_PublicationMutable__get_PubmedId(state), "PubMed Id", (s_5) => {
        Helper_PublicationMutable__set_PubmedId_6DFDD678(state, s_5);
    }]), createPersonFieldTextInput([Helper_PublicationMutable__get_Doi(state), "DOI", (s_6) => {
        Helper_PublicationMutable__set_Doi_6DFDD678(state, s_6);
    }])]))), delay(() => append(singleton_1(createPersonFieldTextInput([Helper_PublicationMutable__get_Authors(state), "Authors", (s_7) => {
        Helper_PublicationMutable__set_Authors_6DFDD678(state, s_7);
    }])), delay(() => append(singleton_1(createElement(FormComponents_OntologyAnnotationInput_154CF37E, {
        input: defaultArg(Helper_PublicationMutable__get_Status(state), OntologyAnnotation.empty),
        label: "Status",
        setter: (s_8) => {
            Helper_PublicationMutable__set_Status_Z41EC1759(state, equals(s_8, OntologyAnnotation.empty) ? void 0 : s_8);
            setter(Helper_PublicationMutable__ToPublication(state));
        },
    })), delay(() => append(singleton_1(FormComponents_CommentsInput_2201E571(defaultArg(Helper_PublicationMutable__get_Comments(state), []), "Comments", (c) => {
        Helper_PublicationMutable__set_Comments_Z1781AAC1(state, equalsWith(equals, c, []) ? void 0 : c);
        setter(Helper_PublicationMutable__ToPublication(state));
    })), delay(() => ((deletebutton != null) ? singleton_1(Helper_deleteButton(value_36(deletebutton))) : empty_1()))))))))))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))]);
    return createElement("div", {
        className: "card",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    });
}

export function FormComponents_PublicationsInput_Z2B6713CF(input, label, setter) {
    return createElement(FormComponents_InputSequence_6A786D4E, {
        "inputs\'": input,
        empty: Publication.create(),
        label: label,
        setter: setter,
        inputComponent: (tupledArg) => createElement(FormComponents_PublicationInput_Z3FC1A874, {
            input: tupledArg[0],
            setter: tupledArg[2],
            deletebutton: tupledArg[3],
        }),
    });
}

export function FormComponents_FactorInput_ZB005F54(formComponents_FactorInput_ZB005F54InputProps) {
    let elms_1, elems, children, fields, all, elems_2, elms, elems_4;
    const deletebutton = formComponents_FactorInput_ZB005F54InputProps.deletebutton;
    const setter = formComponents_FactorInput_ZB005F54InputProps.setter;
    const input = formComponents_FactorInput_ZB005F54InputProps.input;
    const patternInput = useFeliz_React__React_useState_Static_1505(false);
    const isExtended = patternInput[0];
    const patternInput_1 = useFeliz_React__React_useState_Static_1505(Helper_FactorMutable_fromFactor_Z55333BD7(input));
    const state = patternInput_1[0];
    useReact_useEffect_311B4086(() => {
        patternInput_1[1](Helper_FactorMutable_fromFactor_Z55333BD7(input));
    }, [input]);
    const name = defaultArg(Helper_FactorMutable__get_Name(state), "<name>");
    const type$0027 = defaultArg(map((x) => x.NameText, Helper_FactorMutable__get_FactorType(state)), "<type>");
    const elms_2 = ofArray([(elms_1 = ofArray([createElement("div", createObj(Helpers_combineClasses("card-header-title", singleton((elems = [(children = ofArray([createElement("h5", {
        className: "title is-5",
        children: name,
    }), createElement("h6", {
        className: "subtitle is-6",
        children: type$0027,
    })]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    })), createElement("div", {
        style: {
            marginLeft: "auto",
        },
        children: (fields = ofArray([Helper_FactorMutable__get_Name(state), map((_arg_1) => "", Helper_FactorMutable__get_FactorType(state)), map((_arg_2) => "", Helper_FactorMutable__get_Comments(state))]), (all = (length(fields) | 0), `${length(choose((x_1) => x_1, fields))}/${all}`)),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))]))))), createElement("a", createObj(Helpers_combineClasses("card-header-icon", ofArray([["onClick", (_arg_4) => {
        patternInput[1](!isExtended);
    }], (elems_2 = [(elms = singleton(createElement("i", {
        className: join(" ", ["fas", "fa-angle-down"]),
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))]), createElement("header", {
        className: "card-header",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), createElement("div", createObj(Helpers_combineClasses("card-content", ofArray([["className", join(" ", toList(delay(() => (!isExtended ? singleton_1("is-hidden") : empty_1()))))], (elems_4 = toList(delay(() => {
        let tupledArg;
        return append(singleton_1((tupledArg = [Helper_FactorMutable__get_Name(state), "Name", (s_4) => {
            Helper_FactorMutable__set_Name_6DFDD678(state, s_4);
        }], createElement(FormComponents_TextInput_468641B7, {
            input: defaultArg(tupledArg[0], ""),
            label: tupledArg[1],
            setter: (s) => {
                tupledArg[2]((s === "") ? void 0 : s);
                setter(Helper_FactorMutable__ToFactor(state));
            },
            fullwidth: true,
        }))), delay(() => append(singleton_1(createElement(FormComponents_OntologyAnnotationInput_154CF37E, {
            input: defaultArg(Helper_FactorMutable__get_FactorType(state), OntologyAnnotation.empty),
            label: "Status",
            setter: (s_5) => {
                Helper_FactorMutable__set_FactorType_Z41EC1759(state, equals(s_5, OntologyAnnotation.empty) ? void 0 : s_5);
                setter(Helper_FactorMutable__ToFactor(state));
            },
        })), delay(() => append(singleton_1(FormComponents_CommentsInput_2201E571(defaultArg(Helper_FactorMutable__get_Comments(state), []), "Comments", (c) => {
            Helper_FactorMutable__set_Comments_Z1781AAC1(state, equalsWith(equals, c, []) ? void 0 : c);
            setter(Helper_FactorMutable__ToFactor(state));
        })), delay(() => ((deletebutton != null) ? singleton_1(Helper_deleteButton(value_36(deletebutton))) : empty_1())))))));
    })), ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))]);
    return createElement("div", {
        className: "card",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    });
}

export function FormComponents_FactorsInput_Z611A41CF(input, label, setter) {
    return createElement(FormComponents_InputSequence_6A786D4E, {
        "inputs\'": input,
        empty: Factor.create(),
        label: label,
        setter: setter,
        inputComponent: (tupledArg) => createElement(FormComponents_FactorInput_ZB005F54, {
            input: tupledArg[0],
            setter: tupledArg[2],
            deletebutton: tupledArg[3],
        }),
    });
}

export function FormComponents_OntologySourceReferenceInput_24F6272C(formComponents_OntologySourceReferenceInput_24F6272CInputProps) {
    let elms_1, elems, children, fields, all, elems_2, elms, elems_4;
    const deletebutton = formComponents_OntologySourceReferenceInput_24F6272CInputProps.deletebutton;
    const setter = formComponents_OntologySourceReferenceInput_24F6272CInputProps.setter;
    const input = formComponents_OntologySourceReferenceInput_24F6272CInputProps.input;
    const patternInput = useFeliz_React__React_useState_Static_1505(false);
    const isExtended = patternInput[0];
    const patternInput_1 = useFeliz_React__React_useState_Static_1505(Helper_OntologySourceReferenceMutable_fromOntologySourceReference_Z79C650B(input));
    const state = patternInput_1[0];
    useReact_useEffect_311B4086(() => {
        patternInput_1[1](Helper_OntologySourceReferenceMutable_fromOntologySourceReference_Z79C650B(input));
    }, [input]);
    const name = defaultArg(Helper_OntologySourceReferenceMutable__get_Name(state), "<name>");
    const version = defaultArg(Helper_OntologySourceReferenceMutable__get_Version(state), "<version>");
    const createFieldTextInput = (tupledArg) => createElement(FormComponents_TextInput_468641B7, {
        input: defaultArg(tupledArg[0], ""),
        label: tupledArg[1],
        setter: (s) => {
            tupledArg[2]((s === "") ? void 0 : s);
            setter(Helper_OntologySourceReferenceMutable__ToOntologySourceReference(state));
        },
        fullwidth: true,
    });
    const elms_2 = ofArray([(elms_1 = ofArray([createElement("div", createObj(Helpers_combineClasses("card-header-title", singleton((elems = [(children = ofArray([createElement("h5", {
        className: "title is-5",
        children: name,
    }), createElement("h6", {
        className: "subtitle is-6",
        children: version,
    })]), createElement("div", {
        children: Interop_reactApi.Children.toArray(Array.from(children)),
    })), createElement("div", {
        style: {
            marginLeft: "auto",
        },
        children: (fields = ofArray([Helper_OntologySourceReferenceMutable__get_Name(state), Helper_OntologySourceReferenceMutable__get_File(state), Helper_OntologySourceReferenceMutable__get_Version(state), Helper_OntologySourceReferenceMutable__get_Description(state), map((_arg_1) => "", Helper_OntologySourceReferenceMutable__get_Comments(state))]), (all = (length(fields) | 0), `${length(choose((x) => x, fields))}/${all}`)),
    })], ["children", Interop_reactApi.Children.toArray(Array.from(elems))]))))), createElement("a", createObj(Helpers_combineClasses("card-header-icon", ofArray([["onClick", (_arg_3) => {
        patternInput[1](!isExtended);
    }], (elems_2 = [(elms = singleton(createElement("i", {
        className: join(" ", ["fas", "fa-angle-down"]),
    })), createElement("span", {
        className: "icon",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    }))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))))]), createElement("header", {
        className: "card-header",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), createElement("div", createObj(Helpers_combineClasses("card-content", ofArray([["className", join(" ", toList(delay(() => (!isExtended ? singleton_1("is-hidden") : empty_1()))))], (elems_4 = toList(delay(() => append(singleton_1(createFieldTextInput([Helper_OntologySourceReferenceMutable__get_Name(state), "Name", (s_4) => {
        Helper_OntologySourceReferenceMutable__set_Name_6DFDD678(state, s_4);
    }])), delay(() => append(singleton_1(Helper_cardFormGroup(ofArray([createFieldTextInput([Helper_OntologySourceReferenceMutable__get_Version(state), "Version", (s_5) => {
        Helper_OntologySourceReferenceMutable__set_Version_6DFDD678(state, s_5);
    }]), createFieldTextInput([Helper_OntologySourceReferenceMutable__get_File(state), "File", (s_6) => {
        Helper_OntologySourceReferenceMutable__set_File_6DFDD678(state, s_6);
    }])]))), delay(() => append(singleton_1(createElement(FormComponents_TextInput_468641B7, {
        input: defaultArg(Helper_OntologySourceReferenceMutable__get_Description(state), ""),
        label: "Description",
        setter: (s_7) => {
            Helper_OntologySourceReferenceMutable__set_Description_6DFDD678(state, (s_7 === "") ? void 0 : s_7);
            setter(Helper_OntologySourceReferenceMutable__ToOntologySourceReference(state));
        },
        fullwidth: true,
        isarea: true,
    })), delay(() => append(singleton_1(FormComponents_CommentsInput_2201E571(defaultArg(Helper_OntologySourceReferenceMutable__get_Comments(state), []), "Comments", (c) => {
        Helper_OntologySourceReferenceMutable__set_Comments_Z1781AAC1(state, equalsWith(equals, c, []) ? void 0 : c);
        setter(Helper_OntologySourceReferenceMutable__ToOntologySourceReference(state));
    })), delay(() => ((deletebutton != null) ? singleton_1(Helper_deleteButton(value_36(deletebutton))) : empty_1()))))))))))), ["children", Interop_reactApi.Children.toArray(Array.from(elems_4))])]))))]);
    return createElement("div", {
        className: "card",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    });
}

export function FormComponents_OntologySourceReferencesInput_689B99B1(input, label, setter) {
    return createElement(FormComponents_InputSequence_6A786D4E, {
        "inputs\'": input,
        empty: OntologySourceReference.create(),
        label: label,
        setter: setter,
        inputComponent: (tupledArg) => createElement(FormComponents_OntologySourceReferenceInput_24F6272C, {
            input: tupledArg[0],
            setter: tupledArg[2],
            deletebutton: tupledArg[3],
        }),
    });
}

//# sourceMappingURL=Forms.js.map
