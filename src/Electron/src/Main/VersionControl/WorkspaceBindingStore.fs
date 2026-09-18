/// Persists the library's WorkspaceBinding per vault root in the application settings
/// folder. Bindings are stored exactly as the provider returned them. Lookup compares
/// roots with the resolver's normalization and the host's case sensitivity, so a
/// differently spelled path still finds its binding without rewriting the stored one.
module Main.VersionControl.WorkspaceBindingStore

open System
open Fable.Core
open Thoth.Json.Core
open VersionControlService.Abstractions

[<Literal>]
let bindingsSettingsFileName = "version-control-bindings.json"

type IWorkspaceBindingStore =
    abstract TryFind: workspaceRoot: string -> WorkspaceBinding option
    abstract Save: WorkspaceBinding -> Result<unit, string>
    abstract Remove: workspaceRoot: string -> Result<unit, string>
    abstract List: unit -> WorkspaceBinding[]

/// Fails as a decode error, never as an exception, so a bad provider id counts as a
/// damaged entry and the other entries survive.
let private providerIdDecoder: Decoder<ProviderId> =
    Decode.string
    |> Decode.andThen (fun value ->
        match ProviderId.tryCreate value with
        | Ok providerId -> Decode.succeed providerId
        | Error message -> Decode.fail message
    )

/// Entries written by a newer schema are skipped instead of handed to a provider.
let private schemaVersionDecoder: Decoder<int> =
    Decode.int
    |> Decode.andThen (fun version ->
        if version > WorkspaceBinding.CurrentSchemaVersion then
            Decode.fail
                $"Binding schema version {version} is newer than the supported {WorkspaceBinding.CurrentSchemaVersion}."
        else
            Decode.succeed version
    )

let private locationDecoder: Decoder<RepositoryLocation> =
    Decode.object (fun get -> {
        ProviderId = get.Required.Field "providerId" providerIdDecoder
        DisplayName = get.Optional.Field "displayName" Decode.string
        ProviderLocation = get.Required.Field "providerLocation" Decode.string
        ConnectionProfileId = get.Optional.Field "connectionProfileId" Decode.string
    })

let private bindingDecoder: Decoder<WorkspaceBinding> =
    Decode.object (fun get -> {
        SchemaVersion = get.Required.Field "schemaVersion" schemaVersionDecoder
        ProviderId = get.Required.Field "providerId" providerIdDecoder
        WorkspaceRoot = get.Required.Field "workspaceRoot" Decode.string
        ProviderStateRef = get.Optional.Field "providerStateRef" Decode.string
        Location = get.Required.Field "location" locationDecoder
        ConnectionProfileId = get.Optional.Field "connectionProfileId" Decode.string
    })

/// One entry that does not decode becomes None, so a damaged entry drops out
/// without taking the other bindings with it.
let private tolerantBindingDecoder: Decoder<WorkspaceBinding option> =
    Decode.oneOf [ Decode.map Some bindingDecoder; Decode.succeed None ]

let private bindingsDecoder: Decoder<WorkspaceBinding[]> =
    Decode.object (fun get ->
        get.Required.Field "bindings" (Decode.array tolerantBindingDecoder)
        |> Array.choose id
    )

let private encodeBinding (binding: WorkspaceBinding) = {|
    schemaVersion = binding.SchemaVersion
    providerId = ProviderId.value binding.ProviderId
    workspaceRoot = binding.WorkspaceRoot
    providerStateRef = binding.ProviderStateRef
    location = {|
        providerId = ProviderId.value binding.Location.ProviderId
        displayName = binding.Location.DisplayName
        providerLocation = binding.Location.ProviderLocation
        connectionProfileId = binding.Location.ConnectionProfileId
    |}
    connectionProfileId = binding.ConnectionProfileId
|}

let serialize (bindings: WorkspaceBinding[]) : string =
    JS.JSON.stringify (
        {|
            bindings = bindings |> Array.map encodeBinding
        |},
        space = 2
    )

/// Unreadable content yields no bindings. A single damaged entry is skipped.
let deserialize (content: string) : WorkspaceBinding[] =
    try
        if String.IsNullOrWhiteSpace content then
            [||]
        else
            ARCtrl.Json.Decode.fromJsonString bindingsDecoder content
    with _ -> [||]

let rootsEqual (sensitivity: PathCaseSensitivity) (left: string) (right: string) =
    let comparison =
        match sensitivity with
        | CaseSensitive -> StringComparison.Ordinal
        | CaseInsensitive -> StringComparison.OrdinalIgnoreCase

    String.Equals(ProviderResolver.normalizePath left, ProviderResolver.normalizePath right, comparison)

/// A store over caller-supplied read and write functions, so tests and the settings
/// folder share one implementation. The write function reports its own failure.
let create
    (sensitivity: PathCaseSensitivity)
    (read: unit -> string option)
    (write: string -> Result<unit, string>)
    : IWorkspaceBindingStore =
    let load () =
        read () |> Option.map deserialize |> Option.defaultValue [||]

    let sameRoot (workspaceRoot: string) (binding: WorkspaceBinding) =
        rootsEqual sensitivity binding.WorkspaceRoot workspaceRoot

    let persist (bindings: WorkspaceBinding[]) =
        try
            write (serialize bindings)
        with error ->
            Error $"Could not persist the workspace bindings: {error.Message}"

    { new IWorkspaceBindingStore with
        member _.TryFind workspaceRoot =
            load () |> Array.tryFind (sameRoot workspaceRoot)

        member _.Save binding =
            let others = load () |> Array.filter (sameRoot binding.WorkspaceRoot >> not)

            persist (Array.append others [| binding |])

        member _.Remove workspaceRoot =
            let current = load ()
            let remaining = current |> Array.filter (sameRoot workspaceRoot >> not)

            if remaining.Length = current.Length then
                Ok()
            else
                persist remaining

        member _.List() = load ()
    }

/// The production store in the application settings folder.
let createSettingsStore (sensitivity: PathCaseSensitivity) : IWorkspaceBindingStore =
    create
        sensitivity
        (fun () -> Main.SettingsStore.tryReadSettingsFile bindingsSettingsFileName)
        (fun content -> Main.SettingsStore.tryWriteSettingsFileAtomic bindingsSettingsFileName content)
