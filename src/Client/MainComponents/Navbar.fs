module MainComponents.Navbar

open Feliz
open Feliz.DaisyUI

open LocalHistory
open Messages
open Components
open MainComponents
open Model
open Swate.Components
open Swate.Components.Shared
open ARCtrl

open LocalStorage.AutosaveConfig

let private FileName (model: Model) =
    let txt =
        match model.SpreadsheetModel.ArcFile with
        | Some(ArcFiles.Assay a) -> a.Identifier
        | Some(ArcFiles.Study(s, _)) -> s.Identifier
        | Some(ArcFiles.Investigation i) -> i.Identifier
        | Some(ArcFiles.Template t) -> t.FileName
        | _ -> ""

    let letter =
        match model.SpreadsheetModel.ArcFile with
        | Some(ArcFiles.Assay a) -> "A"
        | Some(ArcFiles.Study(s, _)) -> "S"
        | Some(ArcFiles.Investigation i) -> "I"
        | Some(ArcFiles.Template t) -> "T"
        | _ -> ""

    match model.SpreadsheetModel.ArcFile with
    | Some _ ->
        Html.div [
            prop.className
                "swt:text-lg swt:font-bold swt:inline-flex swt:items-center swt:max-w-[125px] swt:px-2 swt:truncate"
            prop.children [
                Html.span [ prop.className "swt:hidden swt:lg:block"; prop.text txt ]
                Html.span [
                    prop.className "swt:block swt:lg:hidden"
                    prop.text letter // Truncate for smaller screens
                ]
            ]
            prop.title txt
        ]
    | None -> Html.none

let private QuickAccessButtonListStart (state: LocalHistory.Model) dispatch =
    React.fragment [
        QuickAccessButton.QuickAccessButton(
            "Backward",
            Icons.Backward(),
            (fun _ ->
                let newPosition = state.HistoryCurrentPosition + 1
                //let newPosition_clamped = System.Math.Min(newPosition, state.HistoryExistingItemCount)
                //let noChange = newPosition_clamped = Spreadsheet.LocalStorage.CurrentHistoryPosition
                //let overMax = newPosition_clamped = Spreadsheet.LocalStorage.MaxHistory
                //let notEnoughHistory = Spreadsheet.LocalStorage.AvailableHistoryItems - (Spreadsheet.LocalStorage.CurrentHistoryPosition + 1) <= 0
                if state.NextPositionIsValid(newPosition) then
                    History.UpdateHistoryPosition newPosition |> Msg.HistoryMsg |> dispatch),
            isDisabled = (state.NextPositionIsValid(state.HistoryCurrentPosition + 1) |> not)
        )
        QuickAccessButton.QuickAccessButton(
            "Forward",
            Icons.Forward(),
            (fun _ ->
                let newPosition = state.HistoryCurrentPosition - 1

                if state.NextPositionIsValid(newPosition) then
                    History.UpdateHistoryPosition newPosition |> Msg.HistoryMsg |> dispatch),
            isDisabled = (state.NextPositionIsValid(state.HistoryCurrentPosition - 1) |> not)
        )
    ]

