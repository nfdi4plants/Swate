module Renderer.Components.MainContent.GitMergeConflictTarget

open Fable.Core
open Feliz
open Swate.Electron.Shared.GitTypes

[<ReactComponent>]
let Main (mergeData: GitMergeConflictViewDataDto) =

    let gitStateCtx = Renderer.Context.GitStateCtx.useGitState ()
    let isConfirming, setIsConfirming = React.useState false
    let isConfirmingRef = React.useRef false

    let confirmMergeResolution resolvedContent =
        if isConfirmingRef.current then
            ()
        else
            isConfirmingRef.current <- true
            setIsConfirming true

            promise {
                let! result =
                    gitStateCtx.confirmMergeResolution {
                        Path = mergeData.Path
                        ExpectedConflictContent = mergeData.MergeConflictContent
                        ResolvedContent = resolvedContent
                    }

                match result with
                | Ok() ->
                    ()
                | Error _ ->
                    isConfirmingRef.current <- false
                    setIsConfirming false
            }
            |> Promise.start

    Html.div [
        prop.className "swt:h-full swt:w-full swt:min-h-0"
        prop.children [
            if isConfirming then
                Html.div [
                    prop.className "swt:border-b swt:border-base-content/10 swt:bg-base-200/70 swt:px-4 swt:py-2 swt:text-sm swt:text-base-content/70"
                    prop.text "Applying merge resolution..."
                ]

            Swate.Components.GitMergeConflictViewer.Viewer(
                mergeConflictContent = mergeData.MergeConflictContent,
                currentTitle = mergeData.Path,
                resolvedTitle = mergeData.Path,
                onConfirmMerge = confirmMergeResolution,
                testIdPrefix = "renderer-git-merge"
            )
        ]
    ]
