namespace Swate.Components

open System
open Fable.Core
open Feliz

open Swate.Components.GitSidebarTypes

module private GitSidebarInternal =

    let hasConflicts (status: GitSidebarStatus) (changedFiles: GitSidebarChange[]) =
        status.IsMergeInProgress || (changedFiles |> Array.exists _.IsConflicted)

    let normalizeStatusCode (code: string) =
        let trimmed = code.Trim()

        if String.IsNullOrWhiteSpace trimmed then
            "."
        else
            trimmed

    let describeChange (change: GitSidebarChange) =
        let indexCode = normalizeStatusCode change.IndexStatus
        let worktreeCode = normalizeStatusCode change.WorkingTreeStatus
        $"{indexCode}{worktreeCode}"

    let changeBadgeClasses (change: GitSidebarChange) =
        if change.IsConflicted then
            "swt:badge swt:badge-error swt:badge-sm"
        else
            "swt:badge swt:badge-ghost swt:badge-sm"

    let branchKindLabel (kind: GitSidebarBranchKind) =
        match kind with
        | GitSidebarBranchKind.Local -> "Local"
        | GitSidebarBranchKind.Remote -> "Remote"

    let progressText (progress: GitSidebarProgress) =
        [
            progress.Method
            progress.Stage
            progress.ProgressPercent |> Option.map (fun value -> $"{Math.Round(value)}%%")
        ]
        |> List.choose id
        |> String.concat " | "

