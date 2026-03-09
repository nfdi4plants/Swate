module Main.Bindings.SimpleGit

open Fable.Core
open Fable.Core.JS

type TaskOptions = U2<string[], obj>
type GitOptions = obj
type GitConfigScope = string
type CheckRepoAction = string
type CleanMode = string
type ResetMode = string
type GitGrepQuery = obj
type LogOptions = obj
type SimpleGitBinary = U3<string, string[], (string * string)>
type SimpleGitErrorsHandler = obj option -> obj -> obj option

[<AllowNullLiteral>]
type IAbortSignal =
    abstract member aborted: bool
    abstract member reason: obj option

[<AllowNullLiteral>]
type IAbortController =
    abstract member signal: IAbortSignal
    abstract member abort: ?reason: obj -> unit

[<AllowNullLiteral>]
type SimpleGitTimeoutOptions
    [<ParamObject>]
    (
        ?block: int,
        ?stdOut: bool,
        ?stdErr: bool
    ) =
    member val block: int option = block with get, set
    member val stdOut: bool option = stdOut with get, set
    member val stdErr: bool option = stdErr with get, set

[<AllowNullLiteral>]
type SimpleGitUnsafeOptions
    [<ParamObject>]
    (
        ?allowUnsafeCustomBinary: bool,
        ?allowUnsafeProtocolOverride: bool,
        ?allowUnsafePack: bool
    ) =
    member val allowUnsafeCustomBinary: bool option = allowUnsafeCustomBinary with get, set
    member val allowUnsafeProtocolOverride: bool option = allowUnsafeProtocolOverride with get, set
    member val allowUnsafePack: bool option = allowUnsafePack with get, set

[<AllowNullLiteral>]
type SimpleGitProgressEvent =
    abstract member method: string
    abstract member stage: string
    abstract member progress: float
    abstract member processed: float
    abstract member total: float

type SimpleGitProgressHandler = SimpleGitProgressEvent -> unit

[<AllowNullLiteral>]
type VersionResult =
    abstract member major: int
    abstract member minor: int
    abstract member patch: U2<int, string>
    abstract member agent: string
    abstract member installed: bool

[<AllowNullLiteral>]
type CountObjectsResult =
    abstract member count: int
    abstract member size: int
    abstract member inPack: int
    abstract member packs: int
    abstract member sizePack: int
    abstract member prunePackable: int
    abstract member garbage: int
    abstract member sizeGarbage: int

[<AllowNullLiteral>]
type SimpleGitFileStatus =
    abstract member path: string
    abstract member index: string
    abstract member working_dir: string
    abstract member ``from``: string option

[<AllowNullLiteral>]
type StatusResultRenamed =
    abstract member ``from``: string
    abstract member ``to``: string

[<AllowNullLiteral>]
type StatusResult =
    abstract member not_added: string[]
    abstract member conflicted: string[]
    abstract member created: string[]
    abstract member deleted: string[]
    abstract member ignored: string[] option
    abstract member modified: string[]
    abstract member renamed: StatusResultRenamed[]
    abstract member staged: string[]
    abstract member files: SimpleGitFileStatus[]
    abstract member ahead: int
    abstract member behind: int
    abstract member current: string option
    abstract member tracking: string option
    abstract member detached: bool
    abstract member isClean: unit -> bool

[<AllowNullLiteral>]
type BranchSummaryBranch =
    abstract member current: bool
    abstract member name: string
    abstract member commit: string
    abstract member label: string
    abstract member linkedWorkTree: bool

[<AllowNullLiteral>]
type BranchSummary =
    abstract member detached: bool
    abstract member current: string
    abstract member all: string[]
    abstract member branches: System.Collections.Generic.Dictionary<string, BranchSummaryBranch>

[<AllowNullLiteral>]
type BranchSingleDeleteSuccess =
    abstract member branch: string
    abstract member hash: string
    abstract member success: bool

