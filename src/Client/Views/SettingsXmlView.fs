module SettingsXmlView

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open Fable.Core.JS
open Fable.Core.JsInterop

open Shared

open Model
open Messages

let dangerZone model dispatch =
    div [][
        Label.label [][str "Dangerzone"]
        Container.container [
            Container.Props [Style [
                Padding "1rem"
                Border (sprintf "2.5px solid %s" NFDIColors.Red.Base)
                BorderRadius "10px"
            ]]
        ][
            Button.a [
                Button.Color Color.IsDanger
                Button.IsFullWidth
                Button.OnClick (fun e -> DeleteAllCustomXml |> ExcelInterop |> dispatch )
                Button.Props [Style []; Title "Be sure you know what you do. This cannot be undone!"]
            ] [
                Icon.icon [ ] [
                    Fa.i [Fa.Solid.ExclamationTriangle][]
                ]
                span [] [str "Delete All Custom Xml!"]
                Icon.icon [ ] [
                    Fa.i [Fa.Solid.ExclamationTriangle][]
                ]
            ]
        ]
    ]

let breadcrumbEle dispatch =
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
                OnClick (fun e -> UpdatePageState (Some Routing.Route.SettingsXml) |> dispatch)
            ][
                str Routing.Route.SettingsXml.toStringRdbl
            ]
        ]
    ]

let showCustomXmlButton model dispatch =
    Field.div [][
        Button.a [
            Button.Color Color.IsInfo
            Button.IsFullWidth
            Button.OnClick (fun e -> GetSwateCustomXml |> ExcelInterop |> dispatch )
            Button.Props [Title "Show record type data of Swate custom Xml"]
        ] [
            span [] [str "Show Custom Xml"]
        ]
    ]

let textAreaEle (model:Model) dispatch = 
    Media.media [][
        Media.content [][
            Field.div [][
                Control.div [][
                    Textarea.textarea [
                        Textarea.Props [Style []]
                        Textarea.IsReadOnly true
                        Textarea.Value model.SettingsXmlState.RawXml
                    ][ ]
                ]
            ]
        ]
        Media.right [][
            Field.div [][
                Button.a [
                    Button.Props [Title "Copy to Clipboard"]
                    Button.Color IsInfo
                    Button.OnClick (fun e ->
                        let txt = model.SettingsXmlState.RawXml
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
                    Fa.i [Fa.Regular.Clipboard ] [] 
                ]
            ]
            Field.div [][
                Button.a [
                    Button.OnClick (fun e -> UpdateRawCustomXml "" |> SettingXmlMsg |> dispatch)
                    Button.Color IsDanger
                    Button.Props [Title "Remove custom xml from the text area"]
                ][
                    Fa.i [Fa.Solid.Times][]
                ]
            ]
        ]
    ]


let showCustomXmlEle (model:Model) dispatch =
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            //BorderRadius "15px 15px 0 0"
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ][
        showCustomXmlButton model dispatch

        if model.SettingsXmlState.RawXml <> "" then
            textAreaEle model dispatch
    ]

let settingsXmlViewComponent (model:Model) dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Display custom xml."]

        showCustomXmlEle model dispatch

        dangerZone model dispatch
    ]