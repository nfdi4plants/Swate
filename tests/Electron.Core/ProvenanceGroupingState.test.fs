module ElectronCore.ProvenanceGroupingStateTests

open Swate.Components.Shared.ProvenanceGrouping.Fixtures
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Page.ProvenanceGrouping
open Vitest

Vitest.describe (
    "ProvenanceGrouping State",
    fun () ->
        Vitest.test (
            "Layers.ensure returns the same instance for unchanged state",
            fun () ->
                let session = sampleSession ()
                let state = State.init session
                let ensured = State.Layers.ensure session state

                Vitest.expect(obj.ReferenceEquals(ensured, state)).toBe (true)
        )

        Vitest.test (
            "Layers.ensure still prunes pair-scoped state for removed pairs",
            fun () ->
                let session = sampleSession ()
                let pair = Session.activePair session
                let header = pair.Model.PropertyValues |> Map.toList |> List.head |> snd |> _.Header

                let staleSlot = "removed-pair", ProvenanceSide.Input, State.Keys.groupingKey header

                let state = {
                    State.init session with
                        ExpandedProperties = Set.singleton staleSlot
                }

                let ensured = State.Layers.ensure session state

                Vitest.expect(obj.ReferenceEquals(ensured, state)).toBe (false)
                Vitest.expect(ensured.ExpandedProperties.Count).toBe (0)
        )
)