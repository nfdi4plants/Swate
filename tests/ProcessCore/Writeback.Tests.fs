module ProcessCoreWritebackTests

open Expecto
open ProcessCore
open ProcessCoreProvenanceFixtures
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreConverter
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreWriteback

let private propertyByName name model =
    model.PropertyValues
    |> Map.toList
    |> List.find (fun (_, value) -> value.Header.Category.Name = name)

let private update propertyId value unit session =
    Session.updatePropertyValue propertyId value unit session |> expectOk |> fst

let private createSet side header name session =
    Session.createLoadedSet
        {
            Side = side
            Header = header
            Name = name
        }
        session
    |> expectOk
    |> fst

let private connect inputId outputId session =
    Session.connectSets inputId outputId None session |> expectOk |> fst

let tests =
    testList "ProcessCore writeback" [
        testCase "updates every indexed duplicate annotation in memory"
        <| fun _ ->
            let arc, parameterOne, parameterTwo = annotated ()
            let converted = fromArc loadedTable arc |> expectOk
            let propertyId, _ = propertyByName "parameter-neutral" converted.Model

            let session =
                Session.init converted.Model
                |> update propertyId (ProvenanceValue.Integer 9) None

            let summary = writeBack converted.Index session arc |> expectOk

            Expect.equal summary.UpdatedAnnotations 2 "Both occurrences must be updated."
            Expect.equal parameterOne.Value (Some "9") "First annotation must contain the invariant integer."
            Expect.equal parameterTwo.Value (Some "9") "Second annotation must contain the invariant integer."

        testCase "writes floats with invariant culture"
        <| fun _ ->
            let arc, parameterOne, parameterTwo = annotated ()
            let converted = fromArc loadedTable arc |> expectOk
            let propertyId, _ = propertyByName "parameter-neutral" converted.Model

            let session =
                Session.init converted.Model
                |> update propertyId (ProvenanceValue.Float 1.5) None

            let originalCulture = System.Globalization.CultureInfo.CurrentCulture

            try
                System.Globalization.CultureInfo.CurrentCulture <- System.Globalization.CultureInfo("de-DE")
                writeBack converted.Index session arc |> expectOk |> ignore
                Expect.equal parameterOne.Value (Some "1.5") "Float must use an invariant decimal separator."
                Expect.equal parameterTwo.Value (Some "1.5") "Every duplicate must use invariant formatting."
            finally
                System.Globalization.CultureInfo.CurrentCulture <- originalCulture

        testCase "writes term and unit accessions and clears them for text"
        <| fun _ ->
            let arc, _, _ = annotated ()
            let converted = fromArc loadedTable arc |> expectOk
            let propertyId, _ = propertyByName "category-neutral" converted.Model

            let term = {
                Name = "changed-neutral"
                TermSource = None
                TermAccession = Some "term:changed"
            }

            let unit = {
                Name = "changed-unit"
                TermSource = None
                TermAccession = Some "term:changed-unit"
            }

            let first =
                Session.init converted.Model
                |> update propertyId (ProvenanceValue.Term term) (Some unit)

            writeBack converted.Index first arc |> expectOk |> ignore
            let reconverted = fromArc loadedTable arc |> expectOk
            let nextId, _ = propertyByName "category-neutral" reconverted.Model

            let second =
                Session.init reconverted.Model
                |> update nextId (ProvenanceValue.Text "plain-neutral") None

            writeBack reconverted.Index second arc |> expectOk |> ignore

            let annotation = arc.AllProcesses().[0].Inputs.[0].AsSample().AdditionalProperty.[0]
            Expect.equal annotation.Value (Some "plain-neutral") "Text value must be written."
            Expect.isNone annotation.ValueTAN "Text write must clear the value accession."
            Expect.isNone annotation.Unit "Removing the unit must clear its text."
            Expect.isNone annotation.UnitTAN "Removing the unit must clear its accession."

        testCase "updates an upstream value at its original location"
        <| fun _ ->
            let arc, previousAnnotation = withPreviousContext ()
            let converted = fromArc loadedTable arc |> expectOk
            let propertyId, _ = propertyByName "previous-parameter" converted.Model

            let session =
                Session.init converted.Model
                |> update propertyId (ProvenanceValue.Text "changed-upstream") None

            writeBack converted.Index session arc |> expectOk |> ignore

            Expect.equal
                previousAnnotation.Value
                (Some "changed-upstream")
                "Writer must mutate the upstream annotation."

        testCase "adds a disconnected set as a one-sided ProcessCore process"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk

            let session =
                Session.init converted.Model
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "added-output"

            let summary = writeBack converted.Index session fixture.Arc |> expectOk

            let added =
                fixture.Dataset.Processes
                |> Seq.find (fun proc -> proc.Outputs |> Seq.exists (fun node -> node.Key() = "M:added-output"))

            Expect.equal added.Name "stage-neutral" "Added set must remain in the loaded logical group."
            Expect.isEmpty added.Inputs "Disconnected output must use a one-sided process."
            Expect.equal summary.AddedProcesses 1 "One one-sided process must be added."

        testCase "adds a connection without leaving added sets as placeholder rows"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let initial = Session.init converted.Model

            let withInput =
                createSet
                    ProvenanceSide.Input
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "added-input"
                    initial

            let withBoth =
                createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "added-output"
                    withInput

            let layer = Session.activeLayer withBoth

            let inputId =
                layer.Model.InputSets
                |> Map.toList
                |> List.find (fun (_, set) -> set.Name = "added-input")
                |> fst

            let outputId =
                layer.Model.OutputSets
                |> Map.toList
                |> List.find (fun (_, set) -> set.Name = "added-output")
                |> fst

            let finalSession = connect inputId outputId withBoth

            writeBack converted.Index finalSession fixture.Arc |> expectOk |> ignore

            let matching =
                fixture.Dataset.Processes
                |> Seq.filter (fun proc ->
                    proc.Inputs |> Seq.exists (fun node -> node.Key() = "M:added-input")
                    || proc.Outputs |> Seq.exists (fun node -> node.Key() = "M:added-output")
                )
                |> Seq.toList

            Expect.equal matching.Length 1 "The final connected pair must not leave one-sided placeholders."
            Expect.equal matching.Head.Inputs.Count 1 "Connection process must have one input."
            Expect.equal matching.Head.Outputs.Count 1 "Connection process must have one output."

        testCase "removes one all-to-all edge while preserving both endpoint sets"
        <| fun _ ->
            let arc, dataset, _ = allToAll ()
            let converted = fromArc loadedTable arc |> expectOk
            let connectionId = converted.Model.Connections |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> Session.removeConnection connectionId
                |> expectOk
                |> fst

            writeBack converted.Index session arc |> expectOk |> ignore
            let reconverted = fromArc loadedTable arc |> expectOk
            Expect.equal reconverted.Model.InputSets.Count 1 "Removed edge must not remove its input set."
            Expect.equal reconverted.Model.OutputSets.Count 2 "Removed edge must not remove either output set."
            Expect.equal reconverted.Model.Connections.Count 1 "Exactly one all-to-all edge must remain."

        testCase "consumes a loaded-layer connection that was added and then removed"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let inputId = converted.Model.InputSets |> Map.toList |> List.head |> fst

            let withOutput =
                Session.init converted.Model
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "transient-output"

            let outputId =
                (Session.activeLayer withOutput).Model.OutputSets
                |> Map.toList
                |> List.find (fun (_, set) -> set.Name = "transient-output")
                |> fst

            let connected = connect inputId outputId withOutput

            let connectionId =
                (Session.activeLayer connected).Model.Connections
                |> Map.toList
                |> List.find (fun (_, connection) -> connection.OutputSetId = outputId)
                |> fst

            let finalSession =
                Session.removeConnection connectionId connected |> expectOk |> fst

            writeBack converted.Index finalSession fixture.Arc |> expectOk |> ignore
            let reconverted = fromArc loadedTable fixture.Arc |> expectOk

            let transient =
                reconverted.Model.OutputSets
                |> Map.toList
                |> List.map snd
                |> List.find (fun set -> set.Name = "transient-output")

            Expect.isFalse
                (reconverted.Model.Connections
                 |> Map.exists (fun _ connection ->
                     reconverted.Model.OutputSets.[connection.OutputSetId].Name = "transient-output"
                 ))
                "The added-then-removed connection must not materialize."

            Expect.equal transient.Name "transient-output" "The transient set must remain as a disconnected endpoint."

        testCase "rejects two loaded sets that materialize to one node with conflicting headers"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk

            let session =
                Session.init converted.Model
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "conflicted-name"
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Aliquot"
                    }
                    "conflicted-name"

            let beforeCount = fixture.Dataset.Processes.Count
            let errors = writeBack converted.Index session fixture.Arc |> expectError

            Expect.isTrue
                (errors
                 |> List.exists (
                     function
                     | ProcessCoreWritebackError.ConflictingNodeIdentity _ -> true
                     | _ -> false
                 ))
                "Distinct sets sharing one node identity must fail validation."

            Expect.equal fixture.Dataset.Processes.Count beforeCount "Conflicting identity must not mutate the graph."

        testCase "round-trips a sample set name containing a hash"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk

            let session =
                Session.init converted.Model
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "sample#name"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore
            let reconverted = fromArc loadedTable fixture.Arc |> expectOk

            Expect.isTrue
                (reconverted.Model.OutputSets
                 |> Map.exists (fun _ set -> set.Name = "sample#name"))
                "A hash is an opaque part of a ProcessCore sample name."

        testCase "creates a Data endpoint by splitting the final hash"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk

            let session =
                Session.init converted.Model
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.dataEndpoint
                        Text = "Data"
                    }
                    "file#section#row-neutral"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            let added =
                fixture.Dataset.Processes
                |> Seq.collect (fun proc -> proc.Outputs)
                |> Seq.choose (
                    function
                    | DataNode data -> Some data
                    | _ -> None
                )
                |> Seq.find (fun data -> data.Path = "file#section")

            Expect.equal added.Selector (Some "row-neutral") "The final hash suffix must become the data selector."

            let reconverted = fromArc loadedTable fixture.Arc |> expectOk

            Expect.isTrue
                (reconverted.Model.OutputSets
                 |> Map.exists (fun _ set -> set.Name = "file#section#row-neutral"))
                "The data path and selector must reconvert to the editor name."

        testCase "rejects an unsupported endpoint kind before mutation"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk

            let session =
                Session.init converted.Model
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProvenanceKind.create "process-core:endpoint:unsupported" "Unsupported"
                        Text = "Unsupported"
                    }
                    "unsupported-neutral"

            let beforeCount = fixture.Dataset.Processes.Count
            let errors = writeBack converted.Index session fixture.Arc |> expectError

            Expect.isTrue
                (errors
                 |> List.exists (
                     function
                     | ProcessCoreWritebackError.UnsupportedEndpointKind "process-core:endpoint:unsupported" -> true
                     | _ -> false
                 ))
                "Unsupported endpoint kinds must return a typed error."

            Expect.equal fixture.Dataset.Processes.Count beforeCount "Unsupported endpoint validation must be atomic."

        testCase "replays generated set IDs in numeric ordinal order"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let names = [ 1..11 ] |> List.map (fun ordinal -> $"ordered-set-{ordinal}")

            let session =
                names
                |> List.fold
                    (fun state name ->
                        createSet
                            ProvenanceSide.Output
                            {
                                Kind = ProcessCoreKinds.sampleEndpoint
                                Text = "Sample"
                            }
                            name
                            state
                    )
                    (Session.init converted.Model)

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            let addedNames =
                fixture.Dataset.Processes
                |> Seq.skip 1
                |> Seq.map (fun proc -> proc.Outputs.[0].AsSample().Name)
                |> Seq.toList

            Expect.sequenceEqual
                addedNames
                names
                "Generated ordinals must be parsed numerically; lexical order would place 10 before 2."

        testCase "preserves parameters and components when a removed edge splits a process"
        <| fun _ ->
            let input = Sample("split-input")
            let outputOne = Sample("split-output-one")
            let outputTwo = Sample("split-output-two")

            let parameter =
                Annotation("split-parameter", value = "parameter-value", additionalType = "ParameterValue")

            let recipeComponent =
                Annotation("split-component", value = "component-value", additionalType = "Component")

            let recipe = Recipe(name = "split-recipe", components = [ recipeComponent ])

            let proc =
                mkProcessFull "stage-neutral" (Some recipe) [ SampleNode input ] [
                    SampleNode outputOne
                    SampleNode outputTwo
                ] [ parameter ]

            let dataset = Dataset("dataset-neutral", processes = [ proc ])
            let arc = ARC("arc-neutral", hasPart = [ dataset ])
            let converted = fromArc loadedTable arc |> expectOk

            let removedId =
                converted.Model.Connections
                |> Map.toList
                |> List.find (fun (_, connection) ->
                    converted.Model.OutputSets.[connection.OutputSetId].Name = "split-output-one"
                )
                |> fst

            let finalSession =
                Session.init converted.Model
                |> Session.removeConnection removedId
                |> expectOk
                |> fst

            writeBack converted.Index finalSession arc |> expectOk |> ignore
            let reconverted = fromArc loadedTable arc |> expectOk

            let namesForSet set =
                ProvenanceSet.effectivePropertyValueIds set
                |> List.choose (fun propertyId -> reconverted.Model.PropertyValues.TryFind propertyId)
                |> List.map (fun property -> property.Header.Category.Name)

            let disconnectedOutput =
                reconverted.Model.OutputSets
                |> Map.toList
                |> List.map snd
                |> List.find (fun set -> set.Name = "split-output-one")

            Expect.contains
                (namesForSet disconnectedOutput)
                "split-parameter"
                "Disconnected replacement must retain the parameter."

            Expect.contains
                (namesForSet disconnectedOutput)
                "split-component"
                "Disconnected replacement must retain the component."
    ]
