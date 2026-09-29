/// Version control dependencies used to build the main process session host.
module Main.VersionControl.VersionControlRuntime

open VersionControlService.Abstractions

type VersionControlRuntime = {
    Catalog: ProviderResolver.ProviderCatalog
    Bindings: WorkspaceBindingStore.IWorkspaceBindingStore
    PathCaseSensitivity: PathCaseSensitivity
}

/// Creates production dependencies after the app is ready, when the settings root exists.
/// The session host is built after the runtime, so the caller passes its window lookup.
let createProduction (windowOwnerOf: OperationContext -> string) : VersionControlRuntime =
    let sensitivity = ProviderComposition.currentPathCaseSensitivity ()

    {
        Catalog = ProviderComposition.createProductionCatalog sensitivity windowOwnerOf
        Bindings = WorkspaceBindingStore.createSettingsStore sensitivity
        PathCaseSensitivity = sensitivity
    }
