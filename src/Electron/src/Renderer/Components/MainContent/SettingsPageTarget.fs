module Renderer.Components.MainContent.SettingsPageTarget

open Feliz
open Renderer.Components.Helper.ArcVaultHelper
open Swate.Components.Primitive.ErrorModal.Context

[<ReactComponent(true)>]
let SettingsPage () =
    let errorModal = useErrorModalCtx ()
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()

    let onEnsureNotesError =
        createErrorModalCallback errorModal.enqueue "Could not create notes folder" appStateCtx

    let onAutoCreateNotesFolderEnabled () =
        ensureNotesFolder onEnsureNotesError |> Promise.start

    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-y-auto"
        prop.testId "main-content-settings-page"
        prop.children [
            Swate.Components.PageComponents.SettingsPage.SettingsPage.SettingsPage(
                onAutoCreateNotesFolderEnabled = onAutoCreateNotesFolderEnabled
            )
        ]
    ]