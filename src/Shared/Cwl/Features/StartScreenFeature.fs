module Swate.Components.Shared.Cwl.Features.StartScreenFeature

open Swate.Components.Shared.Cwl.State.Types

type StartScreenProps = {
    ErrorMessage: string option
    IsLoading: bool
}

let toStartScreenProps (state: AppState) : StartScreenProps = {
    ErrorMessage = state.Notifications.ErrorMessage
    IsLoading = state.Async.IsLoading
}
