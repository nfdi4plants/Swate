module Main.Auth.GitLabApi

open Fable.Core
open Fetch
open Swate.Electron.Shared.AuthTypes

type GitLabAuthFailure = {
    Kind: AuthFailureKind
    Message: string
}

/// Call GitLab /api/v4/user with the provided PAT to verify the token.
/// Uses Fable.Fetch following the pattern in Authentication.fs.
let verifyToken (baseUrl: string) (pat: string) : JS.Promise<Result<AuthUserDto, GitLabAuthFailure>> = promise {
    let url = $"{baseUrl}/api/v4/user"

    let requestOptions = [
        RequestProperties.Method HttpMethod.GET
        requestHeaders [ HttpRequestHeaders.Custom("PRIVATE-TOKEN", pat) ]
    ]

    try
        let! response = fetchUnsafe url requestOptions

        if not response.Ok then
            return
                match response.Status with
                | 401 ->
                    Error {
                        Kind = AuthFailureKind.Unauthorized
                        Message = "Personal Access Token is invalid or expired."
                    }
                | 403 ->
                    Error {
                        Kind = AuthFailureKind.Forbidden
                        Message = "Access forbidden. The token may lack required scopes."
                    }
                | 404 ->
                    Error {
                        Kind = AuthFailureKind.EndpointInvalid
                        Message = "GitLab API endpoint not found. Check the DataHub URL."
                    }
                | code ->
                    Error {
                        Kind = AuthFailureKind.Unknown
                        Message = $"Unexpected HTTP status {code}."
                    }
        else
            let! json =
                response.json<
                    {|
                        name: string
                        email: string
                        avatar_url: string
                    |}
                 > ()

            let accountId = SecureAuthStore.generateAccountId baseUrl json.email

            let user: AuthUserDto = {
                AccountId = accountId
                Name = json.name
                Email = json.email
                AvatarUrl = json.avatar_url
                TargetDataHub = baseUrl
            }

            return Ok user
    with _ ->
        return
            Error {
                Kind = AuthFailureKind.Network
                Message = "Network error contacting the DataHub. Check your connection."
            }
}