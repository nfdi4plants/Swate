module ElectronRenderer.InitStateTests

open Renderer.Components.Composite.ArcOpening
open Vitest

let private createRequestGate initiallyBusy =
    let mutable isBusy = initiallyBusy

    let gate: Helper.RequestGate = {
        tryBegin =
            fun () ->
                if isBusy then
                    false
                else
                    isBusy <- true
                    true
        finish = fun () -> isBusy <- false
    }

    gate

Vitest.describe (
    "InitState ARC opening",
    fun () ->
        Vitest.test (
            "does not enter the busy state when folder selection is cancelled",
            fun () -> promise {
                let busyStates = ResizeArray<bool>()
                let mutable openWasCalled = false

                do!
                    Helper.openWithProgress
                        (createRequestGate false)
                        (fun () -> promise { return Error(exn "Cancelled") })
                        (fun _ ->
                            openWasCalled <- true
                            promise { return false }
                        )
                        ignore
                        busyStates.Add

                Vitest.expect(openWasCalled).toBe (false)
                Vitest.expect(busyStates.ToArray()).toEqual ([||])
            }
        )

        Vitest.test (
            "enters the busy state only after selection and clears it after opening",
            fun () -> promise {
                let busyStates = ResizeArray<bool>()

                do!
                    Helper.openWithProgress
                        (createRequestGate false)
                        (fun () -> promise {
                            Vitest.expect(busyStates.ToArray()).toEqual ([||])
                            return Ok "C:/selected-arc"
                        })
                        (fun selectedPath -> promise {
                            Vitest.expect(selectedPath).toBe ("C:/selected-arc")
                            Vitest.expect(busyStates.ToArray()).toEqual ([| true |])
                            return true
                        })
                        ignore
                        busyStates.Add

                Vitest.expect(busyStates.ToArray()).toEqual ([| true; false |])
            }
        )

        Vitest.test (
            "ignores another request while an ARC is already opening",
            fun () -> promise {
                let mutable pickerWasCalled = false

                do!
                    Helper.openWithProgress
                        (createRequestGate true)
                        (fun () ->
                            pickerWasCalled <- true
                            promise { return Ok "C:/other-arc" }
                        )
                        (fun _ -> promise { return true })
                        ignore
                        ignore

                Vitest.expect(pickerWasCalled).toBe (false)
            }
        )

        Vitest.test (
            "uses the busy state when opening an already selected ARC path",
            fun () -> promise {
                let busyStates = ResizeArray<bool>()

                do!
                    Helper.openPathWithProgress
                        (createRequestGate false)
                        "C:/recent-arc"
                        (fun selectedPath -> promise {
                            Vitest.expect(selectedPath).toBe ("C:/recent-arc")
                            Vitest.expect(busyStates.ToArray()).toEqual ([| true |])
                            return true
                        })
                        busyStates.Add

                Vitest.expect(busyStates.ToArray()).toEqual ([| true; false |])
            }
        )
)
