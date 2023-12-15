module MainComponents.Navbar

open Feliz
open Feliz.Bulma


open LocalHistory
open Messages
open Components.QuickAccessButton


let quickAccessButtonListStart (state: LocalHistory.Model) dispatch =
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

let quickAccessButtonListEnd (model: Model) dispatch =
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
                (fun _ -> Modals.Controller.renderModal("ResetTableWarning", Modals.ResetTable.Main dispatch) ),
                buttonProps = [Bulma.color.isDanger; Bulma.button.isInverted; Bulma.button.isOutlined]
            ).toReactElement()
        ]
    ]


[<ReactComponent>]
let Main (model: Messages.Model) dispatch =
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
                    Bulma.navbarStart.div [
                        prop.style [style.display.flex; style.alignItems.stretch; style.justifyContent.flexStart; style.custom("marginRight", "auto")]
                        prop.children [
                            quickAccessButtonListStart model.History dispatch
                        ]
                    ]
                    Bulma.navbarEnd.div [
                        prop.style [style.display.flex; style.alignItems.stretch; style.justifyContent.flexEnd; style.custom("marginLeft", "auto")]
                        prop.children [
                            quickAccessButtonListEnd model dispatch
                        ]
                    ]
                ]
            ]
        ]
    ]