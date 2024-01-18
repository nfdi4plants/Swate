module SplitWindowView

open Feliz
open Elmish
open Browser.Types
open LocalStorage.SplitWindow

[<Literal>]
let private sidebarId = "sidebar_window"
let private minWidth = 400

let private calculateNewSideBarSize (model:SplitWindow) (pos:float) =
    let windowWidth = Browser.Dom.window.innerWidth
    // must cast to MouseEvent here. Earlier and `Browser.Dom.document.addEventListener` will make problems, and without i cannot access `pageX`.
    let newWidthSide_pre = windowWidth - pos - model.ScrollbarWidth
    let maxWidth = int Browser.Dom.window.innerWidth - minWidth - int model.ScrollbarWidth
    // Make sure it does not exceed maxWidth, to prevent increasing over screen size.
    let newWidthSide = System.Math.Min(newWidthSide_pre,maxWidth)
    newWidthSide

let private onResize_event (model:SplitWindow) (setModel: SplitWindow -> unit) = (fun (e: Event) ->
    /// must get width like this, cannot propagate model correctly.
    let sidebarWindow = Browser.Dom.document.getElementById(sidebarId).clientWidth
    let windowWidth = Browser.Dom.window.innerWidth
    let new_sidebarWidth = calculateNewSideBarSize model (windowWidth - sidebarWindow)
    { model with RightWindowWidth = new_sidebarWidth } |> setModel
)
    
/// <summary> This event changes the size of main window and sidebar </summary>
let private mouseMove_event (model:SplitWindow) (setModel: SplitWindow -> unit) = (fun (e: Event) ->
    let pos = (e :?> MouseEvent).clientX
    let new_sidebarWidth = calculateNewSideBarSize model pos
    //propagateWindowSize new_sidebarWidth dispatch 
    //Browser.Dom.console.log("side: ", newWidthSide)
    { model with RightWindowWidth = new_sidebarWidth } |> setModel
)

let rec private mouseUp_event (mouseMove : Event -> unit) = (fun e ->
    //Browser.Dom.console.log("UP")
    Browser.Dom.document.removeEventListener("mousemove", mouseMove)
    Browser.Dom.document.removeEventListener("mouseup", mouseUp_event mouseMove)
)

open Fable.Core.JsInterop

/// The only event directly referenced in `dragbar`.
/// Will add the mousemove event (updating the RightWindowWidth) and the mouseup event (will remove both mousemove and mouseup --itself-- event)
let private mouseDown_event (mouseMove : Event -> unit) : Event -> unit = (fun e ->
    e.preventDefault()
    //Browser.Dom.console.log("DOWN")
    Browser.Dom.document.addEventListener("mousemove", mouseMove)
    // Without options = !!{|once = true|} this somehow gets not removed.
    Browser.Dom.document.addEventListener("mouseup", mouseUp_event mouseMove, options = !!{|once = true; capture = false; passive = false|})
)

let private dragbar (model:SplitWindow) (setModel: SplitWindow -> unit) (dispatch: Messages.Msg -> unit) =
    Html.div [
        prop.style [
            style.minWidth (length.px 3)
            style.width (length.px 3)
            style.height (length.perc 100)
            style.backgroundColor.darkGray
            style.cursor.columnResize
            style.zIndex 39
        ]
        prop.onMouseDown <| mouseDown_event (mouseMove_event model setModel)
    ]

let exampleTerm =
    Shared.TermTypes.createTerm
        "MS:1023810"
        "instrument model"
        "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet."
        false
        "MS"

// https://jsfiddle.net/gaby/Bek9L/
// https://stackoverflow.com/questions/6219031/how-can-i-resize-a-div-by-dragging-just-one-side-of-it
/// Splits screen into two parts. Left and right, with a dragbar in between to change size of right side.
[<ReactComponent>]
let Main (left:seq<Fable.React.ReactElement>) (right:seq<Fable.React.ReactElement>) (dispatch: Messages.Msg -> unit) =
    let (model, setModel) = React.useState(SplitWindow.init)
    React.useEffect(model.WriteToLocalStorage, [|box model|])
    React.useEffectOnce(fun _ -> Browser.Dom.window.addEventListener("resize", onResize_event model setModel))
    Html.div [
        prop.style [
            style.display.flex
        ]
        prop.children [
            Html.div [
                prop.style [
                    style.minWidth(minWidth)
                    style.flexGrow 1
                    style.flexShrink 1
                    style.height(length.vh 100)
                    style.width(length.perc 100)
                ]
                prop.children left
            ]
            Html.div [
                prop.id sidebarId
                prop.style [
                    style.float'.right;
                    style.minWidth(minWidth);
                    style.flexBasis(length.px model.RightWindowWidth); style.flexShrink 0; style.flexGrow 0
                    style.height(length.vh 100)
                    style.width(length.perc 100)
                    style.overflow.hidden
                    style.display.flex
                ]
                prop.children [
                    dragbar model setModel dispatch
                    yield! right 
                ]
            ]
        ]
    ]