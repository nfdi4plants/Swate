module Main.NoteSearchReader

open System
open Fable.Core
open Swate.Components.Composite.Notes.Editor
open Swate.Electron.Shared.FileIOHelper
open Swate.Components.Composite.Notes.Types
open Swate.Components.Shared
open Main.Bindings.Filesystem
open Main.Bindings.Path
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

let private discoverNoteFiles (arcPath: string) : JS.Promise<(string * string)[]> = promise {
    let notesRoot = join [| arcPath; NotesRootFolderName |]

    if not (existsSync notesRoot) then
        return [||]
    else
        let directories = ResizeArray<string>()
        let noteFiles = ResizeArray<string * string>()
        directories.Add(notesRoot)

        while directories.Count > 0 do
            let directoryPath = directories.[directories.Count - 1]
            directories.RemoveAt(directories.Count - 1)

            let! dirents = readdirWithTypesAsync directoryPath (ReaddirOptions(withFileTypes = true))

            dirents
            |> Array.iter (fun dirent ->
                let absolutePath = join [| directoryPath; dirent.name |]

                if dirent.isDirectory () then
                    directories.Add(absolutePath)
                elif dirent.isFile () then
                    tryGetRepoRelativePath arcPath absolutePath
                    |> Option.filter isNoteMarkdownPath
                    |> Option.iter (fun relativePath -> noteFiles.Add(absolutePath, relativePath))
            )

        return noteFiles.ToArray()
}

let readNotes (arcPath: string) : JS.Promise<Note[]> = promise {
    let! noteFiles = discoverNoteFiles arcPath

    // Process all note files in parallel, preserving per-file error handling
    let notePromises =
        noteFiles
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
