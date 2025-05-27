import { ReactElement, createElement } from "react";
import { FSharpList, ofArray } from "../fable_modules/fable-library-ts.4.24.0/List.js";
import { reactApi } from "../fable_modules/Feliz.2.9.0/./Interop.fs.js";

export function BuildingBlock(): ReactElement {
    const children: FSharpList<ReactElement> = ofArray([createElement<any>("i", {
        className: "fa-solid fa-circle-plus",
    }), createElement<any>("i", {
        className: "fa-solid fa-table-columns",
    })]);
    return createElement<any>("span", {
        children: reactApi.Children.toArray(Array.from(children)),
    });
}

export function FilePicker(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-file-signature",
    });
}

export function DataAnnotator(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-object-group",
    });
}

export function FileExport(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-file-export",
    });
}

export function Terms(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-magnifying-glass-plus",
    });
}

export function Templates(): ReactElement {
    const children: FSharpList<ReactElement> = ofArray([createElement<any>("i", {
        className: "fa-solid fa-circle-plus",
    }), createElement<any>("i", {
        className: "fa-solid fa-table",
    })]);
    return createElement<any>("span", {
        children: reactApi.Children.toArray(Array.from(children)),
    });
}

export function Settings(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-cog",
    });
}

export function About(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-question-circle",
    });
}

export function PrivacyPolicy(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-fingerprint",
    });
}

export function Docs(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-book",
    });
}

export function Contact(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-comments",
    });
}

export function Save(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-floppy-disk",
    });
}

export function Delete(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-trash-can",
    });
}

export function Forward(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-rotate-right",
    });
}

export function Back(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-rotate-left",
    });
}

export function BuildingBlockInformation(): ReactElement {
    const children: FSharpList<ReactElement> = ofArray([createElement<any>("i", {
        className: "fa-solid fa-question pr-1",
    }), createElement<any>("i", {
        className: "fa-solid fa-table-columns",
    })]);
    return createElement<any>("span", {
        children: reactApi.Children.toArray(Array.from(children)),
    });
}

export function RemoveBuildingBlock(): ReactElement {
    const children: FSharpList<ReactElement> = ofArray([createElement<any>("i", {
        className: "fa-solid fa-minus pr-1",
    }), createElement<any>("i", {
        className: "fa-solid fa-table-columns",
    })]);
    return createElement<any>("span", {
        children: reactApi.Children.toArray(Array.from(children)),
    });
}

export function RectifyOntologyTerms(reactElement: ReactElement): ReactElement {
    const children: FSharpList<ReactElement> = ofArray([createElement<any>("i", {
        className: "fa-solid fa-spell-check",
    }), reactElement, createElement<any>("i", {
        className: "fa-solid fa-pen",
    })]);
    return createElement<any>("span", {
        children: reactApi.Children.toArray(Array.from(children)),
    });
}

export function AutoformatTable(): ReactElement {
    return createElement<any>("i", {
        className: "fa-solid fa-rotate",
    });
}

export function CreateAnnotationTable(): ReactElement {
    const children: FSharpList<ReactElement> = ofArray([createElement<any>("i", {
        className: "fa-solid fa-plus",
    }), createElement<any>("i", {
        className: "fa-solid fa-table",
    })]);
    return createElement<any>("span", {
        children: reactApi.Children.toArray(Array.from(children)),
    });
}

export function CreateMetadata(): ReactElement {
    const children: FSharpList<ReactElement> = ofArray([createElement<any>("i", {
        className: "fa-solid fa-plus",
    }), createElement<any>("i", {
        className: "fa-solid fa-info",
    })]);
    return createElement<any>("span", {
        children: reactApi.Children.toArray(Array.from(children)),
    });
}

