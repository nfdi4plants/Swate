module Main.Git.GitAuthAdapter

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.SimpleGit

type GitFactory = SimpleGitOptions -> ISimpleGit

type GitCommandAuthentication = {
    ConfigArgs: string[]
    Environment: obj
}

let private authorizationRedactPattern =
    Regex(@"(Authorization:\s*)(?:Bearer\s*|Basic\s*)[^\s'""]+", RegexOptions.IgnoreCase)

let private tokenHeaderRedactPattern =
    Regex(@"((?:Private-Token|X-Access-Token)\s*:\s*)[^\s'""]+", RegexOptions.IgnoreCase)

let private credentialUrlPattern =
    Regex("(https?://)([^\\s/@]+(?::[^\\s/@]*)?@)", RegexOptions.IgnoreCase)

let private baseConfigEntries (options: SimpleGitOptions) =
    options.config |> Option.defaultValue [||]

let toConfigEntries (args: string[]) =
    args
    |> Array.mapi (fun index value ->
        if index > 0 && args.[index - 1] = "-c" then
            Some value
        else
            None
    )
    |> Array.choose id

let createNonInteractiveEnv () : obj =
    // Keep all existing environment variables and disable git interactive prompts.
    emitJsExpr () "{ ...process.env, GIT_TERMINAL_PROMPT: '0' }"

let applyNonInteractiveEnv (git: ISimpleGit) = git.env (createNonInteractiveEnv ())

[<Emit("Buffer.from($0, 'utf8').toString('base64')")>]
let private toBase64 (value: string) : string = jsNative

[<Emit("encodeURIComponent($0)")>]
let private encodeUriComponent (value: string) : string = jsNative

let private gitLabBasicAuthUsername = "oauth2"

let private buildBasicAuthorizationValue (username: string) (token: string) =
    let credentials = $"{username}:{token}"
    let base64Credentials = toBase64 credentials
    $"Basic {base64Credentials}"

let private buildAuthenticatedRemoteUrl (remoteUrl: string) (token: string) =
    if remoteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then
        let remoteUrlWithoutScheme = remoteUrl.Substring("https://".Length)
        let encodedToken = encodeUriComponent token
        $"https://{gitLabBasicAuthUsername}:{encodedToken}@{remoteUrlWithoutScheme}"
    else
        remoteUrl

let private buildLfsEndpointUrl (remoteUrl: string) =
    let trimmedRemoteUrl = remoteUrl.TrimEnd('/')
    $"{trimmedRemoteUrl}/info/lfs"

let buildAuthArgs (_host: string) (token: string) (remoteName: string option) (remoteUrl: string option) : string[] = [|
    yield "-c"
    yield $"http.extraHeader=Authorization: {buildBasicAuthorizationValue gitLabBasicAuthUsername token}"

    match remoteName, remoteUrl with
    | Some name, Some url when url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ->
        let authenticatedRemoteUrl = buildAuthenticatedRemoteUrl url token
        let plainLfsUrl = buildLfsEndpointUrl url
        let authenticatedLfsUrl = buildLfsEndpointUrl authenticatedRemoteUrl
        yield "-c"
        yield $"remote.{name}.url={authenticatedRemoteUrl}"
        yield "-c"
        yield $"remote.{name}.pushurl={authenticatedRemoteUrl}"
        yield "-c"
        yield $"remote.{name}.lfsurl={authenticatedLfsUrl}"
        yield "-c"
        yield $"lfs.url={authenticatedLfsUrl}"
        yield "-c"
        yield $"lfs.{plainLfsUrl}.access=basic"
        yield "-c"
        yield $"lfs.{authenticatedLfsUrl}.access=basic"
    | _ ->
        ()
|]

let createCommandAuthentication
    (host: string)
    (token: string)
    (remoteName: string option)
    (remoteUrl: string option)
    : GitCommandAuthentication =
    {
        ConfigArgs = buildAuthArgs host token remoteName remoteUrl
        Environment = createNonInteractiveEnv ()
    }

let applyAuth
    (gitFactory: GitFactory)
    (baseOptions: SimpleGitOptions)
    (host: string)
    (token: string)
    (remoteName: string option)
    (remoteUrl: string option)
    : ISimpleGit =
    let commandAuth = createCommandAuthentication host token remoteName remoteUrl
    let authConfig = toConfigEntries commandAuth.ConfigArgs

    let mergedConfig = [|
        yield! baseConfigEntries baseOptions
        yield! authConfig
    |]

    let scopedOptions: SimpleGitOptions =
        emitJsExpr (baseOptions, mergedConfig) "{ ...$0, config: $1 }"

    gitFactory scopedOptions

let redactToken (text: string) : string =
    if String.IsNullOrWhiteSpace text then
        text
    else
        text
        |> fun t ->
            authorizationRedactPattern.Replace(
                t,
                MatchEvaluator(fun matched -> $"{matched.Groups.[1].Value}[REDACTED]")
            )
        |> fun t ->
            tokenHeaderRedactPattern.Replace(
                t,
                MatchEvaluator(fun matched -> $"{matched.Groups.[1].Value}[REDACTED]")
            )
        |> fun t -> credentialUrlPattern.Replace(t, "$1[REDACTED]@")

let redactArgs (args: string[]) : string[] = args |> Array.map redactToken
