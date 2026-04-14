module Swate.Components.Widgets.Contexts

open Fable.Core
open Feliz

[<RequireQualifiedAccess>]
[<StringEnum>]
type WidgetType =
    | BuildingBlock
    | Template
    | FilePicker
    | DataAnnotator
    | Playground

type WidgetDefinition = {|
    prefix: string
    content: ReactElement
|}

type WidgetControllerContext = {
    activeWidgets: WidgetType list
    isActive: WidgetType -> bool
    openWidget: WidgetType -> unit
    closeWidget: WidgetType -> unit
    toggleWidget: WidgetType -> unit
    focusWidget: WidgetType -> unit
}

module WidgetControllerContext =

    let init () = {
        activeWidgets = []
        isActive = fun _ -> false
        openWidget = fun _ -> ()
        closeWidget = fun _ -> ()
        toggleWidget = fun _ -> ()
        focusWidget = fun _ -> ()
    }

let WidgetControllerCtx =
    React.createContext<WidgetControllerContext> (WidgetControllerContext.init ())

let ActiveWidgetContext = WidgetControllerCtx

[<Hook>]
let useWidgetControllerCtx () = React.useContext WidgetControllerCtx

[<Hook>]
let useWidgetController () = useWidgetControllerCtx ()
