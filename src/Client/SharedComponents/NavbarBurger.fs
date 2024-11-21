namespace Components

open Feliz
open Feliz.DaisyUI

type NavbarBurger =

    static member private BurgerSwap(isOpen, toggle) =
        Html.label [
            prop.onClick toggle
            prop.className [
                "swap swap-rotate text-white"
                if isOpen then "swap-active";
            ]
            prop.children [
                // hamburger icon
                Svg.svg [
                    svg.className "swap-off fill-current size-6"
                    svg.xmlns "http://www.w3.org/2000/svg"
                    svg.viewBox (0, 0, 512, 512)
                    svg.children [
                        Svg.path [svg.d "M64,384H448V341.33H64Zm0-106.67H448V234.67H64ZM64,128v42.67H448V128Z"]
                    ]
                ]

                // close icon
                Svg.svg [
                    svg.className "swap-on fill-current size-6"
                    svg.xmlns "http://www.w3.org/2000/svg"
                    svg.viewBox (0, 0, 512, 512)
                    svg.children [
                        Svg.polygon [svg.points "400 145.49 366.51 112 256 222.51 145.49 112 112 145.49 222.51 256 112 366.51 145.49 400 256 289.49 366.51 400 400 366.51 289.49 256 400 145.49"]
                    ]
                ]
            ]
        ]

    static member private DropdownItem(text: string, icon: ReactElement, props: IReactProperty) =
        Html.li [
            Html.a [
                props
                prop.className "flex flex-row justify-between items-center"
                prop.children [
                    Html.span [
                        prop.text text
                    ]
                    icon
                ]
            ]
        ]

    [<ReactComponent>]
    static member private Dropdown(isOpen, setIsOpen, model: Model.Model, dispatch) =

        let navigateTo = fun (mainPage: Routing.MainPage) -> {model with Model.PageState.MainPage = mainPage} |> Messages.UpdateModel |> dispatch
        Components.BaseDropdown.Main(
            isOpen,
            setIsOpen,
            NavbarBurger.BurgerSwap(isOpen, (fun _ -> setIsOpen (not isOpen))),
            [
                NavbarBurger.DropdownItem(
                    "Settings",
                    Html.i [prop.className "fa-solid fa-cog"],
                    prop.onClick (fun _ -> navigateTo Routing.MainPage.Settings)
                )
                NavbarBurger.DropdownItem("About", Html.i [prop.className "fa-solid fa-question-circle"], prop.onClick (fun _ -> navigateTo Routing.MainPage.About))
                NavbarBurger.DropdownItem("Privacy Policy", Html.i [prop.className "fa-solid fa-fingerprint"], prop.onClick (fun _ -> navigateTo Routing.MainPage.PrivacyPolicy))
                NavbarBurger.DropdownItem("Docs", Html.i [prop.className "fa-solid fa-book"], prop.href Shared.URLs.SWATE_WIKI)
                NavbarBurger.DropdownItem("Contact", Html.i [prop.className "fa-solid fa-comments"], prop.href Shared.URLs.CONTACT)
            ],
            style = Style.init "dropdown-end flex text-base-content"
        )
    [<ReactComponent>]
    static member Main(model, dispatch) =
        let isOpen, setIsOpen = React.useState(false)
        QuickAccessButton.QuickAccessButton(
            "More",
            NavbarBurger.Dropdown(isOpen, setIsOpen, model, dispatch),
            (fun _ -> setIsOpen (not isOpen))
        )