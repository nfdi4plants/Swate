module Swate.Components.Composite.ValidationPackageSelector.AvprApi

open System
open Fable.Core
open Fable.Core.JsInterop
open Fetch
open ARCtrl.Helper.SemVer
open Swate.Components.Composite.ValidationPackageSelector.Types
open Swate.Components.Composite.ValidationPackageSelector.Helper

// The ARC validation package registry (AVPR) lists every published version of every package as its own entry.
// The selector works on one row per package, so this module decodes the raw registry payload and reduces it
// to the latest version per package name.

[<Literal>]
let BaseUrl = "https://avpr.nfdi4plants.org"

[<Literal>]
let PackagesEndpoint = "/api/v1/packages"

let PackagesUrl = BaseUrl + PackagesEndpoint

module private Decode =

    let stringOrEmpty (value: obj) : string =
        if isNullOrUndefined value then "" else string value

    let optionalString (value: obj) : string option =
        match stringOrEmpty value with
        | "" -> None
        | text -> Some text

    let intOrZero (value: obj) : int =
        if isNullOrUndefined value then
            0
        else
            match Double.TryParse(string value) with
            | true, number -> int number
            | _ -> 0

    let dateOrMin (value: obj) : DateTime =
        match optionalString value with
        | None -> DateTime.MinValue
        | Some text ->
            match DateTime.TryParse text with
            | true, date -> date
            | _ -> DateTime.MinValue

    let arrayOrEmpty (value: obj) : obj[] =
        if isNullOrUndefined value || not (JS.Constructors.Array.isArray value) then
            [||]
        else
            unbox<obj[]> value

    let tag (raw: obj) : OntologyAnnotationDTO = {
        Name = optionalString raw?Name
        TermSourceREF = optionalString raw?TermSourceREF
        TermAccessionNumber = optionalString raw?TermAccessionNumber
    }

    let author (raw: obj) : AuthorDTO = {
        FullName = optionalString raw?FullName
        Email = optionalString raw?Email
        Affiliation = optionalString raw?Affiliation
        AffiliationLink = optionalString raw?AffiliationLink
    }

    let package (raw: obj) : ValidationPackageDTO option =
        match optionalString raw?Name with
        | None -> None
        | Some name ->
            Some(
                ValidationPackageDTO.Create(
                    name,
                    stringOrEmpty raw?Summary,
                    stringOrEmpty raw?Description,
                    intOrZero raw?MajorVersion,
                    intOrZero raw?MinorVersion,
                    intOrZero raw?PatchVersion,
                    stringOrEmpty raw?PreReleaseVersionSuffix,
                    stringOrEmpty raw?BuildMetadataVersionSuffix,
                    // The selector never reads the package script, so the base64 payload is dropped instead of decoded.
                    [||],
                    dateOrMin raw?ReleaseDate,
                    arrayOrEmpty raw?Tags |> Array.map tag,
                    stringOrEmpty raw?ReleaseNotes,
                    stringOrEmpty raw?CQCHookEndpoint,
                    arrayOrEmpty raw?Authors |> Array.map author,
                    stringOrEmpty raw?ProgrammingLanguage
                )
            )

/// Decodes the raw JSON returned by the AVPR packages endpoint. Entries without a name are skipped.
let decodePackages (json: string) : Result<ValidationPackageDTO[], exn> =
    try
        let parsed = JS.JSON.parse json

        if not (JS.Constructors.Array.isArray parsed) then
            Error(exn "Expected the AVPR packages response to be a JSON array.")
        else
            unbox<obj[]> parsed |> Array.choose Decode.package |> Ok
    with error ->
        Error error

/// True when `candidate` is a newer release than `current`.
/// Falls back to a plain numeric comparison when either version string is not valid SemVer.
let private isNewerVersion (current: ValidationPackageDTO) (candidate: ValidationPackageDTO) =
    match SemVer.tryOfString (toVersionString current), SemVer.tryOfString (toVersionString candidate) with
    | Some currentSemVer, Some candidateSemVer -> SemVer.isOlder currentSemVer candidateSemVer
    | _ ->
        (current.MajorVersion, current.MinorVersion, current.PatchVersion) < (candidate.MajorVersion,
                                                                              candidate.MinorVersion,
                                                                              candidate.PatchVersion)

/// Reduces the registry listing to one entry per package name, keeping the newest version.
let latestPerPackage (packages: ValidationPackageDTO[]) : ValidationPackageDTO[] =
    packages
    |> Array.groupBy (fun dto -> dto.Name)
    |> Array.map (fun (_, versions) ->
        versions
        |> Array.reduce (fun current candidate ->
            if isNewerVersion current candidate then
                candidate
            else
                current
        )
    )

/// Decodes the AVPR packages JSON and keeps only the latest version of each package.
let decodeLatestPackages (json: string) : Result<ValidationPackageDTO[], exn> =
    decodePackages json |> Result.map latestPerPackage

/// Fetches the latest version of every package directly from AVPR.
/// Intended for browser hosts. Electron routes the request through the main process instead.
let fetchLatestPackages () : JS.Promise<ValidationPackageDTO[]> = promise {
    let! response =
        fetch PackagesUrl [
            RequestProperties.Method HttpMethod.GET
            requestHeaders [ Accept "application/json" ]
        ]

    if not response.Ok then
        return failwith $"AVPR request failed with HTTP {response.Status}."
    else
        let! json = response.text ()

        match decodeLatestPackages json with
        | Ok packages -> return packages
        | Error error -> return raise error
}
