module Protocol.Search

open Fable.React
open Fable.React.Props
open Model
open Messages
open Feliz
open Feliz.Bulma

let breadcrumbEle (model:Model) dispatch =
    Bulma.breadcrumb [
        Bulma.breadcrumb.hasArrowSeparator
        prop.children [
            Html.ul [
                Html.li [Html.a [
                    prop.onClick (fun _ -> UpdatePageState (Some Routing.Route.Protocol) |> dispatch)
                    prop.text (Routing.Route.Protocol.toStringRdbl)
                ]]
                Html.li [
                    prop.className "is-active"
                    prop.children (Html.a [
                        prop.onClick (fun _ -> UpdatePageState (Some Routing.Route.Protocol) |> dispatch)
                        prop.text (Routing.Route.ProtocolSearch.toStringRdbl)
                    ])
                ]    
            ]
        ]
    ]

open Fable.Core

[<ReactComponent>]
let ProtocolSearchView (model:Model) dispatch =
    React.useEffectOnce(fun () ->
        Messages.Protocol.GetAllProtocolsRequest |> ProtocolMsg |> dispatch
    )
    let isEmpty = model.ProtocolState.ProtocolsAll |> isNull || model.ProtocolState.ProtocolsAll |> Array.isEmpty
    let isLoading = model.ProtocolState.Loading
    div [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle model dispatch

        if isEmpty && not isLoading then
            Bulma.help [Bulma.color.isDanger; prop.text "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

        if isLoading then
            Modals.Loading.loadingModal

        Bulma.label "Search the database for protocol templates."

        if not isEmpty then
            Protocol.Component.ProtocolContainer model dispatch
    ]