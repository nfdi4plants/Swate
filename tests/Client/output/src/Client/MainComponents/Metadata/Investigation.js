import { createElement } from "react";
import { FormComponents_CommentsInput_2201E571, FormComponents_TextInputs_Z2C75BD80, FormComponents_PersonsInput_4FAF87F1, FormComponents_PublicationsInput_Z2B6713CF, FormComponents_OntologySourceReferencesInput_689B99B1, FormComponents_DateTimeInput_Z7B9B1480, FormComponents_TextInput_468641B7 } from "./Forms.js";
import { setInvestigationIdentifier } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/IdentifierSetters.fs.js";
import { ARCtrlHelper_ArcFiles } from "../../../Shared/ARCtrl.Helper.js";
import { Msg } from "../../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../../Messages.js";
import { defaultArg } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function Main(inv, model, dispatch) {
    const elms = ofArray([createElement(FormComponents_TextInput_468641B7, {
        input: inv.Identifier,
        label: "Identifier",
        setter: (s) => {
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [setInvestigationIdentifier(s, inv)])])]));
        },
        fullwidth: true,
    }), createElement(FormComponents_TextInput_468641B7, {
        input: defaultArg(inv.Title, ""),
        label: "Title",
        setter: (s_1) => {
            inv.Title = ((s_1 === "") ? void 0 : s_1);
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
        },
        fullwidth: true,
    }), createElement(FormComponents_TextInput_468641B7, {
        input: defaultArg(inv.Description, ""),
        label: "Description",
        setter: (s_2) => {
            inv.Description = ((s_2 === "") ? void 0 : s_2);
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
        },
        fullwidth: true,
        isarea: true,
    }), createElement(FormComponents_DateTimeInput_Z7B9B1480, {
        input: defaultArg(inv.SubmissionDate, ""),
        label: "Submission Date",
        setter: (s_3) => {
            inv.SubmissionDate = ((s_3 === "") ? void 0 : s_3);
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
        },
    }), createElement(FormComponents_DateTimeInput_Z7B9B1480, {
        input: defaultArg(inv.PublicReleaseDate, ""),
        label: "Public Release Date",
        setter: (s_4) => {
            inv.PublicReleaseDate = ((s_4 === "") ? void 0 : s_4);
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
        },
    }), FormComponents_OntologySourceReferencesInput_689B99B1(inv.OntologySourceReferences, "Ontology Source References", (oas) => {
        inv.OntologySourceReferences = oas;
        dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
    }), FormComponents_PublicationsInput_Z2B6713CF(inv.Publications, "Publications", (i) => {
        inv.Publications = i;
        dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
    }), FormComponents_PersonsInput_4FAF87F1(inv.Contacts, "Contacts", (i_1) => {
        inv.Contacts = i_1;
        dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
    }), createElement(FormComponents_TextInputs_Z2C75BD80, {
        texts: Array.from(inv.RegisteredStudyIdentifiers),
        label: "RegisteredStudyIdentifiers",
        setter: (i_2) => {
            inv.RegisteredStudyIdentifiers = Array.from(i_2);
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
        },
    }), FormComponents_CommentsInput_2201E571(inv.Comments, "Comments", (i_3) => {
        inv.Comments = i_3;
        dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(1, [inv])])]));
    })]);
    return createElement("section", {
        className: "section",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

//# sourceMappingURL=Investigation.js.map
