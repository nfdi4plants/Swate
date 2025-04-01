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

    let breadcrumbEle setIsProtocolSearch =
        Html.button [
            prop.className "btn btn-outline btn-sm"
            prop.onClick (fun _ ->
                Messages.Protocol.UpdateShowSearch false
                |> Messages.ProtocolMsg
                |> setIsProtocolSearch)
            prop.children [
                Html.i [ prop.className "fa-solid fa-chevron-left" ]
                Html.span [ prop.text "Back" ]
            ]
        ]

open Fable.Core

type SearchContainer =

    static member HeaderElement(toggleShowFilter, dispatch: Messages.Msg -> unit) =
        Html.div [
            prop.className [ "flex" ]
            prop.children [
                HelperProtocolSearch.breadcrumbEle dispatch
                Html.div [
                    prop.className "flex flex-row gap-2 ml-auto"
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
                                        prop.className
                                            "relative left-1/2 -translate-x-1/2 mt-2 w-64 p-3 bg-gray-800 text-white text-sm rounded-lg shadow-xl opacity-0 group-hover:opacity-100 transition-opacity duration-300 z-[100]"
                                        prop.children [ Html.li [ Search.InfoField() ] ]
                                    ]
                                ]
                            ]
                        ]
                        Daisy.button.a [
                            button.sm
                            prop.className "fa-solid fa-cog"
                            button.success
                            prop.onClick (fun _ -> toggleShowFilter ())
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =
        let config, setConfig = React.useState (TemplateFilterConfig.init)
        let showTemplatesFilter, setShowTemplatesFilter = React.useState (false)

        let filteredTemplates =
            React.useMemo (
                (fun _ -> Protocol.Search.filterTemplates (model.ProtocolState.Templates, config)),
                [| box model.ProtocolState.Templates; box config |]
            )

        React.useEffectOnce (fun _ -> Messages.Protocol.GetAllProtocolsRequest |> Messages.ProtocolMsg |> dispatch)

        let isEmpty =
            model.ProtocolState.Templates |> isNull
            || model.ProtocolState.Templates |> Array.isEmpty

        let isLoading = model.ProtocolState.Loading

        Html.div [
            prop.className "flex flex-col gap-2 lg:gap-4 overflow-hidden"
            prop.children [
                SearchContainer.HeaderElement((fun _ -> setShowTemplatesFilter (not showTemplatesFilter)), dispatch)
                if showTemplatesFilter then
                    Protocol.Search.FileSortElement(model, config, setConfig)
                if isEmpty && not isLoading then
                    Html.p [
                        prop.className "text-error text-sm"
                        prop.text
                            "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."
                    ]
                else
                    Search.SelectTemplatesButton(model, dispatch)
                    Protocol.Search.Component(filteredTemplates, model, dispatch)
            ]
        ]