module Renderer.Components.MainContent.GitDiffTarget

open Feliz
open Swate.Electron.Shared.GitTypes

[<ReactComponent>]
let Main (diffData: GitDiffViewDataDto) =
    let pageStateCtx = Renderer.Context.PageStateCtx.usePageState ()

    Html.div [
        prop.className "swt:flex swt:h-full swt:w-full swt:min-h-0 swt:min-w-0 swt:flex-col"
        prop.children [
            Html.div [
                prop.className "swt:flex swt:items-center swt:justify-end swt:border-b swt:border-base-content/10 swt:bg-base-100 swt:px-4 swt:py-2"
                prop.children [
                    Html.button [
                        prop.testId "renderer-git-diff-close"
                        prop.className "swt:btn swt:btn-ghost swt:btn-sm swt:gap-2 swt:normal-case"
                        prop.onClick (fun _ -> pageStateCtx.setState None)
                        prop.children [
                            Html.span [ prop.className "swt:iconify swt:fluent--dismiss-24-regular swt:size-4" ]
                            Html.span "Close"
                        ]
                    ]
                ]
            ]
            Html.div [
                prop.className "swt:min-h-0 swt:min-w-0 swt:flex-1 swt:p-4"
                prop.children [
                    Swate.Components.GitDiffViewer.Viewer(
                        wordDiffText = diffData.WordDiffText,
                        previousContent = diffData.PreviousContent,
                        currentContent = diffData.CurrentContent,
                        currentTitle = diffData.Path,
                        testIdPrefix = "renderer-git-diff"
                    )
                ]
            ]
        ]
    ]
