module Swate.Components.DataHubTypes

open Fable.Core
open Swate.Components.Api.GitLabApi

type ExploreTab =
    | All
    | YourRepos
    | MostStarred
    | YourOrganisations

type ExploreSortField =
    | LastUpdated
    | DateCreated
    | Name
    | Stars

module ExploreSortField =

    let toProjectSortField =
        function
        | ExploreSortField.LastUpdated -> ProjectSortField.UpdatedAt
        | ExploreSortField.DateCreated -> ProjectSortField.CreatedAt
        | ExploreSortField.Name -> ProjectSortField.Name
        | ExploreSortField.Stars -> ProjectSortField.StarCount

type ExploreLoadRequest = {
    Target: ExploreTab
    SearchTerm: string
    Page: int
    PerPage: int
    SortField: ExploreSortField
    SortDirection: SortDirection
    SelectedGroupId: int option
    IsAuthenticated: bool
    User: CurrentUserDto option
}

type ExploreLoadResult = {
    Repos: ExploreProjectDto array
    Pagination: PaginationMetadata option
    Groups: GroupDto array
    GroupsLoaded: bool
    GroupsLoadError: string option
}

module ExploreLoadResult =

    let empty = {
        Repos = [||]
        Pagination = None
        Groups = [||]
        GroupsLoaded = false
        GroupsLoadError = None
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