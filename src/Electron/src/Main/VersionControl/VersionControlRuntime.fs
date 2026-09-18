/// Process-wide version control state of the main process: the provider catalog and
/// the binding store. main.fs initializes it once the Electron app is ready, because
/// the settings root is only available from then on.
module Main.VersionControl.VersionControlRuntime

open VersionControlService.Abstractions

type VersionControlRuntime = {
    Catalog: ProviderResolver.ProviderCatalog
    Bindings: WorkspaceBindingStore.IWorkspaceBindingStore
    PathCaseSensitivity: PathCaseSensitivity
}

let mutable private current: VersionControlRuntime option = None

/// Installs the runtime for the process. Calling it again replaces the previous one,
/// which tests use to swap in their own catalog and store.
let initialize (runtime: VersionControlRuntime) = current <- Some runtime

let createProduction () : VersionControlRuntime =
    let sensitivity = ProviderComposition.currentPathCaseSensitivity ()

    {
        Catalog = ProviderComposition.createProductionCatalog sensitivity
        Bindings = WorkspaceBindingStore.createSettingsStore sensitivity
        PathCaseSensitivity = sensitivity
    }

let tryGet () = current

/// Fails when nothing initialized the runtime, so a call before the app is ready or a
/// test that forgot to install its runtime fails loudly instead of building a
/// production runtime as a side effect.
let get () =
    match current with
    | Some runtime -> runtime
    | None -> failwith "The version control runtime has not been initialized."

let resolveVault (workspaceRoot: string) =
    let runtime = get ()
    ProviderComposition.resolveVault runtime.Catalog runtime.Bindings runtime.PathCaseSensitivity workspaceRoot
