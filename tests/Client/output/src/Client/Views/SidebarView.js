import { Record } from "../../../fable_modules/fable-library.4.9.0/Types.js";
import { WindowSize_ofWidth_Z524259A4, WindowSize, WindowSize_$reflection } from "../Model.js";
import { obj_type, record_type } from "../../../fable_modules/fable-library.4.9.0/Reflection.js";
import { Route, Route__get_toStringRdbl, Route_toIcon_Z5040D2F2, Route__isActive_Z5040D2F2 } from "../Routing.js";
import { createElement } from "react";
import React from "react";
import * as react from "react";
import { createObj } from "../../../fable_modules/fable-library.4.9.0/Util.js";
import { Helpers_combineClasses } from "../../../fable_modules/Feliz.Bulma.3.0.0/ElementBuilders.fs.js";
import { empty, singleton, append, delay, toList } from "../../../fable_modules/fable-library.4.9.0/Seq.js";
import { TopLevelMsg, Msg } from "../Messages.js";
import { Interop_reactApi } from "../../../fable_modules/Feliz.2.7.0/Interop.fs.js";
import { ofArray } from "../../../fable_modules/fable-library.4.9.0/List.js";
import { printf, toText } from "../../../fable_modules/fable-library.4.9.0/String.js";
import { keyValueList } from "../../../fable_modules/fable-library.4.9.0/MapUtil.js";
import { DOMAttr, HTMLAttr } from "../../../fable_modules/Fable.React.9.3.0/Fable.React.Props.fs.js";
import { termSearchComponent } from "../Pages/TermSearch/TermSearchView.js";
import { filePickerComponent } from "../Pages/FilePicker/FilePickerView.js";
import { fileUploadViewComponent } from "../Pages/ProtocolTemplates/ProtocolView.js";
import { jsonExporterMainElement } from "../Pages/JsonExporter/JsonExporter.js";
import { newNameMainElement } from "../Pages/TemplateMetadata/TemplateMetadata.js";
import { ProtocolSearchView } from "../Pages/ProtocolTemplates/ProtocolSearchView.js";
import { activityLogComponent } from "../Pages/ActivityLog/ActivityLogView.js";
import { settingsViewComponent } from "../Pages/Settings/SettingsView.js";
import { mainElement } from "../Pages/Dag/Dag.js";
import { infoComponent } from "../Pages/Info/InfoView.js";
import { notFoundComponent } from "./NotFoundView.js";
import { addBuildingBlockComponent } from "../Pages/BuildingBlock/BuildingBlockView.js";
import { useReact_useState_FCFD9EF } from "../../../fable_modules/Feliz.2.7.0/React.fs.js";
import { NavbarComponent } from "../SidebarComponents/Navbar.js";
import { annotationTableMissingWarningComponent } from "../SidebarComponents/AnnotationTableMissingWarning.js";

class SidebarStyle extends Record {
    constructor(Size) {
        super();
        this.Size = Size;
    }
}

function SidebarStyle_$reflection() {
    return record_type("SidebarView.SidebarStyle", [], SidebarStyle, () => [["Size", WindowSize_$reflection()]]);
}

function SidebarStyle_init() {
    return new SidebarStyle(new WindowSize(3, []));
}

function createNavigationTab(pageLink, model, dispatch, sidebarsize) {
    const isActive = Route__isActive_Z5040D2F2(pageLink, model.PageState.CurrentPage);
    return createElement("li", createObj(Helpers_combineClasses("", toList(delay(() => append(isActive ? singleton(["className", "is-active"]) : empty(), delay(() => singleton(["children", createElement("a", createObj(toList(delay(() => append(singleton(["onClick", (e) => {
        e.preventDefault();
        dispatch(new Msg(19, [pageLink]));
    }]), delay(() => {
        const matchValue = sidebarsize;
        switch (matchValue.tag) {
            case 0:
            case 1:
                return singleton(["children", Route_toIcon_Z5040D2F2(pageLink)]);
            default:
                return singleton(["children", Route__get_toStringRdbl(pageLink)]);
        }
    }))))))]))))))));
}

function tabRow(model, tabs_1) {
    return createElement("div", createObj(Helpers_combineClasses("tabs", ofArray([["className", "is-centered"], ["className", "is-fullwidth"], ["className", "is-boxed"], ["style", {
        paddingTop: 1 + "rem",
        borderBottom: (((2 + "px ") + "solid") + " ") + "#1FC2A7",
    }], ["children", Interop_reactApi.Children.toArray(Array.from(tabs_1))]]))));
}

function tabs(model, dispatch, sidebarsize) {
    const isIEBrowser = window.document.documentMode;
    return tabRow(model, toList(delay(() => (!model.PageState.IsExpert ? append(singleton(createNavigationTab(new Route(1, []), model, dispatch, sidebarsize)), delay(() => append(singleton(createNavigationTab(new Route(2, []), model, dispatch, sidebarsize)), delay(() => append(singleton(createNavigationTab(new Route(5, []), model, dispatch, sidebarsize)), delay(() => append(singleton(createNavigationTab(new Route(3, []), model, dispatch, sidebarsize)), delay(() => singleton(createNavigationTab(new Route(4, []), model, dispatch, sidebarsize)))))))))) : append(singleton(createNavigationTab(new Route(8, []), model, dispatch, sidebarsize)), delay(() => append(singleton(createNavigationTab(new Route(9, []), model, dispatch, sidebarsize)), delay(() => singleton(createNavigationTab(new Route(4, []), model, dispatch, sidebarsize))))))))));
}