[<AllowNullLiteral>]
type BranchSingleDeleteFailure =
    abstract member branch: string
    // Upstream defines this as always null on failure.
    abstract member hash: obj
    abstract member success: bool

type BranchSingleDeleteResult = U2<BranchSingleDeleteFailure, BranchSingleDeleteSuccess>

[<AllowNullLiteral>]
type BranchMultiDeleteResult =
    abstract member all: BranchSingleDeleteResult[]
    abstract member branches: System.Collections.Generic.Dictionary<string, BranchSingleDeleteResult>
    abstract member errors: BranchSingleDeleteResult[]
    abstract member success: bool

[<AllowNullLiteral>]
type CleanSummary =
    abstract member dryRun: bool
    abstract member paths: string[]
    abstract member files: string[]
    abstract member folders: string[]

[<AllowNullLiteral>]
type CommitResultAuthor =
    abstract member email: string
    abstract member name: string

[<AllowNullLiteral>]
type CommitResultSummary =
    abstract member changes: int
    abstract member insertions: int
    abstract member deletions: int

[<AllowNullLiteral>]
type CommitResult =
    abstract member author: CommitResultAuthor option
    abstract member branch: string
    abstract member commit: string
    abstract member root: bool
    abstract member summary: CommitResultSummary

type ConfigScopes = System.Collections.Generic.Dictionary<string, string[]>

type ConfigValue = U2<string, string[]>

type ConfigValues = System.Collections.Generic.Dictionary<string, ConfigValue>

type ConfigFileValues = System.Collections.Generic.Dictionary<string, ConfigValues>

[<AllowNullLiteral>]
type ConfigGetResult =
    abstract member key: string
    abstract member value: string option
    abstract member values: string[]
    abstract member paths: string[]
    abstract member scopes: ConfigScopes

[<AllowNullLiteral>]
type ConfigListSummary =
    abstract member all: ConfigValues
    abstract member files: string[]
    abstract member values: ConfigFileValues

[<AllowNullLiteral>]
type DiffResultTextFile =
    abstract member file: string
    abstract member changes: int
    abstract member insertions: int
    abstract member deletions: int
    abstract member binary: bool

[<AllowNullLiteral>]
type DiffResultBinaryFile =
    abstract member file: string
    abstract member before: int
    abstract member after: int
    abstract member binary: bool

[<AllowNullLiteral>]
type DiffResultNameStatusFile =
    inherit DiffResultTextFile
    abstract member status: string option
    abstract member ``from``: string option
    abstract member similarity: float

[<AllowNullLiteral>]
type DiffResult =
    abstract member changed: int
    abstract member files: U3<DiffResultTextFile, DiffResultBinaryFile, DiffResultNameStatusFile>[]
    abstract member insertions: int
    abstract member deletions: int

[<AllowNullLiteral>]
type FetchResultBranch =
    abstract member name: string
    abstract member tracking: string

[<AllowNullLiteral>]
type FetchResultUpdate =
    abstract member name: string
    abstract member tracking: string
    abstract member ``to``: string
    abstract member ``from``: string

[<AllowNullLiteral>]
type FetchResultDeleted =
    abstract member tracking: string

[<AllowNullLiteral>]
type FetchResult =
    abstract member raw: string
    abstract member remote: string option
    abstract member branches: FetchResultBranch[]
    abstract member tags: FetchResultBranch[]
    abstract member updated: FetchResultUpdate[]
    abstract member deleted: FetchResultDeleted[]

[<AllowNullLiteral>]
type GrepResultLine =
    abstract member line: int
    abstract member path: string
    abstract member preview: string

[<AllowNullLiteral>]
type GrepResult =
    abstract member paths: System.Collections.Generic.HashSet<string>
    abstract member results: System.Collections.Generic.Dictionary<string, GrepResultLine[]>

[<AllowNullLiteral>]
type InitResult =
    abstract member bare: bool
    abstract member existing: bool
    abstract member path: string
    abstract member gitDir: string

