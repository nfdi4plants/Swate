import { createElement } from "react";
import { FormComponents_PersonsInput_4FAF87F1, FormComponents_OntologyAnnotationsInput_532A28D8, FormComponents_DateTimeInput_Z478DEF28, FormComponents_TextInput_468641B7, FormComponents_GUIDInput_25667B6A } from "./Forms.js";
import { parse } from "../../../../fable_modules/fable-library.4.9.0/Guid.js";
import { ARCtrlHelper_ArcFiles } from "../../../Shared/ARCtrl.Helper.js";
import { Msg } from "../../States/Spreadsheet.js";
import { Msg as Msg_1 } from "../../Messages.js";
import { toString } from "../../../../fable_modules/fable-library.4.9.0/Types.js";
import { Organisation } from "../../../../fable_modules/ARCtrl.1.0.4/Templates/Template.fs.js";
import { ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";

export function Main(template, model, dispatch) {
    const elms = ofArray([createElement(FormComponents_GUIDInput_25667B6A, {
        input: template.Id,
        label: "Identifier",
        setter: (s) => {
            template.Id = parse(s);
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
        },
        fullwidth: true,
    }), createElement(FormComponents_TextInput_468641B7, {
        input: template.Name,
        label: "Name",
        setter: (s_1) => {
            template.Name = s_1;
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
        },
        fullwidth: true,
    }), createElement(FormComponents_TextInput_468641B7, {
        input: template.Description,
        label: "Description",
        setter: (s_2) => {
            template.Description = s_2;
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
        },
        fullwidth: true,
        isarea: true,
    }), createElement(FormComponents_TextInput_468641B7, {
        input: toString(template.Organisation),
        label: "Organisation",
        setter: (s_3) => {
            template.Organisation = Organisation.ofString(s_3);
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
        },
        fullwidth: true,
    }), createElement(FormComponents_TextInput_468641B7, {
        input: template.Version,
        label: "Version",
        setter: (s_4) => {
            template.Version = s_4;
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
        },
        fullwidth: true,
    }), FormComponents_DateTimeInput_Z478DEF28(template.LastUpdated, "Last Updated", (dt) => {
        template.LastUpdated = dt;
        dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
    }), createElement(FormComponents_OntologyAnnotationsInput_532A28D8, {
        oas: template.Tags,
        label: "Tags",
        setter: (s_5) => {
            template.Tags = s_5;
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
        },
    }), createElement(FormComponents_OntologyAnnotationsInput_532A28D8, {
        oas: template.EndpointRepositories,
        label: "Endpoint Repositories",
        setter: (s_6) => {
            template.EndpointRepositories = s_6;
            dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
        },
    }), FormComponents_PersonsInput_4FAF87F1(template.Authors, "Authors", (s_7) => {
        template.Authors = s_7;
        dispatch(new Msg_1(15, [new Msg(24, [new ARCtrlHelper_ArcFiles(0, [template])])]));
    })]);
    return createElement("section", {
        className: "section",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    });
}

//# sourceMappingURL=Template.js.map
