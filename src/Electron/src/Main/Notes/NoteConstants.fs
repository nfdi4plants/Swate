module Main.Notes.NoteConstants

[<Literal>]
let NotesRootFolderName = "notes"

let NotesRootFolderPrefix = NotesRootFolderName + "/"

[<Literal>]
let NoteMarkdownExtension = ".md"

[<Literal>]
let NotesReadmeFileName = "README.md"

let NotesReadmeContent =
    """# Notes folder

Swate created this optional folder automatically because "Automatically create notes folder" is enabled.

To disable this behavior, open Swate Settings and turn off "Automatically create notes folder".
"""