module Renderer.Components.MainContent.MarkdownEditorTargetView

open Feliz
open Swate.Components.Composite.MarkdownText
open Swate.Components.Composite.MarkdownText.Types
open Swate.Components.Composite.Notes.Editor
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.MainContent.NoteMoveHelper
open Renderer.Components.MainContent.NoteTargetConflictHelper

[<ReactComponent(true)>]
let MarkdownEditorTarget (content: string) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let fileStateCtx = Renderer.Context.FileStateContext.useFileStateCtx ()
    let errorModalCtx = useErrorModalCtx ()
    let markdown, setMarkdown = React.useState content
    let lastSavedContent, setLastSavedContent = React.useState content
    let isSaving, setIsSaving = React.useState false
    let saveError, setSaveError = React.useState (None: string option)
    let selectedExistingTarget, setSelectedExistingTarget = React.useState (None: ExistingTargetRef option)
    let isMovingToExistingTarget, setIsMovingToExistingTarget = React.useState false
    let moveError, setMoveError = React.useState (None: string option)

    let selectedPath =
        fileStateCtx.state.Selection.TreePath |> Option.map PathHelpers.normalizePath

    let hasUnsavedChanges = markdown <> lastSavedContent
    let isBusy = isSaving || isMovingToExistingTarget

    let availableNotesTargets =
        React.useMemo (
            (fun _ -> createAvailableNotesTargets fileStateCtx.state.FileTree),
            [| box fileStateCtx.state.FileTree |]
        )

    React.useEffect (
        (fun () ->
            setMarkdown content
            setLastSavedContent content
            setSaveError None
            setMoveError None
            setSelectedExistingTarget None
        ),
        [| box content |]
    )

    let saveMarkdown () =
        match selectedPath with
        | None -> setSaveError (Some "No markdown file is selected.")
        | Some relativePath ->
            promise {
                setIsSaving true
                setSaveError None
                setMoveError None

                let request: FileContentDTO =
                    FileContentDTO.create FileContentType.Markdown markdown relativePath

                let! writeResult = Api.ipcArcVaultApi.writeFile request

                match writeResult with
                | Ok() -> setLastSavedContent markdown
                | Error exn -> setSaveError (Some $"Failed to save markdown file: {exn.Message}")

                setIsSaving false
            }
            |> Promise.start

    let executeMovePlan (plan: ExistingTargetNoteMovePlan) =
        promise {
            setIsMovingToExistingTarget true
            setSaveError None
            setMoveError None

            let! writeResult = Api.ipcArcVaultApi.writeFile plan.Request

            match writeResult with
            | Error exn -> setMoveError (Some $"Failed to move note: {exn.Message}")
            | Ok() ->
                let! deleteResult = Api.ipcArcVaultApi.deletePath plan.SourcePath

                match deleteResult with
                | Error exn ->
                    setMoveError (
                        Some
                            $"Moved note to '{plan.TargetPath}', but failed to delete the original note: {exn.Message}"
                    )
                | Ok() ->
                    fileStateCtx.setSelection (ArcSelection.forTreePath (Some plan.TargetPath))
                    setLastSavedContent markdown
                    setSelectedExistingTarget None

                    let! previewResult = Api.ipcArcVaultApi.openFile plan.TargetPath

                    match previewResult with
                    | Ok previewData ->
                        let pageState = Renderer.Types.PageState.fromFileContentDTO previewData
                        pageStateCtx.setState (Some pageState)
                    | Error _ -> pageStateCtx.setState (Some(Renderer.Types.PageState.MarkdownPage markdown))

            setIsMovingToExistingTarget false
        }
        |> Promise.catch (fun exn ->
            setMoveError (Some $"Failed to move note: {exn.Message}")
            setIsMovingToExistingTarget false
        )
        |> Promise.start

    let showMoveConflict (plan: ExistingTargetNoteMovePlan) =
        setMoveError None
        showOverwriteConflictModal errorModalCtx plan.TargetPath (fun () -> executeMovePlan plan)

    let blockOrExecuteMovePlan (plan: ExistingTargetNoteMovePlan) =
        promise {
            setIsMovingToExistingTarget true
            setSaveError None
            setMoveError None

            let! targetExists = targetExistsOnDisk plan.TargetPath

            if targetExists then
                setIsMovingToExistingTarget false
                showMoveConflict plan
            else
                executeMovePlan plan
        }
        |> Promise.catch (fun exn ->
            setMoveError (Some $"Failed to check target note: {exn.Message}")
            setIsMovingToExistingTarget false
        )
        |> Promise.start

    let requestMoveToExistingTarget () =
        match selectedExistingTarget with
        | None -> setMoveError (Some "Select a Study or Assay target first.")
        | Some targetRef ->
            match
                tryBuildMoveToExistingTargetPlan
                    selectedPath
                    markdown
                    targetRef
                    (fileStateCtx.state.FileTree |> Array.map _.path)
            with
            | Error errorMessage -> setMoveError (Some errorMessage)
            | Ok(ExistingTargetNoteMovePlanResult.Ready plan) -> blockOrExecuteMovePlan plan
            | Ok(ExistingTargetNoteMovePlanResult.TargetConflict plan) -> showMoveConflict plan

    let saveStatusText =
        match saveError, moveError with
        | Some err, _ -> err
        | None, Some err -> err
        | None, None when hasUnsavedChanges -> "Unsaved changes"
        | None, None -> "Saved"

    Html.div [
        prop.className
            "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-auto swt:bg-base-100 swt:p-4 swt:flex swt:flex-col swt:gap-3"
        prop.children [
            Html.div [
                prop.className "swt:flex swt:flex-wrap swt:items-center swt:justify-between swt:gap-2"
                prop.children [
                    Html.span [
                        prop.className (
                            if saveError.IsSome || moveError.IsSome then
                                "swt:text-xs swt:text-error"
                            else
                                "swt:text-xs swt:opacity-70"
                        )
                        prop.text saveStatusText
                    ]
                    Html.button [
                        prop.type'.button
                        prop.className "swt:btn swt:btn-sm swt:btn-primary"
                        prop.disabled (isBusy || selectedPath.IsNone || not hasUnsavedChanges)
                        prop.text (if isSaving then "Saving..." else "Save")
                        prop.onClick (fun _ -> saveMarkdown ())
                    ]
                    Html.button [
                        prop.type'.button
                        prop.testId "markdown-add-existing-button"
                        prop.className "swt:btn swt:btn-sm swt:btn-secondary"
                        prop.disabled (isBusy || selectedPath.IsNone || selectedExistingTarget.IsNone)
                        prop.text "Add to existing Assay/Study"
                        prop.onClick (fun _ -> requestMoveToExistingTarget ())
                    ]
                ]
            ]
            Html.div [
                prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-2"
                prop.children [
                    Html.div [
                        prop.className "swt:min-w-64 swt:max-w-md swt:flex-1"
                        prop.children [
                            TargetSelector.Main(
                                selectedExistingTarget,
                                (fun target ->
                                    setSelectedExistingTarget target
                                    setMoveError None
                                ),
                                availableNotesTargets,
                                isBusy
                            )
                        ]
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
                        height = 560
                    )
                ]
            ]
        ]
    ]
