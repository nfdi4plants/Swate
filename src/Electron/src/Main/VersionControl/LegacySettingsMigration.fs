module Main.VersionControl.LegacySettingsMigration

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Node
open VersionControlService.Abstractions

[<Literal>]
let LegacyAutoTrackThresholdKey = "swate.lfs.autotrackthresholdmb"

[<Literal>]
let LegacyDownloadLargeFilesKey = "swate.lfs.downloadlargefiles"

[<Literal>]
let AutoTrackThresholdKey = "versioncontrolservice.lfs.autotrackthresholdmb"

[<Literal>]
let MaterializeLargeObjectsKey = "versioncontrolservice.lfs.materializelargeobjects"

type private ProcessResult = {
    ExitCode: int
    Stdout: string
    Stderr: string
    ErrorMessage: string option
}

let private processErrorText (result: ProcessResult) =
    result.ErrorMessage
    |> Option.orElseWith (fun () -> result.Stderr |> Option.ofObj)
    |> Option.defaultValue "git config failed."

let private execGitConfig (workspaceRoot: string) (arguments: string[]) : Async<Result<ProcessResult, string>> = async {
    try
        let! result =
            Fable.Core.JS.Constructors.Promise.Create(fun resolve _reject ->
                let callback (error: obj) (stdout: obj) (stderr: obj) =
                    let errorOption = error |> Option.ofObj

                    resolve {
                        ExitCode = errorOption |> Option.map execFileErrorExitCode |> Option.defaultValue 0
                        Stdout = stdout |> Option.ofObj |> Option.map string |> Option.defaultValue ""
                        Stderr = stderr |> Option.ofObj |> Option.map string |> Option.defaultValue ""
                        ErrorMessage = errorOption |> Option.map execFileErrorMessage
                    }

                try
                    execFile
                        childProcessDynamic
                        "git"
                        arguments
                        (createObj [
                            "cwd" ==> workspaceRoot
                            "encoding" ==> "utf8"
                            "shell" ==> false
                        ])
                        callback
                    |> ignore
                with error ->
                    resolve {
                        ExitCode = -1
                        Stdout = ""
                        Stderr = ""
                        ErrorMessage = Some error.Message
                    }
            )
            |> Async.AwaitPromise

        return Ok result
    with error ->
        return Error error.Message
}

let private readConfig (workspaceRoot: string) (name: string) : Async<Result<string option, string>> = async {
    let! result = execGitConfig workspaceRoot [| "config"; "--local"; "--get"; name |]

    return
        match result with
        | Error message -> Error message
        | Ok result when result.ExitCode = 0 -> Ok(Some(result.Stdout.TrimEnd([| '\r'; '\n' |])))
        | Ok result when result.ExitCode = 1 -> Ok None
        | Ok result -> Error(processErrorText result)
}

let private unsetConfig (workspaceRoot: string) (name: string) : Async<Result<unit, string>> = async {
    let! result = execGitConfig workspaceRoot [| "config"; "--local"; "--unset-all"; name |]

    return
        match result with
        | Error message -> Error message
        | Ok result when result.ExitCode = 0 || result.ExitCode = 5 -> Ok()
        | Ok result -> Error(processErrorText result)
}

let private positiveWholeNumber (value: string option) =
    match value with
    | Some raw ->
        let mutable parsed = 0

        if Int32.TryParse(raw.Trim(), &parsed) && parsed > 0 then
            Some parsed
        else
            None
    | None -> None

let private isTrue (value: string option) =
    value
    |> Option.exists (fun raw -> String.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))

let migrateIfNeeded
    (workspaceRoot: string)
    (session: WorkspaceSession)
    (context: OperationContext)
    : Async<Result<unit, string>> =
    async {
        match session.StoragePolicy with
        | None -> return Ok()
        | Some storagePolicy ->
            let! legacyThreshold = readConfig workspaceRoot LegacyAutoTrackThresholdKey
            let! legacyMaterialization = readConfig workspaceRoot LegacyDownloadLargeFilesKey

            match legacyThreshold, legacyMaterialization with
            | Ok None, Ok None -> return Ok()
            | Error message, _
            | _, Error message -> return Error message
            | Ok threshold, Ok materialization ->
                let! libraryThreshold = readConfig workspaceRoot AutoTrackThresholdKey
                let! libraryMaterialization = readConfig workspaceRoot MaterializeLargeObjectsKey

                let! settingsMigration =
                    match libraryThreshold, libraryMaterialization with
                    | Error message, _
                    | _, Error message -> async { return Error message }
                    | Ok(Some _), _
                    | _, Ok(Some _) -> async { return Ok() }
                    | Ok None, Ok None ->
                        let settings = {
                            AutoPolicyThresholdMb = positiveWholeNumber threshold
                            MaterializeLargeObjects = isTrue materialization
                        }

                        async {
                            let! settingsResult = storagePolicy.SetSettings settings context

                            return
                                match settingsResult with
                                | Failed failure -> Error failure.Message
                                | Succeeded _
                                | PartiallySucceeded _ -> Ok()
                        }

                match settingsMigration with
                | Error message -> return Error message
                | Ok() ->
                    let! thresholdRemoval = unsetConfig workspaceRoot LegacyAutoTrackThresholdKey
                    let! materializationRemoval = unsetConfig workspaceRoot LegacyDownloadLargeFilesKey

                    match thresholdRemoval, materializationRemoval with
                    | Ok(), Ok() -> return Ok()
                    | Error message, _
                    | _, Error message -> return Error message
    }

let wrapGitFactory (factory: ProviderFactory) : ProviderFactory = {
    factory with
        Open =
            fun binding context -> async {
                let! result = factory.Open binding context

                match result with
                | Succeeded outcome
                | PartiallySucceeded(outcome, _) ->
                    let! migration = migrateIfNeeded binding.WorkspaceRoot outcome.Value context

                    match migration with
                    | Ok() -> ()
                    | Error message -> Browser.Dom.console.error ("Legacy Git settings migration failed.", message)
                | Failed _ -> ()

                return result
            }
}
