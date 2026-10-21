module Main.LoadedDirectoryWatcher

open Fable.Core

/// The ARC root key in the watcher map. The root is covered whenever any coverage is active.
[<Literal>]
let RootDirectory = ""

/// The observation of one covered directory: a native watcher of its direct children, which holds one
/// handle, whatever the number of entries.
type internal DirectoryObserver(stop: unit -> unit) =
    member _.Close() = stop ()

type internal LoadedDirectoryWatcherState = {
    LifecycleActive: bool
    MutationActive: bool
    /// One observer per covered directory, keyed by ARC-relative path ("" is the root).
    /// A directory keeps its observer while other directories are added or removed.
    Watchers: Map<string, DirectoryObserver>
    LoadedDirectories: Set<string>
    PendingHandoffs: Set<string>
    MutationQueueTail: JS.Promise<unit>
    Refreshes: Map<string, bool>
}

[<RequireQualifiedAccess>]
module private LoadedDirectoryWatcherState =

    let initial = {
        LifecycleActive = true
        MutationActive = false
        Watchers = Map.empty
        LoadedDirectories = Set.empty
        PendingHandoffs = Set.empty
        MutationQueueTail = JS.Constructors.Promise.resolve ()
        Refreshes = Map.empty
    }

/// Owns the single changing value for the loaded-directory watcher subsystem.
/// ArcVault keeps an immutable reference to this controller; every change replaces the immutable snapshot.
type internal LoadedDirectoryWatcherController() =
    let state = ref LoadedDirectoryWatcherState.initial

    member _.Current = state.Value

    member private _.Update transition = state.Value <- transition state.Value

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

    member this.ClearLifecycleData() =
        this.Update(fun current -> {
            current with
                LoadedDirectories = Set.empty
                PendingHandoffs = Set.empty
                Refreshes = Map.empty
        })

    member this.StopLifecycle() =
        this.ClearLifecycleData()
        this.Update(fun current -> { current with LifecycleActive = false })

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

    member this.UpdateRefreshes update =
        this.Update(fun current -> {
            current with
                Refreshes = update current.Refreshes
        })
