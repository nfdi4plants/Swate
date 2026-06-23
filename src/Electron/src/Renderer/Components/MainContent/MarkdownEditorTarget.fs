module Renderer.Components.MainContent.MarkdownEditorTargetView

open Feliz
open Renderer.Context.UnsavedChangesContext
open Swate.Components.Composite.MarkdownText
open Swate.Components.Composite.MarkdownText.Types
open Swate.Components.Shared
open Renderer.Components.Helper.FileSystemHelper

[<ReactComponent(true)>]
let MarkdownEditorTarget (content: string) =
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()

    let markdown, setMarkdown = React.useState content
    let lastSavedContent, setLastSavedContent = React.useState content
    let isSaving, setIsSaving = React.useState false
    let saveError, setSaveError = React.useState (None: string option)
    let pendingImageAssetsRef = React.useRef ([]: ExternalAssetLink list)

    let selectedPath =
        fileStateCtx.state.Selection.TreePath |> Option.map PathHelpers.normalizePath

    let hasUnsavedChanges = markdown <> lastSavedContent
    let markdownRef = React.useRef markdown
    let lastSavedContentRef = React.useRef lastSavedContent
    let selectedPathRef = React.useRef selectedPath

    React.useEffect (
        (fun () ->
            setMarkdown content
            setLastSavedContent content
            markdownRef.current <- content
            lastSavedContentRef.current <- content
            setSaveError None
            pendingImageAssetsRef.current <- []
        ),
        [| box content |]
    )

    React.useEffect ((fun () -> markdownRef.current <- markdown), [| box markdown |])
    React.useEffect ((fun () -> lastSavedContentRef.current <- lastSavedContent), [| box lastSavedContent |])
    React.useEffect ((fun () -> selectedPathRef.current <- selectedPath), [| box selectedPath |])

    Renderer.Components.MainContent.Helper.usePublishedUnsavedNoteChanges hasUnsavedChanges

    let imageFilePickerAdapter =
        Renderer.Components.MainContent.Helper.useNoteImageFilePickerAdapter pendingImageAssetsRef

    let saveMarkdownAsync () =
        match selectedPathRef.current with
        | None ->
            let message = "No markdown file is selected."
            setSaveError (Some message)
            promise { return Error(exn message) }
        | Some relativePath -> promise {
            setIsSaving true
            setSaveError None

            try
                let currentMarkdown = markdownRef.current
                let currentAssets = pendingImageAssetsRef.current
                let! writeResult = Renderer.Components.MainContent.Helper.writeMarkdownNote relativePath currentMarkdown currentAssets

                match writeResult with
                | Ok() ->
                    setLastSavedContent currentMarkdown
                    lastSavedContentRef.current <- currentMarkdown
                    pendingImageAssetsRef.current <- []
                    return Ok()
                | Error error ->
                    let message = $"Failed to save markdown file: {error.Message}"
                    setSaveError (Some message)
                    return Error(exn message)
            finally
                setIsSaving false
          }

    let saveMarkdown () =
        promise {
            let! _ = saveMarkdownAsync ()
            return ()
        }
        |> Promise.start

    useUnsavedChangesGuard (
        UnsavedChangesGuard.note saveMarkdownAsync (fun () -> markdownRef.current <> lastSavedContentRef.current)
    )

    let saveStatusText =
        match saveError with
        | Some err -> err
        | None when hasUnsavedChanges -> "Unsaved changes"
        | None -> "Saved"

    Html.div [
        prop.className
            "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-auto swt:bg-base-100 swt:p-4 swt:flex swt:flex-col swt:gap-3"
        prop.children [
            Html.div [
                prop.className "swt:flex swt:flex-wrap swt:items-center swt:justify-between swt:gap-2"
                prop.children [
                    Html.span [
                        prop.className (
                            if saveError.IsSome then
                                "swt:text-xs swt:text-error"
                            else
                                "swt:text-xs swt:opacity-70"
                        )
                        prop.text saveStatusText
                    ]
                    Html.button [
                        prop.type'.button
                        prop.className "swt:btn swt:btn-sm swt:btn-primary"
                        prop.disabled (isSaving || selectedPath.IsNone || not hasUnsavedChanges)
                        prop.text (if isSaving then "Saving..." else "Save")
                        prop.onClick (fun _ -> saveMarkdown ())
                    ]
                ]
            ]
            Html.div [
                prop.className "swt:flex-1"
                prop.children [
                    TextInputWithMarkdown.TextInputWithMarkdown(
                        markdown,
                        setMarkdown,
                        placeholder = "Write markdown...",
                        mode = PreviewMode.Live,
                        height = 560,
                        filePickerAdapter = imageFilePickerAdapter
                    )
                ]
            ]
        ]
    ]
