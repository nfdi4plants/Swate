module Routing

open Elmish.UrlParser
open Fable.FontAwesome
open Fulma.Extensions.Wikiki

type SwateEntry =
| Core
| Expert

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Route =
| Home
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
        | Route.Home                -> "/"
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
        | Route.Home                -> ""
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

    member this.toSwateEntry =
        match this with
        | Route.Validation | Route.TemplateMetadata | Route.JsonExport -> SwateEntry.Expert
        | _ -> SwateEntry.Core

    static member toIcon (p: Route)=
        let createElem icons name =
            Fable.React.Standard.span [ Fable.React.Props.HTMLAttr.Title name
            ] (
                icons
                |> List.map ( fun icon -> Fa.span [icon] [] )
            )

        match p with
        | Route.Home                -> createElem [Fa.Solid.Home                            ]   (p.toStringRdbl)
        | Route.TermSearch          -> createElem [Fa.Solid.SearchPlus                      ]   (p.toStringRdbl)
        | Route.BuildingBlock       -> createElem [Fa.Solid.PlusCircle; Fa.Solid.Columns    ]   (p.toStringRdbl)
        | Route.Protocol            -> createElem [Fa.Solid.PlusCircle; Fa.Solid.Table      ]   (p.toStringRdbl)
        | Route.ProtocolSearch      -> createElem [Fa.Solid.Table; Fa.Solid.Search          ]   (p.toStringRdbl)
        | Route.Dag                 -> createElem [Fa.Solid.ProjectDiagram                  ]   (p.toStringRdbl)
        | Route.Validation          -> createElem [Fa.Solid.ClipboardCheck                  ]   (p.toStringRdbl)
        | Route.JsonExport          -> createElem [Fa.Solid.FileExport                      ]   (p.toStringRdbl)
        | Route.TemplateMetadata    -> createElem [Fa.Solid.PlusCircle; Fa.Solid.Table;     ]   (p.toStringRdbl)     
        | Route.FilePicker          -> createElem [Fa.Solid.Upload                          ]   (p.toStringRdbl)
        | Route.ActivityLog         -> createElem [Fa.Solid.History                         ]   (p.toStringRdbl)
        | Route.Info                -> createElem [Fa.Solid.Question                        ]   (p.toStringRdbl)  
        | _                         -> Fa.i [Fa.Solid.QuestionCircle]   []

///explained here: https://elmish.github.io/browser/routing.html
//let curry f x y = f (x,y)

module Routing =

    open Elmish
    open Elmish.Navigation

    /// The URL is turned into a Result.
    let route : Parser<Route -> Route,_> =
        oneOf [
            map Route.Home                  (s "")
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
            /// Redirect
            map Route.Validation            (s "Experts")
            map Route.BuildingBlock         (s "Core")
        ]


