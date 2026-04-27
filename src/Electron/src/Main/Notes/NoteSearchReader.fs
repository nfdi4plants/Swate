module Main.NoteSearchReader

open System
open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Components.NoteTypes

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private pathDynamic: obj = importAll "path"

let private normalizePath (path: string) = path.Replace("\\", "/")

let private tryGetRepoRelativePath (repoRoot: string) (absolutePath: string) =
    let relativePath =
        pathDynamic?relative (repoRoot, absolutePath) |> unbox<string> |> normalizePath

    if String.IsNullOrWhiteSpace relativePath || relativePath = "." then
        None
    else
        Some relativePath

let private isNoteMarkdownPath (relativePath: string) =
    let normalizedPath = normalizePath relativePath
    let lowered = normalizedPath.ToLowerInvariant()
    lowered.StartsWith("notes/") && lowered.EndsWith(".md")

let private tryParseDateText (value: string) =
    let parts = value.Trim().Split('_', StringSplitOptions.RemoveEmptyEntries)

    if parts.Length <> 3 then
        None
    else
        match Int32.TryParse(parts.[0]), Int32.TryParse(parts.[1]), Int32.TryParse(parts.[2]) with
        | (true, day), (true, month), (true, year) ->
            try
                Some(DateTime(year, month, day))
            with _ ->
                None
        | _ -> None

let private tryParseDateFromPath (relativePath: string) =
    (normalizePath relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries)
    |> Array.tryPick tryParseDateText

let private tryGetHeaderValue (headerKey: string) (lines: string[]) =
    lines
    |> Array.tryPick (fun line ->
        let trimmedLine = line.Trim()
        let separatorIndex = trimmedLine.IndexOf(':')

        if separatorIndex < 0 then
            None
        else
            let key = trimmedLine.Substring(0, separatorIndex).Trim()

            if String.Equals(key, headerKey, StringComparison.OrdinalIgnoreCase) then
                Some(trimmedLine.Substring(separatorIndex + 1).Trim())
            else
                None
    )

let private fallbackTitleFromPath (relativePath: string) =
    let fileName = pathDynamic?basename (relativePath, ".md") |> unbox<string>
    fileName.Replace("_", " ").Trim()

let private mkTagFromText (tagText: string) =
    let parts =
        tagText.Split([| '|' |], StringSplitOptions.None)
        |> Array.map (fun part -> part.Trim())
        |> Array.filter (String.IsNullOrWhiteSpace >> not)

    match parts with
    | [| name |] -> Some(OntologyAnnotation(name, ""))
    | [| name; source |] -> Some(OntologyAnnotation(name, source))
    | [| name; source; accession |] -> Some(OntologyAnnotation(name, source, accession))
    // Preserve legacy/fallback behavior for unexpected formats.
    | _ when String.IsNullOrWhiteSpace tagText |> not -> Some(OntologyAnnotation(tagText.Trim(), ""))
    | _ -> None

let private parseTags (value: string option) =
    value
    |> Option.bind (fun tagText ->
        tagText.Split([| ',' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.choose mkTagFromText
        |> ResizeArray
        |> fun tags -> if tags.Count = 0 then None else Some tags
    )

let private normalizeNewlines (content: string) = content.Replace("\r\n", "\n").Replace("\r", "\n")

let private readUtf8FileAsync (absolutePath: string) : JS.Promise<string> =
    fsPromisesDynamic?readFile (absolutePath, "utf8") |> unbox<JS.Promise<string>>

let private unixEpoch = DateTime(1970, 1, 1, 0, 0, 0)

let private tryGetFileModifiedDate (absolutePath: string) : JS.Promise<DateTime option> = promise {
    let! stats = fsPromisesDynamic?stat (absolutePath) |> unbox<JS.Promise<obj>>
    let mtimeMs = stats?mtimeMs |> unbox<float>

    if Double.IsNaN mtimeMs || Double.IsInfinity mtimeMs then
        return None
    else
        return Some(unixEpoch.AddMilliseconds(mtimeMs))
}

let private parseNote (relativePath: string) (content: string) (modifiedAt: DateTime option) =
    let normalizedContent = normalizeNewlines content
    let lines = normalizedContent.Split('\n')

    let separatorIndex =
        lines
        |> Array.tryFindIndex (fun line -> line.Trim() = "---")

    let headerLines, bodyLines =
        match separatorIndex with
        | Some index ->
            let headerLines =
                if index > 0 then
                    lines.[0 .. index - 1]
                else
                    [||]

            let bodyLines =
                if index + 1 < lines.Length then
                    lines.[index + 1 ..]
                else
                    [||]

            headerLines, bodyLines
        | None -> lines, lines

    let title =
        headerLines
        |> Array.tryPick (fun line ->
            if line.StartsWith("# ") then
                Some(line.Substring(2).Trim())
            else
                None
        )
        |> Option.defaultValue (fallbackTitleFromPath relativePath)

    let date =
        match tryGetHeaderValue "Date Created" headerLines |> Option.bind tryParseDateText with
        | Some parsedDate -> parsedDate
        | None ->
            match tryParseDateFromPath relativePath with
            | Some parsedDate -> parsedDate
            | None -> defaultArg modifiedAt DateTime.MinValue

    let tags = headerLines |> tryGetHeaderValue "Tags" |> parseTags

    let body =
        bodyLines
        |> String.concat "\n"
        |> fun text -> text.Trim()
        |> fun text ->
            if String.IsNullOrWhiteSpace text && separatorIndex.IsNone then
                normalizedContent.Trim()
            else
                text

    {
        RelativePath = relativePath
        Title = title
        Date = date
        Tags = tags
        Content = body
    }

let readNotes (arcPath: string) (fileEntries: FileEntry[]) : JS.Promise<Note[]> = promise {
    let noteEntries =
        fileEntries
        |> Array.filter (fun entry -> not entry.isDirectory)
        |> Array.choose (fun entry ->
            tryGetRepoRelativePath arcPath entry.path
            |> Option.filter isNoteMarkdownPath
            |> Option.map (fun relativePath -> entry.path, relativePath)
        )

    // Process all note files in parallel, preserving per-file error handling
    let notePromises =
        noteEntries
        |> Array.map (fun (absolutePath, relativePath) ->
            promise {
                try
                    let! modifiedAt = tryGetFileModifiedDate absolutePath
                    let! content = readUtf8FileAsync absolutePath
                    return Some(parseNote relativePath content modifiedAt)
                with _ ->
                    // Match previous behavior: silently skip files that fail to read/parse
                    return None
            }
        )

    let! notesWithOptions = Fable.Core.JS.Constructors.Promise.all notePromises

    return
        notesWithOptions
        |> fun notesWithOptions -> notesWithOptions :?> Note option []
        |> Array.choose id
        |> Array.sortByDescending (fun note -> note.Date)
}
