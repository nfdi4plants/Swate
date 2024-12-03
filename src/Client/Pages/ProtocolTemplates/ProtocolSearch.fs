namespace Protocol

open Fable.React
open Fable.React.Props
open Model
open Messages
open Feliz
open Feliz.DaisyUI

open Components

module private HelperProtocolSearch =

    let breadcrumbEle (model:Model) dispatch =
        Daisy.breadcrumbs [
            prop.children [
                Html.ul [
                    Html.li [Html.a [
                        prop.onClick (fun _ -> UpdateModel {model with Model.PageState.SidebarPage = Routing.SidebarPage.Protocol} |> dispatch)
                        prop.text (Routing.SidebarPage.Protocol.AsStringRdbl)
                    ]]
                    Html.li [
                        prop.className "is-active"
                        prop.children (Html.a [
                            prop.onClick (fun _ -> UpdateModel {model with Model.PageState.SidebarPage = Routing.SidebarPage.ProtocolSearch} |> dispatch)
                            prop.text (Routing.SidebarPage.ProtocolSearch.AsStringRdbl)
                        ])
                    ]
                ]
            ]
        ]

open Fable.Core

type SearchContainer =

    [<ReactComponent>]
    static member Main (model:Model) dispatch =
        let templates, setTemplates = React.useState(model.ProtocolState.Templates)
        let config, setConfig = React.useState(TemplateFilterConfig.init)
        let filteredTemplates = Protocol.Search.filterTemplates (templates, config)
        React.useEffectOnce(fun _ -> Messages.Protocol.GetAllProtocolsRequest |> Messages.ProtocolMsg |> dispatch)
        React.useEffect((fun _ -> setTemplates model.ProtocolState.Templates), [|box model.ProtocolState.Templates|])
        let isEmpty = model.ProtocolState.Templates |> isNull || model.ProtocolState.Templates |> Array.isEmpty
        let isLoading = model.ProtocolState.Loading
        Html.div [
            prop.onSubmit (fun e -> e.preventDefault())
            // https://keycode.info/
            prop.onKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
            prop.children [
                HelperProtocolSearch.breadcrumbEle model dispatch

                if isEmpty && not isLoading then
                    Html.p [prop.className "text-error text-sm"; prop.text "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

                Html.p "Search the database for protocol templates."

                Components.LogicContainer [
                    Protocol.Search.InfoField()
                    Protocol.Search.FileSortElement(model, config, setConfig)
                    Protocol.Search.Component (filteredTemplates, model, dispatch)
                ]
            ]
        ]