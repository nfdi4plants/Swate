module Routing

open Elmish.UrlParser
open Feliz
open Swate.Components

[<RequireQualifiedAccess>]
type SidebarPage =
    | BuildingBlock
    | TermSearch
    | FilePicker
    | Protocol
    | JsonExport
    | DataAnnotator

    member this.AsStringRdbl =
        match this with
        | BuildingBlock -> "Building Blocks"
        | TermSearch -> "Terms"
        | FilePicker -> "File Picker"
        | Protocol -> "Templates"
        | JsonExport -> "Json Export"
        | DataAnnotator -> "Data Annotator"

    member this.AsIcon() =
        let createElem (icons: ReactElement) =
            Html.i [ prop.title this.AsStringRdbl; prop.children [ icons ] ]

        match this with
        | TermSearch -> createElem <| Icons.Terms()
        | BuildingBlock -> createElem <| Icons.BuildingBlock()
        | Protocol -> createElem <| Icons.Templates()
        | JsonExport -> createElem <| Icons.FileExport()
        | FilePicker -> createElem <| Icons.FilePicker()
        | DataAnnotator -> createElem <| Icons.DataAnnotator()

[<RequireQualifiedAccess>]
type MainPage =
    | Default
    | About
    | PrivacyPolicy
    | Settings

    member this.AsStringRdbl =
        match this with
        | About -> "About"
        | PrivacyPolicy -> "Privacy Policy"
        | Settings -> "Settings"
        | Default -> "Home"

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Route = Home of int option

///explained here: https://elmish.github.io/browser/routing.html
//let curry f x y = f (x,y)

module Routing =

    open Elmish
    open Elmish.Navigation

    /// The URL is turned into a Result.
    let route: Parser<Route -> Route, _> =
        oneOf [ map Route.Home (s "" <?> intParam "is_swatehost") ]

    let parsePath (location: Browser.Types.Location) : Route option =
        Elmish.UrlParser.parsePath route location