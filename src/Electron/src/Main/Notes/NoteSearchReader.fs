module Main.NoteSearchReader

open System
open Fable.Core
open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Swate.Components.Composite.Notes.Types
open Swate.Components.Shared
open Main.Bindings.Filesystem
open Main.Notes.NoteConstants

let private isNoteMarkdownPath (relativePath: string) =
    let normalizedPath = PathHelpers.normalizeSeparators relativePath
    let lowered = normalizedPath.ToLowerInvariant()

    lowered.StartsWith(NotesRootFolderPrefix)
    && lowered.EndsWith(NoteMarkdownExtension)

let private readUtf8FileAsync (absolutePath: string) : JS.Promise<string> =
    readFileAsync absolutePath TextEncoding.Utf8

let private parseNote (relativePath: string) (content: string) =
    match NoteConversion.tryDecodeMarkdownFrontmatter content with
    | Some(frontmatter, bodyText) -> {
        RelativePath = relativePath
        Title = frontmatter.Title
        Date = frontmatter.Date
        Tags = frontmatter.Tags
        Content = bodyText.Trim()
      }
    | None -> failwith $"Note file '{relativePath}' does not contain YAML frontmatter."

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
        |> Array.map (fun (absolutePath, relativePath) -> promise {
            try
                let! content = readUtf8FileAsync absolutePath
                return Some(parseNote relativePath content)
            with _ ->
                // Keep malformed or unreadable files isolated from the rest of the search index.
                return None
        })

    let! notesWithOptions = Fable.Core.JS.Constructors.Promise.all notePromises

    return
        notesWithOptions
        |> fun notesWithOptions -> notesWithOptions :?> Note option[]
        |> Array.choose id
        |> Array.sortByDescending (fun note -> note.Date)
}