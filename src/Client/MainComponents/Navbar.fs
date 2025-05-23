module MainComponents.Navbar

open Feliz
open Feliz.DaisyUI


open LocalHistory
open Messages
open Components
open MainComponents
open Model
open Swate.Components.Shared
open Swate.Components
open ARCtrl

let private FileName (model: Model) =
    let txt =
        match model.SpreadsheetModel.ArcFile with
        | Some(ArcFiles.Assay a) -> a.Identifier
        | Some(ArcFiles.Study(s, _)) -> s.Identifier
        | Some(ArcFiles.Investigation i) -> i.Identifier
        | Some(ArcFiles.Template t) -> t.FileName
        | _ -> ""

    match model.SpreadsheetModel.ArcFile with
    | Some _ ->
        Html.div [
            prop.className "text-white text-lg font-bold inline-flex items-center max-w-[125px] px-2 truncate"
            prop.text txt
            prop.title txt
        ]
    | None -> Html.none

let private QuickAccessButtonListStart (state: LocalHistory.Model) dispatch =
    Html.div [
        prop.style [ style.display.flex; style.flexDirection.row ]
        prop.children [
            QuickAccessButton.QuickAccessButton(
                "Back",
                React.fragment [ Html.i [ prop.className "fa-solid fa-rotate-left" ] ],
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
                React.fragment [ Html.i [ prop.className "fa-solid fa-rotate-right" ] ],
                (fun _ ->
                    let newPosition = state.HistoryCurrentPosition - 1

                    if state.NextPositionIsValid(newPosition) then
                        History.UpdateHistoryPosition newPosition |> Msg.HistoryMsg |> dispatch),
                isDisabled = (state.NextPositionIsValid(state.HistoryCurrentPosition - 1) |> not)
            )
        ]
    ]

let private QuickAccessButtonListEnd (model: Model) dispatch =
    Html.div [
        prop.style [ style.display.flex; style.flexDirection.row ]
        prop.children [
            QuickAccessButton.QuickAccessButton(
                "Save",
                React.fragment [ Html.i [ prop.className "fa-solid fa-floppy-disk" ] ],
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
            match model.PersistentStorageState.Host with
            | Some Swatehost.Browser ->
                QuickAccessButton.QuickAccessButton(
                    "Reset",
                    React.fragment [ Html.i [ prop.className "fa-solid fa-trash-can" ] ],
                    (fun _ ->
                        Model.ModalState.TableModals.ResetTable
                        |> Model.ModalState.ModalTypes.TableModal
                        |> Some
                        |> Messages.UpdateModal
                        |> dispatch),
                    classes = "hover:!text-error"
                )

                NavbarBurger.Main(model, dispatch)
            | _ -> Html.none
        ]
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
            (fun _ -> addWidget Widget._DataAnnotator)
        )

    Html.div [
        prop.className "flex flex-row"
        prop.children [
            match model.SpreadsheetModel.ActiveView with
            | Spreadsheet.ActivePattern.IsTable ->
                addBuildingBlock
                addTemplate
                filePicker
                dataAnnotator
            | Spreadsheet.ActivePattern.IsDataMap -> dataAnnotator
            | Spreadsheet.ActivePattern.IsMetadata -> Html.none
        ]
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
        Html.div [ prop.className "grow-0"; prop.children [ FileName model ] ]
        Daisy.navbarCenter [
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
            prop.className "ml-auto"
            prop.children [ QuickAccessButtonListEnd model dispatch ]
        ]
    ]