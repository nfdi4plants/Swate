module ElectronCore.VersionControlCompositionTests

open Fable.Core
open Main.VersionControl
open Swate.Components.Composite.Authentication.Types
open VersionControlService.Abstractions
open VersionControlService.Git.GitCredentialStrategy
open Vitest
open ElectronCore.TestHelpers

let private account
    (localId: string)
    (hub: string)
    (username: string)
    (email: string)
    (commitEmail: string option)
    : AccountSummary =
    {
        User = {
            Id = 1
            LocalSwateAccountId = localId
            Username = username
            Name = "Display Name"
            Email = email
            CommitEmail = commitEmail
            AvatarUrl = ""
            TargetDataHub = hub
        }
        DateAdded = "2026-09-18T00:00:00Z"
        TokenStatus = TokenStatus.Ok
        TokenExpiresOn = None
    }

let private gitLab =
    account
        "acc-gitlab"
        "https://git.nfdi4plants.org"
        "carol"
        "carol@example.org"
        (Some "123-carol@noreply.example.org")

let private other =
    account "acc-other" "https://gitlab.example.com/" "other" "other@example.com" None

let private state (active: AccountSummary option) (stored: AccountSummary list) : AuthStateDto = {
    ActiveAccount = active
    StoredAccounts = List.toArray stored
}

let private source (authState: AuthStateDto) (tokens: (string * string) list) : DataHubStrategies.DataHubAccountSource = {
    GetState = fun () -> authState
    TryGetTokenForAccount = fun localId -> tokens |> List.tryFind (fun (id, _) -> id = localId) |> Option.map snd
    TryGetTokenForHost =
        fun host ->
            authState.StoredAccounts
            |> Array.tryFind (fun candidate -> DataHubStrategies.hostOfDataHub candidate.User.TargetDataHub = host)
            |> Option.bind (fun candidate ->
                tokens
                |> List.tryFind (fun (id, _) -> id = candidate.User.LocalSwateAccountId)
                |> Option.map snd
            )
}

let private identityRequest (host: string option) (profile: string option) : RevisionIdentityRequest = {
    WorkspaceRoot = "C:/arcs/demo"
    TargetHost = host
    ConnectionProfileId = profile
}

let private gitProviderId =
    ProviderId.tryCreate "git" |> Result.defaultWith failwith

let private lakeFsProviderId =
    ProviderId.tryCreate "lakefs" |> Result.defaultWith failwith

let private bindingFor (providerId: ProviderId) (root: string) : WorkspaceBinding = {
    SchemaVersion = WorkspaceBinding.CurrentSchemaVersion
    ProviderId = providerId
    WorkspaceRoot = root
    ProviderStateRef = Some "state-1"
    Location = {
        ProviderId = providerId
        DisplayName = Some "Demo"
        ProviderLocation = "https://git.nfdi4plants.org/carol/demo.git"
        ConnectionProfileId = None
    }
    ConnectionProfileId = Some "acc-gitlab"
}

let private memoryStore (sensitivity: PathCaseSensitivity) =
    let mutable content: string option = None
    WorkspaceBindingStore.create sensitivity (fun () -> content) (fun next -> content <- Some next)

Vitest.describe (
    "DataHub identity strategy",
    fun () ->
        Vitest.test (
            "the target host selects the stored account for that hub over the active one",
            fun () ->
                let identity =
                    DataHubStrategies.resolveIdentity
                        (source (state (Some other) [ gitLab; other ]) [])
                        (identityRequest (Some "git.nfdi4plants.org") None)

                Vitest
                    .expect(identity)
                    .toEqual (
                        Some {
                            Name = "carol"
                            Email = "123-carol@noreply.example.org"
                        }
                    )
        )

        Vitest.test (
            "no target host falls back to the active account",
            fun () ->
                let identity =
                    DataHubStrategies.resolveIdentity
                        (source (state (Some other) [ gitLab; other ]) [])
                        (identityRequest None None)

                Vitest
                    .expect(identity)
                    .toEqual (
                        Some {
                            Name = "other"
                            Email = "other@example.com"
                        }
                    )
        )

        Vitest.test (
            "a host without a matching account yields no identity",
            fun () ->
                let identity =
                    DataHubStrategies.resolveIdentity
                        (source (state (Some gitLab) [ gitLab ]) [])
                        (identityRequest (Some "github.com") None)

                Vitest.expect(identity).toEqual None
        )

        Vitest.test (
            "nobody signed in yields no identity",
            fun () ->
                let identity =
                    DataHubStrategies.resolveIdentity (source AuthStateDto.Empty []) (identityRequest None None)

                Vitest.expect(identity).toEqual None
        )

        Vitest.test (
            "the connection profile selects its account when the host matches and is ignored otherwise",
            fun () ->
                let authState = state (Some gitLab) [ gitLab; other ]

                let matching =
                    DataHubStrategies.resolveIdentity
                        (source authState [])
                        (identityRequest (Some "gitlab.example.com") (Some "acc-other"))

                let mismatched =
                    DataHubStrategies.resolveIdentity
                        (source authState [])
                        (identityRequest (Some "git.nfdi4plants.org") (Some "acc-other"))

                Vitest.expect(matching |> Option.map _.Name).toEqual (Some "other")
                Vitest.expect(mismatched |> Option.map _.Name).toEqual (Some "carol")
        )

        Vitest.test (
            "a throwing account source yields None instead of an exception",
            fun () ->
                let broken: DataHubStrategies.DataHubAccountSource = {
                    GetState = fun () -> failwith "store unavailable"
                    TryGetTokenForAccount = fun _ -> failwith "store unavailable"
                    TryGetTokenForHost = fun _ -> failwith "store unavailable"
                }

                Vitest.expect(DataHubStrategies.resolveIdentity broken (identityRequest None None)).toEqual None
                Vitest.expect(DataHubStrategies.resolveCredential broken "git.nfdi4plants.org" None).toEqual None
        )
)

