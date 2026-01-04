#r "nuget: Fable.Core"

open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop

[<StringEnum>]
type ApplyOptions =
    | [<CompiledName("--stat")>] Stat
    | [<CompiledName("--numstat")>] NumStat
    | [<CompiledName("--summary")>] Summary
    | [<CompiledName("--check")>] Check
    | [<CompiledName("--index")>] Index
    | [<CompiledName("--cached")>] Cached
    | [<CompiledName("--intent-to-add")>] IntentToAdd
    | [<CompiledName("--3way")>] Way
    | [<CompiledName("--ours")>] Ours
    | [<CompiledName("--theirs")>] Theirs
    | [<CompiledName("--union")>] Union
    | [<CompiledName("--reverse")>] Reverse
    | [<CompiledName("--reject")>] Reject
    | [<CompiledName("--z")>] Z
    | [<CompiledName("--unidiff-zero")>] UnidiffZero
    | [<CompiledName("--apply")>] Apply
    | [<CompiledName("--no-add")>] NoAdd
    | [<CompiledName("--allow-binary-replacement")>] AllowBinaryReplacement
    | [<CompiledName("--binary")>] Binary
    | [<CompiledName("--ignore-space-change")>] IgnoreSpaceChange
    | [<CompiledName("--ignore-whitespace")>] IgnoreWhitespace
    | [<CompiledName("--inaccurate-eof")>] InaccurateEof
    | [<CompiledName("--verbose")>] Verbose
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("--recount")>] Recount
    | [<CompiledName("--unsafe-paths")>] UnsafePaths
    | [<CompiledName("--allow-empty")>] AllowEmpty

[<StringEnum>]
type ApplyOptionsWithValues =
    | [<CompiledName("--build-fake-ancestor")>] BuildFakeAncestor
    | [<CompiledName("--p")>] P
    | [<CompiledName("--n")>] N
    | [<CompiledName("--exclude")>] Exclude
    | [<CompiledName("--include")>] Include
    | [<CompiledName("--directory")>] Directory

let applyFlagWithValue (flag: ApplyOptionsWithValues) (value: string) = $"{flag}={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type WhiteSpaceActions =
    | Nowarn
    | Warn
    | Fix
    | Error
    | [<CompiledName("error-all")>] ErrorAll

let whiteSpaceWithActions (value: WhiteSpaceActions) = $"--whitespace={value}"

[<StringEnum>]
type BranchOptions =
    | [<CompiledName("--delete")>] Delete
    | [<CompiledName("-D")>] DeleteForce
    | [<CompiledName("--create-reflog")>] CreateReflog
    | [<CompiledName("--force")>] Force
    | [<CompiledName("--move")>] Move
    | [<CompiledName("-M")>] MoveForce
    | [<CompiledName("--copy")>] Copy
    | [<CompiledName("-Copy")>] CopyForce
    | [<CompiledName("--color")>] Colour
    | [<CompiledName("--no-color")>] NoColour
    | [<CompiledName("--ignore-case")>] IgnoreCase
    | [<CompiledName("--omit-empty")>] OmitEmpty
    | [<CompiledName("--column")>] Column
    | [<CompiledName("--no-column")>] NoColumn
    | [<CompiledName("--remotes")>] Remotes
    | [<CompiledName("--all")>] All
    | [<CompiledName("--list")>] List
    | [<CompiledName("--show-current")>] ShowCurrent
    | [<CompiledName("--verbose")>] Verbose
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("--no-abbrev")>] NoAbbrev
    | [<CompiledName("--no-track")>] NoTrack
    | [<CompiledName("--recurse-submodules")>] RecurseSubmodules
    | [<CompiledName("--set-upstream")>] SetUpstream
    | [<CompiledName("--unset-upstream")>] UnsetUpstream
    | [<CompiledName("--edit-description ")>] EditDescription

[<StringEnum>]
type BranchOptionsWithStrings =
    | [<CompiledName("--column")>] Column
    | [<CompiledName("--no-column")>] NoColumn
    | [<CompiledName("--sort")>] Sort
    | [<CompiledName("--abbrev")>] Abbrev
    | [<CompiledName("--set-upstream-to")>] SetUpstreamTo
    | [<CompiledName("--contains")>] Contains
    | [<CompiledName("--no-contains")>] NoContains
    | [<CompiledName("--merged")>] Merged
    | [<CompiledName("--no-merged")>] NoMerged
    | [<CompiledName("--points-at")>] PointsAt
    | [<CompiledName("--format")>] Format

let branchWithValue (flag: BranchOptionsWithStrings) (value: string) = $"{flag}={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type WhenColour =
    | Always
    | Never
    | Auto

