/// Keeps one open library session per vault root and tracks running operations so the
/// renderer can cancel them. Resolution goes through the persisted binding first and
/// adopts an unbound workspace that exactly one provider owns.
module Main.VersionControl.WorkspaceSessionHost

open System
open System.Collections.Generic
open Fable.Core
open Main.VersionControl.VersionControlSettings
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions

type HostedSession = {
    SessionId: string
    Binding: WorkspaceBinding
    Session: WorkspaceSession
    Settings: VersionControlSettings
}

/// A registered operation: its context for the library call and the completion
/// callback that removes it from the registry.
type TrackedOperation = {
    Key: OperationRequestDto
    Context: OperationContext
    Complete: unit -> unit
}

type private RunningOperation = {
    WorkspaceRoot: string option
    Source: OperationCancellation.Source
}

let private ambiguousFailure (candidates: ProviderResolver.DetectionCandidate[]) =
    let providers =
        candidates
        |> Array.map (fun candidate -> ProviderId.value candidate.ProviderId)
        |> String.concat ", "

    OperationFailure.create
        Validation
        VersionControlCodes.WorkspaceAmbiguous
        $"Several providers claim this workspace ({providers}). Bind it to one explicitly."

let private unmanagedFailure (workspaceRoot: string) (diagnostics: OperationFailure[]) = {
    OperationFailure.create
        NotFound
        VersionControlCodes.WorkspaceUnmanaged
        "This folder is not a version controlled workspace." with
        Details =
            diagnostics
            |> Array.map (fun diagnostic -> $"{diagnostic.Code}: {diagnostic.Message}")
        AffectedPaths = [| workspaceRoot |]
}

let private withValue (value: 'U) (outcome: OperationOutcome<'T>) : OperationOutcome<'U> = {
    Value = value
    Effect = outcome.Effect
    Warnings = outcome.Warnings
    AffectedPaths = outcome.AffectedPaths
    ResultingRevision = outcome.ResultingRevision
    ResultingWorkspaceVersion = outcome.ResultingWorkspaceVersion
    Publication = outcome.Publication
}

