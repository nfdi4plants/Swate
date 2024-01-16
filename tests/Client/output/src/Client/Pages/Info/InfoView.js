import { createElement } from "react";
import * as react from "react";
import { keyValueList } from "../../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { createObj } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { Interop_reactApi } from "../../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { singleton, ofArray } from "../../../../fable_modules/fable-library.4.9.0/List.js";
import { HTMLAttr } from "../../../../fable_modules/Fable.React.9.3.0/Fable.React.Props.fs.js";
import { Docs_FileType, Docs_OntologyApi, Helpdesk_get_UrlSwateTopic, Helpdesk_get_Url } from "../../../Shared/URLs.js";
import { join } from "../../../../fable_modules/fable-library.4.9.0/String.js";
import { pageHeader } from "../../SidebarComponents/LayoutHelper.js";

export function introductionElement(model, dispatch) {
    let s_11;
    const props_14 = [["style", {
        textAlign: "justify",
    }]];
    const children_14 = [react.createElement("b", {}, "Swate"), " is a ", react.createElement("b", {}, "S"), "wate ", react.createElement("b", {}, "W"), "orkflow ", react.createElement("b", {}, "A"), "nnotation ", react.createElement("b", {}, "T"), "ool for ", react.createElement("b", {}, "E"), (s_11 = "xcel. This tool provides an easy way to annotate experimental data in an excel application that every wet lab scientist is familiar with. If you are interested check out the full ", s_11), react.createElement("a", {
        href: "https://nfdi4plants.org/nfdi4plants.knowledgebase/docs/implementation/SwateManual/index.html",
        target: "_blank",
    }, "documentation"), " 📚."];
    return react.createElement("p", keyValueList(props_14, 1), ...children_14);
}

export function iconContainer(left, icon) {
    let elems_1;
    return createElement("div", createObj(Helpers_combineClasses("field", ofArray([["className", "is-flex"], (elems_1 = [createElement("div", {
        style: {
            marginRight: 2 + "rem",
        },
        children: Interop_reactApi.Children.toArray(Array.from(left)),
    }), icon], ["children", Interop_reactApi.Children.toArray(Array.from(elems_1))])]))));
}

export function getInContactElement(model, dispatch) {
    let elems_2, children_8, props_5, children_2, props_3, children_12, props_13, s_11, props_25;
    return createElement("div", createObj(Helpers_combineClasses("content", ofArray([["style", {
        textAlign: "justify",
    }], (elems_2 = [createElement("label", {
        className: "label",
        children: "Get In Contact With Us",
    }), react.createElement("p", {}, "Swate is part of the DataPLANT organisation."), (children_8 = [(props_5 = [new HTMLAttr(94, ["https://nfdi4plants.de/"]), new HTMLAttr(157, ["_Blank"]), new HTMLAttr(158, ["DataPLANT"]), new HTMLAttr(65, ["nfdiIcon"]), ["style", {
        float: "right",
        marginLeft: "2em",
    }]], (children_2 = [(props_3 = [new HTMLAttr(149, ["https://raw.githubusercontent.com/nfdi4plants/Branding/138420e3b6f9ec9e125c1ca8840874b2be2a1262/logos/DataPLANT_logo_minimal_square_bg_darkblue.svg"]), ["style", {
        width: "54px",
    }]], react.createElement("img", keyValueList(props_3, 1)))], react.createElement("a", keyValueList(props_5, 1), ...children_2))), "Services and infrastructures to support ", react.createElement("a", {
        href: "https://twitter.com/search?q=%23FAIRData&src=hashtag_click",
    }, "#FAIRData"), " science and good data management practices within the plant basic research community. ", react.createElement("a", {
        href: "https://twitter.com/search?q=%23NFDI&src=hashtag_click",
    }, "#NFDI")], react.createElement("p", {}, ...children_8)), (children_12 = ["Got a good idea or just want to get in touch? ", (props_13 = [new HTMLAttr(94, [Helpdesk_get_Url()]), new HTMLAttr(157, ["_Blank"])], react.createElement("a", keyValueList(props_13, 1), "Reach out to us!"))], react.createElement("p", {}, ...children_12)), iconContainer(ofArray([createElement("span", {
        children: ["Follow us on Twitter for the more up-to-date information about research data management! "],
    }), react.createElement("a", {
        href: "https://twitter.com/nfdi4plants",
        target: "_Blank",
    }, "@nfdi4plants")]), createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["href", "https://twitter.com/nfdi4plants"], ["target", "_Blank"], ["title", "@nfdi4plants on Twitter"], ["className", "is-large"], ["children", createElement("i", {
        className: join(" ", ["fa-brands fa-twitter", "myFaBrand myFaTwitter", "is-size-3"]),
    })]]))))), iconContainer(ofArray(["You can find the Swate source code  ", react.createElement("a", {
        href: "https://github.com/nfdi4plants/Swate",
        target: "_Blank",
    }, "here"), (s_11 = ". Our developers are always happy to get in contact with you! If you don\'t have a GitHub account but want to reach out or want to snitch on some nasty bugs 🐛 you can tell us ", s_11), (props_25 = [new HTMLAttr(94, [Helpdesk_get_UrlSwateTopic()]), new HTMLAttr(157, ["_Blank"])], react.createElement("a", keyValueList(props_25, 1), "here")), "."]), createElement("span", createObj(Helpers_combineClasses("icon", ofArray([["href", "https://github.com/nfdi4plants/Swate"], ["target", "_Blank"], ["title", "Swate on GitHub"], ["className", "is-large"], ["children", createElement("i", {
        className: join(" ", ["fa-brands fa-github", "myFaBrand myFaGithub", "is-size-3"]),
    })]])))))], ["children", Interop_reactApi.Children.toArray(Array.from(elems_2))])]))));
}

export function infoComponent(model, dispatch) {
    let elms, elms_1, children_15, children_13, children_5, children_3, children_11, children_9, props_8, elms_2;
    const elms_3 = ofArray([pageHeader("Swate"), (elms = singleton(introductionElement(model, dispatch)), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms)),
    })), (elms_1 = singleton((children_15 = [createElement("label", {
        className: "label",
        children: "Documentation",
    }), (children_13 = [(children_5 = [(children_3 = [react.createElement("a", {
        href: "https://nfdi4plants.org/nfdi4plants.knowledgebase/docs/implementation/SwateManual/index.html",
        target: "_blank",
    }, "User documentation")], react.createElement("p", {}, ...children_3))], react.createElement("li", {}, ...children_5)), (children_11 = [(children_9 = ["OpenApi docs for ", (props_8 = [new HTMLAttr(94, [Docs_OntologyApi(new Docs_FileType(0, []))]), new HTMLAttr(157, ["_blank"])], react.createElement("a", keyValueList(props_8, 1), "IOntologyAPI")), "."], react.createElement("p", {}, ...children_9))], react.createElement("li", {}, ...children_11))], react.createElement("ul", {}, ...children_13))], react.createElement("div", {}, ...children_15))), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_1)),
    })), (elms_2 = singleton(getInContactElement(model, dispatch)), createElement("div", {
        className: "field",
        children: Interop_reactApi.Children.toArray(Array.from(elms_2)),
    }))]);
    return createElement("div", {
        className: "content",
        children: Interop_reactApi.Children.toArray(Array.from(elms_3)),
    });
}

//# sourceMappingURL=InfoView.js.map
