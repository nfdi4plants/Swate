module Components.QuickAccessButton

open Feliz
open Feliz.Bulma

type QuickAccessButton = {
    Description : string
    FaList      : ReactElement list
    Msg         : Browser.Types.MouseEvent -> unit
    IsActive    : bool
    ButtonProps : IReactProperty list
} with
    static member create(description, faList, msg, ?isActive: bool, ?buttonProps : IReactProperty list) = {
        Description = description
        FaList      = faList
        Msg         = msg
        IsActive    = Option.defaultValue true isActive
        ButtonProps = Option.defaultValue List.empty buttonProps
    }
    member this.toReactElement() =
        let isDisabled = not this.IsActive
        Bulma.navbarItem.div [
            prop.title this.Description
            prop.style [
                style.padding 0; style.minWidth(length.px 45)
                if isDisabled then
                    style.cursor.notAllowed
            ]
            prop.children [
                Bulma.button.a [
                    prop.tabIndex (if isDisabled then -1 else 0)
                    yield! this.ButtonProps
                    prop.disabled isDisabled
                    prop.className "myNavbarButton"
                    prop.onClick this.Msg
                    prop.children [
                        Html.div [
                            prop.children this.FaList
                        ]
                    ]
                ]
            ]
        ]