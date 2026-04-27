module Renderer.Components.MainContent.NotesSearchTarget

open System
open Fable.Core.JsInterop
open Feliz
open ARCtrl
open Swate.Components
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Components.NoteTypes

module private NoteSearchInterop =

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
            tryGetPreferredStringProperty rawTag [ "Name"; "_name" ]

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

[<ReactComponent>]
let NotesSearchTarget () =

    let pageCtx = Renderer.Context.PageStateContext.usePageStateCtx()
    let fileTreeCtx = Renderer.Context.FileStateContext.useFileStateCtx()
    let arcObjectCtx = Renderer.Context.ArcObjectExplorerContext.useArcObjectExplorerCtx()
    let notes, setNotes = React.useState ([]: Note list)

    let isLoading, setIsLoading = React.useState true
    let error, setError = React.useState (None: string option)

    React.useEffect (
        (fun () ->
            let mutable isDisposed = false

            setIsLoading true
            setError None

            promise {
                let! result = Api.ipcArcVaultApi.readNotes (unbox null)

                if not isDisposed then
                    match result with
                    | Ok nextNotes ->
                        setNotes (nextNotes |> Array.map NoteSearchInterop.rehydrateNote |> Array.toList)
                        setIsLoading false
                    | Result.Error exn ->
                        setNotes []
                        setError (Some $"Failed to load notes: {exn.Message}")
                        setIsLoading false
            }
            |> Promise.start

            fun () -> isDisposed <- true
        ),
        [||]
    )

    let openNote (relativePath: string) =
        promise {

            let! result = Api.ipcArcVaultApi.openFile (unbox null) relativePath

            match result with
            | Ok dto ->
                let selectedPath = normalizePath relativePath
                fileTreeCtx.setSelection (ArcSelection.forTreePath (Some selectedPath))

                dto
                |> Renderer.Components.ARCHelper.viewLoadResultOfDto
                |> Renderer.Components.ARCHelper.applyLoadedView
                    pageCtx.setState
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage
            | Result.Error exn ->
                fileTreeCtx.setSelection (ArcSelection.clearExplorerNode fileTreeCtx.state.Selection)

                Renderer.Components.ARCHelper.applyViewError
                    pageCtx.setState
                    arcObjectCtx.setArcFileState
                    arcObjectCtx.setPreviewState
                    arcObjectCtx.setStatusMessage
                    $"Could not open note: {exn.Message}"
        }
        |> Promise.start

    SearchComponent.Main(notes, isLoading, error, openNote)
