import { createElement } from "react";
import { FormComponents_CommentsInput_2201E571, FormComponents_PersonsInput_4FAF87F1, FormComponents_OntologyAnnotationInput_154CF37E, FormComponents_TextInput_468641B7 } from "./Forms.js";
import { setAssayIdentifier } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/IdentifierSetters.fs.js";
import { ARCtrlHelper_ArcFiles } from "../../../Shared/ARCtrl.Helper.js";
import { Msg } from "../../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../../Messages.js";
import { defaultArg } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { OntologyAnnotation } from "../../../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologyAnnotation.fs.js";
import { equals } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function Main(assay, model, dispatch) {
    const elms = ofArray([createElement(FormComponents_TextInput_468641B7, {
        input: assay.Identifier,
        label: "Identifier",
        setter: (s) => {
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(3, [setAssayIdentifier(s, assay)])])]));
        },
        fullwidth: true,
    }), createElement(FormComponents_OntologyAnnotationInput_154CF37E, {
        input: defaultArg(assay.MeasurementType, OntologyAnnotation.empty),
        label: "Measurement Type",
        setter: (oa) => {
            const oa_1 = equals(oa, OntologyAnnotation.empty) ? void 0 : oa;
            assay.MeasurementType = oa_1;
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(3, [assay])])]));
        },
    }), createElement(FormComponents_OntologyAnnotationInput_154CF37E, {
        input: defaultArg(assay.TechnologyType, OntologyAnnotation.empty),
        label: "Technology Type",
        setter: (oa_2) => {
            const oa_3 = equals(oa_2, OntologyAnnotation.empty) ? void 0 : oa_2;
            assay.TechnologyType = oa_3;
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(3, [assay])])]));
        },
    }), createElement(FormComponents_OntologyAnnotationInput_154CF37E, {
        input: defaultArg(assay.TechnologyPlatform, OntologyAnnotation.empty),
        label: "Technology Platform",
        setter: (oa_4) => {
            const oa_5 = equals(oa_4, OntologyAnnotation.empty) ? void 0 : oa_4;
            assay.TechnologyPlatform = oa_5;
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(3, [assay])])]));
        },
    }), FormComponents_PersonsInput_4FAF87F1(assay.Performers, "Performers", (persons) => {
        assay.Performers = persons;
        dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(3, [assay])])]));
    }), FormComponents_CommentsInput_2201E571(assay.Comments, "Comments", (comments) => {
        assay.Comments = comments;
        dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(3, [assay])])]));
    })]);
    return createElement("section", {
        className: "section",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

//# sourceMappingURL=Assay.js.map
