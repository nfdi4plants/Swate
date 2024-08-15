module SidebarComponents.Navbar

open Model
open Messages

open Feliz
open Feliz.Bulma

type private NavbarState = {
    BurgerActive: bool
    QuickAccessActive: bool
} with
    static member init = {
        BurgerActive = false
        QuickAccessActive = false
    }

open Components.QuickAccessButton


let private shortCutIconList model dispatch =
    [
        QuickAccessButton.create(
            "Create Annotation Table",
            [
                Html.i [prop.className "fa-solid fa-plus"]
                Html.i [prop.className "fa-solid fa-table"]
            ],
            (fun e ->
                e.preventDefault()
                let ctrl = e.metaKey || e.ctrlKey
                SpreadsheetInterface.CreateAnnotationTable ctrl |> InterfaceMsg |> dispatch
            )
        )
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel ->
            QuickAccessButton.create(
                "Autoformat Table",
                [
                    Html.i [prop.className "fa-solid fa-rotate"]
                ],
                (fun e ->
                    e.preventDefault()
                    let ctrl = not (e.metaKey || e.ctrlKey)
                    OfficeInterop.AutoFitTable ctrl |> OfficeInteropMsg |> dispatch
                )
            )
        | _ ->
            ()
        QuickAccessButton.create(
            "Update Ontology Terms",
            [
                Html.i [prop.className "fa-solid fa-spell-check"]
                Html.span model.ExcelState.FillHiddenColsStateStore.toReadableString
                Html.i [prop.className "fa-solid fa-pen"]
            ],
            (fun _ -> SpreadsheetInterface.UpdateTermColumns |> InterfaceMsg |> dispatch)
        )
        QuickAccessButton.create(
            "Remove Building Block",
            [
                Html.i [prop.className "fa-solid fa-minus pr-1"]
                Html.i [prop.className "fa-solid fa-table-columns"]
            ],
            (fun _ -> SpreadsheetInterface.RemoveBuildingBlock |> InterfaceMsg |> dispatch)
        )
        QuickAccessButton.create(
            "Get Building Block Information",
            [
                Html.i [prop.className "fa-solid fa-question pr-1"]
                Html.span model.BuildingBlockDetailsState.CurrentRequestState.toStringMsg
                Html.i [prop.className "fa-solid fa-table-columns"]
            ],
            (fun _ -> SpreadsheetInterface.EditBuildingBlock |> InterfaceMsg |> dispatch)
        )
        QuickAccessButton.create(
            "Validate Annotation Table",
            [
                Html.i [prop.className "fa-solid fa-check"]
                Html.i [prop.className "fa-solid fa-table"]
            ],
            (fun _ -> SpreadsheetInterface.ValidateAnnotationTable |> InterfaceMsg |> dispatch
            )
        )
    ]

let private navbarShortCutIconList model dispatch =
    [
        for icon in shortCutIconList model dispatch do
            yield
                icon.toReactElement()
    ]

