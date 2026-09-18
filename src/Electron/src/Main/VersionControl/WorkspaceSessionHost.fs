/// Keeps one open library session per vault root and tracks running operations so the
/// renderer can cancel them. Resolution goes through the persisted binding first and
/// adopts an unbound workspace that exactly one provider owns.
module Main.VersionControl.WorkspaceSessionHost

open System
open System.Collections.Generic
open Fable.Core
open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions

type HostedSession = {
    SessionId: string
    Binding: WorkspaceBinding
    Factory: ProviderFactory
    Session: WorkspaceSession
}

/// A registered operation: its context for the library call and the completion
/// callback that removes it from the registry.
type TrackedOperation = {
    Key: OperationKeyDto
    Context: OperationContext
    Complete: unit -> unit
}

type private RunningOperation = {
    SessionId: string
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

    let openBinding (factory: ProviderFactory) (binding: WorkspaceBinding) (context: OperationContext) = async {
        let! opened = factory.Open binding context

        let host (outcome: OperationOutcome<WorkspaceSession>) =
            let hosted = {
                SessionId = Guid.NewGuid().ToString()
                Binding = binding
                Factory = factory
                Session = outcome.Value
            }

            sessions[hosted.SessionId] <- hosted
            withValue hosted outcome

        match opened with
        | Succeeded outcome -> return Succeeded(host outcome)
        | PartiallySucceeded(outcome, failure) -> return PartiallySucceeded(host outcome, failure)
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
                | Error message -> return Failed(OperationFailure.create ProviderError "binding_not_persisted" message)
            | Failed failure -> return Failed failure
        | ProviderComposition.AmbiguousVault candidates -> return Failed(ambiguousFailure candidates)
        | ProviderComposition.UnmanagedVault diagnostics -> return Failed(unmanagedFailure workspaceRoot diagnostics)
    }

    member _.Runtime = runtime

    member _.TryGetSession(workspaceRoot: string) : HostedSession option = tryFindSession workspaceRoot

    member _.TryGetSessionById(sessionId: string) : HostedSession option =
        match sessions.TryGetValue sessionId with
        | true, hosted -> Some hosted
        | _ -> None

    /// Returns the open session for the root, or resolves the root, adopts it when
    /// exactly one provider owns it, persists the binding and opens the session.
    /// Concurrent callers for one root share a single open, so a vault never ends up
    /// with two sessions.
    member _.OpenSession(workspaceRoot: string, context: OperationContext) : Async<OperationResult<HostedSession>> = async {
        match tryFindSession workspaceRoot with
        | Some hosted -> return OperationResult.succeeded hosted
        | None ->
            let key = normalizeRoot workspaceRoot

            let pending =
                match pendingOpens.TryGetValue key with
                | true, inFlight -> inFlight
                | _ ->
                    let inFlight = promise {
                        try
                            return! resolveAndOpen workspaceRoot context |> Async.StartAsPromise
                        finally
                            pendingOpens.Remove key |> ignore
                    }

                    pendingOpens[key] <- inFlight
                    inFlight

            return! Async.AwaitPromise pending
    }

    member _.CloseSession(workspaceRoot: string) : Async<unit> = async {
        let matching =
            sessions.Values
            |> Seq.filter (fun hosted -> sameRoot hosted.Binding.WorkspaceRoot workspaceRoot)
            |> Seq.toArray

        for hosted in matching do
            sessions.Remove hosted.SessionId |> ignore

            try
                do! hosted.Session.Close()
            with _ ->
                ()
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
    /// the first progress event still lands. Operations without a session (clone,
    /// initialize, or a session still opening) register with an empty session id.
    member _.BeginOperation
        (sessionId: string, operationId: string, reportProgress: VersionControlProgressDto -> unit)
        : TrackedOperation =
        let source = OperationCancellation.Source()

        operations[operationId] <- {
            SessionId = sessionId
            Source = source
        }

        let currentSessionId () =
            match operations.TryGetValue operationId with
            | true, running -> running.SessionId
            | _ -> sessionId

        {
            Key = {
                SessionId = sessionId
                OperationId = operationId
            }
            Context =
                OperationContext.create
                    operationId
                    source.Cancellation
                    (fun progress -> reportProgress (Mappings.progress (currentSessionId ()) operationId progress))
            Complete = fun () -> operations.Remove operationId |> ignore
        }

    /// Attaches a session to an operation that was registered before its session was
    /// known. Progress and cancellation keys reported afterwards carry the session id.
    member _.AssignSession(operationId: string, sessionId: string) =
        match operations.TryGetValue operationId with
        | true, running -> operations[operationId] <- { running with SessionId = sessionId }
        | _ -> ()

    /// Cancels a tracked operation. An empty session id on either side matches: the
    /// renderer may not know the session yet, and an operation may not have one.
    member _.Cancel(sessionId: string, operationId: string) : bool =
        match operations.TryGetValue operationId with
        | true, running when
            String.IsNullOrEmpty sessionId
            || String.IsNullOrEmpty running.SessionId
            || running.SessionId = sessionId
            ->
            running.Source.Cancel()
            true
        | _ -> false

    /// Operation ids that may be running under one session. An operation whose session
    /// is not assigned yet counts too, because it may end up in this session.
    member _.RunningOperationIds(sessionId: string) : string[] =
        operations
        |> Seq.filter (fun entry -> String.IsNullOrEmpty entry.Value.SessionId || entry.Value.SessionId = sessionId)
        |> Seq.map (fun entry -> entry.Key)
        |> Seq.toArray

    member this.IsIdle(sessionId: string) : bool =
        (this.RunningOperationIds sessionId).Length = 0

let mutable private current: WorkspaceSessionHost option = None

/// The process-wide host over the current runtime. Tests replace it with a host over
/// their own runtime.
let initialize (host: WorkspaceSessionHost) = current <- Some host

let get () : WorkspaceSessionHost =
    match current with
    | Some host -> host
    | None ->
        let host = WorkspaceSessionHost(VersionControlRuntime.get ())
        current <- Some host
        host

/// The host only when one was created. Vault lifecycle hooks use it so that closing a
/// vault never builds the production runtime as a side effect.
let tryCurrent () = current