Vitest.describe (
    "DataHub credential strategy",
    fun () ->
        Vitest.test (
            "returns the oauth2 credential for the account of the requested host",
            fun () ->
                let credential =
                    DataHubStrategies.resolveCredential
                        (source (state (Some other) [ gitLab; other ]) [
                            "acc-gitlab", "glpat-1"
                            "acc-other", "glpat-2"
                        ])
                        "GIT.nfdi4plants.org"
                        None

                Vitest
                    .expect(credential)
                    .toEqual (
                        Some {
                            Username = "oauth2"
                            Secret = "glpat-1"
                        }
                    )
        )

        Vitest.test (
            "returns None for a host without a usable token",
            fun () ->
                let credential =
                    DataHubStrategies.resolveCredential
                        (source (state (Some gitLab) [ gitLab ]) [])
                        "git.nfdi4plants.org"
                        None

                Vitest.expect(credential).toEqual None
        )

        Vitest.test (
            "the connection profile is only used for its own host",
            fun () ->
                let tokens = [ "acc-gitlab", "glpat-1"; "acc-other", "glpat-2" ]
                let authState = state (Some gitLab) [ gitLab; other ]

                let ownHost =
                    DataHubStrategies.resolveCredential
                        (source authState tokens)
                        "gitlab.example.com"
                        (Some "acc-other")

                let foreignHost =
                    DataHubStrategies.resolveCredential
                        (source authState tokens)
                        "git.nfdi4plants.org"
                        (Some "acc-other")

                Vitest.expect(ownHost |> Option.map _.Secret).toEqual (Some "glpat-2")
                Vitest.expect(foreignHost |> Option.map _.Secret).toEqual (Some "glpat-1")
        )
)

Vitest.describe (
    "Workspace binding store",
    fun () ->
        Vitest.test (
            "stores the binding verbatim and finds it under a differently cased root",
            fun () ->
                let store = memoryStore CaseInsensitive
                let binding = bindingFor gitProviderId "C:\\Users\\Carol\\ARCs\\Demo"

                match store.Save binding with
                | Ok() -> ()
                | Error message -> failwith message

                let found = store.TryFind "c:/users/carol/arcs/demo/"
                Vitest.expect(found).toEqual (Some binding)
                Vitest.expect(found |> Option.map _.WorkspaceRoot).toEqual (Some "C:\\Users\\Carol\\ARCs\\Demo")
        )

        Vitest.test (
            "case sensitive stores do not match a differently cased root",
            fun () ->
                let store = memoryStore CaseSensitive
                store.Save(bindingFor gitProviderId "/home/carol/arcs/demo") |> ignore

                Vitest.expect(store.TryFind "/home/carol/arcs/Demo").toEqual None

                Vitest
                    .expect(store.TryFind "/home/carol/arcs/demo")
                    .toEqual (Some(bindingFor gitProviderId "/home/carol/arcs/demo"))
        )

        Vitest.test (
            "saving a binding for the same root replaces the previous one and remove drops it",
            fun () ->
                let store = memoryStore CaseInsensitive
                store.Save(bindingFor gitProviderId "C:/arcs/demo") |> ignore
                store.Save(bindingFor lakeFsProviderId "c:/ARCS/demo") |> ignore

                Vitest.expect(store.List().Length).toBe 1
                Vitest.expect(store.TryFind "C:/arcs/demo" |> Option.map _.ProviderId).toEqual (Some lakeFsProviderId)

                store.Remove "C:/arcs/demo"
                Vitest.expect(store.List().Length).toBe 0
        )

        Vitest.test (
            "the serialized form round trips every field",
            fun () ->
                let binding = bindingFor lakeFsProviderId "C:/arcs/demo"

                let decoded =
                    WorkspaceBindingStore.deserialize (WorkspaceBindingStore.serialize [| binding |])

                Vitest.expect(decoded).toEqual [| binding |]
        )

        Vitest.test (
            "unreadable content yields an empty store instead of an exception",
            fun () -> Vitest.expect(WorkspaceBindingStore.deserialize "{ not json").toEqual [||]
        )

        Vitest.test (
            "a damaged entry is skipped and the others survive",
            fun () ->
                let binding = bindingFor gitProviderId "C:/arcs/demo"
                let serialized = WorkspaceBindingStore.serialize [| binding |]

                let damaged =
                    serialized.Replace("\"bindings\": [", "\"bindings\": [ { \"schemaVersion\": 1 },")

                Vitest.expect(WorkspaceBindingStore.deserialize damaged).toEqual [| binding |]
        )
)

