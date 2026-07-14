module ProcessCoreProvenanceFixtures

open Expecto
open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes

type BasicFixture = {
    Arc: ARC
    Dataset: Dataset
    Process: Process
    Input: Sample
    Output: Sample
}

/// ProcessCore 0.0.7's .NET build throws `FailInit` when `inputs`/`outputs`/
/// `parameterValue` are supplied through the `Process` constructor (its `do`
/// block calls `this.AddInput`/etc. on a not-yet-initialized instance).
/// Constructing empty and adding afterward is the safe, working pattern.
let mkProcess (name: string) (inputs: IONode list) (outputs: IONode list) =
    let proc = Process(name)
    inputs |> List.iter proc.AddInput
    outputs |> List.iter proc.AddOutput
    proc

let basic () =
    let input = Sample("input-neutral", additionalType = "Sample")
    let output = Sample("output-neutral", additionalType = "Sample")
    let proc = mkProcess "stage-neutral" [ SampleNode input ] [ SampleNode output ]
    let dataset = Dataset("dataset-neutral", processes = [ proc ])
    let arc = ARC("arc-neutral", hasPart = [ dataset ])

    {
        Arc = arc
        Dataset = dataset
        Process = proc
        Input = input
        Output = output
    }

let processGroup (inputs: IONode list) (outputs: IONode list) =
    let proc = mkProcess "stage-neutral" inputs outputs
    let dataset = Dataset("dataset-neutral", processes = [ proc ])
    ARC("arc-neutral", hasPart = [ dataset ]), dataset, proc

let positional () =
    processGroup [
        SampleNode(Sample("input-one"))
        SampleNode(Sample("input-two"))
    ] [
        SampleNode(Sample("output-one"))
        SampleNode(Sample("output-two"))
    ]

let allToAll () =
    processGroup [ SampleNode(Sample("input-one")) ] [
        SampleNode(Sample("output-one"))
        SampleNode(Sample("output-two"))
    ]

let inputOnly () =
    processGroup [ DataNode(Data("input.dat")) ] []

let outputOnly () =
    processGroup [] [ DataNode(Data("output.dat", selector = "row=1")) ]

let loadedTable: ProcessCoreTableLocation = {
    DatasetPath = [ "arc-neutral"; "dataset-neutral" ]
    TableName = "stage-neutral"
}

let expectOk =
    function
    | Ok value -> value
    | Error error -> failtestf "Expected Ok but received %A" error

let expectError =
    function
    | Error error -> error
    | Ok value -> failtestf "Expected Error but received %A" value
