namespace Swate.Components

open System

module internal GitTextComparisonCore =

    [<RequireQualifiedAccess>]
    type LineChangeKind =
        | Neutral
        | Added
        | Removed

    type InlineSegment = {
        Text: string
        Kind: LineChangeKind
    }

    type SideLine = {
        Number: int option
        Text: string
        Segments: InlineSegment list
        Kind: LineChangeKind
    }

    type DiffRow = {
        Left: SideLine
        Right: SideLine
    }

    type DiffMetadata = {
        PreviousPath: string option
        CurrentPath: string option
    }

    type MergeConflictBlock = {
        Id: int
        CurrentLabel: string option
        CurrentContent: string
        IncomingLabel: string option
        IncomingContent: string
        RawContent: string
        StartIndex: int
    }

    type MergeConflictFragment =
        | PlainText of string
        | ConflictBlock of MergeConflictBlock

    module Text =

        let normalizeLineEndings (content: string) =
            content.Replace("\r\n", "\n").Replace("\r", "\n")

        let splitContentToLines (content: string) =
            let normalized = normalizeLineEndings content

            if String.IsNullOrEmpty normalized then
                [||]
            else
                let lines = normalized.Split('\n')

                if normalized.EndsWith("\n", StringComparison.Ordinal) then
                    lines.[0 .. lines.Length - 2]
                else
                    lines

        let sliceLines (lines: string[]) (startLineNumber: int) (lineCount: int) =
            if lineCount > 0 then
                Array.sub lines (startLineNumber - 1) lineCount
            else
                [||]

    module Metadata =

        let private diffGitHeaderPattern =
            Text.RegularExpressions.Regex(
                @"^diff --git (?<old>""(?:\\.|[^""])+""|\S+) (?<new>""(?:\\.|[^""])+""|\S+)$",
                Text.RegularExpressions.RegexOptions.Compiled
            )

        let private unquotePathToken (pathText: string) =
            let trimmed = pathText.Trim()

            if trimmed.Length >= 2 && trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal) then
                let quotedValue = trimmed.Substring(1, trimmed.Length - 2)

                try
                    Text.RegularExpressions.Regex.Unescape quotedValue
                with _ ->
                    quotedValue
            else
                trimmed.Trim('"')

        let private normalizeDiffPath (pathText: string) =
            let trimmed = pathText |> unquotePathToken |> fun value -> value.Trim()

            if String.IsNullOrWhiteSpace trimmed || trimmed = "/dev/null" then
                None
            elif trimmed.StartsWith("a/", StringComparison.Ordinal) || trimmed.StartsWith("b/", StringComparison.Ordinal) then
                Some(trimmed.Substring(2))
            else
                Some trimmed

        let private extractFromPatchHeaders (lines: string[]) =
            let previousPath =
                lines
                |> Array.tryFind (fun line -> line.StartsWith("--- ", StringComparison.Ordinal))
                |> Option.bind (fun line -> normalizeDiffPath (line.Substring(4)))

            let currentPath =
                lines
                |> Array.tryFind (fun line -> line.StartsWith("+++ ", StringComparison.Ordinal))
                |> Option.bind (fun line -> normalizeDiffPath (line.Substring(4)))

            {
                PreviousPath = previousPath
                CurrentPath = currentPath
            }

        let private extractFromDiffLine (diffLine: string) =
            let matched = diffGitHeaderPattern.Match diffLine

            if matched.Success then
                Some {
                    PreviousPath = normalizeDiffPath matched.Groups.["old"].Value
                    CurrentPath = normalizeDiffPath matched.Groups.["new"].Value
                }
            else
                None

        let extractDiffMetadata (diffText: string) =
            let lines =
                diffText
                |> Text.normalizeLineEndings
                |> Text.splitContentToLines

            match lines |> Array.tryFind (fun line -> line.StartsWith("diff --git ", StringComparison.Ordinal)) with
            | Some diffLine ->
                extractFromDiffLine diffLine
                |> Option.defaultValue (extractFromPatchHeaders lines)
            | None ->
                extractFromPatchHeaders lines

        let resolveHeaderLabel (fallbackLabel: string) (explicitLabel: string option) (pathLabel: string option) =
            explicitLabel
            |> Option.filter (String.IsNullOrWhiteSpace >> not)
            |> Option.orElse pathLabel
            |> Option.defaultValue fallbackLabel

    module InlineDiff =

        type private DiffOperation =
            | Keep of string
            | Delete of string
            | Insert of string

        let private buildTokenLcsMatrix (leftTokens: string[]) (rightTokens: string[]) =
            let matrix =
                Array.init (leftTokens.Length + 1) (fun _ -> Array.zeroCreate<int> (rightTokens.Length + 1))

            for leftIndex = leftTokens.Length - 1 downto 0 do
                for rightIndex = rightTokens.Length - 1 downto 0 do
                    matrix.[leftIndex].[rightIndex] <-
                        if leftTokens.[leftIndex] = rightTokens.[rightIndex] then
                            matrix.[leftIndex + 1].[rightIndex + 1] + 1
                        else
                            max matrix.[leftIndex + 1].[rightIndex] matrix.[leftIndex].[rightIndex + 1]

            matrix

        let private buildTokenDiffOperations (leftTokens: string[]) (rightTokens: string[]) =
            let matrix = buildTokenLcsMatrix leftTokens rightTokens

            let rec loop leftIndex rightIndex acc =
                if leftIndex >= leftTokens.Length && rightIndex >= rightTokens.Length then
                    List.rev acc
                elif leftIndex >= leftTokens.Length then
                    loop leftIndex (rightIndex + 1) (Insert rightTokens.[rightIndex] :: acc)
                elif rightIndex >= rightTokens.Length then
                    loop (leftIndex + 1) rightIndex (Delete leftTokens.[leftIndex] :: acc)
                elif leftTokens.[leftIndex] = rightTokens.[rightIndex] then
                    loop (leftIndex + 1) (rightIndex + 1) (Keep leftTokens.[leftIndex] :: acc)
                elif matrix.[leftIndex + 1].[rightIndex] >= matrix.[leftIndex].[rightIndex + 1] then
                    loop (leftIndex + 1) rightIndex (Delete leftTokens.[leftIndex] :: acc)
                else
                    loop leftIndex (rightIndex + 1) (Insert rightTokens.[rightIndex] :: acc)

            loop 0 0 []

        let private tokenize (text: string) =
            let tokens = ResizeArray<string>()
            let buffer = Text.StringBuilder()
            let mutable currentIsWhitespace: bool option = None

            let flushBuffer () =
                if buffer.Length > 0 then
                    tokens.Add(buffer.ToString())
                    buffer.Clear() |> ignore

            for character in text do
                let isWhitespace = Char.IsWhiteSpace character

                match currentIsWhitespace with
                | Some currentKind when currentKind <> isWhitespace ->
                    flushBuffer ()
                    buffer.Append(character) |> ignore
                    currentIsWhitespace <- Some isWhitespace
                | Some _ ->
                    buffer.Append(character) |> ignore
                | None ->
                    buffer.Append(character) |> ignore
                    currentIsWhitespace <- Some isWhitespace

            flushBuffer ()
            tokens |> Seq.toArray

        let private appendSegment kind text (segments: ResizeArray<InlineSegment>) =
            if not (String.IsNullOrEmpty text) then
                match segments.Count with
                | count when count > 0 && segments.[count - 1].Kind = kind ->
                    let previous = segments.[count - 1]
                    segments.[count - 1] <- { previous with Text = previous.Text + text }
                | _ ->
                    segments.Add({ Text = text; Kind = kind })

        let buildInlineSegments (leftText: string) (rightText: string) =
            let leftSegments = ResizeArray<InlineSegment>()
            let rightSegments = ResizeArray<InlineSegment>()

            buildTokenDiffOperations (tokenize leftText) (tokenize rightText)
            |> List.iter (function
                | Keep text ->
                    appendSegment LineChangeKind.Neutral text leftSegments
                    appendSegment LineChangeKind.Neutral text rightSegments
                | Delete text ->
                    appendSegment LineChangeKind.Removed text leftSegments
                | Insert text ->
                    appendSegment LineChangeKind.Added text rightSegments
            )

            leftSegments |> Seq.toList, rightSegments |> Seq.toList

    module Rows =

        let private createSideLine (number: int option) (text: string) (kind: LineChangeKind) (segments: InlineSegment list) = {
            Number = number
            Text = text
            Segments = segments
            Kind = kind
        }

        let private createNeutralLine number text =
            createSideLine
                (Some number)
                text
                LineChangeKind.Neutral
                [
                    {
                        Text = text
                        Kind = LineChangeKind.Neutral
                    }
                ]

        let private createRemovedLine number text =
            createSideLine
                (Some number)
                text
                LineChangeKind.Removed
                [
                    {
                        Text = text
                        Kind = LineChangeKind.Removed
                    }
                ]

        let private createAddedLine number text =
            createSideLine
                (Some number)
                text
                LineChangeKind.Added
                [
                    {
                        Text = text
                        Kind = LineChangeKind.Added
                    }
                ]

        let emptyRow () = {
            Left = createSideLine None "" LineChangeKind.Neutral []
            Right = createSideLine None "" LineChangeKind.Neutral []
        }

        let buildIndexedRows
            (leftLineNumberStart: int)
            (rightLineNumberStart: int)
            (leftLines: string[])
            (rightLines: string[])
            =
            let rowCount = max leftLines.Length rightLines.Length

            [
                for rowIndex in 0 .. rowCount - 1 do
                    let leftLine =
                        if rowIndex < leftLines.Length then
                            Some leftLines.[rowIndex]
                        else
                            None

                    let rightLine =
                        if rowIndex < rightLines.Length then
                            Some rightLines.[rowIndex]
                        else
                            None

                    yield
                        match leftLine, rightLine with
                        | Some leftText, Some rightText when leftText = rightText ->
                            {
                                Left = createNeutralLine (leftLineNumberStart + rowIndex) leftText
                                Right = createNeutralLine (rightLineNumberStart + rowIndex) rightText
                            }
                        | Some leftText, Some rightText ->
                            let leftSegments, rightSegments = InlineDiff.buildInlineSegments leftText rightText

                            {
                                Left =
                                    createSideLine
                                        (Some(leftLineNumberStart + rowIndex))
                                        leftText
                                        LineChangeKind.Removed
                                        leftSegments
                                Right =
                                    createSideLine
                                        (Some(rightLineNumberStart + rowIndex))
                                        rightText
                                        LineChangeKind.Added
                                        rightSegments
                            }
                        | Some leftText, None ->
                            {
                                Left = createRemovedLine (leftLineNumberStart + rowIndex) leftText
                                Right = (emptyRow ()).Right
                            }
                        | None, Some rightText ->
                            {
                                Left = (emptyRow ()).Left
                                Right = createAddedLine (rightLineNumberStart + rowIndex) rightText
                            }
                        | None, None ->
                            emptyRow ()
            ]

        let buildRows (leftContent: string) (rightContent: string) =
            buildIndexedRows 1 1 (Text.splitContentToLines leftContent) (Text.splitContentToLines rightContent)

    module WordDiff =

        type DiffHunk = {
            OldStart: int
            OldCount: int
            NewStart: int
            NewCount: int
        }

        let private unchangedBoundary (startLine: int) (lineCount: int) =
            if lineCount = 0 then startLine + 1 else startLine

        let private buildUnchangedRows
            (leftLines: string[])
            (rightLines: string[])
            (leftLineNumberStart: int)
            (rightLineNumberStart: int)
            (leftLineNumberEndExclusive: int)
            (rightLineNumberEndExclusive: int)
            =
            let leftCount = max 0 (leftLineNumberEndExclusive - leftLineNumberStart)
            let rightCount = max 0 (rightLineNumberEndExclusive - rightLineNumberStart)

            let leftSlice = Text.sliceLines leftLines leftLineNumberStart leftCount
            let rightSlice = Text.sliceLines rightLines rightLineNumberStart rightCount

            if leftCount = rightCount && Array.forall2 (=) leftSlice rightSlice then
                [
                    for rowIndex in 0 .. leftCount - 1 do
                        yield {
                            Left = {
                                Number = Some(leftLineNumberStart + rowIndex)
                                Text = leftSlice.[rowIndex]
                                Segments = [ { Text = leftSlice.[rowIndex]; Kind = LineChangeKind.Neutral } ]
                                Kind = LineChangeKind.Neutral
                            }
                            Right = {
                                Number = Some(rightLineNumberStart + rowIndex)
                                Text = rightSlice.[rowIndex]
                                Segments = [ { Text = rightSlice.[rowIndex]; Kind = LineChangeKind.Neutral } ]
                                Kind = LineChangeKind.Neutral
                            }
                        }
                ]
            else
                // If the supplied contents do not line up with the word-diff hunk boundaries,
                // fall back to the generic row pairing so the UI still renders a sane comparison.
                Rows.buildIndexedRows leftLineNumberStart rightLineNumberStart leftSlice rightSlice

        let parseWordDiffHunks (wordDiffText: string) =
            let hunkHeaderPattern =
                Text.RegularExpressions.Regex(
                    @"^@@ -(?<oldStart>\d+)(,(?<oldCount>\d+))? \+(?<newStart>\d+)(,(?<newCount>\d+))? @@",
                    Text.RegularExpressions.RegexOptions.Compiled
                )

            wordDiffText
            |> Text.normalizeLineEndings
            |> Text.splitContentToLines
            |> Array.choose (fun line ->
                let matched = hunkHeaderPattern.Match line

                if matched.Success then
                    let parseCount (groupName: string) =
                        let group = matched.Groups.[groupName]

                        if group.Success then
                            Int32.Parse group.Value
                        else
                            1

                    Some {
                        OldStart = Int32.Parse matched.Groups.["oldStart"].Value
                        OldCount = parseCount "oldCount"
                        NewStart = Int32.Parse matched.Groups.["newStart"].Value
                        NewCount = parseCount "newCount"
                    }
                else
                    None
            )
            |> Array.toList

        let buildRowsFromWordDiff (wordDiffText: string) (leftContent: string) (rightContent: string) =
            let leftLines = Text.splitContentToLines leftContent
            let rightLines = Text.splitContentToLines rightContent
            let hunks = parseWordDiffHunks wordDiffText

            let rec loop leftCursor rightCursor remainingHunks =
                match remainingHunks with
                | hunk :: rest ->
                    let leftBoundary = unchangedBoundary hunk.OldStart hunk.OldCount
                    let rightBoundary = unchangedBoundary hunk.NewStart hunk.NewCount

                    let unchangedRows =
                        buildUnchangedRows leftLines rightLines leftCursor rightCursor leftBoundary rightBoundary

                    let changedRows =
                        Rows.buildIndexedRows
                            leftBoundary
                            rightBoundary
                            (Text.sliceLines leftLines leftBoundary hunk.OldCount)
                            (Text.sliceLines rightLines rightBoundary hunk.NewCount)

                    [
                        yield! unchangedRows
                        yield! changedRows
                        yield! loop (leftBoundary + hunk.OldCount) (rightBoundary + hunk.NewCount) rest
                    ]
                | [] ->
                    buildUnchangedRows
                        leftLines
                        rightLines
                        leftCursor
                        rightCursor
                        (leftLines.Length + 1)
                        (rightLines.Length + 1)

            loop 1 1 hunks

    module MergeConflicts =

        let containsConflictMarkers (content: string) =
            content
            |> Text.normalizeLineEndings
            |> Text.splitContentToLines
            |> Array.exists (fun line ->
                line.StartsWith("<<<<<<<", StringComparison.Ordinal)
                || line.StartsWith("=======", StringComparison.Ordinal)
                || line.StartsWith(">>>>>>>", StringComparison.Ordinal)
                || line.StartsWith("|||||||", StringComparison.Ordinal)
            )

        let private normalizeConflictLabel (markerLine: string) =
            if markerLine.Length <= 7 then
                None
            else
                markerLine.Substring(7).Trim()
                |> Option.ofObj
                |> Option.filter (String.IsNullOrWhiteSpace >> not)

        let parseMergeConflictFragments (mergeConflictContent: string) =
            let normalized = Text.normalizeLineEndings mergeConflictContent
            let lines = normalized.Split('\n')
            let lineStartIndices = Array.zeroCreate<int> lines.Length

            do
                let mutable nextStartIndex = 0

                for lineIndex in 0 .. lines.Length - 1 do
                    lineStartIndices.[lineIndex] <- nextStartIndex
                    nextStartIndex <- nextStartIndex + lines.[lineIndex].Length

                    if lineIndex < lines.Length - 1 then
                        nextStartIndex <- nextStartIndex + 1

            let isMarker marker (line: string) =
                line.StartsWith(marker, StringComparison.Ordinal)

            let flushPlainFragment plainLinesRev fragmentsRev =
                match plainLinesRev with
                | [] -> fragmentsRev
                | _ -> PlainText(String.concat "\n" (List.rev plainLinesRev)) :: fragmentsRev

            let sliceToText startIndex endIndexInclusive =
                let startOffset = lineStartIndices.[startIndex]
                let endExclusive = lineStartIndices.[endIndexInclusive] + lines.[endIndexInclusive].Length
                normalized.Substring(startOffset, endExclusive - startOffset)

            let sliceToTextToEnd startIndex =
                normalized.Substring(lineStartIndices.[startIndex])

            let rec collectCurrent currentRev index =
                if index >= lines.Length then
                    Error()
                else
                    match lines.[index] with
                    | line when isMarker "|||||||" line -> skipBase currentRev (index + 1)
                    | line when isMarker "=======" line -> collectIncoming currentRev [] (index + 1)
                    | line -> collectCurrent (line :: currentRev) (index + 1)

            and skipBase currentRev index =
                if index >= lines.Length then
                    Error()
                else
                    match lines.[index] with
                    | line when isMarker "=======" line -> collectIncoming currentRev [] (index + 1)
                    | _ -> skipBase currentRev (index + 1)

            and collectIncoming currentRev incomingRev index =
                if index >= lines.Length then
                    Error()
                else
                    match lines.[index] with
                    | line when isMarker ">>>>>>>" line ->
                        Ok(List.rev currentRev, normalizeConflictLabel line, List.rev incomingRev, index + 1, index)
                    | line ->
                        collectIncoming currentRev (line :: incomingRev) (index + 1)

            let tryParseConflict conflictId startIndex =
                let currentLabel = normalizeConflictLabel lines.[startIndex]

                match collectCurrent [] (startIndex + 1) with
                | Error() ->
                    None
                | Ok(currentLines, incomingLabel, incomingLines, nextIndex, endIndex) ->
                    Some(
                        {
                            Id = conflictId
                            CurrentLabel = currentLabel
                            CurrentContent = String.concat "\n" currentLines
                            IncomingLabel = incomingLabel
                            IncomingContent = String.concat "\n" incomingLines
                            RawContent = sliceToText startIndex endIndex
                            StartIndex = lineStartIndices.[startIndex]
                        },
                        nextIndex
                    )

            let rec loop plainRev fragmentsRev nextConflictId index =
                if index >= lines.Length then
                    flushPlainFragment plainRev fragmentsRev |> List.rev
                else
                    match lines.[index] with
                    | line when isMarker "<<<<<<<" line ->
                        match tryParseConflict nextConflictId index with
                        | Some(block, nextIndex) ->
                            let fragmentsRev = flushPlainFragment plainRev fragmentsRev
                            loop [] (ConflictBlock block :: fragmentsRev) (nextConflictId + 1) nextIndex
                        | None ->
                            let fragmentsRev = flushPlainFragment plainRev fragmentsRev
                            List.rev (PlainText(sliceToTextToEnd index) :: fragmentsRev)
                    | line ->
                        loop (line :: plainRev) fragmentsRev nextConflictId (index + 1)

            loop [] [] 1 0

        let tryReplaceExactTextAt (startIndex: int) (expectedText: string) (replacementText: string) (sourceText: string) =
            if String.IsNullOrEmpty expectedText then
                Some sourceText
            elif startIndex < 0 || startIndex + expectedText.Length > sourceText.Length then
                None
            else
                let actualText = sourceText.Substring(startIndex, expectedText.Length)

                if actualText = expectedText then
                    Some(sourceText.Substring(0, startIndex) + replacementText + sourceText.Substring(startIndex + expectedText.Length))
                else
                    None
