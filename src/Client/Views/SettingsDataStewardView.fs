module SettingsDataStewardView

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
//open Fable.Core.JS
open Fable.Core.JsInterop

open Shared

open Model
open Messages


let breadcrumbEle model dispatch =
    Breadcrumb.breadcrumb [Breadcrumb.HasArrowSeparator][
        Breadcrumb.item [][
            a [
                OnClick (fun e -> UpdatePageState (Some Routing.Route.Settings) |> dispatch)
            ][
                str (Routing.Route.Settings.toStringRdbl)
            ]
        ]
        Breadcrumb.item [ Breadcrumb.Item.IsActive true ][
            a [
                Style [Color model.SiteStyleState.ColorMode.Text]
                OnClick (fun e -> UpdatePageState (Some Routing.Route.SettingsDataStewards) |> dispatch)
            ][
                str Routing.Route.SettingsXml.toStringRdbl
            ]
        ]
    ]

let createPointerJsonButton (model:Model) dispatch =
    Columns.columns [Columns.IsMobile][
        Column.column [][
            Button.a [
                Button.Color IsInfo
                Button.IsFullWidth
                Button.OnClick (fun e -> CreatePointerJson |> ExcelInterop |> dispatch)
            ][
                str "Create pointer json"
            ]
        ]
        if model.SettingsDataStewardState.PointerJson.IsSome then
            Column.column [Column.Width (Screen.All, Column.IsNarrow)][
                Button.a [
                    Button.OnClick (fun e -> UpdatePointerJson None |> SettingDataStewardMsg |> dispatch)
                    Button.Color IsDanger
                ][
                    Fa.i [Fa.Solid.Times][]
                ]
            ]
    ]

let textFieldEle (model:Model) dispatch =
    Columns.columns [Columns.IsMobile][
        Column.column [][
            Textarea.textarea [
                Textarea.Color IsSuccess
                Textarea.IsReadOnly true
                Textarea.Value model.SettingsDataStewardState.PointerJson.Value
            ][]
        ]
        Column.column [Column.Width (Screen.All, Column.IsNarrow)] [
            Field.div [][
                Button.a [
                    Button.Props [
                        Style [Width "40.5px"]
                        Title "Copy to Clipboard"
                    ]
                    Button.Color IsInfo
                    Button.OnClick (fun e ->
                        CustomComponents.ResponsiveFA.triggerResponsiveReturnEle "clipboard_settingsDataSteward"
                        let txt = model.SettingsDataStewardState.PointerJson.Value
                        let textArea = Browser.Dom.document.createElement "textarea"
                        textArea?value <- txt
                        textArea?style?top <- "0"
                        textArea?style?left <- "0"
                        textArea?style?position <- "fixed"

                        Browser.Dom.document.body.appendChild textArea |> ignore

                        textArea.focus()
                        /// Can't belive this actually worked
                        textArea?select()

                        let t = Browser.Dom.document.execCommand("copy")
                        Browser.Dom.document.body.removeChild(textArea) |> ignore
                        ()
                    )
                ][
                    CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_settingsDataSteward" Fa.Regular.Clipboard Fa.Solid.Check
                ]
            ]
        ]
    ]

let createPointerJsonEle (model:Model) dispatch =
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ][
        Field.div [][
            b [] [str "Here you can create a prefilled template for a template pointer .json."]
            str " This is used to smooth the creation of new templates "
            a [Href @"https://github.com/nfdi4plants/SWATE_templates"; Target "_Blank"][str "here"]
            str ". If you want to create/request a new template please open an issue "
            a [Href @"https://github.com/nfdi4plants/SWATE_templates/issues/new?assignees=&labels=&template=feature_request.md&title=%5BFEATURE%5D"; Target "_Blank"][str "here"]
            str "."
        ]

        Field.div [][
            createPointerJsonButton model dispatch
        ]

        if model.SettingsDataStewardState.PointerJson.IsSome then
            Field.div [][
                textFieldEle model dispatch
            ]
    ]

let settingsDataStewardViewComponent (model:Model) dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Display raw custom xml."]
        createPointerJsonEle model dispatch

    ]