module ProvenanceGroupingTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Swate.Components.Page.ProvenanceGrouping
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Fixtures
open Swate.Components.Shared.ProvenanceGrouping.Session

let typeTests =
    testList "Types" [
        testCase "loaded input set carries the actual input name"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"

            let inputSet = {
                Id = "input-a"
                TableName = "assay-table"
                Header = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
                Name = "Input A"
                PropertyValueIds = [ "pv-species-a" ]
                InheritedPropertyValueIds = Map.empty
            }

            let propertyValue = {
                Id = "pv-species-a"
                Header = species
                Value = ProvenanceValue.Text "Arabidopsis"
                Unit = None
                Source =
                    Some {
                        TableName = "previous-table"
                        ProcessName = Some "previous-process"
                        Header = species
                        InputNames = [ "Ancestor A" ]
                        OutputNames = []
                    }
            }

            Expect.equal inputSet.Name "Input A" "Loaded input name should live on the set."
            Expect.equal inputSet.PropertyValueIds [ propertyValue.Id ] "Set should point to property value occurrence."

            match propertyValue.Source with
            | Some source ->
                Expect.equal source.TableName "previous-table" "Collapsed value should keep writeback table metadata."
            | None -> failwith "Expected collapsed value source."

        testCase "inherited property helpers replace and remove connection-specific pointers"
        <| fun _ ->
            let inputSet = {
                Id = "input-a"
                TableName = "assay-table"
                Header = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
                Name = "Input A"
                PropertyValueIds = []
                InheritedPropertyValueIds = Map.empty
            }

            let inherited =
                inputSet
                |> ProvenanceSet.inheritPropertyValueIds "connection-a" [ "pv-a"; "pv-a"; "pv-b" ]

            Expect.equal
                inherited.InheritedPropertyValueIds.["connection-a"]
                [ "pv-a"; "pv-b" ]
                "Inherited property IDs should be distinct per connection."

            let replaced = inherited |> ProvenanceSet.inheritPropertyValueIds "connection-a" []

            Expect.isFalse
                (replaced.InheritedPropertyValueIds.ContainsKey "connection-a")
                "Empty inherited IDs should remove the connection entry."

            let removed =
                inherited |> ProvenanceSet.removeInheritedPropertyValueIds "connection-a"

            Expect.isFalse
                (removed.InheritedPropertyValueIds.ContainsKey "connection-a")
                "Explicit removal should drop only the requested connection entry."

        testCase "adapter endpoint and property kinds keep the editor model source agnostic"
        <| fun _ ->
            let customEndpointKind =
                ProvenanceKind.create "external:endpoint:plate-well" "Plate well"

            let customPropertyKind =
                ProvenanceKind.create "external:property:quality-score" "Quality Score"

            let customEndpoint = ioHeader customEndpointKind "Input [Plate well]"
            let customProperty = propertyHeader customPropertyKind "Quality Score"

            let built =
                model
                    "external-process-table"
                    [
                        propertyValue "pv-quality" customProperty (ProvenanceValue.Float 0.98) None None
                    ]
                    [
                        inputSet "input-well-a1" "external-process-table" customEndpoint "A1" [ "pv-quality" ]
                    ] [] []

            let input = built.InputSets.["input-well-a1"]
            let value = built.PropertyValues.["pv-quality"]

            Expect.equal input.Header.Kind customEndpointKind "Endpoint kind should not require an ISA IO type."
            Expect.equal value.Header.Kind customPropertyKind "Property kind should not require an ISA property family."
            Expect.equal input.Name "A1" "Custom process endpoints should still use ProvenanceSet.Name for display."
    ]

let modelTests =
    testList "Model" [
        testCase "direct model builder preserves loaded names and repeated distinct property values"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-species-a"
                            species
                            (ProvenanceValue.Text "Arabidopsis")
                            None
                            (Some(anchor "previous-table" (Some "previous-process") species [ "Ancestor A" ] []))
                        propertyValue
                            "pv-rep-1"
                            replicate
                            (ProvenanceValue.Text "1")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                        propertyValue
                            "pv-rep-2"
                            replicate
                            (ProvenanceValue.Text "2")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [
                            "pv-species-a"
                            "pv-rep-1"
                            "pv-rep-2"
                        ]
                    ] [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-rep-1"; "pv-rep-2" ]
                    ] [
                        connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                    ]

            Expect.equal built.InputSets.["input-a"].Name "Input A" "Direct model should keep the loaded input name."

            Expect.equal
                built.OutputSets.["output-a"].Name
                "Output A"
                "Direct model should keep the loaded output name."

            Expect.equal built.PropertyValues.Count 3 "Distinct repeated values should remain separate model values."

        testCase "direct model builder keeps previous-context anchors without previous first-class sets"
        <| fun _ ->
            let previousTreatment =
                propertyHeader FixtureKinds.characteristicProperty "Previous Treatment"

            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-previous-treatment-a"
                            previousTreatment
                            (ProvenanceValue.Text "Drought")
                            None
                            (Some(
                                anchor "previous-study-table" (Some "previous-process") previousTreatment [
                                    "Ancestor A"
                                ] []
                            ))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-previous-treatment-a" ]
                    ] [] []

            let value = built.PropertyValues.["pv-previous-treatment-a"]
            Expect.equal built.InputSets.Count 1 "Only loaded sets should be first-class sets."

            match value.Source with
            | Some source ->
                Expect.equal
                    source.TableName
                    "previous-study-table"
                    "Collapsed previous-context values should keep their source table."
            | None -> failwith "Expected a previous-context source anchor."
    ]

