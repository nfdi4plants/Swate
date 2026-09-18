/// The one place in Swate that knows which version control providers exist. It builds
/// the Git factory over the DataHub account store and the lakeFS factory over an
/// application-owned state root, and it resolves the factory for a vault from the
/// persisted binding. Everything above this module works with the neutral SPI only.
module Main.VersionControl.ProviderComposition

open Main.Bindings.Path
open VersionControlService.Abstractions

module GitWorkspaceSession = VersionControlService.Git.GitWorkspaceSession
module LakeFsCredentials = VersionControlService.LakeFs.LakeFsCredentials
module LakeFsProviderOptions = VersionControlService.LakeFs.LakeFsProviderOptions
module LakeFsWorkspaceSession = VersionControlService.LakeFs.LakeFsWorkspaceSession

/// Folder below the application settings root that holds provider state which must
/// never live inside a vault (lakeFS indexes and transactions).
[<Literal>]
let providerStateFolderName = "VersionControlState"

/// Windows and macOS compare paths without regard to case, Linux does not.
let pathCaseSensitivityForPlatform (platform: string) : PathCaseSensitivity =
    match platform with
    | "win32"
    | "darwin" -> CaseInsensitive
    | _ -> CaseSensitive

let currentPathCaseSensitivity () =
    pathCaseSensitivityForPlatform (Main.Bindings.Node.processPlatform ())

let dataHubAccountSource: DataHubStrategies.DataHubAccountSource = {
    GetState = Main.Auth.AuthService.peekState
    TryGetTokenForAccount = Main.Auth.AuthService.tryGetTokenForAccount
    TryGetTokenForHost = Main.Auth.AuthService.tryGetTokenForHost
}

let createGitFactory (source: DataHubStrategies.DataHubAccountSource) : ProviderFactory =
    GitWorkspaceSession.createFactoryWithCredentialsAndIdentity
        GitWorkspaceSession.GitSessionHooks.none
        (DataHubStrategies.createCredentialStrategy source)
        (DataHubStrategies.createIdentityStrategy source)

let lakeFsOptions
    (settingsRoot: string)
    (sensitivity: PathCaseSensitivity)
    : LakeFsProviderOptions.LakeFsProviderOptions =
    {
        StateRoot = join [| settingsRoot; providerStateFolderName |]
        PathCaseSensitivity = sensitivity
    }

let createLakeFsFactory
    (options: LakeFsProviderOptions.LakeFsProviderOptions)
    (credentials: LakeFsCredentials.LakeFsCredentialStrategy)
    : ProviderFactory =
    LakeFsWorkspaceSession.createFactory options credentials

let createCatalog (factories: ProviderFactory list) : ProviderResolver.ProviderCatalog =
    match ProviderResolver.tryCreateCatalog factories with
    | Ok catalog -> catalog
    | Error message -> failwith message

/// The production catalog: Git over the DataHub accounts, lakeFS without any
/// configured connection until lakeFS accounts exist in Swate. The caller passes the
/// path case sensitivity it also hands to the resolver and the binding store.
let createProductionCatalog (sensitivity: PathCaseSensitivity) : ProviderResolver.ProviderCatalog =
    let settingsRoot = Main.SettingsStore.getSettingsRootPath ()

    createCatalog [
        createGitFactory dataHubAccountSource
        createLakeFsFactory (lakeFsOptions settingsRoot sensitivity) LakeFsCredentials.unconfigured
    ]

let gitProviderId =
    match ProviderId.tryCreate WellKnownProviderIds.Git with
    | Ok providerId -> providerId
    | Error message -> failwith message

let lakeFsProviderId =
    match ProviderId.tryCreate WellKnownProviderIds.LakeFs with
    | Ok providerId -> providerId
    | Error message -> failwith message

/// The provider new vaults are initialized with. Swate only provisions Git today.
let defaultProviderId = gitProviderId

/// Turns the location text the renderer sends into a repository location for the
/// provider that understands it. Git takes https and ssh URLs, lakeFS takes its own
/// scheme. Anything else is refused here, before any provider runs.
let tryCreateLocation (providerLocation: string) (displayName: string option) : Result<RepositoryLocation, string> =
    let trimmed = (providerLocation |> Option.ofObj |> Option.defaultValue "").Trim()

    let location providerId = {
        ProviderId = providerId
        DisplayName = displayName
        ProviderLocation = trimmed
        ConnectionProfileId = None
    }

    if trimmed.Length = 0 then
        Error "The repository location is empty."
    elif
        trimmed.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase)
        || trimmed.StartsWith("ssh://", System.StringComparison.OrdinalIgnoreCase)
    then
        Ok(location gitProviderId)
    elif trimmed.StartsWith("lakefs://", System.StringComparison.OrdinalIgnoreCase) then
        Ok(location lakeFsProviderId)
    else
        Error "The repository location must be an https://, ssh:// or lakefs:// address."

/// Lock files a killed provider process can leave behind in a workspace. The library
/// reports such a lock with the recovery code remove_index_lock and refuses to delete
/// it because it cannot prove who owns the lock. Swate can: it is the only process
/// that runs git in a vault window, so once its own operations are idle the lock is
/// stale. Only a plain repository (a .git directory) is handled here. A .git file
/// (linked worktree or submodule) keeps its git directory elsewhere, and for those the
/// lock is left to the user together with the library's instructions. This reads the
/// filesystem once to tell the two cases apart.
let staleLockPaths (providerId: ProviderId) (workspaceRoot: string) : string[] =
    let gitDirectory = join [| workspaceRoot; ".git" |]

    let isPlainGitDirectory () =
        VersionControlService.Runtime.Node.FileSystem.tryLstatSync gitDirectory
        |> Option.exists (fun stats -> stats.isDirectory ())

    if providerId = gitProviderId && isPlainGitDirectory () then
        [| join [| gitDirectory; "index.lock" |] |]
    else
        [||]

/// Resolution outcome for one vault root, as the session host consumes it.
type VaultResolution =
    /// A persisted binding selected its factory.
    | BoundVault of WorkspaceBinding * ProviderFactory
    /// No binding is stored, but exactly one provider owns the root and can adopt it.
    | AdoptableVault of ProviderFactory * ProviderResolver.DetectionCandidate
    /// Several providers claim the root. The host has to bind explicitly.
    | AmbiguousVault of ProviderResolver.DetectionCandidate[]
    /// No provider owns the root.
    | UnmanagedVault of diagnostics: OperationFailure[]

let resolveVault
    (catalog: ProviderResolver.ProviderCatalog)
    (bindings: WorkspaceBindingStore.IWorkspaceBindingStore)
    (sensitivity: PathCaseSensitivity)
    (workspaceRoot: string)
    : Async<VaultResolution> =
    async {
        let! report =
            ProviderResolver.resolve catalog {
                WorkspacePath = workspaceRoot
                ExplicitBinding = bindings.TryFind workspaceRoot
                PathCaseSensitivity = sensitivity
            }

        return
            match report.Resolution with
            | ProviderResolver.Bound(binding, factory) -> BoundVault(binding, factory)
            | ProviderResolver.ProbedWorkspace(factory, candidate) -> AdoptableVault(factory, candidate)
            | ProviderResolver.AmbiguousWorkspace candidates -> AmbiguousVault candidates
            | ProviderResolver.UnmanagedWorkspace -> UnmanagedVault report.Diagnostics
    }
