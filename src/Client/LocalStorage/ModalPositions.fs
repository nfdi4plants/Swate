module LocalStorage.Widgets

open Feliz
open Fable.Core.JsInterop

/// <summary>
/// Is not only used to store position but also size.
/// </summary>
type Rect = {
    X: int
    Y: int
} with 
    static member init () = {
        X = 0
        Y = 0
    }

open Fable.SimpleJson

let [<Literal>] BuildingBlockWidgets = "BuildingBlock"

[<RequireQualifiedAccess>]
module Position =

    open Browser

    let [<Literal>] private Key_Prefix = "WidgetsPosition_"

    let write(modalName:string, dt: Rect) = 
        let s = Json.serialize dt
        WebStorage.localStorage.setItem(Key_Prefix + modalName, s)

    let load(modalName:string) =
        let key = Key_Prefix + modalName
        try 
            WebStorage.localStorage.getItem(key)
            |> Json.parseAs<Rect>
            |> Some
        with
            |_ -> 
                WebStorage.localStorage.removeItem(key)
                printfn "Could not find %s" key
                None


[<RequireQualifiedAccess>]
module Size =
    open Browser

    let [<Literal>] private Key_Prefix = "WidgetsSize_"

    let write(modalName:string, dt: Rect) = 
        let s = Json.serialize dt
        WebStorage.localStorage.setItem(Key_Prefix + modalName, s)

    let load(modalName:string) =
        let key = Key_Prefix + modalName
        try 
            WebStorage.localStorage.getItem(key)
            |> Json.parseAs<Rect>
            |> Some
        with
            |_ -> 
                WebStorage.localStorage.removeItem(key)
                printfn "Could not find %s" key
                None
