
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

let applyFlagWithValue (flag: ApplyOptionsWithValues) (value: string) =
    $"{flag}={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type WhiteSpaceActions =
    | Nowarn
    | Warn
    | Fix
    | Error
    | [<CompiledName("error-all")>] ErrorAll

let whiteSpaceWithActions (value: WhiteSpaceActions) =
    $"--whitespace={value}"

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

let cleanEPattern (pattern: string) =
    $"-e {pattern}"

let cleanExcludePattern (pattern: string) =
    $"--exclude={pattern}"

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

let createCheckoutConflict (value: ConflictOptions) =
    $"--conflict={value}"

[<StringEnum>]
type CheckoutOptionsWithNumbers =
    | [<CompiledName("--unified")>] Unified
    | [<CompiledName("--inter-hunk-context")>] InterHunkContext

let createCheckoutOptionsWithNumbers (tag: CheckoutOptionsWithNumbers) (n: int) =
    $"{tag}={n}"

[<StringEnum>]
type CheckoutOptionsWithValues =
    | [<CompiledName("--pathspec-from-file=")>] PathspecFromFile

let createCheckoutOptionsWithValues (tag: CheckoutOptionsWithValues) (value: string) =
    $"{tag}={value}"

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
type CloneOptionsWithValues =
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

let cloneOptionWithValue (tag: CloneOptionsWithValues) (value: string) =
    $"{tag}={value}"

[<StringEnum>]
type CloneOptionsWithNumbers =
    | [<CompiledName("--depth")>] Depth
    | [<CompiledName("--jobs")>] Jobs

let cloneOptionWithNumber (tag: CloneOptionsWithNumbers) (n: int) =
    $"{tag} {n}"

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
type InitOptions =
    | [<CompiledName("--quiet")>] Quiet
    | [<CompiledName("--bare")>] Bare
    | [<CompiledName("--shared")>] Shared

[<StringEnum(CaseRules.LowerFirst)>]
type SharedValues =
    | False
    | True
    | Umask
    | Group
    | All
    | World
    | Everybody

let sharedWithValue (value: SharedValues) =
    $"--shared={value}"

[<StringEnum>]
type InitOptionsWithValues =
    | [<CompiledName("--object-format")>] ObjectFormat
    | [<CompiledName("--ref-format")>] RefFormat
    | [<CompiledName("--template")>] Template
    | [<CompiledName("--separate-git-dir")>] SeparateGitDir
    | [<CompiledName("--initial-branch")>] InitialBranch

let initFlagWithValue (flag: InitOptionsWithValues) (value: string) =
    $"{flag}={value}"

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
type BranchOptionsWithValues =
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

let branchWithValue (flag: BranchOptionsWithValues) (value: string) =
    $"{flag}={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type WhenColour =
    | Always
    | Never
    | Auto

let createColourWithValue (value: WhenColour) =
    $"--color={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type TrackOptions =
    | Direct
    | Inherit

let trackWithValue (value: TrackOptions) =
    $"--track={value}"

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
type MergeOptionsWithValues =
    | [<CompiledName("--cleanup")>] Cleanup
    | [<CompiledName("--s")>] S
    | [<CompiledName("--gpg-sign")>] GpgSign
    | [<CompiledName("--log")>] Log
    | [<CompiledName("--strategy")>] Strategy
    | [<CompiledName("--strategy-option")>] StrategyOption
    | [<CompiledName("--m")>] M
    | [<CompiledName("--into-name")>] IntoName
    | [<CompiledName("--file")>] File

let mergeWithValue (flag: MergeOptionsWithValues) (value: string) =
    $"{flag}={value}"

[<StringEnum>]
type MoveOptions =
    | [<CompiledName("--force")>] Force
    | [<CompiledName("-k")>] K
    | [<CompiledName("--dry-run")>] DryRun
    | [<CompiledName("--verbose")>] Verbose