Vitest.describe (
    "Provider composition",
    fun () ->
        let catalog () =
            ProviderComposition.createCatalog [
                ProviderComposition.createGitFactory (source AuthStateDto.Empty [])
                ProviderComposition.createLakeFsFactory
                    (ProviderComposition.lakeFsOptions "C:/settings" CaseInsensitive)
                    VersionControlService.LakeFs.LakeFsCredentials.unconfigured
            ]

        Vitest.test (
            "the catalog registers exactly the git and lakefs providers",
            fun () ->
                let ids =
                    ProviderResolver.factories (catalog ())
                    |> Array.map (fun factory -> ProviderId.value factory.Id)
                    |> Array.sort

                Vitest.expect(ids).toEqual [| "git"; "lakefs" |]
        )

        Vitest.test (
            "lakeFS state lives below the settings root",
            fun () ->
                let options = ProviderComposition.lakeFsOptions "C:/settings" CaseSensitive
                Vitest.expect(options.StateRoot.Replace('\\', '/')).toBe "C:/settings/VersionControlState"
                Vitest.expect(options.PathCaseSensitivity).toEqual CaseSensitive
        )

        Vitest.test (
            "path case sensitivity follows the platform",
            fun () ->
                Vitest.expect(ProviderComposition.pathCaseSensitivityForPlatform "win32").toEqual CaseInsensitive
                Vitest.expect(ProviderComposition.pathCaseSensitivityForPlatform "darwin").toEqual CaseInsensitive
                Vitest.expect(ProviderComposition.pathCaseSensitivityForPlatform "linux").toEqual CaseSensitive
        )

        Vitest.test (
            "a persisted binding selects its factory without probing",
            fun () -> promise {
                let store = memoryStore CaseInsensitive
                store.Save(bindingFor lakeFsProviderId "C:/arcs/not-on-disk") |> ignore

                let! resolution =
                    ProviderComposition.resolveVault (catalog ()) store CaseInsensitive "c:/ARCS/not-on-disk"
                    |> Async.StartAsPromise

                match resolution with
                | ProviderComposition.BoundVault(binding, factory) ->
                    Vitest.expect(ProviderId.value factory.Id).toBe "lakefs"
                    Vitest.expect(binding.WorkspaceRoot).toBe "C:/arcs/not-on-disk"
                | other -> failwith $"Expected a bound vault, got {other}"
            }
        )

        Vitest.test (
            "an unbound git repository is adoptable and a plain folder is unmanaged",
            fun () -> promise {
                let! root = createTempDirectoryAsync "swate-vc-composition-"

                try
                    let repoRoot = Main.Bindings.Path.join [| root; "repo" |]
                    let plainRoot = Main.Bindings.Path.join [| root; "plain" |]

                    Main.Bindings.Filesystem.mkdirSync
                        plainRoot
                        (Main.Bindings.Filesystem.MkdirOptions(recursive = true))

                    let store = memoryStore CaseInsensitive
                    let providers = catalog ()

                    let gitFactory =
                        ProviderResolver.tryGetFactory providers gitProviderId
                        |> Option.defaultWith (fun () -> failwith "git factory missing")

                    let! initialized =
                        gitFactory.Initialize
                            {
                                TargetPath = repoRoot
                                Location = None
                            }
                            (OperationContext.detached "composition-init")
                        |> Async.StartAsPromise

                    match initialized with
                    | Succeeded _ -> ()
                    | PartiallySucceeded(_, failure)
                    | Failed failure -> failwith $"Initialize failed ({failure.Code}): {failure.Message}"

                    let! repoResolution =
                        ProviderComposition.resolveVault providers store CaseInsensitive repoRoot
                        |> Async.StartAsPromise

                    match repoResolution with
                    | ProviderComposition.AdoptableVault(factory, _) ->
                        Vitest.expect(ProviderId.value factory.Id).toBe "git"
                    | other -> failwith $"Expected an adoptable vault, got {other}"

                    let! plainResolution =
                        ProviderComposition.resolveVault providers store CaseInsensitive plainRoot
                        |> Async.StartAsPromise

                    match plainResolution with
                    | ProviderComposition.UnmanagedVault _ -> ()
                    | other -> failwith $"Expected an unmanaged vault, got {other}"

                    do! removeDirectoryAsync root
                with error ->
                    do! removeDirectoryAsync root
                    return raise error
            }
        )
)