let createColourWithValue (value: WhenColour) = $"--color={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type TrackOptions =
    | Direct
    | Inherit

let trackWithValue (value: TrackOptions) = $"--track={value}"

[<StringEnum>]
type CheckoutOptions =
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("--force")>] Force
    | [<CompiledName("--ours")>] Ours
    | [<CompiledName("--theirs")>] Theirs
    | [<CompiledName("--no-track")>] NoTrack
    | [<CompiledName("--guess")>] Guess
    | [<CompiledName("--no-guess")>] NoGuess
    | [<CompiledName("--l")>] L
    | [<CompiledName("--detach")>] Detach
    | [<CompiledName("--ignore-skip-worktree-bits")>] IgnoreSkipWorktreeBits
    | [<CompiledName("--merge")>] Merge
    | [<CompiledName("--patch")>] Patch
    | [<CompiledName("--ignore-other-worktrees")>] IgnoreOtherWorktrees
    | [<CompiledName("--overwrite-ignore")>] OverwriteIgnore
    | [<CompiledName("--no-overwrite-ignore")>] NoOverwriteIgnore
    | [<CompiledName("--recurse-submodules")>] RecurseSubmodules
    | [<CompiledName("--no-recurse-submodules")>] NoRecurseSubmodules
    | [<CompiledName("--overlay")>] Overlay
    | [<CompiledName("--no-overlay")>] NoOverlay
    | [<CompiledName("--pathspec-file-nul")>] PathspecFileNul
    | [<CompiledName("--single-branch")>] SingleBranch
    | [<CompiledName("--no-single-branch")>] NoSingleBranch
    | [<CompiledName("--tags")>] GitTags
    | [<CompiledName("--no-tags")>] NoTags
    | [<CompiledName("--shallow-submodules")>] ShallowSubmodules
    | [<CompiledName("--no-shallow-submodules")>] NoShallowSubmodules
    | [<CompiledName("--remote-submodules")>] RemoteSubmodules
    | [<CompiledName("--no-remote-submodules")>] NoRemoteSubmodules

[<StringEnum>]
type CheckoutOptionsWithNumber =
    | [<CompiledName("--unified")>] Unified
    | [<CompiledName("--inter-hunk-context")>] InterHunkContext

let createCheckoutOptionsWithNumber (tag: CheckoutOptionsWithNumber) (n: int) = $"{tag}={n}"

[<StringEnum>]
type CheckoutOptionsWithString = | [<CompiledName("--pathspec-from-file=")>] PathspecFromFile

let createCheckoutOptionsWithValues (tag: CheckoutOptionsWithString) (value: string) = $"{tag}={value}"

[<StringEnum>]
type CheckoutWhat =
    | [<CompiledName("-b")>] NewBranch
    | [<CompiledName("-B")>] SelectBranch

let checkOutBranchWhat (tag: CheckoutWhat) (branchName: string) (startPoint: string option) =
    if startPoint.IsSome then
        $"{tag} {branchName} {startPoint}"
    else
        $"{tag} {branchName}"

[<StringEnum>]
type CheckoutBranchOptions =
    | [<CompiledName("-b")>] NewBranch
    | [<CompiledName("-B")>] SelectBranch
    | [<CompiledName("--orphan")>] Orphan

let checkOutBranchOptions (tag: CheckoutBranchOptions) (branchName: string) (startPoint: string option) =
    if startPoint.IsSome then
        $"{tag} {branchName} {startPoint}"
    else
        $"{tag} {branchName}"

[<StringEnum(CaseRules.LowerFirst)>]
type ConflictOptions =
    | Merge
    | [<CompiledName("diff3")>] Diff
    | [<CompiledName("zdiff3")>] Zdiff

let createCheckoutConflict (value: ConflictOptions) = $"--conflict={value}"

[<StringEnum>]
type CleanOptions =
    | [<CompiledName("-d")>] D
    | [<CompiledName("-f")>] F
    | [<CompiledName("--force")>] Force
    | [<CompiledName("-i")>] I
    | [<CompiledName("--interactive")>] Interactive
    | [<CompiledName("-n")>] N
    | [<CompiledName("--dry-run")>] DryRun
    | [<CompiledName("-q")>] Q
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("-x")>] Ignore
    | [<CompiledName("-X")>] X

let cleanEPattern (pattern: string) = $"-e {pattern}"

let cleanExcludePattern (pattern: string) = $"--exclude={pattern}"