[<StringEnum>]
type PullOptions =
    | [<CompiledName("--quiet")>] Force
    | [<CompiledName("--verbose")>] Verbose

[<StringEnum>]
type PullOptionTags =
    | [<CompiledName("--recurse-submodules")>] RecurseSubmodules
    | [<CompiledName("--no-recurse-submodules")>] NoRecurseSubmodules

[<StringEnum(CaseRules.LowerFirst)>]
type PullOptionValues =
    | Yes
    | No
    | [<CompiledName("on-demand")>] OnDemand

let createPullOptions (flag: PullOptionTags) (value: PullOptionValues) =
    $"{flag}={value}"

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

let rebaseWithValue (value: PullRebaseValues) =
    $"rebase={value}"

[<StringEnum>]
type PullOptionsMergingWithValues =
    | [<CompiledName("--cleanup")>] Cleanup
    | [<CompiledName("--s")>] S
    | [<CompiledName("--gpg-sign")>] GpgSign
    | [<CompiledName("--log")>] Log
    | [<CompiledName("--strategy")>] Strategy
    | [<CompiledName("--strategy-option")>] StrategyOption

let pullMergingWithValue (flag: PullOptionsMergingWithValues) (value: string) =
    $"{flag}={value}"

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

let createPullFetchOptionsWithNumbers (flag: PullOptionsFetchingWithNumbers) (value: int) =
    $"{flag}={value}"

[<StringEnum>]
type PullOptionsFetchingWithValue =
    | [<CompiledName("--shallow-since")>] ShallowSince
    | [<CompiledName("--shallow-exclude")>] ShallowExclude
    | [<CompiledName("--refmap")>] Refmap
    | [<CompiledName("--upload-pack")>] UploadPack
    | [<CompiledName("--server-option")>] ServerOption

let createPullFetchOptionsWithValue (flag: PullOptionsFetchingWithValue) (value: string) =
    $"{flag}={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type PullOptionNegotiateValues =
    | [<CompiledName("--commit")>] Commit
    | [<CompiledName("--glob")>] Glob

let createPullOptionNegotiation (value: PullOptionNegotiateValues) =
    $"--negotiation-tip={value}"

[<StringEnum>]
type PushOptions =
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
type PushOptionsWithValue =
    | [<CompiledName("--push-option")>] PushOption
    | [<CompiledName("--receive-pack")>] ReceivePack
    | [<CompiledName("--exec")>] Exec
    | [<CompiledName("--force-with-lease")>] ForceWithLease
    | [<CompiledName("--repo")>] Repo

let createPushOptionsWithValue (flag: PushOptionsWithValue) (value: string) =
    $"{flag}={value}"
    
[<StringEnum(CaseRules.LowerFirst)>]
type PushOptionsSignesValues =
    | True
    | False
    | [<CompiledName("if-asked")>] IfAsked

let createPushOptionSigned (value: PushOptionsSignesValues) =
    $"--signed={value}"

[<StringEnum(CaseRules.LowerFirst)>]
type PushOptionsRecureseValues =
    | Check
    | [<CompiledName("on-demand")>] OnDemand
    | Only
    | No

let createPushOptionRecurse (value: PushOptionsSignesValues) =
    $"--recurse-submodules={value}"

[<AllowNullLiteral>]
type SimpleGitOptions
    [<ParamObjectAttribute; Emit("$0")>](
      ?baseDir: string,
      ?binary: string,
      ?maxConcurrentProcesses: int,
      ?trimmed: bool
    ) =
    member val baseDir: string option = baseDir with get, set
    member val binary: string option = binary with get, set
    member val maxConcurrentProcesses: int option = maxConcurrentProcesses with get, set
    member val trimmed: bool option = trimmed with get, set

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
    abstract branches: {|key: string; value: IBranchSummaryBranch|} option
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
    abstract all: IBranchSingleDeleteResult [] option
    abstract branches: {|key: string; value: IBranchSummaryBranch|} option
    abstract errors: IBranchSingleDeleteResult [] option
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

