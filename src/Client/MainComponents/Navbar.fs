module MainComponents.Navbar

open Feliz
open Feliz.Bulma


open LocalHistory
open Messages
open Components.QuickAccessButton

[<RequireQualifiedAccess>]
type private Modal = 
| BuildingBlock
| Template

let private quickAccessButtonListStart (state: LocalHistory.Model) (setModal: Modal option -> unit) dispatch =
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
                "Save as xlsx",
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
                buttonProps = [Bulma.color.isDanger; Bulma.button.isInverted; Bulma.button.isOutlined]
            ).toReactElement()
        ]
    ]

let private WidgetNavbarList (model, dispatch, setModal) =
    Html.div [
        prop.style [
            style.display.flex; style.flexDirection.row
        ]
        prop.children [
            QuickAccessButton.create(
                "Add Building Block",
                [
                    Bulma.icon [ 
                        Html.i [prop.className "fa-solid fa-circle-plus" ]
                        Html.i [prop.className "fa-solid fa-table-columns" ]
                    ]
                ],
                (fun _ -> setModal (Some Modal.BuildingBlock))
            ).toReactElement()
            QuickAccessButton.create(
                "Add Template",
                [
                    Bulma.icon [ 
                        Html.i [prop.className "fa-solid fa-circle-plus" ]
                        Html.i [prop.className "fa-solid fa-table" ]
                    ]
                ],
                (fun _ -> setModal (Some Modal.Template))
            ).toReactElement()
        ]
    ]

let private modalDisplay (modal: Modal option, model, dispatch, setModal) = 
    let rmv = fun _ -> setModal (None)
    match modal with
    | None -> Html.none
    | Some Modal.BuildingBlock ->
        MainComponents.Widgets.BuildingBlock (model, dispatch, rmv)
    | Some Modal.Template ->
        MainComponents.Widgets.Templates (model, dispatch, rmv)

[<ReactComponent>]
let Main (model: Messages.Model) dispatch =
    let modal, setModal = React.useState(None)
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
            modalDisplay (modal, model, dispatch, setModal)
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
                    | Some Swatehost.ARCitect ->
                        Bulma.navbarStart.div [
                            prop.style [style.display.flex; style.alignItems.stretch; style.justifyContent.flexStart; style.custom("marginRight", "auto")]
                            prop.children [
                                Html.div [
                                    prop.style [
                                        style.display.flex; style.flexDirection.row
                                    ]
                                    prop.children [ 
                                        QuickAccessButton.create(
                                            "Return to ARCitect", 
                                            [
                                                Bulma.icon [Html.i [prop.className "fa-solid fa-circle-left";]]
                                            ],
                                            (fun _ -> ARCitect.ARCitect.send Model.ARCitect.TriggerSwateClose)
                                        ).toReactElement()
                                        QuickAccessButton.create(
                                            "Alpha State", 
                                            [
                                                Html.span "ALPHA STATE"
                                            ],
                                            (fun e -> ()),
                                            false
                                        ).toReactElement()
                                        quickAccessButtonListStart (model.History: LocalHistory.Model) setModal dispatch
                                        if model.SpreadsheetModel.TableViewIsActive() then WidgetNavbarList(model, dispatch, setModal)
                                    ]
                                ]
                            ]
                        ]
                    | Some _ ->
                        Bulma.navbarStart.div [
                            prop.style [style.display.flex; style.alignItems.stretch; style.justifyContent.flexStart; style.custom("marginRight", "auto")]
                            prop.children [
                                quickAccessButtonListStart model.History setModal dispatch
                                if model.SpreadsheetModel.TableViewIsActive() then WidgetNavbarList(model, dispatch, setModal)
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