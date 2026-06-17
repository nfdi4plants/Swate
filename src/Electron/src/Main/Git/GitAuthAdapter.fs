module Main.Git.GitAuthAdapter

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.SimpleGit

type GitFactory = SimpleGitOptions -> ISimpleGit

/// Auth material that can be applied either through simple-git config entries or spawned git commands.
/// ConfigArgs contains `-c key=value` pairs; Environment includes non-interactive prompt suppression.
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

/// Converts command-line `-c key=value` pairs into simple-git config entries.
/// Used by applyAuth so credentials stay scoped to the in-memory git instance.
let toConfigEntries (args: string[]) =
    args
    |> Array.mapi (fun index value ->
        if index > 0 && args.[index - 1] = "-c" then
            Some value
        else
            None
    )
    |> Array.choose id

/// Builds a git process environment that disables terminal prompts.
/// All Git entry points should use this so Electron never blocks waiting for credentials.
let createNonInteractiveEnv () : obj =
    // Keep existing env variables but drop unsafe git/editor overrides that simple-git rejects by default.
    let safeEnv: obj =
        emitJsExpr
            ()
            """
            (() => {
                const blocked = new Set([
                    'editor',
                    'git_askpass',
                    'git_config_global',
                    'git_config_system',
                    'git_config_count',
                    'git_config',
                    'git_editor',
                    'git_exec_path',
                    'git_external_diff',
                    'git_pager',
                    'git_proxy_command',
                    'git_template_dir',
                    'git_sequence_editor',
                    'git_ssh',
                    'git_ssh_command',
                    'pager',
                    'prefix',
                    'ssh_askpass'
                ]);
                const source = process.env ?? {};
                const safeEnv = {};

                for (const [key, value] of Object.entries(source)) {
                    if (!blocked.has(String(key).toLowerCase())) {
                        safeEnv[key] = value;
                    }
                }

                safeEnv.GIT_TERMINAL_PROMPT = '0';
                return safeEnv;
            })()
            """

    GitCommandResolver.ensureGitToolPath safeEnv

/// Applies the non-interactive environment to a simple-git instance.
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

let private tryBuildScopedAuthUrl (remoteUrl: string) =
    let mutable uri = Unchecked.defaultof<Uri>

    if
        Uri.TryCreate(remoteUrl, UriKind.Absolute, &uri)
        && uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
    then
        let authority =
            if uri.IsDefaultPort then
                uri.Host
            else
                $"{uri.Host}:{uri.Port}"

        Some($"{uri.Scheme}://{authority}/")
    else
        None

/// Builds per-command auth config for HTTPS Git and Git LFS operations.
/// The token is injected as scoped config and optional authenticated remote URLs; nothing is persisted to repository config.
let buildAuthArgs (_host: string) (token: string) (remoteName: string option) (remoteUrl: string option) : string[] = [|
    let authorizationValue = buildBasicAuthorizationValue gitLabBasicAuthUsername token

    match remoteUrl |> Option.bind tryBuildScopedAuthUrl with
    | Some scopeUrl ->
        yield "-c"
        yield $"http.{scopeUrl}.extraHeader=Authorization: {authorizationValue}"
    | _ -> ()

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
    | _ -> ()
|]

/// Creates auth data for lower-level spawned commands such as `git lfs push`.
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

/// Returns a simple-git instance with authentication config merged into the supplied base options.
/// Use this instead of writing credentials to `.git/config`.
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

/// Redacts bearer/basic headers and credential URLs before errors or diagnostics leave Main.
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
            tokenHeaderRedactPattern.Replace(t, MatchEvaluator(fun matched -> $"{matched.Groups.[1].Value}[REDACTED]"))
        |> fun t -> credentialUrlPattern.Replace(t, "$1[REDACTED]@")

/// Redacts each command argument for diagnostic output.
let redactArgs (args: string[]) : string[] = args |> Array.map redactToken
