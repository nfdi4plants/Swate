namespace Swate.Components.Page.GitHistory

open System
open Fable.Core
open Feliz
open Swate.Components.Page.GitHistory.Types

module private GitHistoryHelpers =

    let shortRevision (revision: string) =
        revision.Substring(0, min 8 revision.Length)

    let subject (message: string) =
        message.Split('\n').[0].Trim()
        |> fun value ->
            if String.IsNullOrWhiteSpace value then
                "Untitled saved version"
            else
                value

    let friendlyDate (timestamp: string) =
        match DateTime.TryParse timestamp with
        | true, parsed ->
            let localDate = parsed.ToLocalTime()

            if localDate.Date = DateTime.Now.Date then
                "Today"
            elif localDate.Date = DateTime.Now.Date.AddDays(-1.) then
                "Yesterday"
            else
                localDate.ToString("MMM d, yyyy")
        | _ -> timestamp

    let changeLabel =
        function
        | GitHistoryChangeKind.Added -> "Added"
        | GitHistoryChangeKind.Modified -> "Modified"
        | GitHistoryChangeKind.Deleted -> "Deleted"
        | GitHistoryChangeKind.Renamed -> "Renamed"
        | GitHistoryChangeKind.Copied -> "Copied"
        | GitHistoryChangeKind.TypeChanged -> "Type changed"

    let changeColor =
        function
        | GitHistoryChangeKind.Added -> "swt:text-success"
        | GitHistoryChangeKind.Deleted -> "swt:text-error"
        | GitHistoryChangeKind.Renamed
        | GitHistoryChangeKind.Copied -> "swt:text-info"
        | _ -> "swt:text-warning"

    let fileName (path: string) =
        path.Substring(path.LastIndexOf('/') + 1)

    let directory (path: string) =
        let separator = path.LastIndexOf('/')
        if separator < 0 then "" else path.Substring(0, separator)

