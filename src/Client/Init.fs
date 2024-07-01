module Init

open Elmish.UrlParser
open Elmish
open LocalHistory
open Model
open Messages
open Update

let initializeModel () =
    let dt = LocalStorage.Darkmode.DataTheme.GET()
    LocalStorage.Darkmode.DataTheme.SET dt
    {
        PageState                   = PageState                 .init ()
        PersistentStorageState      = PersistentStorageState    .init ()
        DevState                    = DevState                  .init ()
        TermSearchState             = TermSearch.Model          .init ()
        ExcelState                  = OfficeInterop.Model       .init ()
        FilePickerState             = FilePicker.Model          .init ()
        AddBuildingBlockState       = BuildingBlock.Model       .init ()
        ProtocolState               = Protocol.Model            .init ()
        BuildingBlockDetailsState   = BuildingBlockDetailsState .init ()
        CytoscapeModel              = Cytoscape.Model           .init ()
        SpreadsheetModel            = Spreadsheet.Model         .fromLocalStorage()
        History                     = LocalHistory.Model        .init().UpdateFromSessionStorage()
    }


// defines the initial state and initial command (= side-effect) of the application
let init (pageOpt: Routing.Route option) : Model * Cmd<Msg> =
    let initialModel, pageCmd = initializeModel () |> urlUpdate pageOpt
    let cmd = Cmd.ofMsg <| InterfaceMsg (SpreadsheetInterface.Initialize initialModel.PersistentStorageState.Host.Value)
    let batch = Cmd.batch [|pageCmd; cmd|]
    initialModel, batch