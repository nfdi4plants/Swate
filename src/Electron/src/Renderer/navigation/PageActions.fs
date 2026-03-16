module Renderer.Navigation.PageActions

open ARCtrl
open Swate.Components
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper


let private clearSelectionAndPreview (setArcFileState: ArcFiles option -> unit) (setSelectedTreeItemPath: string option -> unit) =
    setSelectedTreeItemPath None
    setArcFileState None

let openLandingPage (landingCtx: StateContext<Renderer.Context.LandingStateCtx.LandingState>) (setArcFileState: ArcFiles option -> unit) (setSelectedTreeItemPath: string option -> unit) (setPageState: PageState option -> unit) =
    Renderer.Context.LandingStateCtx.reset landingCtx
    clearSelectionAndPreview setArcFileState setSelectedTreeItemPath
    setPageState (Some PageState.LandingDraft)

let createNewNote (notesCtx: StateContext<Renderer.Context.NotesStateCtx.NotesState>) (setArcFileState: ArcFiles option -> unit) (setSelectedTreeItemPath: string option -> unit) (setPageState: PageState option -> unit) =
    Renderer.Context.NotesStateCtx.reset notesCtx
    clearSelectionAndPreview setArcFileState setSelectedTreeItemPath
    setPageState (Some PageState.NotesDraft)

let openNotesSearchPage (setArcFileState: ArcFiles option -> unit) (setSelectedTreeItemPath: string option -> unit) (setPageState: PageState option -> unit) =
    clearSelectionAndPreview setArcFileState setSelectedTreeItemPath
    setPageState (Some PageState.NotesSearch)

let openArcStartPage (notesCtx: StateContext<Renderer.Context.NotesStateCtx.NotesState>) (landingCtx: StateContext<Renderer.Context.LandingStateCtx.LandingState>) (setArcFileState: ArcFiles option -> unit) (setSelectedTreeItemPath: string option -> unit) (setPageState: PageState option -> unit) =
    Renderer.Context.NotesStateCtx.reset notesCtx
    openLandingPage landingCtx setArcFileState setSelectedTreeItemPath setPageState
