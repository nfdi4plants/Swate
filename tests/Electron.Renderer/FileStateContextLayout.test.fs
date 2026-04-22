module ElectronRenderer.FileStateContextLayoutTests

open Renderer.Context.FileSelectionContext
open Renderer.Context.FileTreeContext
open Renderer.RendererStoreState
open Swate.Components.Shared
open Vitest

Vitest.describe (
    "File state context defaults",
    fun () ->
        Vitest.test (
            "file tree context default is empty and not requested",
            fun () ->
                Vitest.expect(EmptyFileTreeState.Entries).toEqual [||]
                Vitest.expect(EmptyFileTreeState.Status).toEqual (LoadStatus.NotRequested)
        )

        Vitest.test (
            "file selection context default uses empty selection and no-op mutators",
            fun () ->
                Vitest.expect(DefaultFileSelectionController.selection).toEqual (ArcSelection.empty)
                DefaultFileSelectionController.setSelection (ArcSelection.forTreePath (Some "ignored"))
                DefaultFileSelectionController.updateSelection ArcSelection.clearExplorerNode
                Vitest.expect(DefaultFileSelectionController.selection).toEqual (ArcSelection.empty)
        )
)