[<Erase; Mangle(false)>]
type GitSidebar =

    [<ReactComponent>]
    static member private SectionHeader(title: string, countText: string option) =
        Html.div [
            prop.className "swt:flex swt:items-center swt:justify-between swt:gap-2 swt:px-3 swt:pt-3 swt:text-xs swt:font-semibold swt:uppercase swt:tracking-[0.2em] swt:text-base-content/60"
            prop.children [
                Html.span title
                match countText with
                | Some value ->
                    Html.span [
                        prop.className "swt:text-[0.65rem] swt:text-base-content/50"
                        prop.text value
                    ]
                | None ->
                    Html.none
            ]
        ]

    [<ReactComponent>]
    static member private ActionButton
        (
            label: string,
            iconClassName: string,
            isBusy: bool,
            onClick: unit -> unit,
            ?testId: string
        ) =
        Html.button [
            if testId.IsSome then
                prop.testId testId.Value
            prop.className
                "swt:btn swt:btn-sm swt:justify-start swt:gap-2 swt:normal-case swt:bg-base-100 swt:border-base-300"
            prop.disabled isBusy
            prop.onClick (fun _ -> onClick ())
            prop.children [
                Html.span [ prop.className ("swt:iconify " + iconClassName + " swt:size-4") ]
                Html.span label
            ]
        ]

    [<ReactComponent>]
    static member Main
        (
            status: GitSidebarStatus,
            changedFiles: GitSidebarChange[],
            branchOptions: GitSidebarBranchOption[],
            onRefresh: unit -> JS.Promise<Result<unit, string>>,
            onFetch: unit -> JS.Promise<Result<unit, string>>,
            onPull: unit -> JS.Promise<Result<unit, string>>,
            onPush: unit -> JS.Promise<Result<unit, string>>,
            onSync: unit -> JS.Promise<Result<unit, string>>,
            onCommitSelection: GitSidebarCommitSelectionRequest -> JS.Promise<Result<unit, string>>,
            onCommitAll: string -> JS.Promise<Result<unit, string>>,
            onCreateBranch: GitSidebarCreateBranchRequest -> JS.Promise<Result<unit, string>>,
            onSwitchBranch: string -> JS.Promise<Result<unit, string>>,
            onSelectChange: GitSidebarChange -> JS.Promise<Result<unit, string>>,
            ?currentProgress: GitSidebarProgress,
            ?selectedFile: string,
            ?busyNotice: string,
            ?errorNotice: string
        ) =

        let busyNotice = busyNotice
        let errorNotice = errorNotice
        let selectedFile = selectedFile

        let localError, setLocalError = React.useState (None: string option)
        let activeAction, setActiveAction = React.useState (None: string option)
        let isCreateBranchModalOpen, setIsCreateBranchModalOpen = React.useState false
        let isSwitchBranchModalOpen, setIsSwitchBranchModalOpen = React.useState false
        let branchName, setBranchName = React.useState ""
        let commitMessage, setCommitMessage = React.useState ""
        let selectedCommitPaths, setSelectedCommitPaths = React.useStateWithUpdater Set.empty<string>
        let selectedStartPoint, setSelectedStartPoint = React.useState (None: string option)
        let selectedSwitchBranch, setSelectedSwitchBranch = React.useState (None: string option)

        let isBusy =
            activeAction.IsSome || busyNotice.IsSome

        let visibleError =
            errorNotice |> Option.orElse localError

        React.useEffect (
            (fun () ->
                setSelectedCommitPaths (fun current ->
                    current
                    |> Set.filter (fun path ->
                        changedFiles
                        |> Array.exists (fun change -> String.Equals(change.Path, path, StringComparison.Ordinal))
                    )
                )),
            [| box changedFiles |]
        )

        let branchOptionsWithHead =
            if branchOptions.Length = 0 then
                [| None, "Current HEAD" |]
            else
                [| None, "Current HEAD" |]
                |> Array.append (
                    branchOptions
                    |> Array.map (fun branch ->
                        let marker =
                            [
                                if branch.IsCurrent then
                                    "current"

                                if branch.IsTracking then
                                    "tracking"
                            ]
                            |> String.concat ", "

                        let label =
                            if String.IsNullOrWhiteSpace marker then
                                $"{branch.DisplayLabel} ({GitSidebarInternal.branchKindLabel branch.Kind})"
                            else
                                $"{branch.DisplayLabel} ({GitSidebarInternal.branchKindLabel branch.Kind}, {marker})"

                        Some branch.RefName, label
                    )
                )

        let localBranchOptionsForSwitch =
            branchOptions
            |> Array.filter (fun branch ->
                branch.Kind = GitSidebarBranchKind.Local
                && not branch.IsCurrent
            )

        let runAction (label: string) (operation: unit -> JS.Promise<Result<unit, string>>) =
            promise {
                setLocalError None
                setActiveAction (Some label)

                try
                    let! result = operation ()

                    match result with
                    | Ok () -> ()
                    | Error message -> setLocalError (Some message)
                finally
                    setActiveAction None
            }
            |> Promise.start

        let openCreateBranchModal () =
            setLocalError None
            setBranchName ""

            let defaultStartPoint =
                branchOptions
                |> Array.tryFind _.IsTracking
                |> Option.orElseWith (fun () -> branchOptions |> Array.tryFind _.IsCurrent)
                |> Option.map _.RefName

            setSelectedStartPoint defaultStartPoint
            setIsCreateBranchModalOpen true

        let openSwitchBranchModal () =
            setLocalError None

            let defaultBranch =
                localBranchOptionsForSwitch
                |> Array.tryHead
                |> Option.map _.RefName

            setSelectedSwitchBranch defaultBranch
            setIsSwitchBranchModalOpen true

        let toggleCommitSelection (path: string) =
            setSelectedCommitPaths (fun current ->
                if Set.contains path current then
                    Set.remove path current
                else
                    Set.add path current
            )

        let submitCommitSelection () =
            let normalizedCommitMessage = commitMessage.Trim()
            let selectedPaths =
                selectedCommitPaths
                |> Set.toArray
                |> Array.sort

            if String.IsNullOrWhiteSpace normalizedCommitMessage then
                setLocalError (Some "Commit message must not be empty.")
            elif selectedPaths.Length = 0 then
                setLocalError (Some "Select at least one file to commit.")
            else
                promise {
                    setLocalError None
                    setActiveAction (Some "Commit Selection")

                    try
                        let! result =
                            onCommitSelection {
                                Message = normalizedCommitMessage
                                Paths = selectedPaths
                            }

                        match result with
                        | Ok () ->
                            setCommitMessage ""
                            setSelectedCommitPaths (fun _ -> Set.empty)
                        | Error message ->
                            setLocalError (Some message)
                    finally
                        setActiveAction None
                }
                |> Promise.start

        let submitCommitAll () =
            let normalizedCommitMessage = commitMessage.Trim()

            if String.IsNullOrWhiteSpace normalizedCommitMessage then
                setLocalError (Some "Commit message must not be empty.")
            else
                promise {
                    setLocalError None
                    setActiveAction (Some "Commit All")

                    try
                        let! result = onCommitAll normalizedCommitMessage

                        match result with
                        | Ok () ->
                            setCommitMessage ""
                        | Error message ->
                            setLocalError (Some message)
                    finally
                        setActiveAction None
                }
                |> Promise.start

        let submitCreateBranch () =
            let normalizedBranchName = branchName.Trim()

            if String.IsNullOrWhiteSpace normalizedBranchName then
                setLocalError (Some "Branch name must not be empty.")
            else
                promise {
                    setLocalError None
                    setActiveAction (Some "Create Branch From")

                    try
                        let! result =
                            onCreateBranch {
                                BranchName = normalizedBranchName
                                StartPoint = selectedStartPoint
                            }

                        match result with
                        | Ok () ->
                            setIsCreateBranchModalOpen false
                            setBranchName ""
                        | Error message ->
                            setLocalError (Some message)
                    finally
                        setActiveAction None
                }
                |> Promise.start

        let submitSwitchBranch () =
            match selectedSwitchBranch with
            | None ->
                setLocalError (Some "Select a branch to switch to.")
            | Some branchName ->
                promise {
                    setLocalError None
                    setActiveAction (Some "Switch Branch")

                    try
                        let! result = onSwitchBranch branchName

                        match result with
                        | Ok () ->
                            setIsSwitchBranchModalOpen false
                        | Error message ->
                            setLocalError (Some message)
                    finally
                        setActiveAction None
                }
                |> Promise.start

        let hasConflicts = GitSidebarInternal.hasConflicts status changedFiles
        let canEditCommit = not status.IsClean && not hasConflicts && not isBusy
        let selectedCommitCount = Set.count selectedCommitPaths
        let canSubmitCommitSelection =
            canEditCommit
            && selectedCommitCount > 0
            && not (String.IsNullOrWhiteSpace(commitMessage.Trim()))

        let canSubmitCommitAll =
            canEditCommit
            && not (String.IsNullOrWhiteSpace(commitMessage.Trim()))

        Html.div [
            prop.testId "GitSidebar"
            prop.className "swt:flex swt:h-full swt:min-h-0 swt:flex-col swt:bg-base-100"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:justify-between swt:border-b swt:border-base-content/10 swt:px-3 swt:py-3"
                    prop.children [
                        Html.div [
                            prop.className "swt:flex swt:items-center swt:gap-2"
                            prop.children [
                                Html.span [ prop.className "swt:iconify swt:fluent--branch-fork-24-regular swt:size-5" ]
                                Html.div [
                                    prop.className "swt:flex swt:flex-col"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:text-sm swt:font-semibold"
                                            prop.text "Source Control"
                                        ]
                                        Html.span [
                                            prop.className "swt:text-xs swt:text-base-content/60"
                                            prop.text (
                                                match status.CurrentBranch with
                                                | Some branch -> branch
                                                | None -> "Detached HEAD"
                                            )
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.button [
                            prop.testId "GitSidebarRefreshButton"
                            prop.className "swt:btn swt:btn-ghost swt:btn-square swt:btn-sm"
                            prop.disabled isBusy
                            prop.title "Refresh git status"
                            prop.onClick (fun _ -> runAction "Refresh" onRefresh)
                            prop.children [
                                Html.span [ prop.className "swt:iconify swt:fluent--arrow-clockwise-24-regular swt:size-4" ]
                            ]
                        ]
                    ]
                ]

                Html.div [
                    prop.className "swt:px-3 swt:pt-3"
                    prop.children [
                        Html.div [
                            prop.className "swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-200/60 swt:p-3"
                            prop.children [
                                Html.div [
                                    prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:badge swt:badge-primary swt:badge-sm"
                                            prop.text (
                                                if status.IsClean then
                                                    "Clean"
                                                else
                                                    "Changes"
                                            )
                                        ]

                                        if hasConflicts then
                                            Html.span [
                                                prop.className "swt:badge swt:badge-warning swt:badge-sm"
                                                prop.text "Merge in progress"
                                            ]

                                        if status.Ahead > 0 then
                                            Html.span [
                                                prop.className "swt:badge swt:badge-outline swt:badge-sm"
                                                prop.text $"Ahead {status.Ahead}"
                                            ]

                                        if status.Behind > 0 then
                                            Html.span [
                                                prop.className "swt:badge swt:badge-outline swt:badge-sm"
                                                prop.text $"Behind {status.Behind}"
                                            ]
                                    ]
                                ]

                                match status.TrackingBranch with
                                | Some trackingBranch ->
                                    Html.div [
                                        prop.className "swt:mt-2 swt:flex swt:items-center swt:gap-2 swt:text-xs swt:text-base-content/70"
                                        prop.children [
                                            Html.span [ prop.className "swt:iconify swt:fluent--arrow-sync-24-regular swt:size-4" ]
                                            Html.span $"Tracking {trackingBranch}"
                                        ]
                                    ]
                                | None ->
                                    Html.none
                            ]
                        ]
                    ]
                ]

                GitSidebar.SectionHeader("Actions", None)

                Html.div [
                    prop.className "swt:grid swt:grid-cols-2 swt:gap-2 swt:px-3"
                    prop.children [
                        GitSidebar.ActionButton(
                            "Fetch",
                            "swt:fluent--arrow-download-24-regular",
                            isBusy,
                            (fun () -> runAction "Fetch" onFetch),
                            testId = "GitSidebarFetchButton"
                        )
                        GitSidebar.ActionButton(
                            "Pull",
                            "swt:fluent--arrow-down-24-regular",
                            isBusy,
                            (fun () -> runAction "Pull" onPull),
                            testId = "GitSidebarPullButton"
                        )
                        GitSidebar.ActionButton(
                            "Push",
                            "swt:fluent--arrow-up-24-regular",
                            isBusy,
                            (fun () -> runAction "Push" onPush),
                            testId = "GitSidebarPushButton"
                        )
                        GitSidebar.ActionButton(
                            "Sync",
                            "swt:fluent--arrow-sync-24-regular",
                            isBusy,
                            (fun () -> runAction "Sync" onSync),
                            testId = "GitSidebarSyncButton"
                        )
                    ]
                ]

                Html.div [
                    prop.className "swt:px-3 swt:pt-2"
                    prop.children [
                        Html.div [
                            prop.className "swt:grid swt:grid-cols-2 swt:gap-2"
                            prop.children [
                                GitSidebar.ActionButton(
                                    "Create Branch",
                                    "swt:fluent--branch-fork-24-regular",
                                    isBusy,
                                    openCreateBranchModal,
                                    testId = "GitSidebarCreateBranchButton"
                                )
                                GitSidebar.ActionButton(
                                    "Switch Branch",
                                    "swt:fluent--arrow-swap-24-regular",
                                    isBusy || localBranchOptionsForSwitch.Length = 0,
                                    openSwitchBranchModal,
                                    testId = "GitSidebarSwitchBranchButton"
                                )
                            ]
                        ]
                    ]
                ]

                GitSidebar.SectionHeader("Commit", None)

                Html.div [
                    prop.className "swt:px-3"
                    prop.children [
                        Html.div [
                            prop.className "swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-100 swt:p-3"
                            prop.children [
                                Html.label [
                                    prop.className "swt:flex swt:flex-col swt:gap-2"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:text-sm swt:font-medium"
                                            prop.text "Commit message"
                                        ]
                                        Html.textarea [
                                            prop.testId "GitSidebarCommitMessageInput"
                                            prop.className
                                                "swt:textarea swt:textarea-bordered swt:min-h-24 swt:w-full swt:resize-y"
                                            prop.disabled (not canEditCommit)
                                            prop.value commitMessage
                                            prop.placeholder (
                                                if hasConflicts then
                                                    "Resolve merge conflicts before committing."
                                                elif status.IsClean then
                                                    "No changes to commit."
                                                else
                                                    "Describe your changes"
                                            )
                                            prop.onChange setCommitMessage
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "swt:mt-2 swt:flex swt:items-center swt:justify-between swt:gap-3 swt:text-xs swt:text-base-content/60"
                                    prop.children [
                                        Html.span (
                                            if selectedCommitCount = 1 then
                                                "1 file selected for commit selection"
                                            else
                                                $"{selectedCommitCount} files selected for commit selection"
                                        )
                                        if not canEditCommit && hasConflicts then
                                            Html.span "Commit selection is disabled while conflicts remain."
                                        elif not canEditCommit && status.IsClean then
                                            Html.span "No changes available to commit."
                                        else
                                            Html.none
                                    ]
                                ]
                                Html.div [
                                    prop.className "swt:mt-3 swt:grid swt:grid-cols-2 swt:gap-2"
                                    prop.children [
                                        Html.button [
                                            prop.testId "GitSidebarCommitSelectionButton"
                                            prop.className "swt:btn swt:btn-sm swt:btn-outline swt:gap-2 swt:normal-case"
                                            prop.disabled (not canSubmitCommitSelection)
                                            prop.onClick (fun _ -> submitCommitSelection ())
                                            prop.children [
                                                Html.span [
                                                    prop.className
                                                        "swt:iconify swt:fluent--checkbox-checked-24-regular swt:size-4"
                                                ]
                                                Html.span (
                                                    if activeAction = Some "Commit Selection" then
                                                        "Committing..."
                                                    else
                                                        "Commit Selection"
                                                )
                                            ]
                                        ]
                                        Html.button [
                                            prop.testId "GitSidebarCommitAllButton"
                                            prop.className "swt:btn swt:btn-sm swt:btn-secondary swt:gap-2 swt:normal-case"
                                            prop.disabled (not canSubmitCommitAll)
                                            prop.onClick (fun _ -> submitCommitAll ())
                                            prop.children [
                                                Html.span [
                                                    prop.className
                                                        "swt:iconify swt:fluent--checkmark-circle-24-regular swt:size-4"
                                                ]
                                                Html.span (
                                                    if activeAction = Some "Commit All" then
                                                        "Committing..."
                                                    else
                                                        "Commit All"
                                                )
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]

                match currentProgress with
                | Some progress ->
                    Html.div [
                        prop.className "swt:px-3 swt:pt-3"
                        prop.children [
                            Html.div [
                                prop.testId "GitSidebarProgressNotice"
                                prop.className "swt:alert swt:alert-info swt:px-3 swt:py-2 swt:text-sm"
                                prop.children [
                                    Html.span [ prop.className "swt:iconify swt:fluent--arrow-sync-circle-24-regular swt:size-4" ]
                                    Html.div [
                                        prop.className "swt:flex swt:min-w-0 swt:flex-col"
                                        prop.children [
                                            Html.span [
                                                prop.className "swt:font-medium"
                                                prop.text (
                                                    busyNotice
                                                    |> Option.defaultValue
                                                        (GitSidebarInternal.progressText progress |> function
                                                            | "" -> "Git operation in progress"
                                                            | text -> text)
                                                )
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                | None ->
                    match busyNotice with
                    | Some notice ->
                        Html.div [
                            prop.className "swt:px-3 swt:pt-3"
                            prop.children [
                                Html.div [
                                    prop.testId "GitSidebarBusyNotice"
                                    prop.className "swt:alert swt:alert-info swt:px-3 swt:py-2 swt:text-sm"
                                    prop.children [
                                        Html.span [ prop.className "swt:iconify swt:fluent--clock-24-regular swt:size-4" ]
                                        Html.span notice
                                    ]
                                ]
                            ]
                        ]
                    | None ->
                        Html.none

                match visibleError with
                | Some message ->
                    Html.div [
                        prop.className "swt:px-3 swt:pt-3"
                        prop.children [
                            Html.div [
                                prop.testId "GitSidebarErrorNotice"
                                prop.className "swt:alert swt:alert-error swt:px-3 swt:py-2 swt:text-sm"
                                prop.children [
                                    Html.span [ prop.className "swt:iconify swt:fluent--warning-24-regular swt:size-4" ]
                                    Html.span message
                                ]
                            ]
                        ]
                    ]
                | None ->
                    Html.none

                if hasConflicts then
                    Html.div [
                        prop.className "swt:px-3 swt:pt-3"
                        prop.children [
                            Html.div [
                                prop.testId "GitSidebarMergeBanner"
                                prop.className "swt:alert swt:alert-warning swt:px-3 swt:py-2 swt:text-sm"
                                prop.children [
                                    Html.span [ prop.className "swt:iconify swt:fluent--warning-shield-24-regular swt:size-4" ]
                                    Html.span "Resolve all conflicted files before pushing."
                                ]
                            ]
                        ]
                    ]

                GitSidebar.SectionHeader(
                    "Changes",
                    Some(
                        if changedFiles.Length = 1 then
                            "1 file"
                        else
                            $"{changedFiles.Length} files"
                    )
                )

                Html.div [
                    prop.className "swt:min-h-0 swt:flex-1 swt:overflow-y-auto swt:px-3 swt:pb-3"
                    prop.children [
                        if changedFiles.Length = 0 then
                            Html.div [
                                prop.className "swt:mt-2 swt:rounded-box swt:border swt:border-dashed swt:border-base-content/15 swt:bg-base-200/40 swt:px-4 swt:py-6 swt:text-sm swt:text-base-content/60"
                                prop.text "No changed files. Your repository is in sync."
                            ]
                        else
                            Html.div [
                                prop.className "swt:mt-2 swt:flex swt:flex-col swt:gap-2"
                                prop.children [
                                    for change in changedFiles do
                                        let isSelected =
                                            selectedFile
                                            |> Option.exists (fun selected -> String.Equals(selected, change.Path, StringComparison.Ordinal))
                                        let isSelectedForCommit = Set.contains change.Path selectedCommitPaths

                                        Html.button [
                                            prop.testId ("GitSidebarChangeRow-" + change.Path)
                                            prop.key change.Path
                                            prop.className [
                                                "swt:flex swt:w-full swt:flex-col swt:items-start swt:gap-2 swt:rounded-box swt:border swt:px-3 swt:py-2 swt:text-left swt:transition-colors"
                                                if change.IsConflicted then
                                                    "swt:border-error/40 swt:bg-error/5 hover:swt:bg-error/10"
                                                elif isSelected then
                                                    "swt:border-primary/40 swt:bg-primary/5 hover:swt:bg-primary/10"
                                                else
                                                    "swt:border-base-content/10 swt:bg-base-100 hover:swt:bg-base-200/80"
                                            ]
                                            prop.onClick (fun _ -> runAction $"Open {change.Path}" (fun () -> onSelectChange change))
                                            prop.children [
                                                Html.div [
                                                    prop.className "swt:flex swt:w-full swt:items-start swt:gap-3"
                                                    prop.children [
                                                        Html.input [
                                                            prop.testId ("GitSidebarCommitSelectionCheckbox-" + change.Path)
                                                            prop.className "swt:checkbox swt:checkbox-sm swt:mt-0.5 swt:shrink-0"
                                                            prop.type'.checkbox
                                                            prop.disabled (not canEditCommit || change.IsConflicted)
                                                            prop.isChecked isSelectedForCommit
                                                            prop.onClick (fun event -> event.stopPropagation ())
                                                            prop.onChange (fun (_: bool) -> toggleCommitSelection change.Path)
                                                        ]
                                                        Html.span [
                                                            prop.className [
                                                                "swt:mt-0.5 swt:font-mono swt:text-[0.7rem] swt:tracking-[0.2em]"
                                                                if change.IsConflicted then
                                                                    "swt:text-error"
                                                                else
                                                                    "swt:text-base-content/60"
                                                            ]
                                                            prop.text (GitSidebarInternal.describeChange change)
                                                        ]
                                                        Html.div [
                                                            prop.className "swt:min-w-0 swt:flex-1"
                                                            prop.children [
                                                                Html.div [
                                                                    prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                                                    prop.children [
                                                                        Html.span [
                                                                            prop.className "swt:truncate swt:text-sm swt:font-medium"
                                                                            prop.text change.Path
                                                                        ]
                                                                        Html.span [
                                                                            prop.className (GitSidebarInternal.changeBadgeClasses change)
                                                                            prop.text (
                                                                                if change.IsConflicted then
                                                                                    "Conflict"
                                                                                else
                                                                                    "Changed"
                                                                            )
                                                                        ]
                                                                    ]
                                                                ]
                                                                match change.OriginalPath with
                                                                | Some originalPath ->
                                                                    Html.div [
                                                                        prop.className "swt:mt-1 swt:text-xs swt:text-base-content/60"
                                                                        prop.text $"Renamed from {originalPath}"
                                                                    ]
                                                                | None ->
                                                                    Html.none
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                ]
                            ]
                    ]
                ]

                BaseModal.Modal(
                    isOpen = isCreateBranchModalOpen,
                    setIsOpen = setIsCreateBranchModalOpen,
                    header = Html.text "Create Branch From",
                    description = Html.text "Create a new branch and optionally base it on a local or remote ref.",
                    debug = "GitSidebarCreateBranchModal",
                    children =
                        Html.div [
                            prop.testId "GitSidebarCreateBranchModal"
                            prop.className "swt:flex swt:flex-col swt:gap-3"
                            prop.children [
                                Html.label [
                                    prop.className "swt:flex swt:flex-col swt:gap-2"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:text-sm swt:font-medium"
                                            prop.text "Branch name"
                                        ]
                                        Html.input [
                                            prop.testId "GitSidebarBranchNameInput"
                                            prop.className "swt:input swt:input-bordered swt:w-full"
                                            prop.value branchName
                                            prop.placeholder "feature/my-change"
                                            prop.onChange setBranchName
                                        ]
                                    ]
                                ]
                                Html.label [
                                    prop.className "swt:flex swt:flex-col swt:gap-2"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:text-sm swt:font-medium"
                                            prop.text "Start point"
                                        ]
                                        Html.select [
                                            prop.testId "GitSidebarStartPointSelect"
                                            prop.className "swt:select swt:select-bordered swt:w-full"
                                            prop.value (selectedStartPoint |> Option.defaultValue "")
                                            prop.onChange (fun nextValue ->
                                                if String.IsNullOrWhiteSpace nextValue then
                                                    setSelectedStartPoint None
                                                else
                                                    setSelectedStartPoint (Some nextValue)
                                            )
                                            prop.children [
                                                for startPoint, label in branchOptionsWithHead do
                                                    Html.option [
                                                        prop.key (defaultArg startPoint "")
                                                        prop.value (defaultArg startPoint "")
                                                        prop.text label
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ],
                    footer =
                        React.Fragment [
                            Html.button [
                                prop.className "swt:btn swt:btn-ghost"
                                prop.disabled activeAction.IsSome
                                prop.text "Cancel"
                                prop.onClick (fun _ -> setIsCreateBranchModalOpen false)
                            ]
                            Html.button [
                                prop.testId "GitSidebarCreateBranchSubmit"
                                prop.className "swt:btn swt:btn-primary swt:ml-auto"
                                prop.disabled activeAction.IsSome
                                prop.text (
                                    if activeAction = Some "Create Branch From" then
                                        "Creating..."
                                    else
                                        "Create Branch"
                                )
                                prop.onClick (fun _ -> submitCreateBranch ())
                            ]
                        ]
                )

                BaseModal.Modal(
                    isOpen = isSwitchBranchModalOpen,
                    setIsOpen = setIsSwitchBranchModalOpen,
                    header = Html.text "Switch Branch",
                    description = Html.text "Switch to an existing local branch.",
                    debug = "GitSidebarSwitchBranchModal",
                    children =
                        Html.div [
                            prop.testId "GitSidebarSwitchBranchModal"
                            prop.className "swt:flex swt:flex-col swt:gap-3"
                            prop.children [
                                Html.label [
                                    prop.className "swt:flex swt:flex-col swt:gap-2"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:text-sm swt:font-medium"
                                            prop.text "Branch"
                                        ]
                                        Html.select [
                                            prop.testId "GitSidebarSwitchBranchSelect"
                                            prop.className "swt:select swt:select-bordered swt:w-full"
                                            prop.value (selectedSwitchBranch |> Option.defaultValue "")
                                            prop.onChange (fun (nextValue: string) ->
                                                if String.IsNullOrWhiteSpace nextValue then
                                                    setSelectedSwitchBranch None
                                                else
                                                    setSelectedSwitchBranch (Some nextValue)
                                            )
                                            prop.children [
                                                for branch in localBranchOptionsForSwitch do
                                                    Html.option [
                                                        prop.key branch.RefName
                                                        prop.value branch.RefName
                                                        prop.text branch.DisplayLabel
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ],
                    footer =
                        React.Fragment [
                            Html.button [
                                prop.className "swt:btn swt:btn-ghost"
                                prop.disabled activeAction.IsSome
                                prop.text "Cancel"
                                prop.onClick (fun _ -> setIsSwitchBranchModalOpen false)
                            ]
                            Html.button [
                                prop.testId "GitSidebarSwitchBranchSubmit"
                                prop.className "swt:btn swt:btn-primary swt:ml-auto"
                                prop.disabled (activeAction.IsSome || localBranchOptionsForSwitch.Length = 0)
                                prop.text (
                                    if activeAction = Some "Switch Branch" then
                                        "Switching..."
                                    else
                                        "Switch Branch"
                                )
                                prop.onClick (fun _ -> submitSwitchBranch ())
                            ]
                        ]
                )
            ]
        ]
