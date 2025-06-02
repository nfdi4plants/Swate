import { Union } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Types.js";
import { class_type, union_type, TypeInfo } from "../Components/src/fable_modules/fable-library-ts.4.24.0/Reflection.js";

export type Docs_FileType_$union = 
    | Docs_FileType<0>
    | Docs_FileType<1>

export type Docs_FileType_$cases = {
    0: ["Html", []],
    1: ["Yaml", []]
}

export function Docs_FileType_Html() {
    return new Docs_FileType<0>(0, []);
}

export function Docs_FileType_Yaml() {
    return new Docs_FileType<1>(1, []);
}

export class Docs_FileType<Tag extends keyof Docs_FileType_$cases> extends Union<Tag, Docs_FileType_$cases[Tag][0]> {
    constructor(readonly tag: Tag, readonly fields: Docs_FileType_$cases[Tag][1]) {
        super();
    }
    cases() {
        return ["Html", "Yaml"];
    }
}

export function Docs_FileType_$reflection(): TypeInfo {
    return union_type("Swate.Components.Shared.URLs.Docs.FileType", [], Docs_FileType, () => [[], []]);
}

export function Docs_FileType__get_toStr(this$: Docs_FileType_$union): string {
    if (this$.tag === /* Yaml */ 1) {
        return ".yaml";
    }
    else {
        return ".html";
    }
}

const Docs_Base = "/docs";

export function Docs_OntologyApi(filetype: Docs_FileType_$union): string {
    return (Docs_Base + "/IOntologyAPIv2") + Docs_FileType__get_toStr(filetype);
}

export class Helpdesk {
    constructor() {
    }
}

export function Helpdesk_$reflection(): TypeInfo {
    return class_type("Swate.Components.Shared.URLs.Helpdesk", undefined, Helpdesk);
}

export function Helpdesk_get_Url(): string {
    return "https://support.nfdi4plants.org";
}

export function Helpdesk_get_UrlSwateTopic(): string {
    return Helpdesk_get_Url() + "/?topic=Tools_Swate";
}

export function Helpdesk_get_UrlOntologyTopic(): string {
    return Helpdesk_get_Url() + "/?topic=Metadata_OntologyUpdate";
}

export function Helpdesk_get_UrlTemplateTopic(): string {
    return Helpdesk_get_Url() + "/?topic=Metadata_SwateTemplate";
}

export const CONTACT: string = Helpdesk_get_Url();

