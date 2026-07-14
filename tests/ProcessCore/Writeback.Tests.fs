module ProcessCoreWritebackTests

open Expecto
open ProcessCore
open ProcessCoreProvenanceFixtures
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Edit
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

let private addLayer name selectedSets session =
    Session.addLayer
        {
            Name = name
            SelectedSets = selectedSets
        }
        session
    |> expectOk
    |> fst

let private createProperty target kind category value session =
    Session.createLoadedPropertyValue
        {
            Target = target
            CopiedFrom = None
            Header = {
                Kind = kind
                Category = {
                    Name = category
                    TermSource = None
                    TermAccession = None
                }
            }
            Value = ProvenanceValue.Text value
            Unit = None
        }
        session
    |> expectOk
    |> fst

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

        testCase "stores an explicit characteristic on an output node"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let outputId = converted.Model.OutputSets |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.OutputSets [ outputId ])
                    ProcessCoreKinds.characteristic
                    "unfinished-characteristic"
                    "value-neutral"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            Expect.isTrue
                (fixture.Process.Outputs.[0].AsSample().AdditionalProperty
                 |> Seq.exists (fun annotation ->
                     annotation.Name = "unfinished-characteristic"
                     && annotation.AdditionalType = Some "CharacteristicValue"
                 ))
                "Explicit output placement must be retained."

        testCase "stores an explicit factor on an input node"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let inputId = converted.Model.InputSets |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.InputSets [ inputId ])
                    ProcessCoreKinds.factor
                    "unfinished-factor"
                    "level-neutral"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            Expect.isTrue
                (fixture.Process.Inputs.[0].AsSample().AdditionalProperty
                 |> Seq.exists (fun annotation ->
                     annotation.Name = "unfinished-factor"
                     && annotation.AdditionalType = Some "FactorValue"
                 ))
                "Explicit input placement must be retained."

        testCase "stores a set-targeted parameter only on the exact output node"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let inputId = converted.Model.InputSets |> Map.toList |> List.head |> fst
            let outputId = converted.Model.OutputSets |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.OutputSets [ outputId ])
                    ProcessCoreKinds.parameter
                    "set-parameter"
                    "parameter-value"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            Expect.isTrue
                (fixture.Process.Outputs.[0].AsSample().AdditionalProperty
                 |> Seq.exists (fun annotation ->
                     annotation.Name = "set-parameter"
                     && annotation.AdditionalType = Some "ParameterValue"
                 ))
                "A set-targeted parameter must be stored on the selected node."

            Expect.isFalse
                (fixture.Process.ParameterValue
                 |> Seq.exists (fun annotation -> annotation.Name = "set-parameter"))
                "A set-targeted parameter must not spread through the process."

            let reconverted = fromArc loadedTable fixture.Arc |> expectOk
            let propertyId, _ = propertyByName "set-parameter" reconverted.Model

            Expect.contains
                reconverted.Model.OutputSets.[outputId].PropertyValueIds
                propertyId
                "The parameter must reconvert on the selected output."

            Expect.isFalse
                (reconverted.Model.InputSets.[inputId].PropertyValueIds
                 |> List.contains propertyId)
                "The parameter must not reconvert on the input."

        testCase "stores a set-targeted component only on the exact input node"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let inputId = converted.Model.InputSets |> Map.toList |> List.head |> fst
            let outputId = converted.Model.OutputSets |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.InputSets [ inputId ])
                    ProcessCoreKinds.componentKind
                    "set-component"
                    "component-value"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            Expect.isTrue
                (fixture.Process.Inputs.[0].AsSample().AdditionalProperty
                 |> Seq.exists (fun annotation ->
                     annotation.Name = "set-component"
                     && annotation.AdditionalType = Some "Component"
                 ))
                "A set-targeted component must be stored on the selected node."

            Expect.isTrue
                (fixture.Process.ExecutesProtocol.IsNone
                 || (fixture.Process.ExecutesProtocol.Value.Components
                     |> Seq.forall (fun annotation -> annotation.Name <> "set-component")))
                "A set-targeted component must not spread through a recipe."

            let reconverted = fromArc loadedTable fixture.Arc |> expectOk
            let propertyId, _ = propertyByName "set-component" reconverted.Model

            Expect.contains
                reconverted.Model.InputSets.[inputId].PropertyValueIds
                propertyId
                "The component must reconvert on the selected input."

            Expect.isFalse
                (reconverted.Model.OutputSets.[outputId].PropertyValueIds
                 |> List.contains propertyId)
                "The component must not reconvert on the output."

        testCase "stores connection-targeted node properties on both endpoints"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let connectionId = converted.Model.Connections |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.Connections [ connectionId ])
                    ProcessCoreKinds.additionalProperty
                    "edge-note"
                    "note-neutral"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            Expect.isTrue
                (fixture.Process.Inputs.[0].AsSample().AdditionalProperty
                 |> Seq.exists (fun a -> a.Name = "edge-note"))
                "Input node must receive the edge property."

            Expect.isTrue
                (fixture.Process.Outputs.[0].AsSample().AdditionalProperty
                 |> Seq.exists (fun a -> a.Name = "edge-note"))
                "Output node must receive the edge property."

        testCase "stores a connection parameter only on its exact process"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let connectionId = converted.Model.Connections |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.Connections [ connectionId ])
                    ProcessCoreKinds.parameter
                    "edge-parameter"
                    "parameter-value"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            Expect.isTrue
                (fixture.Process.ParameterValue
                 |> Seq.exists (fun annotation -> annotation.Name = "edge-parameter"))
                "The exact connection process must receive the parameter."

        testCase "creates a recipe component for an exact connection"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let connectionId = converted.Model.Connections |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.Connections [ connectionId ])
                    ProcessCoreKinds.componentKind
                    "edge-component"
                    "component-value"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            Expect.isTrue
                fixture.Process.ExecutesProtocol.IsSome
                "A recipe must be created for the connection component."

            Expect.isTrue
                (fixture.Process.ExecutesProtocol.Value.Components
                 |> Seq.exists (fun annotation -> annotation.Name = "edge-component"))
                "The exact connection recipe must receive the component."

        testCase "writes the final value of a property that was added and then updated"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let outputId = converted.Model.OutputSets |> Map.toList |> List.head |> fst

            let withProperty =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.OutputSets [ outputId ])
                    ProcessCoreKinds.factor
                    "final-factor"
                    "initial-value"

            let propertyId, _ =
                propertyByName "final-factor" (Session.activeLayer withProperty).Model

            let finalSession =
                update propertyId (ProvenanceValue.Text "final-value") None withProperty

            writeBack converted.Index finalSession fixture.Arc |> expectOk |> ignore

            let written =
                fixture.Process.Outputs.[0].AsSample().AdditionalProperty
                |> Seq.find (fun annotation -> annotation.Name = "final-factor")

            Expect.equal written.Value (Some "final-value") "Final session state must override the add-patch payload."

        testCase "replays same-category property IDs in numeric ordinal order"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let inputId = converted.Model.InputSets |> Map.toList |> List.head |> fst
            let values = [ 1..11 ] |> List.map (fun ordinal -> $"value-{ordinal}")

            let session =
                values
                |> List.fold
                    (fun state value ->
                        createProperty
                            (ProvenancePropertyTarget.InputSets [ inputId ])
                            ProcessCoreKinds.characteristic
                            "duplicate-category"
                            value
                            state
                    )
                    (Session.init converted.Model)

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            let written =
                fixture.Process.Inputs.[0].AsSample().AdditionalProperty
                |> Seq.filter (fun annotation -> annotation.Name = "duplicate-category")
                |> Seq.map (fun annotation -> annotation.Value)
                |> Seq.toList

            Expect.sequenceEqual
                written
                (values |> List.map Some)
                "Generated ordinals must be parsed numerically; lexical order would place 10 before 2."

        testCase "rejects a recipe-component collision that differs only by value accession"
        <| fun _ ->
            let fixture = basic ()

            let existing =
                Annotation(
                    "collision-category",
                    value = "collision-value",
                    valueTAN = "term:existing",
                    additionalType = "Component"
                )

            let recipe = Recipe(name = "collision-recipe", components = [ existing ])
            fixture.Process.ExecutesProtocol <- Some recipe
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let connectionId = converted.Model.Connections |> Map.toList |> List.head |> fst

            let session =
                Session.createLoadedPropertyValue
                    {
                        Target = ProvenancePropertyTarget.Connections [ connectionId ]
                        CopiedFrom = None
                        Header = {
                            Kind = ProcessCoreKinds.componentKind
                            Category = {
                                Name = "collision-category"
                                TermSource = None
                                TermAccession = None
                            }
                        }
                        Value =
                            ProvenanceValue.Term {
                                Name = "collision-value"
                                TermSource = None
                                TermAccession = Some "term:requested"
                            }
                        Unit = None
                    }
                    (Session.init converted.Model)
                |> expectOk
                |> fst

            let beforeCount = recipe.Components.Count
            let errors = writeBack converted.Index session fixture.Arc |> expectError

            Expect.isTrue
                (errors
                 |> List.exists (
                     function
                     | ProcessCoreWritebackError.ConflictingAnnotationIdentity _ -> true
                     | _ -> false
                 ))
                "A narrower ProcessCore equality collision must fail preflight."

            Expect.equal recipe.Components.Count beforeCount "A collision must not partially add a component."

            Expect.equal
                existing.ValueTAN
                (Some "term:existing")
                "A collision must leave the existing annotation unchanged."

        testCase "rejects a node annotation collision that differs only by discriminator"
        <| fun _ ->
            let fixture = basic ()
            let output = fixture.Process.Outputs.[0].AsSample()

            output.AddAdditionalProperty(
                Annotation("kind-collision", value = "same-value", additionalType = "CharacteristicValue")
            )

            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let outputId = converted.Model.OutputSets |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.OutputSets [ outputId ])
                    ProcessCoreKinds.factor
                    "kind-collision"
                    "same-value"

            let beforeCount = output.AdditionalProperty.Count
            let errors = writeBack converted.Index session fixture.Arc |> expectError

            Expect.isTrue
                (errors
                 |> List.exists (
                     function
                     | ProcessCoreWritebackError.ConflictingAnnotationIdentity _ -> true
                     | _ -> false
                 ))
                "Different property kinds must not be silently deduplicated."

            Expect.equal output.AdditionalProperty.Count beforeCount "A discriminator collision must be atomic."

        testCase "copies an upstream property into the current group without changing its original"
        <| fun _ ->
            let arc, upstreamAnnotation = withPreviousContext ()
            let converted = fromArc loadedTable arc |> expectOk
            let previousId, _ = propertyByName "previous-parameter" converted.Model
            let inputId = converted.Model.InputSets |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> Session.copyPropertyValueToLoadedTarget previousId (ProvenancePropertyTarget.InputSets [ inputId ])
                |> expectOk
                |> fst

            writeBack converted.Index session arc |> expectOk |> ignore

            let current =
                arc.AllProcesses() |> Seq.find (fun proc -> proc.Name = "stage-neutral")

            Expect.equal
                upstreamAnnotation.Value
                (Some "previous-value")
                "Copying must not mutate the upstream annotation."

            Expect.isTrue
                (current.Inputs.[0].AsSample().AdditionalProperty
                 |> Seq.exists (fun annotation ->
                     annotation.Name = "previous-parameter"
                     && annotation.Value = Some "previous-value"
                     && annotation.AdditionalType = Some "ParameterValue"
                 ))
                "A parameter copied to an input set must be stored on that exact node."

        testCase "adds multiple new logical groups to the selected dataset in layer order"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let first = Session.init converted.Model |> addLayer "new-stage-one" []
            let second = first |> addLayer "new-stage-two" []

            let summary = writeBack converted.Index second fixture.Arc |> expectOk

            let addedNames =
                fixture.Dataset.Processes
                |> Seq.map (fun proc -> proc.Name)
                |> Seq.filter (fun name -> name.StartsWith("new-stage-"))
                |> Seq.toList

            Expect.sequenceEqual
                addedNames
                [ "new-stage-one"; "new-stage-two" ]
                "Layer order must determine process-group order."

            Expect.isGreaterThanOrEqual summary.AddedProcesses 2 "Each new layer must materialize."

        testCase "reuses a reference-linked canonical node"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let outputId = converted.Model.OutputSets |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> addLayer "new-stage" [ ProvenanceSide.Output, outputId ]

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            let created =
                fixture.Dataset.Processes |> Seq.find (fun proc -> proc.Name = "new-stage")

            Expect.isTrue
                (obj.ReferenceEquals(created.Inputs.[0].AsSample(), fixture.Process.Outputs.[0].AsSample()))
                "Reference link must resolve to the same canonical node object."

        testCase "retains an empty new layer as an empty process"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let session = Session.init converted.Model |> addLayer "empty-stage" []
            let emptyLayer = Session.activeLayer session

            let session = {
                session with
                    Layers =
                        session.Layers
                        |> List.map (fun layer ->
                            if layer.Id = emptyLayer.Id then
                                {
                                    layer with
                                        Model = {
                                            layer.Model with
                                                InputSets = Map.empty
                                        }
                                }
                            else
                                layer
                        )
                    ReferenceLinks = []
            }

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore

            let created =
                fixture.Dataset.Processes |> Seq.find (fun proc -> proc.Name = "empty-stage")

            Expect.isEmpty created.Inputs "Empty layer must have no inputs."
            Expect.isEmpty created.Outputs "Empty layer must have no outputs."

        testCase "materializes an added-then-removed connection only as disconnected endpoints"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let withLayer = Session.init converted.Model |> addLayer "unfinished-stage" []

            let projectedInputId =
                (Session.activeLayer withLayer).Model.InputSets
                |> Map.toList
                |> List.head
                |> fst

            let withOutput =
                createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "unfinished-output"
                    withLayer

            let outputId =
                (Session.activeLayer withOutput).Model.OutputSets
                |> Map.toList
                |> List.head
                |> fst

            let connected = connect projectedInputId outputId withOutput

            let connectionId =
                (Session.activeLayer connected).Model.Connections
                |> Map.toList
                |> List.head
                |> fst

            let finalSession =
                Session.removeConnection connectionId connected |> expectOk |> fst

            writeBack converted.Index finalSession fixture.Arc |> expectOk |> ignore

            let rows =
                fixture.Dataset.Processes
                |> Seq.filter (fun proc -> proc.Name = "unfinished-stage")
                |> Seq.toList

            Expect.isTrue
                (rows
                 |> List.forall (fun proc -> proc.Inputs.Count = 0 || proc.Outputs.Count = 0))
                "Removed connection must not reappear."

        testCase "rejects a blank new layer name without mutation"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let session = Session.init converted.Model |> addLayer "   " []
            let layer = Session.activeLayer session
            let beforeCount = fixture.Dataset.Processes.Count
            let errors = writeBack converted.Index session fixture.Arc |> expectError

            Expect.contains
                errors
                (ProcessCoreWritebackError.BlankLayerName layer.Id)
                "Blank layer must fail validation."

            Expect.equal fixture.Dataset.Processes.Count beforeCount "Blank-name failure must not add a process."

        testCase "rejects a new layer name that already exists in the dataset"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let session = Session.init converted.Model |> addLayer "stage-neutral" []
            let beforeCount = fixture.Dataset.Processes.Count
            let errors = writeBack converted.Index session fixture.Arc |> expectError

            Expect.contains
                errors
                (ProcessCoreWritebackError.DuplicateLayerName "stage-neutral")
                "Existing group name must fail validation."

            Expect.equal fixture.Dataset.Processes.Count beforeCount "Duplicate-name failure must not add a process."

        testCase "rejects incompatible reference links without mutation"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let initial = Session.init converted.Model
            let initialLayer = Session.activeLayer initial
            let inputId = initialLayer.Model.InputSets |> Map.toList |> List.head |> fst
            let outputId = initialLayer.Model.OutputSets |> Map.toList |> List.head |> fst
            let withLayer = addLayer "linked-stage" [ ProvenanceSide.Output, outputId ] initial
            let targetLayer = Session.activeLayer withLayer
            let targetId = targetLayer.Model.InputSets |> Map.toList |> List.head |> fst

            let incompatible = {
                Source = {
                    LayerId = initialLayer.Id
                    Side = ProvenanceSide.Input
                    SetId = inputId
                }
                Target = {
                    LayerId = targetLayer.Id
                    Side = ProvenanceSide.Input
                    SetId = targetId
                }
            }

            let invalidSession = {
                withLayer with
                    ReferenceLinks = withLayer.ReferenceLinks @ [ incompatible ]
            }

            let beforeCount = fixture.Dataset.Processes.Count
            let errors = writeBack converted.Index invalidSession fixture.Arc |> expectError

            Expect.contains
                errors
                (ProcessCoreWritebackError.InvalidReferenceLink incompatible)
                "Conflicting node keys must fail validation."

            Expect.equal fixture.Dataset.Processes.Count beforeCount "Invalid-link failure must not add a process."

        testCase "rejects a stale source before applying any edit"
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
                    "must-not-appear"

            fixture.Process.Name <- "changed-concurrently"
            let beforeCount = fixture.Dataset.Processes.Count
            let result = writeBack converted.Index session fixture.Arc |> expectError

            Expect.contains
                result
                ProcessCoreWritebackError.StaleArc
                "Concurrent graph change must invalidate the index."

            Expect.equal fixture.Dataset.Processes.Count beforeCount "No process may be added after stale validation."

            Expect.isFalse
                (fixture.Dataset.AllSamples()
                 |> Seq.exists (fun sample -> sample.Name = "must-not-appear"))
                "No planned node may be created after stale validation."

        testCase "collects domain errors before applying valid earlier patches"
        <| fun _ ->
            let fixture = basic ()
            let converted = fromArc loadedTable fixture.Arc |> expectOk

            let withValidSet =
                Session.init converted.Model
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "must-not-appear"

            let missingPropertyId = "missing-property-neutral"

            let missingHeader = {
                Kind = ProcessCoreKinds.parameter
                Category = {
                    Name = "missing-category"
                    TermSource = None
                    TermAccession = None
                }
            }

            let missingAnchor = {
                Source = converted.Model.Source
                ProcessId = None
                ProcessName = Some converted.Model.Source.Name
                Header = missingHeader
                InputNames = []
                OutputNames = []
            }

            let invalidSession = {
                withValidSet with
                    PatchLog =
                        withValidSet.PatchLog
                        @ [
                            ProvenanceTablePatch.UpdatePropertyValue(
                                missingPropertyId,
                                missingAnchor,
                                ProvenanceValue.Text "old",
                                ProvenanceValue.Text "new",
                                None
                            )
                        ]
            }

            let beforeCount = fixture.Dataset.Processes.Count
            let errors = writeBack converted.Index invalidSession fixture.Arc |> expectError

            Expect.contains
                errors
                (ProcessCoreWritebackError.PropertyNotFound missingPropertyId)
                "Missing final property must be reported."

            Expect.equal
                fixture.Dataset.Processes.Count
                beforeCount
                "Valid earlier patches must not apply when a later patch is invalid."

            Expect.isFalse
                (fixture.Dataset.AllSamples()
                 |> Seq.exists (fun sample -> sample.Name = "must-not-appear"))
                "The valid earlier set patch must remain unapplied."

        testCase "rejects structural creation in previous context"
        <| fun _ ->
            let arc, _ = withPreviousContext ()
            let converted = fromArc loadedTable arc |> expectOk
            let _, previousProperty = propertyByName "previous-parameter" converted.Model

            let previousSource =
                match previousProperty.Origin with
                | ProvenancePropertyOrigin.Real anchor -> anchor.Source
                | other -> failtestf "Expected real previous origin but received %A" other

            let forged = {
                Session.init converted.Model with
                    PatchLog = [
                        ProvenanceTablePatch.AddLoadedSet(
                            ProvenanceSide.Output,
                            previousSource.Name,
                            {
                                Kind = ProcessCoreKinds.sampleEndpoint
                                Text = "Sample"
                            },
                            "forbidden-previous-set"
                        )
                    ]
            }

            let beforeCount = arc.AllProcesses().Count
            let errors = writeBack converted.Index forged arc |> expectError

            Expect.contains
                errors
                (ProcessCoreWritebackError.StructuralPreviousContextEdit previousSource.Id)
                "Previous context must allow value updates only, not structural creation."

            Expect.equal (arc.AllProcesses().Count) beforeCount "Rejected previous structure must not mutate the graph."

        testCase "reconverts the complete final session from the mutated ARC"
        <| fun _ ->
            let arc, _, _ = annotated ()
            let dataset = arc.HasPart.[0]
            let converted = fromArc loadedTable arc |> expectOk
            let categoryId, _ = propertyByName "category-neutral" converted.Model

            let initialConnectionId =
                converted.Model.Connections |> Map.toList |> List.head |> fst

            let afterValue =
                Session.init converted.Model
                |> update categoryId (ProvenanceValue.Text "roundtrip-value") None

            let afterRemoval =
                Session.removeConnection initialConnectionId afterValue |> expectOk |> fst

            let afterSet =
                afterRemoval
                |> createSet
                    ProvenanceSide.Output
                    {
                        Kind = ProcessCoreKinds.sampleEndpoint
                        Text = "Sample"
                    }
                    "roundtrip-output"

            let initialLayer = Session.activeLayer afterSet

            let inputId =
                initialLayer.Model.InputSets
                |> Map.toList
                |> List.find (fun (_, set) -> set.Name = "input-neutral")
                |> fst

            let originalOutputId =
                initialLayer.Model.OutputSets
                |> Map.toList
                |> List.find (fun (_, set) -> set.Name = "output-neutral")
                |> fst

            let addedOutputId =
                initialLayer.Model.OutputSets
                |> Map.toList
                |> List.find (fun (_, set) -> set.Name = "roundtrip-output")
                |> fst

            let afterConnection = connect inputId addedOutputId afterSet

            let retainedConnectionId =
                (Session.activeLayer afterConnection).Model.Connections
                |> Map.toList
                |> List.find (fun (_, connection) -> connection.OutputSetId = addedOutputId)
                |> fst

            let afterCharacteristic =
                afterConnection
                |> createProperty
                    (ProvenancePropertyTarget.OutputSets [ originalOutputId ])
                    ProcessCoreKinds.characteristic
                    "roundtrip-characteristic"
                    "roundtrip-characteristic-value"

            let afterParameter =
                afterCharacteristic
                |> createProperty
                    (ProvenancePropertyTarget.Connections [ retainedConnectionId ])
                    ProcessCoreKinds.parameter
                    "roundtrip-parameter"
                    "roundtrip-parameter-value"

            let afterComponent =
                afterParameter
                |> createProperty
                    (ProvenancePropertyTarget.Connections [ retainedConnectionId ])
                    ProcessCoreKinds.componentKind
                    "roundtrip-component"
                    "roundtrip-component-value"

            let withUnfinished =
                addLayer "roundtrip-unfinished" [ ProvenanceSide.Output, addedOutputId ] afterComponent

            let finalSession = addLayer "roundtrip-empty" [] withUnfinished

            writeBack converted.Index finalSession arc |> expectOk |> ignore

            let loadedAgain = fromArc loadedTable arc |> expectOk

            let pairs =
                loadedAgain.Model.Connections
                |> Map.toList
                |> List.map (fun (_, connection) ->
                    loadedAgain.Model.InputSets.[connection.InputSetId].Name,
                    loadedAgain.Model.OutputSets.[connection.OutputSetId].Name
                )

            Expect.contains pairs ("input-neutral", "roundtrip-output") "Retained added connection must reconvert."

            Expect.isFalse
                (pairs |> List.contains ("input-neutral", "output-neutral"))
                "Removed original connection must stay removed."

            let _, category = propertyByName "category-neutral" loadedAgain.Model
            Expect.equal category.Value (ProvenanceValue.Text "roundtrip-value") "Updated value must reconvert."
            propertyByName "roundtrip-characteristic" loadedAgain.Model |> ignore
            propertyByName "roundtrip-parameter" loadedAgain.Model |> ignore
            propertyByName "roundtrip-component" loadedAgain.Model |> ignore

            let unfinishedLocation = {
                loadedTable with
                    TableName = "roundtrip-unfinished"
            }

            let unfinished = fromArc unfinishedLocation arc |> expectOk

            Expect.sequenceEqual
                (unfinished.Model.InputSets |> Map.toList |> List.map (fun (_, set) -> set.Name))
                [ "roundtrip-output" ]
                "Reference-linked input must reconvert."

            Expect.isEmpty unfinished.Model.OutputSets "Unfinished layer must remain output-free."
            Expect.isEmpty unfinished.Model.Connections "Unfinished layer must remain disconnected."

            let emptyLocation = {
                loadedTable with
                    TableName = "roundtrip-empty"
            }

            let empty = fromArc emptyLocation arc |> expectOk
            Expect.isEmpty empty.Model.InputSets "Empty layer must remain input-free."
            Expect.isEmpty empty.Model.OutputSets "Empty layer must remain output-free."
            Expect.isEmpty empty.Model.Connections "Empty layer must remain connection-free."

            let newGroupOrder =
                dataset.Processes
                |> Seq.map (fun proc -> proc.Name)
                |> Seq.filter (fun name -> name.StartsWith("roundtrip-"))
                |> Seq.distinct
                |> Seq.toList

            Expect.sequenceEqual
                newGroupOrder
                [ "roundtrip-unfinished"; "roundtrip-empty" ]
                "New groups must be appended in session layer order."

        testCase "does not persist through the ARC path"
        <| fun _ ->
            let fixture = basic ()

            let isolatedPath =
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "swate-processcore-" + System.Guid.NewGuid().ToString("N")
                )

            fixture.Arc.ArcPath <- Some isolatedPath
            let converted = fromArc loadedTable fixture.Arc |> expectOk
            let outputId = converted.Model.OutputSets |> Map.toList |> List.head |> fst

            let session =
                Session.init converted.Model
                |> createProperty
                    (ProvenancePropertyTarget.OutputSets [ outputId ])
                    ProcessCoreKinds.factor
                    "memory-only"
                    "value-neutral"

            writeBack converted.Index session fixture.Arc |> expectOk |> ignore
            Expect.isFalse (System.IO.Directory.Exists isolatedPath) "Adapter must not write through ARC.ArcPath."
    ]
