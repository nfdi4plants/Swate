module Init

open Elmish.UrlParser
open Elmish
open Model
open Messages
open Update
open Thoth.Elmish
open Spreadsheet.LocalStorage

let initializeModel (pageOpt: Routing.Route option) =
    let isDarkMode =
        let cookies = Browser.Dom.document.cookie
        let cookiesSplit = cookies.Split([|";"|], System.StringSplitOptions.RemoveEmptyEntries)
        cookiesSplit
        |> Array.tryFind (fun x -> x.StartsWith (Model.Cookies.IsDarkMode.toCookieString + "="))
        |> fun cookieOpt ->
            if cookieOpt.IsSome then
                cookieOpt.Value.Replace(Model.Cookies.IsDarkMode.toCookieString + "=","")
                |> fun cookie ->
                    match cookie with
                    | "false"| "False"  -> false
                    | "true" | "True"   -> true
                    | anyElse -> false
            else
                false
    {
        DebouncerState              = Debouncer                 .create ()
        PageState                   = PageState                 .init pageOpt
        PersistentStorageState      = PersistentStorageState    .init ()
        DevState                    = DevState                  .init ()
        SiteStyleState              = SiteStyleState            .init (darkMode=isDarkMode)
        TermSearchState             = TermSearch.Model          .init ()
        AdvancedSearchState         = AdvancedSearch.Model      .init ()
        ExcelState                  = OfficeInterop.Model       .init ()
        ApiState                    = ApiState                  .init ()
        FilePickerState             = FilePicker.Model          .init ()
        AddBuildingBlockState       = BuildingBlock.Model       .init ()
        ValidationState             = Validation.Model          .init ()
        ProtocolState               = Protocol.Model            .init ()
        BuildingBlockDetailsState   = BuildingBlockDetailsState .init ()
        SettingsXmlState            = SettingsXml.Model         .init ()
        JsonExporterModel           = JsonExporter.State.Model  .init ()
        TemplateMetadataModel       = TemplateMetadata.Model    .init ()
        DagModel                    = Dag.Model                 .init ()
        CytoscapeModel              = Cytoscape.Model           .init ()
        SpreadsheetModel            = Spreadsheet.Model         .tryInitFromLocalStorage()
    }

// defines the initial state and initial command (= side-effect) of the application
let init (pageOpt: Routing.Route option) : Model * Cmd<Msg> =
    let route = (parseHash Routing.Routing.route) Browser.Dom.document.location
    let initialModel = initializeModel (pageOpt)
    // The initial command from urlUpdate is not needed yet. As we use a reduced variant of subModels with no own Msg system.
    let model, _ = urlUpdate route initialModel
    let cmd = Cmd.ofMsg <| InterfaceMsg SpreadsheetInterface.Initialize 
    model, cmd