module Swate.Electron.Shared.ApplicationVersion

open Fable.Core

[<ImportDefault("../../../../CHANGELOG.md?raw")>]
let private changelog: string = jsNative

let current =
    changelog.Split(Swate.Components.ClipboardContract.Contract.LineBreaks, System.StringSplitOptions.None)
    |> Array.tryPick (fun line ->
        if line.StartsWith "## " then
            let heading = line.Substring 3
            let dateSeparatorIndex = heading.IndexOf " - "

            if dateSeparatorIndex > 0 then
                heading.Substring(0, dateSeparatorIndex)
                |> ARCtrl.Helper.SemVer.SemVer.tryOfString
                |> Option.map _.AsString()
            else
                None
        else
            None
    )
    |> Option.defaultValue "Unknown"

let windowTitle (arcName: string option) =
    match arcName with
    | Some name when not (System.String.IsNullOrWhiteSpace name) -> $"Swate {current} — {name}"
    | _ -> $"Swate {current}"