[<AllowNullLiteral>]
type MoveResultItem =
    abstract member ``from``: string
    abstract member ``to``: string

[<AllowNullLiteral>]
type MoveResult =
    abstract member moves: MoveResultItem[]

[<AllowNullLiteral>]
type PullDetailSummary =
    abstract member changes: int
    abstract member insertions: int
    abstract member deletions: int

[<AllowNullLiteral>]
type RemoteMessagesObjectTotal =
    abstract member count: int
    abstract member delta: int

[<AllowNullLiteral>]
type RemoteMessagesObjectEnumeration =
    abstract member enumerating: int
    abstract member counting: int
    abstract member compressing: int
    abstract member total: RemoteMessagesObjectTotal
    abstract member reused: RemoteMessagesObjectTotal
    abstract member packReused: int

[<AllowNullLiteral>]
type RemoteMessages =
    abstract member all: string[]
    abstract member objects: RemoteMessagesObjectEnumeration option

type PullDetailFileChanges = System.Collections.Generic.Dictionary<string, int>

[<AllowNullLiteral>]
type PullResult =
    abstract member files: string[]
    abstract member insertions: PullDetailFileChanges
    abstract member deletions: PullDetailFileChanges
    abstract member summary: PullDetailSummary
    abstract member created: string[]
    abstract member deleted: string[]
    abstract member remoteMessages: RemoteMessages

[<AllowNullLiteral>]
type TagResult =
    abstract member all: string[]
    abstract member latest: string option

[<AllowNullLiteral>]
type ListLogLine =
    abstract member diff: DiffResult option

[<AllowNullLiteral>]
type DefaultLogFields =
    abstract member hash: string
    abstract member date: string
    abstract member message: string
    abstract member refs: string
    abstract member body: string
    abstract member author_name: string
    abstract member author_email: string

[<AllowNullLiteral>]
type LogResultLine =
    inherit DefaultLogFields
    inherit ListLogLine

[<AllowNullLiteral>]
type LogResult =
    abstract member all: LogResultLine[]
    abstract member total: int
    abstract member latest: LogResultLine option

[<AllowNullLiteral>]
type MergeConflict =
    abstract member reason: string
    abstract member file: string option
    abstract member meta: obj option

[<AllowNullLiteral>]
type MergeResult =
    inherit PullResult
    abstract member conflicts: MergeConflict[]
    abstract member merges: string[]
    abstract member result: string
    abstract member failed: bool

[<AllowNullLiteral>]
type PushResultPushedItem =
    abstract member local: string
    abstract member remote: string
    abstract member deleted: bool
    abstract member tag: bool
    abstract member branch: bool
    abstract member ``new``: bool
    abstract member alreadyUpdated: bool

[<AllowNullLiteral>]
type PushResultRef =
    abstract member local: string

[<AllowNullLiteral>]
type PushResultBranch =
    abstract member local: string
    abstract member remote: string
    abstract member remoteName: string

[<AllowNullLiteral>]
type PushResultBranchHead =
    abstract member local: string
    abstract member remote: string

[<AllowNullLiteral>]
type PushResultBranchHash =
    abstract member ``from``: string
    abstract member ``to``: string

[<AllowNullLiteral>]
type PushResultBranchUpdate =
    abstract member head: PushResultBranchHead
    abstract member hash: PushResultBranchHash

[<AllowNullLiteral>]
type PushResultRemoteVulnerabilities =
    abstract member count: int
    abstract member summary: string
    abstract member url: string

[<AllowNullLiteral>]
type PushResultRemoteMessages =
    inherit RemoteMessages
    abstract member pullRequestUrl: string option
    abstract member vulnerabilities: PushResultRemoteVulnerabilities option

[<AllowNullLiteral>]
type PushResult =
    abstract member pushed: PushResultPushedItem[]
    abstract member repo: string option
    abstract member ref: PushResultRef option
    abstract member branch: PushResultBranch option
    abstract member update: PushResultBranchUpdate option
    abstract member remoteMessages: PushResultRemoteMessages

