namespace Swate.Components

open Fable.Core
open Feliz

type WidgetType = Swate.Components.Widgets.Contexts.WidgetType

type WidgetDefinition = Swate.Components.Widgets.Contexts.WidgetDefinition

type WidgetControllerContext = Swate.Components.Widgets.Contexts.WidgetControllerContext

[<Erase; Mangle(false)>]
module WidgetContext =

    type WidgetControllerContext = Swate.Components.Widgets.Contexts.WidgetControllerContext

    let WidgetControllerCtx = Swate.Components.Widgets.Contexts.WidgetControllerCtx

    let ActiveWidgetContext = Swate.Components.Widgets.Contexts.ActiveWidgetContext

    [<Hook>]
    let useWidgetControllerCtx () = Swate.Components.Widgets.Contexts.useWidgetControllerCtx ()

    [<Hook>]
    let useWidgetController () = Swate.Components.Widgets.Contexts.useWidgetController ()
