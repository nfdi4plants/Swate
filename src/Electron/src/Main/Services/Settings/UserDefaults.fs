/// User overrides of the defaults that every new ARC session starts from. The overrides live in
/// user-defaults.json in the settings folder. Only values that differ from the built-in ones are
/// stored, so a profile without overrides behaves exactly like a fresh install.
module Main.UserDefaults

open System
open Fable.Core
open Thoth.Json.Core
open Main.VersionControl.VersionControlSettings
open Swate.Electron.Shared.IPCTypes

[<Literal>]
let userDefaultsFileName = "user-defaults.json"

[<Literal>]
let CurrentSchemaVersion = 1

/// The values Swate ships with. The git values come from VersionControlSettings.defaults.
let builtIn: UserDefaultValuesDto = {
    LfsAutoTrackThresholdMb = VersionControlSettings.defaults.AutoTrackThresholdMb
    DownloadLargeFiles = VersionControlSettings.defaults.DownloadLargeFiles
    DiffIndexingLimitMb = VersionControlSettings.defaults.DiffIndexingLimitMb
    UploadRightAway = true
    AutoCreateNotesFolder = true
}

/// One entry per value. None means the user has not changed it.
type UserDefaultOverrides = {
    LfsAutoTrackThresholdMb: int option
    DownloadLargeFiles: bool option
    DiffIndexingLimitMb: int option
    UploadRightAway: bool option
    AutoCreateNotesFolder: bool option
}

let private noOverrides: UserDefaultOverrides = {
    LfsAutoTrackThresholdMb = None
    DownloadLargeFiles = None
    DiffIndexingLimitMb = None
    UploadRightAway = None
    AutoCreateNotesFolder = None
}

let private thresholdInRange (value: int) =
    value >= VersionControlSettings.MinAutoTrackThresholdMb
    && value <= VersionControlSettings.MaxAutoTrackThresholdMb

let private diffLimitInRange (value: int) =
    value >= VersionControlSettings.MinDiffIndexingLimitMb
    && value <= VersionControlSettings.MaxDiffIndexingLimitMb

/// Ok when every value is within its allowed range. The message names the first refused field.
let validate (values: UserDefaultValuesDto) : Result<UserDefaultValuesDto, string> =
    if not (thresholdInRange values.LfsAutoTrackThresholdMb) then
        Error
            $"The automatic large-object threshold (LfsAutoTrackThresholdMb) must be from {VersionControlSettings.MinAutoTrackThresholdMb} to {VersionControlSettings.MaxAutoTrackThresholdMb} MiB."
    elif not (diffLimitInRange values.DiffIndexingLimitMb) then
        Error
            $"The background indexing limit (DiffIndexingLimitMb) must be from {VersionControlSettings.MinDiffIndexingLimitMb} to {VersionControlSettings.MaxDiffIndexingLimitMb} MiB."
    else
        Ok values

type private StoredDefaults =
    /// Written by a newer Swate. The file is ignored and never overwritten.
    | NewerSchema
    | Stored of UserDefaultOverrides

