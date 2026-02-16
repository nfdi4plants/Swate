module Renderer.components.Widgets

open Feliz

open ARCtrl

open Swate.Components

open LocalStorage.Widgets

open MainComponents
open MainComponents.InitExtensions


[<RequireQualifiedAccess>]
    type DropdownPage =
        | Main
        | More
        | IOTypes of CompositeHeaderDiscriminate

        member this.toString =
            match this with
            | Main -> "Main Page"
            | More -> "More"
            | IOTypes t -> t.ToString()

        member this.toTooltip =
            match this with
            | More -> "More"
            | IOTypes t -> $"Per table only one {t} is allowed. The value of this column must be a unique identifier."
            | _ -> ""

type BuildingBlockUIState = {
    DropdownIsActive: bool
    DropdownPage: DropdownPage
} with

    static member init() = {
        DropdownIsActive = false
        DropdownPage = DropdownPage.Main
    }

let addWidget widgets setWidgets (widget: MainComponents.Widget) =
    let add (widget) widgets =
        widget :: widgets |> List.rev |> setWidgets

    if widgets |> List.contains widget then
        List.filter (fun w -> w <> widget) widgets
        |> fun filteredWidgets -> add widget filteredWidgets
    else
        add widget widgets

[<ReactComponent>]
let BaseWidget (prefix: string) (content: ReactElement) (onRemove: Browser.Types.MouseEvent -> unit) =
    let position, setPosition = React.useState (fun _ -> Rect.initPositionFromPrefix prefix)
    let size, setSize = React.useState (fun _ -> Rect.initSizeFromPrefix prefix)

    let innerWidth, setInnerWidth = React.useState (fun _ -> Browser.Dom.window.innerWidth)
    let innerHeight, setInnerHeight = React.useState (fun _ -> Browser.Dom.window.innerHeight)

    let element = React.useElementRef ()

    ResizeEventListener.windowSizeChange setInnerWidth setInnerHeight

    let debouncedAdaptElement =
        React.useDebouncedCallback (
            fun () ->
                match position, size with
                | Some p, Some s ->
                    ResizeEventListener.adaptElement (int innerWidth) (int innerHeight) s p setSize setPosition
                | _ -> ()
            , 100
        )

    React.useEffectOnce (fun _ -> debouncedAdaptElement ())
    React.useEffect ((fun () -> debouncedAdaptElement ()), [| box innerWidth; box innerHeight |])

    React.useLayoutEffectOnce (fun _ ->
        position
        |> Option.iter (fun p ->
            MoveEventListener.ensurePositionInsideWindow element p
            |> Some
            |> setPosition
        )
    )

    let startMove (e: Browser.Types.MouseEvent) =
        e.preventDefault()
        e.stopPropagation()

        let startMouse = { X = int e.clientX; Y = int e.clientY }

        let startPos =
            match position with
            | Some p -> p
            | None ->
                { X = int element.current.Value.offsetLeft
                  Y = int element.current.Value.offsetTop }

        let onmousemove : Browser.Types.Event -> unit =
            fun ev ->
                let me = ev :?> Browser.Types.MouseEvent
                let dx = int me.clientX - startMouse.X
                let dy = int me.clientY - startMouse.Y
                setPosition (Some { X = startPos.X + dx; Y = startPos.Y + dy })

        let onmouseup : Browser.Types.Event -> unit =
            fun _ ->
                Browser.Dom.document.removeEventListener("mousemove", onmousemove)

        Browser.Dom.document.addEventListener("mousemove", onmousemove)
        let opts = Fable.Core.JsInterop.createEmpty<Browser.Types.AddEventListenerOptions>
        opts.once <- true
        Browser.Dom.document.addEventListener("mouseup", onmouseup, opts)

    let startResize (e: Browser.Types.MouseEvent) =
        e.preventDefault()
        e.stopPropagation()

        let startPosition : Rect = { X = int e.clientX; Y = int e.clientY }
        let startSize : Rect = { X = int element.current.Value.offsetWidth; Y = int element.current.Value.offsetHeight }

        let onmousemove = ResizeEventListener.onmousemove startPosition startSize setSize
        let onmouseup = fun _ -> ResizeEventListener.onmouseup (prefix, element) onmousemove

        Browser.Dom.document.addEventListener("mousemove", onmousemove)
        let opts = Fable.Core.JsInterop.createEmpty<Browser.Types.AddEventListenerOptions>
        opts.once <- true
        Browser.Dom.document.addEventListener("mouseup", onmouseup, opts)

    Html.div [
        prop.ref element
        prop.className "swt:shadow-md swt:border swt:border-base-300 swt:rounded-lg swt:border-r-2 swt:bg-base-100"
        prop.style [
            style.zIndex 999
            style.position.fixedRelativeToWindow
            style.display.flex
            style.flexDirection.column

            style.minWidth 320
            style.minHeight 200

            if size.IsSome then
                style.width size.Value.X
                style.height size.Value.Y

            if position.IsNone then
                style.top (length.perc 20)
                style.left (length.perc 20)
            else
                style.top position.Value.Y
                style.left position.Value.X
        ]
        prop.children [
            Html.div [
                prop.className "swt:cursor-move swt:flex swt:justify-between swt:items-center swt:px-2 swt:py-1 swt:bg-gradient-to-br swt:from-primary swt:to-base-200 swt:rounded-t-lg"
                prop.onMouseDown startMove
                prop.children [
                    Html.span [ prop.className "swt:text-sm"; prop.text prefix ]
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost swt:btn-xs"
                        prop.text "×"
                        prop.onClick onRemove
                    ]
                ]
            ]
            Html.div [
                prop.className "swt:flex-1 swt:overflow-auto swt:p-2"
                prop.children [ content ]
            ]
            Html.div [
                prop.className "swt:opacity-60 hover:swt:opacity-100"
                prop.style [
                    style.position.absolute
                    style.right 6
                    style.bottom 6
                    style.width 14
                    style.height 14
                    style.cursor.northWestSouthEastResize
                ]
                prop.onMouseDown startResize
            ]
        ]
    ]

