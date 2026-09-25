module internal Swate.Components.Tests.UseMutableStore

open ARCtrl
open Swate.Components.Hooks.UseMutableStore
open Swate.Components.Shared
open Vitest

let private makeArcFile identifier =
    let assay = ArcAssay.init identifier
    assay.AddTable(ArcTable.init "Test Table")
    ArcFiles.Assay assay

Vitest.describe (
    "MutableStore",
    fun () ->
        Vitest.test (
            "keeps a stable snapshot while nothing changes",
            fun () ->
                let store = MutableStore(makeArcFile "stable")
                Vitest.expect(store.GetSnapshot()).toBe (store.GetSnapshot())
        )

        Vitest.test (
            "mutate signals subscribers even when the tracked reference is unchanged",
            fun () ->
                let arcFile = makeArcFile "same-reference"
                let store = MutableStore(arcFile)
                let mutable notifications = 0
                store.Subscribe(fun () -> notifications <- notifications + 1) |> ignore

                store.Mutate(fun current ->
                    Vitest.expect(current).toBe arcFile
                    current.Tables().[0].AddColumn(CompositeHeader.Comment "Added")
                )

                Vitest.expect(notifications).toBe 1
                let snapshot = store.GetSnapshot()
                Vitest.expect(snapshot.value).toBe arcFile
                Vitest.expect(snapshot.value.Tables().[0].ColumnCount).toBe 1
                Vitest.expect(snapshot.version).toBe 1
        )

        Vitest.test (
            "in-place mutate callback increments the revision",
            fun () ->
                let store = MutableStore(makeArcFile "revision")
                store.Mutate ignore
                Vitest.expect(store.GetSnapshot().version).toBe 1
                store.Mutate ignore
                Vitest.expect(store.GetSnapshot().version).toBe 2
        )

        Vitest.test (
            "replace swaps the tracked value and notifies subscribers",
            fun () ->
                let first = makeArcFile "first"
                let second = makeArcFile "second"
                let store = MutableStore(first)
                let mutable notifications = 0
                store.Subscribe(fun () -> notifications <- notifications + 1) |> ignore

                store.Replace second

                Vitest.expect(notifications).toBe 1
                Vitest.expect(store.GetSnapshot().value).toBe second
                Vitest.expect(store.GetSnapshot().version).toBe 1
        )

        Vitest.test (
            "replace with an equal reference is a no-op",
            fun () ->
                let arcFile = makeArcFile "no-op"
                let store = MutableStore(arcFile)
                let mutable notifications = 0
                store.Subscribe(fun () -> notifications <- notifications + 1) |> ignore

                store.Replace arcFile

                Vitest.expect(notifications).toBe 0
                Vitest.expect(store.GetSnapshot().version).toBe 0
        )

        Vitest.test (
            "set with the same reference triggers a re-render signal",
            fun () ->
                let arcFile = makeArcFile "same-reference-set"
                let store = MutableStore(arcFile)
                let mutable notifications = 0
                store.Subscribe(fun () -> notifications <- notifications + 1) |> ignore

                store.Set arcFile

                Vitest.expect(notifications).toBe 1
                let snapshot = store.GetSnapshot()
                Vitest.expect(snapshot.value).toBe arcFile
                Vitest.expect(snapshot.version).toBe 1
        )

        Vitest.test (
            "set with a new reference replaces the tracked value",
            fun () ->
                let first = makeArcFile "set-first"
                let second = makeArcFile "set-second"
                let store = MutableStore(first)
                let mutable notifications = 0
                store.Subscribe(fun () -> notifications <- notifications + 1) |> ignore

                store.Set second

                Vitest.expect(notifications).toBe 1
                Vitest.expect(store.GetSnapshot().value).toBe second
                Vitest.expect(store.GetSnapshot().version).toBe 1
        )

        Vitest.test (
            "unsubscribe cleanup prevents further callbacks",
            fun () ->
                let store = MutableStore(makeArcFile "unsubscribe")
                let mutable notifications = 0
                let unsubscribe = store.Subscribe(fun () -> notifications <- notifications + 1)

                store.Mutate ignore
                Vitest.expect(notifications).toBe 1

                unsubscribe ()
                store.Mutate ignore
                Vitest.expect(notifications).toBe 1
        )

        Vitest.test (
            "a single mutation intent produces exactly one notification",
            fun () ->
                let store = MutableStore(makeArcFile "single-notification")
                let mutable notifications = 0
                store.Subscribe(fun () -> notifications <- notifications + 1) |> ignore

                store.Mutate(fun current ->
                    let table = current.Tables().[0]
                    table.AddColumn(CompositeHeader.Comment "A")
                    table.AddColumn(CompositeHeader.Comment "B")
                    table.AddRowsEmpty 2
                )

                Vitest.expect(notifications).toBe 1
                Vitest.expect(store.GetSnapshot().version).toBe 1
        )

        Vitest.test (
            "tracks arbitrary mutable value types",
            fun () ->
                let store = MutableStore(ResizeArray [ 1 ])
                let mutable notifications = 0
                store.Subscribe(fun () -> notifications <- notifications + 1) |> ignore

                store.Mutate(fun values -> values.Add 2)

                Vitest.expect(notifications).toBe 1
                Vitest.expect(store.GetSnapshot().value.Count).toBe 2
                Vitest.expect(store.GetSnapshot().value.[1]).toBe 2
                Vitest.expect(store.GetSnapshot().version).toBe 1
        )
)
