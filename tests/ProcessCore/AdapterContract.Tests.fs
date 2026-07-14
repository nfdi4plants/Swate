module ProcessCoreAdapterContractTests

open Expecto
open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes
open ProcessCoreProvenanceFixtures
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreConverter

let private contractTests =
    testList "ProcessCore adapter contract" [
        testCase "exposes source-specific endpoint and property kinds"
        <| fun _ ->
            Expect.equal ProcessCoreKinds.sampleEndpoint.Id "process-core:endpoint:sample" "Sample kind must be stable."

            Expect.equal
                ProcessCoreKinds.parameter.Id
                "process-core:property:parameter"
                "Parameter kind must be stable."

            Expect.equal
                ProcessCoreKinds.componentKind.Id
                "process-core:property:component"
                "Component kind must be stable without using a reserved F# identifier."

        testCase "represents selection as a dataset path and process-group name"
        <| fun _ ->
            let location = {
                DatasetPath = [ "arc-neutral"; "dataset-neutral" ]
                TableName = "stage-neutral"
            }

            Expect.sequenceEqual
                location.DatasetPath
                [ "arc-neutral"; "dataset-neutral" ]
                "Dataset path must retain order."

            Expect.equal location.TableName "stage-neutral" "Logical table name must be retained."
    ]

let private selectionTests =
    testList "selection" [
        testCase "selects an exact dataset path and process group"
        <| fun _ ->
            let fixture = basic ()
            let result = fromArc loadedTable fixture.Arc |> expectOk

            Expect.equal result.Model.Source.Name "stage-neutral" "Selected group name must become the source name."
            Expect.equal result.Index.LoadedTable loadedTable "The exact selector must be retained."
            Expect.isNotEmpty result.Index.ArcFingerprint "The source graph must be fingerprinted."

        testCase "returns a typed error for a missing dataset"
        <| fun _ ->
            let fixture = basic ()

            let missing = {
                loadedTable with
                    DatasetPath = [ "arc-neutral"; "missing-neutral" ]
            }

            match fromArc missing fixture.Arc |> expectError with
            | ProcessCoreConversionError.DatasetNotFound path ->
                Expect.sequenceEqual path missing.DatasetPath "Error must retain the requested path."
            | other -> failtestf "Expected DatasetNotFound but received %A" other

        testCase "returns a typed error for an ambiguous dataset path"
        <| fun _ ->
            let first =
                Dataset(
                    "dataset-neutral",
                    processes = [
                        mkProcess "stage-neutral" [ SampleNode(Sample("ambiguous-input")) ] []
                    ]
                )

            let second = Dataset("dataset-shadow")
            let arc = ARC("arc-neutral", hasPart = [ first; second ])
            // AddPart deduplicates equal identifiers, so create a valid graph first and
            // then model a corrupted in-memory graph with an ambiguous path.
            second.Identifier <- "dataset-neutral"

            Expect.equal
                (fromArc loadedTable arc |> expectError)
                (ProcessCoreConversionError.AmbiguousDatasetPath loadedTable.DatasetPath)
                "Duplicate sibling dataset identifiers must fail conversion instead of first-match-wins."

        testCase "returns a typed error for a missing process group"
        <| fun _ ->
            let fixture = basic ()

            let missing = {
                loadedTable with
                    TableName = "missing-stage"
            }

            Expect.equal
                (fromArc missing fixture.Arc |> expectError)
                (ProcessCoreConversionError.ProcessGroupNotFound missing)
                "A dataset without the selected group must fail."

        testCase "produces stable source identity for an unchanged graph"
        <| fun _ ->
            let fixture = basic ()
            let first = fromArc loadedTable fixture.Arc |> expectOk
            let second = fromArc loadedTable fixture.Arc |> expectOk

            Expect.equal first.Model.Source second.Model.Source "Source identity must be deterministic."
            Expect.equal first.Index.ArcFingerprint second.Index.ArcFingerprint "Fingerprint must be deterministic."
    ]

let tests = testList "ProcessCore adapter" [ contractTests; selectionTests ]
