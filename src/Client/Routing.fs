module Routing

open Elmish.UrlParser
open Feliz

[<RequireQualifiedAccess>]
type SidebarPage =
    | BuildingBlock
    | TermSearch
    | FilePicker
    | Protocol
    | ProtocolSearch
    | JsonExport
    | DataAnnotator

    member this.AsStringRdbl =
        match this with
        | BuildingBlock     -> "Building Blocks"
        | TermSearch        -> "Terms"
        | FilePicker        -> "File Picker"
        | Protocol          -> "Templates"
        | ProtocolSearch    -> "Template Search"
        | JsonExport        -> "Json Export"
        | DataAnnotator     -> "Data Annotator"

    member this.AsIcon() =
        let createElem (icons: ReactElement list) =
            Html.i [
                prop.title this.AsStringRdbl
                prop.children icons
            ]

        match this with
        | TermSearch        ->
            createElem [Html.i [prop.className "fa-solid fa-magnifying-glass-plus" ]]
        | BuildingBlock     ->
            createElem [ Html.i [prop.className "fa-solid fa-circle-plus" ]; Html.i [prop.className "fa-solid fa-table-columns" ]]
        | Protocol          ->
            createElem [ Html.i [prop.className "fa-solid fa-circle-plus" ];Html.i [prop.className "fa-solid fa-table" ]]
        | ProtocolSearch    ->
            createElem [ Html.i [prop.className "fa-solid fa-table" ]; Html.i [prop.className "fa-solid fa-magnifying-glass" ]]
        | JsonExport        ->
            createElem [ Html.i [prop.className "fa-solid fa-file-export" ]]
        | FilePicker        ->
            createElem [ Html.i [prop.className "fa-solid fa-file-signature" ]]
        | DataAnnotator     ->
            createElem [ Html.i [prop.className "fa-solid fa-object-group" ]]

[<RequireQualifiedAccess>]
type MainPage =
    | Default
    | About
    | PrivacyPolicy
    | Settings

    member this.AsStringRdbl =
        match this with
        | About         -> "About"
        | PrivacyPolicy -> "Privacy Policy"
        | Settings      -> "Settings"
        | Default       -> "Home"

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Route =
| Home of int option

///explained here: https://elmish.github.io/browser/routing.html
//let curry f x y = f (x,y)

module Routing =

    open Elmish
    open Elmish.Navigation

    /// The URL is turned into a Result.
    let route : Parser<Route -> Route, _> =
        oneOf [
            map Route.Home                  (s "" <?> intParam "is_swatehost")
        ]


    let parsePath (location:Browser.Types.Location) : Route option = Elmish.UrlParser.parsePath route location