module Swate.Electron.Shared.AuthTypes

/// Request sent from Renderer to Main to sign in.
type AuthSignInRequest = {
    GitLabBaseUrl: string
    PersonalAccessToken: string
}

/// User information returned on successful authentication.
type AuthUserDto = {
    AccountId: string
    Name: string
    Email: string
    AvatarUrl: string
    TargetDataHub: string
}

/// Typed failure categories for auth operations.
[<RequireQualifiedAccess>]
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
    User: AuthUserDto option
    FailureKind: AuthFailureKind option
    Message: string option
}

/// Summary of a stored account for listing purposes.
type AuthAccountSummary = {
    AccountId: string
    Name: string
    Email: string
    AvatarUrl: string
    TargetDataHub: string
    IsActive: bool
}

/// Current auth state returned by getAuthState.
type AuthStateDto = {
    IsAuthenticated: bool
    User: AuthUserDto option
    Accounts: AuthAccountSummary array
}