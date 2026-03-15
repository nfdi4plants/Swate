module Renderer.Navigation.PageActions

open ARCtrl
open Swate.Components
open Swate.Electron.Shared.IPCTypes.IPCTypesHelper

type Deps = {
    setPageState: PageState option -> unit
    setArcFileState: ArcFiles option -> unit
    setSelectedTreeItemPath: string option -> unit
    landingCtx: StateContext<Renderer.Context.LandingStateCtx.LandingState>
    notesCtx: StateContext<Renderer.Context.NotesStateCtx.NotesState>
}

let private clearSelectionAndPreview (deps: Deps) =
    deps.setSelectedTreeItemPath None
    deps.setArcFileState None

let openLandingPage (deps: Deps) =
    Renderer.Context.LandingStateCtx.reset deps.landingCtx
    clearSelectionAndPreview deps
    deps.setPageState (Some PageState.LandingDraft)

let openNotesPage (deps: Deps) =
    Renderer.Context.NotesStateCtx.reset deps.notesCtx
    clearSelectionAndPreview deps
    deps.setPageState (Some PageState.NotesDraft)

let openNotesSearchPage (deps: Deps) =
    clearSelectionAndPreview deps
    deps.setPageState (Some PageState.NotesSearch)

let openArcStartPage (deps: Deps) =
    Renderer.Context.NotesStateCtx.reset deps.notesCtx
    openLandingPage deps