let private validModel () =
    let species = propertyHeader FixtureKinds.characteristicProperty "Species"
    let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
    let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
    let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

    model
        "assay-table"
        [
            propertyValue
                "pv-species-arabidopsis-a"
                species
                (ProvenanceValue.Text "Arabidopsis")
                None
                (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
            propertyValue
                "pv-species-arabidopsis-b"
                species
                (ProvenanceValue.Text "Arabidopsis")
                None
                (Some(anchor "assay-table" (Some "assay-process") species [ "Input B" ] []))
            propertyValue
                "pv-rep-output-b-1"
                replicate
                (ProvenanceValue.Text "1")
                None
                (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output B" ]))
            propertyValue
                "pv-rep-output-b-2"
                replicate
                (ProvenanceValue.Text "2")
                None
                (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input B" ] [ "Output B" ]))
        ]
        [
            inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-arabidopsis-a" ]
            inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-species-arabidopsis-b" ]
            inputSet "input-c" "assay-table" inputHeader "Input C" []
        ] [
            outputSet "output-a" "assay-table" outputHeader "Output A" []
            outputSet "output-b" "assay-table" outputHeader "Output B" [ "pv-rep-output-b-1"; "pv-rep-output-b-2" ]
            outputSet "output-c" "assay-table" outputHeader "Output C" []
        ] [
            connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
            connection "connection-b" "assay-table" (Some "assay-process") "input-a" "output-b"
            connection "connection-c" "assay-table" (Some "assay-process") "input-b" "output-b"
            connection "connection-d" "assay-table" (Some "assay-process") "input-c" "output-c"
        ]

let groupingTests =
    testList "Grouping" [
        testCase "no grouping displays each loaded set by name"
        <| fun _ ->
            let model = validModel ()
            let groups = displayGroups model ProvenanceSide.Input []

            Expect.equal
                (groups |> List.map (fun group -> group.Members.Head.Name))
                [ "Input A"; "Input B"; "Input C" ]
                "No grouping should preserve loaded input names."

        testCase "multi-value grouping uses the complete value set as one group key"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-input-a-species-chlamy"
                            species
                            (ProvenanceValue.Text "Chlamydomonas")
                            None
                            None
                        propertyValue
                            "pv-input-b-species-arabidopsis"
                            species
                            (ProvenanceValue.Text "Arabidopsis")
                            None
                            None
                        propertyValue
                            "pv-input-c-species-arabidopsis"
                            species
                            (ProvenanceValue.Text "Arabidopsis")
                            None
                            None
                        propertyValue
                            "pv-input-c-species-chlamy"
                            species
                            (ProvenanceValue.Text "Chlamydomonas")
                            None
                            None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species-chlamy" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-input-b-species-arabidopsis" ]
                        inputSet "input-c" "assay-table" inputHeader "Input C" [
                            "pv-input-c-species-arabidopsis"
                            "pv-input-c-species-chlamy"
                        ]
                    ] [] []

            let groups = displayGroups built ProvenanceSide.Input [ { Header = species } ]

            let membersByTitle =
                groups
                |> List.map (fun group ->
                    let title =
                        group.GroupingValues
                        |> List.map (fun value ->
                            match value.Value with
                            | ProvenanceValue.Text text -> text
                            | other -> string other
                        )
                        |> List.sort
                        |> String.concat ", "

                    title, group.Members |> List.map (fun member' -> member'.SetId) |> List.sort
                )
                |> Map.ofList

            Expect.equal
                (membersByTitle |> Map.toList |> List.map fst)
                [
                    "Arabidopsis"
                    "Arabidopsis, Chlamydomonas"
                    "Chlamydomonas"
                ]
                "Grouping by a multi-valued property should create one group for the complete value set."

            Expect.isTrue
                (groups
                 |> List.exists (fun group -> group.Id = "input:Species=Arabidopsis | Chlamydomonas"))
                "A multi-valued group id should print the grouping key once and join values with ' | '."

            Expect.equal
                membersByTitle.["Arabidopsis"]
                [ "input-b" ]
                "The multi-valued set must not appear in the Arabidopsis-only group."

            Expect.equal
                membersByTitle.["Chlamydomonas"]
                [ "input-a" ]
                "The multi-valued set must not appear in the Chlamydomonas-only group."

            Expect.equal
                membersByTitle.["Arabidopsis, Chlamydomonas"]
                [ "input-c" ]
                "The multi-valued set should appear in its combined value-set group."

        testCase "grouped members retain non-grouping property values for editing"
        <| fun _ ->
            let model = sampleModel ()
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let groups = displayGroups model ProvenanceSide.Input [ { Header = species } ]

            let inputA =
                groups
                |> List.collect (fun group -> group.Members)
                |> List.find (fun member' -> member'.SetId = "input-a")

            Expect.isTrue
                (inputA.PropertyValueIds |> List.contains "pv-input-a-temperature")
                "Grouping by Species must not hide Temperature from the member editing surface."

        testCase "outputs expose input properties inherited through current loaded connections"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let temperature = propertyHeader FixtureKinds.parameterProperty "Temperature"
            let analysis = propertyHeader FixtureKinds.parameterProperty "Analysis"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-species" species (ProvenanceValue.Text "Arabidopsis") None None
                        propertyValue "pv-input-b-temperature" temperature (ProvenanceValue.Text "12 C") None None
                        propertyValue "pv-output-a-analysis" analysis (ProvenanceValue.Text "LC-MS") None None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-input-b-temperature" ]
                    ] [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-analysis" ]
                    ] [
                        connection "connection-a" "assay-table" None "input-a" "output-a"
                        connection "connection-b" "assay-table" None "input-b" "output-a"
                    ]

            let outputA =
                displayGroups built ProvenanceSide.Output []
                |> List.collect (fun group -> group.Members)
                |> List.find (fun member' -> member'.SetId = "output-a")

            Expect.equal
                (outputA.PropertyValueIds |> List.sort)
                [
                    "pv-input-a-species"
                    "pv-input-b-temperature"
                    "pv-output-a-analysis"
                ]
                "An output should expose its own properties plus properties inherited from all directly connected loaded inputs."

        testCase "output inheritance follows the current loaded connection set"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let build connections =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-species" species (ProvenanceValue.Text "Arabidopsis") None None
                        propertyValue "pv-input-b-species" species (ProvenanceValue.Text "Chlamydomonas") None None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-input-b-species" ]
                    ]
                    [
                        outputSet "output-a" "assay-table" outputHeader "Output A" []
                    ]
                    connections

            let withOneConnection =
                build [
                    connection "connection-a" "assay-table" None "input-a" "output-a"
                ]

            let withTwoConnections =
                build [
                    connection "connection-a" "assay-table" None "input-a" "output-a"
                    connection "connection-b" "assay-table" None "input-b" "output-a"
                ]

            let inheritedIds model =
                displayGroups model ProvenanceSide.Output []
                |> List.collect (fun group -> group.Members)
                |> List.find (fun member' -> member'.SetId = "output-a")
                |> fun member' -> member'.PropertyValueIds |> List.sort

            Expect.equal
                (inheritedIds withOneConnection)
                [ "pv-input-a-species" ]
                "Only properties from currently connected inputs should be inherited."

            Expect.equal
                (inheritedIds withTwoConnections)
                [ "pv-input-a-species"; "pv-input-b-species" ]
                "Adding a connection should add that input's properties to the output's effective properties."

        testCase "grouping collapses identical equal values for one set into one display member"
        <| fun _ ->
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-rep-1a"
                            replicate
                            (ProvenanceValue.Text "1")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                        propertyValue
                            "pv-rep-1b"
                            replicate
                            (ProvenanceValue.Text "1")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                    ]
                    [] [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-rep-1a"; "pv-rep-1b" ]
                    ] []

            let groups = displayGroups built ProvenanceSide.Output [ { Header = replicate } ]
            let members = groups |> List.collect (fun group -> group.Members)

            Expect.equal groups.Length 1 "Identical equal values should collapse into one output group."
            Expect.equal members.Length 1 "The output set should appear once in the collapsed group."

            Expect.equal
                members.Head.PropertyValueIds
                [ "pv-rep-1a"; "pv-rep-1b" ]
                "Collapsed display membership should keep all underlying property value IDs."

        testCase "grouping keeps equal text with different units separate"
        <| fun _ ->
            let temperature = propertyHeader FixtureKinds.parameterProperty "Temperature"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let celsius = term "C"
            let fahrenheit = term "F"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-temp-c"
                            temperature
                            (ProvenanceValue.Integer 12)
                            (Some celsius)
                            (Some(anchor "assay-table" (Some "assay-process") temperature [ "Input A" ] []))
                        propertyValue
                            "pv-temp-f"
                            temperature
                            (ProvenanceValue.Integer 12)
                            (Some fahrenheit)
                            (Some(anchor "assay-table" (Some "assay-process") temperature [ "Input B" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-temp-c" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-temp-f" ]
                    ] [] []

            let groups = displayGroups built ProvenanceSide.Input [ { Header = temperature } ]

            Expect.equal groups.Length 2 "Values with the same scalar value but different units must not collapse."

        testCase "scoped input grouping uses multiple own values as one value-set group"
        <| fun _ ->
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-input-a-rep-1"
                            replicate
                            (ProvenanceValue.Text "1")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] []))
                        propertyValue
                            "pv-input-a-rep-2"
                            replicate
                            (ProvenanceValue.Text "2")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [
                            "pv-input-a-rep-1"
                            "pv-input-a-rep-2"
                        ]
                    ] [] []

            let groups =
                displayGroupsForAssignments built ProvenanceSide.Input [
                    {
                        Key = { Header = replicate }
                        Scope = GroupingScope.Input
                    }
                ]

            let groupedValues =
                groups
                |> List.choose (fun group ->
                    if group.Members |> List.exists (fun member' -> member'.SetId = "input-a") then
                        group.GroupingValues
                        |> List.map (fun groupingValue -> groupingValue.Value)
                        |> List.sort
                        |> Some
                    else
                        None
                )
                |> List.exactlyOne

            Expect.equal
                groupedValues
                [ ProvenanceValue.Text "1"; ProvenanceValue.Text "2" ]
                "An input with multiple values for the same grouping key must appear in one combined value-set group."

        testCase "both-side grouping lets inputs inherit missing values from direct connected outputs"
        <| fun _ ->
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-output-a-rep-1"
                            replicate
                            (ProvenanceValue.Text "1")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [] [ "Output A" ]))
                        propertyValue
                            "pv-output-b-rep-2"
                            replicate
                            (ProvenanceValue.Text "2")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [] [ "Output B" ]))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ] [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-rep-1" ]
                        outputSet "output-b" "assay-table" outputHeader "Output B" [ "pv-output-b-rep-2" ]
                    ] [
                        connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                        connection "connection-b" "assay-table" (Some "assay-process") "input-a" "output-b"
                    ]

            let groups =
                displayGroupsForAssignments built ProvenanceSide.Input [
                    {
                        Key = { Header = replicate }
                        Scope = GroupingScope.Both
                    }
                ]

            let groupedValues =
                groups
                |> List.find (fun group -> group.Members |> List.exists (fun member' -> member'.SetId = "input-a"))
                |> fun group ->
                    group.GroupingValues
                    |> List.map (fun groupingValue -> groupingValue.Value)
                    |> List.sort

            Expect.equal
                groupedValues
                [ ProvenanceValue.Text "1"; ProvenanceValue.Text "2" ]
                "A missing input property should inherit direct connected output values as one combined value-set group."

        testCase "both-side grouping prefers input values over inherited output values"
        <| fun _ ->
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-input-a-rep-3"
                            replicate
                            (ProvenanceValue.Text "3")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] []))
                        propertyValue
                            "pv-output-a-rep-1"
                            replicate
                            (ProvenanceValue.Text "1")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") replicate [] [ "Output A" ]))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-rep-3" ]
                    ] [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-rep-1" ]
                    ] [
                        connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                    ]

            let groups =
                displayGroupsForAssignments built ProvenanceSide.Input [
                    {
                        Key = { Header = replicate }
                        Scope = GroupingScope.Both
                    }
                ]

            let groupedValues =
                groups
                |> List.filter (fun group -> group.Members |> List.exists (fun member' -> member'.SetId = "input-a"))
                |> List.choose (fun group ->
                    group.GroupingValues
                    |> List.tryExactlyOne
                    |> Option.map (fun groupingValue -> groupingValue.Value)
                )
                |> List.sort

            Expect.equal
                groupedValues
                [ ProvenanceValue.Text "3" ]
                "An input's own values should be used before direct connected output values are considered."

        testCase "displayGroups works for input-only loaded models"
        <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ] [] []

            let groups = displayGroups built ProvenanceSide.Input []

            Expect.equal groups.Length 1 "Input-only models should still render input groups."
            Expect.equal groups.Head.Members.Head.Name "Input A" "The loaded input should still be visible."

        testCase "displayConnections expands to represented loaded set pairs only"
        <| fun _ ->
            let model = validModel ()
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputGroups = displayGroups model ProvenanceSide.Input [ { Header = species } ]
            let outputGroups = displayGroups model ProvenanceSide.Output []
            let connections = displayConnections model inputGroups outputGroups

            let representedIds =
                connections
                |> List.collect (fun connection -> connection.ConnectionIds)
                |> List.sort

            Expect.equal
                representedIds
                [
                    "connection-a"
                    "connection-b"
                    "connection-c"
                    "connection-d"
                ]
                "Display lines should represent real loaded connections only."

        testCase "displayConnections returns no lines when one side is absent"
        <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ] [] []

            let inputGroups = displayGroups built ProvenanceSide.Input []
            let outputGroups = displayGroups built ProvenanceSide.Output []
            let connections = displayConnections built inputGroups outputGroups

            Expect.isEmpty connections "One-sided models must not invent display connections."
    ]

let private sourceFromLoadedMembershipModel () =
    let species = propertyHeader FixtureKinds.characteristicProperty "Species"
    let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
    let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

    let pvId = "pv-species-a"
    let setAId = "input-a"
    let setBId = "output-a"
    let connId = "connection-a"

    model
        "assay-table"
        [
            propertyValue pvId species (ProvenanceValue.Text "Arabidopsis") None None
        ]
        [
            inputSet setAId "assay-table" inputHeader "Input A" [ pvId ]
        ] [
            outputSet setBId "assay-table" outputHeader "Output A" [ pvId ]
        ] [
            connection connId "assay-table" (Some "assay-process") setAId setBId
        ]

