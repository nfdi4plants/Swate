module MainComponents.Navbar

open Feliz
open Feliz.Bulma


open LocalHistory
open Messages
open Components.QuickAccessButton

[<RequireQualifiedAccess>]
type private Widget = 
| BuildingBlock
| Template

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
                buttonProps = [Bulma.color.isDanger; Bulma.button.isInverted; Bulma.button.isOutlined]
            ).toReactElement()
        ]
    ]

let private WidgetNavbarList (model, dispatch, addWidget: Widget -> unit) =
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
                (fun _ -> addWidget Widget.BuildingBlock)
            ).toReactElement()
            QuickAccessButton.create(
                "Add Template",
                [
                    Bulma.icon [ 
                        Html.i [prop.className "fa-solid fa-circle-plus" ]
                        Html.i [prop.className "fa-solid fa-table" ]
                    ]
                ],
                (fun _ -> addWidget Widget.Template)
            ).toReactElement()
        ]
    ]

let private modalDisplay (widgets: Widget list, rmvWidget: Widget -> unit, model, dispatch) = 
    let rmv (widget: Widget) = fun _ -> rmvWidget widget
    let displayWidget (widget: Widget) (widgetComponent) =
        if widgets |> List.contains widget then
            widgetComponent (model, dispatch, rmv widget)
        else
            Html.none
    match widgets.Length with
    | 0 -> 
        Html.none
    | _ ->
        Html.div [
            displayWidget Widget.BuildingBlock MainComponents.Widgets.BuildingBlock
            displayWidget Widget.Template MainComponents.Widgets.Templates
        ]

[<ReactComponent>]
let Main (model: Messages.Model) dispatch =
    let widgets, setWidgets = React.useState([])
    let rmvWidget (widget: Widget) = widgets |> List.except [widget] |> setWidgets
    let addWidget (widget: Widget) = 
        if widgets |> List.contains widget then () else 
            widget::widgets |> setWidgets
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
            modalDisplay (widgets, rmvWidget, model, dispatch)
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
                                        quickAccessButtonListStart (model.History: LocalHistory.Model) dispatch
                                        if model.SpreadsheetModel.TableViewIsActive() then WidgetNavbarList(model, dispatch, addWidget)
                                    ]
                                ]
                            ]
                        ]
                    | Some _ ->
                        Bulma.navbarStart.div [
                            prop.style [style.display.flex; style.alignItems.stretch; style.justifyContent.flexStart; style.custom("marginRight", "auto")]
                            prop.children [
                                quickAccessButtonListStart model.History dispatch
                                if model.SpreadsheetModel.TableViewIsActive() then WidgetNavbarList(model, dispatch, addWidget)
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