let private QuickAccessButtonListEnd (model: Model) dispatch =
    let autoSaveConfig = getAutosaveConfiguration()
    React.fragment [
        match model.PersistentStorageState.Host with
        | Some Swatehost.Browser ->
            QuickAccessButton.QuickAccessButton(
                "Save",
                Icons.Save(),
                (fun _ ->
                    match model.PersistentStorageState.Host with
                    | Some(Swatehost.Browser) ->
                        Spreadsheet.ExportXlsx model.SpreadsheetModel.ArcFile.Value
                        |> SpreadsheetMsg
                        |> dispatch
                    | Some(Swatehost.ARCitect) ->
                        ARCitect.Save model.SpreadsheetModel.ArcFile.Value |> ARCitectMsg |> dispatch
                    | _ -> ()),
                isDisabled = model.SpreadsheetModel.ArcFile.IsNone
            )

            QuickAccessButton.QuickAccessButton(
                "Reset",
                Icons.Delete(),
                (fun _ ->
                    Model.ModalState.TableModals.ResetTable
                    |> Model.ModalState.ModalTypes.TableModal
                    |> Some
                    |> Messages.UpdateModal
                    |> dispatch),
                color = DaisyUIColors.Error
            )

            NavbarBurger.Main(model, dispatch)
        | Some Swatehost.ARCitect ->
            if autoSaveConfig.IsSome && not autoSaveConfig.Value then
                QuickAccessButton.QuickAccessButton(
                    "Save",
                    Icons.Save(),
                    (fun _ ->
                        match model.PersistentStorageState.Host with
                        | Some(Swatehost.Browser) ->
                            Spreadsheet.ExportXlsx model.SpreadsheetModel.ArcFile.Value
                            |> SpreadsheetMsg
                            |> dispatch
                        | Some(Swatehost.ARCitect) ->
                            ARCitect.Save model.SpreadsheetModel.ArcFile.Value |> ARCitectMsg |> dispatch
                        | _ -> ()),
                    isDisabled = model.SpreadsheetModel.ArcFile.IsNone
                )

            NavbarBurger.Main(model, dispatch)
        | _ -> Html.none
    ]

let private WidgetNavbarList (model, dispatch, addWidget: Widget -> unit) =
    let addBuildingBlock =
        QuickAccessButton.QuickAccessButton(
            "Add Building Block",
            Icons.BuildingBlock(),
            (fun _ -> addWidget Widget._BuildingBlock)
        )

    let addTemplate =
        QuickAccessButton.QuickAccessButton("Add Template", Icons.Templates(), (fun _ -> addWidget Widget._Template))

    let filePicker =
        QuickAccessButton.QuickAccessButton("File Picker", Icons.FilePicker(), (fun _ -> addWidget Widget._FilePicker))

    let dataAnnotator =
        QuickAccessButton.QuickAccessButton(
            "Data Annotator",
            Icons.DataAnnotator(),
            (fun _ -> addWidget Widget._DataAnnotator),
            classes = "swt:w-min"
        )

    React.fragment [
        match model.SpreadsheetModel.ActiveView with
        | Spreadsheet.ActivePattern.IsTable ->
            addBuildingBlock
            addTemplate
            filePicker
            dataAnnotator
        | Spreadsheet.ActivePattern.IsDataMap -> dataAnnotator
        | Spreadsheet.ActivePattern.IsMetadata -> Html.none
    ]

[<ReactComponent>]
let Main (model: Model, dispatch, widgets, setWidgets) =
    let addWidget (widget: Widget) =
        let add (widget) widgets =
            widget :: widgets |> List.rev |> setWidgets

        if widgets |> List.contains widget then
            List.filter (fun w -> w <> widget) widgets
            |> fun filteredWidgets -> add widget filteredWidgets
        else
            add widget widgets

    Components.BaseNavbar.Main [
        Html.div [
            prop.className "swt:grow-0"
        ]
        Html.div [
            prop.className "swt:navbar-center swt:overflow-x-auto swt:min-w-0 swt:shrink swt:grow"
            prop.children [
                QuickAccessButtonListStart model.History dispatch
                WidgetNavbarList(model, dispatch, addWidget)
            ]
        ]
        // match model.PersistentStorageState.Host with
        // | Some (Swatehost.ARCitect) ->
        //     Html.none
        // | Some _ ->
        //     Html.div [
        //         prop.className "ml-auto"
        //         prop.children [
        //             QuickAccessButtonListEnd model dispatch
        //         ]
        //     ]
        // | _ -> Html.none
        Html.div [
            prop.className "swt:ml-auto swt:grow-0 swt:shrink-0"
            prop.children [ QuickAccessButtonListEnd model dispatch ]
        ]
    ]