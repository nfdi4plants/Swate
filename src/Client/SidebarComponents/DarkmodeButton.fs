module SidebarComponents.DarkmodeButton

open Feliz
open Feliz.Bulma
open LocalStorage.Darkmode

[<ReactComponent>]
let Main() =
    let state = React.useContext(LocalStorage.Darkmode.themeContext)
    Bulma.navbarItem.a [
        prop.onClick (fun e ->
            e.preventDefault()
            let next = if state.Theme = Dark then Light else Dark
            DataTheme.SET next
            state.SetTheme {state with Theme = next}
        )
        prop.children [
            Bulma.icon [
                Bulma.icon.isMedium
                prop.children state.Theme.toIcon
            ]
        ]
    ]