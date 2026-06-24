module GHActions

open System
open System.IO


let setGitHubOutput (name: string) (value: string) =
    match Environment.GetEnvironmentVariable("GITHUB_OUTPUT") with
    | null
    | "" -> printfn "GITHUB_OUTPUT not set — likely running outside GitHub Actions."
    | path -> File.AppendAllText(path, $"{name}={value}\n")

let setShouldSkip () = setGitHubOutput "should_skip" "true"

let setVersion (version: string) = setGitHubOutput "version" version
