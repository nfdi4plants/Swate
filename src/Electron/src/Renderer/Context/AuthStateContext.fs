module Renderer.Context.AuthStateContext

open Feliz
open Swate.Components.Authentication.Types

let AuthStateCtx = React.createContext<AuthStateDto> AuthStateDto.Empty

[<Hook>]
let useAuthStateCtx () = React.useContext AuthStateCtx

/// This component stores the current account information. If you want to log out, switch account, login, etc.
/// - you simply can use the IAuthApi via IPC. Any changes will be broadcasted to all open windows via: `authAccountsUpdate`
[<ReactComponent>]
let Provider (children: ReactElement) =
    let authSnapshot = Renderer.MainUpdateRendererBridge.useAuthAccountsUpdate ()
    AuthStateCtx.Provider(authSnapshot.Value, children)
