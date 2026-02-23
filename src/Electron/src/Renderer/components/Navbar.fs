module Renderer.components.Navbar


let onSaveClick arcFileState setPreviewData setPreviewError setDidSelectFile _ =
    match arcFileState with
    | None -> ()
    | Some arcFile ->
        promise {
            let! result = Renderer.ArcFilePersistence.saveArcFileWithPreview arcFile

            match result with
            | Ok updatedPreview ->
                setPreviewData (Some updatedPreview)
                setPreviewError None
                setDidSelectFile true
            | Error errorMsg ->
                setPreviewError (Some $"Save failed: {errorMsg}")
        }
        |> Promise.start

