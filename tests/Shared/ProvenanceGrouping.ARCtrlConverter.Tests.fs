module ProvenanceGroupingARCtrlConverterTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open ARCtrl
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.ARCtrlConverter

let private oa name =
    OntologyAnnotation.create (name = name)

let private text value =
    CompositeCell.createFreeText value

let private term value =
    CompositeCell.createTermFromString (name = value)

let private table (name: string) (headers: CompositeHeader list) (rows: CompositeCell list list) =
    let rows =
        rows
        |> List.map (fun row -> ResizeArray<CompositeCell>(row))
        |> ResizeArray

    ArcTable.createFromRows(name, ResizeArray<CompositeHeader>(headers), rows)

let private loadedAssayTable () =
    table
        "assay-table"
        [
            CompositeHeader.Input IOType.Sample
            CompositeHeader.Characteristic(oa "Species")
            CompositeHeader.Parameter(oa "Temperature")
            CompositeHeader.Output IOType.Sample
            CompositeHeader.Factor(oa "Replicate")
        ]
        [
            [ text "sample-a"; term "Arabidopsis"; term "22"; text "extract-a"; term "R1" ]
            [ text "sample-b"; term "Arabidopsis"; term "23"; text "extract-b"; term "R2" ]
        ]

let private previousStudyTable () =
    table
        "study-table"
        [
            CompositeHeader.Input IOType.Source
            CompositeHeader.Characteristic(oa "Organism")
            CompositeHeader.Output IOType.Sample
        ]
        [
            [ text "source-a"; term "Plant"; text "sample-a" ]
            [ text "source-b"; term "Plant"; text "sample-b" ]
        ]

let private loadedAssayTableWithDuplicateTemperatureColumns () =
    table
        "assay-table"
        [
            CompositeHeader.Input IOType.Sample
            CompositeHeader.Parameter(oa "Temperature")
            CompositeHeader.Parameter(oa "Temperature")
            CompositeHeader.Output IOType.Sample
        ]
        [
            [ text "sample-a"; term "22"; term "22"; text "extract-a" ]
        ]

let private inputOnlyAssayTable () =
    table
        "input-only-table"
        [
            CompositeHeader.Input IOType.Sample
            CompositeHeader.Characteristic(oa "Species")
            CompositeHeader.Parameter(oa "Temperature")
        ]
        [
            [ text "sample-a"; term "Arabidopsis"; term "22" ]
            [ text "sample-b"; term "Arabidopsis"; term "23" ]
        ]

let private outputOnlyAssayTable () =
    table
        "output-only-table"
        [
            CompositeHeader.Parameter(oa "Temperature")
            CompositeHeader.Output IOType.Sample
            CompositeHeader.Factor(oa "Replicate")
        ]
        [
            [ term "22"; text "extract-a"; term "R1" ]
            [ term "23"; text "extract-b"; term "R2" ]
        ]

let private previousStudyTableWithDuplicateOrganismColumns () =
    table
        "study-table"
        [
            CompositeHeader.Input IOType.Source
            CompositeHeader.Characteristic(oa "Organism")
            CompositeHeader.Characteristic(oa "Organism")
            CompositeHeader.Output IOType.Sample
        ]
        [
            [ text "source-a"; term "Plant"; term "Plant"; text "sample-a" ]
        ]

let private arcFixture () =
    let study =
        ArcStudy.create (
            identifier = "study-1",
            tables = ResizeArray [ previousStudyTable () ],
            registeredAssayIdentifiers = ResizeArray [ "assay-1" ]
        )

    let assay =
        ArcAssay.create (
            identifier = "assay-1",
            tables = ResizeArray [ loadedAssayTable () ]
        )

    ARC(
        identifier = "arc-1",
        studies = ResizeArray [ study ],
        assays = ResizeArray [ assay ]
    )

let private arcFixtureWithDuplicateTemperatureColumns () =
    let assay =
        ArcAssay.create (
            identifier = "assay-1",
            tables = ResizeArray [ loadedAssayTableWithDuplicateTemperatureColumns () ]
        )

    ARC(
        identifier = "arc-1",
        studies = ResizeArray [],
        assays = ResizeArray [ assay ]
    )

let private arcFixtureWithDuplicatePreviousColumns () =
    let study =
        ArcStudy.create (
            identifier = "study-1",
            tables = ResizeArray [ previousStudyTableWithDuplicateOrganismColumns () ],
            registeredAssayIdentifiers = ResizeArray [ "assay-1" ]
        )

    let assay =
        ArcAssay.create (
            identifier = "assay-1",
            tables = ResizeArray [ loadedAssayTable () ]
        )

    ARC(
        identifier = "arc-1",
        studies = ResizeArray [ study ],
        assays = ResizeArray [ assay ]
    )

