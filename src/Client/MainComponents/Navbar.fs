module MainComponents.Navbar

open Feliz
open Feliz.Bulma


open LocalHistory
open Messages
open Components.QuickAccessButton
open MainComponents
open Model

let private quickAccessButtonListStart (state: LocalHistory.Model) dispatch =
    Html.div [
        prop.style [
            style.display.flex; style.flexDirection.row
        ]
        prop.children [
            
            QuickAccessButton.create(
                "Back",
                [
                    Bulma.icon [Html.i [prop.className "fa-solid fa-rotate-left"]]
                ],
                (fun _ ->
                    let newPosition = state.HistoryCurrentPosition + 1 
                    //let newPosition_clamped = System.Math.Min(newPosition, state.HistoryExistingItemCount)
                    //let noChange = newPosition_clamped = Spreadsheet.LocalStorage.CurrentHistoryPosition
                    //let overMax = newPosition_clamped = Spreadsheet.LocalStorage.MaxHistory
                    //let notEnoughHistory = Spreadsheet.LocalStorage.AvailableHistoryItems - (Spreadsheet.LocalStorage.CurrentHistoryPosition + 1) <= 0
                    if state.NextPositionIsValid(newPosition) then
                        Spreadsheet.UpdateHistoryPosition newPosition |> Msg.SpreadsheetMsg |> dispatch
                ),
                isActive = (state.NextPositionIsValid(state.HistoryCurrentPosition + 1))
            ).toReactElement()
            QuickAccessButton.create(
                "Forward",
                [
                    Bulma.icon [Html.i [prop.className "fa-solid fa-rotate-right"]]
                ],
                (fun _ ->
                    let newPosition = state.HistoryCurrentPosition - 1
                    if state.NextPositionIsValid(newPosition) then
                        Spreadsheet.UpdateHistoryPosition newPosition |> Msg.SpreadsheetMsg |> dispatch
                ),
                isActive = (state.NextPositionIsValid(state.HistoryCurrentPosition - 1))
            ).toReactElement()
        ]
    ]

let private quickAccessButtonListEnd (model: Model) dispatch =
    Html.div [
        prop.style [
            style.display.flex; style.flexDirection.row
        ]
        prop.children [
            QuickAccessButton.create(
                "Save",
                [
                    Bulma.icon [Html.i [prop.className "fa-solid fa-floppy-disk";]]
                ],
                (fun _ -> Spreadsheet.ExportXlsx model.SpreadsheetModel.ArcFile.Value |> SpreadsheetMsg |> dispatch)
            ).toReactElement()
            QuickAccessButton.create(
                "Reset",
                [
                    Bulma.icon [Html.i [prop.className "fa-sharp fa-solid fa-trash";]]
                ],
                (fun _ -> Modals.Controller.renderModal("ResetTableWarning", Modals.ResetTable.Main dispatch)),
                buttonProps = [Bulma.color.isDanger]
            ).toReactElement()
        ]
    ]

let private WidgetNavbarList (model, dispatch, addWidget: Widget -> unit) =
    let addBuildingBlock =
        QuickAccessButton.create(
            "Add Building Block",
            [
                Bulma.icon [ 
                    Html.i [prop.className "fa-solid fa-circle-plus" ]
                    Html.i [prop.className "fa-solid fa-table-columns" ]
                ]
            ],
            (fun _ -> addWidget Widget._BuildingBlock)
        ).toReactElement()
    let addTemplate =
        QuickAccessButton.create(
            "Add Template",
            [
                Bulma.icon [ 
                    Html.i [prop.className "fa-solid fa-circle-plus" ]
                    Html.i [prop.className "fa-solid fa-table" ]
                ]
            ],
            (fun _ -> addWidget Widget._Template)
        ).toReactElement()
    let filePicker =
        QuickAccessButton.create(
            "File Picker",
            [
                Bulma.icon [ 
                    Html.i [prop.className "fa-solid fa-file-signature" ]
                ]
            ],
            (fun _ -> addWidget Widget._FilePicker)
        ).toReactElement()
    let dataAnnotator =
        QuickAccessButton.create(
            "Data Annotator",
            [
                Bulma.icon [ 
                    Html.i [prop.className "fa-solid fa-object-group" ]
                ]
            ],
            (fun _ -> addWidget Widget._DataAnnotator)
        ).toReactElement()
    Html.div [
        prop.style [
            style.display.flex; style.flexDirection.row
        ]
        prop.children [
            match model.SpreadsheetModel.ActiveView with
            | Spreadsheet.ActivePattern.IsTable ->
                addBuildingBlock
                addTemplate
                filePicker
                dataAnnotator
            | Spreadsheet.ActivePattern.IsDataMap ->
                dataAnnotator
            | Spreadsheet.ActivePattern.IsMetadata ->
                Html.none
        ]
    ]



[<ReactComponent>]
let Main(model: Model, dispatch, widgets, setWidgets) =
    let addWidget (widget: Widget) = 
        let add (widget) widgets = widget::widgets |> List.rev |> setWidgets
        if widgets |> List.contains widget then 
            List.filter (fun w -> w <> widget) widgets
            |> fun filteredWidgets -> add widget filteredWidgets
        else 
            add widget widgets
    Bulma.navbar [
        prop.className "myNavbarSticky"
        prop.id "swate-mainNavbar"
        prop.role "navigation"
        prop.ariaLabel "main navigation"
        prop.style [
            style.flexWrap.wrap; style.alignItems.stretch; style.display.flex;
            style.minHeight(length.rem 3.25)
        ]
        prop.children [
            Bulma.navbarBrand.div [

            ]
            Html.div [
                prop.style [
                    style.display.flex; style.flexGrow 1; style.flexShrink 0;
                    style.alignItems.stretch; 
                ]
                prop.ariaLabel "menu"
                prop.children [
                    match model.PersistentStorageState.Host with
                    | Some (Swatehost.ARCitect) ->
                        Bulma.navbarStart.div [
                            prop.style [style.display.flex; style.alignItems.stretch; style.justifyContent.flexStart; style.custom("marginRight", "auto")]
                            prop.children [
                                quickAccessButtonListStart model.History dispatch
                                if model.SpreadsheetModel.TableViewIsActive() then WidgetNavbarList(model, dispatch, addWidget)
                            ]
                        ]
                    | Some _ ->
                        Bulma.navbarStart.div [
                            prop.style [style.display.flex; style.alignItems.stretch; style.justifyContent.flexStart; style.custom("marginRight", "auto")]
                            prop.children [
                                quickAccessButtonListStart model.History dispatch
                                WidgetNavbarList(model, dispatch, addWidget)
                            ]
                        ]
                        Bulma.navbarEnd.div [
                            prop.style [style.display.flex; style.alignItems.stretch; style.justifyContent.flexEnd; style.custom("marginLeft", "auto")]
                            prop.children [
                                quickAccessButtonListEnd model dispatch
                            ]
                        ]
                    | _ -> Html.none
                ]
            ]
        ]
    ]