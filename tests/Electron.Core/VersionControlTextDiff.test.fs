module ElectronCore.VersionControlTextDiffTests

open Fable.Core
open Main.VersionControl
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions
open Vitest

module TextDiffProtocol = VersionControlService.Git.TextDiff.TextDiffProtocol

/// The worker limit of a page or an expansion reply, measured on the worker message.
[<Literal>]
let private PageLimitBytes = 524288

/// The worker limit of a line slice reply.
[<Literal>]
let private LineLimitBytes = 65536

[<Literal>]
let private IpcMarginBytes = 8192

[<Emit("Buffer.byteLength(JSON.stringify($0), 'utf8')")>]
let private jsonBytes (_value: obj) : int = jsNative

[<Emit("structuredClone($0)")>]
let private structuredCloneOf (_value: obj) : obj = jsNative

/// UTF-8 bytes of the JSON of an IPC reply. Electron sends a structured clone, which
/// drops the toJSON of unions, so both shapes are measured.
let private ipcPayloadBytes (payload: Result<OperationResultDto<'T>, exn>) : int =
    max (jsonBytes (box payload)) (jsonBytes (structuredCloneOf (box payload)))

let private reply (mapValue: 'T -> 'U) (value: 'T) : Result<OperationResultDto<'U>, exn> =
    Ok(Mappings.textDiffResult mapValue (OperationResult.succeeded value))

let private workerBytes (payload: TextDiffProtocol.ResultPayload) : int =
    TextDiffProtocol.TextDiffMessage.Result("0f8fad5b-d9cb-469f-a165-70867728950e", 1, payload)
    |> TextDiffProtocol.encode
    |> TextDiffProtocol.envelopeByteLength

/// The largest count of repeated units whose worker message stays within the limit. The
/// linear estimate can overshoot (separators, growing digit counts), so it steps back.
let private fillTo (limit: int) (sizeWith: int -> int) : int =
    let empty = sizeWith 0
    let perUnit = sizeWith 1 - empty
    let mutable count = max 0 ((limit - empty) / perUnit)

    while count > 0 && sizeWith count > limit do
        count <- count - 1

    count

// Long line numbers and the pending preview leave room for fewer rows than the row limit
// of 1,000 before the byte limit is reached.
[<Literal>]
let private PageRows = 600

// Line numbers above 2^53 make every int64 as long as it gets in both encodings.
let private largeNumber = 9_000_000_000_000_000_000L

// JSON escapes a control character as six bytes, the most one UTF-16 unit can cost.
let private expensiveText (length: int) = String.replicate length "\u0001"

let private token (prefix: string) =
    prefix + String.replicate (64 - prefix.Length) "0"

let private highlights (count: int) : Highlight[] =
    Array.init
        count
        (fun index -> {
            Start = index * 2
            Length = 1
            Kind =
                if index % 2 = 0 then
                    HighlightKind.ChangedText
                else
                    HighlightKind.UnchangedText
        })

let private line (number: int64) (text: string) (highlightCount: int) : DiffLine = {
    Number = number
    Ending = LineEnding.CRLF
    Slice = {
        OffsetUtf16 = largeNumber
        TotalUtf16 = Some largeNumber
        Text = text
        Highlights = highlights highlightCount
    }
}

let private range: LineRange = {
    Start = largeNumber
    Count = largeNumber
}

let private progress: ScanProgress = {
    ValidatedBytes = largeNumber
    TotalBytes = largeNumber
    ScanComplete = false
}

let private maximumPreview: PendingPreview = {
    Previous =
        PendingSide.Snippet {
            Line = largeNumber
            OffsetUtf16 = largeNumber
            Text = expensiveText 2048
            End = SnippetEnd.MoreTextPending
        }
    Current =
        PendingSide.Snippet {
            Line = largeNumber
            OffsetUtf16 = largeNumber
            Text = expensiveText 2048
            End = SnippetEnd.Truncated
        }
    Mismatch =
        Some {
            PreviousOffsetUtf16 = largeNumber
            CurrentOffsetUtf16 = largeNumber
        }
}

/// A partial page at the fragment limit, with every line text grown until the worker
/// message reaches its byte limit.
let private pageWith (textLength: int) : DiffPage =
    let rows =
        Array.init
            PageRows
            (fun index -> {
                Id = $"row-{index:D6}"
                Kind = DiffRowKind.Replaced
                Previous = Some(line (largeNumber + int64 index) (expensiveText textLength) 1)
                Current = Some(line (largeNumber + int64 index) (expensiveText textLength) 1)
            })

    {
        PageId = token "page-"
        NextCursor = Some(token "cursor-")
        Parts =
            rows
            |> Array.chunkBySize 32
            |> Array.mapi (fun index chunk ->
                DiffPart.Hunk {
                    HunkId = $"hunk-{index:D6}"
                    PreviousRange = range
                    CurrentRange = range
                    StartsHunk = true
                    EndsHunk = false
                    Body = HunkBody.AlignedRows chunk
                }
            )
        Progress = progress
        OutputComplete = false
        Pending = Some maximumPreview
    }

/// An expansion of the largest line count with line texts grown to the byte limit.
let private expansionWith (textLength: int) : DiffPart[] = [|
    DiffPart.ExpandedContext(
        token "gap-",
        Array.init
            100
            (fun index -> {
                Id = $"row-{index:D6}"
                Kind = DiffRowKind.Context
                Previous = Some(line (largeNumber + int64 index) (expensiveText textLength) 0)
                Current = Some(line (largeNumber + int64 index) (expensiveText textLength) 0)
            })
    )
|]

/// A slice of the largest length with as many highlights as the byte limit allows.
let private sliceWith (highlightCount: int) : DiffLine =
    line largeNumber (expensiveText 8192) highlightCount

let private failureWith (message: string) (evidence: string) : OperationFailure = {
    OperationFailure.create Validation TextDiffFailureCodes.ContentNotText message with
        Details = [| "detail line" |]
        DiffDetail =
            Some {
                Side = DiffSide.Current
                Evidence = evidence
            }
}

let private boundFailure (failure: OperationFailure) : OperationFailureDto =
    match Mappings.textDiffResult id (Failed failure: OperationResult<unit>) with
    | OperationResultDto.Failed mapped -> mapped
    | other -> failwith $"Expected a failure, got {other}"

let private utf8Bytes (text: string) = jsonBytes (box text) - 2

let private lineRequest
    (operationId: string)
    (handleId: string)
    (lineText: string)
    (offset: string)
    : ReadTextDiffLineRequestDto =
    {
        OperationId = operationId
        HandleId = handleId
        HandleVersion = "v1"
        Side = DiffSideDto.Current
        Line = lineText
        OffsetUtf16 = offset
        MaxUtf16 = 8192
        Continuation = None
    }

let private expectInvalid (result: Result<'T, OperationFailure>) =
    match result with
    | Error failure -> Vitest.expect(failure.Code).toBe VersionControlCodes.InvalidDiffRequest
    | Ok value -> failwith $"Expected a rejected request, got {value}"

Vitest.describe (
    "Text diff IPC payload size",
    fun () ->
        Vitest.test (
            "a partial page at the worker limit with a maximum pending preview stays within the limit plus 8 KiB",
            fun () ->
                let sizeOf textLength =
                    workerBytes (TextDiffProtocol.ResultPayload.ReadPage(Resumable.Ready(pageWith textLength)))

                let page = pageWith (fillTo PageLimitBytes sizeOf)

                let workerSize =
                    workerBytes (TextDiffProtocol.ResultPayload.ReadPage(Resumable.Ready page))

                Vitest.expect(workerSize).toBeLessThanOrEqual PageLimitBytes
                Vitest.expect(workerSize).toBeGreaterThan (PageLimitBytes - 2 * PageRows * 6)

                Vitest
                    .expect(ipcPayloadBytes (reply Mappings.resumablePage (Resumable.Ready page)))
                    .toBeLessThanOrEqual (PageLimitBytes + IpcMarginBytes)

                Vitest
                    .expect(ipcPayloadBytes (reply Mappings.diffPage page))
                    .toBeLessThanOrEqual (PageLimitBytes + IpcMarginBytes)
        )

        Vitest.test (
            "a maximum expansion stays within the worker limit plus 8 KiB",
            fun () ->
                let sizeOf textLength =
                    workerBytes (TextDiffProtocol.ResultPayload.Expand(Resumable.Ready(expansionWith textLength)))

                let parts = expansionWith (fillTo PageLimitBytes sizeOf)

                let workerSize =
                    workerBytes (TextDiffProtocol.ResultPayload.Expand(Resumable.Ready parts))

                Vitest.expect(workerSize).toBeLessThanOrEqual PageLimitBytes
                Vitest.expect(workerSize).toBeGreaterThan (PageLimitBytes - 2 * 100 * 6 * 2)

                Vitest
                    .expect(ipcPayloadBytes (reply Mappings.resumableParts (Resumable.Ready parts)))
                    .toBeLessThanOrEqual (PageLimitBytes + IpcMarginBytes)
        )

        Vitest.test (
            "a maximum line slice with highlights stays within the worker limit plus 8 KiB",
            fun () ->
                let sizeOf highlightCount =
                    workerBytes (TextDiffProtocol.ResultPayload.ReadLine(Resumable.Ready(sliceWith highlightCount)))

                let highlightCount = fillTo LineLimitBytes sizeOf
                let slice = sliceWith highlightCount

                Vitest.expect(highlightCount).toBeGreaterThan 0
                Vitest.expect(slice.Slice.Text.Length).toBe 8192

                Vitest
                    .expect(workerBytes (TextDiffProtocol.ResultPayload.ReadLine(Resumable.Ready slice)))
                    .toBeLessThanOrEqual
                    LineLimitBytes

                Vitest
                    .expect(ipcPayloadBytes (reply Mappings.resumableLine (Resumable.Ready slice)))
                    .toBeLessThanOrEqual (LineLimitBytes + IpcMarginBytes)
        )

        Vitest.test (
            "a scanning result with a maximum pending preview stays within the worker limit plus 8 KiB",
            fun () ->
                let scanning: Resumable<OpenDiffResult> =
                    Resumable.Scanning(progress, token "continuation-", Some maximumPreview)

                Vitest
                    .expect(ipcPayloadBytes (reply Mappings.resumableOpen scanning))
                    .toBeLessThanOrEqual (PageLimitBytes + IpcMarginBytes)
        )
)

Vitest.describe (
    "Text diff union mapping",
    fun () ->
        Vitest.test (
            "sides map to their DTO and back",
            fun () ->
                for side in [ DiffSide.Previous; DiffSide.Current ] do
                    Vitest.expect(Mappings.diffSideFromDto (Mappings.diffSide side)).toEqual side

                Vitest.expect(box (Mappings.diffSide DiffSide.Previous)).toBe "Previous"
                Vitest.expect(box (Mappings.diffSide DiffSide.Current)).toBe "Current"
        )

        Vitest.test (
            "field-less cases travel as their case names",
            fun () ->
                let endings =
                    [
                        LineEnding.NoEnding
                        LineEnding.LF
                        LineEnding.CRLF
                        LineEnding.CR
                    ]
                    |> List.map (Mappings.lineEnding >> box)

                let highlightKinds =
                    [ HighlightKind.UnchangedText; HighlightKind.ChangedText ]
                    |> List.map (fun kind -> box (Mappings.highlight { Start = 0; Length = 1; Kind = kind }).Kind)

                let rowKinds =
                    [
                        DiffRowKind.Context
                        DiffRowKind.Added
                        DiffRowKind.Removed
                        DiffRowKind.Replaced
                        DiffRowKind.EndingChanged
                    ]
                    |> List.map (Mappings.diffRowKind >> box)

                let snippetEnds =
                    [
                        SnippetEnd.Truncated
                        SnippetEnd.MoreTextPending
                        SnippetEnd.LineEnd
                        SnippetEnd.EndOfFile
                    ]
                    |> List.map (Mappings.snippetEnd >> box)

                Vitest.expect(endings).toEqual [ box "NoEnding"; box "LF"; box "CRLF"; box "CR" ]
                Vitest.expect(highlightKinds).toEqual [ box "UnchangedText"; box "ChangedText" ]

                Vitest.expect(rowKinds).toEqual [
                    box "Context"
                    box "Added"
                    box "Removed"
                    box "Replaced"
                    box "EndingChanged"
                ]

                Vitest.expect(snippetEnds).toEqual [
                    box "Truncated"
                    box "MoreTextPending"
                    box "LineEnd"
                    box "EndOfFile"
                ]
        )

        Vitest.test (
            "hunk bodies and diff parts keep their case and payload",
            fun () ->
                let previous = line 1L "a" 0
                let current = line 2L "b" 0

                let row: DiffRow = {
                    Id = "row-1"
                    Kind = DiffRowKind.Replaced
                    Previous = Some previous
                    Current = Some current
                }

                let fragment body : HunkFragment = {
                    HunkId = "hunk-1"
                    PreviousRange = { Start = 1L; Count = 1L }
                    CurrentRange = { Start = 2L; Count = 1L }
                    StartsHunk = true
                    EndsHunk = false
                    Body = body
                }

                let gap: EqualGap = {
                    GapId = "gap-1"
                    PreviousRange = { Start = 3L; Count = 4L }
                    CurrentRange = { Start = 5L; Count = 4L }
                }

                let rowDto = Mappings.diffRow row

                Vitest.expect(rowDto.Previous).toEqual (Some(Mappings.diffLine previous))
                Vitest.expect(rowDto.Current |> Option.map _.Number).toEqual (Some "2")

                match Mappings.diffPart (DiffPart.Hunk(fragment (HunkBody.AlignedRows [| row |]))) with
                | DiffPartDto.Hunk mapped ->
                    Vitest.expect(mapped.Body).toEqual (HunkBodyDto.AlignedRows [| rowDto |])

                    Vitest.expect(mapped.CurrentRange).toEqual {
                        LineRangeDto.Start = "2"
                        Count = "1"
                    }

                    Vitest.expect(mapped.StartsHunk).toBe true
                    Vitest.expect(mapped.EndsHunk).toBe false
                | other -> failwith $"Expected a hunk, got {other}"

                match
                    Mappings.diffPart (DiffPart.Hunk(fragment (HunkBody.UnalignedSides([| previous |], [| current |]))))
                with
                | DiffPartDto.Hunk mapped ->
                    Vitest
                        .expect(mapped.Body)
                        .toEqual (
                            HunkBodyDto.UnalignedSides(
                                [| Mappings.diffLine previous |],
                                [| Mappings.diffLine current |]
                            )
                        )
                | other -> failwith $"Expected a hunk, got {other}"

                Vitest
                    .expect(Mappings.diffPart (DiffPart.HiddenEqual gap))
                    .toEqual (
                        DiffPartDto.HiddenEqual {
                            GapId = "gap-1"
                            PreviousRange = { Start = "3"; Count = "4" }
                            CurrentRange = { Start = "5"; Count = "4" }
                        }
                    )

                Vitest
                    .expect(Mappings.diffPart (DiffPart.ExpandedContext("gap-1", [| row |])))
                    .toEqual (DiffPartDto.ExpandedContext("gap-1", [| rowDto |]))
        )

        Vitest.test (
            "pending sides keep their case and payload",
            fun () ->
                let snippet = {
                    Line = 7L
                    OffsetUtf16 = 12L
                    Text = "pending"
                    End = SnippetEnd.LineEnd
                }

                Vitest.expect(Mappings.pendingSide PendingSide.NoActiveLine).toEqual PendingSideDto.NoActiveLine

                Vitest
                    .expect(Mappings.pendingSide (PendingSide.Snippet snippet))
                    .toEqual (
                        PendingSideDto.Snippet {
                            Line = "7"
                            OffsetUtf16 = "12"
                            Text = "pending"
                            End = SnippetEndDto.LineEnd
                        }
                    )

                Vitest.expect(Mappings.pendingSide (PendingSide.Exhausted 42L)).toEqual (PendingSideDto.Exhausted "42")
        )

        Vitest.test (
            "blockers keep their case, side and payload",
            fun () ->
                let candidates = [|
                    {
                        Encoding = "windows-1252"
                        Preview = "café"
                    }
                |]

                Vitest
                    .expect(Mappings.diffBlocker (DiffBlocker.Binary(DiffSide.Previous, "NUL at 12")))
                    .toEqual (DiffBlockerDto.Binary(DiffSideDto.Previous, "NUL at 12"))

                Vitest
                    .expect(Mappings.diffBlocker (DiffBlocker.LocalContentUnavailable(DiffSide.Current, Some "oid")))
                    .toEqual (DiffBlockerDto.LocalContentUnavailable(DiffSideDto.Current, Some "oid"))

                Vitest
                    .expect(
                        Mappings.diffBlocker (
                            DiffBlocker.EncodingRequired(DiffSide.Current, { Id = "token-1" }, candidates)
                        )
                    )
                    .toEqual (
                        DiffBlockerDto.EncodingRequired(
                            DiffSideDto.Current,
                            { PreparationTokenDto.Id = "token-1" },
                            [|
                                {
                                    EncodingCandidateDto.Encoding = "windows-1252"
                                    Preview = "café"
                                }
                            |]
                        )
                    )

                Vitest
                    .expect(Mappings.diffBlocker (DiffBlocker.NotRegularFile DiffSide.Previous))
                    .toEqual (DiffBlockerDto.NotRegularFile DiffSideDto.Previous)

                Vitest.expect(Mappings.diffBlocker DiffBlocker.ProviderUnsupported).toEqual
                    DiffBlockerDto.ProviderUnsupported
        )

        Vitest.test (
            "open results and resumable results keep their case and payload",
            fun () ->
                let page: DiffPage = {
                    PageId = "page-1"
                    NextCursor = None
                    Parts = [||]
                    Progress = { progress with ScanComplete = true }
                    OutputComplete = true
                    Pending = None
                }

                let info (revision: string option) : DiffSourceInfo = {
                    Path = RepositoryPath.tryCreate "data/a.txt" |> Result.defaultWith failwith
                    Revision = revision |> Option.map (RevisionId.tryCreate >> Result.defaultWith failwith)
                    IsAbsent = false
                    ByteLength = 10L
                    LineCount = Some 2L
                    Encoding = Some "utf-8"
                    EncodingWasChosen = false
                    HasBom = false
                }

                let pageDto = Mappings.diffPage page
                let progressDto = Mappings.scanProgress progress
                let previewDto = Mappings.pendingPreview maximumPreview

                Vitest
                    .expect(
                        Mappings.resumableOpen (
                            Resumable.Ready(OpenDiffResult.NotDiffable DiffBlocker.ProviderUnsupported)
                        )
                    )
                    .toEqual (ResumableOpenDto.Ready(OpenDiffResultDto.NotDiffable DiffBlockerDto.ProviderUnsupported))

                match
                    Mappings.resumableOpen (
                        Resumable.Ready(
                            OpenDiffResult.Opened(
                                { Id = "h"; Version = "v" },
                                info (Some "abc"),
                                info None,
                                Resumable.Ready page
                            )
                        )
                    )
                with
                | ResumableOpenDto.Ready(OpenDiffResultDto.Opened(handle, previous, current, first)) ->
                    Vitest.expect(handle).toEqual {
                        DiffHandleDto.Id = "h"
                        Version = "v"
                    }

                    Vitest.expect(previous.Path).toBe "data/a.txt"
                    Vitest.expect(previous.Revision).toEqual (Some "abc")
                    Vitest.expect(current.Revision).toEqual None
                    Vitest.expect(current.LineCount).toEqual (Some "2")
                    Vitest.expect(first).toEqual (ResumablePageDto.Ready pageDto)
                | other -> failwith $"Expected an opened diff, got {other}"

                Vitest
                    .expect(Mappings.resumableOpen (Resumable.Scanning(progress, "c-open", Some maximumPreview)))
                    .toEqual (ResumableOpenDto.Scanning(progressDto, "c-open", Some previewDto))

                Vitest
                    .expect(Mappings.resumablePage (Resumable.Scanning(progress, "c-page", None)))
                    .toEqual (ResumablePageDto.Scanning(progressDto, "c-page", None))

                Vitest
                    .expect(Mappings.resumableParts (Resumable.Ready [| DiffPart.ExpandedContext("gap", [||]) |]))
                    .toEqual (ResumablePartsDto.Ready [| DiffPartDto.ExpandedContext("gap", [||]) |])

                Vitest
                    .expect(Mappings.resumableParts (Resumable.Scanning(progress, "c-expand", None)))
                    .toEqual (ResumablePartsDto.Scanning(progressDto, "c-expand", None))

                Vitest
                    .expect(Mappings.resumableLine (Resumable.Ready(line 3L "text" 1)))
                    .toEqual (ResumableLineDto.Ready(Mappings.diffLine (line 3L "text" 1)))

                Vitest
                    .expect(Mappings.resumableLine (Resumable.Scanning(progress, "c-line", Some maximumPreview)))
                    .toEqual (ResumableLineDto.Scanning(progressDto, "c-line", Some previewDto))

                Vitest.expect(Mappings.diffSourceInfoPair (info (Some "abc"), info None)).toEqual {
                    Previous = Mappings.diffSourceInfo (info (Some "abc"))
                    Current = Mappings.diffSourceInfo (info None)
                }
        )

        Vitest.test (
            "int64 values above 2^53 travel as exact decimal strings",
            fun () ->
                let mapped =
                    Mappings.lineRange {
                        Start = 9007199254740993L
                        Count = 9223372036854775807L
                    }

                Vitest.expect(mapped).toEqual {
                    LineRangeDto.Start = "9007199254740993"
                    Count = "9223372036854775807"
                }

                Vitest.expect(Mappings.tryNonNegativeInt64 "line" mapped.Start).toEqual (Ok 9007199254740993L)
                Vitest.expect(Mappings.tryNonNegativeInt64 "line" mapped.Count).toEqual (Ok 9223372036854775807L)
        )
)

Vitest.describe (
    "Text diff IPC envelope",
    fun () ->
        Vitest.test (
            "a long failure message is cut to 4 KiB of UTF-8 without splitting a character",
            fun () ->
                let message = "a" + String.replicate 3000 "é\U0001F600"
                let mapped = boundFailure (failureWith message "evidence")
                let lastCode = int mapped.Message[mapped.Message.Length - 1]

                Vitest.expect(utf8Bytes mapped.Message).toBeLessThanOrEqual Mappings.MaxTextDiffMessageBytes
                Vitest.expect(utf8Bytes mapped.Message).toBeGreaterThan (Mappings.MaxTextDiffMessageBytes - 4)
                Vitest.expect(message.StartsWith mapped.Message).toBe true
                Vitest.expect(lastCode >= 0xD800 && lastCode <= 0xDBFF).toBe false
                Vitest.expect(mapped.Details).toEqual [| "detail line" |]
                Vitest.expect(mapped.Code).toBe TextDiffFailureCodes.ContentNotText
        )

        Vitest.test (
            "a short failure message and evidence pass unchanged",
            fun () ->
                let mapped = boundFailure (failureWith "The source is not text." "NUL at 3")

                Vitest.expect(mapped.Message).toBe "The source is not text."

                Vitest
                    .expect(mapped.DiffDetail)
                    .toEqual (
                        Some {
                            Side = DiffSideDto.Current
                            Evidence = "NUL at 3"
                        }
                    )
        )

        Vitest.test (
            "diff detail evidence is cut to 256 characters",
            fun () ->
                let mapped = boundFailure (failureWith "not text" (String.replicate 300 "x"))

                Vitest.expect(mapped.DiffDetail |> Option.map _.Evidence.Length).toEqual (Some 256)
                Vitest.expect(mapped.DiffDetail |> Option.map _.Side).toEqual (Some DiffSideDto.Current)
        )

        Vitest.test (
            "operation ids and handle ids longer than 128 characters are rejected",
            fun () ->
                let atLimit = String.replicate 128 "a"
                let overLimit = String.replicate 129 "a"

                match Mappings.tryReadLineRequest (lineRequest atLimit atLimit "0" "0") with
                | Ok request -> Vitest.expect(request.Handle.Id).toBe atLimit
                | Error failure -> failwith failure.Message

                expectInvalid (Mappings.tryReadLineRequest (lineRequest overLimit "handle" "0" "0"))
                expectInvalid (Mappings.tryReadLineRequest (lineRequest "op" overLimit "0" "0"))

                expectInvalid (
                    Mappings.tryDiffHandle {
                        OperationId = "op"
                        HandleId = overLimit
                        HandleVersion = "v1"
                    }
                )

                expectInvalid (
                    Mappings.tryOpenDiffRequest {
                        OperationId = overLimit
                        Path = "data/a.txt"
                        PreviousPath = None
                        PreparationTokenId = None
                        PreviousEncoding = None
                        CurrentEncoding = None
                        ContextLines = 3
                        Continuation = None
                    }
                )
        )

        Vitest.test (
            "line numbers and offsets must be non-negative decimal integers",
            fun () ->
                for text in [ "-1"; "abc"; "1.5"; ""; " 1"; "99999999999999999999" ] do
                    expectInvalid (Mappings.tryReadLineRequest (lineRequest "op" "handle" text "0"))
                    expectInvalid (Mappings.tryReadLineRequest (lineRequest "op" "handle" "0" text))

                match Mappings.tryReadLineRequest (lineRequest "op" "handle" "12" "8192") with
                | Ok request ->
                    Vitest.expect(request.Line).toEqual 12L
                    Vitest.expect(request.OffsetUtf16).toEqual 8192L
                    Vitest.expect(request.Side).toEqual DiffSide.Current
                | Error failure -> failwith failure.Message
        )
)

Vitest.describe (
    "Text diff handle registry",
    fun () ->
        Vitest.test (
            "a handle arriving after its window closed or reloaded is closed at once, and other windows cannot use a recorded one",
            fun () -> promise {
                let host =
                    WorkspaceSessionHost.WorkspaceSessionHost(
                        TestHelpers.createRuntime
                            "text-diff-registry-settings"
                            VersionControlService.LakeFs.LakeFsCredentials.unconfigured
                            (TestHelpers.memoryBindings ())
                    )

                WorkspaceSessionHost.initialize host
                let originalIsWindowAlive = TextDiffHandles.isWindowAlive
                let closedWindow = 7
                let reloadedWindow = 5
                TextDiffHandles.isWindowAlive <- fun windowId -> windowId <> closedWindow

                let closes = ResizeArray<string * int option>()
                let bothClosed, resolveBothClosed = TestHelpers.deferred ()

                let unexpected (name: string) : Async<OperationResult<'T>> = async {
                    return failwith $"{name} was not expected."
                }

                // The close records the window the host assigns to its operation, which is
                // the owner the library checks.
                let service: TextDiffService = {
                    Open = fun _ _ -> unexpected "Open"
                    ReadPage = fun _ _ -> unexpected "ReadPage"
                    ReplayPage = fun _ _ -> unexpected "ReplayPage"
                    Expand = fun _ _ -> unexpected "Expand"
                    ReadLine = fun _ _ -> unexpected "ReadLine"
                    GetSourceInfo = fun _ _ -> unexpected "GetSourceInfo"
                    Close =
                        fun handle context -> async {
                            closes.Add(handle.Id, host.TryGetOperationWindowId context.OperationId)

                            if closes.Count = 2 then
                                resolveBothClosed ()

                            return OperationResult.succeeded ()
                        }
                }

                let handle (id: string) : DiffHandle = { Id = id; Version = "v1" }

                try
                    TextDiffHandles.recordOrClose (Some closedWindow) 0 "workspace" service (handle "closed-window")

                    let countBeforeReload = TextDiffHandles.reloadCount (Some reloadedWindow)
                    TextDiffHandles.windowReloaded reloadedWindow

                    TextDiffHandles.recordOrClose
                        (Some reloadedWindow)
                        countBeforeReload
                        "workspace"
                        service
                        (handle "before-reload")

                    TextDiffHandles.recordOrClose
                        (Some reloadedWindow)
                        (TextDiffHandles.reloadCount (Some reloadedWindow))
                        "workspace"
                        service
                        (handle "after-reload")

                    do! bothClosed

                    Vitest.expect(closes |> Seq.sort |> Seq.toArray).toEqual [|
                        "before-reload", Some reloadedWindow
                        "closed-window", Some closedWindow
                    |]

                    Vitest.expect(TextDiffHandles.isRecorded "closed-window").toBe false
                    Vitest.expect(TextDiffHandles.isRecorded "before-reload").toBe false
                    Vitest.expect(TextDiffHandles.isRecorded "after-reload").toBe true

                    let! fromOtherWindow =
                        TextDiffHandles.withOwnedHandle
                            (Some 6)
                            (handle "after-reload")
                            (fun () -> unexpected "the call")
                        |> Async.StartAsPromise

                    match fromOtherWindow with
                    | Failed failure -> Vitest.expect(failure.Code).toBe TextDiffFailureCodes.SessionClosed
                    | other -> failwith $"Expected a rejected handle, got {other}"

                    let! fromOwnWindow =
                        TextDiffHandles.withOwnedHandle
                            (Some reloadedWindow)
                            (handle "after-reload")
                            (fun () -> async { return OperationResult.succeeded "read" })
                        |> Async.StartAsPromise

                    Vitest.expect(TestHelpers.expectValue "own window call" fromOwnWindow).toBe "read"
                finally
                    TextDiffHandles.isWindowAlive <- originalIsWindowAlive
                    TextDiffHandles.windowClosed reloadedWindow
                    WorkspaceSessionHost.resetForTests ()
            }
        )
)
