module Main.IPC.GitHistory

open System
open System.Collections.Generic
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Fable.Electron
open Fable.Electron.Main
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.DTOs.GitHistoryDto
open Swate.Components.Page.GitHistory.Types

// These bindings deliberately live here: committed history has no dependency on
// the workspace Git service and cannot write its index, worktree or references.
[<AllowNullLiteral>]
type private ExecError =
    abstract message: string
    abstract code: U2<string, int> option
    abstract killed: bool

// Anonymous records become native objects without a generated constructor.
type private ExecOptions = {|
    cwd: string
    windowsHide: bool
    shell: bool
    timeout: int
    maxBuffer: int
    encoding: string
    env: obj
|}

[<Import("execFile", "node:child_process")>]
let private execFile
    (file: string)
    (arguments: string[])
    (options: ExecOptions)
    (callback: ExecError -> obj -> obj -> unit)
    : obj =
    jsNative

// Git for Windows accepts NUL, but rejects Node's device path (\\.\nul).
let private gitNullFile () =
    if Main.Bindings.Node.processPlatform () = "win32" then
        "NUL"
    else
        "/dev/null"

// Ignore inherited Git directory/config overrides, while retaining PATH and the
// normal operating-system environment needed to find the installed Git binary.
[<Emit("(() => { const env = {...process.env}; for (const key of Object.keys(env)) { if (key.startsWith('GIT_')) delete env[key]; } env.GIT_OPTIONAL_LOCKS = '0'; env.GIT_NO_LAZY_FETCH = '1'; env.GIT_CONFIG_NOSYSTEM = '1'; env.GIT_CONFIG_GLOBAL = $0; env.GIT_TERMINAL_PROMPT = '0'; return env; })()")>]
let private readOnlyEnvironment (nullFile: string) : obj = jsNative

[<Emit("new TextDecoder('utf-8', {fatal: true}).decode($0)")>]
let private decodeUtf8 (bytes: obj) : string = jsNative

let private maxOutputBytes = 16 * 1024 * 1024
let private hashPattern = Regex("^(?:[0-9a-fA-F]{40}|[0-9a-fA-F]{64})$")

let private validHash (value: string) =
    not (isNull value)
    && (value.Length = 40 || value.Length = 64)
    && hashPattern.IsMatch value

[<Emit("Number.isSafeInteger($0)")>]
let private safeInteger (value: int) : bool = jsNative

let private executionError (error: ExecError) (stderr: obj) =
    let details =
        try
            decodeUtf8 stderr |> fun value -> value.Trim()
        with _ ->
            ""

    match error.code with
    | Some(U2.Case1 "ENOENT") -> "Git is unavailable. Install Git and reopen Swate to view history."
    | Some(U2.Case1 "ERR_CHILD_PROCESS_STDIO_MAXBUFFER") ->
        "Git history exceeds the bounded output limit. Select a smaller commit or file."
    | _ when error.killed -> "Git took longer than 30 seconds to read history. Retry or check the repository."
    | _ when details.Contains("not a git repository") -> "This ARC folder is not a Git repository."
    | _ when not (String.IsNullOrWhiteSpace details) ->
        "Git could not read history. "
        + (if details.Length > 1000 then
               details.Substring(0, 1000)
           else
               details)
    | _ -> "Git could not read the requested history. The commit may be unavailable or the repository may be damaged."

let private runRaw root limit arguments : JS.Promise<obj> =
    Promise.create (fun resolve reject ->
        let nullFile = gitNullFile ()

        let options: ExecOptions = {|
            cwd = root
            windowsHide = true
            shell = false
            timeout = 30000
            maxBuffer = limit
            encoding = "buffer"
            env = readOnlyEnvironment nullFile
        |}

        let args =
            Array.append
                [|
                    "--no-optional-locks"
                    "--no-replace-objects"
                    "--literal-pathspecs"
                    "-c"
                    "core.fsmonitor=false"
                    "-c"
                    "core.hooksPath=" + nullFile
                    "-c"
                    "diff.external="
                    "-c"
                    "core.pager=cat"
                |]
                arguments

        execFile
            "git"
            args
            options
            (fun error stdout stderr ->
                if isNull error then
                    resolve stdout
                else
                    reject (exn (executionError error stderr))
            )
        |> ignore
    )

