module Main.LoadedDirectoryWatcher

open Fable.Core
open Main.Bindings

type internal LoadedDirectoryWatcherState = {
    LifecycleActive: bool
    MutationActive: bool
    Generation: int
    ActiveGeneration: int option
    EstablishmentCancellation: (int * (unit -> unit)) option
    Watcher: Chokidar.IWatcher option
    LoadedDirectories: Set<string>
    PendingHandoffs: Set<string>
    MutationQueueTail: JS.Promise<unit>
    WatcherTransitionTail: JS.Promise<unit>
    Refreshes: Map<string, bool>
}

[<RequireQualifiedAccess>]
module private LoadedDirectoryWatcherState =

    let initial = {
        LifecycleActive = true
        MutationActive = false
        Generation = 0
        ActiveGeneration = None
        EstablishmentCancellation = None
        Watcher = None
        LoadedDirectories = Set.empty
        PendingHandoffs = Set.empty
        MutationQueueTail = JS.Constructors.Promise.resolve ()
        WatcherTransitionTail = JS.Constructors.Promise.resolve ()
        Refreshes = Map.empty
    }

/// Owns the single changing value for the loaded-directory watcher subsystem.
/// ArcVault keeps an immutable reference to this controller; every change replaces the immutable snapshot.
type internal LoadedDirectoryWatcherController() =
    let state = ref LoadedDirectoryWatcherState.initial

    member _.Current = state.Value

    member private _.Update transition = state.Value <- transition state.Value

    member this.BeginEstablishment() =
        state.Value.EstablishmentCancellation
        |> Option.iter (fun (_, cancel) -> cancel ())

        let cancel = ref ignore

        let cancellation =
            JS.Constructors.Promise.Create(fun resolve _ -> cancel.Value <- fun () -> resolve ())

        let generation = state.Value.Generation + 1

        state.Value <- {
            state.Value with
                Generation = generation
                EstablishmentCancellation = Some(generation, cancel.Value)
        }

        generation, cancellation

    member this.CompleteEstablishment generation =
        this.Update(fun current ->
            match current.EstablishmentCancellation with
            | Some(currentGeneration, _) when currentGeneration = generation -> {
                current with
                    EstablishmentCancellation = None
              }
            | _ -> current
        )

    member this.ActivateGeneration(generation, watcher) =
        let previous = state.Value.Watcher

        this.Update(fun current -> {
            current with
                ActiveGeneration = Some generation
                Watcher = Some watcher
        })

        previous

    member this.RetireWatcher() =
        let previous = state.Value.Watcher

        this.Update(fun current -> {
            current with
                ActiveGeneration = None
                Watcher = None
        })

        previous

    member this.BeginMutation() =
        this.Update(fun current -> { current with MutationActive = true })

    member this.EndMutation() =
        this.Update(fun current -> { current with MutationActive = false })

    member this.ActivateLifecycle() =
        this.Update(fun current -> { current with LifecycleActive = true })

    member this.StopLifecycle() =
        this.Update(fun current -> {
            current with
                LifecycleActive = false
                LoadedDirectories = Set.empty
                PendingHandoffs = Set.empty
                Refreshes = Map.empty
        })

    member this.ClearLifecycleData() =
        this.Update(fun current -> {
            current with
                LoadedDirectories = Set.empty
                PendingHandoffs = Set.empty
                Refreshes = Map.empty
        })

    member this.ClearLoadedDirectories() =
        this.Update(fun current -> {
            current with
                LoadedDirectories = Set.empty
        })

    member this.ClearPendingHandoffs() =
        this.Update(fun current -> {
            current with
                PendingHandoffs = Set.empty
        })

    member this.RemoveLoadedDirectory path =
        this.Update(fun current -> {
            current with
                LoadedDirectories = current.LoadedDirectories.Remove path
                PendingHandoffs = current.PendingHandoffs.Remove path
        })

    member this.RemovePendingHandoff path =
        this.Update(fun current -> {
            current with
                PendingHandoffs = current.PendingHandoffs.Remove path
        })

    member this.AddLoadedDirectory(path, pendingHandoff) =
        this.Update(fun current ->
            let next = {
                current with
                    LoadedDirectories = current.LoadedDirectories.Add path
            }

            if pendingHandoff then
                {
                    next with
                        PendingHandoffs = next.PendingHandoffs.Add path
                }
            else
                next
        )

    member this.AddLoadedDirectoriesToPendingHandoffs() =
        this.Update(fun current -> {
            current with
                PendingHandoffs = Set.union current.PendingHandoffs current.LoadedDirectories
        })

    member this.SetMutationQueueTail promise =
        this.Update(fun current -> {
            current with
                MutationQueueTail = promise
        })

    member this.SetLifecycleTail promise =
        this.Update(fun current -> {
            current with
                WatcherTransitionTail = promise
        })

    member this.UpdateRefreshes update =
        this.Update(fun current -> {
            current with
                Refreshes = update current.Refreshes
        })
