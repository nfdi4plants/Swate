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
open Swate.Components.Page.GitComparison.GitPagedDiffTypes

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
let private maxPatchBytes = 8 * 1024 * 1024
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
            }
        )

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

        return {
            BranchName = branch
            HeadRevision = head
            Commits = commits |> Array.truncate pageSize
            NextSkip =
                if commits.Length > pageSize then
                    Some(request.Skip + pageSize)
                else
                    None
        }
}

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

let private unavailableBlob root revision path = promise {
    let objectName = revision + ":" + path
    let! kind = run root [| "cat-file"; "-t"; objectName |]

    if kind.Trim() <> "blob" then
        return Some "This change contains a Git submodule or another non-text object."
    else
        let! size = run root [| "cat-file"; "-s"; objectName |]

        if Double.Parse(size.Trim()) > float maxOutputBytes then
            return
                Some "This committed file exceeds the 16 MiB inspection limit; its historical text diff is unavailable."
        else
            let! bytes = runRaw root maxOutputBytes [| "cat-file"; "blob"; objectName |]

            let content =
                Main.Bindings.Node.bufferSubarray bytes 0 4096
                |> Main.Bindings.Node.bufferToUtf8String

            if content.StartsWith("version https://git-lfs.github.com/spec/v1") then
                return
                    Some
                        "This revision stores a Git LFS pointer. Historical research-file contents are unavailable in this viewer."
            else
                return None
}

let private hunkPattern =
    Regex("^@@ -([0-9]+)(?:,([0-9]+))? \\+([0-9]+)(?:,([0-9]+))? @@")

let private parsePatch (patch: string) =
    let parts = ResizeArray<PagedPart>()
    let rows = ResizeArray<PagedRow>()
    let removed = ResizeArray<PagedLine>()
    let added = ResizeArray<PagedLine>()
    let mutable hunkId = ""
    let mutable previousRange = { Start = 0.; Count = 0. }
    let mutable currentRange = { Start = 0.; Count = 0. }
    let mutable previousNumber = 0.
    let mutable currentNumber = 0.
    let mutable lastSide = ' '

    let line number changed (content: string) =
        let crlf = content.EndsWith('\r')

        let text =
            if crlf then
                content.Substring(0, content.Length - 1)
            else
                content

        {
            Number = number
            Ending = if crlf then PagedLineEnding.CRLF else PagedLineEnding.LF
            OffsetUtf16 = 0.
            TotalUtf16 = Some(float text.Length)
            Text = text
            Highlights =
                if text.Length = 0 then
                    [||]
                else
                    [|
                        {
                            Start = 0
                            Length = text.Length
                            Changed = changed
                        }
                    |]
        }

    let addRow kind previous current =
        rows.Add {
            Id = hunkId + ":" + string rows.Count
            Kind = kind
            Previous = previous
            Current = current
        }

    let flushChanges () =
        for index in 0 .. max removed.Count added.Count - 1 do
            let previous = if index < removed.Count then Some removed[index] else None
            let current = if index < added.Count then Some added[index] else None

            let kind =
                match previous, current with
                | Some _, Some _ -> PagedRowKind.Replaced
                | Some _, None -> PagedRowKind.Removed
                | _ -> PagedRowKind.Added

            addRow kind previous current

        removed.Clear()
        added.Clear()

    let flushHunk () =
        flushChanges ()

        if hunkId <> "" then
            parts.Add(HunkRows(hunkId, previousRange, currentRange, true, true, rows.ToArray()))

        rows.Clear()

    let noNewline item = {
        item with
            Ending =
                if item.Ending = PagedLineEnding.CRLF then
                    PagedLineEnding.CR
                else
                    PagedLineEnding.NoEnding
    }

    for content in patch.Split('\n') do
        let header = hunkPattern.Match content

        if header.Success then
            flushHunk ()
            hunkId <- "history-hunk-" + string parts.Count

            let count (index: int) =
                if header.Groups[index].Success then
                    Double.Parse header.Groups[index].Value
                else
                    1.
            // Git patches use one-based line numbers; the viewer uses zero-based
            // positions and adds one when displaying a nonempty range.
            let range (startIndex: int) (countIndex: int) : PagedRange =
                let length = count countIndex
                let start = Double.Parse header.Groups[startIndex].Value

                {
                    Start = (if length = 0.0 then start else start - 1.0)
                    Count = length
                }

            previousRange <- range 1 2
            currentRange <- range 3 4
            previousNumber <- previousRange.Start
            currentNumber <- currentRange.Start
            lastSide <- ' '
        elif hunkId <> "" && content.StartsWith("\\ No newline at end of file") then
            match lastSide with
            | '-' when removed.Count > 0 -> removed[removed.Count - 1] <- noNewline removed[removed.Count - 1]
            | '+' when added.Count > 0 -> added[added.Count - 1] <- noNewline added[added.Count - 1]
            | ' ' when rows.Count > 0 ->
                let row = rows[rows.Count - 1]

                rows[rows.Count - 1] <- {
                    row with
                        Previous = row.Previous |> Option.map noNewline
                        Current = row.Current |> Option.map noNewline
                }
            | _ -> ()
        elif hunkId <> "" && content.Length > 0 then
            match content[0] with
            | '-' ->
                removed.Add(line previousNumber true (content.Substring 1))
                previousNumber <- previousNumber + 1.
                lastSide <- '-'
            | '+' ->
                added.Add(line currentNumber true (content.Substring 1))
                currentNumber <- currentNumber + 1.
                lastSide <- '+'
            | ' ' ->
                flushChanges ()

                addRow
                    PagedRowKind.Context
                    (Some(line previousNumber false (content.Substring 1)))
                    (Some(line currentNumber false (content.Substring 1)))

                previousNumber <- previousNumber + 1.
                currentNumber <- currentNumber + 1.
                lastSide <- ' '
            | _ -> ()

    flushHunk ()
    parts.ToArray()

