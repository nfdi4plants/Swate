module internal Renderer.Components.MainContent.NoteSearchInterop

open System
open Fable.Core.JsInterop
open ARCtrl
open Swate.Components.NoteTypes

let private tryGetStringProperty (source: obj) (propertyName: string) =
    if isNullOrUndefined source then
        None
    else
        let value: obj = source?(propertyName)

        if isNullOrUndefined value then
            None
        else
            match value with
            | :? string as text when String.IsNullOrWhiteSpace text |> not -> Some(text.Trim())
            | _ -> None

let private tryGetPreferredStringProperty (source: obj) (propertyNames: string list) =
    propertyNames |> List.tryPick (tryGetStringProperty source)

let private rehydrateTag (rawTag: obj) =
    let name =
        tryGetPreferredStringProperty rawTag [ "Name"; "_name"; "NameText"; "_nameText" ]

    let source =
        tryGetPreferredStringProperty rawTag [ "TermSourceREF"; "_termSourceREF" ]

    let accession =
        tryGetPreferredStringProperty rawTag [ "TermAccessionNumber"; "_termAccessionNumber" ]

    if name.IsNone && source.IsNone && accession.IsNone then
        None
    else
        Some(OntologyAnnotation(?name = name, ?tsr = source, ?tan = accession))

let rehydrateNote (note: Note) =
    let normalizedTags =
        note.Tags
        |> Option.bind (fun tags ->
            tags
            |> Seq.choose (fun tag -> rehydrateTag (tag :> obj))
            |> ResizeArray
            |> fun normalized ->
                if normalized.Count = 0 then
                    None
                else
                    Some normalized
        )

    match note.Tags, normalizedTags with
    | None, _ -> note
    | Some _, Some tags -> { note with Tags = Some tags }
    | Some _, None -> note
