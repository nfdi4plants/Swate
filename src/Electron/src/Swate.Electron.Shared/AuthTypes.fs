module Swate.Electron.Shared.AuthTypes

open Swate.Components.AuthenticationTypes

/// Request sent from Renderer to Main to sign in.
type AuthSignInRequest = {
    GitLabBaseUrl: string
    PersonalAccessToken: string
}

/// Typed failure categories for auth operations.
[<RequireQualifiedAccess>]
[<Fable.Core.StringEnum>]
type AuthFailureKind =
    | Unauthorized
    | Forbidden
    | Network
    | EndpointInvalid
    | StorageUnavailable
    | Unknown

/// Result returned by auth sign-in / revalidate operations.
type AuthResult = {
    Success: bool
    User: Swate.Components.AuthenticationTypes.AuthStateDto option
    FailureKind: AuthFailureKind option
    Message: string option
}