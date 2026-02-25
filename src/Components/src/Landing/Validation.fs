namespace Swate.Components.Landing

open System
open System.Text.RegularExpressions
open ARCtrl

[<RequireQualifiedAccess>]
module Validation =

    let toOptionalString (value: string) =
        let trimmed = value.Trim()
        if String.IsNullOrWhiteSpace trimmed then None else Some trimmed

    let isRequiredDataValid (draft: LandingDraft) =
        not (String.IsNullOrWhiteSpace draft.Title)
        && not (String.IsNullOrWhiteSpace draft.Description)

    let private sanitizeIdentifierCandidate (candidate: string) =
        let cleaned = Regex.Replace(candidate, @"[^a-zA-Z0-9_\- ]", " ").Trim()
        Regex.Replace(cleaned, @"\s+", " ").Trim()

    let private generateSafeIdentifierFromTitle (title: string) =
        let sanitized =
            title
            |> sanitizeIdentifierCandidate
            |> fun value -> Regex.Replace(value, @"\s+", "_").Trim([| '_'; '-' |])

        let candidate =
            if String.IsNullOrWhiteSpace sanitized then
                "Experiment"
            else
                sanitized

        if ARCtrl.Helper.Identifier.tryCheckValidCharacters candidate then
            candidate
        else
            "Experiment"

    let resolveIdentifier (draft: LandingDraft) =
        match toOptionalString draft.Identifier with
        | Some identifier -> identifier
        | None -> generateSafeIdentifierFromTitle draft.Title