let private loadDiff root revision path = promise {
    if not (validPath path) then
        failwith "Select a valid repository-relative changed path."

    let! parent, changes = loadChanges root revision
    let change = changes |> Array.tryFind (fun change -> change.Path = path)

    match change with
    | None -> return failwith "This path is not a changed file in the selected commit."
    | Some change ->
        let result parts blocked = {
            Revision = revision
            ParentRevision = parent
            Path = change.Path
            PreviousPath = change.PreviousPath
            Kind = change.Kind
            Parts = parts
            BlockedReason = blocked
        }

        let! blocked = promise {
            if change.Path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) then
                return Some "Excel workbooks are binary research files; a text diff is unavailable."
            elif change.Insertions.IsNone || change.Deletions.IsNone then
                return Some "This commit contains binary file content; a text diff is unavailable."
            else
                let! current =
                    if change.Kind = GitHistoryChangeKind.Deleted then
                        Promise.lift None
                    else
                        unavailableBlob root revision change.Path

                match current, parent with
                | Some reason, _ -> return Some reason
                | None, Some previous when change.Kind <> GitHistoryChangeKind.Added ->
                    return! unavailableBlob root previous (change.PreviousPath |> Option.defaultValue change.Path)
                | _ -> return None
        }

        match blocked with
        | Some reason -> return result [||] (Some reason)
        | None ->
            try
                let patchOptions = [|
                    "--patch"
                    "--unified=3"
                    "--diff-algorithm=myers"
                    "--no-indent-heuristic"
                |]

                let! args = promise {
                    match parent with
                    | Some previous when
                        change.Kind <> GitHistoryChangeKind.Added
                        && change.Kind <> GitHistoryChangeKind.Deleted
                        ->
                        // Compare the selected file's blobs directly. Including both
                        // paths in a tree diff could also include unrelated changes
                        // to a copy source, or lose rename identity after filtering.
                        let oldPath = change.PreviousPath |> Option.defaultValue change.Path
                        let! oldObject = run root [| "rev-parse"; "--verify"; previous + ":" + oldPath |]
                        let! newObject = run root [| "rev-parse"; "--verify"; revision + ":" + change.Path |]
                        let oldHash, newHash = oldObject.Trim(), newObject.Trim()

                        if not (validHash oldHash && validHash newHash) then
                            failwith "The selected file objects are unavailable."

                        return
                            Array.concat [|
                                [| "diff"; "--no-ext-diff"; "--no-textconv"; "--no-color" |]
                                patchOptions
                                [| oldHash; newHash; "--" |]
                            |]
                    | _ -> return Array.append (diffArguments revision parent patchOptions) [| change.Path |]
                }

                let! bytes = runRaw root maxPatchBytes args
                let patch = decodeUtf8 bytes

                let binaryMetadata =
                    patch.Split('\n')
                    |> Array.takeWhile (fun line -> not (line.StartsWith("@@ ")))
                    |> Array.exists (fun line -> line = "GIT binary patch" || line.StartsWith("Binary files "))

                if binaryMetadata then
                    return result [||] (Some "This commit contains binary file content; a text diff is unavailable.")
                else
                    return result (parsePatch patch) None
            with _ ->
                return
                    result
                        [||]
                        (Some
                            "The committed patch could not be displayed. It may exceed the 8 MiB limit, use a non-UTF-8 encoding, or Git may be unavailable.")
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
