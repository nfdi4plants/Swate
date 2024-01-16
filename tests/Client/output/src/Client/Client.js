import { empty, delay, toList, singleton } from "../../fable_modules/fable-library.4.9.0/Seq.js";
import { createElement } from "react";
import React from "react";
import { Main } from "./Views/MainWindowView.js";
import { SidebarView } from "./Views/SidebarView.js";
import { Main as Main_1 } from "./Views/SplitWindowView.js";
import { React_contextProvider_34D9BBBD, useReact_useState_FCFD9EF } from "../../fable_modules/Feliz.2.7.0/React.fs.js";
import { State, themeContext, State_init } from "./LocalStorage/Darkmode.js";
import { equals } from "../../fable_modules/fable-library.4.9.0/Util.js";
import { Swatehost } from "./Host.js";
import { singleton as singleton_1 } from "../../fable_modules/fable-library.4.9.0/List.js";
import { initEventListener } from "./ARCitect/Interop.js";
import { EventHandler } from "./ARCitect/ARCitect.js";
import { ProgramModule_mkProgram, ProgramModule_withSubscription, ProgramModule_run } from "../../fable_modules/Fable.Elmish.4.1.0/program.fs.js";
import { Program_withReactBatched } from "../../fable_modules/Fable.Elmish.React.4.0.0/react.fs.js";
import { ProgramModule_toNavigable } from "../../fable_modules/Fable.Elmish.Browser.4.0.3/navigation.fs.js";
import { parsePath } from "../../fable_modules/Fable.Elmish.Browser.4.0.3/parser.fs.js";
import { Routing_route } from "./Routing.js";
import { update, urlUpdate } from "./Update.js";
import { init } from "./Init.js";
import "../../../../../src/Client/style.scss";


/**
 * This is a basic test case used in Client unit tests
 */
export function sayHello(name) {
    return `Hello ${name}`;
}

function split_container(model, dispatch) {
    const mainWindow = singleton(createElement(Main, {
        model: model,
        dispatch: dispatch,
    }));
    const sideWindow = singleton(createElement(SidebarView, {
        model: model,
        dispatch: dispatch,
    }));
    return createElement(Main_1, {
        left: mainWindow,
        right: sideWindow,
        dispatch: dispatch,
    });
}

export function View(viewInputProps) {
    let matchValue;
    const dispatch = viewInputProps.dispatch;
    const model = viewInputProps.model;
    const patternInput = useReact_useState_FCFD9EF(State_init);
    return React_contextProvider_34D9BBBD(themeContext, new State(patternInput[0].Theme, patternInput[1]), (matchValue = model.PersistentStorageState.Host, (matchValue != null) ? ((matchValue.tag === 1) ? createElement(SidebarView, {
        model: model,
        dispatch: dispatch,
    }) : split_container(model, dispatch)) : split_container(model, dispatch)));
}

export function ARCitect_subscription(initial) {
    return toList(delay(() => (equals(initial.PersistentStorageState.Host, new Swatehost(2, [])) ? singleton([singleton_1("ARCitect"), (dispatch) => {
        const rmv = initEventListener(EventHandler(dispatch));
        return {
            Dispose() {
                rmv();
            },
        };
    }]) : empty())));
}

ProgramModule_run(Program_withReactBatched("elmish-app", ProgramModule_toNavigable((location) => parsePath(Routing_route, location), urlUpdate, ProgramModule_withSubscription(ARCitect_subscription, ProgramModule_mkProgram(init, update, (model_1, dispatch) => createElement(View, {
    model: model_1,
    dispatch: dispatch,
}))))));

//# sourceMappingURL=Client.js.map
