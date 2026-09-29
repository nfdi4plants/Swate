/// Remembers which window opened each text diff handle. A window reaches only its own
/// handles, and the handles of a window that closes or reloads are closed in the library,
/// because the renderer that knew them is gone.
module Main.VersionControl.TextDiffHandles

open System
open System.Collections.Generic
open Fable.Core
open Fable.Electron.Main
open VersionControlService.Abstractions

type private OpenHandle = {
    Handle: DiffHandle
    WindowId: int option
    WorkspaceRoot: string
}

let private handles = Dictionary<string, OpenHandle>()

// Counts the reloads of each live window. An open that started before a reload sees a
// different count when it finishes. The entry goes away with its window.
let private reloads = Dictionary<int, int>()

/// Whether the window still exists. Tests replace it, because they have no Electron windows.
let mutable isWindowAlive: int -> bool =
    fun windowId ->
        match BrowserWindow.fromId windowId with
        | Some window -> not (window.isDestroyed ())
        | None -> false

/// The reload count of a window, taken before an open so the open can tell whether the
/// renderer that asked for it is still there when the handle arrives.
let reloadCount (windowId: int option) : int =
    match windowId with
    | Some id ->
        match reloads.TryGetValue id with
        | true, count -> count
        | _ -> 0
    | None -> 0

let isRecorded (handleId: string) : bool = handles.ContainsKey handleId

let remove (handleId: string) = handles.Remove handleId |> ignore

/// Whether another window opened the handle. A handle this registry does not know
/// passes, and the library answers for it.
let belongsToOtherWindow (windowId: int option) (handleId: string) : bool =
    match handles.TryGetValue handleId with
    | true, entry -> entry.WindowId <> windowId
    | _ -> false

/// Answers a handle another window opened like a closed one, so a renderer cannot tell it
/// apart from a handle that never existed.
let withOwnedHandle
    (windowId: int option)
    (handle: DiffHandle)
    (call: unit -> Async<OperationResult<'T>>)
    : Async<OperationResult<'T>> =
    if belongsToOtherWindow windowId handle.Id then
        async {
            return
                Failed(
                    OperationFailure.create Validation TextDiffFailureCodes.SessionClosed "The diff session is closed."
                )
        }
    else
        call ()

let private closeInBackground
    (host: WorkspaceSessionHost.WorkspaceSessionHost)
    (service: TextDiffService)
    (windowId: int)
    (workspaceRoot: string)
    (handle: DiffHandle)
    =
    // The library binds a handle to the window of the operation that opened it, so the
    // close runs as an operation of that window. The registration happens before the
    // first await, which keeps the session open until the close is done.
    let tracked =
        host.BeginOperation(Guid.NewGuid().ToString(), Some workspaceRoot, Some windowId, false, ignore)

    promise {
        try
            try
                let! _ = service.Close handle tracked.Context |> Async.StartAsPromise
                ()
            with error ->
                Browser.Dom.console.error ("A text diff handle could not be closed.", error.Message)
        finally
            tracked.Complete()
    }
    |> Promise.start

/// Records the handle of a finished open. When the window closed or reloaded while the
/// open ran, nobody can use the handle any more, so it is closed right away instead.
let recordOrClose
    (windowId: int option)
    (reloadCountAtStart: int)
    (workspaceRoot: string)
    (service: TextDiffService)
    (handle: DiffHandle)
    =
    match windowId with
    | Some id when not (isWindowAlive id) || reloadCount windowId <> reloadCountAtStart ->
        match WorkspaceSessionHost.tryCurrent () with
        | Some host -> closeInBackground host service id workspaceRoot handle
        | None -> ()
    | _ ->
        handles[handle.Id] <- {
            Handle = handle
            WindowId = windowId
            WorkspaceRoot = workspaceRoot
        }

let private closeHandlesOf (windowId: int) =
    let owned =
        handles.Values
        |> Seq.filter (fun entry -> entry.WindowId = Some windowId)
        |> Seq.toArray

    for entry in owned do
        handles.Remove entry.Handle.Id |> ignore

    match WorkspaceSessionHost.tryCurrent () with
    | Some host ->
        for entry in owned do
            match host.TryGetSession entry.WorkspaceRoot |> Option.bind _.Session.TextDiff with
            | Some service -> closeInBackground host service windowId entry.WorkspaceRoot entry.Handle
            | None -> ()
    | None -> ()

/// Closes the handles of a reloaded window. Opens that started before the reload close
/// their handle when they finish. Closing is idempotent in the library.
let windowReloaded (windowId: int) =
    reloads[windowId] <- reloadCount (Some windowId) + 1
    closeHandlesOf windowId

/// Closes the handles of a closed window in the background. Opens still running for it
/// find the window gone and close their handle when they finish.
let windowClosed (windowId: int) =
    reloads.Remove windowId |> ignore
    closeHandlesOf windowId
