module rec LocalStorage.Darkmode

open Feliz
open Fable.Core.JsInterop

[<RequireQualifiedAccess>]
module private Attribute =
    let getDataTheme() =
        let v = Browser.Dom.document.documentElement.getAttribute("data-theme")
        if isNull v then 
            None 
        else 
            DataTheme.ofString v |> Some

    let setDataTheme(theme: string) =
        Browser.Dom.document.documentElement.setAttribute("data-theme", theme.ToLower() )

[<RequireQualifiedAccess>]
module private BrowserSetting =
    let getDefault() =
        let m : bool = Browser.Dom.window?matchMedia("(prefers-color-scheme: dark)")?matches
        // Browser.Dom.console.log(m)
        if m then Dark else Light

[<RequireQualifiedAccess>]
module private LocalStorage =

    open Browser

    let [<Literal>] DataTheme_Key = "DataTheme"

    let write(dt: DataTheme) = 
        let s = string dt
        WebStorage.localStorage.setItem(DataTheme_Key, s)

    let load() =
        try 
            WebStorage.localStorage.getItem(DataTheme_Key)
            |> DataTheme.ofString
            |> Some
        with
            |_ -> 
                WebStorage.localStorage.removeItem(DataTheme_Key)
                printfn "Could not find %s" DataTheme_Key
                None

type DataTheme =
| Dark 
| Light
    static member ofString (str: string) =
        match str.ToLower() with
        | "dark" -> Dark
        | "light" | _ -> Light
        
    static member SET(theme:DataTheme) =
        Attribute.setDataTheme <| string theme
        LocalStorage.write <| theme // This helps remember

    static member GET() =
        // Check local storage
        let localStorage = LocalStorage.load()
        // check data theme attribute
        let dataTheme = Attribute.getDataTheme()
        match localStorage, dataTheme with
        | _, Some dt -> dt // this value actually decides the theme via styles.scss
        | Some dt, _ -> dt // this value is set by website but does not reflect actual styling directly
        | _, _ -> BrowserSetting.getDefault() // if all fails we check for the browser setting
    member this.isDark = this = Dark

    member this.toIcon : ReactElement =
        let c = 
            match this with
            | Light -> "fa-solid fa-lightbulb"
            | Dark -> "fa-solid fa-moon"
        Html.i [
            prop.className (sprintf "%s fa-xl" c)
        ]

[<RequireQualifiedAccess>]
type State = {
    Theme: DataTheme
    SetTheme: State -> unit
} with 
    static member init() = 
        let dt = DataTheme.GET()
        DataTheme.SET dt
        {
            Theme = dt
            SetTheme = fun (state) -> failwith "This is not implemented and serves as placeholder"
        }

let themeContext = React.createContext(name="Theme", defaultValue=State.init())