let editTests =
    testList "Edit" [
        testCase "updatePropertyValue preserves collapsed source anchor"
        <| fun _ ->
            let model = validModel ()

            match updatePropertyValue "pv-species-arabidopsis-a" (ProvenanceValue.Text "A. thaliana") None model with
            | Ok(nextModel, [ ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, source, _, newValue, _) ]) ->
                Expect.equal propertyValueId "pv-species-arabidopsis-a" "Patch should identify edited occurrence."
                Expect.equal source.InputNames [ "Input A" ] "Patch should preserve source input name."
                Expect.equal newValue (ProvenanceValue.Text "A. thaliana") "Patch should carry edited value."

                Expect.equal
                    nextModel.PropertyValues.["pv-species-arabidopsis-a"].Value
                    (ProvenanceValue.Text "A. thaliana")
                    "Model should update the occurrence."
            | other -> failwithf "Expected one UpdatePropertyValue patch, got %A" other

        testCase "createLoadedPropertyValue adds occurrence to target loaded set"
        <| fun _ ->
            let model = validModel ()
            let treatment = propertyHeader FixtureKinds.characteristicProperty "Treatment"

            let command = {
                Target = ProvenancePropertyTarget.InputSets [ "input-c" ]
                CopiedFrom = None
                Header = treatment
                Value = ProvenanceValue.Text "Drought"
                Unit = None
            }

            match createLoadedPropertyValue command model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, copiedFrom, header, value, _) ]) ->
                Expect.equal
                    target
                    (ProvenancePropertyTarget.InputSets [ "input-c" ])
                    "Patch should target the loaded input set."

                Expect.equal copiedFrom None "New value should not be copied from another occurrence."
                Expect.equal header treatment "Patch should carry the requested header."
                Expect.equal value (ProvenanceValue.Text "Drought") "Patch should carry the requested value."

                Expect.equal
                    (nextModel.InputSets.["input-c"].PropertyValueIds.Length)
                    1
                    "Target input set should point to the new value."
            | other -> failwithf "Expected one AddLoadedPropertyValue patch, got %A" other

        testCase "copyPropertyValueToLoadedTarget copies previous value to existing loaded connection"
        <| fun _ ->
            let model = validModel ()

            match
                copyPropertyValueToLoadedTarget
                    "pv-species-arabidopsis-a"
                    (ProvenancePropertyTarget.Connections [ "connection-d" ])
                    model
            with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, copiedFrom, _, value, _) ]) ->
                Expect.equal
                    target
                    (ProvenancePropertyTarget.Connections [ "connection-d" ])
                    "Patch should target the existing loaded connection."

                Expect.equal
                    copiedFrom
                    (Some "pv-species-arabidopsis-a")
                    "Patch should preserve copied source occurrence."

                Expect.equal value (ProvenanceValue.Text "Arabidopsis") "Patch should copy the value."

                Expect.isTrue
                    (nextModel.InputSets.["input-c"].PropertyValueIds.Length > model.InputSets.["input-c"]
                        .PropertyValueIds.Length)
                    "Connection input set should point to the copied loaded occurrence."

                Expect.isTrue
                    (nextModel.OutputSets.["output-c"].PropertyValueIds.Length > model.OutputSets.["output-c"]
                        .PropertyValueIds.Length)
                    "Connection output set should point to the copied loaded occurrence."
            | other -> failwithf "Expected one AddLoadedPropertyValue patch, got %A" other

        testCase "createLoadedPropertyValue is a no-op when an identical loaded value already exists on the same target"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-species-a"
                            species
                            (ProvenanceValue.Text "Arabidopsis")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a" ]
                    ] [] []

            match
                createLoadedPropertyValue
                    {
                        Target = ProvenancePropertyTarget.InputSets [ "input-a" ]
                        CopiedFrom = None
                        Header = species
                        Value = ProvenanceValue.Text "Arabidopsis"
                        Unit = None
                    }
                    built
            with
            | Ok(nextModel, []) ->
                Expect.equal nextModel built "An exact duplicate loaded value should not create a second model value."
            | other -> failwithf "Expected a no-op duplicate create, got %A" other

        testCase "copyPropertyValueToLoadedTarget is a no-op when the target already has an identical value"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue
                            "pv-species-a"
                            species
                            (ProvenanceValue.Text "Arabidopsis")
                            None
                            (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a" ]
                    ] [] []

            match
                copyPropertyValueToLoadedTarget "pv-species-a" (ProvenancePropertyTarget.InputSets [ "input-a" ]) built
            with
            | Ok(nextModel, []) ->
                Expect.equal nextModel built "Copying an identical loaded value onto the same target should be a no-op."
            | other -> failwithf "Expected a no-op duplicate copy, got %A" other

        testCase "createLoadedSet adds the missing output side to an input-only loaded model"
        <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ] [] []

            match
                createLoadedSet
                    {
                        Side = ProvenanceSide.Output
                        Header = outputHeader
                        Name = "Output A"
                    }
                    built
            with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedSet(side, tableName, header, name) ]) ->
                Expect.equal side ProvenanceSide.Output "Patch should carry the created side."
                Expect.equal tableName "assay-table" "Patch should target the loaded table."
                Expect.equal header outputHeader "Patch should preserve the created header."
                Expect.equal name "Output A" "Patch should preserve the created name."
                Expect.equal nextModel.OutputSets.Count 1 "The missing output side should be created."
            | other -> failwithf "Expected AddLoadedSet patch, got %A" other

        testCase "createLoadedSet is a no-op when the same loaded endpoint already exists"
        <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ] [] []

            match
                createLoadedSet
                    {
                        Side = ProvenanceSide.Input
                        Header = inputHeader
                        Name = "Input A"
                    }
                    built
            with
            | Ok(nextModel, []) ->
                Expect.equal nextModel built "Creating an already-existing loaded endpoint should be a no-op."
            | other -> failwithf "Expected no-op duplicate create, got %A" other

        testCase "connectSets works after the missing side was created on a partial model"
        <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ] [] []

            let created =
                match
                    createLoadedSet
                        {
                            Side = ProvenanceSide.Output
                            Header = outputHeader
                            Name = "Output A"
                        }
                        built
                with
                | Ok(nextModel, _) -> nextModel
                | Error error -> failwithf "Unexpected createLoadedSet error: %A" error

            let createdOutputId = created.OutputSets |> Map.toList |> List.exactlyOne |> fst

            match connectSets "input-a" createdOutputId None created with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedConnection(_, _, inputSetId, outputSetId) ]) ->
                Expect.equal inputSetId "input-a" "Connection patch should target the existing loaded input."
                Expect.equal outputSetId createdOutputId "Connection patch should target the created loaded output."

                Expect.equal
                    nextModel.Connections.Count
                    1
                    "The partial model should become connectable after creating the missing side."
            | other -> failwithf "Expected AddLoadedConnection patch, got %A" other

        testCase "connectSets creates a loaded input output connection"
        <| fun _ ->
            let model = validModel ()

            match connectSets "input-c" "output-a" None model with
            | Ok(nextModel,
                 [ ProvenanceTablePatch.AddLoadedConnection(tableName, processName, inputSetId, outputSetId) ]) ->
                Expect.equal tableName "assay-table" "Patch should target loaded table."
                Expect.equal processName None "Caller may create a connection without assigning a process name yet."
                Expect.equal inputSetId "input-c" "Patch should keep input set."
                Expect.equal outputSetId "output-a" "Patch should keep output set."

                Expect.isTrue
                    (nextModel.Connections
                     |> Map.exists (fun _ connection ->
                         connection.InputSetId = "input-c" && connection.OutputSetId = "output-a"
                     ))
                    "Model should contain new connection."
            | other -> failwithf "Expected one AddLoadedConnection patch, got %A" other

        testCase "removeConnection removes the loaded connection and its inherited values"
        <| fun _ ->
            let model = validModel ()

            Expect.isTrue
                (model.OutputSets.["output-b"].InheritedPropertyValueIds.ContainsKey "connection-c")
                "Sanity: output-b should inherit values through connection-c before removal."

            match removeConnection "connection-c" model with
            | Ok(nextModel,
                 [ ProvenanceTablePatch.RemoveLoadedConnection(tableName, processName, inputSetId, outputSetId) ]) ->
                Expect.equal tableName "assay-table" "Patch should target the loaded table."
                Expect.equal processName (Some "assay-process") "Patch should keep the process name for writeback."
                Expect.equal inputSetId "input-b" "Patch should keep the input set."
                Expect.equal outputSetId "output-b" "Patch should keep the output set."

                Expect.isFalse
                    (nextModel.Connections.ContainsKey "connection-c")
                    "Model should no longer contain the removed connection."

                Expect.isFalse
                    (nextModel.OutputSets.["output-b"].InheritedPropertyValueIds.ContainsKey "connection-c")
                    "Inherited values from the removed connection should be dropped."

                Expect.isTrue
                    (nextModel.OutputSets.["output-b"].InheritedPropertyValueIds.ContainsKey "connection-b")
                    "Other connections keep their inherited values."
            | other -> failwithf "Expected one RemoveLoadedConnection patch, got %A" other

        testCase "removeConnection rejects unknown connections"
        <| fun _ ->
            match removeConnection "missing-connection" (validModel ()) with
            | Error(EditError.ConnectionNotFound connectionId) ->
                Expect.equal connectionId "missing-connection" "Error should carry the missing connection id."
            | other -> failwithf "Expected ConnectionNotFound, got %A" other

        testCase "updatePropertyValue finds anchor from loaded set membership when Source is None"
        <| fun _ ->
            let model = sourceFromLoadedMembershipModel ()

            match updatePropertyValue "pv-species-a" (ProvenanceValue.Text "A. thaliana") None model with
            | Ok(nextModel, [ ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, source, _, newValue, _) ]) ->
                Expect.equal propertyValueId "pv-species-a" "Patch should identify edited occurrence."

                Expect.equal
                    source.InputNames
                    [ "Input A" ]
                    "Source should derive input names from loaded set membership."

                Expect.equal
                    source.OutputNames
                    [ "Output A" ]
                    "Source should derive output names from loaded set membership."

                Expect.equal newValue (ProvenanceValue.Text "A. thaliana") "Patch should carry edited value."

                Expect.isSome
                    nextModel.PropertyValues.["pv-species-a"].Source
                    "Model should persist the derived source anchor."
            | other -> failwithf "Expected one UpdatePropertyValue patch, got %A" other
    ]

