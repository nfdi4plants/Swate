module ProcessCoreConverterTests

open Expecto
open Swate.Components.Shared.ProvenanceGrouping.Types
open ProcessCoreProvenanceFixtures
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreConverter

let private names (sets: Map<ProvenanceSetId, ProvenanceSet>) =
    sets |> Map.toList |> List.map (fun (_, set) -> set.Name) |> List.sort

let tests =
    testList "ProcessCore converter" [
        testCase "converts sample and data endpoints"
        <| fun _ ->
            let fixture = basic ()
            let result = fromArc loadedTable fixture.Arc |> expectOk

            Expect.sequenceEqual (names result.Model.InputSets) [ "input-neutral" ] "Input sample must be projected."
            Expect.sequenceEqual (names result.Model.OutputSets) [ "output-neutral" ] "Output sample must be projected."
            Expect.equal result.Index.EndpointLocations.Count 2 "Both source endpoint locations must be indexed."

        testCase "uses positional connections when input and output counts match"
        <| fun _ ->
            let arc, _, _ = positional ()
            let result = fromArc loadedTable arc |> expectOk

            let pairs =
                result.Model.Connections
                |> Map.toList
                |> List.map (fun (_, connection) ->
                    result.Model.InputSets.[connection.InputSetId].Name,
                    result.Model.OutputSets.[connection.OutputSetId].Name
                )
                |> List.sort

            Expect.sequenceEqual
                pairs
                [ "input-one", "output-one"; "input-two", "output-two" ]
                "Equal lanes must map by position."

        testCase "uses all-to-all connections when counts differ"
        <| fun _ ->
            let arc, _, _ = allToAll ()
            let result = fromArc loadedTable arc |> expectOk
            Expect.equal result.Model.Connections.Count 2 "One input and two outputs must produce two edges."

        testCase "retains input-only and output-only sets without inventing connections"
        <| fun _ ->
            let inputArc, _, _ = inputOnly ()
            let outputArc, _, _ = outputOnly ()
            let inputResult = fromArc loadedTable inputArc |> expectOk
            let outputResult = fromArc loadedTable outputArc |> expectOk

            Expect.equal inputResult.Model.InputSets.Count 1 "Input-only process must retain its endpoint."
            Expect.isEmpty inputResult.Model.Connections "Input-only process must remain disconnected."
            Expect.equal outputResult.Model.OutputSets.Count 1 "Output-only process must retain its endpoint."
            Expect.isEmpty outputResult.Model.Connections "Output-only process must remain disconnected."
    ]