[<StringEnum>]
type CloneOptions =
    | [<CompiledName("--local")>] Local
    | [<CompiledName("--no-hardlinks")>] NoHardlinks
    | [<CompiledName("--dissociate")>] Dissociate
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("--verbose")>] Verbose
    | [<CompiledName("--progress")>] Progress
    | [<CompiledName("--no-checkout")>] NoCheckout
    | [<CompiledName("--reject-shallow")>] RejectShallow
    | [<CompiledName("--no-reject-shallow")>] NoRejectShallow
    | [<CompiledName("--bare")>] Bare
    | [<CompiledName("--sparse")>] Sparse
    | [<CompiledName("--also-filter-submodules")>] AlsoFilterSubmodules
    | [<CompiledName("--mirror")>] Mirror

[<StringEnum>]
type CloneOptionsWithStrings =
    | [<CompiledName("--reference")>] Reference
    | [<CompiledName("--reference-if-able")>] ReferenceIfAble
    | [<CompiledName("--server-option")>] ServerOption
    | [<CompiledName("--filter")>] Filter
    | [<CompiledName("--origin")>] Origin
    | [<CompiledName("--branch")>] Branch
    | [<CompiledName("--revision")>] Revision
    | [<CompiledName("--upload-pack")>] UploadPack
    | [<CompiledName("--template")>] Template
    //| [<CompiledName("--config")>] Config
    | [<CompiledName("--shallow-since")>] ShallowSince
    | [<CompiledName("--shallow-exclude")>] ShallowExclude
    | [<CompiledName("--recurse-submodules")>] RecurseSubmodules
    | [<CompiledName("--separate-git-dir")>] SeparateGitDir
    | [<CompiledName("--ref-format")>] RefFormat
    | [<CompiledName("--bundle-uri")>] BundleUri

let cloneOptionWithValue (tag: CloneOptionsWithStrings) (value: string) = $"{tag}={value}"

[<StringEnum>]
type CloneOptionsWithNumbers =
    | [<CompiledName("--depth")>] Depth
    | [<CompiledName("--jobs")>] Jobs

let cloneOptionWithNumber (tag: CloneOptionsWithNumbers) (n: int) = $"{tag} {n}"

[<StringEnum>]
type DiffOptions =
    | [<CompiledName("--cached")>] Cached
    | [<CompiledName("--merge-base")>] MergeBase
    | [<CompiledName("--no-index")>] NoIndex
    | [<CompiledName("--name-only")>] NameOnly
    | [<CompiledName("--stat")>] Stat
    | [<CompiledName("--color")>] Colour
    | [<CompiledName("--patch")>] Patch
    | [<CompiledName("--no-patch")>] NoPatch
    | [<CompiledName("--raw")>] Raw
    | [<CompiledName("--minimal")>] Minimal
    | [<CompiledName("--patience")>] Patience
    | [<CompiledName("--histogram")>] Histogram

[<StringEnum(CaseRules.LowerFirst)>]
type DiffAlgorithmOptions =
    | Default
    | Myers
    | Minimal
    | Patience
    | Histogram

let diffAlgorithm (value: DiffAlgorithmOptions) = $"--diff-algorithm={value}"

[<StringEnum>]
type InitOptions =
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("--bare")>] Bare
    | [<CompiledName("--shared")>] Shared

[<StringEnum>]
type InitOptionsWithStrings =
    | [<CompiledName("--object-format")>] ObjectFormat
    | [<CompiledName("--ref-format")>] RefFormat
    | [<CompiledName("--template")>] Template
    | [<CompiledName("--separate-git-dir")>] SeparateGitDir
    | [<CompiledName("--initial-branch")>] InitialBranch

let initFlagWithValue (flag: InitOptionsWithStrings) (value: string) = $"{flag}={value}"

[<StringEnum>]
type MergeOptions =
    | [<CompiledName("--edit")>] Edit
    | [<CompiledName("--no-edit")>] NoEdit
    | [<CompiledName("--ff")>] FF
    | [<CompiledName("--no-ff")>] NoFF
    | [<CompiledName("--ff-only")>] FFOnly
    | [<CompiledName("--no-gpg-sign")>] NoGpgSign
    | [<CompiledName("--no-log")>] NoLog
    | [<CompiledName("--signoff")>] SignOff
    | [<CompiledName("--no-signoff")>] NoSignOff
    | [<CompiledName("--stat")>] Stat
    | [<CompiledName("-n")>] N
    | [<CompiledName("--no-stat")>] NoStat
    | [<CompiledName("--compact-summary")>] CompactSummary
    | [<CompiledName("--squash")>] Squash
    | [<CompiledName("--no-squash")>] NoSquash
    | [<CompiledName("--verify")>] Verify
    | [<CompiledName("--no-verify")>] NoVerify
    | [<CompiledName("--verify-signatures")>] VerifySignatures
    | [<CompiledName("--no-verify-signatures")>] NoVerifySignatures
    | [<CompiledName("--summary")>] Summary
    | [<CompiledName("--no-Summary")>] NoSummary
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("--verbose")>] Verbose
    | [<CompiledName("--progress")>] Progress
    | [<CompiledName("--no-progress")>] NoProgress
    | [<CompiledName("--autostash")>] Autostash
    | [<CompiledName("--no-autostash")>] NoAutostash
    | [<CompiledName("--allow-unrelated-histories")>] AllowUnrelatedHistories
    | [<CompiledName("--rerere-autoupdate")>] RerereAutoupdate
    | [<CompiledName("--no-rerere-autoupdate")>] NoRerereAutoupdate
    | [<CompiledName("--overwrite-ignore")>] OverwriteIgnore
    | [<CompiledName("--no-overwrite-ignore")>] NoOverwriteIgnore
    | [<CompiledName("--abort")>] Abort
    | [<CompiledName("--quit")>] Quit
    | [<CompiledName("--continue")>] Continue

