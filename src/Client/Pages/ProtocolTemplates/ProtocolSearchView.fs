module Protocol.Search

open Fulma
open Fable.React
open Fable.React.Props
open Model
open Messages

let breadcrumbEle (model:Model) dispatch =
    Breadcrumb.breadcrumb [Breadcrumb.HasArrowSeparator] [
        Breadcrumb.item [] [
            a [
                OnClick (fun _ -> UpdatePageState (Some Routing.Route.Protocol) |> dispatch)
            ] [
                str (Routing.Route.Protocol.toStringRdbl)
            ]
        ];
        Breadcrumb.item [
            Breadcrumb.Item.IsActive true
        ] [
            a [
                Style [Color model.SiteStyleState.ColorMode.Text]
                OnClick (fun _ -> UpdatePageState (Some Routing.Route.Protocol) |> dispatch)
            ] [
                str Routing.Route.ProtocolSearch.toStringRdbl
            ]
        ]
    ]


let protocolSearchView (model:Model) dispatch =
    let isEmpty = model.ProtocolState.ProtocolsAll |> isNull || model.ProtocolState.ProtocolsAll |> Array.isEmpty
    let isLoading = model.ProtocolState.Loading
    div [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle model dispatch

        if isEmpty && not isLoading then
            Help.help [Help.Color IsDanger] [str "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

        if isLoading then
            Modals.Loading.loadingModal

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Search the database for protocol templates."]

        if not isEmpty then
            Protocol.Component.ProtocolContainer model dispatch
    ]