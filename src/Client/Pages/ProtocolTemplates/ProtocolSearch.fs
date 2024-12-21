namespace Protocol

open Fable.React
open Fable.React.Props
open Model
open Messages
open Feliz
open Feliz.DaisyUI
open Modals

module private HelperProtocolSearch =

    let breadcrumbEle (model:Model) setIsProtocolSearch dispatch =
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
                            prop.onClick (fun _ ->
                                setIsProtocolSearch true
                                UpdateModel model |> dispatch)
                        ])
                    ]
                ]
            ]
        ]

open Fable.Core

type SearchContainer =

    [<ReactComponent>]
    static member Main (model:Model) setProtocolSearch importTypeState setImportTypeState dispatch =
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
                HelperProtocolSearch.breadcrumbEle model setProtocolSearch dispatch

                if isEmpty && not isLoading then
                    Html.p [prop.className "text-error text-sm"; prop.text "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

                Html.p "Search the database for protocol templates."

                Html.div [
                    prop.className "relative flex p-4 shadow-md gap-4 flex-col"
                    prop.children [
                        Protocol.Search.InfoField()
                        Protocol.Search.FileSortElement(model, config, setConfig)
                        ModalElements.Box("Selected Templates", "fa-solid fa-cog", Search.SelectedTemplatesElement model setProtocolSearch importTypeState setImportTypeState dispatch)
                        Protocol.Search.Component (filteredTemplates, model, setProtocolSearch, importTypeState, setImportTypeState, dispatch)
                    ]
                ]
            ]
        ]