let private run root arguments = promise {
    let! bytes = runRaw root maxOutputBytes arguments
    return decodeUtf8 bytes
}

let private canonicalPath path =
    let normalized = Main.Bindings.Path.resolve [| path |]

    if Main.Bindings.Node.processPlatform () = "win32" then
        normalized.ToLowerInvariant()
    else
        normalized

let private repositoryRoot (event: IpcMainInvokeEvent) = promise {
    match IPCHelper.tryGetVaultAndArcPath event with
    | Error _ -> return failwith "Open an ARC folder before viewing Git history."
    | Ok(_, arcPath) ->
        let! value = run arcPath [| "rev-parse"; "--show-toplevel" |]
        let root = value.TrimEnd([| '\r'; '\n' |])

        if canonicalPath root <> canonicalPath arcPath then
            failwith "Git history is available only when the ARC folder is the repository root."

        return arcPath
}

let private verifyRevision root revision = promise {
    if not (validHash revision) then
        failwith "Select a valid full commit revision."

    let! verified = run root [| "rev-parse"; "--verify"; revision + "^{commit}" |]
    let value = verified.Trim()

    if not (validHash value) then
        failwith "The selected commit is unavailable."

    return value
}

let private parentRevision root revision = promise {
    let! output = run root [| "rev-parse"; revision + "^@" |]

    return
        output.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryHead
}

let private branchAndHead root = promise {
    // rev-parse cannot resolve HEAD in an unborn repository. Read the Git-owned
    // HEAD path (also valid for linked worktrees) to retain its branch name.
    let! headPath = run root [| "rev-parse"; "--git-path"; "HEAD" |]

    let! headContents =
        Main.Bindings.Filesystem.readFileAsync
            (Main.Bindings.Path.resolve [| root; headPath.TrimEnd([| '\r'; '\n' |]) |])
            Main.Bindings.Filesystem.TextEncoding.Utf8

    let symbolic =
        if headContents.Trim().StartsWith("ref: refs/heads/") then
            Some(headContents.Trim().Substring(5))
        else
            None

    let branch =
        symbolic |> Option.map (fun value -> value.Substring("refs/heads/".Length))

    let! head = promise {
        try
            let! value = run root [| "rev-parse"; "--verify"; "HEAD^{commit}" |]
            return Some(value.Trim())
        with _ ->
            match symbolic with
            | None -> return failwith "The repository HEAD is unavailable."
            | Some reference ->
                let! refPath = run root [| "rev-parse"; "--git-path"; reference |]
                let! packedPath = run root [| "rev-parse"; "--git-path"; "packed-refs" |]

                let looseFile =
                    Main.Bindings.Path.resolve [| root; refPath.TrimEnd([| '\r'; '\n' |]) |]

                let packedFile =
                    Main.Bindings.Path.resolve [| root; packedPath.TrimEnd([| '\r'; '\n' |]) |]

                let! packed =
                    if Main.Bindings.Filesystem.existsSync packedFile then
                        Main.Bindings.Filesystem.readFileAsync packedFile Main.Bindings.Filesystem.TextEncoding.Utf8
                    else
                        Promise.lift ""

                let hasPackedRef =
                    packed.Split('\n')
                    |> Array.exists (fun line -> line.TrimEnd('\r').EndsWith(" " + reference))

                if Main.Bindings.Filesystem.existsSync looseFile || hasPackedRef then
                    return failwith "The repository HEAD cannot be read. Its branch reference may be damaged."
                else
                    return None
    }

    return branch, head
}

let private parseCommits (output: string) =
    let fields = output.Split('\000')

    let count =
        if fields.Length > 0 && fields[fields.Length - 1] = "" then
            fields.Length - 1
        else
            fields.Length

    if count % 7 <> 0 then
        failwith "Git returned incomplete commit metadata."

    Array.init
        (count / 7)
        (fun index ->
            let offset = index * 7

            {
                Revision = fields[offset]
                ParentRevisions = fields[offset + 1].Split(' ', StringSplitOptions.RemoveEmptyEntries)
                Message = fields[offset + 2]
                AuthorName = fields[offset + 3]
                AuthorEmail = fields[offset + 4]
                AuthoredAt = fields[offset + 5]
                CommittedAt = fields[offset + 6]
                Summary = None
            }
        )

