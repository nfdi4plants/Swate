module Components.QuickAccessButton

open Feliz
open Feliz.Bulma

type QuickAccessButton = {
    Description : string
    FaList      : ReactElement list
    Msg         : Browser.Types.MouseEvent -> unit
    IsActive    : bool
} with
    static member create(description, faList, msg, ?isActive: bool) = {
        Description = description
        FaList      = faList
        Msg         = msg
        IsActive    = Option.defaultValue true isActive
    }
    member this.toReactElement() =
        let isDisabled = not this.IsActive
        Bulma.navbarItem.a [
            prop.className ["myNavbarButtonContainer"]
            prop.title this.Description
            prop.style [
                style.padding 0; style.minWidth(length.px 45)
                if not this.IsActive then
                    style.cursor.notAllowed
            ]
            prop.children [
                Bulma.button.a [
                    prop.disabled isDisabled
                    prop.className "myNavbarButton"
                    prop.onClick this.Msg
                    prop.children [
                        Html.div [
                            prop.className "myNavbarButton"
                            prop.children this.FaList
                        ]
                    ]
                ]
            ]
        ]

        //Bulma.navbarItem.a [
        //    prop.disabled (not this.IsActive)
        //    prop.className "myNavbarButtonContainer"
        //    prop.title this.Description
        //    prop.style [
        //        style.padding 0; style.minWidth(length.px 45)
        //        if not this.IsActive then
        //            style.cursor.notAllowed
        //            style.color.red
        //    ]
        //    prop.children [
        //        Html.div [
        //            prop.className "myNavbarButton"
        //            prop.onClick this.Msg
        //            prop.children this.FaList
        //        ]
        //    ]
        //]