module Renderer.Components.MainContent.ValidationPackagesTargetHelper

open System
open ARCtrl.ValidationPackages
open ARCtrl.Yaml
open Swate.Components.Composite.ValidationPackageSelector
open Swate.Components.Composite.ValidationPackageSelector.Types

// Main hands the renderer plain strings over IPC. This module converts between those strings and the
// ARCtrl config object that the ValidationPackageSelector component works with.

let emptyConfig () =
    ValidationPackagesConfig.make (ResizeArray<ValidationPackage>()) None

/// A missing or blank config file yields an empty config so the page can start from scratch.
let parseConfigOrDefault (yaml: string option) : Result<ValidationPackagesConfig, exn> =
    match yaml with
    | None -> Ok(emptyConfig ())
    | Some text when String.IsNullOrWhiteSpace text -> Ok(emptyConfig ())
    | Some text ->
        try
            Ok(ValidationPackagesConfig.fromYamlString text)
        with error ->
            Error error

/// Uses the compact sequence layout so the file matches what ARCitect writes.
let serializeConfig (config: ValidationPackagesConfig) : string =
    ValidationPackagesYaml.toCompactYamlString config

let decodePackages (json: string) : Result<ValidationPackageDTO[], exn> = AvprApi.decodeLatestPackages json
