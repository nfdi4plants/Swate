module Main.LoadedDirectoryWatcher

open Fable.Core
open Main.Bindings

/// The ARC root key in the watcher map. The root is covered whenever any coverage is active.
[<Literal>]
let RootDirectory = ""

/// The observation of one covered directory. Loaded directories use a shallow native watcher that
/// reports individual changes. The ARC root is polled instead, so Swate never holds a handle on it.
type internal DirectoryObserver =
    | NativeWatcher of Chokidar.IWatcher
    | ModificationPoller of stop: (unit -> JS.Promise<unit>)

    member this.Close() =
        match this with
        | NativeWatcher watcher -> watcher.close ()
        | ModificationPoller stop -> stop ()

type internal LoadedDirectoryWatcherState = {
    LifecycleActive: bool
    MutationActive: bool
    /// Counts coverage requests. A newer request cancels the establishment of an older one.
    Generation: int
    EstablishmentCancellation: (int * (unit -> unit)) option
    /// One observer per covered directory, keyed by ARC-relative path ("" is the root).
    /// A directory keeps its observer while other directories are added or removed.
    Watchers: Map<string, DirectoryObserver>
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
        EstablishmentCancellation = None
        Watchers = Map.empty
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

    member this.ActivateWatcher(directory, watcher) =
        this.Update(fun current -> {
            current with
                Watchers = current.Watchers.Add(directory, watcher)
        })

    /// Removes the watchers of the given directories, or of every directory, and returns them for closing.
    member this.RetireWatchers(?directories: string seq) =
        let retired =
            match directories with
            | None -> state.Value.Watchers |> Map.toList
            | Some directories ->
                directories
                |> Seq.choose (fun directory ->
                    state.Value.Watchers.TryFind directory
                    |> Option.map (fun watcher -> directory, watcher)
                )
                |> Seq.toList

        this.Update(fun current -> {
            current with
                Watchers =
                    retired
                    |> List.fold (fun watchers (directory, _) -> Map.remove directory watchers) current.Watchers
        })

        retired |> List.map snd

    /// True when the observer is still the active observer of the directory.
    member _.OwnsDirectory(directory, observer: DirectoryObserver) =
        state.Value.Watchers.TryFind directory
        |> Option.exists (fun active -> obj.ReferenceEquals(active, observer))

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

    /// Forgets a loaded directory and every loaded directory below it. The root is always loaded.
    member this.RemoveLoadedDirectory(path: string) =
        if path <> RootDirectory then
            let isRemoved directory =
                Swate.Components.Shared.PathHelpers.isSameOrDescendantPath directory path

            this.Update(fun current -> {
                current with
                    LoadedDirectories = current.LoadedDirectories |> Set.filter (isRemoved >> not)
                    PendingHandoffs = current.PendingHandoffs |> Set.filter (isRemoved >> not)
            })

    member this.AddPendingHandoff path =
        this.Update(fun current -> {
            current with
                PendingHandoffs = current.PendingHandoffs.Add path
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

    /// Every directory that loses its watcher during a mutation needs a catch-up read afterwards.
    member this.AddCoveredDirectoriesToPendingHandoffs() =
        this.Update(fun current -> {
            current with
                PendingHandoffs =
                    current.Watchers
                    |> Map.fold (fun pending directory _ -> Set.add directory pending) current.PendingHandoffs
                    |> Set.union current.LoadedDirectories
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