let fixtureTests =
    testList "Fixtures" [
        testCase "sampleModel includes loaded names and previous collapsed value"
        <| fun _ ->
            let model = sampleModel ()

            Expect.equal model.InputSets.["input-a"].Name "Input A" "Fixture should expose actual loaded input name."

            Expect.equal
                model.OutputSets.["output-b"].Name
                "Output B"
                "Fixture should expose actual loaded output name."

            let previousValues =
                model.InputSets.["input-a"].PropertyValueIds
                |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)
                |> List.filter (fun value ->
                    value.Source
                    |> Option.exists (fun source -> source.TableName = "previous-study-table")
                )

            Expect.isNonEmpty previousValues "Fixture should include collapsed previous-context property value."

        testCase "sampleModel connections are loaded set pairs"
        <| fun _ ->
            let model = sampleModel ()

            let pairs =
                model.Connections
                |> Map.toList
                |> List.map (fun (_, connection) -> connection.InputSetId, connection.OutputSetId)
                |> List.sort

            Expect.equal
                pairs
                [
                    "input-a", "output-a"
                    "input-a", "output-b"
                    "input-b", "output-b"
                    "input-c", "output-c"
                    "input-d", "output-d"
                ]
                "Fixture should preserve exact loaded input/output set connections."
    ]

