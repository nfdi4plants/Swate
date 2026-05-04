module Main.NoteSearchReader

open System
open Fable.Core
open Fable.Core.JsInterop
open Swate.Components.Notes.Editor
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

let private readUtf8FileAsync (absolutePath: string) : JS.Promise<string> =
    fsPromisesDynamic?readFile (absolutePath, "utf8") |> unbox<JS.Promise<string>>

let private parseNote (relativePath: string) (content: string) =
    match NoteConversion.tryDecodeMarkdownFrontmatter content with
    | Some(frontmatter, bodyText) ->
        {
            RelativePath = relativePath
            Title = frontmatter.Title
            Date = frontmatter.Date
            Tags = frontmatter.Tags
            Content = bodyText.Trim()
        }
    | None ->
        failwith $"Note file '{relativePath}' does not contain YAML frontmatter."

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
                    let! content = readUtf8FileAsync absolutePath
                    return Some(parseNote relativePath content)
                with _ ->
                    // Keep malformed or unreadable files isolated from the rest of the search index.
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