[<StringEnum>]
type MergeOptionsWithStrings =
    | [<CompiledName("--cleanup")>] Cleanup
    | [<CompiledName("--s")>] S
    | [<CompiledName("--gpg-sign")>] GpgSign
    | [<CompiledName("--log")>] Log
    | [<CompiledName("--strategy")>] Strategy
    | [<CompiledName("--strategy-option")>] StrategyOption
    | [<CompiledName("--m")>] M
    | [<CompiledName("--into-name")>] IntoName
    | [<CompiledName("--file")>] File

let mergeWithValue (flag: MergeOptionsWithStrings) (value: string) = $"{flag}={value}"

[<StringEnum>]
type MoveOptions =
    | [<CompiledName("--force")>] Force
    | [<CompiledName("-k")>] K
    | [<CompiledName("--dry-run")>] DryRun
    | [<CompiledName("--verbose")>] Verbose

[<StringEnum>]
type PullOptionsWithoutValues =
    | [<CompiledName("--quiet")>] Force
    | [<CompiledName("--verbose")>] Verbose
    | [<CompiledName("--no-recurse-submodules")>] NoRecurseSubmodules

[<StringEnum(CaseRules.LowerFirst)>]
type PullOptionRecurseValues =
    | Yes
    | No
    | [<CompiledName("on-demand")>] OnDemand

let createPullOptionRecurseSubmodul (value: PullOptionRecurseValues) = $"--recurse-submodules={value}"

[<StringEnum>]
type PullOptionsMerging =
    | [<CompiledName("--commit")>] Commit
    | [<CompiledName("--no-commit")>] NoCommit
    | [<CompiledName("--edit")>] Edit
    | [<CompiledName("--no-edit")>] NoEdit
    | [<CompiledName("--ff-only")>] FFOnly
    | [<CompiledName("--ff")>] FF
    | [<CompiledName("--no-ff")>] NoFF
    | [<CompiledName("--no-gpg-sign")>] NoGpgSign
    | [<CompiledName("--no-log")>] NoLog
    | [<CompiledName("--signoff")>] SignOff
    | [<CompiledName("--no-signoff")>] NoSignOff
    | [<CompiledName("--stat")>] Stat
    | [<CompiledName("-n")>] N
    | [<CompiledName("--no-stat")>] NoStat
    | [<CompiledName("--compact-summary")>] CompactSummary
    | [<CompiledName("--squash")>] Squash
    | [<CompiledName("--no-squash")>] NoSquash
    | [<CompiledName("--verify")>] Verify
    | [<CompiledName("--no-verify")>] NoVerify
    | [<CompiledName("--verify-signatures")>] VerifySignatures
    | [<CompiledName("--no-verify-signatures")>] NoVerifySignatures
    | [<CompiledName("--summary")>] Summary
    | [<CompiledName("--no-summary")>] NoSummary
    | [<CompiledName("--autostash")>] Autostash
    | [<CompiledName("--no-autostash")>] NoAutostash
    | [<CompiledName("--allow-unrelated-histories")>] AllowUnrelatedHistories
    | [<CompiledName("--no-rebase")>] NoRebase

[<StringEnum(CaseRules.LowerFirst)>]
type PullRebaseValues =
    | False
    | True
    | Merges
    | Interactive

let rebaseWithValue (value: PullRebaseValues) = $"rebase={value}"

[<StringEnum>]
type PullOptionsMergingWithValues =
    | [<CompiledName("--cleanup")>] Cleanup
    | [<CompiledName("--gpg-sign")>] GpgSign
    | [<CompiledName("--log")>] Log
    | [<CompiledName("--strategy")>] Strategy
    | [<CompiledName("--strategy-option")>] StrategyOption