let sessionTests =
    testList "Session" [
        testCase "init creates one conceptual layer with input and output sides"
        <| fun _ ->
            let initial = sampleModel ()
            let session = Session.init initial
            let active = Session.activeLayer session

            Expect.equal session.LayerOrder [ "layer-1" ] "Initial session should contain one conceptual layer."
            Expect.equal session.ActiveLayerId "layer-1" "Initial layer should be active."
            Expect.equal active.Id "layer-1" "Active layer id should be layer-1."
            Expect.equal active.InputSideId "layer-1-input" "Input side id should belong to layer-1."
            Expect.equal active.OutputSideId "layer-1-output" "Output side id should belong to layer-1."
            Expect.equal active.Model initial "Initial active layer should preserve the converted model."

        testCase "session layer ids and side ids use distinct namespaces"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session

            Expect.notEqual layer.Id layer.InputSideId "Layer id should not be reused as input side id."
            Expect.notEqual layer.Id layer.OutputSideId "Layer id should not be reused as output side id."
            Expect.notEqual layer.InputSideId layer.OutputSideId "Input and output side ids should be distinct."

        testCase "selectLayer switches display layers without producing writeback patches"
        <| fun _ ->
            let session = Session.init (sampleModel ())

            match Session.selectLayer "layer-1" session with
            | Ok(next, patches) ->
                Expect.equal next.ActiveLayerId "layer-1" "Known layer should become active."
                Expect.isEmpty patches "Navigation is view/session state only."
            | Error error -> failwithf "Expected layer selection success, got %A" error

        testCase "addLayer defaults to active outputs and creates a new layer with seeded inputs"
        <| fun _ ->
            let session = Session.init (sampleModel ())

            match Session.addLayer { SelectedSets = [] } session with
            | Ok(next, patches) ->
                let layer = Session.activeLayer next

                let names =
                    layer.Model.InputSets
                    |> Map.toList
                    |> List.map (fun (_, set) -> set.Name)
                    |> List.sort

                Expect.equal next.LayerOrder [ "layer-1"; "layer-2" ] "A second conceptual layer should be created."
                Expect.equal next.ActiveLayerId "layer-2" "The new layer should become active."
                Expect.equal layer.InputSideId "layer-2-input" "New input side should belong to layer-2."
                Expect.equal layer.OutputSideId "layer-2-output" "New output side should belong to layer-2."

                Expect.equal
                    names
                    [
                        "Output A"
                        "Output B"
                        "Output C"
                        "Output D"
                        "Output E"
                    ]
                    "Active outputs should seed later inputs by default."

                Expect.isEmpty layer.Model.OutputSets "New layer starts without designated outputs."
                Expect.isEmpty layer.Model.Connections "No connector exists until the user creates one."
                Expect.equal next.ReferenceLinks.Length 5 "Each seeded input should keep an upstream reference."
                Expect.isEmpty patches "Layer derivation should not write ARC data."
            | Error error -> failwithf "Expected layer addition success, got %A" error

        testCase "addLayer treats selected active inputs as new layer inputs"
        <| fun _ ->
            let session = Session.init (sampleModel ())

            match
                Session.addLayer
                    {
                        SelectedSets = [ ProvenanceSide.Input, "input-a" ]
                    }
                    session
            with
            | Ok(next, patches) ->
                let layer = Session.activeLayer next
                let input = layer.Model.InputSets |> Map.toList |> List.exactlyOne |> snd

                Expect.equal
                    next.LayerOrder
                    [ "layer-1"; "layer-2" ]
                    "Input-only selection should create one new conceptual layer."

                Expect.equal input.Name "Input A" "The selected active input should become a new layer input snapshot."
                Expect.equal next.ReferenceLinks.Length 1 "The selected active input should keep an upstream reference."
                Expect.isEmpty layer.Model.OutputSets "New layer starts without designated outputs."
                Expect.isEmpty patches "Layer derivation should not write ARC data."
            | Error error -> failwithf "Expected input-seeded layer addition success, got %A" error

        testCase "addLayer carries output properties inherited through loaded connections"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let analysis = propertyHeader FixtureKinds.parameterProperty "Analysis"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let initial =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-species" species (ProvenanceValue.Text "Arabidopsis") None None
                        propertyValue "pv-output-a-analysis" analysis (ProvenanceValue.Text "LC-MS") None None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species" ]
                    ] [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-analysis" ]
                    ] [
                        connection "connection-a" "assay-table" None "input-a" "output-a"
                    ]
                |> Session.init

            match
                Session.addLayer
                    {
                        SelectedSets = [ ProvenanceSide.Output, "output-a" ]
                    }
                    initial
            with
            | Ok(next, patches) ->
                Expect.isEmpty patches "Layer projection should not write ARC data."

                let projected =
                    (Session.activeLayer next).Model.InputSets
                    |> Map.toList
                    |> List.exactlyOne
                    |> snd

                Expect.equal
                    (ProvenanceSet.effectivePropertyValueIds projected |> List.sort)
                    [ "pv-input-a-species"; "pv-output-a-analysis" ]
                    "A projected output should keep properties inherited from its previously connected loaded input."
            | Error error -> failwithf "Expected layer addition success, got %A" error

        testCase "addLayer treats mixed input and output seeds as new layer inputs"
        <| fun _ ->
            let session = Session.init (sampleModel ())

            let selected = {
                SelectedSets = [
                    ProvenanceSide.Input, "input-a"
                    ProvenanceSide.Output, "output-b"
                ]
            }

            match Session.addLayer selected session with
            | Ok(next, patches) ->
                let layer = Session.activeLayer next

                let names =
                    layer.Model.InputSets
                    |> Map.toList
                    |> List.map (fun (_, set) -> set.Name)
                    |> List.sort

                Expect.equal
                    next.LayerOrder
                    [ "layer-1"; "layer-2" ]
                    "Mixed selection should create one new conceptual layer."

                Expect.equal names [ "Input A"; "Output B" ] "Mixed selection should become the next input side."
                Expect.equal next.ReferenceLinks.Length 2 "Only seeded entities should be linked."

                Expect.isFalse
                    (next.Layers |> List.exists (fun layer -> layer.Id = "selection-3"))
                    "Mixed selection should not create a virtual selection layer."

                Expect.isEmpty patches "Layer derivation should not write ARC data."
            | Error error -> failwithf "Expected mixed layer addition success, got %A" error

        testCase "property edits propagate through linked snapshots on focus change"
        <| fun _ ->
            let first = Session.init (sampleModel ())

            let layered =
                match
                    Session.addLayer
                        {
                            SelectedSets = [ ProvenanceSide.Output, "output-a" ]
                        }
                        first
                with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected addLayer error: %A" error

            let layer2 = Session.activeLayer layered
            let carriedInput = layer2.Model.InputSets |> Map.toList |> List.exactlyOne |> snd
            let analysisId = carriedInput.PropertyValueIds |> List.head

            let edited =
                match Session.updatePropertyValue analysisId (ProvenanceValue.Text "Imaging") None layered with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected updatePropertyValue error: %A" error

            let layer1BeforeFocus =
                (Session.layerById "layer-1" edited).Model.PropertyValues.[analysisId].Value

            let layer2BeforeFocus =
                (Session.layerById "layer-2" edited).Model.PropertyValues.[analysisId].Value

            Expect.notEqual
                layer1BeforeFocus
                (ProvenanceValue.Text "Imaging")
                "Upstream layer should not update before focus refresh."

            Expect.equal
                layer2BeforeFocus
                (ProvenanceValue.Text "Imaging")
                "Focused downstream layer should update immediately."

            match Session.selectLayer "layer-1" edited with
            | Ok(refreshed, patches) ->
                let layer1Value =
                    (Session.layerById "layer-1" refreshed).Model.PropertyValues.[analysisId].Value

                let layer2Value =
                    (Session.layerById "layer-2" refreshed).Model.PropertyValues.[analysisId].Value

                Expect.isEmpty patches "Focus refresh should not emit writeback patches."

                Expect.equal
                    layer1Value
                    (ProvenanceValue.Text "Imaging")
                    "Upstream layer should receive linked property edit."

                Expect.equal
                    layer2Value
                    (ProvenanceValue.Text "Imaging")
                    "Downstream layer should keep linked property edit."

                Expect.isEmpty refreshed.DirtyPropertyValueIds "Focus refresh should clear dirty property ids."
            | Error error -> failwithf "Unexpected selectLayer error: %A" error

        testCase "upstream property additions refresh into downstream referenced inputs on focus change"
        <| fun _ ->
            let layered =
                Session.init (sampleModel ())
                |> Session.addLayer {
                    SelectedSets = [ ProvenanceSide.Output, "output-d" ]
                }
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected addLayer error: %A" error

            let backToLayer1 =
                match Session.selectLayer "layer-1" layered with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected selectLayer error: %A" error

            let treatment = propertyHeader FixtureKinds.characteristicProperty "Treatment"

            let command = {
                Target = ProvenancePropertyTarget.OutputSets [ "output-d" ]
                CopiedFrom = None
                Header = treatment
                Value = ProvenanceValue.Text "Drought"
                Unit = None
            }

            let edited =
                match Session.createLoadedPropertyValue command backToLayer1 with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected createLoadedPropertyValue error: %A" error

            let layer2BeforeFocus = Session.layerById "layer-2" edited

            let inputBeforeFocus =
                layer2BeforeFocus.Model.InputSets |> Map.toList |> List.exactlyOne |> snd

            let valuesBeforeFocus =
                ProvenanceSet.effectivePropertyValueIds inputBeforeFocus
                |> List.choose (fun id -> layer2BeforeFocus.Model.PropertyValues.TryFind id)

            Expect.isFalse
                (valuesBeforeFocus |> List.exists (fun pv -> pv.Header = treatment))
                "Downstream input should not receive upstream structural additions before focus refresh."

            match Session.selectLayer "layer-2" edited with
            | Ok(refreshed, patches) ->
                let layer2 = Session.layerById "layer-2" refreshed
                let input = layer2.Model.InputSets |> Map.toList |> List.exactlyOne |> snd

                let values =
                    ProvenanceSet.effectivePropertyValueIds input
                    |> List.choose (fun id -> layer2.Model.PropertyValues.TryFind id)

                Expect.isEmpty patches "Focus refresh should not emit writeback patches."

                Expect.isTrue
                    (values |> List.exists (fun pv -> pv.Header = treatment))
                    "Downstream input should receive upstream property addition."
            | Error error -> failwithf "Unexpected selectLayer error: %A" error

        testCase "upstream property removals refresh into downstream referenced inputs on focus change"
        <| fun _ ->
            let layered =
                Session.init (sampleModel ())
                |> Session.addLayer {
                    SelectedSets = [ ProvenanceSide.Output, "output-d" ]
                }
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected addLayer error: %A" error

            let backToLayer1 =
                match Session.selectLayer "layer-1" layered with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected selectLayer error: %A" error

            let layer1 = Session.layerById "layer-1" backToLayer1
            let output = layer1.Model.OutputSets.["output-d"]
            let removedId = ProvenanceSet.effectivePropertyValueIds output |> List.head

            let trimmedOutput =
                if output.PropertyValueIds |> List.contains removedId then
                    {
                        output with
                            PropertyValueIds = output.PropertyValueIds |> List.filter ((<>) removedId)
                    }
                else
                    {
                        output with
                            InheritedPropertyValueIds =
                                output.InheritedPropertyValueIds
                                |> Map.map (fun _ ids -> ids |> List.filter ((<>) removedId))
                                |> Map.filter (fun _ ids -> not ids.IsEmpty)
                    }

            let trimmedLayer = {
                layer1 with
                    Model = {
                        layer1.Model with
                            OutputSets = layer1.Model.OutputSets |> Map.add output.Id trimmedOutput
                    }
            }

            let edited = {
                backToLayer1 with
                    Layers =
                        backToLayer1.Layers
                        |> List.map (fun layer -> if layer.Id = trimmedLayer.Id then trimmedLayer else layer)
            }

            let layer2BeforeFocus = Session.layerById "layer-2" edited

            let inputBeforeFocus =
                layer2BeforeFocus.Model.InputSets |> Map.toList |> List.exactlyOne |> snd

            Expect.isTrue
                (ProvenanceSet.effectivePropertyValueIds inputBeforeFocus
                 |> List.contains removedId)
                "Downstream input should keep removed upstream property ids until focus refresh."

            match Session.selectLayer "layer-2" edited with
            | Ok(refreshed, patches) ->
                let layer2 = Session.layerById "layer-2" refreshed
                let input = layer2.Model.InputSets |> Map.toList |> List.exactlyOne |> snd
                let inputIds = ProvenanceSet.effectivePropertyValueIds input

                Expect.isEmpty patches "Focus refresh should not emit writeback patches."

                Expect.isFalse
                    (inputIds |> List.contains removedId)
                    "Downstream input should drop removed upstream property ids."
            | Error error -> failwithf "Unexpected selectLayer error: %A" error

        testCase "connection-derived properties refresh downstream on focus change"
        <| fun _ ->
            let layered =
                Session.init (sampleModel ())
                |> Session.addLayer {
                    SelectedSets = [ ProvenanceSide.Output, "output-a" ]
                }
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected addLayer error: %A" error

            let backToLayer1 =
                match Session.selectLayer "layer-1" layered with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected selectLayer error: %A" error

            let command = {
                Target = ProvenancePropertyTarget.Connections [ "connection-a" ]
                CopiedFrom = None
                Header = propertyHeader FixtureKinds.parameterProperty "Analysis"
                Value = ProvenanceValue.Text "Microscopy"
                Unit = None
            }

            let edited =
                match Session.createLoadedPropertyValue command backToLayer1 with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected createLoadedPropertyValue error: %A" error

            let outputBeforeFocus =
                (Session.layerById "layer-1" edited).Model.OutputSets.["output-a"]

            let inputBeforeFocus =
                (Session.layerById "layer-2" edited).Model.InputSets.["layer-2-from-output-0-output-a"]

            Expect.notEqual
                inputBeforeFocus.PropertyValueIds
                outputBeforeFocus.PropertyValueIds
                "Downstream input should not mirror connection-derived upstream property changes before focus refresh."

            match Session.selectLayer "layer-2" edited with
            | Ok(refreshed, patches) ->
                let firstOutput =
                    (Session.layerById "layer-1" refreshed).Model.OutputSets.["output-a"]

                let nextInput =
                    (Session.layerById "layer-2" refreshed).Model.InputSets.["layer-2-from-output-0-output-a"]

                Expect.isEmpty patches "Focus refresh should not emit writeback patches."

                Expect.equal
                    nextInput.PropertyValueIds
                    firstOutput.PropertyValueIds
                    "Downstream input should mirror connection-derived upstream output properties after refresh."
            | Error error -> failwithf "Unexpected selectLayer error: %A" error

        testCase "focus refresh does not change unreferenced downstream snapshots"
        <| fun _ ->
            let layered =
                Session.init (sampleModel ())
                |> Session.addLayer {
                    SelectedSets = [ ProvenanceSide.Output, "output-a" ]
                }
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected addLayer error: %A" error

            let backToLayer1 =
                match Session.selectLayer "layer-1" layered with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected selectLayer error: %A" error

            let addedOutput =
                match
                    Session.createLoadedSet
                        {
                            Side = ProvenanceSide.Output
                            Header = ioHeader FixtureKinds.dataEndpoint "Output [Data]"
                            Name = "Unreferenced output"
                        }
                        backToLayer1
                with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected createLoadedSet error: %A" error

            match Session.selectLayer "layer-2" addedOutput with
            | Ok(refreshed, patches) ->
                let layer2 = Session.layerById "layer-2" refreshed

                Expect.isEmpty patches "Focus refresh should not emit writeback patches."

                Expect.equal
                    layer2.Model.InputSets.Count
                    1
                    "Only the referenced downstream input snapshot should remain present."

                Expect.isFalse
                    (layer2.Model.InputSets
                     |> Map.exists (fun _ set -> set.Name = "Unreferenced output"))
                    "Unreferenced upstream structural additions should not appear downstream."
            | Error error -> failwithf "Unexpected selectLayer error: %A" error

        testCase "assigning a palette value to a carried next input writes only to the active layer"
        <| fun _ ->
            let first = Session.init (sampleModel ())

            let layered =
                match
                    Session.addLayer
                        {
                            SelectedSets = [ ProvenanceSide.Output, "output-d" ]
                        }
                        first
                with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected addLayer error: %A" error

            let projectedId =
                (Session.activeLayer layered).Model.InputSets
                |> Map.toList
                |> List.exactlyOne
                |> fst

            let previousBefore =
                (Session.layerById "layer-1" layered).Model.OutputSets.["output-d"]

            let treatment = propertyHeader FixtureKinds.characteristicProperty "Treatment"

            let command = {
                Target = ProvenancePropertyTarget.InputSets [ projectedId ]
                CopiedFrom = None
                Header = treatment
                Value = ProvenanceValue.Text "Drought"
                Unit = None
            }

            match Session.createCurrentLoadedPropertyValue command layered with
            | Ok(next, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, _, header, value, _) ]) ->
                let previousOutput =
                    (Session.layerById "layer-1" next).Model.OutputSets.["output-d"]

                let nextInput = (Session.layerById "layer-2" next).Model.InputSets.[projectedId]

                Expect.equal
                    target
                    (ProvenancePropertyTarget.InputSets [ projectedId ])
                    "Palette drops should patch the active displayed target."

                Expect.equal header treatment "Patch should carry the palette property."
                Expect.equal value (ProvenanceValue.Text "Drought") "Patch should carry the palette value."

                Expect.equal
                    previousOutput.PropertyValueIds
                    previousBefore.PropertyValueIds
                    "Palette additions must not write through to the native boundary owner."

                Expect.isTrue
                    (nextInput.PropertyValueIds.Length > previousBefore.PropertyValueIds.Length)
                    "The active layer input should receive the new property value."
            | other -> failwithf "Expected one active-layer property-add patch, got %A" other

        testCase "updating multiple property values uses the edit path for every value"
        <| fun _ ->
            let session = Session.init (validModel ())

            let updates = [
                "pv-species-arabidopsis-a", ProvenanceValue.Text "A. thaliana", None
                "pv-species-arabidopsis-b", ProvenanceValue.Text "A. thaliana", None
            ]

            match Session.updatePropertyValues updates session with
            | Ok(next, patches) ->
                let model = (Session.activeLayer next).Model
                Expect.equal patches.Length 2 "Every existing value should produce an edit patch."

                Expect.equal
                    model.PropertyValues.["pv-species-arabidopsis-a"].Value
                    (ProvenanceValue.Text "A. thaliana")
                    "First value should be edited."

                Expect.equal
                    model.PropertyValues.["pv-species-arabidopsis-b"].Value
                    (ProvenanceValue.Text "A. thaliana")
                    "Second value should be edited."
            | other -> failwithf "Expected batched edit success, got %A" other

        testCase "creating and connecting a later output modifies only the active layer"
        <| fun _ ->
            let layered =
                Session.init (sampleModel ())
                |> Session.addLayer {
                    SelectedSets = [ ProvenanceSide.Output, "output-a" ]
                }
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "%A" error

            let outputHeader = ioHeader FixtureKinds.dataEndpoint "Output [Data]"

            let created =
                match
                    Session.createLoadedSet
                        {
                            Side = ProvenanceSide.Output
                            Header = outputHeader
                            Name = "Raw file"
                        }
                        layered
                with
                | Ok(next, _) -> next
                | Error error -> failwithf "%A" error

            let layer = Session.activeLayer created
            let inputId = layer.Model.InputSets |> Map.toList |> List.exactlyOne |> fst
            let outputId = layer.Model.OutputSets |> Map.toList |> List.exactlyOne |> fst

            match Session.connectSets inputId outputId None created with
            | Ok(next, [ ProvenanceTablePatch.AddLoadedConnection _ ]) ->
                Expect.equal
                    (Session.layerById "layer-1" next).Model.Connections.Count
                    5
                    "Prior transition should remain unchanged."

                Expect.equal
                    (Session.layerById "layer-2" next).Model.Connections.Count
                    1
                    "Later transition gets its connection."
            | other -> failwithf "Expected a later connection patch, got %A" other

        testCase "removeConnections removes loaded connections from the active layer"
        <| fun _ ->
            let session = sampleSession ()
            let initialCount = (Session.activeLayer session).Model.Connections.Count

            match Session.removeConnections [ "connection-a"; "connection-b"; "connection-a" ] session with
            | Ok(next, patches) ->
                Expect.equal patches.Length 2 "Duplicate ids should collapse to one patch per removed connection."

                Expect.all
                    patches
                    (function
                    | ProvenanceTablePatch.RemoveLoadedConnection _ -> true
                    | _ -> false)
                    "All emitted patches should remove loaded connections."

                Expect.equal
                    (Session.activeLayer next).Model.Connections.Count
                    (initialCount - 2)
                    "Both connections should be removed from the active layer."
            | Error error -> failwithf "Unexpected removeConnections error: %A" error

        testCase "init and addLayer work for an output-only model"
        <| fun _ ->
            let initial = Session.init (outputOnlyModel ())

            Expect.isEmpty
                (Session.activeLayer initial).Model.InputSets
                "Output-only input has no synthetic input endpoints."

            Expect.isNonEmpty (Session.activeLayer initial).Model.OutputSets "Real output endpoints remain visible."

            match Session.addLayer { SelectedSets = [] } initial with
            | Ok(next, patches) ->
                let layer = Session.activeLayer next
                Expect.isNonEmpty layer.Model.InputSets "Current outputs should seed the next displayed input side."
                Expect.isEmpty layer.Model.OutputSets "A newly displayed transition has no invented outputs."
                Expect.isEmpty patches "View-layer derivation is not a persistence edit."
            | Error error -> failwithf "Expected output-only layer derivation success, got %A" error

        testCase "adding a value to a removed connection returns a session error"
        <| fun _ ->
            let command = {
                Target = ProvenancePropertyTarget.Connections [ "missing-connection" ]
                CopiedFrom = None
                Header = propertyHeader FixtureKinds.parameterProperty "Analysis"
                Value = ProvenanceValue.Text "Microscopy"
                Unit = None
            }

            match Session.createLoadedPropertyValue command (Session.init (sampleModel ())) with
            | Error(SessionError.EditFailed(EditError.ConnectionNotFound connectionId)) ->
                Expect.equal connectionId "missing-connection" "Removed target identity should be returned."
            | other -> failwithf "Expected missing-connection session error, got %A" other
    ]

