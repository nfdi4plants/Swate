module Renderer.context.LandingStateCtx

open Swate.Components.Landing
open Feliz

type LandingStateContext = {
    Draft: LandingDraft
    SetDraft: LandingDraft -> unit
    UiState: LandingUiState
    SetUiState: LandingUiState -> unit
    Reset: unit -> unit
} with

    static member init () = {
        Draft = LandingDraft.init
        SetDraft = ignore
        UiState = LandingUiState.init
        SetUiState = ignore
        Reset = ignore
    }

let LandingStateCtx =
    React.createContext<LandingStateContext> (LandingStateContext.init ())
