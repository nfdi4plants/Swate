namespace Protocol

open Fable.React
open Fable.React.Props
open Model
open Messages
open Feliz
open Feliz.DaisyUI
open Modals
open Swate.Components.Shared

module private HelperProtocolSearch =

    let breadcrumbEle (model: Model) setIsProtocolSearch dispatch =
        Html.button [
            prop.className "btn btn-outline btn-sm"
            prop.onClick (fun _ ->
                setIsProtocolSearch false
                UpdateModel {model with Model.PageState.SidebarPage = Routing.SidebarPage.Protocol} |> dispatch)
            prop.children [
                Html.i [ prop.className "fa-solid fa-chevron-left" ]
                Html.span [
                    prop.text "Back"
                ]
            ]
        ]

open Fable.Core

type SearchContainer =

    [<ReactComponent>]
    static member Main(model:Model, setProtocolSearch, importTypeStateData, dispatch, ?hasBreadCrumps) =
        let hasBreadCrumps = defaultArg hasBreadCrumps false
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
            prop.className "flex flex-col gap-2"
            prop.children [
                Html.div [
                    prop.className [ if hasBreadCrumps then "flex flex-row justify-between" else "flex justify-end" ]
                    prop.children [
                        if hasBreadCrumps then
                            HelperProtocolSearch.breadcrumbEle model setProtocolSearch dispatch
                        Html.div [
                            prop.className "flex flex-row gap-2"
                            prop.children [
                                Daisy.dropdown [
                                    prop.className "dropdown dropdown-bottom dropdown-end group relative z-[9999]"
                                    prop.children [
                                        Daisy.button.a [
                                            button.sm
                                            button.info
                                            prop.className "btn fa-solid fa-info"
                                            prop.tabIndex 0
                                        ]
                                        Daisy.dropdownContent [
                                            Html.ul [
                                                prop.tabIndex 0
                                                prop.className "relative left-1/2 -translate-x-1/2 mt-2 w-64 p-3 bg-gray-800 text-white text-sm rounded-lg shadow-xl opacity-0 group-hover:opacity-100 transition-opacity duration-300 z-[100]"
                                                prop.children [
                                                    Html.li [ Search.InfoField() ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                                Daisy.button.a [
                                    button.sm
                                    prop.className "fa-solid fa-cog"
                                    button.success
                                    prop.onClick(fun _ -> not showTemplatesFilter |> setShowTemplatesFilter)
                                ]
                            ]
                        ]
                    ]
                ]
                if isEmpty && not isLoading then
                    Html.p [prop.className "text-error text-sm"; prop.text "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."]

                Html.div [
                    prop.className "relative flex shadow-md gap-4 flex-col !m-0 !p-0"
                    prop.children [
                        if showTemplatesFilter then
                            Protocol.Search.FileSortElement(model, config, setConfig)
                        Search.SelectTemplatesButton(model, setProtocolSearch, importTypeStateData, dispatch)
                        Protocol.Search.Component (filteredTemplates, model, dispatch)
                    ]
                ]
            ]
        ]