/// Version control dependencies used to build the main process session host.
module Main.VersionControl.VersionControlRuntime

open VersionControlService.Abstractions

type VersionControlRuntime = {
    Catalog: ProviderResolver.ProviderCatalog
    Bindings: WorkspaceBindingStore.IWorkspaceBindingStore
    PathCaseSensitivity: PathCaseSensitivity
    /// What a new session starts from. The host calls it when it opens a session, and it also
    /// serves roots without a session (the settings of such a root and the diff storage policy
    /// fallback). An open session keeps the values it started with when the user defaults change.
    SessionDefaults: unit -> Main.UserDefaults.SessionDefaults
}

/// Creates production dependencies after the app is ready, when the settings root exists.
/// The session host is built after the runtime, so the caller passes its window lookup.
let createProduction (windowOwnerOf: OperationContext -> string) : VersionControlRuntime =
    let sensitivity = ProviderComposition.currentPathCaseSensitivity ()

    {
        Catalog = ProviderComposition.createProductionCatalog sensitivity windowOwnerOf
        Bindings = WorkspaceBindingStore.createSettingsStore sensitivity
        PathCaseSensitivity = sensitivity
        SessionDefaults = fun () -> Main.UserDefaults.sessionDefaultsOf Main.UserDefaults.production
    }
