module SettingsProtocolView

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
                OnClick (fun e -> UpdatePageState (Some Routing.Route.SettingsXml) |> dispatch)
            ][
                str Routing.Route.SettingsProtocol.toStringRdbl
            ]
        ]
    ]

let getActiveProtocolButton (model:Model) dispatch =
    Columns.columns [Columns.IsMobile][
        Column.column [][
            Button.a [
                Button.IsFullWidth
                Button.Color IsInfo
                Button.OnClick (fun e -> GetActiveProtocolGroupXmlParsed |> SettingsProtocolMsg |> dispatch)
            ][
                str "Check protocols for version"
            ]
        ]
        if model.SettingsProtocolState.ProtocolsFromDB <> [||] || model.SettingsProtocolState.ProtocolsFromExcel.IsSome then
            Column.column [Column.Width(Screen.All, Column.IsNarrow)][
                Button.a [
                    Button.OnClick (fun e ->
                        UpdateProtocolsFromDB [||] |> SettingsProtocolMsg |> dispatch
                        UpdateProtocolsFromExcel None |> SettingsProtocolMsg |> dispatch
                    )
                    Button.Color IsDanger
                ][
                    Fa.i [Fa.Solid.Times][]
                ]
            ]
    ]

let splitVersion (str:string) =
    let s = str.Split([|"."|], System.StringSplitOptions.RemoveEmptyEntries)
    {|Major = s.[0]; Minor = s.[1]; Patch = s.[2]|}

open OfficeInterop.Types.Xml.GroupTypes

let applyNewestVersionButton (protocol:Protocol) (dbProtocolTemplate:Shared.ProtocolTemplate) dispatch =
    Button.a [
        Button.IsStatic (protocol.ProtocolVersion = dbProtocolTemplate.Version)
        Button.Size IsSmall
        Button.Color IsWarning
        Button.IsFullWidth
        Button.OnClick (fun e ->
            let msg = UpdateProtocolByNewVersion (protocol, dbProtocolTemplate) |> SettingsProtocolMsg
            let messageBody = "This function has major impact on your table. Please save your progress before clicking 'Continue'."
            let nM = {|ModalMessage = messageBody;NextMsg = msg|} |> Some
            UpdateWarningModal nM |> dispatch
        )
    ][
        str "update"
    ]

let displayVersionControlEle (model:Model) dispatch =
    Table.table [Table.IsFullWidth][
        thead [][
            tr [][
                th [][str "Protocol Name"]
                th [][str "Used Version"]
                th [][str "Newest Version"]
                th [][str "Docs"]
                th [][]
            ]
        ]
        tbody [][
            for prot in model.SettingsProtocolState.ProtocolsFromExcel.Value.Protocols do
                let dbProts = model.SettingsProtocolState.ProtocolsFromDB
                let relatedDBProt = dbProts |> Array.tryFind (fun x -> x.Name = prot.Id)
                let color =
                    if relatedDBProt.IsNone then
                        None
                    else
                        let dbVersion = splitVersion relatedDBProt.Value.Version
                        let usedVersion = splitVersion prot.ProtocolVersion
                        if dbVersion.Major > usedVersion.Major then
                            Some NFDIColors.Red.Base
                        elif dbVersion.Minor > usedVersion.Minor then
                            Some "orange"
                        elif dbVersion.Patch > usedVersion.Patch then
                            Some NFDIColors.Yellow.Lighter20
                        elif dbVersion = usedVersion then
                            Some NFDIColors.Mint.Base
                        else
                            None
                let docTag() = Tag.tag [] [ a [ OnClick (fun e -> e.stopPropagation()); Href relatedDBProt.Value.DocsLink; Target "_Blank" ] [str "docs"] ]
                yield
                    tr [][
                        td [][str prot.Id]
                        td [
                            if relatedDBProt.IsNone then
                                Title "Could not find protocol in DB."
                            elif color.IsNone then
                                Title "Versions could not be compared. Make sure they have the format '1.0.0'"
                            else
                                Style [Color color.Value; FontWeight "bold"]
                        ][
                            str prot.ProtocolVersion
                        ]
                        td [][str (if relatedDBProt.IsSome then relatedDBProt.Value.Version else "-")]
                        td [][if relatedDBProt.IsSome then docTag() else str "-"]
                        td [][if relatedDBProt.IsSome && relatedDBProt.Value.Version <> prot.ProtocolVersion then applyNewestVersionButton prot relatedDBProt.Value dispatch]
                    ]
        ]
    ]

let checkProtocolEle (model:Model) dispatch =
    div [ Style [
        BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
        Padding "0.25rem 1rem"
        MarginBottom "1rem"
    ]] [
        Field.div [][
            str "Here you can check protocols, used in the Swate table of your open Excel worksheet, for updates."
        ]

        Field.div [][
            getActiveProtocolButton model dispatch
        ]

        if model.SettingsProtocolState.ProtocolsFromExcel.IsSome then
            Field.div [][
                displayVersionControlEle model dispatch
            ]
    ]

let settingsProtocolViewComponent (model:Model) dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Check protocols for newest versions."]
        checkProtocolEle model dispatch
    ]