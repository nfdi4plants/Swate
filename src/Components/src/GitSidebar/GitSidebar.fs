namespace Swate.Components

open System
open Fable.Core
open Feliz

open Swate.Components.GitSidebarTypes

module private GitSidebarInternal =

    type GitChangeKind =
        | Added
        | Modified
        | Deleted
        | Renamed
        | Untracked
        | Conflict

    let hasConflicts (status: GitSidebarStatus) (changedFiles: GitSidebarChange[]) =
        status.IsMergeInProgress || (changedFiles |> Array.exists _.IsConflicted)

    let private rawChangeCode (change: GitSidebarChange) =
        let indexCode = GitStatusCode.normalize change.IndexStatus
        let worktreeCode = GitStatusCode.normalize change.WorkingTreeStatus
        $"{indexCode}{worktreeCode}"

    let describeChange (change: GitSidebarChange) = $"git: {rawChangeCode change}"

    let classifyChange (change: GitSidebarChange) =
        let indexCode = GitStatusCode.normalize change.IndexStatus
        let worktreeCode = GitStatusCode.normalize change.WorkingTreeStatus

        if change.IsConflicted then
            GitChangeKind.Conflict
        elif change.OriginalPath.IsSome || indexCode.StartsWith("R", StringComparison.Ordinal) || worktreeCode.StartsWith("R", StringComparison.Ordinal) then
            GitChangeKind.Renamed
        elif indexCode = "?" || worktreeCode = "?" then
            GitChangeKind.Untracked
        elif indexCode = "D" || worktreeCode = "D" then
            GitChangeKind.Deleted
        elif indexCode = "A" || worktreeCode = "A" then
            GitChangeKind.Added
        else
            GitChangeKind.Modified

    let changePresentation (change: GitSidebarChange) =
        match classifyChange change with
        | GitChangeKind.Added ->
            "Added", "swt:badge swt:badge-success swt:badge-sm", "swt:fluent--add-24-regular"
        | GitChangeKind.Modified ->
            "Modified", "swt:badge swt:badge-info swt:badge-sm", "swt:fluent--edit-24-regular"
        | GitChangeKind.Deleted ->
            "Deleted", "swt:badge swt:badge-error swt:badge-sm", "swt:fluent--delete-24-regular"
        | GitChangeKind.Renamed ->
            "Renamed", "swt:badge swt:badge-warning swt:badge-sm", "swt:fluent--arrow-swap-24-regular"
        | GitChangeKind.Untracked ->
            "Untracked", "swt:badge swt:badge-neutral swt:badge-sm", "swt:fluent--document-24-regular"
        | GitChangeKind.Conflict ->
            "Conflict", "swt:badge swt:badge-error swt:badge-sm", "swt:fluent--warning-24-regular"

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

    let formatThresholdInput (thresholdMb: int) = string thresholdMb

[<RequireQualifiedAccess>]
type private ActiveDialog =
    | None
    | AdvancedActions
    | CreateBranch
    | SwitchBranch

type private BranchHeaderProps = {
    Status: GitSidebarStatus
    HasConflicts: bool
    IsBusy: bool
    OnRefresh: unit -> unit
}

type private LfsSettingsSectionProps = {
    IsBusy: bool
    LfsThresholdInput: string
    SetLfsThresholdInput: string -> unit
    CanSaveLfsThreshold: bool
    SubmitLfsThreshold: unit -> unit
    ActiveAction: string option
}

type private AdvancedActionsProps = {
    DownloadLargeFilesInput: bool
    IsBusy: bool
    RemoteActionsEnabled: bool
    RemoteActionsWarning: string option
    SubmitDownloadLargeFiles: bool -> unit
    SubmitSync: unit -> unit
    IsAdvancedActionsOpen: bool
    ToggleAdvancedActions: unit -> unit
    SubmitFetch: unit -> unit
    SubmitPull: unit -> unit
    SubmitPush: unit -> unit
    OpenCreateBranchModal: unit -> unit
    OpenSwitchBranchModal: unit -> unit
    CanSwitchBranch: bool
    LfsSettings: LfsSettingsSectionProps
}

type private CommitSectionProps = {
    Status: GitSidebarStatus
    HasConflicts: bool
    CanEditCommit: bool
    CommitMessage: string
    SetCommitMessage: string -> unit
    SelectedCommitCount: int
    CanSubmitCommitSelection: bool
    CanSubmitCommitAll: bool
    ActiveAction: string option
    SubmitCommitSelection: unit -> unit
    SubmitCommitAll: unit -> unit
}

type private ChangedFilesListProps = {
    ChangedFiles: GitSidebarChange[]
    SelectedFile: string option
    SelectedCommitPaths: Set<string>
    IsBusy: bool
    CanEditCommit: bool
    ToggleCommitSelection: string -> unit
    OpenChange: GitSidebarChange -> unit
}

type private ModalsProps = {
    IsCreateBranchModalOpen: bool
    SetCreateBranchModalOpen: bool -> unit
    BranchName: string
    SetBranchName: string -> unit
    BranchOptionsWithHead: (string option * string)[]
    SelectedStartPoint: string option
    SetSelectedStartPoint: string option -> unit
    ActiveAction: string option
    SubmitCreateBranch: unit -> unit
    IsSwitchBranchModalOpen: bool
    SetSwitchBranchModalOpen: bool -> unit
    LocalBranchOptionsForSwitch: GitSidebarBranchOption[]
    SelectedSwitchBranch: string option
    SetSelectedSwitchBranch: string option -> unit
    SubmitSwitchBranch: unit -> unit
    CloseDialog: unit -> unit
}

