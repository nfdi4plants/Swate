namespace Swate.Components

open Fable.Core
open Fable.Core.JsInterop
open Feliz

open AuthenticationTypes


type private DataHubInformation = {
    Name: string
    Url: string
    Description: string option
} with

    member this.GetDescription() =
        match this.Description with
        | Some description -> description
        | None -> this.Url


type private DataHubCollection = {
    Default: DataHubInformation
    Supported: DataHubInformation list
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

    let getUserAPIRequest (signInInfo: SignInInformation) : JS.Promise<Result<UserInformation, SignInError>> = promise {
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
                    let! gitLabUserInfo = response.json<AuthenticationTypes.GitLabUser> ()

                    let userInfo =
                        UserInformation.FromGitLabUser gitLabUserInfo pat signInInfo.GitLabBaseUrl

                    return Ok userInfo

                with ex ->
                    return Error(SignInError.DecodeError ex)

        // ---- NETWORK ERROR ----
        with ex ->
            return Error(SignInError.NetworkError ex)
    }

module private AuthenticationHelper =

    let prefillGitLabPATScopes (gitlabBaseUrl: string) =
        let gitlabBaseUrl = gitlabBaseUrl.TrimEnd('/')

        let scopes = [
            "api"
            "read_user"
            "read_repository"
            "read_api"
            "write_repository"
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

    [<Literal>]
    let Default_DataHub_Url = "https://git.nfdi4plants.org/"

    let Default_DataHub = {
        Name = "PLANTdataHUB (offical)"
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

[<Erase; Mangle(false)>]
type Authentication =

    [<ReactComponent>]
    static member private AvatarPlaceholder() =
        Html.div [
            prop.className [ "swt:avatar swt:avatar-placeholder swt:size-full" ]
            prop.children [
                Html.div [
                    prop.className "swt:bg-neutral swt:text-neutral-content swt:rounded-full swt:p-2"
                    prop.children [
                        Html.span [
                            prop.className "swt:size-full swt:iconify swt:fluent--person-12-filled"
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private UserAvatarIcon(userInformation: UserInformation) =
        Html.img [
            prop.className "swt:w-8 swt:h-8 swt:rounded-full"
            prop.src userInformation.AvatarUrl
            prop.alt userInformation.Name
        ]


    [<ReactComponent>]
    static member private CustomDataHubRadioItem(activeDataHub, radioid, setDataHub: DataHubInformation -> unit) =

        let customDataHub, setCustomDataHub = React.useState "https://"

        let mkCustomDataHubInfoFromUrl (url: string) : DataHubInformation = {
            Name = url
            Url = url
            Description = None
        }

        Html.label [
            prop.className "swt:label swt:gap-2"
            prop.children [
                Html.input [
                    prop.testId "CustomDataHubRadio"
                    prop.type'.radio
                    prop.name radioid
                    prop.className "swt:radio swt:radio-sm"
                    prop.isChecked (activeDataHub.Url = customDataHub)
                    prop.onChange (fun (_: bool) -> setDataHub (mkCustomDataHubInfoFromUrl customDataHub))
                ]
                Html.input [
                    prop.testId "CustomDataHubInput"
                    prop.className "swt:input swt:input-xs"
                    prop.value customDataHub
                    prop.placeholder "https://"
                    prop.onChange (fun value ->
                        setCustomDataHub value
                        setDataHub (mkCustomDataHubInfoFromUrl value)
                    )
                ]
            ]
        ]

    [<ReactComponent>]
    static member private DataHubRadioItem(activeDataHub, radioid, dataHub, setDataHub, ?radioClassName) =
        Html.label [
            prop.className "swt:label swt:gap-2"
            prop.children [
                Html.input [
                    prop.testId ($"DataHubRadio-{dataHub.Name}")
                    prop.type'.radio
                    prop.name radioid
                    prop.id dataHub.Url
                    prop.className (Option.defaultValue "swt:radio swt:radio-sm" radioClassName)
                    prop.isChecked (activeDataHub.Url = dataHub.Url)
                    prop.onChange (fun (_: bool) -> setDataHub dataHub)
                ]
                Html.span [ prop.text dataHub.Name; prop.title dataHub.Url ]
            ]
        ]

    [<ReactComponent>]
    static member private DataHubSelect(dataHub: DataHubInformation, setDataHub: DataHubInformation -> unit) =

        let radioid = React.useId ()

        Html.fieldSet [
            prop.className "swt:fieldset"
            prop.children [
                Html.legend [
                    prop.className "swt:fieldset-legend"
                    prop.text "Select DataHub"
                ]
                Html.div [
                    prop.className
                        "swt:flex swt:flex-col swt:w-full swt:cursor-not-allowed swt:gap-0.5 swt:p-2 swt:rounded swt:border swt:border-dashed swt:border-base-content"
                    prop.children [
                        Html.p [
                            prop.className "swt:text-base-content swt:text-lg swt:font-semibold"
                            prop.text dataHub.Name
                        ]
                        Html.p [
                            prop.className "swt:text-xs swt:text-base-content/70"
                            prop.text (dataHub.GetDescription())
                        ]
                        Html.a [
                            prop.href dataHub.Url
                            prop.target "_blank"
                            prop.className "swt:link swt:link-info"
                            prop.text dataHub.Url
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:label"
                    prop.children [
                        Html.details [
                            prop.className "swt:collapse swt:w-full swt:collapse-arrow"
                            prop.children [
                                Html.summary [
                                    prop.testId "DataHubOptionsToggle"
                                    prop.className "swt:collapse-title swt:p-1"
                                    prop.children [ Html.span "Other options" ]
                                ]
                                Html.div [
                                    prop.className "swt:collapse-content swt:text-sm swt:flex swt:flex-col swt:gap-0.5"
                                    prop.children [
                                        Authentication.DataHubRadioItem(
                                            dataHub,
                                            radioid,
                                            AuthenticationHelper.Default_DataHub_Collection.Default,
                                            setDataHub,
                                            radioClassName = "swt:radio swt:radio-sm swt:radio-primary"
                                        )
                                        for dataHubI in AuthenticationHelper.Default_DataHub_Collection.Supported do
                                            Authentication.DataHubRadioItem(dataHub, radioid, dataHubI, setDataHub)
                                        Authentication.CustomDataHubRadioItem(dataHub, radioid, setDataHub)
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private NotAuthenticatedView(onSignIn: SignInInformation -> unit, setError: exn option -> unit) =

        let datahubUrl, setDataHubUrl = React.useState AuthenticationHelper.Default_DataHub
        let pat, setPat = React.useState ""

        Html.div [
            prop.className "swt:flex swt:flex-col swt:p-4 swt:w-md swt:gap-2"
            prop.children [
                Html.h1 [
                    prop.text "DataHub Authentication"
                    prop.className "swt:text-center swt:text-3xl swt:font-bold swt:mb-2"
                ]
                Html.p [
                    prop.text "Sign in to your DataHub account to access your ARCs."
                    prop.className "swt:text-center swt:text-sm swt:text-base-content/80"
                ]
                Html.fieldSet [
                    prop.className "swt:fieldset"
                    prop.children [
                        Html.label [
                            prop.className "swt:fieldset-legend"
                            prop.text "Personal Access Token"
                        ]
                        Html.input [
                            prop.testId "PersonalAccessTokenInput"
                            prop.className "swt:input swt:w-full"
                            prop.placeholder "Personal Access token"
                            prop.type'.password
                            prop.value pat
                            prop.onChange (fun value -> setPat value)
                        ]
                    ]
                ]
                Authentication.DataHubSelect(datahubUrl, setDataHubUrl)
                Html.a [
                    prop.testId "GeneratePatLink"
                    prop.className "swt:link swt:link-info swt:text-sm swt:text-center swt:py-2"
                    prop.text "Click here to generate a new GitLab Personal Access Token"
                    prop.href (AuthenticationHelper.prefillGitLabPATScopes datahubUrl.Url)
                    prop.target.blank
                ]
                Html.button [
                    prop.testId "SignInButton"
                    prop.className "swt:btn swt:btn-primary swt:btn-lg swt:w-full"
                    prop.text "Sign In"
                    prop.disabled (
                        System.String.IsNullOrWhiteSpace pat
                        || System.String.IsNullOrWhiteSpace datahubUrl.Url
                    )
                    prop.onClick (fun _ ->
                        onSignIn {
                            GitLabBaseUrl = datahubUrl.Url
                            PersonalAccessToken = pat
                            OnErrorCallback = fun ex -> setError (Some ex)
                        }
                    )
                ]
                Html.p [
                    prop.className "swt:text-sm swt:text-center swt:text-base-content/80"
                    prop.text "Don't have an account? Sign up on your DataHub instance to create one."
                ]
            ]
        ]

    [<ReactComponent>]
    static member private UserInfo(user: UserInformation, onLogout: unit -> unit) =
        React.Fragment [
            // header area with name and email
            Html.li [
                prop.children [
                    Html.a [
                        prop.className "swt:justify-between"
                        prop.text user.Name
                    ]
                    Html.p [
                        prop.className "swt:text-sm swt:text-gray-500"
                        prop.text user.Email
                    ]
                    Html.p [
                        prop.testId "LoggedInDataHub"
                        prop.className "swt:text-xs swt:text-gray-500"
                        prop.textf "DataHub: %s" user.TargetDataHub
                    ]
                ]
            ]
            // options area with a list of buttons, for now only logout
            Html.li [
                prop.children [
                    Html.button [
                        prop.testId "LogoutButton"
                        prop.className "swt:btn swt:btn-error swt:btn-outline swt:btn-sm swt:w-full"
                        prop.text "Logout"
                        prop.onClick (fun _ -> onLogout ())
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member UserAvatar
        (
            userInformation: UserInformation option,
            onSignIn: SignInInformation -> unit,
            onLogout: unit -> unit,
            ?isLoading: bool
        ) =

        let isOpen, setIsOpen = React.useState false
        let error, setError = React.useState (None: exn option)

        let onSignIn =
            fun signInInfo ->
                setError None
                setIsOpen false
                onSignIn signInInfo

        let ToggleContent =
            React.useMemo (
                (fun () ->
                    match isLoading, userInformation with
                    | Some true, _ ->
                        Html.span [
                            prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                        ]
                    | _, Some userInformation -> Authentication.UserAvatarIcon(userInformation)
                    | _, None -> Authentication.AvatarPlaceholder()
                ),
                [| box userInformation; box isLoading |]
            )

        let content =
            React.useMemo (
                (fun () ->
                    match userInformation with
                    | Some userInformation -> Authentication.UserInfo(userInformation, onLogout)
                    | None -> Authentication.NotAuthenticatedView(onSignIn, setError)
                ),
                [| box userInformation |]
            )

        React.Fragment [
            BaseModal.ErrorBaseModal(
                error.IsSome,
                (fun _ -> setError None),
                error
                |> Option.map _.Message
                |> Option.defaultValue "An unknown error occurred."
            )
            Dropdown.Main(
                isOpen,
                setIsOpen,
                Html.button [
                    prop.testId "UserButtonToggle"
                    prop.onClick (fun _ -> setIsOpen (not isOpen))
                    prop.className "swt:btn swt:btn-circle"
                    prop.children [ ToggleContent ]
                ],
                Html.div [ prop.testId "UserDropdownContent"; prop.children content ],
                contentClassName =
                    "swt:flex swt:flex-col swt:min-w-xs swt:gap-2 swt:border-base-content swt:border-1 swt:p-2 swt:shadow-lg swt:rounded swt:bg-base-100 swt:top-[110%]",
                closeOnClick = false
            )
        ]

    [<ReactComponent>]
    static member Entry() =

        let userInformation, setUserInformation =
            React.useState (None: UserInformation option)

        let isLoading, setIsLoading = React.useState false

        let exmpUserInformation = {
            Name = "John Doe"
            Email = "john-doe@mail.com"
            AvatarUrl = "https://www.gravatar.com/avatar/00000000000000000000000000000000?d=mp&f=y"
            Token = "1234567890"
            TargetDataHub = AuthenticationHelper.Default_DataHub_Url
        }

        let onSignIn =
            fun (signInInfo: SignInInformation) ->
                promise {
                    setIsLoading true
                    do! Promise.sleep 1000
                    // Testing, ignore for story tests
                    // let! userInformation = GitLabAPI.getUserAPIRequest signInInfo
                    // Here should be try get user information from the DataHub using the provided signInInfo and handle possible errors by calling signInInfo.OnErrorCallback with the error message. For now we just simulate a successful sign in with example user information and a delay.
                    setUserInformation (
                        Some {
                            exmpUserInformation with
                                TargetDataHub = signInInfo.GitLabBaseUrl
                                Token = signInInfo.PersonalAccessToken
                        }
                    )

                    setIsLoading false
                }
                |> Promise.start

        let onLogout = fun () -> setUserInformation None

        Html.div [
            prop.className "swt:flex swt:flex-col swt:m-10 swt:gap-2"
            prop.children [
                Html.h1 [
                    prop.text "Authentication"
                    prop.className "swt:text-2xl swt:font-bold swt:mb-4"
                ]
                Html.p [
                    prop.testId "SignedInInfo"
                    prop.textf "Signed In: %b" (Option.isSome userInformation)
                ]
                Authentication.UserAvatar(userInformation, onSignIn, onLogout, isLoading = isLoading)
            ]
        ]