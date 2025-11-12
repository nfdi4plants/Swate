
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
type InitOptions =
    | [<CompiledName("-q")>] Quiet
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
    //| [<CompiledName("branch-name")>] BranchName
    //| [<CompiledName("start-point")>] StartPoint
    //| [<CompiledName("old-branch")>] OldBranch
    //| [<CompiledName("new-branch")>] NewBranch

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
type BranchSummaryBranch
    [<ParamObjectAttribute; Emit("$0")>](
      ?current: bool,
      ?name: string,
      ?commit: string,
      ?label: string,
      ?linkedWorkTree: bool
    ) =
    member val current: bool option = current with get, set
    member val name: string option = name with get, set
    member val commit: string option = commit with get, set
    member val label: string option = label with get, set
    member val linkedWorkTree: bool option = linkedWorkTree with get, set

[<AllowNullLiteral>]
type BranchSummaryResult
    [<ParamObjectAttribute; Emit("$0")>](
      ?all: string[],
      ?branches: {|key: string; value: BranchSummaryBranch|},
      ?current: string,
      ?detached: bool
    ) =
    member val all: string[] option = all with get, set
    member val branches: {|key: string; value: BranchSummaryBranch|} option = branches with get, set
    member val current: string option = current with get, set
    member val detached: bool option = detached with get, set

[<AllowNullLiteral>]
type BranchSingleDeleteSuccess
    [<ParamObjectAttribute; Emit("$0")>](
      ?branch: string,
      ?hash: string,
      ?success: bool
    ) =
    member val branch: string option = branch with get, set
    member val hash: string option = hash with get, set
    member val success: bool option = success with get, set

[<AllowNullLiteral>]
type BranchSingleDeleteFailure
    [<ParamObjectAttribute; Emit("$0")>](
      ?branch: string,
      ?hash: objnull,
      ?success: bool
    ) =
    member val branch: string option = branch with get, set
    member val hash: objnull option = hash with get, set
    member val success: bool option = success with get, set

[<Erase>]
type BranchSingleDeleteResult = U2<BranchSingleDeleteFailure, BranchSingleDeleteSuccess>

[<AllowNullLiteral>]
type BranchMultiDeleteResult
    [<ParamObjectAttribute; Emit("$0")>](
      ?all: BranchSingleDeleteResult[],
      ?branches: {|key: string; value: BranchSummaryBranch|},
      ?errors: BranchSingleDeleteResult[],
      ?success: bool
    ) =
    member val all: BranchSingleDeleteResult [] option = all with get, set
    member val branches: {|key: string; value: BranchSummaryBranch|} option = branches with get, set
    member val errors: BranchSingleDeleteResult [] option = errors with get, set
    member val success: bool option = success with get, set

type ISimpleGit =

    abstract member apply: patch:string -> Promise<unit>
    abstract member apply: patch:string * options: string[] -> Promise<unit>
    abstract member apply: patches:string[] -> Promise<unit>
    abstract member apply: patches:string[] * options: string[] -> Promise<unit>

    abstract member clean: options: string -> Promise<unit>
    abstract member clean: options: string[] -> Promise<unit>
    abstract member clean: cleanSwitches: string * options: string[] -> Promise<unit>

    abstract member init: unit -> Promise<ISimpleGit>
    abstract member init: bare: bool -> Promise<ISimpleGit>
    abstract member init: options: string[] -> Promise<ISimpleGit>
    abstract member init: bare: bool * options: string[] -> Promise<ISimpleGit>

    abstract member diff: options: string[] -> Promise<unit>

    abstract member branch: options: string[] -> Promise<U2<BranchSummaryResult, BranchSingleDeleteResult>>

    abstract member branchLocal: unit ->  Promise<BranchSummaryResult>

    abstract member deleteLocalBranch: branchName: string -> Promise<BranchSingleDeleteResult>
    abstract member deleteLocalBranch: branchName: string * forceDelete: bool -> Promise<BranchSingleDeleteResult>

    abstract member deleteLocalBranches: branchNames: string[] -> Promise<BranchMultiDeleteResult>
    abstract member deleteLocalBranches: branchNames: string[] * forceDelete: bool -> Promise<BranchMultiDeleteResult>

[<Erase>]
type SimpleGit =

    [<Import("simpleGit", "simple-git")>]
    static member simpleGit (options: SimpleGitOptions) : ISimpleGit = jsNative
