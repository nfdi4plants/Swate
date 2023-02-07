module CustomComponents.Navbar

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fulma.Extensions.Wikiki

open ExcelColors
open Model
open Messages

type private NavbarState = {
    BurgerActive: bool
    QuickAccessActive: bool
} with
    static member init = {
        BurgerActive = false
        QuickAccessActive = false
    }

type private ShortCutIcon = {
    Description : string
    FaList      : ReactElement list
    Msg         : Browser.Types.MouseEvent -> unit
    Category    : string
} with
    static member create description faList msg category = {
        Description = description
        FaList      = faList
        Msg         = msg
        Category    = category
    }

let private shortCutIconList model dispatch =
    [
        ShortCutIcon.create
            "Add Annotation Table"
            [
                Fa.span [Fa.Solid.Plus] []
                Fa.span [Fa.Solid.Table] []
            ]
            (fun e -> OfficeInterop.CreateAnnotationTable e.ctrlKey |> OfficeInteropMsg |> dispatch)
            "Table"
        ShortCutIcon.create
            "Autoformat Table"
            [
                Fa.i [Fa.Solid.SyncAlt] []
            ]
            (fun e -> OfficeInterop.AutoFitTable (not e.ctrlKey) |> OfficeInteropMsg |> dispatch)
            "Formatting"
        ShortCutIcon.create
            "Update Ontology Terms"
            [
                Fa.span [Fa.Solid.SpellCheck] []
                span [] [str model.ExcelState.FillHiddenColsStateStore.toReadableString]
                Fa.span [Fa.Solid.Pen] []
            ]
            (fun _ -> OfficeInterop.FillHiddenColsRequest |> OfficeInteropMsg |> dispatch)
            "Formatting"
        ShortCutIcon.create
            "Remove Building Block"
            [ 
                Fa.span [Fa.Solid.Minus; Fa.Props [Style [PaddingRight "0.15rem"]]] []
                Fa.span [Fa.Solid.Columns] []
            ]
            (fun _ -> OfficeInterop.RemoveAnnotationBlock |> OfficeInteropMsg |> dispatch)
            "BuildingBlock"
        ShortCutIcon.create
            "Get Building Block Information"
            [ 
                Fa.span [Fa.Solid.Question; Fa.Props [Style [PaddingRight "0.15rem"]]] []
                span [] [str model.BuildingBlockDetailsState.CurrentRequestState.toStringMsg]
                Fa.span [Fa.Solid.Columns] []
            ]
            (fun _ -> OfficeInterop.GetSelectedBuildingBlockTerms |> OfficeInteropMsg |> dispatch)
            "BuildingBlock"
    ]
    
let private navbarShortCutIconList model dispatch =
    [
        for icon in shortCutIconList model dispatch do
            yield
                Navbar.Item.a [Navbar.Item.CustomClass "myNavbarButtonContainer";Navbar.Item.Props [Title icon.Description ;Style [Padding "0px"; MinWidth "45px"]]] [
                    div [
                        Class "myNavbarButton"
                        OnClick icon.Msg
                       
                    ] icon.FaList
                ]
    ]

let private quickAccessDropdownElement model dispatch (state: NavbarState) (setState: NavbarState -> unit) (isSndNavbar:bool) =
    Navbar.Item.div [
        Navbar.Item.Props [
            OnClick (fun _ -> setState {state with QuickAccessActive = not state.QuickAccessActive})
            Style [
                Padding "0px";
                if isSndNavbar then
                    MarginLeft "auto"
            ]
            Title (if state.QuickAccessActive then "Close quick access" else "Open quick access")
        ]
    ] [
        div [Style [
            Width "100%"
            Height "100%"
            Position PositionOptions.Relative
            if model.SiteStyleState.IsDarkMode then
                BorderColor model.SiteStyleState.ColorMode.ControlForeground
            else
                BorderColor model.SiteStyleState.ColorMode.Fade
        ]] [
            Button.a [
                Button.Props [Style [ BackgroundColor "transparent"; Height "100%"; if state.QuickAccessActive then Color NFDIColors.Yellow.Base]]
                Button.Color Color.IsWhite
                Button.IsInverted
            ] [
                div [Style [
                    Display DisplayOptions.InlineFlex
                    Position PositionOptions.Relative
                    JustifyContent "center"
                ]] [
                    Fa.i [
                        Fa.Props [Style [
                            Position PositionOptions.Absolute
                            Display DisplayOptions.Block
                            Transition "opacity 0.25s, transform 0.25s"
                            if state.QuickAccessActive then Opacity "1" else Opacity "0"
                            if state.QuickAccessActive then Transform "rotate(-180deg)" else Transform "rotate(0deg)"
                        ]]
                        Fa.Solid.Times
                    ] []
                    Fa.i [
                        Fa.Props [Style [
                            Position PositionOptions.Absolute
                            Display DisplayOptions.Block
                            Transition "opacity 0.25s, transform 0.25s"
                            if state.QuickAccessActive then Opacity "0" else Opacity "1"
                        ]]
                        Fa.Solid.EllipsisH
                    ] []
                    // Invis placeholder to create correct space (Height, width, margin, padding, etc.)
                    Fa.i [
                        Fa.Props [Style [
                            Display DisplayOptions.Block
                            Opacity "0" 
                        ]]
                        Fa.Solid.EllipsisH
                    ] []
                ]
            ]
        ]
    ]

