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
                        prop.onClick (fun _ ->
                            setIsProtocolSearch false
                            UpdateModel {model with Model.PageState.SidebarPage = Routing.SidebarPage.Protocol} |> dispatch)
                        prop.text "Back"
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
    static member Main (model:Model) setProtocolSearch importTypeStateData dispatch =
        let templates, setTemplates = React.useState(model.ProtocolState.Templates)
        let config, setConfig = React.useState(TemplateFilterConfig.init)
        let showTemplatesFilter, setShowTemplatesFilter = React.useState(false)
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
                Html.div [
                    prop.className "flex items-center gap-4"
                    prop.children [
                        HelperProtocolSearch.breadcrumbEle model setProtocolSearch dispatch
                        Daisy.button.a [
                            prop.className "fa-solid fa-cog flex items-center gap-4"
                            button.success
                            prop.onClick(fun _ -> not showTemplatesFilter |> setShowTemplatesFilter)
                        ]
                    ]
                ]
                if isEmpty && not isLoading then
                    Html.p [prop.className "text-error text-sm"; prop.text "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

                Html.div [
                    prop.className "relative flex p-4 shadow-md gap-4 flex-col !m-0 !p-0"
                    prop.children [
                        if showTemplatesFilter then
                            Protocol.Search.InfoField()
                            Protocol.Search.FileSortElement(model, config, setConfig)
                        Search.SelectedTemplatesElement model setProtocolSearch importTypeStateData dispatch
                        Protocol.Search.Component (filteredTemplates, model, dispatch)
                    ]
                ]
            ]
        ]