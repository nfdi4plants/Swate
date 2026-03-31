module Renderer.Context.AuthStateCtx

open Feliz
open Swate.Electron.Shared.AuthTypes
open Fable.Electron.Remoting.Renderer
open Swate.Components.AuthenticationTypes


module private AuthStateHelper =

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
let useAuthState () =
    React.useContext AuthStateHelper.AuthStateCtx

open AuthStateHelper

/// This component stores the current account information. If you want to log out, switch account, login, etc.
/// - you simply can use the IAuthApi via IPC. Any changes will be broadcasted to all open windows via: `authAccountsUpdate`
[<ReactComponent>]
let Provider (children: ReactElement) =
    let authState, setAuthState = React.useState AuthStateDto.Empty

    let ipcHandler: Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi = {
        Swate.Electron.Shared.IPCTypes.IMainUpdateRendererApi.empty with
            authAccountsUpdate = fun accounts -> setAuthState ({ Accounts = accounts }: AuthStateDto)
    }

    React.useEffectOnce (fun _ ->
        Remoting.init |> Remoting.buildHandler ipcHandler

        // TODO: Add error handling.
        promise {
            match! Api.ipcAuthApi.revalidate () with
            | Ok response ->
                match response with
                | { Success = true } -> do! refreshState setAuthState ignore
                | {
                      Success = false
                      Message = msg
                      FailureKind = err
                  } -> console.error (err, Fable.Core.JS.JSON.stringify msg)
            | Error ex -> console.error (Fable.Core.JS.JSON.stringify ex.Message)

        }
        |> Promise.start
    )


    AuthStateCtx.Provider(authState, children)