let private changeKind (status: string) =
    match status[0] with
    | 'A' -> GitHistoryChangeKind.Added
    | 'D' -> GitHistoryChangeKind.Deleted
    | 'M' -> GitHistoryChangeKind.Modified
    | 'R' -> GitHistoryChangeKind.Renamed
    | 'C' -> GitHistoryChangeKind.Copied
    | 'T' -> GitHistoryChangeKind.TypeChanged
    | _ -> failwith "Git returned an unsupported file change."

let private parseChanges (output: string) =
    let tokens = output.Split('\000')
    let changes = ResizeArray<GitHistoryFileChange>()
    let mutable index = 0

    while index < tokens.Length && tokens[index] <> "" do
        let kind = changeKind tokens[index]

        if index + 1 >= tokens.Length then
            failwith "Git returned incomplete changed paths."

        let oldPath = tokens[index + 1]

        let moved =
            kind = GitHistoryChangeKind.Renamed || kind = GitHistoryChangeKind.Copied

        if moved && index + 2 >= tokens.Length then
            failwith "Git returned an incomplete renamed path."

        changes.Add {
            Path = if moved then tokens[index + 2] else oldPath
            PreviousPath = if moved then Some oldPath else None
            Kind = kind
            Insertions = None
            Deletions = None
        }

        index <- index + (if moved then 3 else 2)

    changes.ToArray()

let private parseStats (output: string) =
    let stats = Dictionary<string, int option * int option>()
    let tokens = output.Split('\000')
    let mutable index = 0

    while index < tokens.Length && tokens[index] <> "" do
        let token = tokens[index]
        let firstTab = token.IndexOf('\t')

        let secondTab =
            if firstTab < 0 then
                -1
            else
                token.IndexOf('\t', firstTab + 1)

        if firstTab < 0 || secondTab < 0 then
            failwith "Git returned incomplete change statistics."

        let number text =
            if text = "-" then None else Some(Int32.Parse text)

        let additions = number (token.Substring(0, firstTab))
        let deletions = number (token.Substring(firstTab + 1, secondTab - firstTab - 1))
        let path = token.Substring(secondTab + 1)

        if path = "" then
            if index + 2 >= tokens.Length then
                failwith "Git returned incomplete rename statistics."

            stats[tokens[index + 2]] <- additions, deletions
            index <- index + 3
        else
            stats[path] <- additions, deletions
            index <- index + 1

    stats

let private diffArguments revision parent options =
    let revisions =
        match parent with
        | Some previous -> [| previous; revision |]
        | None -> [| revision |]

    Array.concat [|
        [|
            "diff-tree"
            "--no-commit-id"
            "-r"
            "--root"
            "--no-ext-diff"
            "--no-textconv"
            "--no-color"
            "-M"
            "-C"
        |]
        options
        revisions
        [| "--" |]
    |]

let private loadSummary root (commit: GitHistoryCommit) = promise {
    try
        let parent = Array.tryHead commit.ParentRevisions
        let! names = run root (diffArguments commit.Revision parent [| "--name-status"; "-z" |])

        return {
            commit with
                Summary = Some(parseChanges names |> GitHistoryChangeSummary.ofChanges)
        }
    with _ ->
        // One unavailable summary must not prevent browsing the remaining history.
        return commit
}

let private loadSummaries root (commits: GitHistoryCommit[]) = promise {
    let loaded = ResizeArray<GitHistoryCommit>()

    // Each summary makes one name-status read. Batches bound native Git reads to
    // four while preserving history order and leaving file details lazy.
    for batch in commits |> Array.chunkBySize 4 do
        let! summaries = batch |> Array.map (loadSummary root) |> Promise.all
        loaded.AddRange summaries

    return loaded.ToArray()
}