let prefixOf (w: MainComponents.Widget) =
    match w with
    | MainComponents.Widget._Template -> WidgetLiterals.Templates
    | MainComponents.Widget._FilePicker -> WidgetLiterals.FilePicker
    | MainComponents.Widget._DataAnnotator -> WidgetLiterals.DataAnnotator
    | MainComponents.Widget._BuildingBlock -> WidgetLiterals.BuildingBlock

[<ReactComponent>]
let searchComponent model dispatch tableName : ReactElement =
    let state_bb, setState_bb = React.useState (BuildingBlockUIState.init)

    let ctx =
        React.useContext (Swate.Components.Contexts.AnnotationTable.AnnotationTableStateCtx)

    let xIndex =
        ctx.state
        |> Map.tryFind tableName
        |> Option.bind (fun x -> x.SelectedCells)
        |> Option.map (fun cells -> cells.xEnd)
        |> Option.map (fun x -> x - 2)

    let callback =
        fun () -> SearchComponentHelper.addBuildingBlock xIndex model dispatch

    Html.div [
        Html.form [
            prop.className "swt:flex swt:flex-col swt:gap-4 swt:p-2"
            prop.onSubmit (fun ev -> ev.preventDefault ()
            )
            prop.children [
                SearchComponent.SearchBuildingBlockHeaderElement(state_bb, setState_bb, model, dispatch)
                if model.AddBuildingBlockState.HeaderCellType.IsTermColumn() then
                    SearchComponent.SearchBuildingBlockBodyElement(model, dispatch)
                SearchComponent.AddBuildingBlockButton(model, callback)
                Html.input [ prop.type'.submit; prop.style [ style.display.none ] ]
            ]
        ]
    ]

let createBuildingBlockWidget () =
    Html.div [
        prop.title "Building Block"
        prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-y-hidden"
    ]

[<ReactComponent>]
let WidgetView (widget: MainComponents.Widget) (onRemove: Browser.Types.MouseEvent -> unit) (onBringToFront: unit -> unit) =
    let content =
        match widget with
        | MainComponents.Widget._BuildingBlock -> createBuildingBlockWidget()
        | MainComponents.Widget._Template ->
            Html.div [
                prop.title "Template"
                prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-y-hidden"
            ]
        | MainComponents.Widget._FilePicker ->
            Html.div [
                prop.title "FilePicker"
                prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-y-hidden"
            ]
        | MainComponents.Widget._DataAnnotator ->
            Html.div [
                prop.title "DataAnnotator"
                prop.className "swt:flex swt:flex-col swt:gap-2 swt:overflow-y-hidden"
            ]

    Html.div [
        prop.onMouseDown (fun _ -> onBringToFront())
        prop.children [
            BaseWidget (prefixOf widget) content onRemove
        ]
    ]

[<ReactComponent>]
let FloatingWidgetLayer (widgets: MainComponents.Widget list) (setWidgets: MainComponents.Widget list -> unit) =

    let rmvWidget w = widgets |> List.except [w] |> setWidgets
    let bringToFront w =
        widgets |> List.except [w] |> fun rest -> (w :: rest |> List.rev) |> setWidgets

    Html.div [
        for w in widgets do
            Html.div [
                prop.key (string w)
                prop.children [
                    WidgetView
                        w
                        (fun e -> e.stopPropagation(); rmvWidget w)
                        (fun () -> bringToFront w)
                ]
            ]
    ]
