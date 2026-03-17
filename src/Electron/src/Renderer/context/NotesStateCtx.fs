module Renderer.Context.NotesStateCtx

open Feliz
open Swate.Components
open Swate.Components.Notes.Editor

type NotesState = {
    Draft: NotesDraft
    UiState: NotesUiState
} with

    static member init() = {
        Draft = NotesDraft.init
        UiState = NotesUiState.init
    }

let reset (ctx: StateContext<NotesState>) =
    ctx.setState (NotesState.init ())

let NotesStateCtx =
    React.createContext<StateContext<NotesState>> (StateContext.init (NotesState.init ()))