let private quickAccessListElement model dispatch =
    div [Style [Display DisplayOptions.Flex; FlexDirection "row"]] [
        yield! navbarShortCutIconList model dispatch
    ]


open Fable.Core.JsInterop

open Feliz

[<ReactComponent>]
let NavbarComponent (model : Model) (dispatch : Msg -> unit) (sidebarsize: Model.WindowSize) =
    let state, setState = React.useState(NavbarState.init)
    Navbar.navbar [
        Navbar.CustomClass "myNavbarSticky"
        Navbar.Props [
            Id "swate-mainNavbar"; Props.Role "navigation"; AriaLabel "main navigation" ;
            Style [yield! ExcelColors.colorElementInArray model.SiteStyleState.ColorMode; FlexWrap "wrap"]
        ]
    ] [
        Html.div [
            prop.style [style.flexBasis (length.percent 100)]
            prop.children [
                Navbar.Brand.div [Props [Style [Width "100%"; ]]] [
                    // Logo
                    Navbar.Item.div [
                        Navbar.Item.Props [
                            OnClick (fun _ -> Routing.Route.BuildingBlock |> Some |> UpdatePageState |> dispatch)
                            Style [Width "100px"; Cursor "pointer"; Padding "0 0.4rem"]
                        ]
                    ] [
                        let path = if model.PersistentStorageState.PageEntry = Routing.Expert then "_e" else ""
                        Image.image [] [ img [
                            Style [MaxHeight "100%"]
                            Props.Src @$"assets\Swate_logo_for_excel{path}.svg"
                        ] ]
                    ]

                    // Quick access buttons
                    match sidebarsize with
                    | WindowSize.Mini ->
                        quickAccessDropdownElement model dispatch state setState false
                    | _ ->
                        quickAccessListElement model dispatch

                    Navbar.burger [
                        Navbar.Burger.IsActive state.BurgerActive
                        Navbar.Burger.OnClick (fun _ -> setState {state with BurgerActive = not state.BurgerActive})
                        Navbar.Burger.Modifiers [Modifier.TextColor IsWhite]
                        Navbar.Burger.Props [
                            Role "button"
                            AriaLabel "menu"
                            Props.AriaExpanded false
                            Style [Display DisplayOptions.Block]
                    ]] [
                        span [AriaHidden true] [ ]
                        span [AriaHidden true] [ ]
                        span [AriaHidden true] [ ]
                    ]
                ]
                Navbar.menu [ Navbar.Menu.Props [
                    Style [yield! ExcelColors.colorControlInArray model.SiteStyleState.ColorMode; if state.BurgerActive then Display DisplayOptions.Block];
                    Id "navbarMenu";
                    Class (if state.BurgerActive then "navbar-menu is-active" else "navbar-menu");
                ]] [
                    Navbar.Dropdown.div [ Navbar.Dropdown.Props [Style [if state.BurgerActive then Display DisplayOptions.Block]] ] [
                        Navbar.Item.a [Navbar.Item.Props [ Href Shared.URLs.NFDITwitterUrl ; Target "_Blank"; Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                            str "News "
                            Fa.i [Fa.Brand.Twitter; Fa.Size Fa.FaLarge; Fa.Props [Style [Color "#1DA1F2"]]] []
                        ]
                        Navbar.Item.a [Navbar.Item.Props [ Href Shared.URLs.SwateWiki ; Target "_Blank"; Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                            str "How to use"
                        ]
                        Navbar.Item.a [Navbar.Item.Props [Href Shared.URLs.Helpdesk.Url; Target "_Blank"; Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                            str "Contact us!"
                        ]
                        Navbar.Item.a [Navbar.Item.Props [
                            OnClick (fun _ ->
                                setState {state with BurgerActive = not state.BurgerActive}
                                UpdatePageState (Some Routing.Route.Settings) |> dispatch
                            )
                            Style [ Color model.SiteStyleState.ColorMode.Text]
                        ]] [
                            str "Settings"
                        ]
                        Navbar.Item.a [Navbar.Item.Props [
                            Style [ Color model.SiteStyleState.ColorMode.Text];
                            OnClick (fun e ->
                                setState {state with BurgerActive = not state.BurgerActive}
                                UpdatePageState (Some Routing.Route.ActivityLog) |> dispatch
                            )
                        ]] [
                            str "Activity Log"
                        ]
                    ]
                ]
            ]
        ]
        if state.QuickAccessActive && sidebarsize = WindowSize.Mini then
            Navbar.Brand.div [Props [Style [FlexGrow "1"; Display DisplayOptions.Flex]]] [
                yield! navbarShortCutIconList model dispatch
            ]
    ]