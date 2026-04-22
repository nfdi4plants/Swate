module ElectronRenderer.IPCStoreTests

open Renderer.IPCStore
open Renderer.RendererStoreState
open Vitest

Vitest.describe (
    "IPCStore",
    fun () ->
        Vitest.test (
            "initial snapshot uses the provided initial value and NotRequested",
            fun () ->
                let store = IPCStore<int>(0)
                let snapshot = store.GetSnapshot()
                Vitest.expect(snapshot.Value).toBe (0)
                Vitest.expect(snapshot.Status).toEqual (LoadStatus.NotRequested)
        )

        Vitest.test (
            "initial snapshot can hold None as a valid domain value",
            fun () ->
                let store = IPCStore<string option>(None)
                let snapshot = store.GetSnapshot()
                Vitest.expect(snapshot.Value).toEqual (None)
                Vitest.expect(snapshot.Status).toEqual (LoadStatus.NotRequested)
        )

        Vitest.test (
            "BeginRefresh transitions NotRequested to Loading and keeps the current value",
            fun () ->
                let store = IPCStore<int>(0)
                store.BeginRefresh()
                let snapshot = store.GetSnapshot()
                Vitest.expect(snapshot.Value).toBe (0)
                Vitest.expect(snapshot.Status).toEqual (LoadStatus.Loading)
        )

        Vitest.test (
            "BeginRefresh while already Loading does not notify twice",
            fun () ->
                let store = IPCStore<int>(0)
                let mutable callCount = 0
                store.Subscribe(fun () -> callCount <- callCount + 1) |> ignore
                store.BeginRefresh()
                store.BeginRefresh()
                Vitest.expect(store.GetSnapshot().Status).toEqual (LoadStatus.Loading)
                Vitest.expect(callCount).toBe (1)
        )

        Vitest.test (
            "Publish sets Ready with the new value",
            fun () ->
                let store = IPCStore<int>(0)
                store.BeginRefresh()
                store.Publish(42)
                let snapshot = store.GetSnapshot()
                Vitest.expect(snapshot.Value).toBe (42)
                Vitest.expect(snapshot.Status).toEqual (LoadStatus.Ready)
        )

        Vitest.test (
            "Publish can set a valid None domain value without nested options",
            fun () ->
                let store = IPCStore<string option>(Some "arc-path")
                store.BeginRefresh()
                store.Publish(None)
                let snapshot = store.GetSnapshot()
                Vitest.expect(snapshot.Value).toEqual (None)
                Vitest.expect(snapshot.Status).toEqual (LoadStatus.Ready)
        )

        Vitest.test (
            "Subscribe fires when BeginRefresh changes status",
            fun () ->
                let store = IPCStore<int>(0)
                let mutable callCount = 0
                store.Subscribe(fun () -> callCount <- callCount + 1) |> ignore
                store.BeginRefresh()
                Vitest.expect(callCount).toBe (1)
        )

        Vitest.test (
            "Subscribe fires on Publish",
            fun () ->
                let store = IPCStore<int>(0)
                let mutable callCount = 0
                store.Subscribe(fun () -> callCount <- callCount + 1) |> ignore
                store.Publish(1)
                Vitest.expect(callCount).toBe (1)
        )

        Vitest.test (
            "Dispose removes only that subscription",
            fun () ->
                let store = IPCStore<int>(0)
                let mutable count1 = 0
                let mutable count2 = 0
                let dispose1 = store.Subscribe(fun () -> count1 <- count1 + 1)
                store.Subscribe(fun () -> count2 <- count2 + 1) |> ignore
                dispose1 ()
                store.Publish(1)
                Vitest.expect(count1).toBe (0)
                Vitest.expect(count2).toBe (1)
        )
)