let private loadHistory root (request: GitHistoryRequest) = promise {
    if not (safeInteger request.Skip) || request.Skip < 0 || request.Skip > 1000000 then
        failwith "The requested history page is out of range."

    if not (safeInteger request.PageSize) then
        failwith "The requested history page size is invalid."

    let pageSize =
        if request.PageSize = 0 then
            30
        else
            max 1 (min 100 request.PageSize)

    let! branch, currentHead = branchAndHead root

    let! head = promise {
        match request.HeadRevision with
        | Some revision ->
            let! verified = verifyRevision root revision
            return Some verified
        | None -> return currentHead
    }

    match head with
    | None ->
        return {
            BranchName = branch
            HeadRevision = None
            Commits = [||]
            NextSkip = None
        }
    | Some revision ->
        let! output =
            run root [|
                "log"
                "--topo-order"
                "-z"
                "--no-show-signature"
                "--no-notes"
                "--format=%H%x00%P%x00%B%x00%an%x00%ae%x00%aI%x00%cI"
                "--skip=" + string request.Skip
                "--max-count=" + string (pageSize + 1)
                revision
                "--"
            |]

        let commits = parseCommits output
        let! visibleCommits = commits |> Array.truncate pageSize |> loadSummaries root

        return {
            BranchName = branch
            HeadRevision = head
            Commits = visibleCommits
            NextSkip =
                if commits.Length > pageSize then
                    Some(request.Skip + pageSize)
                else
                    None
        }
}

let private loadChanges root revision = promise {
    let! parent = parentRevision root revision
    let! names = run root (diffArguments revision parent [| "--name-status"; "-z" |])
    let! numbers = run root (diffArguments revision parent [| "--numstat"; "-z" |])
    let stats = parseStats numbers

    let changes =
        parseChanges names
        |> Array.map (fun change ->
            match stats.TryGetValue change.Path with
            | true, (insertions, deletions) -> {
                change with
                    Insertions = insertions
                    Deletions = deletions
              }
            | _ -> change
        )

    return parent, changes
}

let private validPath (path: string) =
    not (String.IsNullOrEmpty path)
    && not (path.Contains '\000')
    && not (Main.Bindings.Path.isAbsolute path)
    && not (
        path.Split('/')
        |> Array.exists (fun segment -> segment = ".." || segment = "." || segment = "")
    )

// History resolves immutable sources only. The shared text-diff service owns reading,
// classification, paging and recovery for both history and workspace comparisons.
let private loadDiff root revision path = promise {
    if not (validPath path) then
        failwith "Select a valid repository-relative changed path."

    let! parent = parentRevision root revision
    let! names = run root (diffArguments revision parent [| "--name-status"; "-z" |])
    let changes = parseChanges names

    match changes |> Array.tryFind (fun change -> change.Path = path) with
    | None -> return failwith "This path is not a changed file in the selected commit."
    | Some change ->
        return {
            Revision = revision
            ParentRevision = parent
            Path = change.Path
            PreviousPath = change.PreviousPath
            Kind = change.Kind
        }
}

let private protect operation = promise {
    try
        let! value = operation ()
        return Ok value
    with error ->
        let message = error.Message

        return
            Error(
                if String.IsNullOrWhiteSpace message then
                    "Git history could not be loaded."
                else
                    message
            )
}

/// Resolves a selection against the captured workspace of a tracked diff operation.
let prepareDiff root (request: GitHistoryDiffRequest) =
    protect (fun () -> promise {
        let! revision = verifyRevision root request.Revision
        return! loadDiff root revision request.Path
    })

let api (event: IpcMainInvokeEvent) : IGitHistoryApi = {
    listHistory =
        fun request ->
            protect (fun () -> promise {
                let! root = repositoryRoot event
                return! loadHistory root request
            })
    listChanges =
        fun request ->
            protect (fun () -> promise {
                let! root = repositoryRoot event
                let! revision = verifyRevision root request.Revision
                let! _, changes = loadChanges root revision
                return changes
            })
    openDiff =
        fun request ->
            protect (fun () -> promise {
                let! root = repositoryRoot event
                let! revision = verifyRevision root request.Revision
                return! loadDiff root revision request.Path
            })
}
