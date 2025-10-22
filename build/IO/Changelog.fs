/// By @MangelMaxime: https://github.com/easybuild-org/EasyBuild.PackageReleaseNotes.Tasks/blob/main/src/LastVersionFinder.fs
module Changelog

open System
open System.IO
open System.Text.RegularExpressions
open Semver
open FsToolkit.ErrorHandling
open System.Text.Json

type Version = {
    Version: SemVersion
    Date: DateTime option
    Body: string
} with

    static member Zero = {
        Version = SemVersion(0, 0, 0)
        Date = None
        Body = ""
    }

type private VersionLineData = { Version: string; Date: string option }

let private tryCheckVersionLine (line: string) =
    let m =
        Regex.Match(
            line,
            "^##\\s+\\[?v?(?<version>[^\\]\\s]+)\\]?(\\s-\\s(?<date>\\d{4}-\\d{2}-\\d{2}))?$",
            RegexOptions.Multiline
        )

    // I don't know why, but empty lines are matched
    if m.Success then
        let version = m.Groups.["version"].Value
        // Skip Unreleased versions for KeepAChangelog format
        if version = "Unreleased" then
            None
        else
            let date =
                if m.Groups.["date"].Success then
                    Some m.Groups.["date"].Value
                else
                    None

            { Version = version; Date = date } |> Some
    else
        None

type Errors =
    | NoVersionFound
    | InvalidVersionFormat of line: string
    | InvalidDate of string

    member this.ToText() =
        match this with
        | NoVersionFound -> "No version found"
        | InvalidVersionFormat version -> $"Invalid version format: %s{version}"
        | InvalidDate date -> $"Invalid date format: %s{date}"

let tryFindLastVersion (content: string) =
    let lines = content.Replace("\r\n", "\n").Split('\n') |> Seq.toList

    let rec apply (lines: string list) =
        match lines with
        | [] -> Error NoVersionFound
        | line :: rest ->
            match tryCheckVersionLine line with
            | Some data -> result {
                let! version =
                    match SemVersion.TryParse(data.Version, SemVersionStyles.Strict) with
                    | true, version -> Ok version
                    | false, _ -> Error(InvalidVersionFormat data.Version)

                let! date =
                    match data.Date with
                    | Some date ->
                        match DateTime.TryParse(date) with
                        | true, date -> Ok(Some date)
                        | false, _ -> Error(InvalidDate date)
                    | None -> Ok None

                let body =
                    rest
                    |> List.takeWhile (fun line ->
                        match tryCheckVersionLine line with
                        | Some _ -> false
                        | None -> true
                    )
                    // Remove leading and trailing empty lines (not pretty but it works)
                    |> List.skipWhile (fun line -> line.Trim() = "")
                    |> List.rev
                    |> List.skipWhile (fun line -> line.Trim() = "")
                    |> List.rev
                    |> String.concat "\n"

                return {
                    Version = version
                    Date = date
                    Body = body
                }
                }
            | None -> apply rest

    apply lines

let getChangelogFile () : string =
    let changelogPath = Path.GetFullPath "CHANGELOG.md"

    if not (File.Exists changelogPath) then
        failwithf "CHANGELOG.md not found at %s" changelogPath
        exit 1

    System.IO.File.ReadAllText changelogPath

let getLatestVersion () =
    let changelogLines = getChangelogFile ()

    match tryFindLastVersion changelogLines with
    | Ok version ->
        printGreenfn "Latest version found: %O" version.Version
        version
    | Error err ->
        printRedfn "Error: %s" (err.ToText())
        exit 1