let pullMergingWithValue (flag: PullOptionsMergingWithValues) (value: string) = $"{flag}={value}"

[<StringEnum>]
type PullOptionsFetching =
    | [<CompiledName("--all")>] All
    | [<CompiledName("--no-all")>] NoAll
    | [<CompiledName("--append")>] Append
    | [<CompiledName("--atomic")>] Atomic
    | [<CompiledName("--unshallow")>] Unshallow
    | [<CompiledName("--update-shallow")>] UpdateShallow
    | [<CompiledName("--negotiate-only")>] NegotiateOnly
    | [<CompiledName("--dry-run")>] DryRun
    | [<CompiledName("--porcelain")>] Porcelain
    | [<CompiledName("--force")>] Force
    | [<CompiledName("--keep")>] Keep
    | [<CompiledName("--prune")>] Prune
    | [<CompiledName("--no-tags")>] NoTags
    | [<CompiledName("--tags")>] FetchTags
    | [<CompiledName("--set-upstream")>] SetUpstream
    | [<CompiledName("--progress")>] Progress
    | [<CompiledName("--show-forced-updates")>] ShowForcedUpdates
    | [<CompiledName("--no-show-forced-updates")>] NoShowForcedUpdates
    | [<CompiledName("--ipv4")>] Ipv4
    | [<CompiledName("--ipv6")>] Ipv6

[<StringEnum>]
type PullOptionsFetchingWithNumbers =
    | [<CompiledName("--depth")>] Depth
    | [<CompiledName("--deepen")>] Deepen
    | [<CompiledName("--jobs")>] Jobs

let createPullFetchOptionsWithNumbers (flag: PullOptionsFetchingWithNumbers) (value: int) = $"{flag}={value}"

[<StringEnum>]
type PullOptionsFetchingWithValue =
    | [<CompiledName("--shallow-since")>] ShallowSince
    | [<CompiledName("--shallow-exclude")>] ShallowExclude
    | [<CompiledName("--refmap")>] Refmap
    | [<CompiledName("--upload-pack")>] UploadPack
    | [<CompiledName("--server-option")>] ServerOption

let createPullFetchOptionsWithValue (flag: PullOptionsFetchingWithValue) (value: string) = $"{flag}={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type PullOptionNegotiateValues =
    | [<CompiledName("--commit")>] Commit
    | [<CompiledName("--glob")>] Glob

let createPullOptionNegotiation (value: PullOptionNegotiateValues) = $"--negotiation-tip={value}"

[<StringEnum>]
type PushOptionsWithoutValues =
    | [<CompiledName("--all")>] All
    | [<CompiledName("--branches")>] Branches
    | [<CompiledName("--prune")>] Prune
    | [<CompiledName("--mirror")>] Mirror
    | [<CompiledName("--dry-run")>] DryRun
    | [<CompiledName("--porcelain")>] Porcelain
    | [<CompiledName("--delete")>] Delete
    | [<CompiledName("--tags")>] PushTags
    | [<CompiledName("--follow-tags")>] FollowTags
    | [<CompiledName("--no-signed")>] NoSigned
    | [<CompiledName("--atomic")>] Atomic
    | [<CompiledName("--no-atomic")>] NoAtomic
    | [<CompiledName("--no-force-with-lease")>] NoForceWithLease
    | [<CompiledName("--force")>] Force
    | [<CompiledName("--set-upstream")>] SetUpstream
    | [<CompiledName("--thin")>] Thin
    | [<CompiledName("--no-thin")>] NoThin
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("--verbose")>] Verbose
    | [<CompiledName("--progress")>] Progress
    | [<CompiledName("--no-recurse-submodules")>] NoRecurseSubmodules
    | [<CompiledName("--verify")>] Verify
    | [<CompiledName("--no-verify")>] NoVerify
    | [<CompiledName("--ipv4")>] Ipv4
    | [<CompiledName("--ipv6")>] Ipv6

[<StringEnum>]
type PushOptionsWithStrings =
    | [<CompiledName("--push-option")>] PushOption
    | [<CompiledName("--receive-pack")>] ReceivePack
    | [<CompiledName("--exec")>] Exec
    | [<CompiledName("--force-with-lease")>] ForceWithLease
    | [<CompiledName("--repo")>] Repo

let createPushOptionsWithValue (flag: PushOptionsWithStrings) (value: string) = $"{flag}={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type PushOptionsSignedValues =
    | True
    | False
    | [<CompiledName("if-asked")>] IfAsked

let createPushOptionSigned (value: PushOptionsSignedValues) = $"--signed={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type PushOptionsRecurseValues =
    | Check
    | [<CompiledName("on-demand")>] OnDemand
    | Only
    | No

