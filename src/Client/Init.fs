module Init

open Elmish.UrlParser
open Elmish
open LocalHistory
open Model
open Messages
open Update

let initialModel =
    {
        PersistentStorageState      = PersistentStorageState    .init()
        DevState                    = DevState                  .init()
        TermSearchState             = TermSearch.Model          .init()
        ExcelState                  = OfficeInterop.Model       .init()
        FilePickerState             = FilePicker.Model          .init()
        AddBuildingBlockState       = BuildingBlock.Model       .init()
        ProtocolState               = Protocol.Model            .init()
        CytoscapeModel              = Cytoscape.Model           .init()
        DataAnnotatorModel          = DataAnnotator.Model       .init()
        SpreadsheetModel            = Spreadsheet.Model         .init()
        History                     = LocalHistory.Model        .init()
        ModalState                  = ModalState                .init()
    }


// defines the initial state and initial command (= side-effect) of the application
let init (pageOpt: Routing.Route option) : Model * Cmd<Msg> =
    let model, cmd = urlUpdate pageOpt initialModel
    model, cmd