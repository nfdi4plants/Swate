namespace MainComponents

open Feliz
open Feliz.Bulma
open Browser.Types

open LocalStorage.Modal

module InitExtensions =

    type Rect with
        static member initBuildingBlockPosition() =
            match Position.load BuildingBlockModal with
            | Some p -> Some p
            | None -> None       
            
        static member initBuildingBlockSize() =
            match Size.load BuildingBlockModal with
            | Some p -> Some p
            | None -> None

open InitExtensions

open Fable.Core
open Fable.Core.JsInterop

module MoveEventListener =

    open Fable.Core.JsInterop

    let onmousemove (element:IRefValue<HTMLElement option>) (startPosition: Rect) setPosition = fun (e: Event) ->
        let e : MouseEvent = !!e
        let maxX = Browser.Dom.window.innerWidth - element.current.Value.offsetWidth;
        let tempX = int e.clientX - startPosition.X
        let newX = System.Math.Min(System.Math.Max(tempX,0),int maxX)
        let maxY = Browser.Dom.window.innerHeight - element.current.Value.offsetHeight;
        let tempY = int e.clientY - startPosition.Y
        let newY = System.Math.Min(System.Math.Max(tempY,0),int maxY)
        setPosition (Some {X = newX; Y = newY})

    let onmouseup onmousemove = 
        Browser.Dom.document.removeEventListener("mousemove", onmousemove)

module ResizeEventListener =

    open Fable.Core.JsInterop

    let onmousemove (startPosition: Rect) (startSize: Rect) setSize = fun (e: Event) ->
        let e : MouseEvent = !!e
        let width = int e.clientX - startPosition.X + startSize.X
        //let height = int e.clientY - startPosition.Y + startSize.Y
        setSize (Some {X = width; Y = startSize.Y})

    let onmouseup onmousemove = 
        Browser.Dom.document.removeEventListener("mousemove", onmousemove)

module Elements =
    let resizeElement (content: ReactElement)  =
        Html.div [
            prop.style [style.cursor.northWestSouthEastResize; style.border(1, borderStyle.solid, "black")]
            prop.children content
        ]

type Modals =

    [<ReactComponent>]
    static member BuildingBlock (model, dispatch, rmv: MouseEvent -> unit) =
        let position, setPosition = React.useState(Rect.initBuildingBlockPosition)
        let size, setSize = React.useState(Rect.initBuildingBlockSize)
        let element = React.useElementRef()
        let resizeElement (content: ReactElement) =
            Bulma.modalCard [
                prop.ref element
                prop.onMouseDown(fun e -> 
                    e.preventDefault()
                    e.stopPropagation()
                    let startPosition = {X = int e.clientX; Y = int e.clientY}
                    let startSize = {X = int element.current.Value.offsetWidth; Y = int element.current.Value.offsetHeight}
                    let onmousemove = ResizeEventListener.onmousemove startPosition startSize setSize
                    let onmouseup = fun e -> 
                        ResizeEventListener.onmouseup onmousemove
                        if size.IsSome then 
                            Size.write(BuildingBlockModal,{X = int element.current.Value.offsetWidth; Y = int element.current.Value.offsetHeight})
                    Browser.Dom.document.addEventListener("mousemove", onmousemove)
                    let config = createEmpty<AddEventListenerOptions>
                    config.once <- true
                    Browser.Dom.document.addEventListener("mouseup", onmouseup, config)
                )
                prop.style [
                    style.cursor.eastWestResize; style.display.flex
                    style.padding(2); style.overflow.visible
                    style.position.absolute
                    if size.IsSome then
                        style.width size.Value.X
                    if position.IsSome then
                        style.top position.Value.Y; style.left position.Value.X; 
                ]
                prop.children content
            ]
        resizeElement <| Html.div [
            prop.children [
                Bulma.modalCardHead [
                    prop.onMouseDown(fun e -> 
                        e.preventDefault()
                        e.stopPropagation()
                        let x = e.clientX - element.current.Value.offsetLeft
                        let y = e.clientY - element.current.Value.offsetTop;
                        let startPosition = {X = int x; Y = int y}
                        let onmousemove = MoveEventListener.onmousemove element startPosition setPosition
                        let onmouseup = fun e -> 
                            MoveEventListener.onmouseup onmousemove
                            if position.IsSome then 
                                Position.write(BuildingBlockModal,position.Value)
                        Browser.Dom.document.addEventListener("mousemove", onmousemove)
                        let config = createEmpty<AddEventListenerOptions>
                        config.once <- true
                        Browser.Dom.document.addEventListener("mouseup", onmouseup, config)
                    )
                    prop.style [style.cursor.move]
                    prop.children [
                        Bulma.modalCardTitle Html.none
                        Bulma.delete [ prop.onClick rmv ]
                    ]
                ]
                Bulma.modalCardBody [
                    prop.style [style.overflow.inheritFromParent]
                    prop.children [
                        BuildingBlock.SearchComponent.Main model dispatch
                    ]
                ]
            ]
        ]


