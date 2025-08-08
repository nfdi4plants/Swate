namespace Protocol

open Model
open Feliz
open Swate.Components

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

    static member HeaderElement(dispatch: Messages.Msg -> unit) =
        Html.div [
            prop.className [ "swt:flex" ]
            prop.children [
                HelperProtocolSearch.breadcrumbEle dispatch
            ]
        ]

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =

        React.useEffectOnce (fun _ -> Messages.Protocol.GetAllProtocolsRequest |> Messages.ProtocolMsg |> dispatch)

        let isEmpty =
            model.ProtocolState.Templates |> isNull
            || model.ProtocolState.Templates |> Array.isEmpty

        let isLoading = model.ProtocolState.Loading


        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2 swt:lg:gap-4 swt:overflow-hidden"
            prop.children [
                SearchContainer.HeaderElement(dispatch)
                TemplateFilter.TemplateFilterProvider(
                    React.fragment [
                        TemplateFilter.TemplateFilter(model.ProtocolState.Templates, key = "template-filter-provider")
                        TemplateFilter.FilteredTemplateRenderer(fun filteredTemplates ->
                            React.fragment[
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
                        )
                    ]
                )
            ]
        ]