module Renderer.Context.AuthStateContext

open Feliz
open Swate.Electron.Shared.AuthTypes
open Swate.Components.Authentication.Types


module private AuthStateHelper =

    let private isEmptyAuthState (state: AuthStateDto) =
        state.ActiveAccount.IsNone && state.StoredAccounts.Length = 0

    let shouldLogRevalidationFailure (response: AuthResult) =
        match response.Success, response.FailureKind, response.User with
        | false, Some AuthFailureKind.Unauthorized, Some state when isEmptyAuthState state -> false
        | false, _, _ -> true
        | true, _, _ -> false

    let refreshState (setAuthState) (onError) = promise {
        let! stateResult = Api.ipcAuthApi.getAuthState ()

        match stateResult with
        | Ok state -> setAuthState state

        | Error _ ->
            setAuthState AuthStateDto.Empty
            onError ()
    }

    let AuthStateCtx = React.createContext<AuthStateDto> AuthStateDto.Empty

[<Hook>]
let useAuthStateCtx () =
    React.useContext AuthStateHelper.AuthStateCtx

open AuthStateHelper

/// This component stores the current account information. If you want to log out, switch account, login, etc.
/// - you simply can use the IAuthApi via IPC. Any changes will be broadcasted to all open windows via: `authAccountsUpdate`
[<ReactComponent>]
let Provider (children: ReactElement) =
    let authState, setAuthState = React.useState AuthStateDto.Empty

    React.useEffectOnce (fun () ->
        let disposeAuthSubscription = Renderer.MainUpdateRendererBridge.subscribeAuthAccountsUpdate setAuthState

        // TODO: Add error handling.
        promise {
            match! Api.ipcAuthApi.revalidate () with
            | Ok response ->
                do! refreshState setAuthState ignore

                if shouldLogRevalidationFailure response then
                    console.error (response.FailureKind, Fable.Core.JS.JSON.stringify response.Message)
            | Error ex -> console.error (Fable.Core.JS.JSON.stringify ex.Message)

        }
        |> Promise.start

        disposeAuthSubscription
    )


    AuthStateCtx.Provider(authState, children)
