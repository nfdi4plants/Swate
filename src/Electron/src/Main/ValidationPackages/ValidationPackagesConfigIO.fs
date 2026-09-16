module Main.ValidationPackages.ValidationPackagesConfigIO

open System
open Fable.Core
open ARCtrl
open ARCtrl.ValidationPackages
open ARCtrl.Yaml
open Main.Bindings.Filesystem
open Main.Bindings.Path

// The validation packages config is the `.arc/validation_packages.yml` file that the DataHUB CI reads to decide
// which validation packages run against an ARC. ARCtrl ships the YAMLicious based codec for it, so this module
// only deals with locating the file, validating incoming YAML, and moving text between disk and IPC.

/// Path of the config relative to the ARC root, currently `.arc/validation_packages.yml`.
let configRelativePath =
    ArcPathHelper.combineMany [|
        ArcPathHelper.ARCConfigFolderName
        ArcPathHelper.ValidationPackagesYamlFileName
    |]

let configPathAtArcPath (arcPath: string) =
    join [|
        resolve [| arcPath |]
        ArcPathHelper.ARCConfigFolderName
        ArcPathHelper.ValidationPackagesYamlFileName
    |]

/// Parses the YAML with the ARCtrl decoder so content that the DataHUB could not read never reaches the disk.
let tryParseConfigYaml (yaml: string) : Result<ValidationPackagesConfig, exn> =
    try
        Ok(ValidationPackagesConfig.fromYamlString yaml)
    with error ->
        Error error

/// Reads the raw config YAML. A missing file is not an error and yields None.
let readConfigYamlAtArcPath (arcPath: string) : JS.Promise<Result<string option, exn>> = promise {
    try
        if String.IsNullOrWhiteSpace arcPath then
            return Error(exn "ARC path must not be empty.")
        else
            let configPath = configPathAtArcPath arcPath
            let! exists = ARCtrl.FileSystemHelper.fileExistsAsync configPath

            if not exists then
                return Ok None
            else
                let! yaml = readFileAsync configPath TextEncoding.Utf8
                return Ok(Some yaml)
    with error ->
        return Error error
}

/// Validates and writes the config YAML, creating the `.arc` folder when it does not exist yet.
let writeConfigYamlAtArcPath (arcPath: string) (yaml: string) : JS.Promise<Result<unit, exn>> = promise {
    try
        if String.IsNullOrWhiteSpace arcPath then
            return Error(exn "ARC path must not be empty.")
        else
            match tryParseConfigYaml yaml with
            | Error parseError -> return Error(exn $"The validation packages config is not valid: {parseError.Message}")
            | Ok _ ->
                let configPath = configPathAtArcPath arcPath
                let! _ = mkdirAsync (dirname configPath) (MkdirOptions(recursive = true))
                do! writeFileAsync configPath yaml TextEncoding.Utf8
                return Ok()
    with error ->
        return Error error
}
