module CustomComponents.Navbar

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fulma.Extensions.Wikiki

open ExcelColors
open Model
open Messages


let navbarComponent (model : Model) (dispatch : Msg -> unit) =
    Navbar.navbar [Navbar.Props [Props.Role "navigation"; AriaLabel "main navigation" ; ExcelColors.colorElement model.SiteStyleState.ColorMode]] [
        Navbar.Brand.a [] [
            Navbar.Item.a [Navbar.Item.Props [Props.Href "https://csb.bio.uni-kl.de/"]] [
                img [Props.Src "../assets/Swate_logo_for_excel.svg"]
            ]
            Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                Button.a [
                    Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
                    Button.Props [ Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft "0"; PaddingRight "0"]; Tooltip.dataTooltip ("Add Annotation Table") ]
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
            ]
            Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                Button.a [
                    Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
                    Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft "0"; PaddingRight "0"]; Tooltip.dataTooltip ("Autoformat Table")]
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
            ]
            Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                Button.a [
                    Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
                    Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft "0"; PaddingRight "0"]; Tooltip.dataTooltip ("Update Reference Columns")]
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
            ]
            Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                Button.a [
                    Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
                    Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft "0"; PaddingRight "0"]; Tooltip.dataTooltip ("Remove Building Block")]
                    Button.OnClick (fun _ ->
                        PipeActiveAnnotationTable RemoveAnnotationBlock |> ExcelInterop |> dispatch
                    )
                    Button.Color Color.IsWhite
                    Button.IsInverted
                ] [ 
                    Fa.span [Fa.Solid.Minus; Fa.Props [Style [PaddingRight "0.15rem"]]][]
                    Fa.span [Fa.Solid.Columns][]
                ]
            ]
            Navbar.Item.a [Navbar.Item.Props [Style [ Color model.SiteStyleState.ColorMode.Text]]] [
                Button.a [
                    Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
                    Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.ElementBackground; PaddingLeft "0"; PaddingRight "0"]; Tooltip.dataTooltip ("Get Building Block Information")]
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
            Navbar.burger [
                Navbar.Burger.IsActive model.SiteStyleState.BurgerVisible
                Navbar.Burger.OnClick (fun e -> ToggleBurger |> StyleChange |> dispatch)
                Navbar.Burger.Modifiers [Modifier.TextColor IsWhite]
                Navbar.Burger.Props [
                        Role "button"
                        AriaLabel "menu"
                        Props.AriaExpanded false

            ]] [
                span [AriaHidden true] []
                span [AriaHidden true] []
                span [AriaHidden true] []
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