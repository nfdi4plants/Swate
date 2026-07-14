module ProcessCoreAdapterContractTests

open Expecto
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes

let tests =
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