[<AllowNullLiteral>]
type RemoteWithoutRefs =
    abstract member name: string

[<AllowNullLiteral>]
type RemoteRefs =
    abstract member fetch: string
    abstract member push: string

[<AllowNullLiteral>]
type RemoteWithRefs =
    abstract member name: string
    abstract member refs: RemoteRefs

[<AllowNullLiteral>]
type CwdDirectory =
    abstract member path: string
    abstract member root: bool option

[<AllowNullLiteral>]
type SimpleGitCompletionOptions
    [<ParamObject>]
    (
        ?onClose: U2<bool, int>,
        ?onExit: U2<bool, int>
    ) =
    member val onClose: U2<bool, int> option = onClose with get, set
    member val onExit: U2<bool, int> option = onExit with get, set

[<AllowNullLiteral>]
type SimpleGitSpawnOptions
    [<ParamObject>]
    (
        ?uid: int,
        ?gid: int
    ) =
    member val uid: int option = uid with get, set
    member val gid: int option = gid with get, set

[<AllowNullLiteral>]
type SimpleGitOptions
    [<ParamObject>]
    (
        ?baseDir: string,
        ?binary: SimpleGitBinary,
        ?maxConcurrentProcesses: int,
        ?trimmed: bool,
        ?config: string[],
        ?abort: IAbortSignal,
        ?progress: SimpleGitProgressHandler,
        ?errors: SimpleGitErrorsHandler,
        ?completion: SimpleGitCompletionOptions,
        ?timeout: SimpleGitTimeoutOptions,
        ?spawnOptions: SimpleGitSpawnOptions,
        ?``unsafe``: SimpleGitUnsafeOptions
    ) =
    member val baseDir: string option = baseDir with get, set
    member val binary: SimpleGitBinary option = binary with get, set
    member val maxConcurrentProcesses: int option = maxConcurrentProcesses with get, set
    member val trimmed: bool option = trimmed with get, set
    member val config: string[] option = config with get, set
    member val abort: IAbortSignal option = abort with get, set
    member val progress: SimpleGitProgressHandler option = progress with get, set
    member val errors: SimpleGitErrorsHandler option = errors with get, set
    member val completion: SimpleGitCompletionOptions option = completion with get, set
    member val timeout: SimpleGitTimeoutOptions option = timeout with get, set
    member val spawnOptions: SimpleGitSpawnOptions option = spawnOptions with get, set
    member val ``unsafe``: SimpleGitUnsafeOptions option = ``unsafe`` with get, set