/// A value of the wrong type counts as absent, so one damaged entry does not hide the others.
let private tolerant (decoder: Decoder<'T>) : Decoder<'T option> =
    Decode.oneOf [ Decode.map Some decoder; Decode.succeed None ]

let private storedDecoder: Decoder<StoredDefaults> =
    Decode.field "schemaVersion" Decode.int
    |> Decode.andThen (fun version ->
        if version > CurrentSchemaVersion then
            Decode.succeed NewerSchema
        elif version < 1 then
            Decode.fail $"Unknown user defaults schema version {version}."
        else
            Decode.object (fun get ->
                // An out-of-range value falls back to the built-in one, field by field.
                Stored {
                    LfsAutoTrackThresholdMb =
                        get.Optional.Field "lfsAutoTrackThresholdMb" (tolerant Decode.int)
                        |> Option.bind id
                        |> Option.filter thresholdInRange
                    DownloadLargeFiles =
                        get.Optional.Field "downloadLargeFiles" (tolerant Decode.bool) |> Option.bind id
                    DiffIndexingLimitMb =
                        get.Optional.Field "diffIndexingLimitMb" (tolerant Decode.int)
                        |> Option.bind id
                        |> Option.filter diffLimitInRange
                    UploadRightAway = get.Optional.Field "uploadRightAway" (tolerant Decode.bool) |> Option.bind id
                    AutoCreateNotesFolder =
                        get.Optional.Field "autoCreateNotesFolder" (tolerant Decode.bool)
                        |> Option.bind id
                }
            )
    )

/// Unreadable content yields the built-in values.
let private parse (content: string) : StoredDefaults =
    try
        if String.IsNullOrWhiteSpace content then
            Stored noOverrides
        else
            ARCtrl.Json.Decode.fromJsonString storedDecoder content
    with _ ->
        Stored noOverrides

let private serialize (overrides: UserDefaultOverrides) : string =
    JS.JSON.stringify (
        {|
            schemaVersion = CurrentSchemaVersion
            lfsAutoTrackThresholdMb = overrides.LfsAutoTrackThresholdMb
            downloadLargeFiles = overrides.DownloadLargeFiles
            diffIndexingLimitMb = overrides.DiffIndexingLimitMb
            uploadRightAway = overrides.UploadRightAway
            autoCreateNotesFolder = overrides.AutoCreateNotesFolder
        |},
        space = 2
    )

let private effectiveOf (overrides: UserDefaultOverrides) : UserDefaultValuesDto = {
    LfsAutoTrackThresholdMb =
        overrides.LfsAutoTrackThresholdMb
        |> Option.defaultValue builtIn.LfsAutoTrackThresholdMb
    DownloadLargeFiles = overrides.DownloadLargeFiles |> Option.defaultValue builtIn.DownloadLargeFiles
    DiffIndexingLimitMb = overrides.DiffIndexingLimitMb |> Option.defaultValue builtIn.DiffIndexingLimitMb
    UploadRightAway = overrides.UploadRightAway |> Option.defaultValue builtIn.UploadRightAway
    AutoCreateNotesFolder =
        overrides.AutoCreateNotesFolder
        |> Option.defaultValue builtIn.AutoCreateNotesFolder
}

/// A value equal to the built-in one is no override.
let private overridesOf (values: UserDefaultValuesDto) : UserDefaultOverrides =
    let differing (value: 'T) (builtInValue: 'T) =
        if value = builtInValue then None else Some value

    {
        LfsAutoTrackThresholdMb = differing values.LfsAutoTrackThresholdMb builtIn.LfsAutoTrackThresholdMb
        DownloadLargeFiles = differing values.DownloadLargeFiles builtIn.DownloadLargeFiles
        DiffIndexingLimitMb = differing values.DiffIndexingLimitMb builtIn.DiffIndexingLimitMb
        UploadRightAway = differing values.UploadRightAway builtIn.UploadRightAway
        AutoCreateNotesFolder = differing values.AutoCreateNotesFolder builtIn.AutoCreateNotesFolder
    }

type IUserDefaultsStore =
    /// The values a new session starts from, read from the file once and then served from memory.
    abstract Effective: unit -> UserDefaultValuesDto
    /// Validates the values and stores those that differ from the built-in ones. The Error names
    /// the refused field, or says why the file could not be written. Nothing changes on an Error.
    abstract Update: UserDefaultValuesDto -> Result<UserDefaultValuesDto, string>

/// A store over caller-supplied read and write functions, so tests and the settings folder share one
/// implementation. A throwing reader counts as a missing file and a throwing writer as a failed write.
let create (read: unit -> string option) (write: string -> Result<unit, string>) : IUserDefaultsStore =
    let load () =
        try
            match read () with
            | Some content -> parse content
            | None -> Stored noOverrides
        with _ ->
            Stored noOverrides

    let cached: UserDefaultOverrides option ref = ref None

    { new IUserDefaultsStore with
        member _.Effective() =
            match cached.Value with
            | Some overrides -> effectiveOf overrides
            | None ->
                let overrides =
                    match load () with
                    | NewerSchema -> noOverrides
                    | Stored overrides -> overrides

                cached.Value <- Some overrides
                effectiveOf overrides

        member _.Update values =
            match validate values with
            | Error message -> Error message
            | Ok values ->
                // Read the file again, because another Swate process on the same profile may have
                // written a newer schema since the cache was filled.
                match load () with
                | NewerSchema ->
                    cached.Value <- Some noOverrides

                    Error
                        "The saved defaults were written by a newer version of Swate. They stay as they are until Swate is updated."
                | Stored _ ->
                    let overrides = overridesOf values

                    let written =
                        try
                            write (serialize overrides)
                        with error ->
                            Error $"Could not write the user defaults: {error.Message}"

                    match written with
                    | Error message -> Error message
                    | Ok() ->
                        cached.Value <- Some overrides
                        Ok(effectiveOf overrides)
    }

/// The store in the application settings folder.
let createSettingsStore () : IUserDefaultsStore =
    create
        (fun () -> SettingsStore.tryReadSettingsFile userDefaultsFileName)
        (fun content -> SettingsStore.tryWriteSettingsFileAtomic userDefaultsFileName content)

/// The store of the running app. It reads the settings folder on first use, after the app is ready.
let production: IUserDefaultsStore = createSettingsStore ()

let toVersionControlSettings (values: UserDefaultValuesDto) : VersionControlSettings = {
    AutoTrackThresholdMb = values.LfsAutoTrackThresholdMb
    DownloadLargeFiles = values.DownloadLargeFiles
    DiffIndexingLimitMb = values.DiffIndexingLimitMb
}

let effectiveVersionControlSettings (store: IUserDefaultsStore) : VersionControlSettings =
    toVersionControlSettings (store.Effective())

let effectiveUploadRightAway (store: IUserDefaultsStore) : bool = store.Effective().UploadRightAway

let effectiveAutoCreateNotesFolder (store: IUserDefaultsStore) : bool = store.Effective().AutoCreateNotesFolder

/// What a new session starts from. The session host reads it once, when the session opens.
type SessionDefaults = {
    InitialSettings: VersionControlSettings
    InitialUploadRightAway: bool
}

let sessionDefaultsOf (store: IUserDefaultsStore) : SessionDefaults = {
    InitialSettings = effectiveVersionControlSettings store
    InitialUploadRightAway = effectiveUploadRightAway store
}

/// The session defaults of a profile without overrides.
let builtInSessionDefaults () : SessionDefaults = {
    InitialSettings = toVersionControlSettings builtIn
    InitialUploadRightAway = builtIn.UploadRightAway
}
