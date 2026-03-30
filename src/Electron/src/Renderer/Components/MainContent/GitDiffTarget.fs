module Renderer.Components.MainContent.GitDiffTarget

open Feliz
open Swate.Electron.Shared.GitTypes

[<ReactComponent>]
let Main (diffData: GitDiffViewDataDto) =
    Html.div [
        prop.className "swt:h-full swt:w-full swt:min-h-0"
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
