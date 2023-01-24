module SplitWindowView

open Feliz
open Elmish
open Browser.Types

/// Without this the scrollbar will offset the splitWindowElement
let mutable private InitScrollbarWidth = 0.0

let private minWidth = 300

/// If you change anything here. Make sure it is only added ONCE and then removed ONCE!
/// Note: Add the commented console.logs to ensure.
let private setInitWidth =
    let scrollDiv = Browser.Dom.document.createElement "div"
    scrollDiv.className <- "scrollbar-measure"
    //Browser.Dom.console.log("add")
    ignore <| Browser.Dom.document.body.appendChild(scrollDiv)
    let sw = scrollDiv.offsetWidth - scrollDiv.clientWidth
    InitScrollbarWidth <- sw
    //Browser.Dom.console.log("remove")
    scrollDiv.remove()
    
type private SplitWindow = {
    ScrollbarWidth      : float
    RightWindowWidth    : float
} with
    static member init() =
        let initSideBar = 400
        {
            ScrollbarWidth      = InitScrollbarWidth
            RightWindowWidth    = initSideBar
        }

let private mouseMove_event (model:SplitWindow) (setModel: SplitWindow -> unit) = (fun (e: Event) ->
    let windowWidth = Browser.Dom.window.innerWidth
    // must cast to MouseEvent here. Earlier and `Browser.Dom.document.addEventListener` will make problems, and without i cannot access `pageX`.
    let pos = (e :?> MouseEvent).clientX
    let newWidthSide = windowWidth - pos - model.ScrollbarWidth
    //Browser.Dom.console.log("side: ", newWidthSide)
    { model with RightWindowWidth = newWidthSide } |> setModel
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

let private dragbar (model:SplitWindow) (setModel: SplitWindow -> unit) =
    Html.div [
        prop.style [
            style.position.absolute
            style.width (length.px 5)
            style.height (length.perc 100)
            style.float'.left
            style.backgroundColor.darkGray
            style.cursor.columnResize
            style.zIndex 9999
        ]
        prop.onMouseDown <| mouseDown_event (mouseMove_event model setModel)
    ]

// https://jsfiddle.net/gaby/Bek9L/
// https://stackoverflow.com/questions/6219031/how-can-i-resize-a-div-by-dragging-just-one-side-of-it
/// Splits screen into two parts. Left and right, with a dragbar in between to change size of right side.
[<ReactComponent>]
let Main (left:seq<Fable.React.ReactElement>) (right:seq<Fable.React.ReactElement>) =
    let (model, setModel) = React.useState(SplitWindow.init)
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
                prop.style [
                    style.float'.right;
                    //style.width (length.px model.RightWindowWidth); // might not be necessary
                    style.minWidth(minWidth);
                    style.flexBasis(length.px model.RightWindowWidth); style.flexShrink 0; style.flexGrow 0
                    style.height(length.vh 100)
                    style.width(length.perc 100)
                    style.overflow.auto
                ]
                prop.children [
                    dragbar model setModel
                    yield! right 
                ]
            ]
        ]
    ]