let createPushOptionRecurse (value: PushOptionsRecurseValues) = $"--recurse-submodules={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type SharedValues =
    | False
    | True
    | Umask
    | Group
    | All
    | World
    | Everybody

let sharedWithValue (value: SharedValues) = $"--shared={value}"

[<AllowNullLiteral>]
type IBranchSummaryBranch =
    abstract current: bool option
    abstract name: string option
    abstract commit: string option
    abstract label: string option
    abstract linkedWorkTree: bool option

[<AllowNullLiteral>]
type IBranchSummaryResult =
    abstract all: string[] option

    abstract branches:
        {|
            key: string
            value: IBranchSummaryBranch
        |} option

    abstract current: string option
    abstract detached: bool option

[<AllowNullLiteral>]
type IBranchSingleDeleteSuccess =
    abstract branch: string option
    abstract hash: string option
    abstract success: bool option

[<AllowNullLiteral>]
type IBranchSingleDeleteFailure =
    abstract branch: string option
    abstract hash: objnull option
    abstract success: bool option

[<Erase>]
type IBranchSingleDeleteResult = U2<IBranchSingleDeleteFailure, IBranchSingleDeleteSuccess>

[<AllowNullLiteral>]
type IBranchMultiDeleteResult =
    abstract all: IBranchSingleDeleteResult[] option

    abstract branches:
        {|
            key: string
            value: IBranchSummaryBranch
        |} option

    abstract errors: IBranchSingleDeleteResult[] option
    abstract success: bool option

[<AllowNullLiteral>]
type CountObjectsResult =
    abstract count: int option
    abstract size: int option
    abstract inPack: int option
    abstract packs: int option
    abstract sizePack: int option
    abstract prunePackable: int option
    abstract garbage: int option
    abstract sizeGarbage: int option

[<AllowNullLiteral>]
type IDiffResultTextFile =
    abstract file: string option
    abstract changes: int option
    abstract insertions: int option
    abstract deletions: int option
    abstract binary: bool option

[<AllowNullLiteral>]
type IDiffResultBinaryFile =
    abstract file: string option
    abstract before: int option
    abstract after: int option
    abstract binary: bool option

[<AllowNullLiteral>]
type IDiffSummary =
    abstract changed: int option
    abstract deletions: int option
    abstract insertions: int option
    abstract files: U2<IDiffResultTextFile, IDiffResultBinaryFile>[] option

[<AllowNullLiteral>]
type IMergeResult =
    abstract merged: string[] option
    abstract conflicts: string[] option
    abstract failed: bool option

type IApplyOptions = interface end

type applyOptions =
    static member inline options(tag: ApplyOptions) : IApplyOptions = createObj [ string tag, null ] |> unbox

    static member inline optionsWithStrings(tag: ApplyOptionsWithValues, value: string) : IApplyOptions =
        createObj [ string tag, value ] |> unbox

    static member inline whitespace(value: WhiteSpaceActions) : IApplyOptions =
        createObj [ "--whitespace", value ] |> unbox

type IBranchOptions = interface end

type branchOptions =
    static member inline options(tag: BranchOptions) : IBranchOptions = createObj [ string tag, null ] |> unbox

    static member inline optionsWithStrings(tag: BranchOptionsWithStrings, value: string) : IBranchOptions =
        createObj [ string tag, value ] |> unbox

    static member inline colour(value: WhenColour) : IBranchOptions = createObj [ "--color", value ] |> unbox
    static member inline track(value: TrackOptions) : IBranchOptions = createObj [ "--track", value ] |> unbox

type ICleanOptions = interface end

type cleanOptions =
    static member inline options(tag: CleanOptions) : ICleanOptions = createObj [ string tag, null ] |> unbox

    static member inline exclude(value: string) : ICleanOptions =
        createObj [ "--exclude", value ] |> unbox

type ICheckoutOptions = interface end

type checkoutOptions =
    static member inline checkout(tag: CheckoutOptions) : ICheckoutOptions = createObj [ string tag, null ] |> unbox

    static member inline checkoutWithNumbers(tag: CheckoutOptionsWithNumber, value: int) : ICheckoutOptions =
        createObj [ string tag, string value ] |> unbox

    static member inline checkoutWithStrings(tag: CheckoutOptionsWithString, value: string) : ICheckoutOptions =
        createObj [ string tag, value ] |> unbox

    static member inline checkoutBranch(tag: CheckoutBranchOptions, newBranch: string) : ICheckoutOptions =
        createObj [ string tag, newBranch ] |> unbox

    static member inline conflict(value: ConflictOptions) : ICheckoutOptions =
        createObj [ "--conflict", string value ] |> unbox

