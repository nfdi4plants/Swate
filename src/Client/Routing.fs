module Routing

open Elmish.UrlParser
open Fable.FontAwesome
open Fulma.Extensions.Wikiki

/// The different pages of the application. If you add a new page, then add an entry here.
[<RequireQualifiedAccess>]
type Route =
| Home
| AddBuildingBlock
| TermSearch
| Validation
| FilePicker
| Info
| ProtocolInsert
| ProtocolSearch
| XLSXConverter
| ActivityLog
| Settings
| SettingsXml
| SettingsDataStewards
| SettingsProtocol
| NotFound

    static member toRouteUrl (route:Route) =
        match route with
        | Route.Home                -> "/"
        | Route.AddBuildingBlock    -> "/#AddBuildingBlock"
        | Route.TermSearch          -> "/#TermSearch"
        | Route.Validation          -> "/#Validation"   
        | Route.FilePicker          -> "/#FilePicker"
        | Route.ProtocolInsert      -> "/#ProtocolInsert"
        | Route.ProtocolSearch      -> "/#ProtocolSearch"
        | Route.XLSXConverter       -> "/#XLSXConverter"      
        | Route.Info                -> "/#Info"
        | Route.ActivityLog         -> "/#ActivityLog"
        | Route.Settings            -> "/#Settings"
        | Route.SettingsXml         -> "/#Settings/Xml"
        | Route.SettingsDataStewards-> "/#Settings/DataStewards"
        | Route.SettingsProtocol    -> "/#Settings/Protocol"
        | Route.NotFound            -> "/#NotFound"

    member this.toStringRdbl =
        match this with
        | Route.Home                -> ""
        | Route.AddBuildingBlock    -> "Manage Building Blocks"
        | Route.TermSearch          -> "Manage Terms"
        | Route.Validation          -> "Checklist Editor"
        | Route.FilePicker          -> "File Picker"
        | Route.ProtocolInsert      -> "Protocol Insert"
        | Route.ProtocolSearch      -> "Protocol Search"
        | Route.XLSXConverter       -> "XLSX Converter"
        | Route.Info                -> "Info"
        | Route.ActivityLog         -> "Activity Log"
        | Route.Settings            -> "Settings"
        | Route.SettingsXml         -> "Xml Settings"
        | Route.SettingsDataStewards-> "Settings for Data Stewards"
        | Route.SettingsProtocol    -> "Protocol Settings"
        | Route.NotFound            -> "NotFound"

    static member toIcon (p: Route)=
        let createElem icons name =
            Fable.React.Standard.span [
                Fable.React.Props.Class (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
                Tooltip.dataTooltip (name)
            ] (
                icons
                |> List.map ( fun icon -> Fa.span [icon] [] )
            )

        match p with
        | Route.Home                -> createElem [Fa.Solid.Home                            ]   (p.toStringRdbl)
        | Route.TermSearch          -> createElem [Fa.Solid.SearchPlus                      ]   (p.toStringRdbl)
        | Route.Validation          -> createElem [Fa.Solid.ClipboardCheck                  ]   (p.toStringRdbl)
        | Route.AddBuildingBlock    -> createElem [Fa.Solid.PlusCircle; Fa.Solid.Columns    ]   (p.toStringRdbl)
        | Route.ProtocolInsert      -> createElem [Fa.Solid.Table; Fa.Solid.PlusCircle      ]   (p.toStringRdbl)
        | Route.ProtocolSearch      -> createElem [Fa.Solid.Table; Fa.Solid.Search          ]   (p.toStringRdbl)
        | Route.XLSXConverter       -> createElem [Fa.Brand.Microsoft; Fa.Solid.Cogs        ]   (p.toStringRdbl)
        | Route.FilePicker          -> createElem [Fa.Solid.Upload                          ]   (p.toStringRdbl)
        | Route.ActivityLog         -> createElem [Fa.Solid.History                         ]   (p.toStringRdbl)
        | Route.Info                -> createElem [Fa.Solid.Question                        ]   (p.toStringRdbl)  
        | _                         -> Fa.i [Fa.Solid.QuestionCircle]   []

///explained here: https://elmish.github.io/browser/routing.html
//let curry f x y = f (x,y)

module Routing =

    open Elmish.UrlParser
    open Elmish.Navigation

    /// The URL is turned into a Result.
    let route : Parser<Route -> Route,_> =
        oneOf [
            map Route.Home                  (s "")
            map Route.TermSearch            (s "TermSearch")
            map Route.AddBuildingBlock      (s "AddBuildingBlock")
            map Route.Validation            (s "Validation")
            map Route.FilePicker            (s "FilePicker")
            map Route.Info                  (s "Info")
            map Route.ProtocolInsert        (s "ProtocolInsert")
            map Route.ProtocolSearch        (s "ProtocolSearch")
            map Route.XLSXConverter         (s "XLSXConverter")
            map Route.ActivityLog           (s "ActivityLog")
            map Route.Settings              (s "Settings")
            map Route.SettingsXml           (s "Settings" </> s "Xml")
            map Route.SettingsProtocol      (s "Settings" </> s "Protocol")
            map Route.SettingsDataStewards  (s "Settings" </> s "DataStewards")
            map Route.NotFound              (s "NotFound")
        ]

    //this would be the way to got if we would use push based routing, but i decided to use hash based routing. Ill leave this here for now as a note.
    //let urlParser location = parsePath pageParser location


