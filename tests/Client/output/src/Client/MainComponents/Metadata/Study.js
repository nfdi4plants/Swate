import { createElement } from "react";
import { FormComponents_CommentsInput_2201E571, FormComponents_FactorsInput_Z611A41CF, FormComponents_TextInputs_Z2C75BD80, FormComponents_OntologyAnnotationsInput_532A28D8, FormComponents_PersonsInput_4FAF87F1, FormComponents_PublicationsInput_Z2B6713CF, FormComponents_DateTimeInput_Z7B9B1480, FormComponents_TextInput_468641B7 } from "./Forms.js";
import { setStudyIdentifier } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/IdentifierSetters.fs.js";
import { ARCtrlHelper_ArcFiles } from "../../../Shared/ARCtrl.Helper.js";
import { Msg } from "../../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../../Messages.js";
import { defaultArg } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function Main(study, assignedAssays, model, dispatch) {
    const elms = ofArray([createElement(FormComponents_TextInput_468641B7, {
        input: study.Identifier,
        label: "Identifier",
        setter: (s) => {
            let tupledArg;
            dispatch(new Msg_1(15, [new Msg(24, [(tupledArg = [setStudyIdentifier(s, study), assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg[0], tupledArg[1]]))])]));
        },
        fullwidth: true,
    }), createElement(FormComponents_TextInput_468641B7, {
        input: defaultArg(study.Description, ""),
        label: "Description",
        setter: (s_1) => {
            let tupledArg_1;
            const s_2 = (s_1 === "") ? void 0 : s_1;
            study.Description = s_2;
            dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_1 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_1[0], tupledArg_1[1]]))])]));
        },
        fullwidth: true,
        isarea: true,
    }), createElement(FormComponents_DateTimeInput_Z7B9B1480, {
        input: defaultArg(study.SubmissionDate, ""),
        label: "Submission Date",
        setter: (s_3) => {
            let tupledArg_2;
            const s_4 = (s_3 === "") ? void 0 : s_3;
            study.SubmissionDate = s_4;
            dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_2 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_2[0], tupledArg_2[1]]))])]));
        },
    }), createElement(FormComponents_DateTimeInput_Z7B9B1480, {
        input: defaultArg(study.PublicReleaseDate, ""),
        label: "Public ReleaseDate",
        setter: (s_5) => {
            let tupledArg_3;
            const s_6 = (s_5 === "") ? void 0 : s_5;
            study.PublicReleaseDate = s_6;
            dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_3 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_3[0], tupledArg_3[1]]))])]));
        },
    }), FormComponents_PublicationsInput_Z2B6713CF(study.Publications, "Publications", (pubs) => {
        let tupledArg_4;
        study.Publications = pubs;
        dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_4 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_4[0], tupledArg_4[1]]))])]));
    }), FormComponents_PersonsInput_4FAF87F1(study.Contacts, "Contacts", (persons) => {
        let tupledArg_5;
        study.Contacts = persons;
        dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_5 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_5[0], tupledArg_5[1]]))])]));
    }), createElement(FormComponents_OntologyAnnotationsInput_532A28D8, {
        oas: study.StudyDesignDescriptors,
        label: "Study Design Descriptors",
        setter: (oas) => {
            let tupledArg_6;
            study.StudyDesignDescriptors = oas;
            dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_6 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_6[0], tupledArg_6[1]]))])]));
        },
    }), createElement(FormComponents_TextInputs_Z2C75BD80, {
        texts: Array.from(study.RegisteredAssayIdentifiers),
        label: "Registered Assay Identifiers",
        setter: (rais) => {
            let tupledArg_7;
            study.RegisteredAssayIdentifiers = Array.from(rais);
            dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_7 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_7[0], tupledArg_7[1]]))])]));
        },
    }), FormComponents_FactorsInput_Z611A41CF(study.Factors, "Factors", (factors) => {
        let tupledArg_8;
        study.Factors = factors;
        dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_8 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_8[0], tupledArg_8[1]]))])]));
    }), FormComponents_CommentsInput_2201E571(study.Comments, "Comments", (comments) => {
        let tupledArg_9;
        study.Comments = comments;
        dispatch(new Msg_1(15, [new Msg(24, [(tupledArg_9 = [study, assignedAssays], new ARCtrlHelper_ArcFiles(2, [tupledArg_9[0], tupledArg_9[1]]))])]));
    })]);
    return createElement("section", {
        className: "section",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

//# sourceMappingURL=Study.js.map
