module ElectronRenderer.IPCStoreTests

open Renderer.MainUpdateRendererBridge
open Vitest

Vitest.describe (
    "IPCStore",
    fun () ->
        Vitest.test (
            "initial snapshot is ValueNone",
            fun () ->
                let store = IPCStore<int>()
                Vitest.expect(store.GetSnapshot()).toEqual (ValueNone)
        )

        Vitest.test (
            "Update sets ValueSome",
            fun () ->
                let store = IPCStore<int>()
                store.Update(42)
                Vitest.expect(store.GetSnapshot()).toEqual (ValueSome 42)
        )

        Vitest.test (
            "ValueSome None for path close",
            fun () ->
                let store = IPCStore<string option>()
                store.Update(None)
                Vitest.expect(store.GetSnapshot()).toEqual (ValueSome None)
        )

        Vitest.test (
            "Subscribe fires on update",
            fun () ->
                let store = IPCStore<int>()
                let mutable callCount = 0
                store.Subscribe(fun () -> callCount <- callCount + 1) |> ignore
                store.Update(1)
                Vitest.expect(callCount).toBe (1)
        )

        Vitest.test (
            "Multiple subscribers both fire on a single Update",
            fun () ->
                let store = IPCStore<int>()
                let mutable count1 = 0
                let mutable count2 = 0
                store.Subscribe(fun () -> count1 <- count1 + 1) |> ignore
                store.Subscribe(fun () -> count2 <- count2 + 1) |> ignore
                store.Update(1)
                Vitest.expect(count1).toBe (1)
                Vitest.expect(count2).toBe (1)
        )

        Vitest.test (
            "Dispose removes only that subscription",
            fun () ->
                let store = IPCStore<int>()
                let mutable count1 = 0
                let mutable count2 = 0
                let dispose1 = store.Subscribe(fun () -> count1 <- count1 + 1)
                store.Subscribe(fun () -> count2 <- count2 + 1) |> ignore
                dispose1 ()
                store.Update(1)
                Vitest.expect(count1).toBe (0)
                Vitest.expect(count2).toBe (1)
        )

        Vitest.test (
            "No calls after cleanup",
            fun () ->
                let store = IPCStore<int>()
                let mutable callCount = 0
                let dispose = store.Subscribe(fun () -> callCount <- callCount + 1)
                dispose ()
                store.Update(1)
                Vitest.expect(callCount).toBe (0)
        )
)
