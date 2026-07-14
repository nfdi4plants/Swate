module ProcessCoreConverterTests

open Expecto
open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes
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

        testCase "converts node, parameter, and component annotations"
        <| fun _ ->
            let arc, _, _ = annotated ()
            let result = fromArc loadedTable arc |> expectOk

            let kinds =
                result.Model.PropertyValues
                |> Map.toList
                |> List.map (fun (_, property) -> property.Header.Kind.Id)
                |> Set.ofList

            Expect.isTrue (kinds.Contains ProcessCoreKinds.characteristic.Id) "Input characteristic must be converted."
            Expect.isTrue (kinds.Contains ProcessCoreKinds.factor.Id) "Output factor must be converted."
            Expect.isTrue (kinds.Contains ProcessCoreKinds.parameter.Id) "Process parameter must be converted."
            Expect.isTrue (kinds.Contains ProcessCoreKinds.componentKind.Id) "Recipe component must be converted."

        testCase "preserves exact sides for node parameter and component annotations"
        <| fun _ ->
            let arc, _, _ = annotated ()
            let result = fromArc loadedTable arc |> expectOk

            let propertyId name =
                result.Model.PropertyValues
                |> Map.toList
                |> List.find (fun (_, property) -> property.Header.Category.Name = name)
                |> fst

            let parameterId = propertyId "node-parameter-neutral"
            let componentId = propertyId "node-component-neutral"
            let input = result.Model.InputSets |> Map.toList |> List.head |> snd
            let output = result.Model.OutputSets |> Map.toList |> List.head |> snd

            Expect.contains input.PropertyValueIds parameterId "A node parameter must remain on its input side."

            Expect.isFalse
                (output.PropertyValueIds |> List.contains parameterId)
                "A node parameter must not spread to the output side."

            Expect.contains output.PropertyValueIds componentId "A node component must remain on its output side."

            Expect.isFalse
                (input.PropertyValueIds |> List.contains componentId)
                "A node component must not spread to the input side."

        testCase "preserves annotation term and unit accessions"
        <| fun _ ->
            let arc, _, _ = annotated ()
            let result = fromArc loadedTable arc |> expectOk

            let property =
                result.Model.PropertyValues
                |> Map.toList
                |> List.map snd
                |> List.find (fun value -> value.Header.Category.Name = "category-neutral")

            Expect.equal
                property.Header.Category.TermAccession
                (Some "term:category")
                "Category accession must round-trip."

            Expect.isNone property.Header.Category.TermSource "ProcessCore does not supply a category term source."
            Expect.equal property.Unit.Value.TermAccession (Some "term:unit") "Unit accession must round-trip."
            Expect.isNone property.Unit.Value.TermSource "ProcessCore does not supply a unit term source."

            match property.Value with
            | ProvenanceValue.Term term ->
                Expect.equal term.TermAccession (Some "term:value") "Value accession must round-trip."
                Expect.isNone term.TermSource "ProcessCore does not supply a value term source."
            | other -> failtestf "Expected term value but received %A" other

        testCase "collapses exact duplicate values but indexes every annotation occurrence"
        <| fun _ ->
            let arc, _, _ = annotated ()
            let result = fromArc loadedTable arc |> expectOk

            let parameter =
                result.Model.PropertyValues
                |> Map.toList
                |> List.find (fun (_, value) -> value.Header.Category.Name = "parameter-neutral")

            Expect.equal
                result.Index.PropertyValueLocations.[fst parameter].Length
                2
                "Both duplicate source occurrences must be retained."

        testCase "always includes reachable previous properties with their real source"
        <| fun _ ->
            let arc, _ = withPreviousContext ()
            let result = fromArc loadedTable arc |> expectOk

            let previous =
                result.Model.PropertyValues
                |> Map.toList
                |> List.map snd
                |> List.find (fun value -> value.Header.Category.Name = "previous-parameter")

            match previous.Origin with
            | ProvenancePropertyOrigin.Real anchor ->
                Expect.equal anchor.Source.Name "previous-stage" "Upstream property must retain its logical source."
                Expect.notEqual anchor.Source.Id result.Model.Source.Id "Upstream source must not become current."
            | other -> failtestf "Expected a real upstream origin but received %A" other

        testCase "warns for process properties that have no projectable endpoint"
        <| fun _ ->
            let orphanParameter =
                Annotation("orphan-parameter", value = "parameter-value", additionalType = "ParameterValue")

            let orphanComponent =
                Annotation("orphan-component", value = "component-value", additionalType = "Component")

            let recipe = Recipe(name = "orphan-recipe", components = [ orphanComponent ])
            let proc = mkProcessFull "stage-neutral" (Some recipe) [] [] [ orphanParameter ]
            let dataset = Dataset("dataset-neutral", processes = [ proc ])
            let arc = ARC("arc-neutral", hasPart = [ dataset ])
            let result = fromArc loadedTable arc |> expectOk

            let warnedNames =
                result.Warnings
                |> List.choose (
                    function
                    | ProcessCoreConversionWarning.PropertyWithoutEndpoint(_, name) -> Some name
                    | _ -> None
                )
                |> List.sort

            Expect.sequenceEqual
                warnedNames
                [ "orphan-component"; "orphan-parameter" ]
                "Every unattached process property must produce a warning."

            Expect.isEmpty
                result.Model.PropertyValues
                "A property with no target set must not become an unanchored editor value."
    ]