type WorkspaceSessionHost(runtime: VersionControlRuntime.VersionControlRuntime) =

    let sessions = Dictionary<string, HostedSession>()
    let operations = Dictionary<string, RunningOperation>()
    let pendingOpens = Dictionary<string, JS.Promise<OperationResult<HostedSession>>>()
    let closeRequested = HashSet<string>()

    let normalizeRoot (workspaceRoot: string) =
        let normalized = ProviderResolver.normalizePath workspaceRoot

        match runtime.PathCaseSensitivity with
        | CaseSensitive -> normalized
        | CaseInsensitive -> normalized.ToLowerInvariant()

    let sameRoot (left: string) (right: string) =
        WorkspaceBindingStore.rootsEqual runtime.PathCaseSensitivity left right

    let tryFindSession (workspaceRoot: string) =
        sessions.Values
        |> Seq.tryFind (fun hosted -> sameRoot hosted.Binding.WorkspaceRoot workspaceRoot)

    let closeHostedSession (hosted: HostedSession) = async {
        sessions.Remove hosted.SessionId |> ignore

        try
            do! hosted.Session.Close()
        with _ ->
            ()
    }

    /// Keeps a settings value only while that same session is still open. A close that
    /// lands while the provider call runs must not be undone by its continuation.
    let storeSettingsIfLive (hosted: HostedSession) (settings: VersionControlSettings) =
        if sessions.ContainsKey hosted.SessionId then
            sessions[hosted.SessionId] <- { hosted with Settings = settings }

    let openBinding (factory: ProviderFactory) (binding: WorkspaceBinding) (context: OperationContext) = async {
        let! opened = factory.Open binding context

        let initializeSettings (outcome: OperationOutcome<WorkspaceSession>) = async {
            let hosted = {
                SessionId = Guid.NewGuid().ToString()
                Binding = binding
                Session = outcome.Value
                Settings = VersionControlSettings.defaults
            }

            // The app owns these values. Every session starts from the defaults.
            // Provider repository keys only mirror them.
            match hosted.Session.StoragePolicy with
            | Some policy ->
                // A provider that throws here must not lose the session it just opened,
                // so the exception becomes an ordinary failure of the settings push.
                let! applied = async {
                    try
                        return!
                            policy.SetSettings (VersionControlSettings.toStoragePolicySettings hosted.Settings) context
                    with error ->
                        return
                            Failed(
                                OperationFailure.createRedacted
                                    ProviderError
                                    VersionControlCodes.UnexpectedException
                                    error.Message
                            )
                }

                let settingsFailure =
                    match applied with
                    | Failed failure
                    | PartiallySucceeded(_, failure) ->
                        Browser.Dom.console.error (
                            $"Could not apply default version-control settings ({failure.Code}): {failure.Message}"
                        )

                        Some failure
                    | Succeeded _ -> None

                return withValue hosted outcome, settingsFailure
            | None -> return withValue hosted outcome, None
        }

        match opened with
        | Succeeded outcome ->
            let! (initialized, settingsFailure) = initializeSettings outcome

            match settingsFailure with
            | Some failure -> return PartiallySucceeded(initialized, failure)
            | None -> return Succeeded initialized
        | PartiallySucceeded(outcome, failure) ->
            let! (initialized, settingsFailure) = initializeSettings outcome

            match settingsFailure with
            | Some settingsFailure ->
                return
                    PartiallySucceeded(
                        initialized,
                        {
                            failure with
                                Details =
                                    Array.append failure.Details [|
                                        $"{settingsFailure.Code}: {settingsFailure.Message}"
                                    |]
                        }
                    )
            | None -> return PartiallySucceeded(initialized, failure)
        | Failed failure -> return Failed failure
    }

    let resolveAndOpen (workspaceRoot: string) (context: OperationContext) = async {
        let! resolution =
            ProviderComposition.resolveVault runtime.Catalog runtime.Bindings runtime.PathCaseSensitivity workspaceRoot

        match resolution with
        | ProviderComposition.BoundVault(binding, factory) -> return! openBinding factory binding context
        | ProviderComposition.AdoptableVault(factory, candidate) ->
            let! adopted =
                factory.Adopt
                    {
                        WorkspaceRoot = candidate.Root
                        ConnectionProfileId = None
                    }
                    context

            match adopted with
            | Succeeded outcome
            | PartiallySucceeded(outcome, _) ->
                match runtime.Bindings.Save outcome.Value with
                | Ok() -> return! openBinding factory outcome.Value context
                | Error message ->
                    return Failed(OperationFailure.create ProviderError VersionControlCodes.BindingNotPersisted message)
            | Failed failure -> return Failed failure
        | ProviderComposition.AmbiguousVault candidates -> return Failed(ambiguousFailure candidates)
        | ProviderComposition.UnmanagedVault diagnostics -> return Failed(unmanagedFailure workspaceRoot diagnostics)
    }

    member _.Runtime = runtime

    member _.TryGetSession(workspaceRoot: string) : HostedSession option = tryFindSession workspaceRoot

    member _.GetSettings(workspaceRoot: string) : VersionControlSettings =
        tryFindSession workspaceRoot
        |> Option.map (fun hosted -> hosted.Settings)
        |> Option.defaultValue VersionControlSettings.defaults

    member _.SetSettings
        (workspaceRoot: string, settings: VersionControlSettings, context: OperationContext)
        : Async<OperationResult<unit>> =
        async {
            match VersionControlSettings.validate settings with
            | Error reason ->
                return Failed(OperationFailure.create Validation VersionControlCodes.InvalidLfsThreshold reason)
            | Ok settings ->
                match tryFindSession workspaceRoot with
                | None ->
                    return
                        Failed(
                            OperationFailure.create
                                NotFound
                                VersionControlCodes.SessionUnavailable
                                "The workspace session is not open."
                        )
                | Some hosted ->
                    match hosted.Session.StoragePolicy with
                    | Some policy ->
                        let! result =
                            policy.SetSettings (VersionControlSettings.toStoragePolicySettings settings) context

                        match result with
                        | Failed _ -> return result
                        | Succeeded outcome ->
                            storeSettingsIfLive hosted settings
                            return Succeeded(withValue () outcome)
                        | PartiallySucceeded(outcome, failure) ->
                            storeSettingsIfLive hosted settings
                            return PartiallySucceeded(withValue () outcome, failure)
                    | None ->
                        storeSettingsIfLive hosted settings
                        return OperationResult.succeeded ()
        }

    /// Returns the open session for the root, or resolves the root, adopts it when
    /// exactly one provider owns it, persists the binding and opens the session.
    /// Concurrent callers for one root share a single open, so a vault never ends up
    /// with two sessions. The shared open runs without cancellation, because a cancel
    /// of one caller must not fail the others, and it reports progress to the first
    /// caller only. Opening is short (a probe and a session construction), so both
    /// limits are acceptable.
    member _.OpenSession(workspaceRoot: string, context: OperationContext) : Async<OperationResult<HostedSession>> = async {
        match tryFindSession workspaceRoot with
        | Some hosted -> return OperationResult.succeeded hosted
        | None ->
            let key = normalizeRoot workspaceRoot

            let pending =
                match pendingOpens.TryGetValue key with
                | true, inFlight -> inFlight
                | _ ->
                    let openContext =
                        OperationContext.create context.OperationId OperationCancellation.none context.ReportProgress

                    let inFlight = promise {
                        try
                            let! opened = resolveAndOpen workspaceRoot openContext |> Async.StartAsPromise
                            let shouldClose = closeRequested.Remove key

                            match opened with
                            | Succeeded outcome ->
                                if shouldClose then
                                    do! closeHostedSession outcome.Value |> Async.StartAsPromise

                                    return
                                        Failed(
                                            OperationFailure.create
                                                NotFound
                                                VersionControlCodes.SessionUnavailable
                                                "The workspace session is not open."
                                        )
                                else
                                    sessions[outcome.Value.SessionId] <- outcome.Value
                                    return opened
                            | PartiallySucceeded(outcome, _) ->
                                if shouldClose then
                                    do! closeHostedSession outcome.Value |> Async.StartAsPromise

                                    return
                                        Failed(
                                            OperationFailure.create
                                                NotFound
                                                VersionControlCodes.SessionUnavailable
                                                "The workspace session is not open."
                                        )
                                else
                                    sessions[outcome.Value.SessionId] <- outcome.Value
                                    return opened
                            | Failed _ -> return opened
                        finally
                            pendingOpens.Remove key |> ignore
                            closeRequested.Remove key |> ignore
                    }

                    pendingOpens[key] <- inFlight
                    inFlight

            return! Async.AwaitPromise pending
    }

    member _.CloseSession(workspaceRoot: string) : Async<unit> = async {
        let key = normalizeRoot workspaceRoot

        if pendingOpens.ContainsKey key then
            closeRequested.Add key |> ignore

        let matching =
            sessions.Values
            |> Seq.filter (fun hosted -> sameRoot hosted.Binding.WorkspaceRoot workspaceRoot)
            |> Seq.toArray

        for hosted in matching do
            do! closeHostedSession hosted
    }

    /// Closes and reopens the session over the binding stored for the root. Used after
    /// a successful Bind, because an open session keeps the location captured at open.
    member this.ReopenSession(workspaceRoot: string, context: OperationContext) = async {
        do! this.CloseSession workspaceRoot
        return! this.OpenSession(workspaceRoot, context)
    }

    /// A vault folder was renamed on disk. The open session (if any) is closed and the
    /// persisted binding follows the new root verbatim, so the next open resolves it
    /// without probing again. The new entry is written before the old one is removed,
    /// so a failure in between never loses the binding.
    member this.WorkspaceRenamed(previousRoot: string, newRoot: string) : Async<Result<unit, string>> = async {
        do! this.CloseSession previousRoot

        match runtime.Bindings.TryFind previousRoot with
        | None -> return Ok()
        | Some binding ->
            match runtime.Bindings.Save { binding with WorkspaceRoot = newRoot } with
            | Error message -> return Error message
            | Ok() -> return runtime.Bindings.Remove previousRoot
    }

    member _.CloseAll() : Async<unit> = async {
        let open' = sessions.Values |> Seq.toArray
        sessions.Clear()

        for hosted in open' do
            try
                do! hosted.Session.Close()
            with _ ->
                ()
    }

    /// Registers an operation before any library call so a cancel that arrives before
    /// the first progress event still lands. Each operation retains its workspace root
    /// so lock cleanup can scope it correctly.
    member _.BeginOperation
        (operationId: string, workspaceRoot: string option, reportProgress: OperationProgress -> unit)
        : TrackedOperation =
        let source = OperationCancellation.Source()

        operations[operationId] <- {
            WorkspaceRoot = workspaceRoot
            Source = source
        }

        {
            Key = { OperationId = operationId }
            Context = OperationContext.create operationId source.Cancellation reportProgress
            Complete = fun () -> operations.Remove operationId |> ignore
        }

    /// Cancels a tracked operation by its operation id.
    member _.Cancel(operationId: string) : bool =
        match operations.TryGetValue operationId with
        | true, running ->
            running.Source.Cancel()
            true
        | _ -> false

    /// Returns operations registered for the queried workspace root.
    member _.RunningOperationIds(workspaceRoot: string) : string[] =
        operations
        |> Seq.filter (fun entry -> entry.Value.WorkspaceRoot |> Option.exists (sameRoot workspaceRoot))
        |> Seq.map (fun entry -> entry.Key)
        |> Seq.toArray

    member this.IsIdle(workspaceRoot: string) : bool =
        (this.RunningOperationIds workspaceRoot).Length = 0

let mutable private current: WorkspaceSessionHost option = None

/// Installs the process-wide host. Tests replace it with a host over their own runtime.
let initialize (host: WorkspaceSessionHost) = current <- Some host

/// Clears the process-wide host between tests.
let resetForTests () = current <- None

let get () : WorkspaceSessionHost =
    match current with
    | Some host -> host
    | None -> failwith "The workspace session host has not been initialized."

/// Returns the host only when one was installed. Vault lifecycle hooks use it so they
/// can tolerate failed initialization without building a host.
let tryCurrent () = current