[<Erase; Mangle(false)>]
type GitHistory =

    [<ReactComponent>]
    static member private ChangeBadge(label: string, text: string, color: string) =
        Html.span [
            prop.role "img"
            prop.ariaLabel label
            prop.title label
            prop.className (
                "swt:inline-flex swt:items-center swt:gap-0.5 swt:rounded swt:px-1 swt:font-mono swt:text-[10px] swt:leading-4 swt:tabular-nums "
                + color
            )
            prop.text text
        ]

    [<ReactComponent>]
    static member private ChangeBadges(summary: GitHistoryChangeSummary option) =
        Html.span [
            prop.role "group"
            prop.ariaLabel "File change summary"
            prop.className "swt:flex swt:min-w-0 swt:flex-wrap swt:gap-1"
            prop.children [
                match summary with
                | None ->
                    GitHistory.ChangeBadge(
                        "File change summary unavailable",
                        "Counts unavailable",
                        "swt:bg-base-200 swt:text-base-content/55"
                    )
                | Some counts ->
                    let categories = [|
                        "A", "added", counts.Added, "swt:bg-success/10 swt:text-success"
                        "D", "deleted", counts.Deleted, "swt:bg-error/10 swt:text-error"
                        "M", "modified", counts.Modified, "swt:bg-warning/10 swt:text-warning"
                        "R", "renamed", counts.Renamed, "swt:bg-info/10 swt:text-info"
                        "C", "copied", counts.Copied, "swt:bg-secondary/10 swt:text-secondary"
                        "T", "type changed", counts.TypeChanged, "swt:bg-accent/10 swt:text-accent"
                    |]

                    if categories |> Array.forall (fun (_, _, count, _) -> count = 0) then
                        GitHistory.ChangeBadge("0 changed files", "0 files", "swt:bg-base-200 swt:text-base-content/55")
                    else
                        for code, name, count, color in categories do
                            if count > 0 then
                                let noun = if count = 1 then "file" else "files"
                                GitHistory.ChangeBadge($"{count} {name} {noun}", $"{code} {count}", color)
            ]
        ]

    [<ReactComponent>]
    static member private FileRow
        (
            commit: GitHistoryCommit,
            file: GitHistoryFileChange,
            selected: bool,
            onSelect: GitHistoryCommit -> GitHistoryFileChange -> unit
        ) =
        let label = GitHistoryHelpers.changeLabel file.Kind
        let directory = GitHistoryHelpers.directory file.Path

        let description =
            file.Path
            + (file.PreviousPath
               |> Option.map (fun previous -> "\nFrom " + previous)
               |> Option.defaultValue "")
            + (if file.Insertions.IsNone && file.Deletions.IsNone then
                   "\nBinary / no line counts"
               else
                   "")

        Html.li [
            prop.key file.Path
            prop.children [
                Html.button [
                    prop.type'.button
                    prop.ariaPressed selected
                    prop.ariaLabel $"{label}: {file.Path}"
                    prop.title description
                    prop.className (
                        "swt:flex swt:min-h-6 swt:w-full swt:min-w-0 swt:items-center swt:gap-1.5 swt:rounded swt:px-1.5 swt:py-1 swt:text-left swt:text-xs swt:leading-4 swt:focus-visible:outline-2 swt:focus-visible:outline-primary "
                        + if selected then
                              "swt:bg-primary/10"
                          else
                              "swt:hover:bg-base-200"
                    )
                    prop.onClick (fun _ -> onSelect commit file)
                    prop.children [
                        Html.span [
                            prop.className "swt:flex swt:min-w-0 swt:flex-1 swt:items-baseline swt:gap-1.5"
                            prop.children [
                                Html.span [
                                    prop.className "swt:min-w-0 swt:truncate"
                                    prop.text (GitHistoryHelpers.fileName file.Path)
                                ]
                                if directory <> "" then
                                    Html.span [
                                        prop.className
                                            "swt:min-w-0 swt:flex-1 swt:truncate swt:text-[10px] swt:text-base-content/45"
                                        prop.text directory
                                    ]
                            ]
                        ]
                        Html.span [
                            prop.className ("swt:shrink-0 swt:text-[10px] " + GitHistoryHelpers.changeColor file.Kind)
                            prop.text label
                        ]
                        if file.Insertions.IsSome || file.Deletions.IsSome then
                            Html.span [
                                prop.className
                                    "swt:flex swt:shrink-0 swt:gap-1 swt:font-mono swt:text-[10px] swt:tabular-nums"
                                prop.children [
                                    match file.Insertions with
                                    | Some count when count > 0 ->
                                        Html.span [
                                            prop.ariaLabel $"{count} lines added"
                                            prop.className "swt:text-success"
                                            prop.text $"+{count}"
                                        ]
                                    | _ -> ()
                                    match file.Deletions with
                                    | Some count when count > 0 ->
                                        Html.span [
                                            prop.ariaLabel $"{count} lines deleted"
                                            prop.className "swt:text-error"
                                            prop.text $"−{count}"
                                        ]
                                    | _ -> ()
                                ]
                            ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private CommitRow
        (
            commit: GitHistoryCommit,
            details: GitHistoryCommitChanges option,
            expanded: bool,
            selectedRevision: string option,
            selectedPath: string option,
            onToggle: string -> unit,
            onSelect: GitHistoryCommit -> GitHistoryFileChange -> unit
        ) =
        let selected = selectedRevision = Some commit.Revision

        let summary =
            match commit.Summary, details with
            | Some summary, _ -> Some summary
            | None, Some data when not data.Loading && data.Error.IsNone ->
                Some(GitHistoryChangeSummary.ofChanges data.Files)
            | _ -> None

        Html.li [
            prop.key commit.Revision
            prop.className "swt:border-b swt:border-base-200 swt:last:border-b-0"
            prop.children [
                Html.button [
                    prop.type'.button
                    prop.ariaExpanded expanded
                    prop.ariaLabel (
                        (if expanded then "Collapse" else "Expand")
                        + " saved version: "
                        + GitHistoryHelpers.subject commit.Message
                    )
                    prop.title commit.Message
                    prop.className (
                        "swt:flex swt:min-h-10 swt:w-full swt:min-w-0 swt:items-center swt:gap-1.5 swt:rounded swt:px-1 swt:py-1 swt:text-left swt:focus-visible:outline-2 swt:focus-visible:outline-primary "
                        + if selected then
                              "swt:bg-primary/5"
                          else
                              "swt:hover:bg-base-200/60"
                    )
                    prop.onClick (fun _ -> onToggle commit.Revision)
                    prop.children [
                        Html.i [
                            prop.ariaHidden true
                            prop.className (
                                "swt:iconify swt:size-3 swt:shrink-0 swt:text-base-content/50 "
                                + if expanded then
                                      "swt:fluent--chevron-down-20-regular"
                                  else
                                      "swt:fluent--chevron-right-20-regular"
                            )
                        ]
                        Html.div [
                            prop.className "swt:flex swt:min-w-0 swt:flex-1 swt:flex-col"
                            prop.children [
                                Html.span [
                                    prop.className "swt:truncate swt:text-xs swt:font-medium swt:leading-4"
                                    prop.text (GitHistoryHelpers.subject commit.Message)
                                ]
                                Html.div [
                                    prop.className
                                        "swt:flex swt:min-w-0 swt:items-center swt:gap-1.5 swt:text-[10px] swt:leading-4 swt:text-base-content/55"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:min-w-0 swt:truncate"
                                            prop.title commit.AuthorEmail
                                            prop.text (
                                                if String.IsNullOrWhiteSpace commit.AuthorName then
                                                    "Unknown author"
                                                else
                                                    commit.AuthorName
                                            )
                                        ]
                                        Html.time [
                                            prop.className "swt:shrink-0"
                                            prop.dateTime commit.CommittedAt
                                            prop.title $"Committed: {commit.CommittedAt}\nAuthored: {commit.AuthoredAt}"
                                            prop.text (GitHistoryHelpers.friendlyDate commit.CommittedAt)
                                        ]
                                        Html.code [
                                            prop.className "swt:shrink-0 swt:font-mono"
                                            prop.title commit.Revision
                                            prop.text (GitHistoryHelpers.shortRevision commit.Revision)
                                        ]
                                        if commit.ParentRevisions.Length > 1 then
                                            Html.span [
                                                prop.className "swt:shrink-0 swt:text-info"
                                                prop.title "Merge: changes are compared with the first parent."
                                                prop.text "Merge"
                                            ]
                                    ]
                                ]
                                GitHistory.ChangeBadges summary
                            ]
                        ]
                    ]
                ]
                if expanded then
                    Html.div [
                        prop.className "swt:mb-1 swt:ml-4 swt:border-l swt:border-base-200 swt:pl-1"
                        prop.children [
                            match details with
                            | None ->
                                Html.p [
                                    prop.role "status"
                                    prop.className "swt:px-1.5 swt:py-1 swt:text-xs swt:text-base-content/60"
                                    prop.text "Loading changed files…"
                                ]
                            | Some data ->
                                if data.Loading then
                                    Html.p [
                                        prop.role "status"
                                        prop.className "swt:px-1.5 swt:py-1 swt:text-xs swt:text-base-content/60"
                                        prop.text "Loading changed files…"
                                    ]

                                match data.Error with
                                | Some message ->
                                    Html.div [
                                        prop.role.alert
                                        prop.className "swt:rounded swt:bg-error/5 swt:p-2 swt:text-xs"
                                        prop.children [
                                            Html.p "Changed files could not be loaded."
                                            Html.p [
                                                prop.className "swt:break-words swt:text-base-content/65"
                                                prop.text message
                                            ]
                                            Html.button [
                                                prop.type'.button
                                                prop.className "swt:btn swt:btn-xs swt:btn-ghost swt:mt-1"
                                                prop.disabled data.Loading
                                                prop.text "Retry files"
                                                prop.onClick (fun _ -> onToggle commit.Revision)
                                            ]
                                        ]
                                    ]
                                | None when not data.Loading && Array.isEmpty data.Files ->
                                    Html.p [
                                        prop.className "swt:px-1.5 swt:py-1 swt:text-xs swt:text-base-content/60"
                                        prop.text "No file changes in this version."
                                    ]
                                | _ -> ()

                                Html.ul [
                                    prop.ariaLabel "Changed files"
                                    prop.children [
                                        for file in data.Files do
                                            GitHistory.FileRow(
                                                commit,
                                                file,
                                                selected && selectedPath = Some file.Path,
                                                onSelect
                                            )
                                    ]
                                ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member GitHistory
        (
            commits: GitHistoryCommit[],
            changes: GitHistoryCommitChanges[],
            expandedRevisions: string[],
            onToggleCommit: string -> unit,
            onSelectFile: GitHistoryCommit -> GitHistoryFileChange -> unit,
            onLoadMore: unit -> unit,
            onRefresh: unit -> unit,
            ?branchName: string,
            ?selectedRevision: string,
            ?selectedPath: string,
            ?loading: bool,
            ?hasMore: bool,
            ?error: string,
            ?scrollTop: float,
            ?onScroll: float -> unit
        ) =
        let loading = defaultArg loading false
        let hasMore = defaultArg hasMore false
        let containerRef = React.useElementRef ()
        let expanded = Set.ofArray expandedRevisions

        let loadedChanges =
            changes |> Array.map (fun item -> item.Revision, item) |> Map.ofArray

        React.useEffect (
            (fun () ->
                match scrollTop, containerRef.current with
                | Some top, Some element when abs (element.scrollTop - top) > 1. -> element.scrollTop <- top
                | _ -> ()
            ),
            [| box scrollTop |]
        )

        Html.section [
            prop.ariaLabel "Git history"
            prop.className
                "swt:flex swt:h-full swt:min-h-0 swt:min-w-0 swt:flex-col swt:bg-base-100 swt:text-base-content"
            prop.children [
                Html.header [
                    prop.className
                        "swt:flex swt:shrink-0 swt:items-center swt:gap-2 swt:border-b swt:border-base-300 swt:px-1 swt:py-1.5"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-sm swt:font-semibold"
                            prop.text "History"
                        ]
                        Html.div [
                            prop.className
                                "swt:flex swt:min-w-0 swt:flex-1 swt:items-center swt:gap-1 swt:text-[11px] swt:text-base-content/60"
                            prop.children [
                                Html.i [
                                    prop.ariaHidden true
                                    prop.className "swt:iconify swt:fluent--branch-20-regular swt:size-3 swt:shrink-0"
                                ]
                                Html.span [
                                    prop.className "swt:truncate"
                                    prop.title (defaultArg branchName "Current branch")
                                    prop.text (defaultArg branchName "Current branch")
                                ]
                            ]
                        ]
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-xs swt:btn-square swt:btn-ghost"
                            prop.ariaLabel "Refresh history"
                            prop.title "Refresh history"
                            prop.disabled loading
                            prop.onClick (fun _ -> onRefresh ())
                            prop.children [
                                Html.i [
                                    prop.ariaHidden true
                                    prop.className "swt:iconify swt:fluent--arrow-clockwise-20-regular swt:size-3.5"
                                ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.ref containerRef
                    prop.className "swt:min-h-0 swt:flex-1 swt:overflow-auto swt:py-1"
                    prop.onScroll (fun _ ->
                        containerRef.current
                        |> Option.iter (fun element ->
                            onScroll |> Option.iter (fun callback -> callback element.scrollTop)
                        )
                    )
                    prop.children [
                        match error with
                        | Some message ->
                            Html.div [
                                prop.role.alert
                                prop.className "swt:mb-1 swt:rounded swt:bg-error/5 swt:p-2 swt:text-xs"
                                prop.children [
                                    Html.p [
                                        prop.className "swt:font-medium"
                                        prop.text "History could not be loaded."
                                    ]
                                    Html.p [
                                        prop.className "swt:break-words swt:text-base-content/65"
                                        prop.text message
                                    ]
                                    Html.button [
                                        prop.type'.button
                                        prop.className "swt:btn swt:btn-xs swt:mt-1"
                                        prop.disabled loading
                                        prop.onClick (fun _ -> onRefresh ())
                                        prop.text "Retry history"
                                    ]
                                ]
                            ]
                        | None -> ()
                        if Array.isEmpty commits && not loading && error.IsNone then
                            Html.div [
                                prop.className "swt:px-2 swt:py-4 swt:text-xs swt:text-base-content/60"
                                prop.children [
                                    Html.p [
                                        prop.className "swt:font-medium"
                                        prop.text "No saved versions yet"
                                    ]
                                    Html.p "Versions saved on this branch will appear here."
                                ]
                            ]
                        if not (Array.isEmpty commits) then
                            Html.ol [
                                prop.ariaLabel "Saved versions, newest first"
                                prop.children [
                                    for commit in commits do
                                        GitHistory.CommitRow(
                                            commit,
                                            Map.tryFind commit.Revision loadedChanges,
                                            expanded.Contains commit.Revision,
                                            selectedRevision,
                                            selectedPath,
                                            onToggleCommit,
                                            onSelectFile
                                        )
                                ]
                            ]
                        if loading then
                            Html.div [
                                prop.role "status"
                                prop.className
                                    "swt:flex swt:items-center swt:justify-center swt:gap-2 swt:py-3 swt:text-xs swt:text-base-content/60"
                                prop.children [
                                    Html.span [
                                        prop.ariaHidden true
                                        prop.className "swt:loading swt:loading-spinner swt:loading-xs"
                                    ]
                                    Html.span "Loading history…"
                                ]
                            ]
                        if hasMore then
                            Html.button [
                                prop.type'.button
                                prop.className "swt:btn swt:btn-xs swt:btn-ghost swt:mt-1 swt:w-full"
                                prop.disabled loading
                                prop.onClick (fun _ -> onLoadMore ())
                                prop.text "Load older versions"
                            ]
                        elif not loading && not (Array.isEmpty commits) && error.IsNone then
                            Html.p [
                                prop.className "swt:py-2 swt:text-center swt:text-[10px] swt:text-base-content/45"
                                prop.text "Beginning of this branch’s history"
                            ]
                    ]
                ]
            ]
        ]