type ISimpleGit =
    // SimpleGitBase
    abstract member add: files: U2<string, string[]> -> Promise<string>
    abstract member add: files: string -> Promise<string>
    abstract member add: files: string[] -> Promise<string>
    abstract member cwd: directory: CwdDirectory -> Promise<string>
    abstract member cwd: directory: string -> Promise<string>
    abstract member hashObject: path: string * ?write: bool -> Promise<string>
    abstract member init: ?options: TaskOptions -> Promise<InitResult>
    abstract member init: bare: bool * ?options: TaskOptions -> Promise<InitResult>
    abstract member merge: options: TaskOptions -> Promise<MergeResult>
    abstract member mergeFromTo: remote: string * branch: string * ?options: TaskOptions -> Promise<MergeResult>
    abstract member outputHandler: handler: obj -> ISimpleGit
    abstract member push: ?options: TaskOptions -> Promise<PushResult>
    abstract member push: remote: string * branch: string * ?options: TaskOptions -> Promise<PushResult>
    abstract member stash: ?options: TaskOptions -> Promise<string>
    abstract member status: ?options: TaskOptions -> Promise<StatusResult>

    // SimpleGit
    abstract member addAnnotatedTag: tagName: string * tagMessage: string -> Promise<obj>
    abstract member addConfig: key: string * value: string * ?append: bool * ?scope: GitConfigScope -> Promise<string>
    abstract member applyPatch: patches: U2<string, string[]> * ?options: TaskOptions -> Promise<string>
    abstract member listConfig: ?scope: GitConfigScope -> Promise<ConfigListSummary>
    abstract member addRemote: remoteName: string * remoteRepo: string * ?options: TaskOptions -> Promise<string>
    abstract member addTag: name: string -> Promise<obj>
    abstract member binaryCatFile: options: string[] -> Promise<obj>
    abstract member branch: ?options: TaskOptions -> Promise<BranchSummary>
    abstract member branchLocal: unit -> Promise<BranchSummary>
    abstract member catFile: ?options: string[] -> Promise<string>
    abstract member checkIgnore: pathNames: U2<string, string[]> -> Promise<string[]>
    abstract member checkIsRepo: ?action: CheckRepoAction -> Promise<bool>
    abstract member checkout: what: string * ?options: TaskOptions -> Promise<string>
    abstract member checkout: ?options: TaskOptions -> Promise<string>
    abstract member checkoutBranch: branchName: string * startPoint: string * ?options: TaskOptions -> Promise<unit>
    abstract member checkoutLatestTag: branchName: string * startPoint: string -> Promise<unit>
    abstract member checkoutLocalBranch: branchName: string * ?options: TaskOptions -> Promise<unit>
    abstract member clean: args: string[] * ?options: TaskOptions -> Promise<CleanSummary>
    abstract member clean: mode: string * ?options: TaskOptions -> Promise<CleanSummary>
    abstract member clean: ?options: TaskOptions -> Promise<CleanSummary>
    [<System.Obsolete("Deprecated upstream. Removed in v2; prefer abort-plugin configuration for pending task cancellation.")>]
    abstract member clearQueue: unit -> ISimpleGit
    abstract member clone: repoPath: string * ?localPath: string * ?options: TaskOptions -> Promise<string>
    abstract member commit: message: string * ?files: U2<string, string[]> * ?options: GitOptions -> Promise<CommitResult>
    abstract member commit: message: string[] * ?files: U2<string, string[]> * ?options: GitOptions -> Promise<CommitResult>
    abstract member countObjects: unit -> Promise<CountObjectsResult>
    abstract member customBinary: command: U2<string, string[]> -> ISimpleGit
    abstract member deleteLocalBranch: branchName: string * ?forceDelete: bool -> Promise<BranchSingleDeleteResult>
    abstract member deleteLocalBranches: branchNames: string[] * ?forceDelete: bool -> Promise<BranchMultiDeleteResult>
    abstract member diff: ?options: TaskOptions -> Promise<string>
    abstract member diffSummary: ?options: TaskOptions -> Promise<DiffResult>
    abstract member diffSummary: command: U2<string, float> * ?options: TaskOptions -> Promise<DiffResult>
    abstract member env: name: string * value: string -> ISimpleGit
    abstract member env: env: obj -> ISimpleGit
    abstract member exec: handle: (unit -> unit) -> Promise<unit>
    abstract member fetch: ?options: TaskOptions -> Promise<FetchResult>
    abstract member fetch: remote: string * ?options: TaskOptions -> Promise<FetchResult>
    abstract member fetch: remote: string * branch: string * ?options: TaskOptions -> Promise<FetchResult>
    abstract member firstCommit: unit -> Promise<string>
    abstract member getConfig: key: string * ?scope: GitConfigScope -> Promise<ConfigGetResult>
    abstract member getRemotes: ?verbose: bool -> Promise<U2<RemoteWithoutRefs[], RemoteWithRefs[]>>
    abstract member grep: searchTerm: U2<string, GitGrepQuery> * ?options: TaskOptions -> Promise<GrepResult>
    abstract member listRemote: ?args: TaskOptions -> Promise<string>
    abstract member log: ?options: U2<TaskOptions, LogOptions> -> Promise<LogResult>
    abstract member mirror: repoPath: string * localPath: string -> Promise<string>
    abstract member mv: ``from``: U2<string, string[]> * toPath: string -> Promise<MoveResult>
    abstract member pull: ?options: TaskOptions -> Promise<PullResult>
    abstract member pull: remote: string * ?options: TaskOptions -> Promise<PullResult>
    abstract member pull: remote: string * branch: string * ?options: TaskOptions -> Promise<PullResult>
    abstract member pushTags: ?options: TaskOptions -> Promise<PushResult>
    abstract member pushTags: remote: string * ?options: TaskOptions -> Promise<PushResult>
    abstract member raw: command: string -> Promise<string>
    abstract member raw: commands: string[] -> Promise<string>
    abstract member raw: options: TaskOptions -> Promise<string>
    abstract member raw: a: string * options: TaskOptions -> Promise<string>
    abstract member raw: a: string * b: string * options: TaskOptions -> Promise<string>
    abstract member raw: a: string * b: string * c: string * options: TaskOptions -> Promise<string>
    abstract member raw: a: string * b: string * c: string * d: string * options: TaskOptions -> Promise<string>
    abstract member raw: a: string * b: string * c: string * d: string * e: string * options: TaskOptions -> Promise<string>
    abstract member raw: a: string * b: string -> Promise<string>
    abstract member raw: a: string * b: string * c: string -> Promise<string>
    abstract member raw: a: string * b: string * c: string * d: string -> Promise<string>
    abstract member raw: a: string * b: string * c: string * d: string * e: string -> Promise<string>
    abstract member rebase: ?options: TaskOptions -> Promise<string>
    abstract member remote: options: string[] -> Promise<U2<unit, string>>
    abstract member removeRemote: remoteName: string -> Promise<unit>
    abstract member reset: ?options: TaskOptions -> Promise<string>
    abstract member reset: mode: ResetMode * ?options: TaskOptions -> Promise<string>
    abstract member revert: commit: string * ?options: TaskOptions -> Promise<unit>
    abstract member revparse: ?options: TaskOptions -> Promise<string>
    abstract member revparse: option: string * ?options: TaskOptions -> Promise<string>
    abstract member rm: paths: U2<string, string[]> -> Promise<unit>
    abstract member rmKeepLocal: paths: U2<string, string[]> -> Promise<unit>
    abstract member show: ?option: U2<string, TaskOptions> -> Promise<string>
    abstract member showBuffer: option: U2<string, TaskOptions> -> Promise<obj>
    abstract member silent: ?silence: bool -> ISimpleGit
    abstract member stashList: ?options: TaskOptions -> Promise<LogResult>
    abstract member subModule: ?options: TaskOptions -> Promise<string>
    abstract member submoduleAdd: repo: string * path: string -> Promise<string>
    abstract member submoduleInit: ?moduleName: string * ?options: TaskOptions -> Promise<string>
    abstract member submoduleUpdate: ?moduleName: string * ?options: TaskOptions -> Promise<string>
    abstract member tag: ?options: TaskOptions -> Promise<string>
    abstract member tags: ?options: TaskOptions -> Promise<TagResult>
    abstract member updateServerInfo: unit -> Promise<string>
    abstract member version: unit -> Promise<VersionResult>

[<Erase>]
type AbortController =
    [<Emit("new AbortController()")>]
    static member create (): IAbortController = jsNative

[<Erase>]
type SimpleGit =
    [<Import("simpleGit", "simple-git")>]
    static member create (): ISimpleGit = jsNative

    [<Import("simpleGit", "simple-git")>]
    static member create(baseDir: string) : ISimpleGit = jsNative

    [<Import("simpleGit", "simple-git")>]
    static member create(options: SimpleGitOptions) : ISimpleGit = jsNative

    [<Import("simpleGit", "simple-git")>]
    static member create(baseDir: string, options: SimpleGitOptions) : ISimpleGit = jsNative
