namespace Protocol

open ARCtrl

open Fable.React
open Fable.React.Props
open Model
open Messages
open Feliz
open Feliz.DaisyUI
open Modals
open Swate.Components
open Swate.Components.Shared

module private HelperProtocolSearch =

    let breadcrumbEle setIsProtocolSearch =
        Html.button [
            prop.className "swt:btn swt:btn-outline swt:btn-sm"
            prop.onClick (fun _ ->
                Messages.Protocol.UpdateShowSearch false
                |> Messages.ProtocolMsg
                |> setIsProtocolSearch)
            prop.children [
                Icons.ChevronLeft()
                Html.span [ prop.text "Back" ]
            ]
        ]

open Fable.Core

type SearchContainer =

    static member HeaderElement(toggleShowFilter, dispatch: Messages.Msg -> unit) =
        Html.div [
            prop.className [ "swt:flex" ]
            prop.children [
                HelperProtocolSearch.breadcrumbEle dispatch
                Html.div [
                    prop.className "swt:flex swt:flex-row swt:gap-2 swt:ml-auto"
                    prop.children [
                        //Daisy.dropdown [
                        Html.div [
                            prop.className "swt:dropdown swt:dropdown-bottom swt:dropdown-end swt:group swt:relative swt:z-[9999]"
                            prop.children [
                                //Daisy.button.a [
                                Html.button [
                                    prop.className "swt:btn swt:btn-sm swt:btn-info"
                                    prop.children [
                                        Icons.Info()
                                    ]
                                    prop.tabIndex 0
                                ]
                                //Daisy.dropdownContent [
                                Html.div [
                                    prop.className "swt:dropdown-content"
                                    prop.children [
                                        Html.ul [
                                            prop.tabIndex 0
                                            prop.className
                                                "swt:relative swt:left-1/2 swt:-translate-x-1/2 swt:mt-2 swt:w-64 p-3 swt:bg-gray-800 swt:text-white swt:text-sm swt:rounded-lg swt:shadow-xl swt:opacity-0 swt:group-hover:opacity-100 swt:transition-opacity swt:duration-300 swt:z-[100]"
                                            prop.children [ Html.li [ Search.InfoField() ] ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        //Daisy.button.a [
                        Html.button [
                            prop.className "swt:btn swt:btn-sm swt:btn-success"
                            prop.children [
                                Icons.Cog()
                            ]
                            prop.onClick (fun _ -> toggleShowFilter ())
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =
        let showTemplatesFilter, setShowTemplatesFilter = React.useState (false)

        let filteredTemplates, setFilteredTemplates = React.useState model.ProtocolState.Templates

        React.useEffectOnce (fun _ -> Messages.Protocol.GetAllProtocolsRequest |> Messages.ProtocolMsg |> dispatch)

        let isEmpty =
            model.ProtocolState.Templates |> isNull
            || model.ProtocolState.Templates |> Array.isEmpty

        let isLoading = model.ProtocolState.Loading

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:lg:gap-4 swt:overflow-hidden"
            prop.children [
                SearchContainer.HeaderElement((fun _ -> setShowTemplatesFilter (not showTemplatesFilter)), dispatch)

                TemplateFilter.TemplateFilter(
                    model.ProtocolState.Templates, key = "template-filter", onFilteredTemplatesChanged = setFilteredTemplates)

                if isEmpty && not isLoading then
                    Html.p [
                        prop.className "swt:text-error swt:text-sm"
                        prop.text
                            "No templates were found. This can happen if connection to the server was lost. You can try reload this site or contact a developer."
                    ]
                else
                    Search.SelectTemplatesButton(model, dispatch)
                    Protocol.Search.Component(filteredTemplates, model, dispatch)
            ]
        ]