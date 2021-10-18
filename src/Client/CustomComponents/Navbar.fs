module CustomComponents.Navbar

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fulma.Extensions.Wikiki

open ExcelColors
open Model
open Messages

type ShortCutIcon = {
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

let shortCutIconList model dispatch =
    [
        ShortCutIcon.create
            "Add Annotation Table"
            [
                Fa.span [Fa.Solid.Plus][]
                Fa.span [Fa.Solid.Table][]
            ]
            (fun e -> OfficeInterop.CreateAnnotationTable (model.SiteStyleState.IsDarkMode, e.ctrlKey) |> OfficeInteropMsg |> dispatch)
            "Table"
        ShortCutIcon.create
            "Autoformat Table"
            [
                Fa.i [Fa.Solid.SyncAlt][]
            ]
            (fun e -> OfficeInterop.AutoFitTable (not e.ctrlKey) |> OfficeInteropMsg |> dispatch)
            "Formatting"
        ShortCutIcon.create
            "Update Ontology Terms"
            [
                Fa.span [Fa.Solid.SpellCheck][]
                span [][str model.ExcelState.FillHiddenColsStateStore.toReadableString]
                Fa.span [Fa.Solid.Pen][]
            ]
            (fun _ -> OfficeInterop.FillHiddenColsRequest |> OfficeInteropMsg |> dispatch)
            "Formatting"
        ShortCutIcon.create
            "Remove Building Block"
            [ 
                Fa.span [Fa.Solid.Minus; Fa.Props [Style [PaddingRight "0.15rem"]]][]
                Fa.span [Fa.Solid.Columns][]
            ]
            (fun _ -> OfficeInterop.RemoveAnnotationBlock |> OfficeInteropMsg |> dispatch)
            "BuildingBlock"
        ShortCutIcon.create
            "Get Building Block Information"
            [ 
                Fa.span [Fa.Solid.Question; Fa.Props [Style [PaddingRight "0.15rem"]]][]
                span [][str model.BuildingBlockDetailsState.CurrentRequestState.toStringMsg]
                Fa.span [Fa.Solid.Columns][]
            ]
            (fun _ -> OfficeInterop.GetSelectedBuildingBlockTerms |> OfficeInteropMsg |> dispatch)
            "BuildingBlock"
    ]
    
let navbarShortCutIconList model dispatch =
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

let quickAccessDropdownElement model dispatch (isSndNavbar:bool) =
    Navbar.Item.a [
        Navbar.Item.Props [
            OnClick (fun e -> ToggleQuickAcessIconsShown |> StyleChange |> dispatch)
            Style [
                Padding "0px";
                if isSndNavbar then
                    MarginLeft "auto"
            ]
            Title (if model.SiteStyleState.QuickAcessIconsShown then "Close quick access" else "Open quick access")
        ]
        Navbar.Item.CustomClass "hideOver575px"
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
                Button.Props [Style [ BackgroundColor "transparent"; Height "100%"; if model.SiteStyleState.QuickAcessIconsShown then Color NFDIColors.Yellow.Base]]
                Button.Color Color.IsWhite
                Button.IsInverted
            ][
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
                            if model.SiteStyleState.QuickAcessIconsShown then Opacity "1" else Opacity "0"
                            if model.SiteStyleState.QuickAcessIconsShown then Transform "rotate(-180deg)" else Transform "rotate(0deg)"
                        ]]
                        Fa.Solid.Times
                    ][]
                    Fa.i [
                        Fa.Props [Style [
                            Position PositionOptions.Absolute
                            Display DisplayOptions.Block
                            Transition "opacity 0.25s, transform 0.25s"
                            if model.SiteStyleState.QuickAcessIconsShown then Opacity "0" else Opacity "1"
                        ]]
                        Fa.Solid.EllipsisH
                    ][]
                    // Invis placeholder to create correct space (Height, width, margin, padding, etc.)
                    Fa.i [
                        Fa.Props [Style [
                            Display DisplayOptions.Block
                            Opacity "0" 
                        ]]
                        Fa.Solid.EllipsisH
                    ][]
                ]
            ]
        ]
    ]

let quickAccessListElement model dispatch =
    div [Style [Display DisplayOptions.Flex; FlexDirection "row"]; Class "hideUnder575px"][
        yield! navbarShortCutIconList model dispatch
    ]


