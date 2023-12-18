module LocalStorage.SplitWindow

open Feliz
open Fable.Core.JsInterop
open Fable.SimpleJson

type SplitWindow = {
    ScrollbarWidth      : float
    RightWindowWidth    : float
} 

[<RequireQualifiedAccess>]
module private LocalStorage =

    open Browser

    let [<Literal>] Key = "SplitWindow"

    let write(m: SplitWindow) = 
        let jsonString = Json.serialize m
        WebStorage.localStorage.setItem(Key, jsonString)

    let load() =
        try 
            WebStorage.localStorage.getItem(Key)
            |> Json.parseAs<SplitWindow>
            |> Some
        with
            |_ -> 
                WebStorage.localStorage.removeItem(Key)
                printfn "Could not find %s" Key
                None

type SplitWindow with
    static member init() =
        let initSidebarWidth = 400
        let tryFromStorage = LocalStorage.load()
        match tryFromStorage with
        | Some m -> m
        | None ->   
            /// Without this the scrollbar will offset the splitWindowElement
            let mutable InitScrollbarWidth = 0.0
            /// If you change anything here. Make sure it is only added ONCE and then removed ONCE!
            /// Note: Add the commented console.logs to ensure.
            let setInitWidth =
                let scrollDiv = Browser.Dom.document.createElement "div"
                scrollDiv.className <- "scrollbar-measure"
                //Browser.Dom.console.log("add")
                ignore <| Browser.Dom.document.body.appendChild(scrollDiv)
                let sw = scrollDiv.offsetWidth - scrollDiv.clientWidth
                InitScrollbarWidth <- sw
                //Browser.Dom.console.log("remove")
                scrollDiv.remove()
            {
                ScrollbarWidth      = InitScrollbarWidth
                RightWindowWidth    = initSidebarWidth
            }
    member this.WriteToLocalStorage() = LocalStorage.write this
