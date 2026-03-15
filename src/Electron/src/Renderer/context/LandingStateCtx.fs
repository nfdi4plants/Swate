module Renderer.Context.LandingStateCtx

open Swate.Components.Landing
open Feliz
open Swate.Components

type LandingState = {
    Draft: LandingDraft
    UiState: LandingUiState
} with

    static member init() = {
        Draft = LandingDraft.init
        UiState = LandingUiState.init
    }

let reset (ctx: StateContext<LandingState>) =
    ctx.setState (LandingState.init ())

let LandingStateCtx =
    React.createContext<StateContext<LandingState>> (StateContext.init (LandingState.init ()))
