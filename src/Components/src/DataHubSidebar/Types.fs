module Swate.Components.DataHubSidebarTypes

open Fable.Core

/// Represents a single ARC project as returned by the DataHub.
type ARCProject = {
    Id: int
    Name: string
    Description: string option
    WebUrl: string
    LastActivity: string option
}

/// Information about a successfully completed operation.
type OperationResult = {
    Message: string
    Timestamp: System.DateTime
}

/// Represents the overall connection state of the sidebar.
[<StringEnum; RequireQualifiedAccess>]
type ConnectionState =
    | Disconnected
    | Connecting
    | Connected

/// Represents in-flight operation states.
[<StringEnum; RequireQualifiedAccess>]
type OperationState =
    | Idle
    | Loading
    | Success
    | Error

/// Status of a locally changed file.
[<StringEnum; RequireQualifiedAccess>]
type ChangedFileStatus =
    | [<CompiledName("new")>] New
    | [<CompiledName("changed")>] Changed
    | [<CompiledName("deleted")>] Deleted
    | [<CompiledName("moved")>] Moved

/// A file with local changes that hasn't been saved to DataHub yet.
type ChangedFile = {
    Path: string
    Status: ChangedFileStatus
    OldPath: string option
}

/// Filter mode for the ARC Browser section.
[<StringEnum; RequireQualifiedAccess>]
type ARCBrowserMode =
    | [<CompiledName("your-arcs")>] YourARCs
    | [<CompiledName("latest")>] Latest
    | [<CompiledName("featured")>] Featured