module internal Renderer.Components.MainContent.NoteSearchInterop

open Swate.Components.NoteTypes
open Swate.Electron.Shared.NoteSearchDto

let toDomainNote (noteDto: NoteSearchDto) : Note =
    NoteSearchNoteDto.toNote noteDto
