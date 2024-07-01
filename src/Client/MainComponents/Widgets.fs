namespace MainComponents

open Feliz
open Feliz.Bulma
open Browser.Types

open LocalStorage.Widgets

module private InitExtensions =

    type Rect with

        static member initSizeFromPrefix(prefix: string) =
            match Size.load prefix with
            | Some p -> Some p
            | None -> None

        static member initPositionFromPrefix(prefix: string) =
            match Position.load prefix with
            | Some p -> Some p
            | None -> None


open InitExtensions

open Fable.Core
open Fable.Core.JsInterop
open Protocol

module private MoveEventListener =

    open Fable.Core.JsInterop

    let ensurePositionInsideWindow (element:IRefValue<HTMLElement option>) (position: Rect) =
        let maxX = Browser.Dom.window.innerWidth - element.current.Value.offsetWidth;
        let tempX = position.X
        let newX = System.Math.Min(System.Math.Max(tempX,0),int maxX)
        let maxY = Browser.Dom.window.innerHeight - element.current.Value.offsetHeight;
        let tempY = position.Y
        let newY = System.Math.Min(System.Math.Max(tempY,0),int maxY)
        {X = newX; Y = newY}

    let calculatePosition (element:IRefValue<HTMLElement option>) (startPosition: Rect) = fun (e: Event) ->
        let e : MouseEvent = !!e
        let tempX = int e.clientX - startPosition.X
        let tempY = int e.clientY - startPosition.Y
        let tempPosition = {X = tempX; Y = tempY}
        ensurePositionInsideWindow element tempPosition

    let onmousemove (element:IRefValue<HTMLElement option>) (startPosition: Rect) setPosition = fun (e: Event) ->
        let nextPosition = calculatePosition element startPosition e
        setPosition (Some nextPosition)

    let onmouseup (prefix,element:IRefValue<HTMLElement option>) onmousemove = 
        Browser.Dom.document.removeEventListener("mousemove", onmousemove)
        if element.current.IsSome then
            let rect = element.current.Value.getBoundingClientRect()
            let position = {X = int rect.left; Y = int rect.top}
            Position.write(prefix,position)

module private ResizeEventListener =

    open Fable.Core.JsInterop

    let onmousemove (startPosition: Rect) (startSize: Rect) setSize = fun (e: Event) ->
        let e : MouseEvent = !!e
        let width = int e.clientX - startPosition.X + startSize.X
        // I did not enable this, as it creates issues with overlays such as the term search dropdown. 
        // The widget card itself has overflow: visible, which makes a set height impossible, 
        // but wihout the visible overflow term search results might require scrolling.
        // // let height = int e.clientY - startPosition.Y + startSize.Y
        setSize (Some {X = width; Y = startSize.Y})

    let onmouseup (prefix, element: IRefValue<HTMLElement option>) onmousemove = 
        Browser.Dom.document.removeEventListener("mousemove", onmousemove)
        if element.current.IsSome then 
            Size.write(prefix,{X = int element.current.Value.offsetWidth; Y = int element.current.Value.offsetHeight})

module private Elements =

    let helpExtendButton (extendToggle: unit -> unit) =
        Bulma.help [
            prop.className "is-flex"
            prop.children [
                Html.a [
                    prop.text "Help"; 
                    prop.style [style.marginLeft length.auto; style.userSelect.none]
                    prop.onClick (fun e -> e.preventDefault(); e.stopPropagation(); extendToggle())
                ]
            ]
        ]

