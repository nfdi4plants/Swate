module ElectronCore.ArcMerge.ShortCircuitTests

open System
open ARCtrl
open Main.ArcMerge
open Vitest

Vitest.describe("short-circuit", fun () ->
    Vitest.test("clean local: returns copy of remote", fun () ->
        let arcLocal, arcRemote = MockData.createTwoCleanCopies ()
        arcRemote.Title <- Some "Remote Title"

        let merged = ARC.merge arcLocal arcRemote []

        Vitest.expect(Object.ReferenceEquals(merged, arcRemote)).toBe(false)
        Vitest.expect(merged.Title).toEqual(Some "Remote Title")

        merged.Title <- Some "Merged Title"
        Vitest.expect(arcRemote.Title).toEqual(Some "Remote Title"))

    Vitest.test("dirty local with empty event list: returns copy of local", fun () ->
        let arcLocal, arcRemote = MockData.createTwoCleanCopies ()
        arcLocal.Title <- Some "User Title"
        arcRemote.Title <- Some "Disc Title"

        let merged = ARC.merge arcLocal arcRemote []

        Vitest.expect(Object.ReferenceEquals(merged, arcLocal)).toBe(false)
        Vitest.expect(merged.Title).toEqual(Some "User Title")

        merged.Title <- Some "Merged Title"
        Vitest.expect(arcLocal.Title).toEqual(Some "User Title"))

    Vitest.test("clean local with events: returns copy of remote, events are ignored", fun () ->
        let arcLocal, arcRemote = MockData.createTwoCleanCopies ()
        arcRemote.Title <- Some "Disc Title"

        let event = {
            EventName = EventName.Change
            Path = "assays/My Assay/isa.assay.xlsx"
        }

        let merged = ARC.merge arcLocal arcRemote [ event ]

        Vitest.expect(merged.Title).toEqual(Some "Disc Title")
        Vitest.expect(merged.AssayCount).toBe(1))

    Vitest.test("dirty local with events: processes events without mutating inputs", fun () ->
        let arcLocal, arcRemote = MockData.createTwoCleanCopies ()
        arcLocal.Title <- Some "User Title"
        arcRemote.Title <- Some "Disc Title"

        let event = {
            EventName = EventName.Change
            Path = "isa.investigation.xlsx"
        }

        let merged = ARC.merge arcLocal arcRemote [ event ]

        Vitest.expect(Object.ReferenceEquals(merged, arcLocal)).toBe(false)
        Vitest.expect(merged.Title).toEqual(Some "Disc Title")
        Vitest.expect(arcLocal.Title).toEqual(Some "User Title")
        Vitest.expect(arcRemote.Title).toEqual(Some "Disc Title")))
