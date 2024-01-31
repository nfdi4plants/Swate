namespace MainComponents

open Feliz
open Feliz.Bulma
open Browser.Types

open LocalStorage.ModalPositions

module InitExtensions =

    type Position with
        static member initBuildingBlock() =
            match LocalStorage.load LocalStorage.ModalPositions.BuildingBlockModal with
            | Some p -> p
            | None -> Position.init()            

open InitExtensions

type Modals =

    [<ReactComponent>]
    static member BuildingBlock (model, dispatch, rmv: MouseEvent -> unit) =
        let position, setPosition = React.useState(Position.initBuildingBlock)
        let startPosition, setStartPosition = React.useState(None)
        let isMoving, setIsMoving = React.useState false
        let element = React.useElementRef()
        Bulma.modalCard [
            prop.ref element
            prop.style [style.overflow.visible; style.top position.Y; style.left position.X; style.position.absolute]
            prop.children [
                Bulma.modalCardHead [
                    prop.onMouseDown(fun e -> 
                        let x = e.clientX - element.current.Value.offsetLeft
                        let y = e.clientY - element.current.Value.offsetTop;
                        setStartPosition <| Some {X = int x; Y = int y}
                        setIsMoving true
                    )
                    prop.onMouseMove(fun e ->
                        if isMoving then
                            log "move"
                            let maxX = Browser.Dom.window.innerWidth - element.current.Value.offsetWidth;
                            let tempX = int e.clientX - startPosition.Value.X
                            let newX = System.Math.Min(System.Math.Max(tempX,0),int maxX)
                            let maxY = Browser.Dom.window.innerHeight - element.current.Value.offsetHeight;
                            let tempY = int e.clientY - startPosition.Value.Y
                            let newY = System.Math.Min(System.Math.Max(tempY,0),int maxY)
                            setPosition {X = newX; Y = newY}
                    )
                    prop.onMouseUp(fun _ -> 
                        setStartPosition None
                        LocalStorage.write(BuildingBlockModal,position)
                        setIsMoving false
                    )
                    prop.style [style.cursor.move]
                    prop.children [
                        Bulma.modalCardTitle Html.none
                        Bulma.delete [ prop.onClick rmv ]
                    ]
                ]
                Bulma.modalCardBody [
                    BuildingBlock.SearchComponent.Main model dispatch
                ]
            ]
        ]


