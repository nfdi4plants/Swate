module Renderer.Components.MainContent.GitUnsupportedTarget

open Feliz
open Renderer.Types

[<ReactComponent>]
let Main (unsupportedPage: GitUnsupportedPageData) =
    Html.div [
        prop.className "swt:flex swt:h-full swt:w-full swt:items-center swt:justify-center swt:p-8"
        prop.children [
            Html.div [
                prop.className
                    "swt:max-w-xl swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-100 swt:p-6 swt:shadow-sm"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:items-center swt:gap-3"
                        prop.children [
                            Html.span [
                                prop.className "swt:iconify swt:fluent--warning-24-regular swt:size-6 swt:text-warning"
                            ]
                            Html.h2 [
                                prop.className "swt:text-lg swt:font-semibold"
                                prop.text "Preview not available"
                            ]
                        ]
                    ]
                    Html.p [
                        prop.className "swt:mt-4 swt:text-sm swt:text-base-content/70"
                        prop.text
                            $"Swate can only open plain-text git diffs and merge conflicts here. '{unsupportedPage.Path}' is not supported."
                    ]
                    match unsupportedPage.Reason with
                    | Some reason ->
                        Html.pre [
                            prop.className
                                "swt:mt-4 swt:overflow-x-auto swt:rounded-box swt:bg-base-200/80 swt:p-3 swt:text-xs swt:text-base-content/80"
                            prop.text reason
                        ]
                    | None -> Html.none
                ]
            ]
        ]
    ]
