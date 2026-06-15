module Swate.Components.Composite.Authentication.Helper

open Types
open Swate.Components

type DataHubInformation = {
    Name: string
    Url: string
    Description: string option
} with

    member this.GetDescription() =
        match this.Description with
        | Some description -> description
        | None -> this.Url


type DataHubCollection = {
    Default: DataHubInformation
    Supported: DataHubInformation list
}

module GitLabUrls =
    let prefillGitLabPATScopes (gitlabBaseUrl: string) =
        let gitlabBaseUrl = gitlabBaseUrl.TrimEnd('/')

        let scopes = [
            "read_user"
            "read_repository"
            "read_api"
            "write_api"
            "write_repository"
            "self_rotate" // This is used to allow users to rotate their token from within Swate without having to log in to GitLab. It is a scope that only allows the token itself to be revoked, not any other tokens or account access.
        ]

        let scopeParam = scopes |> String.concat ","

        let description =
            "Swate Electron App. Gives access to your repositories and allows Swate to read your user information. This is used to authenticate you and access your ARCs. You can revoke this token at any time without affecting any other tokens or your account."
                .Replace(" ", "%20")

        sprintf
            "%s/-/user_settings/personal_access_tokens?name=swate-electron&description=%s&scopes=%s"
            gitlabBaseUrl
            description
            scopeParam

    let profileUrl (user: Types.AuthUserDto) =
        let normalizedBaseUrl = user.TargetDataHub.TrimEnd('/')
        $"{normalizedBaseUrl}/-/u/{user.Id}"

[<Literal>]
let Default_DataHub_Url = "https://git.nfdi4plants.org/"

let Default_DataHub = {
    Name = "PLANTdataHUB (official)"
    Url = Default_DataHub_Url
    Description = Some "The official PLANTdataHUB instance, hosted by the nfdi4plants. Recommended for most users."
}

let Default_DataHub_Collection = {
    Default = Default_DataHub
    Supported = [
        {
            Name = "gitlab.plantmicrobe.de"
            Url = "https://gitlab.plantmicrobe.de/"
            Description = None
        }
        {
            Name = "datahub.rz.rptu.de"
            Url = "https://datahub.rz.rptu.de/"
            Description = None
        }
    ]
}


module GitLabAPI =

    open Fetch

    [<RequireQualifiedAccess>]
    type SignInError =
        | NetworkError of exn
        | Unauthorized
        | Forbidden
        | NotFound
        | HttpError of int
        | DecodeError of exn

    let getUserAPIRequest (signInInfo: SignInInformation) : Fable.Core.JS.Promise<Result<AuthUserDto, SignInError>> = promise {
        let baseUrl = signInInfo.GitLabBaseUrl.TrimEnd('/')
        let pat = signInInfo.PersonalAccessToken
        let url = $"{baseUrl}/api/v4/user"

        let requestOptions = [
            RequestProperties.Method HttpMethod.GET
            requestHeaders [ HttpRequestHeaders.Custom("PRIVATE-TOKEN", pat) ]
        ]

        try
            console.log ($"Making API request to {url} with PAT")
            let! response = fetchUnsafe url requestOptions

            // ---- HTTP STATUS HANDLING ----
            if not response.Ok then
                return
                    match response.Status with
                    | 401 -> Result.Error SignInError.Unauthorized
                    | 403 -> Result.Error SignInError.Forbidden
                    | 404 -> Result.Error SignInError.NotFound
                    | code -> Result.Error(SignInError.HttpError code)
            else

                // ---- JSON PARSE ----
                try
                    let! gitLabUserInfo = response.json<Types.GitLabUser> ()

                    let userInfo = AuthUserDto.FromGitLabUser gitLabUserInfo signInInfo.GitLabBaseUrl

                    return Ok userInfo

                with ex ->
                    return Error(SignInError.DecodeError ex)

        // ---- NETWORK ERROR ----
        with ex ->
            return Error(SignInError.NetworkError ex)
    }