function footer(model) {
    let children_2;
    const props_5 = [["style", {
        color: "grey",
        position: "sticky",
        width: "inherit",
        bottom: "0",
        textAlign: "center",
    }]];
    const children_4 = [(children_2 = ["Swate Release Version ", react.createElement("a", {
        href: "https://github.com/nfdi4plants/Swate/releases",
    }, model.PersistentStorageState.AppVersion), " Host ", createElement("span", {
        style: {
            color: "#4fb3d9",
        },
        children: toText(printf("%O"))(model.PersistentStorageState.Host),
    })], react.createElement("div", {}, ...children_2))];
    return react.createElement("div", keyValueList(props_5, 1), ...children_4);
}

class ResizeObserver_ResizeObserverEntry extends Record {
    constructor(borderBoxSize, contentBoxSize, contentRect, devicePixelContentBoxSize, target) {
        super();
        this.borderBoxSize = borderBoxSize;
        this.contentBoxSize = contentBoxSize;
        this.contentRect = contentRect;
        this.devicePixelContentBoxSize = devicePixelContentBoxSize;
        this.target = target;
    }
}

function ResizeObserver_ResizeObserverEntry_$reflection() {
    return record_type("SidebarView.ResizeObserver.ResizeObserverEntry", [], ResizeObserver_ResizeObserverEntry, () => [["borderBoxSize", obj_type], ["contentBoxSize", obj_type], ["contentRect", obj_type], ["devicePixelContentBoxSize", obj_type], ["target", obj_type]]);
}

function ResizeObserver_observer(state, setState) {
    return new ResizeObserver((ele) => {
        setState(new SidebarStyle(WindowSize_ofWidth_Z524259A4(ele[0].contentRect.width)));
    });
}

function viewContainer(model, dispatch, state, setState, children) {
    const props = [new HTMLAttr(99, ["SidebarContainer-ID"]), new DOMAttr(13, [(e) => {
        const ele = document.getElementById("SidebarContainer-ID");
        ResizeObserver_observer(state, setState).observe(ele);
    }]), new DOMAttr(40, [(e_1) => {
        if (model.TermSearchState.ShowSuggestions ? true : model.AddBuildingBlockState.ShowUnit2TermSuggestions) {
            dispatch(new Msg(18, [new TopLevelMsg()]));
        }
    }]), ["style", {
        display: "flex",
        flexGrow: "1",
        flexDirection: "column",
        position: "relative",
        maxWidth: "100%",
    }]];
    return react.createElement("div", keyValueList(props, 1), ...children);
}

function Content_main(model, dispatch) {
    const matchValue = model.PageState.CurrentPage;
    switch (matchValue.tag) {
        case 2:
            return termSearchComponent(model, dispatch);
        case 3:
            return filePickerComponent(model, dispatch);
        case 5:
            return fileUploadViewComponent(model, dispatch);
        case 8:
            return jsonExporterMainElement(model, dispatch);
        case 9:
            return newNameMainElement(model, dispatch);
        case 6:
            return createElement(ProtocolSearchView, {
                model: model,
                dispatch: dispatch,
            });
        case 10:
            return activityLogComponent(model, dispatch);
        case 11:
            return settingsViewComponent(model, dispatch);
        case 7:
            return mainElement(model, dispatch);
        case 4:
            return infoComponent(model, dispatch);
        case 12:
            return notFoundComponent(model, dispatch);
        default:
            return addBuildingBlockComponent(model, dispatch);
    }
}

/**
 * The base react component for the sidebar view in the app. contains the navbar and takes body and footer components to create the full view.
 */
export function SidebarView(sidebarViewInputProps) {
    let elems;
    const dispatch = sidebarViewInputProps.dispatch;
    const model = sidebarViewInputProps.model;
    const patternInput = useReact_useState_FCFD9EF(SidebarStyle_init);
    const state = patternInput[0];
    return viewContainer(model, dispatch, state, patternInput[1], ofArray([createElement(NavbarComponent, {
        model: model,
        dispatch: dispatch,
        sidebarsize: state.Size,
    }), createElement("div", createObj(Helpers_combineClasses("container", ofArray([["className", "is-fluid"], (elems = toList(delay(() => append(singleton(tabs(model, dispatch, state.Size)), delay(() => {
        let matchValue, matchValue_1;
        return append((matchValue = model.PersistentStorageState.Host, (matchValue_1 = !model.ExcelState.HasAnnotationTable, (matchValue != null) ? ((matchValue.tag === 1) ? (matchValue_1 ? singleton(annotationTableMissingWarningComponent(model, dispatch)) : (empty())) : (empty())) : (empty()))), delay(() => singleton(Content_main(model, dispatch))));
    })))), ["children", Interop_reactApi.Children.toArray(Array.from(elems))])])))), footer(model)]));
}

//# sourceMappingURL=SidebarView.js.map