type ICloneOptions = interface end

type cloneOptions =
    static member inline options(tag: CloneOptions) : ICloneOptions = createObj [ string tag, null ] |> unbox

    static member inline optionsWithStrings(tag: CloneOptionsWithStrings, value: string) : ICloneOptions =
        createObj [ string tag, value ] |> unbox

    static member inline optionsWithNumbers(tag: CloneOptionsWithNumbers, value: int) : ICloneOptions =
        createObj [ string tag, string value ] |> unbox

type IDiff = interface end

type diffOptions =
    static member inline options(tag: DiffOptions) : IBranchOptions = createObj [ string tag, null ] |> unbox

    static member inline optionsWithStrings(value: DiffAlgorithmOptions) : IBranchOptions =
        createObj [ "--diff-algorithm", string value ] |> unbox

type IInitOptions = interface end

type initOptions =
    static member inline options(tag: InitOptions) : IInitOptions = createObj [ string tag, null ] |> unbox

    static member inline optionsWithStrings(tag: InitOptionsWithStrings, value: string) : IInitOptions =
        createObj [ string tag, value ] |> unbox

type IMergeOptions = interface end

type mergeOptions =
    static member inline options(tag: MergeOptions) : IMergeOptions = createObj [ string tag, null ] |> unbox

    static member inline optionsWithValues(tag: MergeOptionsWithStrings, value: string) : IMergeOptions =
        createObj [ string tag, value ] |> unbox

type IMoveOptions = interface end

type moveOptions =
    static member inline options(tag: MoveOptions) : IMoveOptions = createObj [ string tag, null ] |> unbox

type IPullOptions = interface end

type pullOptions =
    static member inline options(tag: PullOptionsWithoutValues) : IPullOptions = createObj [ string tag, null ] |> unbox

    static member inline rebase(value: PullRebaseValues) : IPullOptions =
        createObj [ "--rebase", value ] |> unbox

    static member inline recurseSubmodules(value: PullOptionRecurseValues) : IPullOptions =
        createObj [ "--recurse-submodules", value ] |> unbox

    static member inline merging(tag: PullOptionsMerging) : IPullOptions = createObj [ string tag, null ] |> unbox

    static member inline mergingWithValues(tag: PullOptionsMergingWithValues, value: string) : IPullOptions =
        createObj [ string tag, value ] |> unbox

type IPushOptions = interface end

type pushOptions =
    static member inline options(tag: PushOptionsWithoutValues) : IPushOptions = createObj [ string tag, null ] |> unbox

    static member inline rebase(tag: PushOptionsWithStrings, value: string) : IPushOptions =
        createObj [ string tag, value ] |> unbox

    static member inline signed(value: PushOptionsSignedValues) : IPushOptions =
        createObj [ "--signed", string value ] |> unbox

    static member inline mergingWithValues(value: SharedValues) : IPushOptions =
        createObj [ "--shared", string value ] |> unbox

    static member inline recurseSubmodules(value: PushOptionsRecurseValues) : IPushOptions =
        createObj [ "--recurse-submodules", string value ] |> unbox

type IAbortSignal =

    abstract member aborted: bool
    abstract member reason: string option

type IAbortController =

    abstract member signal: IAbortSignal
    abstract member abort: ?reason: string -> unit

[<StringEnum(CaseRules.LowerFirst)>]
type Stages =
    | Compressing
    | Counting
    | Receiving
    | Resolving
    | Unknown
    | Writing

type IProgressEvent =
    abstract member method: string option
    abstract member stage: Stages option
    abstract member progress: int option
    abstract member processed: int option
    abstract member total: int option

