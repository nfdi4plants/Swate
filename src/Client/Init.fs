module Init

open Elmish.UrlParser
open Elmish
open LocalHistory
open Model
open Messages
open Update
open LocalStorage.AutosaveConfig

let initialModel = {
    PageState = PageState.init ()
    PersistentStorageState = PersistentStorageState.init ()
    DevState = DevState.init ()
    TermSearchState = TermSearch.Model.init ()
    ExcelState = OfficeInterop.Model.init ()
    FilePickerState = FilePicker.Model.init ()
    AddBuildingBlockState = BuildingBlock.Model.init ()
    ProtocolState = Protocol.Model.init ()
    CytoscapeModel = Cytoscape.Model.init ()
    DataAnnotatorModel = DataAnnotator.Model.init ()
    SpreadsheetModel = Spreadsheet.Model.init ()
    History = LocalHistory.Model.init ()
    ARCitectState = ARCitect.Model.init ()
    ModalState = ModalState.init ()
}


// defines the initial state and initial command (= side-effect) of the application
let init (pageOpt: Routing.Route option) : Model * Cmd<Msg> =
    let model, cmd = urlUpdate pageOpt initialModel

    let autosaveConfig = getAutosaveConfiguration ()

    let newModel =
        autosaveConfig
        |> Option.defaultValue model.PersistentStorageState.Autosave
        |> fun x -> {model with Model.PersistentStorageState.Autosave = x}

    newModel, cmd