let private loadedTable : ArcTableLocation =
    {
        Scope = ArcTableScope.Assay
        ParentIdentifier = "assay-1"
        TableName = "assay-table"
    }

let private convertWithPreviousContext () =
    fromLoadedArc
        {
            LoadedTable = loadedTable
            IncludePreviousContext = true
        }
        (arcFixture ())

let private convertWithoutPreviousContext () =
    fromLoadedArc
        {
            LoadedTable = loadedTable
            IncludePreviousContext = false
        }
        (arcFixture ())

let private expectText expected value =
    match value with
    | ProvenanceValue.Text actual -> Expect.equal actual expected "Expected text value."
    | ProvenanceValue.Term term -> Expect.equal term.Name expected "Expected term value."
    | ProvenanceValue.Integer actual -> Expect.equal (string actual) expected "Expected integer value."
    | ProvenanceValue.Float actual -> Expect.equal (string actual) expected "Expected float value."

let tests =
    testList "ProvenanceGrouping ARCtrl converter" [
        testCase "converts loaded input and output names into first-class sets" <| fun _ ->
            let result = convertWithPreviousContext ()

            let inputNames =
                result.Model.InputSets
                |> Map.toList
                |> List.map (fun (_, set) -> set.Name)
                |> List.sort

            let outputNames =
                result.Model.OutputSets
                |> Map.toList
                |> List.map (fun (_, set) -> set.Name)
                |> List.sort

            Expect.equal inputNames [ "sample-a"; "sample-b" ] "Loaded inputs should come from loaded input cells."
            Expect.equal outputNames [ "extract-a"; "extract-b" ] "Loaded outputs should come from loaded output cells."

        testCase "omits previous context when IncludePreviousContext is false" <| fun _ ->
            let result = convertWithoutPreviousContext ()

            let previousValues =
                result.Model.PropertyValues
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.filter (fun value ->
                    value.Source
                    |> Option.exists (fun source -> source.TableName <> result.Model.LoadedTableName)
                )
                |> Seq.toList

            Expect.isEmpty previousValues "No property values from non-loaded tables should appear when previous context is excluded."

        testCase "converts loaded row input-to-output connections" <| fun _ ->
            let result = convertWithPreviousContext ()

            let connectionPairs =
                result.Model.Connections
                |> Map.toList
                |> List.map (fun (_, connection) ->
                    result.Model.InputSets.[connection.InputSetId].Name,
                    result.Model.OutputSets.[connection.OutputSetId].Name,
                    connection.ProcessName
                )
                |> List.sort

            Expect.equal
                connectionPairs
                [
                    ("sample-a", "extract-a", Some "assay-table_0")
                    ("sample-b", "extract-b", Some "assay-table_1")
                ]
                "Each loaded row should produce a loaded input/output connection."

        testCase "attaches loaded property values to loaded sets" <| fun _ ->
            let result = convertWithPreviousContext ()

            let sampleA =
                result.Model.InputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "sample-a")

            let extractA =
                result.Model.OutputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "extract-a")

            let species =
                sampleA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Species")

            let temperature =
                sampleA.PropertyValueIds
                |> List.append extractA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Temperature")

            let replicate =
                extractA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Replicate")

            expectText "Arabidopsis" species.Value
            expectText "22" temperature.Value
            expectText "R1" replicate.Value

        testCase "collapses previous table context into property values on loaded inputs" <| fun _ ->
            let result = convertWithPreviousContext ()

            let sampleA =
                result.Model.InputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "sample-a")

            let organism =
                sampleA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Organism")

            expectText "Plant" organism.Value
            let location = result.Index.PropertyValueLocations.[organism.Id]
            Expect.equal location.Table.Scope ArcTableScope.Study "Collapsed previous value should remember study scope."
            Expect.equal location.Table.ParentIdentifier "study-1" "Collapsed previous value should remember parent identifier."
            Expect.equal location.Table.TableName "study-table" "Collapsed previous value should remember source table."
            Expect.equal location.OutputNames [ "sample-a" ] "Collapsed previous value should remember where it pointed to."

        testCase "identical duplicate loaded values collapse to one model value and one representative writeback location" <| fun _ ->
            let result =
                fromLoadedArc
                    {
                        LoadedTable = loadedTable
                        IncludePreviousContext = false
                    }
                    (arcFixtureWithDuplicateTemperatureColumns ())

            let sampleA =
                result.Model.InputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "sample-a")

            let temperatures =
                sampleA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.filter (fun value -> value.Header.Category.Name = "Temperature")

            Expect.equal temperatures.Length 1 "Equal duplicate loaded values should collapse into one model value."

            let writebackLocation = result.Index.PropertyValueLocations.[temperatures.Head.Id]
            Expect.equal writebackLocation.Table.TableName "assay-table" "A representative writeback location should still be preserved."

        testCase "collapsed previous-context duplicates keep one representative writeback slot" <| fun _ ->
            let result =
                fromLoadedArc
                    {
                        LoadedTable = loadedTable
                        IncludePreviousContext = true
                    }
                    (arcFixtureWithDuplicatePreviousColumns ())

            let sampleA =
                result.Model.InputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "sample-a")

            let organism =
                sampleA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Organism")

            let location = result.Index.PropertyValueLocations.[organism.Id]

            Expect.equal location.Table.TableName "study-table" "Collapsed previous-context duplicates should keep a representative source location."

        testCase "keeps ARCtrl table locations in the sidecar index" <| fun _ ->
            let result = convertWithPreviousContext ()

            let sampleA =
                result.Model.InputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "sample-a")

            let endpointLocation = result.Index.EndpointLocations.[sampleA.Id]

            Expect.equal endpointLocation.Table.Scope ArcTableScope.Assay "Loaded endpoint should remember assay scope."
            Expect.equal endpointLocation.Table.ParentIdentifier "assay-1" "Loaded endpoint should remember assay identifier."
            Expect.equal endpointLocation.Table.TableName "assay-table" "Loaded endpoint should remember table name."
            Expect.equal endpointLocation.Name "sample-a" "Endpoint location should keep the actual loaded input name."

        testCase "converts an input-only loaded table into loaded inputs without fake outputs or connections" <| fun _ ->
            let assay =
                ArcAssay.create (
                    identifier = "assay-1",
                    tables = ResizeArray [ inputOnlyAssayTable () ]
                )

            let arc =
                ARC(
                    identifier = "arc-1",
                    studies = ResizeArray [],
                    assays = ResizeArray [ assay ]
                )

            let location : ArcTableLocation =
                {
                    Scope = ArcTableScope.Assay
                    ParentIdentifier = "assay-1"
                    TableName = "input-only-table"
                }

            let result =
                fromLoadedArc
                    {
                        LoadedTable = location
                        IncludePreviousContext = false
                    }
                    arc

            let inputNames =
                result.Model.InputSets
                |> Map.toList
                |> List.map (fun (_, set) -> set.Name)
                |> List.sort

            Expect.equal inputNames [ "sample-a"; "sample-b" ] "Real input cells should still load as first-class sets."
            Expect.equal result.Model.OutputSets.Count 0 "Missing output columns must not create synthetic output sets."
            Expect.equal result.Model.Connections.Count 0 "One-sided loaded tables must not synthesize connections."

        testCase "converts an output-only loaded table into loaded outputs without fake inputs or connections" <| fun _ ->
            let assay =
                ArcAssay.create (
                    identifier = "assay-1",
                    tables = ResizeArray [ outputOnlyAssayTable () ]
                )

            let arc =
                ARC(
                    identifier = "arc-1",
                    studies = ResizeArray [],
                    assays = ResizeArray [ assay ]
                )

            let location : ArcTableLocation =
                {
                    Scope = ArcTableScope.Assay
                    ParentIdentifier = "assay-1"
                    TableName = "output-only-table"
                }

            let result =
                fromLoadedArc
                    {
                        LoadedTable = location
                        IncludePreviousContext = false
                    }
                    arc

            let outputNames =
                result.Model.OutputSets
                |> Map.toList
                |> List.map (fun (_, set) -> set.Name)
                |> List.sort

            Expect.equal outputNames [ "extract-a"; "extract-b" ] "Real output cells should still load as first-class sets."
            Expect.equal result.Model.InputSets.Count 0 "Missing input columns must not create synthetic input sets."
            Expect.equal result.Model.Connections.Count 0 "One-sided loaded tables must not synthesize connections."

        testCase "missing table lookup surfaces the ARCtrl exception instead of a converter-owned error DU" <| fun _ ->
            let missing =
                {
                    Scope = ArcTableScope.Assay
                    ParentIdentifier = "assay-1"
                    TableName = "missing-table"
                }

            Expect.throws
                (fun () ->
                    fromLoadedArc
                        {
                            LoadedTable = missing
                            IncludePreviousContext = false
                        }
                        (arcFixture ())
                    |> ignore)
                "Missing table lookup should be delegated to ARCtrl."
    ]
