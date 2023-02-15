module MainComponents.Navbar

open Feliz
open Feliz.Bulma

open Messages

open Components.QuickAccessButton

let quickAccessButtonList (model: Model) dispatch =
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
                    printfn "FIRE"
                    let newPosition = Spreadsheet.LocalStorage.CurrentHistoryPosition + 1
                    let newPosition_clamped = System.Math.Min(newPosition, Spreadsheet.LocalStorage.MaxHistory - 1)
                    if newPosition_clamped = Spreadsheet.LocalStorage.CurrentHistoryPosition then
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
                    printfn "FIRE"
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

let Main (model: Messages.Model) dispatch =
    Bulma.navbar [
        prop.className "myNavbarSticky"
        prop.id "swate-mainNavbar"
        prop.role "navigation"
        prop.ariaLabel "main navigation"
        prop.style [
            yield! ExcelColors.colorElementInArray_Feliz model.SiteStyleState.ColorMode; style.flexWrap.wrap
        ]
        prop.children [
            Bulma.navbarBrand.div [
                prop.ariaLabel "menu"
                prop.children [
                    quickAccessButtonList model dispatch
                ]
            ]
        ]
    ]