type ISimpleGit =

    abstract member apply: patch: string * ?options: IApplyOptions[] -> Promise<unit>
    abstract member apply: patches: string[] * ?options: IApplyOptions[] -> Promise<unit>

    abstract member branch: ?options: IBranchOptions[] -> Promise<U2<IBranchSummaryResult, IBranchSingleDeleteResult>>

    abstract member branchLocal: unit -> Promise<IBranchSummaryResult>

    abstract member checkout: checkoutWhat: string * ?options: string[] -> Promise<string>

    abstract member clean: ?options: ICleanOptions[] -> Promise<unit>
    abstract member clean: cleanSwitches: string * ?options: ICleanOptions[] -> Promise<unit>

    abstract member clone: repopath: string * ?options: ICloneOptions[] -> Promise<string>
    abstract member clone: repopath: string * localPath: string * ?options: ICloneOptions[] -> Promise<string>
    abstract member mirror: repopath: string * ?mirrorIotions: (string * ICloneOptions[])[] -> Promise<string>

    abstract member countObjects: unit -> Promise<CountObjectsResult>

    abstract member deleteLocalBranch: branchName: string * ?forceDelete: bool -> Promise<IBranchSingleDeleteResult>

    abstract member deleteLocalBranches: branchNames: string[] * ?forceDelete: bool -> Promise<IBranchMultiDeleteResult>

    abstract member diff: ?options: string[] -> Promise<string>

    abstract member diffResult: ?options: string[] -> Promise<IDiffSummary>

    abstract member init: ?options: IInitOptions[] -> Promise<ISimpleGit>
    abstract member init: bare: bool * ?options: IInitOptions[] -> Promise<ISimpleGit>

    abstract member merge: ?options: IMergeOptions[] -> Promise<IMergeResult>
    abstract member mergeFromTo: remote: string * branch: string * options: IMergeOptions[] -> Promise<IMergeResult>

    abstract member mv: from: string * target: string * ?options: IMoveOptions[] -> Promise<unit>
    abstract member mv: from: string[] * target: string * ?options: IMoveOptions[] -> Promise<unit>

    abstract member pull: ?options: IPullOptions[] -> Promise<unit>
    abstract member pull: remote: string * branch: string * ?options: IPullOptions[] -> Promise<unit>

    abstract member push: ?options: string[] -> Promise<unit>
    abstract member push: remote: string * branch: string * ?options: string[] -> Promise<unit>
    abstract member push: ?pushMultipletags: (string * string[])[] -> Promise<unit>
    abstract member pushTags: remote: string * ?options: string[] -> Promise<unit>

    abstract member raw: args: string[] * ?handlerFn: (exn option -> string -> unit) -> Promise<string>

[<AllowNullLiteral>]
type SimpleGitOptions
    [<ParamObjectAttribute; Emit("$0")>]
    (
        ?baseDir: string,
        ?binary: string,
        ?maxConcurrentProcesses: int,
        ?trimmed: bool,
        ?abort: IAbortController,
        ?progress: IProgressEvent
    ) =
    member val baseDir: string option = baseDir with get, set
    member val binary: string option = binary with get, set
    member val maxConcurrentProcesses: int option = maxConcurrentProcesses with get, set
    member val trimmed: bool option = trimmed with get, set
    member val abort: IAbortController option = abort with get, set
    member val progress: IProgressEvent option = progress with get, set

[<AllowNullLiteral>]
[<Import("GitPluginError", "simple-git")>]
type IGitPluginError =

    abstract member task: obj option
    abstract member plugin: string option
    abstract member message: string option

type GitPluginException(err: IGitPluginError) =

    inherit System.Exception(err.message |> Option.defaultValue "")

    member _.Plugin = err.plugin
    member _.Task = err.task

[<Erase>]
type GitPluginError =

    [<Emit("$0, $1, $2")>]
    static member create(?task: obj, ?plugin: string, ?message: string) : IGitPluginError = jsNative

[<Erase>]
type AbortController =

    [<Emit("new AbortController()")>]
    static member create() : IAbortController = jsNative

[<Erase>]
type ProgressEvent =

    [<Emit("{ method: $0, stage: $1, progress: $2, processed: $3, total: $4 }")>]
    static member create
        (?method: string, ?stage: Stages, ?progress: int, ?processed: int, ?total: int)
        : IProgressEvent =
        jsNative

[<Erase>]
type SimpleGit =

    [<Import("simpleGit", "simple-git")>]
    static member create(options: SimpleGitOptions) : ISimpleGit = jsNative

//How to

console.log ("AbortController")

let abortController = AbortController.create ()

console.log (abortController)

console.log ("aborted shall be false")

console.log (abortController.signal.aborted)

console.log ("aborted shall be true")

abortController.abort ("Cancelled!")

console.log (abortController.signal.aborted)

console.log ("Progress Events")

let progress = ProgressEvent.create ("Test", Stages.Unknown, 0, 1, 2)

console.log (progress)

console.log ("SimpleGit!")

let simpleGit =
    SimpleGit.create (
        SimpleGitOptions(
            baseDir = "./",
            binary = "git",
            maxConcurrentProcesses = 6,
            trimmed = true,
            abort = abortController,
            progress = progress
        )
    )

console.log (simpleGit)

//ToAdd: AbortController; Progress Events;

console.log ("SimpleGit IPullOptions!")

let x = pullOptions.merging (PullOptionsMerging.AllowUnrelatedHistories)
let y = pullOptions.rebase (PullRebaseValues.False)
let z = pullOptions.recurseSubmodules (PullOptionRecurseValues.Yes)

console.log (x)
console.log (y)

//simpleGit.pull([|pullOptions.options(PullOptionsWithoutValues.Force)|])