let uiStateTests =
    testList "UI state" [
        testCase "initial UI state creates side states for the active layer sides"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let state = State.init session

            Expect.isTrue (state.SideStates.ContainsKey layer.InputSideId) "Input side state should exist."
            Expect.isTrue (state.SideStates.ContainsKey layer.OutputSideId) "Output side state should exist."
            Expect.equal state.SideStates.Count 2 "Initial layer should create exactly two side states."

        testCase "new layer side states do not inherit grouping assignments"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer1 = Session.activeLayer session
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"

            let grouped =
                State.init session
                |> State.GroupingAssignments.toggleSide layer1.OutputSideId ProvenanceSide.Output replicate

            let layered =
                match
                    Session.addLayer
                        {
                            SelectedSets = [ ProvenanceSide.Output, "output-a" ]
                        }
                        session
                with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected addLayer error: %A" error

            let layer2 = Session.activeLayer layered
            let nextState = State.Sides.ensure layered grouped

            Expect.equal
                (State.Sides.get layer2.InputSideId nextState).GroupingAssignments
                []
                "New layer input side should not inherit upstream grouping assignments."

            Expect.equal
                (State.Sides.get layer2.OutputSideId nextState).GroupingAssignments
                []
                "New layer output side should not inherit upstream grouping assignments."

        testCase "toggleSideGrouping applies grouping only to the selected layer side"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let state = State.init session
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"

            let next =
                State.GroupingAssignments.toggleSide layer.OutputSideId ProvenanceSide.Output replicate state

            Expect.equal
                (State.Sides.get layer.InputSideId next).GroupingAssignments
                []
                "Side-only output grouping should not change the input layer state."

            Expect.equal
                (State.Sides.get layer.OutputSideId next).GroupingAssignments
                [
                    {
                        Key = { Header = replicate }
                        Scope = GroupingScope.Output
                    }
                ]
                "Side-only output grouping should be stored as an output-scoped assignment."

        testCase "toggleBothGrouping applies grouping to both active layer states"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let state = State.init session
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"

            let next =
                State.GroupingAssignments.toggleBoth layer.InputSideId layer.OutputSideId replicate state

            let expected = [
                {
                    Key = { Header = replicate }
                    Scope = GroupingScope.Both
                }
            ]

            Expect.equal
                (State.Sides.get layer.InputSideId next).GroupingAssignments
                expected
                "Both-side grouping should be visible from the input layer."

            Expect.equal
                (State.Sides.get layer.OutputSideId next).GroupingAssignments
                expected
                "Both-side grouping should be visible from the output layer."

        testCase "moveGrouping switches a property to the target side only"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let state = State.init session
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"

            let selected =
                State.GroupingAssignments.toggleSide layer.InputSideId ProvenanceSide.Input species state

            let next =
                State.GroupingAssignments.move
                    layer.Id
                    layer.InputSideId
                    layer.OutputSideId
                    ProvenanceSide.Output
                    species
                    selected

            Expect.equal
                (State.Sides.get layer.InputSideId next).GroupingAssignments
                []
                "Dragging a property away should remove it from the source layer."

            Expect.equal
                (State.Sides.get layer.OutputSideId next).GroupingAssignments
                [
                    {
                        Key = { Header = species }
                        Scope = GroupingScope.Output
                    }
                ]
                "Dragging a property to output should make it output-only."

            Expect.equal
                (next.PropertyRailPlacements |> Map.tryFind (layer.Id, { Header = species }))
                (Some ProvenanceSide.Output)
                "Dragging a property to output should move its rail control to the output side."

        testCase "toggleBothGrouping removes only both-scope assignments from inconsistent state"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let state = State.init session
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let key = { Header = species }

            let sideAssignment = {
                Key = key
                Scope = GroupingScope.Input
            }

            let bothAssignment = {
                Key = key
                Scope = GroupingScope.Both
            }

            let inconsistent = {
                state with
                    SideStates =
                        state.SideStates
                        |> Map.add layer.InputSideId {
                            GroupingAssignments = [ sideAssignment; bothAssignment ]
                        }
                        |> Map.add layer.OutputSideId {
                            GroupingAssignments = [ bothAssignment ]
                        }
            }

            let next =
                State.GroupingAssignments.toggleBoth layer.InputSideId layer.OutputSideId species inconsistent

            Expect.equal
                (State.Sides.get layer.InputSideId next).GroupingAssignments
                [ sideAssignment ]
                "Removing a both-side grouping should not silently drop an existing side-specific assignment for the same key."

            Expect.equal
                (State.Sides.get layer.OutputSideId next).GroupingAssignments
                []
                "Only the both-side assignment should be removed from the opposite layer."

        testCase "panel layout clamps side panels and preserves a middle panel"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let state = State.init session

            let tooSmallLeft =
                State.PanelLayout.setLeft layer.Id 2 state |> State.PanelLayout.get layer.Id

            Expect.equal tooSmallLeft.Left 15 "Left panel should not shrink below the minimum."
            Expect.equal tooSmallLeft.Middle 65 "Middle panel should keep the remaining usable width."
            Expect.equal tooSmallLeft.Right 20 "Unchanged right panel should stay at the default width."

            let tooLargeRight =
                state
                |> State.PanelLayout.setLeft layer.Id 45
                |> State.PanelLayout.setRight layer.Id 80
                |> State.PanelLayout.get layer.Id

            Expect.equal tooLargeRight.Left 45 "Left panel should retain the committed user ratio."
            Expect.equal tooLargeRight.Right 25 "Right panel should be clamped so the middle panel keeps its minimum."
            Expect.equal tooLargeRight.Middle 30 "Middle panel must not shrink below its minimum."

        testCase "connection handle identities round-trip through drag and drop ids"
        <| fun _ ->
            let handle: Types.ConnectionHandleRef = {
                Kind = Types.ConnectionHandleKind.GroupMember
                Side = ProvenanceSide.Input
                Id = "input-a"
                ParentGroupId = Some "input:Species=Arabidopsis"
            }

            Expect.equal
                (DragDrop.connectionHandleDragId handle |> DragDrop.tryDragId)
                (Some(DragDrop.Payload.ConnectionHandle handle))
                "Connection drag id should parse back to the same handle reference."

            Expect.equal
                (DragDrop.connectionHandleDropId handle |> DragDrop.tryConnectionDropId)
                (Some handle)
                "Connection drop id should parse back to the same handle reference."

        testCase "connection routing accepts compatible group and member targets only"
        <| fun _ ->
            let group side id : Types.ConnectionHandleRef = {
                Kind = Types.ConnectionHandleKind.GroupCard
                Side = side
                Id = id
                ParentGroupId = None
            }

            let memberHandle side parent id : Types.ConnectionHandleRef = {
                Kind = Types.ConnectionHandleKind.GroupMember
                Side = side
                Id = id
                ParentGroupId = Some parent
            }

            Expect.equal
                (ConnectionRouting.action
                    (group ProvenanceSide.Input "input:input-a")
                    (group ProvenanceSide.Output "output:output-z"))
                (Some(ConnectionRouting.ConnectionAction.ConnectGroups("input:input-a", "output:output-z")))
                "Input group handles should connect to output group handles."

            Expect.equal
                (ConnectionRouting.action
                    (group ProvenanceSide.Input "input:input-a")
                    (group ProvenanceSide.Input "input:input-b"))
                None
                "Same-side group handles should be rejected."

            Expect.equal
                (ConnectionRouting.action
                    (memberHandle ProvenanceSide.Input "input:g" "input-a")
                    (memberHandle ProvenanceSide.Output "output:g" "output-a"))
                (Some(ConnectionRouting.ConnectionAction.ConnectMembers("input:g", "output:g", "input-a", "output-a")))
                "Opposite-side member handles should create a manual member connection action."

        testCase "connection routing ignores property value and property header handles"
        <| fun _ ->
            let handle kind side id : Types.ConnectionHandleRef = {
                Kind = kind
                Side = side
                Id = id
                ParentGroupId = None
            }

            let valueHandle =
                handle Types.ConnectionHandleKind.PropertyValue ProvenanceSide.Input "pv-input-a-species"

            let headerHandle =
                handle Types.ConnectionHandleKind.PropertyHeader ProvenanceSide.Input "species"

            let sameSideGroup =
                handle Types.ConnectionHandleKind.GroupCard ProvenanceSide.Input "input:input-b"

            let oppositeGroup =
                handle Types.ConnectionHandleKind.GroupCard ProvenanceSide.Output "output:output-a"

            Expect.equal
                (ConnectionRouting.action valueHandle sameSideGroup)
                None
                "Value chip assignment is handled by value drag/drop, not connector routing."

            Expect.equal
                (ConnectionRouting.action valueHandle oppositeGroup)
                None
                "Value chip handles should not connect across sides."

            Expect.equal
                (ConnectionRouting.action headerHandle sameSideGroup)
                None
                "Property headers have no drag connection; their connectors derive from model data."

        testCase "member resolution prompt can be converted into a manual resolution layer"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session

            let pending: Types.PendingMemberResolution = {
                LayerId = layer.Id
                InputGroupId = "input:Species=Arabidopsis"
                OutputGroupId = "output:Analysis=Mass Spectrometry"
                InputMemberCount = 3
                OutputMemberCount = 1
            }

            let requested = State.init session |> State.MemberResolution.request pending

            Expect.equal
                requested.PendingMemberResolution
                (Some pending)
                "Mismatched group drops should store a pending user choice."

            let manual = requested |> State.MemberResolution.chooseManual pending

            Expect.equal manual.PendingMemberResolution None "Choosing manual should close the prompt."

            Expect.isTrue
                (State.MemberResolution.isManualPair layer.Id pending.InputGroupId pending.OutputGroupId manual)
                "Choosing manual should keep the layer expanded for member-level resolution."

            Expect.equal
                manual.Detail
                (Some(Types.ProvenanceDetail.Group(ProvenanceSide.Input, pending.InputGroupId)))
                "Choosing manual should expand the input group and allow the output layer to auto-expand from manual state."

        testCase "property value drop plan adds only when every group member has no value for the property"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let treatment = propertyHeader FixtureKinds.characteristicProperty "Treatment"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let model =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-species" species (ProvenanceValue.Text "Arabidopsis") None None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" []
                    ] [] []

            let group: DisplayGroup = {
                Id = "manual"
                TableName = "assay-table"
                Side = ProvenanceSide.Input
                GroupingValues = []
                Members = [
                    {
                        SetId = "input-a"
                        Name = "Input A"
                        PropertyValueIds = [ "pv-input-a-species" ]
                    }
                    {
                        SetId = "input-b"
                        Name = "Input B"
                        PropertyValueIds = []
                    }
                ]
            }

            let source: Types.ValueAssignmentSource = {
                CopiedFrom = None
                Header = treatment
                Value = ProvenanceValue.Text "Drought"
                Unit = None
            }

            match ValueAssignment.planPropertyValueDrop source group model with
            | Ok(Types.ValueAssignmentPlan.AddCurrent command) ->
                Expect.equal
                    command.Target
                    (ProvenancePropertyTarget.InputSets [ "input-a"; "input-b" ])
                    "Add plan should target all current group members."

                Expect.equal command.Header treatment "Add plan should preserve the dropped property."
            | other -> failwithf "Expected an add plan, got %A" other

            let mixedSource: Types.ValueAssignmentSource = { source with Header = species }

            match ValueAssignment.planPropertyValueDrop mixedSource group model with
            | Error(Types.ValueAssignmentError.MixedPropertyValueCounts header) ->
                Expect.equal header species "Mixed zero/one members should reject the drop for that property."
            | other -> failwithf "Expected a mixed-count rejection, got %A" other

        testCase "property value drop plan warns only when every group member has exactly one value"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let model =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-species" species (ProvenanceValue.Text "Arabidopsis") None None
                        propertyValue "pv-input-b-species" species (ProvenanceValue.Text "Chlamydomonas") None None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-input-b-species" ]
                    ] [] []

            let group: DisplayGroup = {
                Id = "manual"
                TableName = "assay-table"
                Side = ProvenanceSide.Input
                GroupingValues = []
                Members = [
                    {
                        SetId = "input-a"
                        Name = "Input A"
                        PropertyValueIds = [ "pv-input-a-species" ]
                    }
                    {
                        SetId = "input-b"
                        Name = "Input B"
                        PropertyValueIds = [ "pv-input-b-species" ]
                    }
                ]
            }

            let source: Types.ValueAssignmentSource = {
                CopiedFrom = None
                Header = species
                Value = ProvenanceValue.Text "A. thaliana"
                Unit = None
            }

            match ValueAssignment.planPropertyValueDrop source group model with
            | Ok(Types.ValueAssignmentPlan.ConfirmOverwrite warning) ->
                Expect.equal
                    warning.ExistingValueIds
                    [ "pv-input-a-species"; "pv-input-b-species" ]
                    "Overwrite warning should target the one editable value per member."

                Expect.equal warning.Header species "Warning should keep the overwritten property."
            | other -> failwithf "Expected an overwrite warning, got %A" other

        testCase "property value drop plan rejects members with multiple values for the same property"
        <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let model =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-species" species (ProvenanceValue.Text "Arabidopsis") None None
                        propertyValue "pv-input-b-species-a" species (ProvenanceValue.Text "Arabidopsis") None None
                        propertyValue "pv-input-b-species-b" species (ProvenanceValue.Text "Chlamydomonas") None None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" [
                            "pv-input-b-species-a"
                            "pv-input-b-species-b"
                        ]
                    ] [] []

            let group: DisplayGroup = {
                Id = "manual"
                TableName = "assay-table"
                Side = ProvenanceSide.Input
                GroupingValues = []
                Members = [
                    {
                        SetId = "input-a"
                        Name = "Input A"
                        PropertyValueIds = [ "pv-input-a-species" ]
                    }
                    {
                        SetId = "input-b"
                        Name = "Input B"
                        PropertyValueIds = [ "pv-input-b-species-a"; "pv-input-b-species-b" ]
                    }
                ]
            }

            let source: Types.ValueAssignmentSource = {
                CopiedFrom = None
                Header = species
                Value = ProvenanceValue.Text "A. thaliana"
                Unit = None
            }

            match ValueAssignment.planPropertyValueDrop source group model with
            | Error(Types.ValueAssignmentError.MultiplePropertyValues(header, setIds)) ->
                Expect.equal header species "Multi-value members should reject the drop for that property."
                Expect.equal setIds [ "input-b" ] "The rejection should identify the multi-value member."
            | other -> failwithf "Expected a multiple-value rejection, got %A" other

        testCase "default state initializes with empty colors and filters"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let state = State.init session

            Expect.equal
                state.PropertyColors
                State.PropertyColors.empty
                "Initial state should have empty property colors."

            Expect.equal state.Filters State.Filters.defaultState "Initial state should have default filters."

        testCase "set and clear property color"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let state = State.init session
            let header = propertyHeader FixtureKinds.characteristicProperty "Species"

            let withColor = State.PropertyColors.setColor header "#2563eb" state
            let key = { Header = header }

            Expect.equal
                withColor.PropertyColors.ManualPropertyColors.[key]
                "#2563eb"
                "Property color should be set by header."

            let cleared = State.PropertyColors.clearColor header withColor

            Expect.isFalse
                (cleared.PropertyColors.ManualPropertyColors.ContainsKey key)
                "Property color should be cleared."

        testCase "set and clear layer color"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let state = State.init session

            let withColor = State.PropertyColors.setLayerColor "layer-1" "#16a34a" state
            Expect.equal withColor.PropertyColors.LayerColors.["layer-1"] "#16a34a" "Layer color should be set."

            let cleared = State.PropertyColors.clearLayerColor "layer-1" withColor
            Expect.isFalse (cleared.PropertyColors.LayerColors.ContainsKey "layer-1") "Layer color should be cleared."

        testCase "layer colors are assigned once per conceptual layer"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let state = State.init session

            let afterEnsure = State.PropertyColors.ensureLayerColors session state

            Expect.equal
                afterEnsure.LayerColors.Count
                session.LayerOrder.Length
                "Each conceptual layer should receive exactly one automatic color."

            Expect.isTrue
                (afterEnsure.LayerColors.ContainsKey "layer-1")
                "Initial conceptual layer should have an automatic color."

            Expect.isFalse
                (afterEnsure.LayerColors.ContainsKey "layer-1-input"
                 || afterEnsure.LayerColors.ContainsKey "layer-1-output")
                "Input and output side ids should not receive separate default colors."

        testCase "automatic colors do not overwrite existing manual layer colors"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let state = State.init session

            let withManual = State.PropertyColors.setLayerColor "layer-1" "#be185d" state
            let afterEnsure = State.PropertyColors.ensureLayerColors session withManual

            Expect.equal afterEnsure.LayerColors.["layer-1"] "#be185d" "Manual layer color should be preserved."

        testCase "stale layer colors are removed during cleanup"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let state = State.init session

            let withStale = {
                state with
                    PropertyColors = {
                        state.PropertyColors with
                            LayerColors = Map.ofList [ "nonexistent", "#000000" ]
                    }
            }

            let afterEnsure = State.PropertyColors.ensureLayerColors session withStale

            Expect.isFalse (afterEnsure.LayerColors.ContainsKey "nonexistent") "Stale layer color should be removed."

        testCase "default filter state uses Any filter and ValueCountDesc sort"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let state = State.init session

            Expect.equal state.Filters.SearchText "" "Default search should be empty."

            Expect.equal
                state.Filters.PropertySort
                Types.PropertySort.ValueCountDesc
                "Default sort should be value count desc."

            Expect.equal
                state.Filters.ValueCountFilter
                Types.PropertyValueCountFilter.Any
                "Default value count filter should be Any."

        testCase "filters can be updated through state helpers"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let state = State.init session

            let withSearch = State.Filters.setSearch "Arabidopsis" state
            Expect.equal withSearch.Filters.SearchText "Arabidopsis" "Search text should be updated."

            let withSort = State.Filters.setPropertySort Types.PropertySort.NameAsc withSearch
            Expect.equal withSort.Filters.PropertySort Types.PropertySort.NameAsc "Property sort should be updated."

            let withFilter =
                State.Filters.setValueCountFilter Types.PropertyValueCountFilter.Multiple withSort

            Expect.equal
                withFilter.Filters.ValueCountFilter
                Types.PropertyValueCountFilter.Multiple
                "Value count filter should be updated."

            let withOrigin =
                State.Filters.setOriginFilter Types.PropertyOriginFilter.CurrentOnly withFilter

            Expect.equal
                withOrigin.Filters.OriginFilter
                Types.PropertyOriginFilter.CurrentOnly
                "Origin filter should be updated."

            let withGroupSort =
                State.Filters.setGroupSort Types.GroupSort.MemberCountDesc withOrigin

            Expect.equal withGroupSort.Filters.GroupSort Types.GroupSort.MemberCountDesc "Group sort should be updated."

        testCase "value count sort keeps distinct value count as primary key"
        <| fun _ ->
            let highCount = propertyHeader (ProvenanceKind.create "z-kind" "Zeta kind") "Zeta"
            let lowCount = propertyHeader (ProvenanceKind.create "a-kind" "Alpha kind") "Alpha"

            let stats =
                Map.ofList [
                    highCount,
                    ({
                        Header = highCount
                        DistinctValueCount = 3
                        SetsWithValueCount = 3
                        TotalSetCount = 3
                    }
                    : Types.PropertyStats)
                    lowCount,
                    ({
                        Header = lowCount
                        DistinctValueCount = 1
                        SetsWithValueCount = 1
                        TotalSetCount = 1
                    }
                    : Types.PropertyStats)
                ]

            let sorted =
                PropertyProjection.sortHeaders Types.PropertySort.ValueCountDesc stats [ lowCount; highCount ]

            Expect.equal sorted [ highCount; lowCount ] "Higher value count should sort before name/kind."

        testCase "rail projection applies search and resolves manual property color"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"

            let uiState =
                State.init session
                |> State.Filters.setSearch "Arabidopsis"
                |> State.PropertyColors.setColor species "#2563eb"

            let projection =
                PropertyProjection.railProjectionWithFilters session layer.Id ProvenanceSide.Output layer.Model uiState

            Expect.contains projection.Headers species "Search should keep headers with matching projected values."
            Expect.equal projection.ColorByHeader.[species] (Some "#2563eb") "Manual color should be projected."

            let nonMatching =
                uiState |> State.Filters.setSearch "definitely-not-a-provenance-value"

            let emptyProjection =
                PropertyProjection.railProjectionWithFilters
                    session
                    layer.Id
                    ProvenanceSide.Output
                    layer.Model
                    nonMatching

            Expect.isEmpty emptyProjection.Headers "Search should remove non-matching property headers."

        testCase "current input properties default to output rail when connected to outputs"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let state = State.init session
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"

            let inputHeaders =
                (PropertyProjection.railProjectionWithFilters session layer.Id ProvenanceSide.Input layer.Model state)
                    .Headers

            let outputHeaders =
                (PropertyProjection.railProjectionWithFilters session layer.Id ProvenanceSide.Output layer.Model state)
                    .Headers

            Expect.isFalse
                (inputHeaders |> List.contains species)
                "A connected current input property should not be duplicated on the input rail by default."

            Expect.contains
                outputHeaders
                species
                "A connected current input property should default to the output rail."

        testCase "previous context properties stay on input rail even when inherited by outputs"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let state = State.init session

            let previousTreatment =
                propertyHeader FixtureKinds.characteristicProperty "Previous Treatment"

            let inputHeaders =
                (PropertyProjection.railProjectionWithFilters session layer.Id ProvenanceSide.Input layer.Model state)
                    .Headers

            let outputHeaders =
                (PropertyProjection.railProjectionWithFilters session layer.Id ProvenanceSide.Output layer.Model state)
                    .Headers

            Expect.contains inputHeaders previousTreatment "A previous-context property should stay on the input rail."

            Expect.isFalse
                (outputHeaders |> List.contains previousTreatment)
                "A previous-context property should not move to the output rail just because it is inherited."

        testCase "current input properties remain on input rail until an output is designated"
        <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"

            let model =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-species" species (ProvenanceValue.Text "Arabidopsis") None None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species" ]
                    ] [] []

            let session = Session.init model
            let layer = Session.activeLayer session
            let state = State.init session

            let inputHeaders =
                (PropertyProjection.railProjectionWithFilters session layer.Id ProvenanceSide.Input layer.Model state)
                    .Headers

            let outputHeaders =
                (PropertyProjection.railProjectionWithFilters session layer.Id ProvenanceSide.Output layer.Model state)
                    .Headers

            Expect.contains
                inputHeaders
                species
                "An incomplete layer with no output should keep current properties on the input rail."

            Expect.isFalse
                (outputHeaders |> List.contains species)
                "A property should not move to the output rail before any output is designated."

        testCase "selected drop targets include selected inputs and outputs only when dropped group is selected"
        <| fun _ ->
            let inputGroup: DisplayGroup = {
                Id = "input-group"
                TableName = "assay-table"
                Side = ProvenanceSide.Input
                GroupingValues = []
                Members = []
            }

            let outputGroup: DisplayGroup = {
                inputGroup with
                    Id = "output-group"
                    Side = ProvenanceSide.Output
            }

            let findGroup side groupId =
                match side, groupId with
                | ProvenanceSide.Input, "input-group" -> Some inputGroup
                | ProvenanceSide.Output, "output-group" -> Some outputGroup
                | _ -> None

            let selectedInputs = Set.ofList [ "layer-1", "input-group" ]
            let selectedOutputs = Set.ofList [ "layer-1", "output-group" ]

            let selectedTargets =
                ValueAssignment.selectedTargetGroupsForDrop
                    "layer-1"
                    ProvenanceSide.Input
                    "input-group"
                    selectedInputs
                    selectedOutputs
                    findGroup

            Expect.equal
                (selectedTargets |> List.map (fun (group: DisplayGroup) -> group.Side, group.Id))
                [
                    ProvenanceSide.Input, "input-group"
                    ProvenanceSide.Output, "output-group"
                ]
                "Dropping onto a selected group should target selected groups on both sides."

            let unselectedDrop =
                ValueAssignment.selectedTargetGroupsForDrop
                    "layer-1"
                    ProvenanceSide.Input
                    "unselected-input"
                    selectedInputs
                    selectedOutputs
                    (fun side groupId ->
                        if side = ProvenanceSide.Input && groupId = "unselected-input" then
                            Some {
                                inputGroup with
                                    Id = "unselected-input"
                            }
                        else
                            findGroup side groupId
                    )

            Expect.equal
                (unselectedDrop |> List.map (fun (group: DisplayGroup) -> group.Id))
                [ "unselected-input" ]
                "Dropping onto an unselected group should keep single-target behavior."
    ]

