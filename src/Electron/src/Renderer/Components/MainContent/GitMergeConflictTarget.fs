module Renderer.Components.MainContent.GitMergeConflictTarget

open Fable.Core
open Feliz
open Swate.Electron.Shared.GitTypes

[<ReactComponent>]
let Main (mergeData: GitMergeConflictViewDataDto) =

    let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()

    let isConfirmingCurrentPath =
        gitStateCtx.state.MergeResolutionPendingPath = Some mergeData.Path

    let isMergeResolutionBusy =
        match gitStateCtx.state.BusyOperation with
        | Some(Renderer.Context.GitWorkflow.GitBusyOperation.ConfirmingMergeResolution _) -> true
        | _ -> false

    let confirmMergeResolution resolvedContent =
        if isMergeResolutionBusy then
            ()
        else
            gitStateCtx.confirmMergeResolution {
                Path = mergeData.Path
                ExpectedConflictContent = mergeData.MergeConflictContent
                ResolvedContent = resolvedContent
                AutoCommit = true
            }

    Html.div [
        prop.className "swt:h-full swt:w-full swt:min-h-0"
        prop.children [
            if isConfirmingCurrentPath then
                Html.div [
                    prop.className
                        "swt:border-b swt:border-base-content/10 swt:bg-base-200/70 swt:px-4 swt:py-2 swt:text-sm swt:text-base-content/70"
                    prop.text "Applying merge resolution..."
                ]

            Swate.Components.GitMergeConflictViewer.Viewer(
                mergeConflictContent = mergeData.MergeConflictContent,
                currentTitle = mergeData.Path,
                resolvedTitle = mergeData.Path,
                onConfirmMerge = confirmMergeResolution,
                confirmDisabled = isMergeResolutionBusy,
                testIdPrefix = "renderer-git-merge"
            )
        ]
    ]