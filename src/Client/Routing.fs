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
| XLSXConverter
| JSONExporter
| TemplateMetadata
| ActivityLog
| Settings
| SettingsXml
| SettingsDataStewards
| NotFound

    static member toRouteUrl (route:Route) =
        match route with
        | Route.Home                -> "/"
        | Route.BuildingBlock       -> "/#BuildingBlock"
        | Route.TermSearch          -> "/#TermSearch"
        | Route.Validation          -> "/#Validation"   
        | Route.FilePicker          -> "/#FilePicker"
        | Route.Protocol            -> "/#ProtocolInsert"
        | Route.ProtocolSearch      -> "/#Protocol/Search"
        | Route.XLSXConverter       -> "/#XLSXConverter"
        | Route.JSONExporter        -> "/#JSONExporter"
        | Route.TemplateMetadata    -> "/#TemplateMetadata"
        | Route.Info                -> "/#Info"
        | Route.ActivityLog         -> "/#ActivityLog"
        | Route.Settings            -> "/#Settings"
        | Route.SettingsXml         -> "/#Settings/Xml"
        | Route.SettingsDataStewards-> "/#Settings/DataStewards"
        | Route.NotFound            -> "/#NotFound"

    member this.toStringRdbl =
        match this with
        | Route.Home                -> ""
        | Route.BuildingBlock       -> "Building Blocks"
        | Route.TermSearch          -> "Terms"
        | Route.Validation          -> "Checklist Editor"
        | Route.FilePicker          -> "File Picker"
        | Route.Protocol            -> "Templates"
        | Route.ProtocolSearch      -> "Template Search"
        | Route.XLSXConverter       -> "XLSX Converter"
        | Route.JSONExporter        -> "JSON Exporter"
        | Route.TemplateMetadata    -> "Template Metadata"
        | Route.Info                -> "Info"
        | Route.ActivityLog         -> "Activity Log"
        | Route.Settings            -> "Settings"
        | Route.SettingsXml         -> "Xml Settings"
        | Route.SettingsDataStewards-> "Settings for Data Stewards"
        | Route.NotFound            -> "NotFound"

    member this.toSwateEntry =
        match this with
        | Route.Validation | Route.SettingsDataStewards | Route.XLSXConverter | Route.TemplateMetadata -> SwateEntry.Expert
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
        | Route.Validation          -> createElem [Fa.Solid.ClipboardCheck                  ]   (p.toStringRdbl)
        | Route.BuildingBlock       -> createElem [Fa.Solid.PlusCircle; Fa.Solid.Columns    ]   (p.toStringRdbl)
        | Route.Protocol            -> createElem [Fa.Solid.Table; Fa.Solid.PlusCircle      ]   (p.toStringRdbl)
        | Route.ProtocolSearch      -> createElem [Fa.Solid.Table; Fa.Solid.Search          ]   (p.toStringRdbl)
        | Route.XLSXConverter       -> createElem [Fa.Brand.Microsoft; Fa.Solid.Cogs        ]   (p.toStringRdbl)
        | Route.JSONExporter        -> createElem [Fa.Solid.FileExport                      ]   (p.toStringRdbl)
        | Route.TemplateMetadata    -> createElem [Fa.Solid.Table; Fa.Solid.Edit            ]   (p.toStringRdbl)     
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
            map Route.Validation            (s "Validation")
            map Route.FilePicker            (s "FilePicker")
            map Route.Info                  (s "Info")
            map Route.Protocol              (s "ProtocolInsert")
            map Route.ProtocolSearch        (s "Protocol" </> s "Search")
            map Route.XLSXConverter         (s "XLSXConverter")
            map Route.JSONExporter          (s "JSONExporter")
            map Route.TemplateMetadata      (s "TemplateMetadata")
            map Route.ActivityLog           (s "ActivityLog")
            map Route.Settings              (s "Settings")
            map Route.SettingsXml           (s "Settings" </> s "Xml")
            map Route.SettingsDataStewards  (s "Settings" </> s "DataStewards")
            map Route.NotFound              (s "NotFound")
            /// Redirect
            map Route.Validation            (s "Experts")
            map Route.BuildingBlock         (s "Core")
        ]