type ISimpleGit =

    abstract member apply: patch:string -> Promise<unit>
    abstract member apply: patch:string * options: string[] -> Promise<unit>
    abstract member apply: patches:string[] -> Promise<unit>
    abstract member apply: patches:string[] * options: string[] -> Promise<unit>

    abstract member branch: options: string[] -> Promise<U2<IBranchSummaryResult, IBranchSingleDeleteResult>>

    abstract member branchLocal: unit ->  Promise<IBranchSummaryResult>

    abstract member clean: options: string -> Promise<unit>
    abstract member clean: options: string[] -> Promise<unit>
    abstract member clean: cleanSwitches: string * options: string[] -> Promise<unit>

    abstract member checkout: checkoutWhat: string -> Promise<string>
    abstract member checkout: checkoutWhat: string * options: string[] -> Promise<string>

    abstract member clone: repopath: string -> Promise<string>
    abstract member clone: repopath: string * localPath: string -> Promise<string>
    abstract member clone: repopath: string * options: string[] -> Promise<string>
    abstract member clone: repopath: string * localPath: string * options: string[] -> Promise<string>

    abstract member mirror: repopath: string * localPath: string -> Promise<string>
    abstract member mirror: repopath: string * localPath: string * options: string[] -> Promise<string>

    abstract member countObjects: unit -> Promise<CountObjectsResult>

    abstract member deleteLocalBranch: branchName: string -> Promise<IBranchSingleDeleteResult>
    abstract member deleteLocalBranch: branchName: string * forceDelete: bool -> Promise<IBranchSingleDeleteResult>

    abstract member deleteLocalBranches: branchNames: string[] -> Promise<IBranchMultiDeleteResult>
    abstract member deleteLocalBranches: branchNames: string[] * forceDelete: bool -> Promise<IBranchMultiDeleteResult>

    abstract member diff: options: string[] -> Promise<string>

    abstract member diffResult: options: string[] -> Promise<IDiffSummary>

    abstract member init: unit -> Promise<ISimpleGit>
    abstract member init: bare: bool -> Promise<ISimpleGit>
    abstract member init: options: string[] -> Promise<ISimpleGit>
    abstract member init: bare: bool * options: string[] -> Promise<ISimpleGit>

    abstract member merge: unit -> Promise<IMergeResult>
    abstract member merge: options: string[] -> Promise<IMergeResult>

    abstract member mergeFromTo: remote:string * branch:string -> Promise<IMergeResult>
    abstract member mergeFromTo: remote:string * branch:string * options: string[] -> Promise<IMergeResult>

    abstract member mv: from:string * target:string -> Promise<unit>
    abstract member mv: from:string * target:string * options: string[] -> Promise<unit>
    abstract member mv: from:string[] * target:string -> Promise<unit>
    abstract member mv: from:string[] * target:string * options: string[] -> Promise<unit>

    abstract member pull: unit -> Promise<unit>
    abstract member pull: options: string[] -> Promise<unit>
    abstract member pull: remote:string * branch:string * options: string[] -> Promise<unit>

    abstract member push: unit -> Promise<unit>
    abstract member push: options: string[] -> Promise<unit>
    abstract member push: remote:string -> Promise<unit>
    abstract member push: remote:string * options: string[] -> Promise<unit>
    abstract member push: remote:string * branch:string * options: string[] -> Promise<unit>

[<Erase>]
type SimpleGit =

    [<Import("simpleGit", "simple-git")>]
    static member simpleGit (options: SimpleGitOptions) : ISimpleGit = jsNative

let simpleGit = SimpleGit.simpleGit(SimpleGitOptions(baseDir = "./", binary = "git", maxConcurrentProcesses = 6, trimmed = true))

console.log("SimpleGit!")
console.log(simpleGit)

//ToAdd: AbortController; Progress Events; 
