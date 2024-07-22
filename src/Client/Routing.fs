module Routing

open Elmish.UrlParser
open Feliz
open Feliz.Bulma

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Route =
| Home of int option
| BuildingBlock
| TermSearch
| FilePicker
| Info
| Protocol
| ProtocolSearch
| JsonExport
| DataAnnotator
| ActivityLog
| Settings
| NotFound

    member this.toStringRdbl =
        match this with
        | Home _ | Route.BuildingBlock  -> "Building Blocks"
        | Route.TermSearch          -> "Terms"
        | Route.FilePicker          -> "File Picker"
        | Route.Protocol            -> "Templates"
        | Route.ProtocolSearch      -> "Template Search"
        | Route.JsonExport          -> "Json Export"
        | Route.DataAnnotator       -> "Data Annotator"
        | Route.Info                -> "Info"
        | Route.ActivityLog         -> "Activity Log"
        | Route.Settings            -> "Settings"
        | Route.NotFound            -> "NotFound"

    member this.isExpert =
        match this with
        | Route.JsonExport -> true
        | _ -> false

    member this.isActive(currentRoute: Route) =
        let activeArr=
            match this with
            | Route.Protocol    -> [|Route.Protocol; Route.ProtocolSearch|]
            | any               -> [|any|]
        Array.contains currentRoute activeArr

    static member toIcon (p: Route)=
        let createElem (icons: ReactElement list) name =
            Bulma.icon [
                prop.title name
                prop.children icons
            ] 

        match p with
        | Route.TermSearch          ->
            createElem [Html.i [prop.className "fa-solid fa-magnifying-glass-plus" ]] p.toStringRdbl
        | Route.BuildingBlock       ->
            createElem [ Html.i [prop.className "fa-solid fa-circle-plus" ]; Html.i [prop.className "fa-solid fa-table-columns" ]]  p.toStringRdbl
        | Route.Protocol            ->
            createElem [ Html.i [prop.className "fa-solid fa-circle-plus" ];Html.i [prop.className "fa-solid fa-table" ]] p.toStringRdbl
        | Route.ProtocolSearch      ->
            createElem [ Html.i [prop.className "fa-solid fa-table" ]; Html.i [prop.className "fa-solid fa-magnifying-glass" ]] p.toStringRdbl
        | Route.JsonExport          ->
            createElem [ Html.i [prop.className "fa-solid fa-file-export" ]] p.toStringRdbl
        | Route.FilePicker          ->
            createElem [ Html.i [prop.className "fa-solid fa-file-signature" ]] p.toStringRdbl
        | Route.ActivityLog         ->
            createElem [ Html.i [prop.className "fa-solid fa-timeline" ]] p.toStringRdbl
        | Route.Info                ->
            createElem [ Html.i [prop.className "fa-solid fa-question" ]] p.toStringRdbl
        | Route.DataAnnotator       ->
            createElem [ Html.i [prop.className "fa-solid fa-object-group" ]] p.toStringRdbl
        | _                         -> Html.i [prop.className "fa-question"]

///explained here: https://elmish.github.io/browser/routing.html
//let curry f x y = f (x,y)

module Routing =

    open Elmish
    open Elmish.Navigation

    /// The URL is turned into a Result.
    let route : Parser<Route -> Route,_> =
        oneOf [
            map Route.Home                  (s "" <?> intParam "is_swatehost")
            map Route.TermSearch            (s "TermSearch")
            map Route.BuildingBlock         (s "BuildingBlock")
            map Route.FilePicker            (s "FilePicker")
            map Route.Info                  (s "Info")
            map Route.Protocol              (s "ProtocolInsert")
            map Route.ProtocolSearch        (s "Protocol" </> s "Search")
            map Route.JsonExport            (s "Experts" </> s "JsonExport")
            map Route.ActivityLog           (s "ActivityLog")
            map Route.Settings              (s "Settings")
            map Route.NotFound              (s "NotFound")
            // Redirect
            map Route.BuildingBlock         (s "Core")
        ]


    let parsePath (location:Browser.Types.Location) : Route option = Elmish.UrlParser.parsePath route location