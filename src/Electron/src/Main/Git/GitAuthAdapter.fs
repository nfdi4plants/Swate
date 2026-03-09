module Main.Git.GitAuthAdapter

open System
open System.Text.RegularExpressions
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.SimpleGit

type GitFactory = SimpleGitOptions -> ISimpleGit

let private redactPattern =
    Regex(@"(Authorization:\s*Bearer\s+)[^\s'""]+", RegexOptions.IgnoreCase)

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

let buildAuthArgs (_host: string) (token: string) : string[] = [|
    "-c"
    $"http.extraHeader=Authorization: Bearer {token}"
|]

let applyAuth (gitFactory: GitFactory) (baseOptions: SimpleGitOptions) (host: string) (token: string) : ISimpleGit =
    let authArgs = buildAuthArgs host token
    let authConfig = toConfigEntries authArgs

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
        |> fun t -> redactPattern.Replace(t, "$1[REDACTED]")
        |> fun t -> credentialUrlPattern.Replace(t, "$1[REDACTED]@")

let redactArgs (args: string[]) : string[] = args |> Array.map redactToken