module Routing

open Elmish.UrlParser
open Feliz
open Feliz.Bulma

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Route =
| BuildingBlock
| TermSearch
| Validation
| FilePicker
| Info
| Protocol
| ProtocolSearch
| Dag /// Directed Acylclic Graph
| JsonExport
| TemplateMetadata
| ActivityLog
| Settings
| SettingsXml
| NotFound

    static member toRouteUrl (route:Route) =
        match route with
        | Route.BuildingBlock       -> "/#BuildingBlock"
        | Route.TermSearch          -> "/#TermSearch"
        | Route.FilePicker          -> "/#FilePicker"
        | Route.Protocol            -> "/#ProtocolInsert"
        | Route.ProtocolSearch      -> "/#Protocol/Search"
        | Route.Dag                 -> "/#Dag"
        | Route.Validation          -> "/#Experts/Validation"   
        | Route.JsonExport          -> "/#Experts/JsonExport"
        | Route.TemplateMetadata    -> "/#Experts/TemplateMetadata"
        | Route.Info                -> "/#Info"
        | Route.ActivityLog         -> "/#ActivityLog"
        | Route.Settings            -> "/#Settings"
        | Route.SettingsXml         -> "/#Settings/Xml"
        | Route.NotFound            -> "/#NotFound"

    member this.toStringRdbl =
        match this with
        | Route.BuildingBlock       -> "Building Blocks"
        | Route.TermSearch          -> "Terms"
        | Route.FilePicker          -> "File Picker"
        | Route.Protocol            -> "Templates"
        | Route.ProtocolSearch      -> "Template Search"
        | Route.Dag                 -> "Directed Acylclic Graph"
        | Route.Validation          -> "Checklist Editor"
        | Route.JsonExport          -> "Json Export"
        | Route.TemplateMetadata    -> "Template Metadata"
        | Route.Info                -> "Info"
        | Route.ActivityLog         -> "Activity Log"
        | Route.Settings            -> "Settings"
        | Route.SettingsXml         -> "Xml Settings"
        | Route.NotFound            -> "NotFound"

    member this.isExpert =
        match this with
        | Route.Validation | Route.TemplateMetadata | Route.JsonExport -> true
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
            createElem [ Html.i [prop.className "fa-solid fa-circle-plus" ]; Html.i [prop.className "fa-solid fa-table-column" ]]  p.toStringRdbl
        | Route.Protocol            ->
            createElem [ Html.i [prop.className "fa-solid fa-circle-plus" ];Html.i [prop.className "fa-solid fa-table" ]] p.toStringRdbl
        | Route.ProtocolSearch      ->
            createElem [ Html.i [prop.className "fa-solid fa-table" ]; Html.i [prop.className "fa-solid fa-magnifying-glass" ]] p.toStringRdbl
        | Route.Dag                 ->
            createElem [ Html.i [prop.className "fa-solid fa-diagram-project" ]] p.toStringRdbl
        | Route.Validation          ->
            createElem [ Html.i [prop.className "fa-solid fa-clipboard-check" ]] p.toStringRdbl
        | Route.JsonExport          ->
            createElem [ Html.i [prop.className "fa-solid fa-file-export" ]] p.toStringRdbl
        | Route.TemplateMetadata    ->
            createElem [ Html.i [prop.className "fa-solid fa-circle-plus" ];Html.i [prop.className "fa-solid fa-table" ]] p.toStringRdbl
        | Route.FilePicker          ->
            createElem [ Html.i [prop.className "fa-solid fa-upload" ]] p.toStringRdbl
        | Route.ActivityLog         ->
            createElem [ Html.i [prop.className "fa-solid fa-timeline" ]] p.toStringRdbl
        | Route.Info                ->
            createElem [ Html.i [prop.className "fa-solid fa-question" ]] p.toStringRdbl
        | _                         -> Html.i [prop.className "fa-question"]

///explained here: https://elmish.github.io/browser/routing.html
//let curry f x y = f (x,y)

module Routing =

    open Elmish
    open Elmish.Navigation

    /// The URL is turned into a Result.
    let route : Parser<Route -> Route,_> =
        oneOf [
            map Route.TermSearch            (s "")
            map Route.TermSearch            (s "TermSearch")
            map Route.BuildingBlock         (s "BuildingBlock")
            map Route.FilePicker            (s "FilePicker")
            map Route.Info                  (s "Info")
            map Route.Protocol              (s "ProtocolInsert")
            map Route.ProtocolSearch        (s "Protocol" </> s "Search")
            map Route.Dag                   (s "Dag")
            map Route.Validation            (s "Experts" </> s "Validation")
            map Route.JsonExport            (s "Experts" </> s "JsonExport")
            map Route.TemplateMetadata      (s "Experts" </> s "TemplateMetadata")
            map Route.ActivityLog           (s "ActivityLog")
            map Route.Settings              (s "Settings")
            map Route.SettingsXml           (s "Settings" </> s "Xml")
            map Route.NotFound              (s "NotFound")
            // Redirect
            map Route.Validation            (s "Experts")
            map Route.BuildingBlock         (s "Core")
        ]