[<RequireQualifiedAccess>]
type Widget =
    | _BuildingBlock
    | _Template
    | _FilePicker

    [<ReactComponent>]
    static member Base(content: ReactElement, prefix: string, rmv: MouseEvent -> unit, ?help: ReactElement) =
        let position, setPosition = React.useState(fun _ -> Rect.initPositionFromPrefix prefix)
        let size, setSize = React.useState(fun _ -> Rect.initSizeFromPrefix prefix)
        let helpIsActive, setHelpIsActive = React.useState(false)
        let element = React.useElementRef()
        React.useLayoutEffectOnce(fun _ -> position |> Option.iter (fun position -> MoveEventListener.ensurePositionInsideWindow element position |> Some |> setPosition)) // Reposition widget inside window
        let resizeElement (content: ReactElement) =
            Bulma.card [
                prop.ref element
                prop.onMouseDown(fun e ->  // resize
                    e.preventDefault()
                    e.stopPropagation()
                    let startPosition = {X = int e.clientX; Y = int e.clientY}
                    let startSize = {X = int element.current.Value.offsetWidth; Y = int element.current.Value.offsetHeight}
                    let onmousemove = ResizeEventListener.onmousemove startPosition startSize setSize
                    let onmouseup = fun e -> ResizeEventListener.onmouseup (prefix, element) onmousemove
                    Browser.Dom.document.addEventListener("mousemove", onmousemove)
                    let config = createEmpty<AddEventListenerOptions>
                    config.once <- true
                    Browser.Dom.document.addEventListener("mouseup", onmouseup, config)
                )
                prop.style [
                    style.zIndex 40
                    style.cursor.eastWestResize//style.cursor.northWestSouthEastResize ; 
                    style.display.flex
                    style.paddingRight(2);
                    style.overflow.visible
                    style.position.fixedRelativeToWindow
                    style.minWidth.minContent
                    if size.IsSome then
                        style.width size.Value.X
                        //style.height size.Value.Y
                    if position.IsNone then
                        //style.transform.translate (length.perc -50,length.perc -50)
                        style.top (length.perc 20); style.left (length.perc 20); 
                    else
                        style.top position.Value.Y; style.left position.Value.X; 
                ]
                prop.children content
            ]
        resizeElement <| Html.div [
            prop.onMouseDown(fun e -> e.stopPropagation())
            prop.style [style.cursor.defaultCursor; style.display.flex; style.flexDirection.column; style.flexGrow 1]
            prop.children [
                Bulma.cardHeader [
                    prop.onMouseDown(fun e -> // move
                        e.preventDefault()
                        e.stopPropagation()
                        let x = e.clientX - element.current.Value.offsetLeft
                        let y = e.clientY - element.current.Value.offsetTop;
                        let startPosition = {X = int x; Y = int y}
                        let onmousemove = MoveEventListener.onmousemove element startPosition setPosition
                        let onmouseup = fun e -> MoveEventListener.onmouseup (prefix, element) onmousemove
                        Browser.Dom.document.addEventListener("mousemove", onmousemove)
                        let config = createEmpty<AddEventListenerOptions>
                        config.once <- true
                        Browser.Dom.document.addEventListener("mouseup", onmouseup, config)
                    )
                    prop.style [style.cursor.move]
                    prop.children [
                        Bulma.cardHeaderTitle.p Html.none
                        Bulma.cardHeaderIcon.a [
                            Bulma.delete [
                                prop.onClick (fun e -> e.stopPropagation(); rmv e)
                            ]
                        ]
                    ]
                ]
                Bulma.cardContent [
                    prop.style [style.overflow.inheritFromParent]
                    prop.children [
                        content
                        if help.IsSome then Elements.helpExtendButton (fun _ -> setHelpIsActive (not helpIsActive))
                    ]
                ]
                Bulma.cardFooter [
                    prop.style [style.padding 5]
                    if help.IsSome then
                        prop.children [
                            Bulma.content [
                                prop.className "widget-help-container"
                                prop.style [style.overflow.hidden; if not helpIsActive then style.display.none; ]
                                prop.children [
                                    help.Value
                                ]
                            ]
                        ]
                ]
            ]
        ]
        
    static member BuildingBlock (model, dispatch, rmv: MouseEvent -> unit) =
        let content = BuildingBlock.SearchComponent.Main model dispatch
        let help = Html.div [
            Html.p "Add a new Building Block."
            Html.ul [
                Html.li "If a cell is selected, a new Building Block is added to the right of the selected cell."
                Html.li "If no cell is selected, a new Building Block is appended at the right end of the table."
            ]
        ]
        let prefix = BuildingBlockWidgets
        Widget.Base(content, prefix, rmv, help)
        

    [<ReactComponent>]
    static member Templates (model: Messages.Model, dispatch, rmv: MouseEvent -> unit) =
        let templates, setTemplates = React.useState(model.ProtocolState.Templates)
        let config, setConfig = React.useState(TemplateFilterConfig.init)
        let filteredTemplates = Protocol.Search.filterTemplates (templates, config)
        React.useEffectOnce(fun _ -> Messages.Protocol.GetAllProtocolsRequest |> Messages.ProtocolMsg |> dispatch)
        React.useEffect((fun _ -> setTemplates model.ProtocolState.Templates), [|box model.ProtocolState.Templates|])
        let selectContent() = 
            [
                Protocol.Search.FileSortElement(model, config, setConfig)
                Protocol.Search.Component (filteredTemplates, model, dispatch, length.px 350)
            ]
        let insertContent() =
            [
                Bulma.field.div [
                    Protocol.TemplateFromDB.addFromDBToTableButton model dispatch
                ]
                Bulma.field.div [
                    prop.style [style.maxHeight (length.px 350); style.overflow.auto]
                    prop.children [
                        Protocol.TemplateFromDB.displaySelectedProtocolEle model dispatch
                    ]
                ]
            ]
        let content = 
            let switchContent = if model.ProtocolState.TemplateSelected.IsNone then selectContent() else insertContent()
            Html.div [
                prop.children switchContent
            ]
        
        let help = Protocol.Search.InfoField()
        let prefix = TemplatesWidgets
        Widget.Base(content, prefix, rmv, help)

    static member FilePicker (model, dispatch, rmv) =
        let content = Html.div [
            FilePicker.uploadButton model dispatch
            if model.FilePickerState.FileNames <> [] then
                FilePicker.fileSortElements model dispatch

                Bulma.field.div [
                    prop.style [style.maxHeight (length.px 350); style.overflow.auto]
                    prop.children [
                        FilePicker.FileNameTable.table model dispatch
                    ]
                ]
                //fileNameElements model dispatch
                FilePicker.insertButton model dispatch
        ]
        let prefix = FilePickerWidgets
        let help = Html.div []
        Widget.Base(content, prefix, rmv, help)