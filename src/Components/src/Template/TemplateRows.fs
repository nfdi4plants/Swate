namespace Swate.Components.Template

open ARCtrl
open Fable.Core
open Feliz

module TemplateHelper = Swate.Components.Template.Helper

[<Erase; Mangle(false)>]
type TemplateRows =

    [<ReactComponent>]
    static member TemplateRows
        (templates: Template[], selectedTemplateIds: Set<System.Guid>, toggleTemplateSelection: System.Guid -> unit)
        =
        if templates.Length = 0 then
            Html.div [
                prop.className "swt:text-sm swt:opacity-70 swt:text-center"
                prop.text "No templates found."
            ]
        else
            Html.table [
                prop.className "swt:table swt:table-fixed swt:w-full"
                prop.children [
                    Html.thead [
                        Html.tr [
                            Html.th [ prop.className "swt:w-10"; prop.text "" ]
                            Html.th [ prop.className "swt:w-[35%]"; prop.text "Template" ]
                            Html.th [ prop.className "swt:w-[20%]"; prop.text "Organisation" ]
                            Html.th [ prop.className "swt:w-[15%]"; prop.text "Version" ]
                            Html.th [ prop.className "swt:w-[30%]"; prop.text "Authors" ]
                        ]
                    ]
                    Html.tbody [
                        for template in templates do
                            let isSelected = selectedTemplateIds.Contains template.Id

                            let authors =
                                template.Authors
                                |> Seq.map TemplateHelper.toFullAuthorName
                                |> String.concat ", "

                            let toggleThisTemplate _ = toggleTemplateSelection template.Id

                            Html.tr [
                                prop.key (string template.Id)
                                prop.className [
                                    "swt:cursor-pointer hover:swt:bg-base-200"
                                    if isSelected then
                                        "swt:bg-primary/10"
                                ]
                                prop.onClick toggleThisTemplate
                                prop.children [
                                    Html.td [
                                        prop.className "swt:w-10"
                                        prop.children [
                                            Html.input [
                                                prop.className "swt:checkbox"
                                                prop.isChecked isSelected
                                                prop.type'.checkbox
                                                prop.custom ("readOnly", true)
                                                prop.onChange (fun (_: bool) -> toggleTemplateSelection template.Id)
                                                prop.onClick (fun event -> event.stopPropagation ())
                                            ]
                                        ]
                                    ]
                                    Html.td [
                                        prop.className "swt:truncate"
                                        prop.title template.Name
                                        prop.text template.Name
                                    ]
                                    Html.td [
                                        prop.className "swt:truncate"
                                        prop.title (template.Organisation.ToString())
                                        prop.text (template.Organisation.ToString())
                                    ]
                                    Html.td [
                                        prop.className "swt:truncate"
                                        prop.title template.Version
                                        prop.text template.Version
                                    ]
                                    Html.td [
                                        prop.className
                                            "swt:text-xs swt:opacity-70 swt:whitespace-nowrap swt:overflow-hidden swt:text-ellipsis"
                                        prop.title authors
                                        prop.text authors
                                    ]
                                ]
                            ]
                    ]
                ]
            ]