let sourceTests =
    testList "Source and origin" [
        testCase "property source info exposes table and process metadata"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let value = layer.Model.PropertyValues.["pv-input-a-species"]
            let source = Session.propertyValueSourceInfo layer value

            Expect.isSome source "A fixture property value should expose source metadata."

        testCase "property source info falls back to loaded membership"
        <| fun _ ->
            let session = Session.init (sampleModel ())
            let layer = Session.activeLayer session
            let original = layer.Model.PropertyValues.["pv-input-a-species"]
            let withoutSource = { original with Source = None }
            let source = Session.propertyValueSourceInfo layer withoutSource

            Expect.equal
                (source |> Option.bind _.TableName)
                (Some layer.Model.LoadedTableName)
                "Membership fallback should keep current table context."

        testCase "property origin uses conceptual upstream layer id"
        <| fun _ ->
            let session = Session.init (sampleModel ())

            match
                Session.addLayer
                    {
                        SelectedSets = [ ProvenanceSide.Output, "output-a" ]
                    }
                    session
            with
            | Ok(layered, _) ->
                let active = Session.activeLayer layered
                let propertyValueId = active.Model.PropertyValues |> Map.toList |> List.head |> fst

                let origin =
                    Session.propertyValueOriginInSession active.Id ProvenanceSide.Input propertyValueId layered

                Expect.equal
                    origin
                    (Some(PropertyOrigin.UpstreamLayer "layer-1"))
                    "Origin should report conceptual upstream layer id."
            | Error error -> failwithf "Unexpected addLayer error: %A" error
    ]

let tests =
    testList "ProvenanceGrouping" [
        typeTests
        modelTests
        groupingTests
        editTests
        fixtureTests
        sessionTests
        sourceTests
        uiStateTests
    ]