let private quickAccessDropdownElement model dispatch (state: NavbarState) (setState: NavbarState -> unit) (isSndNavbar:bool) =
    Bulma.navbarItem.div [
        prop.onClick (fun _ -> setState {state with QuickAccessActive = not state.QuickAccessActive})
        prop.style [ style.padding 0; if isSndNavbar then style.custom("marginLeft", "auto")]
        prop.title (if state.QuickAccessActive then "Close quick access" else "Open quick access")
        prop.children [
            Html.div [
                prop.style [style.width(length.perc 100); style.height (length.perc 100); style.position.relative]
                prop.children [
                    Bulma.button.a [
                        prop.style [style.backgroundColor "transparent"; style.height(length.perc 100); if state.QuickAccessActive then style.color NFDIColors.Yellow.Base]
                        Bulma.color.isWhite
                        Bulma.button.isInverted
                        prop.children [
                            Html.div [
                                prop.style [ style.display.inlineFlex; style.position.relative; style.justifyContent.center]
                                prop.children [
                                    Html.i [
                                        prop.style [
                                            style.position.absolute
                                            style.display.block
                                            style.custom("transition","opacity 0.25s, transform 0.25s")
                                            style.opacity (if state.QuickAccessActive then 1 else 0)
                                            style.transform (if state.QuickAccessActive then [transform.rotate -180] else [transform.rotate 0])
                                        ]
                                        prop.className "fa-solid fa-times"
                                    ]
                                    Html.i [
                                        prop.style [
                                            style.position.absolute
                                            style.display.block
                                            style.custom("transition","opacity 0.25s, transform 0.25s")
                                            style.opacity (if state.QuickAccessActive then 0 else 1)
                                        ]
                                        prop.className "fa-solid fa-ellipsis"
                                    ]
                                    // Invis placeholder to create correct space (Height, width, margin, padding, etc.)
                                    Html.i [
                                        prop.style [
                                            style.display.block
                                            style.opacity 0
                                        ]
                                        prop.className "fa-solid fa-ellipsis"
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let private quickAccessListElement model dispatch =
    Html.div [
        prop.style [style.display.flex; style.flexDirection.row]
        prop.children (navbarShortCutIconList model dispatch)
    ]


open Feliz

[<ReactComponent>]
let NavbarComponent (model : Model) (dispatch : Msg -> unit) (sidebarsize: Model.WindowSize) =
    let state, setState = React.useState(NavbarState.init)
    Bulma.navbar [
        prop.className "myNavbarSticky"
        prop.id "swate-mainNavbar"; prop.role "navigation"; prop.ariaLabel "main navigation" ;
        prop.style [style.flexWrap.wrap]
        prop.children [
            Html.div [
                prop.style [style.flexBasis (length.percent 100)]
                prop.children [
                    Bulma.navbarBrand.div [
                        prop.style [style.width(length.perc 100)]
                        prop.children [
                            // Logo
                            Bulma.navbarItem.div [
                                prop.id "logo"
                                prop.onClick (fun _ -> Routing.Route.BuildingBlock |> Some |> UpdatePageState |> dispatch)
                                prop.style [style.width 100; style.cursor.pointer; style.padding (0,length.rem 0.4)]
                                let path = if model.PageState.IsExpert then "_e" else ""
                                Bulma.image [ Html.img [
                                    prop.style [style.maxHeight(length.perc 100); style.width 100]
                                    prop.src @$"assets\Swate_logo_for_excel{path}.svg"
                                ] ]
                                |> prop.children
                            ]

                            // Quick access buttons
                            match sidebarsize, model.PersistentStorageState.Host with
                            | WindowSize.Mini, Some Swatehost.Excel ->
                                quickAccessDropdownElement model dispatch state setState false
                            | _, Some Swatehost.Excel ->
                                quickAccessListElement model dispatch
                            | _,_ -> Html.none

                            Bulma.navbarBurger [
                                if state.BurgerActive then Bulma.navbarBurger.isActive
                                prop.onClick (fun _ -> setState {state with BurgerActive = not state.BurgerActive})
                                Bulma.color.hasTextWhite
                                prop.role "button"
                                prop.ariaLabel "menu"
                                prop.ariaExpanded false
                                prop.style [style.display.block]
                                prop.children [
                                    Html.span [prop.ariaHidden true]
                                    Html.span [prop.ariaHidden true]
                                    Html.span [prop.ariaHidden true]
                                    Html.span [prop.ariaHidden true]
                                ]
                            ]
                        ]
                    ]
                    Bulma.navbarMenu [
                        prop.style [if state.BurgerActive then style.display.block]
                        prop.id "navbarMenu"
                        prop.className (if state.BurgerActive then "navbar-menu is-active" else "navbar-menu")
                        Bulma.navbarDropdown.div [
                            prop.style [if state.BurgerActive then style.display.block]
                            prop.children [
                                Bulma.navbarItem.a [
                                    prop.href Shared.URLs.NFDITwitterUrl ;
                                    prop.target "_Blank";
                                    prop.children [
                                        Html.span "News "
                                        Html.i [prop.className "fa-brands fa-twitter"; prop.style [style.color "#1DA1F2"; style.marginLeft 2]]
                                    ]
                                ]
                                Bulma.navbarItem.a [
                                    prop.onClick (fun _ ->
                                        setState {state with BurgerActive = not state.BurgerActive}
                                        UpdatePageState (Some Routing.Route.Info) |> dispatch
                                    )
                                    prop.text Routing.Route.Info.toStringRdbl
                                ]
                                Bulma.navbarItem.a [
                                    prop.onClick (fun _ ->
                                        setState {state with BurgerActive = not state.BurgerActive}
                                        UpdatePageState (Some Routing.Route.PrivacyPolicy) |> dispatch
                                    )
                                    prop.text Routing.Route.PrivacyPolicy.toStringRdbl
                                ]
                                Bulma.navbarItem.a [
                                    prop.href Shared.URLs.SwateWiki ;
                                    prop.target "_Blank";
                                    prop.text "How to use"
                                ]
                                Bulma.navbarItem.a [
                                    prop.href Shared.URLs.Helpdesk.Url;
                                    prop.target "_Blank";
                                    prop.text "Contact us!"
                                ]
                                Bulma.navbarItem.a [
                                    prop.onClick (fun _ ->
                                        setState {state with BurgerActive = not state.BurgerActive}
                                        UpdatePageState (Some Routing.Route.Settings) |> dispatch
                                    )
                                    prop.text "Settings"
                                ]
                                Bulma.navbarItem.a [
                                    prop.onClick (fun e ->
                                        setState {state with BurgerActive = not state.BurgerActive}
                                        UpdatePageState (Some Routing.Route.ActivityLog) |> dispatch
                                    )
                                    prop.text "Activity Log"
                                ]
                            ]
                        ]
                        |> prop.children
                    ]
                ]
            ]
            if state.QuickAccessActive && sidebarsize = WindowSize.Mini then
                Bulma.navbarBrand.div [
                    prop.style [style.flexGrow 1; style.display.flex]
                    navbarShortCutIconList model dispatch |> prop.children
                ]
        ]
    ]