open Fable.Core.JsInterop

let quickAccessScalableNavbar (model:Messages.Model) dispatch =
    div [Class "hideOver575px"][
        Navbar.navbar [
            Navbar.CustomClass "wrapFlexBox"
            Navbar.Props [
                Style [
                    ZIndex 29
                    if model.SiteStyleState.QuickAcessIconsShown |> not then
                        Display DisplayOptions.None
                    else Display DisplayOptions.Flex
                    yield! ExcelColors.colorElementInArray model.SiteStyleState.ColorMode
                    BorderTop $".5px solid {model.SiteStyleState.ColorMode.Fade}"
                ]
            ]
        ][
            Navbar.Brand.div [CustomClass "wrapFlexBox"; Props [Style [Flex "1"]]] [
                yield! navbarShortCutIconList model dispatch
                //quickAccessDropdownElement model dispatch true
            ]
        ]
    ]

let navbarComponent (model : Model) (dispatch : Msg -> unit) =
    Navbar.navbar [
        Navbar.CustomClass "myNavbarSticky"
        Navbar.Props [
            Id "swate-mainNavbar"; Props.Role "navigation"; AriaLabel "main navigation" ;
            Style [yield! ExcelColors.colorElementInArray model.SiteStyleState.ColorMode]
        ]
    ] [
        Navbar.Brand.div [Props [Style [Width "100%"]]] [
            Navbar.Item.div [
                Navbar.Item.Props [
                    OnClick (fun e -> Routing.Route.BuildingBlock |> Some |> UpdatePageState |> dispatch)
                    Style [Width "100px"; Cursor "pointer"; Padding "0 0.4rem"]
                ]
            ] [
                let path = if model.PersistentStorageState.PageEntry = Routing.Expert then "_e" else ""
                Image.image [] [img [Props.Src @$"assets\Swate_logo_for_excel{path}.svg"]]
            ]

            quickAccessListElement model dispatch

            quickAccessDropdownElement model dispatch false
            Navbar.burger [
                Navbar.Burger.IsActive model.SiteStyleState.BurgerVisible
                Navbar.Burger.OnClick (fun e -> ToggleBurger |> StyleChange |> dispatch)
                Navbar.Burger.Modifiers [Modifier.TextColor IsWhite]
                Navbar.Burger.Props [
                    Role "button"
                    AriaLabel "menu"
                    Props.AriaExpanded false
            ]] [
                span [AriaHidden true] [ ]
                span [AriaHidden true] [ ]
                span [AriaHidden true] [ ]
            ]
        ]
        div [Class "hideOver575px "][
            Navbar.Brand.div [CustomClass "wrapFlexBox"; Props [Style [
                BorderTop $".5px solid {model.SiteStyleState.ColorMode.Fade}"
                if model.SiteStyleState.QuickAcessIconsShown |> not then
                    Display DisplayOptions.None
                else Display DisplayOptions.Flex
            ]]] [
                yield! navbarShortCutIconList model dispatch
            ]
        ]
        Navbar.menu [Navbar.Menu.Props [Id "navbarMenu"; Class (if model.SiteStyleState.BurgerVisible then "navbar-menu is-active" else "navbar-menu"); ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
            Navbar.Dropdown.div [ ] [
                Navbar.Item.a [Navbar.Item.Props [ Href Shared.URLs.DocsFeatureUrl ; Target "_Blank"; Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                    str "How to use"
                ]
                Navbar.Item.a [Navbar.Item.Props [Href @"https://github.com/nfdi4plants/Swate/issues/new/choose"; Target "_Blank"; Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                    str "Contact"
                ]
                Navbar.Item.a [Navbar.Item.Props [
                    OnClick (fun e ->
                        ToggleBurger |> StyleChange |> dispatch
                        UpdatePageState (Some Routing.Route.Settings) |> dispatch
                    )
                    Style [ Color model.SiteStyleState.ColorMode.Text]
                ]] [
                    str "Settings"
                ]
                Navbar.Item.a [Navbar.Item.Props [
                    Style [ Color model.SiteStyleState.ColorMode.Text];
                    OnClick (fun e ->
                        ToggleBurger |> StyleChange |> dispatch
                        UpdatePageState (Some Routing.Route.ActivityLog) |> dispatch
                    )
                ]] [
                    str "Activity Log"
                ]
            ]
        ]
    ]