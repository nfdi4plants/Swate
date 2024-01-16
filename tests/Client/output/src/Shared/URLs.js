import { replace } from "../../fable_modules/fable-library.4.9.0/String.js";
import { Union } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { class_type, union_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";

/**
 * accession string needs to have format: PO:0007131
 */
export function termAccessionUrlOfAccessionStr(accessionStr) {
    return "http://purl.obolibrary.org/obo/" + replace(accessionStr, ":", "_");
}

export class Docs_FileType extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Html", "Yaml"];
    }
}

export function Docs_FileType_$reflection() {
    return union_type("Shared.URLs.Docs.FileType", [], Docs_FileType, () => [[], []]);
}

export function Docs_FileType__get_toStr(this$) {
    if (this$.tag === 1) {
        return ".yaml";
    }
    else {
        return ".html";
    }
}

const Docs_Base = "/docs";

export function Docs_OntologyApi(filetype) {
    return (Docs_Base + "/IOntologyAPIv2") + Docs_FileType__get_toStr(filetype);
}

export class Helpdesk {
    constructor() {
    }
}

export function Helpdesk_$reflection() {
    return class_type("Shared.URLs.Helpdesk", void 0, Helpdesk);
}

export function Helpdesk_get_Url() {
    return "https://support.nfdi4plants.org";
}

export function Helpdesk_get_UrlSwateTopic() {
    return Helpdesk_get_Url() + "/?topic=Tools_Swate";
}

export function Helpdesk_get_UrlOntologyTopic() {
    return Helpdesk_get_Url() + "/?topic=Metadata_OntologyUpdate";
}

export function Helpdesk_get_UrlTemplateTopic() {
    return Helpdesk_get_Url() + "/?topic=Metadata_SwateTemplate";
}

//# sourceMappingURL=URLs.js.map
