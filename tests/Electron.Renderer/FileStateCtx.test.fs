module ElectronRenderer.FileStateCtxTests

open Renderer.Context.FileStateCtx
open Swate.Components.Shared
open Vitest

Vitest.describe (
    "FileStateCtx controller API",
    fun () ->
        Vitest.test (
            "controller exposes selection mutation without file-tree mutation",
            fun () ->
                let mutable selection = ArcSelection.empty

                let controller: FileStateController = {
                    state = FileState.init ()
                    setSelection = fun nextSelection -> selection <- ArcSelection.normalize nextSelection
                    updateSelection =
                        fun update ->
                            selection <- selection |> update |> ArcSelection.normalize
                }

                controller.setSelection (ArcSelection.forExplorerNode "node-1" None)
                Vitest.expect(selection.ExplorerNodeId).toEqual (Some "node-1")

                controller.updateSelection ArcSelection.clearExplorerNode
                Vitest.expect(selection).toEqual (ArcSelection.empty)
        )
)
