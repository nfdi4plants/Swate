module internal Swate.Components.Composite.Widgets.FilePickerWidgetTests

open ARCtrl
open Feliz
open Fable.Core
open Vitest
open Swate.Components.Shared

Vitest.afterEach (fun () -> RTL.cleanup ())

Vitest.test (
    "file picker keeps path selection and clear-selected after sorting with SortableList",
    fun () -> promise {
        RTL.render (
            FilePickerWidget.Main(
                ArcFiles.Assay(ArcAssay.init "sortable-file-picker"),
                None,
                ignore,
                fun () -> promise { return [| "beta.txt"; "alpha.txt"; "gamma.txt" |] }
            )
        )
        |> ignore

        RTL.fireEvent.click (RTL.screen.getByRole ("button", ByRoleOptions(name = Text "Pick Files")))
        let! beta = RTL.screen.findByTestId "sortable-list-row-beta.txt"
        RTL.fireEvent.click beta
        Vitest.expect(beta.classList.contains "swt:bg-base-300").toBe true

        RTL.fireEvent.click (RTL.screen.getByTestId "sortable-list-down-beta.txt")

        let rows =
            RTL.screen.getAllByTestId (System.Text.RegularExpressions.Regex "^sortable-list-row-")

        Vitest.expect(rows |> Seq.map _.textContent |> Seq.toArray).toEqual [| "alpha.txt"; "beta.txt"; "gamma.txt" |]

        Vitest.expect((RTL.screen.getByTestId "sortable-list-row-beta.txt").classList.contains "swt:bg-base-300").toBe
            true

        RTL.fireEvent.click (RTL.screen.getByTestId "sortable-list-remove-gamma.txt")
        RTL.fireEvent.click (RTL.screen.getByRole ("button", ByRoleOptions(name = Text "Clear Selected")))
        Vitest.expect(RTL.screen.queryByTestId("sortable-list-row-beta.txt").IsNone).toBe true
        Vitest.expect(RTL.screen.queryByTestId("sortable-list-row-gamma.txt").IsNone).toBe true
        RTL.screen.getByTestId "sortable-list-row-alpha.txt" |> ignore

        RTL.screen.getByRole ("button", ByRoleOptions(name = Text "Pick more files"))
        |> ignore

        RTL.screen.getByRole ("button", ByRoleOptions(name = Text "Insert file names"))
        |> ignore
    }
)
