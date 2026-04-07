module Main.IPC.AuthApi

open Fable.Core
open Fable.Electron.Remoting.Main
open Main
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.AuthTypes
open Main.Auth

let private broadcastAccountsUpdate () =
    let authState = AuthService.getState ()

    ARC_VAULTS.Vaults.Values
    |> Array.ofSeq
    |> Array.iter (fun window ->
        Remoting.init
        |> Remoting.withWindow window.window
        |> Remoting.buildClient<IMainUpdateRendererApi>
        |> fun client -> client.authAccountsUpdate authState
    )

let api: IAuthApi = {
    signIn =
        fun (request: AuthSignInRequest) -> promise {
            try
                let! result = AuthService.signIn request

                if result.Success then
                    broadcastAccountsUpdate ()

                return Ok result
            with _ ->
                return Error(exn "Sign-in failed due to an unexpected error.")
        }
    getAuthState =
        fun () -> promise {
            try
                return Ok(AuthService.getState ())
            with _ ->
                return Error(exn "Failed to retrieve auth state.")
        }
    signOut =
        fun () -> promise {
            try
                AuthService.signOut ()
                broadcastAccountsUpdate ()
                return Ok()
            with _ ->
                return Error(exn "Sign-out failed due to an unexpected error.")
        }
    revalidate =
        fun () -> promise {
            try
                let! result = AuthService.revalidate ()
                broadcastAccountsUpdate ()
                return Ok result
            with _ ->
                return Error(exn "Token revalidation failed due to an unexpected error.")
        }
    listAccounts =
        fun () -> promise {
            try
                return Ok(AuthService.listAccounts ())
            with _ ->
                return Error(exn "Failed to list accounts.")
        }
    setActiveAccount =
        fun (accountId: string) -> promise {
            try
                let state = AuthService.setActiveAccount accountId
                broadcastAccountsUpdate ()
                return Ok state
            with _ ->
                return Error(exn "Failed to switch active account.")
        }
    removeAccount =
        fun (accountId: string) -> promise {
            try
                AuthService.removeAccount accountId
                broadcastAccountsUpdate ()
                return Ok()
            with _ ->
                return Error(exn "Failed to remove account.")
        }
}