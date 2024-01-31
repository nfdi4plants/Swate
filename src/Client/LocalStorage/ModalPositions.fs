module LocalStorage.ModalPositions

open Feliz
open Fable.Core.JsInterop

type Position = {
    X: int
    Y: int
} with 
    static member init () = {
        X = 0
        Y = 0
    }

open Fable.SimpleJson

[<RequireQualifiedAccess>]
module LocalStorage =

    open Browser

    let [<Literal>] private DataTheme_Key_Prefix = "ModalPosition_"

    let write(modalName:string, dt: Position) = 
        let s = Json.serialize dt
        WebStorage.localStorage.setItem(DataTheme_Key_Prefix + modalName, s)

    let load(modalName:string) =
        let key = DataTheme_Key_Prefix + modalName
        try 
            WebStorage.localStorage.getItem(key)
            |> Json.parseAs<Position>
            |> Some
        with
            |_ -> 
                WebStorage.localStorage.removeItem(key)
                printfn "Could not find %s" key
                None

let [<Literal>] BuildingBlockModal = "BuildingBlock"
