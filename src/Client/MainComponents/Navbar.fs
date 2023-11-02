module MainComponents.Navbar

open Feliz
open Feliz.Bulma

open Messages

open Components.QuickAccessButton

let quickAccessButtonListStart (model: Model) dispatch =
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
                    let newPosition = Spreadsheet.LocalStorage.CurrentHistoryPosition + 1
                    let newPosition_clamped = System.Math.Min(newPosition, Spreadsheet.LocalStorage.AvailableHistoryItems)
                    let noChange = newPosition_clamped = Spreadsheet.LocalStorage.CurrentHistoryPosition
                    let overMax = newPosition_clamped = Spreadsheet.LocalStorage.MaxHistory
                    let notEnoughHistory = Spreadsheet.LocalStorage.AvailableHistoryItems - (Spreadsheet.LocalStorage.CurrentHistoryPosition + 1) <= 0
                    if noChange || overMax || notEnoughHistory then
                        ()
                    else
                        Spreadsheet.UpdateHistoryPosition newPosition_clamped |> Msg.SpreadsheetMsg |> dispatch
                ),
                isActive = (Spreadsheet.LocalStorage.AvailableHistoryItems - (Spreadsheet.LocalStorage.CurrentHistoryPosition + 1) > 0)
            ).toReactElement()
            QuickAccessButton.create(
                "Forward",
                [
                    Bulma.icon [Html.i [prop.className "fa-solid fa-rotate-right"]]
                ],
                (fun _ ->
                    let newPosition = Spreadsheet.LocalStorage.CurrentHistoryPosition - 1
                    let newPosition_clamped = System.Math.Max(newPosition, 0)
                    if newPosition_clamped = Spreadsheet.LocalStorage.CurrentHistoryPosition then
                        ()
                    else
                        Spreadsheet.UpdateHistoryPosition newPosition_clamped |> Msg.SpreadsheetMsg |> dispatch
                ),
                isActive = (Spreadsheet.LocalStorage.CurrentHistoryPosition > 0)
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
                (fun _ -> SpreadsheetMsg Spreadsheet.ExportXlsx |> dispatch)
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
                            quickAccessButtonListStart model dispatch
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