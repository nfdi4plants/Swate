module Renderer.Components.MainContent.GitHistoryDiffTarget

open Feliz
open Renderer.Context.GitHistoryContext
open Swate.Components.Page.GitComparison
open Swate.Components.Page.GitComparison.GitPagedDiffTypes
open Swate.Components.Page.GitHistory.Types

let private shortRevision (revision: string) =
    revision.Substring(0, min 7 revision.Length)

[<ReactComponent>]
let Main (revision: string, path: string) =
    let history = useGitHistoryCtx ()

    let diff =
        history.state.Diff
        |> Option.filter (fun value -> value.Request.Revision = revision && value.Request.Path = path)

    let value = diff |> Option.bind _.Value

    let previousTitle =
        match value with
        | Some loaded ->
            let previousPath = loaded.PreviousPath |> Option.defaultValue loaded.Path

            match loaded.ParentRevision with
            | Some parent -> $"{previousPath} @ {shortRevision parent}"
            | None -> "Before the first version (empty)"
        | None -> "Previous version"

    let status =
        match diff with
        | None ->
            PagedDiffStatus.Failed
                "This comparison is no longer available. Select the file again in the history sidebar."
        | Some pending when pending.Loading -> PagedDiffStatus.Opening
        | Some failed when failed.Error.IsSome -> PagedDiffStatus.Failed failed.Error.Value
        | Some _ ->
            match value |> Option.bind _.BlockedReason with
            | Some reason -> PagedDiffStatus.Blocked(None, reason)
            | None -> PagedDiffStatus.Ready

    let changeKind =
        match value |> Option.map _.Kind with
        | Some GitHistoryChangeKind.Added -> GitDiffChangeKind.Added
        | Some GitHistoryChangeKind.Deleted -> GitDiffChangeKind.Deleted
        | _ -> GitDiffChangeKind.Modified

    Html.div [
        prop.className "swt:flex swt:h-full swt:w-full swt:min-h-0 swt:min-w-0 swt:flex-col"
        prop.children [
            Html.div [
                prop.className "swt:flex swt:items-center swt:gap-3 swt:border-b swt:border-base-300 swt:px-4 swt:py-3"
                prop.children [
                    Html.button [
                        prop.type'.button
                        prop.className "swt:btn swt:btn-ghost swt:btn-sm swt:gap-2"
                        prop.onClick (fun _ -> history.closeDiff ())
                        prop.children [
                            Html.i [
                                prop.className "swt:iconify swt:fluent--dismiss-20-regular swt:size-4"
                            ]
                            Html.span "Close comparison"
                        ]
                    ]
                    Html.span [
                        prop.className "swt:min-w-0 swt:flex-1 swt:truncate swt:text-sm swt:text-base-content/60"
                        prop.title path
                        prop.text $"{path} · {shortRevision revision}"
                    ]
                    if diff |> Option.exists (fun pending -> pending.Error.IsSome) then
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-sm swt:btn-outline"
                            prop.text "Retry"
                            prop.onClick (fun _ -> history.retryDiff ())
                        ]
                ]
            ]
            Html.div [
                prop.className "swt:min-h-0 swt:min-w-0 swt:flex-1 swt:p-4"
                prop.children [
                    GitPagedDiffViewer.Viewer(
                        parts = (value |> Option.map _.Parts |> Option.defaultValue [||]),
                        status = status,
                        progress = None,
                        hasMore = false,
                        outputComplete = true,
                        previousTitle = previousTitle,
                        currentTitle = $"{path} @ {shortRevision revision}",
                        changeKind = changeKind,
                        testIdPrefix = "git-history-diff"
                    )
                ]
            ]
        ]
    ]
