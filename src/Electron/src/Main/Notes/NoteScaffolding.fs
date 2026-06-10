module Main.Notes.NoteScaffolding

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.Notes.NoteConstants

let private mkdirRecursiveWithCreationFlagAsync (directoryPath: string) : JS.Promise<bool> = promise {
    let! mkdirResult = mkdirAsync directoryPath (MkdirOptions(recursive = true))
    return not (isNullOrUndefined mkdirResult)
}

let private writeUtf8FileAsync (absolutePath: string) (content: string) : JS.Promise<unit> =
    writeFileAsync absolutePath content TextEncoding.Utf8

/// Ensures ARC notes scaffolding exists for the provided ARC root path.
let ensureNotesFolderAtArcPath (arcPath: string) : JS.Promise<Result<unit, exn>> = promise {
    try
        if String.IsNullOrWhiteSpace arcPath then
            return Error(exn "ARC path must not be empty.")
        else
            let resolvedArcPath = resolve [| arcPath |]
            let notesFolderPath = join [| resolvedArcPath; NotesRootFolderName |]
            let notesReadmePath = join [| notesFolderPath; NotesReadmeFileName |]

            let! notesFolderWasCreated = mkdirRecursiveWithCreationFlagAsync notesFolderPath

            if notesFolderWasCreated then
                do! writeUtf8FileAsync notesReadmePath NotesReadmeContent

            return Ok()
    with error ->
        return Error error
}
