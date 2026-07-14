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
let mkProcessFull
    (name: string)
    (executesProtocol: Recipe option)
    (inputs: IONode list)
    (outputs: IONode list)
    (parameterValues: Annotation list)
    =
    let proc = Process(name, ?executesProtocol = executesProtocol)
    inputs |> List.iter proc.AddInput
    outputs |> List.iter proc.AddOutput
    parameterValues |> List.iter proc.AddParameterValue
    proc

let mkProcess (name: string) (inputs: IONode list) (outputs: IONode list) =
    mkProcessFull name None inputs outputs []

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

let annotated () =
    let input = Sample("input-neutral")

    input.AddAdditionalProperty(
        Annotation(
            "category-neutral",
            value = "value-neutral",
            unit = "unit-neutral",
            nameTAN = "term:category",
            valueTAN = "term:value",
            unitTAN = "term:unit",
            additionalType = "CharacteristicValue"
        )
    )

    let output = Sample("output-neutral")
    output.AddAdditionalProperty(Annotation("factor-neutral", value = "level-neutral", additionalType = "FactorValue"))

    input.AddAdditionalProperty(
        Annotation("node-parameter-neutral", value = "node-parameter-value", additionalType = "ParameterValue")
    )

    output.AddAdditionalProperty(
        Annotation("node-component-neutral", value = "node-component-value", additionalType = "Component")
    )

    let parameterOne =
        Annotation("parameter-neutral", value = "5", additionalType = "ParameterValue")

    let parameterTwo =
        Annotation("parameter-neutral", value = "5", additionalType = "ParameterValue")

    let recipeComponent =
        Annotation("component-neutral", value = "device-neutral", additionalType = "Component")

    let recipe = Recipe(name = "recipe-neutral", components = [ recipeComponent ])

    let proc =
        mkProcessFull "stage-neutral" (Some recipe) [ SampleNode input ] [ SampleNode output ] [
            parameterOne
            parameterTwo
        ]

    let dataset = Dataset("dataset-neutral", processes = [ proc ])
    ARC("arc-neutral", hasPart = [ dataset ]), parameterOne, parameterTwo

let withPreviousContext () =
    let source = Sample("source-neutral")

    source.AddAdditionalProperty(
        Annotation("origin-neutral", value = "origin-value", additionalType = "CharacteristicValue")
    )

    let boundary = Sample("boundary-neutral")

    let previousParameter =
        Annotation("previous-parameter", value = "previous-value", additionalType = "ParameterValue")

    let previous =
        mkProcessFull "previous-stage" None [ SampleNode source ] [ SampleNode boundary ] [ previousParameter ]

    let current =
        mkProcess "stage-neutral" [ SampleNode boundary ] [ SampleNode(Sample("result-neutral")) ]

    let dataset = Dataset("dataset-neutral", processes = [ previous; current ])
    ARC("arc-neutral", hasPart = [ dataset ]), previousParameter

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