[<Erase; Mangle(false)>]
type GitSidebar =

    [<ReactComponent>]
    static member OperationStatusNotice
        (
            ?runStatus: GitSidebarRunStatus,
            ?errorNotice: string,
            ?warningNotice: string,
            ?busyTestId: string,
            ?errorTestId: string,
            ?warningTestId: string
        ) =
        let runStatus = defaultArg runStatus GitSidebarRunStatus.Idle

        React.Fragment [
            match runStatus with
            | GitSidebarRunStatus.Progress progress ->
                Html.div [
                    prop.className "swt:px-3 swt:pt-3"
                    prop.children [
                        Html.div [
                            if busyTestId.IsSome then
                                prop.testId busyTestId.Value
                            prop.className "swt:alert swt:alert-info swt:px-3 swt:py-2 swt:text-sm"
                            prop.children [
                                Html.span [
                                    prop.className "swt:iconify swt:fluent--arrow-sync-24-regular swt:size-4"
                                ]
                                Html.span [
                                    prop.text (
                                        GitSidebarInternal.progressText progress
                                        |> function
                                            | "" -> "Git operation in progress"
                                            | text -> text
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
            | GitSidebarRunStatus.Busy notice ->
                Html.div [
                    prop.className "swt:px-3 swt:pt-3"
                    prop.children [
                        Html.div [
                            if busyTestId.IsSome then
                                prop.testId busyTestId.Value
                            prop.className "swt:alert swt:alert-info swt:px-3 swt:py-2 swt:text-sm"
                            prop.children [
                                Html.span [
                                    prop.className "swt:iconify swt:fluent--clock-24-regular swt:size-4"
                                ]
                                Html.span notice
                            ]
                        ]
                    ]
                ]
            | GitSidebarRunStatus.Idle -> Html.none

            match errorNotice with
            | Some message ->
                Html.div [
                    prop.className "swt:px-3 swt:pt-3"
                    prop.children [
                        Html.div [
                            if errorTestId.IsSome then
                                prop.testId errorTestId.Value
                            prop.className "swt:alert swt:alert-error swt:px-3 swt:py-2 swt:text-sm"
                            prop.children [
                                Html.span [
                                    prop.className "swt:iconify swt:fluent--warning-24-regular swt:size-4"
                                ]
                                Html.span message
                            ]
                        ]
                    ]
                ]
            | None -> Html.none

            match warningNotice with
            | Some message ->
                Html.div [
                    prop.className "swt:px-3 swt:pt-3"
                    prop.children [
                        Html.div [
                            if warningTestId.IsSome then
                                prop.testId warningTestId.Value
                            prop.className "swt:alert swt:alert-warning swt:px-3 swt:py-2 swt:text-sm"
                            prop.children [
                                Html.span [
                                    prop.className "swt:iconify swt:fluent--warning-shield-24-regular swt:size-4"
                                ]
                                Html.span message
                            ]
                        ]
                    ]
                ]
            | None -> Html.none
        ]

    [<ReactComponent>]
    static member DownloadLargeFilesToggle
        (
            downloadLargeFiles: bool,
            isBusy: bool,
            onChange: bool -> unit,
            ?testId: string,
            ?label: string,
            ?description: string
        ) =
        Html.label [
            prop.className
                "swt:flex swt:items-start swt:gap-3 swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-100 swt:px-3 swt:py-3"
            prop.children [
                Html.input [
                    if testId.IsSome then
                        prop.testId testId.Value
                    prop.className "swt:checkbox swt:checkbox-sm swt:mt-0.5 swt:shrink-0"
                    prop.type'.checkbox
                    prop.disabled isBusy
                    prop.isChecked downloadLargeFiles
                    prop.onChange onChange
                ]
                Html.div [
                    prop.className "swt:flex swt:min-w-0 swt:flex-col"
                    prop.children [
                        Html.span [
                            prop.className "swt:text-sm swt:font-medium"
                            prop.text (defaultArg label "Download Large Files")
                        ]
                        match description with
                        | Some text ->
                            Html.span [
                                prop.className "swt:text-xs swt:text-base-content/70"
                                prop.text text
                            ]
                        | None -> Html.none
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private SectionHeader(title: string, countText: string option) =
        Html.div [
            prop.className
                "swt:flex swt:items-center swt:justify-between swt:gap-2 swt:px-3 swt:pt-3 swt:text-xs swt:font-semibold swt:uppercase swt:tracking-[0.2em] swt:text-base-content/60"
            prop.children [
                Html.span title
                match countText with
                | Some value ->
                    Html.span [
                        prop.className "swt:text-[0.65rem] swt:text-base-content/50"
                        prop.text value
                    ]
                | None -> Html.none
            ]
        ]

    [<ReactComponent>]
    static member private ActionButton
        (label: string, iconClassName: string, isBusy: bool, onClick: unit -> unit, ?isActive: bool, ?testId: string)
        =
        let isActive = defaultArg isActive false

        Html.button [
            if testId.IsSome then
                prop.testId testId.Value
            prop.className [
                "swt:btn swt:btn-sm swt:justify-start swt:gap-2 swt:normal-case"
                if isActive then
                    "swt:btn-primary"
                else
                    "swt:bg-base-100 swt:border-base-300"
            ]
            prop.disabled isBusy
            prop.onClick (fun _ -> onClick ())
            prop.children [
                Html.span [
                    prop.className [ "swt:iconify"; iconClassName; "swt:size-4" ]
                ]
                Html.span label
            ]
        ]

    [<ReactComponent>]
    static member private BranchHeader(props: BranchHeaderProps) =
        React.Fragment [
            Html.div [
                prop.className
                    "swt:flex swt:items-center swt:justify-between swt:border-b swt:border-base-content/10 swt:px-3 swt:py-3"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:items-center swt:gap-2"
                        prop.children [
                            Html.span [
                                prop.className "swt:iconify swt:fluent--branch-fork-24-regular swt:size-5"
                            ]
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
                                            match props.Status.CurrentBranch with
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
                        prop.disabled props.IsBusy
                        prop.title "Refresh git status"
                        prop.onClick (fun _ -> props.OnRefresh())
                        prop.children [
                            Html.span [
                                prop.className "swt:iconify swt:fluent--arrow-clockwise-24-regular swt:size-4"
                            ]
                        ]
                    ]
                ]
            ]
            Html.div [
                prop.className "swt:px-3 swt:pt-3"
                prop.children [
                    Html.div [
                        prop.className
                            "swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-200/60 swt:p-3"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                prop.children [
                                    Html.span [
                                        prop.className "swt:badge swt:badge-primary swt:badge-sm"
                                        prop.text (if props.Status.IsClean then "Clean" else "Changes")
                                    ]

                                    if props.HasConflicts then
                                        Html.span [
                                            prop.className "swt:badge swt:badge-warning swt:badge-sm"
                                            prop.text "Merge in progress"
                                        ]

                                    if props.Status.Ahead > 0 then
                                        Html.span [
                                            prop.className "swt:badge swt:badge-outline swt:badge-sm"
                                            prop.text $"Ahead {props.Status.Ahead}"
                                        ]

                                    if props.Status.Behind > 0 then
                                        Html.span [
                                            prop.className "swt:badge swt:badge-outline swt:badge-sm"
                                            prop.text $"Behind {props.Status.Behind}"
                                        ]
                                ]
                            ]

                            match props.Status.TrackingBranch with
                            | Some trackingBranch ->
                                Html.div [
                                    prop.className
                                        "swt:mt-2 swt:flex swt:items-center swt:gap-2 swt:text-xs swt:text-base-content/70"
                                    prop.children [
                                        Html.span [
                                            prop.className "swt:iconify swt:fluent--arrow-sync-24-regular swt:size-4"
                                        ]
                                        Html.span $"Tracking {trackingBranch}"
                                    ]
                                ]
                            | None ->
                                match props.Status.CurrentBranch with
                                | Some currentBranch ->
                                    Html.div [
                                        prop.className
                                            "swt:mt-2 swt:flex swt:items-center swt:gap-2 swt:text-xs swt:text-base-content/70"
                                        prop.children [
                                            Html.span [
                                                prop.className
                                                    "swt:iconify swt:fluent--branch-request-20-regular swt:size-4"
                                            ]
                                            Html.span
                                                $"No upstream configured yet. Push will publish and track origin/{currentBranch}."
                                        ]
                                    ]
                                | None -> Html.none
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private LfsSettingsSection(props: LfsSettingsSectionProps) =
        Html.div [
            prop.className "swt:mt-3 swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-100 swt:p-3"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2"
                    prop.children [
                        Html.span [
                            prop.className "swt:iconify swt:fluent--document-arrow-right-24-regular swt:size-4"
                        ]
                        Html.span [
                            prop.className "swt:text-sm swt:font-medium"
                            prop.text "Git LFS auto-track threshold"
                        ]
                    ]
                ]
                Html.p [
                    prop.className "swt:mt-2 swt:text-xs swt:text-base-content/70"
                    prop.text
                        "Files larger than this limit are automatically re-staged through Git LFS during save operations."
                ]
                Html.div [
                    prop.className "swt:mt-3 swt:flex swt:flex-wrap swt:items-end swt:gap-2"
                    prop.children [
                        Html.label [
                            prop.className "swt:flex swt:min-w-[10rem] swt:flex-1 swt:flex-col swt:gap-2"
                            prop.children [
                                Html.span [
                                    prop.className "swt:text-xs swt:font-medium swt:text-base-content/70"
                                    prop.text "Threshold (MB)"
                                ]
                                Html.input [
                                    prop.testId "GitSidebarLfsThresholdInput"
                                    prop.className "swt:input swt:input-bordered swt:w-full"
                                    prop.type'.number
                                    prop.custom ("step", "1")
                                    prop.custom ("min", "1")
                                    prop.custom ("max", "100")
                                    prop.disabled props.IsBusy
                                    prop.value props.LfsThresholdInput
                                    prop.onChange props.SetLfsThresholdInput
                                ]
                            ]
                        ]
                        Html.button [
                            prop.testId "GitSidebarLfsThresholdSaveButton"
                            prop.className "swt:btn swt:btn-sm swt:btn-outline swt:gap-2 swt:normal-case"
                            prop.disabled (not props.CanSaveLfsThreshold)
                            prop.onClick (fun _ -> props.SubmitLfsThreshold())
                            prop.children [
                                Html.span [
                                    prop.className "swt:iconify swt:fluent--save-24-regular swt:size-4"
                                ]
                                Html.span (
                                    if props.ActiveAction = Some "Save Git LFS Threshold" then
                                        "Saving..."
                                    else
                                        "Save Threshold"
                                )
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:mt-2 swt:text-xs swt:text-base-content/60"
                    prop.text "Threshold setting: 1-100 MB. This does not cap LFS-tracked file size."
                ]
            ]
        ]

    [<ReactComponent>]
    static member private AdvancedActions(props: AdvancedActionsProps) =
        React.Fragment [
            GitSidebar.SectionHeader("Actions", None)

            Html.div [
                prop.className "swt:grid swt:grid-cols-2 swt:gap-2 swt:px-3"
                prop.children [
                    Html.label [
                        prop.className "swt:col-span-2"
                        prop.children [
                            GitSidebar.DownloadLargeFilesToggle(
                                props.DownloadLargeFilesInput,
                                props.IsBusy,
                                props.SubmitDownloadLargeFiles,
                                testId = "GitSidebarDownloadLargeFilesCheckbox"
                            )
                        ]
                    ]
                    GitSidebar.ActionButton(
                        "Synchronize Changes",
                        "swt:fluent--arrow-sync-24-regular",
                        props.IsBusy || not props.RemoteActionsEnabled,
                        props.SubmitSync,
                        testId = "GitSidebarSyncButton"
                    )
                    GitSidebar.ActionButton(
                        "More Git Actions",
                        (if props.IsAdvancedActionsOpen then
                             "swt:fluent--chevron-up-24-regular"
                         else
                             "swt:fluent--chevron-down-24-regular"),
                        props.IsBusy,
                        props.ToggleAdvancedActions,
                        isActive = props.IsAdvancedActionsOpen,
                        testId = "GitSidebarAdvancedActionsButton"
                    )
                ]
            ]

            match props.RemoteActionsWarning with
            | Some warning when not props.RemoteActionsEnabled ->
                Html.div [
                    prop.className "swt:px-3 swt:pt-3"
                    prop.children [
                        Html.div [
                            prop.testId "GitSidebarRemoteAuthWarning"
                            prop.className "swt:alert swt:alert-warning swt:px-3 swt:py-2 swt:text-sm"
                            prop.children [
                                Html.span [
                                    prop.className "swt:iconify swt:fluent--warning-shield-24-regular swt:size-4"
                                ]
                                Html.span warning
                            ]
                        ]
                    ]
                ]
            | _ -> Html.none

            if props.IsAdvancedActionsOpen then
                Html.div [
                    prop.className "swt:px-3 swt:pt-3"
                    prop.children [
                        Html.div [
                            prop.testId "GitSidebarAdvancedActionsDivider"
                            prop.className "swt:mb-3 swt:border-t swt:border-base-content/10"
                        ]
                        Html.div [
                            prop.className "swt:grid swt:grid-cols-2 swt:gap-2"
                            prop.children [
                                GitSidebar.ActionButton(
                                    "Check for Changes",
                                    "swt:fluent--arrow-download-24-regular",
                                    props.IsBusy || not props.RemoteActionsEnabled,
                                    props.SubmitFetch,
                                    testId = "GitSidebarFetchButton"
                                )
                                GitSidebar.ActionButton(
                                    "Download Changes",
                                    "swt:fluent--arrow-down-24-regular",
                                    props.IsBusy || not props.RemoteActionsEnabled,
                                    props.SubmitPull,
                                    testId = "GitSidebarPullButton"
                                )
                                GitSidebar.ActionButton(
                                    "Upload Changes",
                                    "swt:fluent--arrow-up-24-regular",
                                    props.IsBusy || not props.RemoteActionsEnabled,
                                    props.SubmitPush,
                                    testId = "GitSidebarPushButton"
                                )
                                GitSidebar.ActionButton(
                                    "Create Work Copy",
                                    "swt:fluent--branch-fork-24-regular",
                                    props.IsBusy,
                                    props.OpenCreateBranchModal,
                                    testId = "GitSidebarCreateBranchButton"
                                )
                                GitSidebar.ActionButton(
                                    "Switch To Work Copy",
                                    "swt:fluent--arrow-swap-24-regular",
                                    props.IsBusy || not props.CanSwitchBranch,
                                    props.OpenSwitchBranchModal,
                                    testId = "GitSidebarSwitchBranchButton"
                                )
                            ]
                        ]
                        GitSidebar.LfsSettingsSection(props.LfsSettings)
                    ]
                ]
            else
                Html.none
        ]

    [<ReactComponent>]
    static member private CommitSection(props: CommitSectionProps) =
        React.Fragment [
            GitSidebar.SectionHeader("Save", None)

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
                                        prop.text "Save message"
                                    ]
                                    Html.textarea [
                                        prop.testId "GitSidebarCommitMessageInput"
                                        prop.className
                                            "swt:textarea swt:textarea-bordered swt:min-h-24 swt:w-full swt:resize-y"
                                        prop.disabled (not props.CanEditCommit)
                                        prop.value props.CommitMessage
                                        prop.placeholder (
                                            if props.HasConflicts then
                                                "Resolve merge conflicts before saving."
                                            elif props.Status.IsClean then
                                                "No changes to save."
                                            else
                                                "Describe your changes"
                                        )
                                        prop.onChange props.SetCommitMessage
                                    ]
                                ]
                            ]
                            Html.div [
                                prop.className
                                    "swt:mt-2 swt:flex swt:items-center swt:justify-between swt:gap-3 swt:text-xs swt:text-base-content/60"
                                prop.children [
                                    Html.span (
                                        if props.SelectedCommitCount = 1 then
                                            "1 file selected to save"
                                        else
                                            $"{props.SelectedCommitCount} files selected to save"
                                    )
                                    if not props.CanEditCommit && props.HasConflicts then
                                        Html.span "Saving selected files is disabled while conflicts remain."
                                    elif not props.CanEditCommit && props.Status.IsClean then
                                        Html.span "No changes available to save."
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
                                        prop.disabled (not props.CanSubmitCommitSelection)
                                        prop.onClick (fun _ -> props.SubmitCommitSelection())
                                        prop.children [
                                            Html.span [
                                                prop.className
                                                    "swt:iconify swt:fluent--checkbox-checked-24-regular swt:size-4"
                                            ]
                                            Html.span (
                                                if props.ActiveAction = Some "Commit Selection" then
                                                    "Committing..."
                                                else
                                                    "Save Selected Changes"
                                            )
                                        ]
                                    ]
                                    Html.button [
                                        prop.testId "GitSidebarCommitAllButton"
                                        prop.className "swt:btn swt:btn-sm swt:btn-secondary swt:gap-2 swt:normal-case"
                                        prop.disabled (not props.CanSubmitCommitAll)
                                        prop.onClick (fun _ -> props.SubmitCommitAll())
                                        prop.children [
                                            Html.span [
                                                prop.className
                                                    "swt:iconify swt:fluent--checkmark-circle-24-regular swt:size-4"
                                            ]
                                            Html.span (
                                                if props.ActiveAction = Some "Commit All" then
                                                    "Committing..."
                                                else
                                                    "Save All Changes"
                                            )
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private ChangedFilesList(props: ChangedFilesListProps) =
        let scrollContainerRef = React.useElementRef ()

        let rowVirtualizer =
            Virtual.useVirtualizer (
                count = props.ChangedFiles.Length,
                getScrollElement = (fun () -> scrollContainerRef.current),
                estimateSize = (fun _ -> 96),
                overscan = 2,
                gap = 8
            )

        React.Fragment [
            GitSidebar.SectionHeader(
                "Changes",
                Some(
                    if props.ChangedFiles.Length = 1 then
                        "1 file"
                    else
                        $"{props.ChangedFiles.Length} files"
                )
            )

            Html.div [
                prop.ref scrollContainerRef
                prop.className "swt:min-h-0 swt:flex-1 swt:overflow-y-auto swt:px-3 swt:pb-3"
                prop.children [
                    if props.ChangedFiles.Length = 0 then
                        Html.div [
                            prop.className
                                "swt:mt-2 swt:rounded-box swt:border swt:border-dashed swt:border-base-content/15 swt:bg-base-200/40 swt:px-4 swt:py-6 swt:text-sm swt:text-base-content/60"
                            prop.text "No changed files. Your repository is in sync."
                        ]
                    else
                        let renderChangeRow (isVirtualized: bool) (index: int) (top: int option) =
                                    let change = props.ChangedFiles.[index]
                                    let isSelected =
                                        props.SelectedFile
                                        |> Option.exists (fun selected ->
                                            String.Equals(selected, change.Path, StringComparison.Ordinal)
                                        )

                                    let isSelectedForCommit = Set.contains change.Path props.SelectedCommitPaths
                                    let badgeLabel, badgeClass, iconClass = GitSidebarInternal.changePresentation change

                                    Html.button [
                                        prop.testId $"GitSidebarChangeRow-{index}"
                                        prop.key change.Path
                                        if isVirtualized then
                                            prop.custom ("data-index", index)
                                        prop.disabled props.IsBusy
                                        prop.className [
                                            "swt:flex swt:w-full swt:flex-col swt:items-start swt:gap-2 swt:rounded-box swt:border swt:px-3 swt:py-2 swt:text-left swt:transition-colors"
                                            if isVirtualized then
                                                "swt:absolute swt:left-0"
                                            if change.IsConflicted then
                                                "swt:border-error/40 swt:bg-error/5 hover:swt:bg-error/10"
                                            elif isSelected then
                                                "swt:border-primary/40 swt:bg-primary/5 hover:swt:bg-primary/10"
                                            else
                                                "swt:border-base-content/10 swt:bg-base-100 hover:swt:bg-base-200/80"
                                        ]
                                        if isVirtualized then
                                            prop.style [
                                                style.top 0
                                                style.custom ("transform", $"translateY({top.Value}px)")
                                            ]
                                        if isVirtualized then
                                            prop.ref rowVirtualizer.measureElement
                                        prop.onClick (fun _ -> props.OpenChange change)
                                        prop.children [
                                            Html.div [
                                                prop.className "swt:flex swt:w-full swt:items-start swt:gap-3"
                                                prop.children [
                                                    Html.input [
                                                        prop.testId ("GitSidebarCommitSelectionCheckbox-" + change.Path)
                                                        prop.className
                                                            "swt:checkbox swt:checkbox-sm swt:mt-0.5 swt:shrink-0"
                                                        prop.type'.checkbox
                                                        prop.disabled (not props.CanEditCommit || change.IsConflicted)
                                                        prop.isChecked isSelectedForCommit
                                                        prop.onClick (fun event -> event.stopPropagation ())
                                                        prop.onChange (fun (_: bool) ->
                                                            props.ToggleCommitSelection change.Path
                                                        )
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
                                                                prop.className
                                                                    "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                                                                prop.children [
                                                                    Html.span [
                                                                        prop.className
                                                                            "swt:truncate swt:text-sm swt:font-medium"
                                                                        prop.text change.Path
                                                                    ]
                                                                    Html.span [
                                                                        prop.className badgeClass
                                                                        prop.children [
                                                                            Html.span [
                                                                                prop.className
                                                                                    $"swt:iconify {iconClass} swt:size-3.5"
                                                                            ]
                                                                            Html.span [
                                                                                prop.text badgeLabel
                                                                            ]
                                                                        ]
                                                                    ]
                                                                ]
                                                            ]
                                                            match change.OriginalPath with
                                                            | Some originalPath ->
                                                                Html.div [
                                                                    prop.className
                                                                        "swt:mt-1 swt:text-xs swt:text-base-content/60"
                                                                    prop.text $"Renamed from {originalPath}"
                                                                ]
                                                            | None -> Html.none
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]

                        let renderRows () =
                            Html.div [
                                prop.className "swt:mt-2 swt:flex swt:flex-col swt:gap-2"
                                prop.children (
                                    props.ChangedFiles
                                    |> Array.mapi (fun index _ -> renderChangeRow false index None)
                                )
                            ]

                        if props.ChangedFiles.Length <= 24 then
                            renderRows ()
                        else
                            Html.div [
                                prop.className "swt:mt-2 swt:relative"
                                prop.style [ style.height (rowVirtualizer.getTotalSize ()) ]
                                prop.children (
                                    rowVirtualizer.getVirtualItems ()
                                    |> Array.map (fun virtualRow ->
                                        renderChangeRow true virtualRow.index (Some virtualRow.start)
                                    )
                                )
                            ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private Modals(props: ModalsProps) =
        React.Fragment [
            BaseModal.Modal(
                isOpen = props.IsCreateBranchModalOpen,
                setIsOpen = props.SetCreateBranchModalOpen,
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
                                        prop.value props.BranchName
                                        prop.placeholder "feature/my-change"
                                        prop.onChange props.SetBranchName
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
                                        prop.value (props.SelectedStartPoint |> Option.defaultValue "")
                                        prop.onChange (fun nextValue ->
                                            if String.IsNullOrWhiteSpace nextValue then
                                                props.SetSelectedStartPoint None
                                            else
                                                props.SetSelectedStartPoint(Some nextValue)
                                        )
                                        prop.children [
                                            for startPoint, label in props.BranchOptionsWithHead do
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
                            prop.disabled props.ActiveAction.IsSome
                            prop.text "Cancel"
                            prop.onClick (fun _ -> props.CloseDialog())
                        ]
                        Html.button [
                            prop.testId "GitSidebarCreateBranchSubmit"
                            prop.className "swt:btn swt:btn-primary swt:ml-auto"
                            prop.disabled props.ActiveAction.IsSome
                            prop.text (
                                if props.ActiveAction = Some "Create Branch From" then
                                    "Creating..."
                                else
                                    "Create Branch"
                            )
                            prop.onClick (fun _ -> props.SubmitCreateBranch())
                        ]
                    ]
            )

            BaseModal.Modal(
                isOpen = props.IsSwitchBranchModalOpen,
                setIsOpen = props.SetSwitchBranchModalOpen,
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
                                        prop.value (props.SelectedSwitchBranch |> Option.defaultValue "")
                                        prop.onChange (fun (nextValue: string) ->
                                            if String.IsNullOrWhiteSpace nextValue then
                                                props.SetSelectedSwitchBranch None
                                            else
                                                props.SetSelectedSwitchBranch(Some nextValue)
                                        )
                                        prop.children [
                                            for branch in props.LocalBranchOptionsForSwitch do
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
                            prop.disabled props.ActiveAction.IsSome
                            prop.text "Cancel"
                            prop.onClick (fun _ -> props.CloseDialog())
                        ]
                        Html.button [
                            prop.testId "GitSidebarSwitchBranchSubmit"
                            prop.className "swt:btn swt:btn-primary swt:ml-auto"
                            prop.disabled (props.ActiveAction.IsSome || props.LocalBranchOptionsForSwitch.Length = 0)
                            prop.text (
                                if props.ActiveAction = Some "Switch Branch" then
                                    "Switching..."
                                else
                                    "Switch Branch"
                            )
                            prop.onClick (fun _ -> props.SubmitSwitchBranch())
                        ]
                    ]
            )
        ]

    [<ReactComponent>]
    static member Main
        (
            status: GitSidebarStatus,
            changedFiles: GitSidebarChange[],
            branchOptions: GitSidebarBranchOption[],
            callbacks: GitSidebarCallbacks,
            downloadLargeFiles: bool,
            lfsAutoTrackThresholdMb: int,
            ?runStatus: GitSidebarRunStatus,
            ?selectedFile: string,
            ?errorNotice: string,
            ?warningNotice: string,
            ?remoteActionsEnabled: bool,
            ?remoteActionsWarning: string
        ) =

        let runStatus = defaultArg runStatus GitSidebarRunStatus.Idle
        let errorNotice = errorNotice
        let warningNotice = warningNotice
        let remoteActionsEnabled = defaultArg remoteActionsEnabled true
        let remoteActionsWarning = remoteActionsWarning
        let selectedFile = selectedFile
        let onRefresh = callbacks.OnRefresh
        let onFetch = callbacks.OnFetch
        let onPull = callbacks.OnPull
        let onPush = callbacks.OnPush
        let onSync = callbacks.OnSync
        let onCommitSelection = callbacks.OnCommitSelection
        let onCommitAll = callbacks.OnCommitAll
        let onSaveDownloadLargeFiles = callbacks.OnSaveDownloadLargeFiles
        let onSaveLfsAutoTrackThreshold = callbacks.OnSaveLfsAutoTrackThreshold
        let onCreateBranch = callbacks.OnCreateBranch
        let onSwitchBranch = callbacks.OnSwitchBranch
        let onSelectChange = callbacks.OnSelectChange

        let localError, setLocalError = React.useState (None: string option)
        let activeAction, setActiveAction = React.useState (None: string option)
        let activeDialog, setActiveDialog = React.useState ActiveDialog.None
        let branchName, setBranchName = React.useState ""
        let commitMessage, setCommitMessage = React.useState ""

        let downloadLargeFilesInput, setDownloadLargeFilesInput =
            React.useState downloadLargeFiles

        let lfsThresholdInput, setLfsThresholdInput =
            React.useState (GitSidebarInternal.formatThresholdInput lfsAutoTrackThresholdMb)

        let selectedCommitPaths, setSelectedCommitPaths =
            React.useStateWithUpdater Set.empty<string>

        let selectedStartPoint, setSelectedStartPoint = React.useState (None: string option)

        let selectedSwitchBranch, setSelectedSwitchBranch =
            React.useState (None: string option)

        let isAdvancedActionsOpen = activeDialog = ActiveDialog.AdvancedActions
        let isCreateBranchModalOpen = activeDialog = ActiveDialog.CreateBranch
        let isSwitchBranchModalOpen = activeDialog = ActiveDialog.SwitchBranch

        let setCreateBranchModalOpen isOpen =
            setActiveDialog (
                if isOpen then
                    ActiveDialog.CreateBranch
                else
                    ActiveDialog.None
            )

        let setSwitchBranchModalOpen isOpen =
            setActiveDialog (
                if isOpen then
                    ActiveDialog.SwitchBranch
                else
                    ActiveDialog.None
            )

        let isBusy =
            activeAction.IsSome
            || match runStatus with
               | GitSidebarRunStatus.Idle -> false
               | GitSidebarRunStatus.Busy _
               | GitSidebarRunStatus.Progress _ -> true

        let visibleError = errorNotice |> Option.orElse localError

        // Sync selectedCommitPaths with changedFiles: remove paths that are no longer in the changed
        // files list (e.g., after a commit or discard operation updates the server-side file list).
        React.useEffect (
            (fun () ->
                setSelectedCommitPaths (fun current ->
                    current
                    |> Set.filter (fun path ->
                        changedFiles
                        |> Array.exists (fun change -> String.Equals(change.Path, path, StringComparison.Ordinal))
                    )
                )
            ),
            [| box changedFiles |]
        )

        // Reset the local downloadLargeFiles toggle when the prop changes
        // (e.g., after the server confirms or rejects a preference update).
        React.useEffect ((fun () -> setDownloadLargeFilesInput downloadLargeFiles), [| box downloadLargeFiles |])

        // Reset the local LFS threshold input when the prop changes
        // (same sync pattern as downloadLargeFiles above).
        React.useEffect (
            (fun () -> setLfsThresholdInput (GitSidebarInternal.formatThresholdInput lfsAutoTrackThresholdMb)),
            [| box lfsAutoTrackThresholdMb |]
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
            |> Array.filter (fun branch -> branch.Kind = GitSidebarBranchKind.Local && not branch.IsCurrent)

        let runSelectChangeAction (change: GitSidebarChange) =
            promise {
                setLocalError None
                setActiveAction (Some $"Open {change.Path}")

                try
                    let! result = onSelectChange change

                    match result with
                    | Ok() -> ()
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
            setActiveDialog ActiveDialog.CreateBranch

        let openSwitchBranchModal () =
            setLocalError None

            let defaultBranch =
                localBranchOptionsForSwitch |> Array.tryHead |> Option.map _.RefName

            setSelectedSwitchBranch defaultBranch
            setActiveDialog ActiveDialog.SwitchBranch

        let toggleCommitSelection (path: string) =
            setSelectedCommitPaths (fun current ->
                if Set.contains path current then
                    Set.remove path current
                else
                    Set.add path current
            )

        let submitCommitSelection () =
            let normalizedCommitMessage = commitMessage.Trim()
            let selectedPaths = selectedCommitPaths |> Set.toArray |> Array.sort

            if String.IsNullOrWhiteSpace normalizedCommitMessage then
                setLocalError (Some "Save message must not be empty.")
            elif selectedPaths.Length = 0 then
                setLocalError (Some "Select at least one file to save.")
            else
                setLocalError None

                onCommitSelection {
                    Message = normalizedCommitMessage
                    Paths = selectedPaths
                }

                setCommitMessage ""
                setSelectedCommitPaths (fun _ -> Set.empty)

        let submitCommitAll () =
            let normalizedCommitMessage = commitMessage.Trim()

            if String.IsNullOrWhiteSpace normalizedCommitMessage then
                setLocalError (Some "Save message must not be empty.")
            else
                setLocalError None
                onCommitAll normalizedCommitMessage
                setCommitMessage ""

        let submitLfsThreshold () =
            let normalizedInput = lfsThresholdInput.Trim()

            let success, parsedThresholdMb = Int32.TryParse normalizedInput

            if not success then
                setLocalError (Some "Git LFS threshold must be a whole number.")
            elif parsedThresholdMb < 1 then
                setLocalError (Some "Git LFS threshold must be at least 1 MB.")
            elif parsedThresholdMb > 100 then
                setLocalError (Some "Git LFS threshold must not exceed 100 MB.")
            else
                setLocalError None
                onSaveLfsAutoTrackThreshold parsedThresholdMb
                setLfsThresholdInput (GitSidebarInternal.formatThresholdInput parsedThresholdMb)

        let submitDownloadLargeFiles (nextValue: bool) =
            if nextValue = downloadLargeFilesInput then
                ()
            else
                setLocalError None
                setDownloadLargeFilesInput nextValue
                onSaveDownloadLargeFiles nextValue

        let submitCreateBranch () =
            let normalizedBranchName = branchName.Trim()

            if String.IsNullOrWhiteSpace normalizedBranchName then
                setLocalError (Some "Branch name must not be empty.")
            else
                setLocalError None

                onCreateBranch {
                    BranchName = normalizedBranchName
                    StartPoint = selectedStartPoint
                }

                setActiveDialog ActiveDialog.None
                setBranchName ""

        let submitSwitchBranch () =
            match selectedSwitchBranch with
            | None -> setLocalError (Some "Select a branch to switch to.")
            | Some branchName ->
                setLocalError None
                onSwitchBranch branchName
                setActiveDialog ActiveDialog.None

        let hasConflicts = GitSidebarInternal.hasConflicts status changedFiles
        let canEditCommit = not status.IsClean && not hasConflicts && not isBusy
        let selectedCommitCount = Set.count selectedCommitPaths

        let canSubmitCommitSelection =
            canEditCommit
            && selectedCommitCount > 0
            && not (String.IsNullOrWhiteSpace(commitMessage.Trim()))

        let canSubmitCommitAll =
            canEditCommit && not (String.IsNullOrWhiteSpace(commitMessage.Trim()))

        let normalizedLfsThresholdInput = lfsThresholdInput.Trim()

        let canSaveLfsThreshold =
            not isBusy
            && not (String.IsNullOrWhiteSpace normalizedLfsThresholdInput)
            && not (
                String.Equals(
                    normalizedLfsThresholdInput,
                    GitSidebarInternal.formatThresholdInput lfsAutoTrackThresholdMb,
                    StringComparison.Ordinal
                )
            )

        Html.div [
            prop.testId "GitSidebar"
            prop.className "swt:flex swt:h-full swt:min-h-0 swt:flex-col swt:bg-base-100"
            prop.children [
                GitSidebar.BranchHeader(
                    {
                        Status = status
                        HasConflicts = hasConflicts
                        IsBusy = isBusy
                        OnRefresh =
                            fun () ->
                                setLocalError None
                                onRefresh ()
                    }
                )

                GitSidebar.AdvancedActions(
                    {
                        DownloadLargeFilesInput = downloadLargeFilesInput
                        IsBusy = isBusy
                        RemoteActionsEnabled = remoteActionsEnabled
                        RemoteActionsWarning = remoteActionsWarning
                        SubmitDownloadLargeFiles = submitDownloadLargeFiles
                        SubmitSync =
                            fun () ->
                                setLocalError None
                                onSync ()
                        IsAdvancedActionsOpen = isAdvancedActionsOpen
                        ToggleAdvancedActions =
                            fun () ->
                                setActiveDialog (
                                    if isAdvancedActionsOpen then
                                        ActiveDialog.None
                                    else
                                        ActiveDialog.AdvancedActions
                                )
                        SubmitFetch =
                            fun () ->
                                setLocalError None
                                onFetch ()
                        SubmitPull =
                            fun () ->
                                setLocalError None
                                onPull ()
                        SubmitPush =
                            fun () ->
                                setLocalError None
                                onPush ()
                        OpenCreateBranchModal = openCreateBranchModal
                        OpenSwitchBranchModal = openSwitchBranchModal
                        CanSwitchBranch = localBranchOptionsForSwitch.Length > 0
                        LfsSettings = {
                            IsBusy = isBusy
                            LfsThresholdInput = lfsThresholdInput
                            SetLfsThresholdInput = setLfsThresholdInput
                            CanSaveLfsThreshold = canSaveLfsThreshold
                            SubmitLfsThreshold = submitLfsThreshold
                            ActiveAction = activeAction
                        }
                    }
                )

                GitSidebar.CommitSection(
                    {
                        Status = status
                        HasConflicts = hasConflicts
                        CanEditCommit = canEditCommit
                        CommitMessage = commitMessage
                        SetCommitMessage = setCommitMessage
                        SelectedCommitCount = selectedCommitCount
                        CanSubmitCommitSelection = canSubmitCommitSelection
                        CanSubmitCommitAll = canSubmitCommitAll
                        ActiveAction = activeAction
                        SubmitCommitSelection = submitCommitSelection
                        SubmitCommitAll = submitCommitAll
                    }
                )

                GitSidebar.OperationStatusNotice(
                    ?runStatus = Some runStatus,
                    ?errorNotice = visibleError,
                    ?warningNotice = warningNotice,
                    busyTestId = "GitSidebarProgressNotice",
                    errorTestId = "GitSidebarErrorNotice"
                )

                if hasConflicts then
                    Html.div [
                        prop.className "swt:px-3 swt:pt-3"
                        prop.children [
                            Html.div [
                                prop.testId "GitSidebarMergeBanner"
                                prop.className "swt:alert swt:alert-warning swt:px-3 swt:py-2 swt:text-sm"
                                prop.children [
                                    Html.span [
                                        prop.className "swt:iconify swt:fluent--warning-shield-24-regular swt:size-4"
                                    ]
                                    Html.span "Resolve all conflicted files before pushing."
                                ]
                            ]
                        ]
                    ]

                GitSidebar.ChangedFilesList(
                    {
                        ChangedFiles = changedFiles
                        SelectedFile = selectedFile
                        SelectedCommitPaths = selectedCommitPaths
                        IsBusy = isBusy
                        CanEditCommit = canEditCommit
                        ToggleCommitSelection = toggleCommitSelection
                        OpenChange = fun change -> runSelectChangeAction change
                    }
                )

                GitSidebar.Modals(
                    {
                        IsCreateBranchModalOpen = isCreateBranchModalOpen
                        SetCreateBranchModalOpen = setCreateBranchModalOpen
                        BranchName = branchName
                        SetBranchName = setBranchName
                        BranchOptionsWithHead = branchOptionsWithHead
                        SelectedStartPoint = selectedStartPoint
                        SetSelectedStartPoint = setSelectedStartPoint
                        ActiveAction = activeAction
                        SubmitCreateBranch = submitCreateBranch
                        IsSwitchBranchModalOpen = isSwitchBranchModalOpen
                        SetSwitchBranchModalOpen = setSwitchBranchModalOpen
                        LocalBranchOptionsForSwitch = localBranchOptionsForSwitch
                        SelectedSwitchBranch = selectedSwitchBranch
                        SetSelectedSwitchBranch = setSelectedSwitchBranch
                        SubmitSwitchBranch = submitSwitchBranch
                        CloseDialog = fun () -> setActiveDialog ActiveDialog.None
                    }
                )
            ]
        ]
