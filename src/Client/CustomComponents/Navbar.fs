module CustomComponents.Navbar

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fulma.Extensions.Wikiki

open ExcelColors
open Model
open Messages

let placerholderInvis =
    let padding = "0.5rem"
    Button.a [
        Button.Props [Style [Opacity "0"; PointerEvents "None"; Cursor "none"; PaddingLeft padding; PaddingRight padding]]
    ] [
        Fa.i [Fa.Solid.SyncAlt][]
    ]

let navbarShortCutIconList model dispatch =
    let padding = "0.5rem"
    [
        Button.a [
            Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            Button.Props [ Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft padding; PaddingRight padding]; Tooltip.dataTooltip ("Add Annotation Table") ]
            Button.OnClick (fun _ ->
                (fun (allNames) ->
                    CreateAnnotationTable (allNames,model.SiteStyleState.IsDarkMode))
                    |> PipeCreateAnnotationTableInfo
                    |> ExcelInterop
                    |> dispatch
            )
            Button.Color Color.IsWhite
            Button.IsInverted
        ] [
            Fa.span [Fa.Solid.Plus][]
            Fa.span [Fa.Solid.Table][]
        ]

        Button.a [
            Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft padding; PaddingRight padding]; Tooltip.dataTooltip ("Autoformat Table")]
            Button.OnClick (fun e ->
                Msg.Batch [
                    PipeActiveAnnotationTable AutoFitTable |> ExcelInterop
                    PipeActiveAnnotationTable UpdateProtocolGroupHeader |> ExcelInterop
                ]  |> dispatch
            )
            Button.Color Color.IsWhite
            Button.IsInverted
        ] [
            Fa.i [Fa.Solid.SyncAlt][]
        ]
        Button.a [
            Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft padding; PaddingRight padding]; Tooltip.dataTooltip ("Update Reference Columns")]
            Button.OnClick (fun _ ->
                PipeActiveAnnotationTable FillHiddenColsRequest |> ExcelInterop |> dispatch
            )
            Button.Color Color.IsWhite
            Button.IsInverted
        ] [
            Fa.span [Fa.Solid.EyeSlash][]
            span [][str model.ExcelState.FillHiddenColsStateStore.toReadableString]
            Fa.span [Fa.Solid.Pen][]
        ]
        Button.a [
            Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft padding; PaddingRight padding]; Tooltip.dataTooltip ("Remove Building Block")]
            Button.OnClick (fun _ ->
                PipeActiveAnnotationTable RemoveAnnotationBlock |> ExcelInterop |> dispatch
            )
            Button.Color Color.IsWhite
            Button.IsInverted
        ] [ 
            Fa.span [Fa.Solid.Minus; Fa.Props [Style [PaddingRight "0.15rem"]]][]
            Fa.span [Fa.Solid.Columns][]
        ]

        Button.a [
            Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft padding; PaddingRight padding]; Tooltip.dataTooltip ("Get Building Block Information")]
            Button.OnClick (fun _ ->
                PipeActiveAnnotationTable GetSelectedBuildingBlockSearchTerms |> ExcelInterop |> dispatch
            )
            Button.Color Color.IsWhite
            Button.IsInverted
        ] [ 
            Fa.span [Fa.Solid.Question; Fa.Props [Style [PaddingRight "0.15rem"]]][]
            span [][str model.BuildingBlockDetailsState.CurrentRequestState.toStringMsg]
            Fa.span [Fa.Solid.Columns][]
        ]
    ]

let quickAccessDropdownElement model dispatch =
    let prepIconLists =
        let split length (xs: seq<'T>) =
            let rec loop xs =
                [
                    yield Seq.truncate length xs |> Seq.toList
                    match Seq.length xs <= length with
                    | false -> yield! loop (Seq.skip length xs)
                    | true -> ()
                ]
            loop xs
        let iconList = navbarShortCutIconList model dispatch
        split 3 iconList
    Navbar.Item.div [
        Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]
        Navbar.Item.CustomClass "hideOver575px"
    ] [
        div [Style [
            Position PositionOptions.Relative
        ]] [
            Button.a [
                Button.OnClick (fun e -> ToggleQuickAcessIconsShown |> StyleChange |> dispatch)
                Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft "0"; PaddingRight "0"]]
                Button.Color Color.IsWhite
                Button.IsInverted
            ][
                div [Style [
                    Position PositionOptions.Relative
                    //Display DisplayOptions.Flex
                    //JustifyContent "center"
                    //AlignItems AlignItemsOptions.Center
                ]] [
                    Fa.i [
                        Fa.Props [Style [
                            Position PositionOptions.Absolute
                            Top "0"
                            Left "0"
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
                            Top "0"
                            Left "0"
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
            div [
                Class "arrow_box"
                Style [
                    Width "150px"
                    Left "-69px"
                    Display (if model.SiteStyleState.QuickAcessIconsShown then DisplayOptions.Block else DisplayOptions.None)
                    Position PositionOptions.Absolute
                    ZIndex "20"
                ]
            ][
                for subList in prepIconLists do
                    yield
                        div [Style [
                            Color model.SiteStyleState.ColorMode.Text
                            Display DisplayOptions.Flex
                            JustifyContent "space-between"
                        ]] [
                            subList |> List.tryItem 0 |> fun x -> if x.IsSome then x.Value else placerholderInvis
                            subList |> List.tryItem 1 |> fun x -> if x.IsSome then x.Value else placerholderInvis
                            subList |> List.tryItem 2 |> fun x -> if x.IsSome then x.Value else placerholderInvis
                        ]
            ]
        ]
    ]

let quickAccessListElement model dispatch =
    Navbar.Item.div [
        Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]
        Navbar.Item.CustomClass "hideUnder575px"
    ] [
        yield! navbarShortCutIconList model dispatch 
    ]


let navbarComponent (model : Model) (dispatch : Msg -> unit) =
    Navbar.navbar [Navbar.Props [Props.Role "navigation"; AriaLabel "main navigation" ; ExcelColors.colorElement model.SiteStyleState.ColorMode]] [
        Navbar.Brand.a [] [
            Navbar.Item.a [Navbar.Item.Props [Props.Href "https://csb.bio.uni-kl.de/"; Target "_Blank"]] [
                img [Props.Src "../assets/Swate_logo_for_excel.svg"]
            ]

            quickAccessListElement model dispatch

            quickAccessDropdownElement model dispatch

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
        Navbar.menu [Navbar.Menu.Props [Id "navbarMenu"; Class (if model.SiteStyleState.BurgerVisible then "navbar-menu is-active" else "navbar-menu") ; ExcelColors.colorControl model.SiteStyleState.ColorMode]] [
            Navbar.Dropdown.div [ ] [
                Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                    str "How to use (WIP)"
                ]
                Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                    str "Contact (WIP)"
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