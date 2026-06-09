module ProvenanceGroupingTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Fixtures
open Swate.Components.Shared.ProvenanceGrouping.Session

module ProvenanceGroupingState = Swate.Components.Composite.ProvenanceGrouping.State
module ProvenanceGroupingValueAssignment = Swate.Components.Composite.ProvenanceGrouping.ValueAssignment
module ProvenanceGroupingTypes = Swate.Components.Composite.ProvenanceGrouping.Types

let typeTests =
    testList "Types" [
        testCase "loaded input set carries the actual input name" <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputSet =
                {
                    Id = "input-a"
                    TableName = "assay-table"
                    Header = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
                    Name = "Input A"
                    PropertyValueIds = [ "pv-species-a" ]
                    InheritedPropertyValueIds = Map.empty
                }

            let propertyValue =
                {
                    Id = "pv-species-a"
                    Header = species
                    Value = ProvenanceValue.Text "Arabidopsis"
                    Unit = None
                    Source =
                        Some
                            {
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
            | None ->
                failwith "Expected collapsed value source."

        testCase "inherited property helpers replace and remove connection-specific pointers" <| fun _ ->
            let inputSet =
                {
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

            let replaced =
                inherited
                |> ProvenanceSet.inheritPropertyValueIds "connection-a" []

            Expect.isFalse
                (replaced.InheritedPropertyValueIds.ContainsKey "connection-a")
                "Empty inherited IDs should remove the connection entry."

            let removed =
                inherited
                |> ProvenanceSet.removeInheritedPropertyValueIds "connection-a"

            Expect.isFalse
                (removed.InheritedPropertyValueIds.ContainsKey "connection-a")
                "Explicit removal should drop only the requested connection entry."

        testCase "adapter endpoint and property kinds keep the editor model source agnostic" <| fun _ ->
            let customEndpointKind = ProvenanceKind.create "external:endpoint:plate-well" "Plate well"
            let customPropertyKind = ProvenanceKind.create "external:property:quality-score" "Quality Score"
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
                    ]
                    []
                    []

            let input = built.InputSets.["input-well-a1"]
            let value = built.PropertyValues.["pv-quality"]

            Expect.equal input.Header.Kind customEndpointKind "Endpoint kind should not require an ISA IO type."
            Expect.equal value.Header.Kind customPropertyKind "Property kind should not require an ISA property family."
            Expect.equal input.Name "A1" "Custom process endpoints should still use ProvenanceSet.Name for display."
    ]

let modelTests =
    testList "Model" [
        testCase "direct model builder preserves loaded names and repeated distinct property values" <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-species-a" species (ProvenanceValue.Text "Arabidopsis") None (Some(anchor "previous-table" (Some "previous-process") species [ "Ancestor A" ] []))
                        propertyValue "pv-rep-1" replicate (ProvenanceValue.Text "1") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                        propertyValue "pv-rep-2" replicate (ProvenanceValue.Text "2") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a"; "pv-rep-1"; "pv-rep-2" ]
                    ]
                    [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-rep-1"; "pv-rep-2" ]
                    ]
                    [
                        connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                    ]

            Expect.equal built.InputSets.["input-a"].Name "Input A" "Direct model should keep the loaded input name."
            Expect.equal built.OutputSets.["output-a"].Name "Output A" "Direct model should keep the loaded output name."
            Expect.equal built.PropertyValues.Count 3 "Distinct repeated values should remain separate model values."

        testCase "direct model builder keeps previous-context anchors without previous first-class sets" <| fun _ ->
            let previousTreatment = propertyHeader FixtureKinds.characteristicProperty "Previous Treatment"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-previous-treatment-a" previousTreatment (ProvenanceValue.Text "Drought") None (Some(anchor "previous-study-table" (Some "previous-process") previousTreatment [ "Ancestor A" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-previous-treatment-a" ]
                    ]
                    []
                    []

            let value = built.PropertyValues.["pv-previous-treatment-a"]
            Expect.equal built.InputSets.Count 1 "Only loaded sets should be first-class sets."

            match value.Source with
            | Some source ->
                Expect.equal source.TableName "previous-study-table" "Collapsed previous-context values should keep their source table."
            | None ->
                failwith "Expected a previous-context source anchor."
    ]

let private validModel () =
    let species = propertyHeader FixtureKinds.characteristicProperty "Species"
    let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
    let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
    let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

    model
        "assay-table"
        [
            propertyValue "pv-species-arabidopsis-a" species (ProvenanceValue.Text "Arabidopsis") None (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
            propertyValue "pv-species-arabidopsis-b" species (ProvenanceValue.Text "Arabidopsis") None (Some(anchor "assay-table" (Some "assay-process") species [ "Input B" ] []))
            propertyValue "pv-rep-output-b-1" replicate (ProvenanceValue.Text "1") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output B" ]))
            propertyValue "pv-rep-output-b-2" replicate (ProvenanceValue.Text "2") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input B" ] [ "Output B" ]))
        ]
        [
            inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-arabidopsis-a" ]
            inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-species-arabidopsis-b" ]
            inputSet "input-c" "assay-table" inputHeader "Input C" []
        ]
        [
            outputSet "output-a" "assay-table" outputHeader "Output A" []
            outputSet "output-b" "assay-table" outputHeader "Output B" [ "pv-rep-output-b-1"; "pv-rep-output-b-2" ]
            outputSet "output-c" "assay-table" outputHeader "Output C" []
        ]
        [
            connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
            connection "connection-b" "assay-table" (Some "assay-process") "input-a" "output-b"
            connection "connection-c" "assay-table" (Some "assay-process") "input-b" "output-b"
            connection "connection-d" "assay-table" (Some "assay-process") "input-c" "output-c"
        ]

let groupingTests =
    testList "Grouping" [
        testCase "no grouping displays each loaded set by name" <| fun _ ->
            let model = validModel ()
            let groups = displayGroups model ProvenanceSide.Input []

            Expect.equal (groups |> List.map (fun group -> group.Members.Head.Name)) [ "Input A"; "Input B"; "Input C" ] "No grouping should preserve loaded input names."

        testCase "multi-value grouping uses the complete value set as one group key" <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-species-chlamy" species (ProvenanceValue.Text "Chlamydomonas") None None
                        propertyValue "pv-input-b-species-arabidopsis" species (ProvenanceValue.Text "Arabidopsis") None None
                        propertyValue "pv-input-c-species-arabidopsis" species (ProvenanceValue.Text "Arabidopsis") None None
                        propertyValue "pv-input-c-species-chlamy" species (ProvenanceValue.Text "Chlamydomonas") None None
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species-chlamy" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-input-b-species-arabidopsis" ]
                        inputSet "input-c" "assay-table" inputHeader "Input C" [ "pv-input-c-species-arabidopsis"; "pv-input-c-species-chlamy" ]
                    ]
                    []
                    []

            let groups = displayGroups built ProvenanceSide.Input [ { Header = species } ]

            let membersByTitle =
                groups
                |> List.map (fun group ->
                    let title =
                        group.GroupingValues
                        |> List.map (fun value ->
                            match value.Value with
                            | ProvenanceValue.Text text -> text
                            | other -> string other)
                        |> List.sort
                        |> String.concat ", "
                    title, group.Members |> List.map (fun member' -> member'.SetId) |> List.sort)
                |> Map.ofList

            Expect.equal
                (membersByTitle |> Map.toList |> List.map fst)
                [ "Arabidopsis"; "Arabidopsis, Chlamydomonas"; "Chlamydomonas" ]
                "Grouping by a multi-valued property should create one group for the complete value set."
            Expect.isTrue
                (groups |> List.exists (fun group -> group.Id = "input:Species=Arabidopsis | Chlamydomonas"))
                "A multi-valued group id should print the grouping key once and join values with ' | '."
            Expect.equal membersByTitle.["Arabidopsis"] [ "input-b" ] "The multi-valued set must not appear in the Arabidopsis-only group."
            Expect.equal membersByTitle.["Chlamydomonas"] [ "input-a" ] "The multi-valued set must not appear in the Chlamydomonas-only group."
            Expect.equal membersByTitle.["Arabidopsis, Chlamydomonas"] [ "input-c" ] "The multi-valued set should appear in its combined value-set group."

        testCase "grouped members retain non-grouping property values for editing" <| fun _ ->
            let model = sampleModel ()
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let groups = displayGroups model ProvenanceSide.Input [ { Header = species } ]
            let inputA =
                groups
                |> List.collect (fun group -> group.Members)
                |> List.find (fun member' -> member'.SetId = "input-a")

            Expect.contains
                inputA.PropertyValueIds
                "pv-input-a-temperature"
                "Grouping by Species must not hide Temperature from the member editing surface."

        testCase "outputs expose input properties inherited through current loaded connections" <| fun _ ->
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
                    ]
                    [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-analysis" ]
                    ]
                    [
                        connection "connection-a" "assay-table" None "input-a" "output-a"
                        connection "connection-b" "assay-table" None "input-b" "output-a"
                    ]

            let outputA =
                displayGroups built ProvenanceSide.Output []
                |> List.collect (fun group -> group.Members)
                |> List.find (fun member' -> member'.SetId = "output-a")

            Expect.equal
                (outputA.PropertyValueIds |> List.sort)
                [ "pv-input-a-species"; "pv-input-b-temperature"; "pv-output-a-analysis" ]
                "An output should expose its own properties plus properties inherited from all directly connected loaded inputs."

        testCase "output inheritance follows the current loaded connection set" <| fun _ ->
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
                build [ connection "connection-a" "assay-table" None "input-a" "output-a" ]

            let withTwoConnections =
                build
                    [
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

        testCase "grouping collapses identical equal values for one set into one display member" <| fun _ ->
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-rep-1a" replicate (ProvenanceValue.Text "1") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                        propertyValue "pv-rep-1b" replicate (ProvenanceValue.Text "1") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                    ]
                    []
                    [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-rep-1a"; "pv-rep-1b" ]
                    ]
                    []

            let groups = displayGroups built ProvenanceSide.Output [ { Header = replicate } ]
            let members = groups |> List.collect (fun group -> group.Members)

            Expect.equal groups.Length 1 "Identical equal values should collapse into one output group."
            Expect.equal members.Length 1 "The output set should appear once in the collapsed group."
            Expect.equal members.Head.PropertyValueIds [ "pv-rep-1a"; "pv-rep-1b" ] "Collapsed display membership should keep all underlying property value IDs."

        testCase "grouping keeps equal text with different units separate" <| fun _ ->
            let temperature = propertyHeader FixtureKinds.parameterProperty "Temperature"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let celsius = term "C"
            let fahrenheit = term "F"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-temp-c" temperature (ProvenanceValue.Integer 12) (Some celsius) (Some(anchor "assay-table" (Some "assay-process") temperature [ "Input A" ] []))
                        propertyValue "pv-temp-f" temperature (ProvenanceValue.Integer 12) (Some fahrenheit) (Some(anchor "assay-table" (Some "assay-process") temperature [ "Input B" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-temp-c" ]
                        inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-temp-f" ]
                    ]
                    []
                    []

            let groups = displayGroups built ProvenanceSide.Input [ { Header = temperature } ]

            Expect.equal groups.Length 2 "Values with the same scalar value but different units must not collapse."

        testCase "scoped input grouping uses multiple own values as one value-set group" <| fun _ ->
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-rep-1" replicate (ProvenanceValue.Text "1") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] []))
                        propertyValue "pv-input-a-rep-2" replicate (ProvenanceValue.Text "2") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-rep-1"; "pv-input-a-rep-2" ]
                    ]
                    []
                    []

            let groups =
                displayGroupsForAssignments
                    built
                    ProvenanceSide.Input
                    [ { Key = { Header = replicate }; Scope = GroupingScope.Input } ]

            let groupedValues =
                groups
                |> List.choose (fun group ->
                    if group.Members |> List.exists (fun member' -> member'.SetId = "input-a") then
                        group.GroupingValues
                        |> List.map (fun groupingValue -> groupingValue.Value)
                        |> List.sort
                        |> Some
                    else
                        None)
                |> List.exactlyOne

            Expect.equal groupedValues [ ProvenanceValue.Text "1"; ProvenanceValue.Text "2" ] "An input with multiple values for the same grouping key must appear in one combined value-set group."

        testCase "both-side grouping lets inputs inherit missing values from direct connected outputs" <| fun _ ->
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-output-a-rep-1" replicate (ProvenanceValue.Text "1") None (Some(anchor "assay-table" (Some "assay-process") replicate [] [ "Output A" ]))
                        propertyValue "pv-output-b-rep-2" replicate (ProvenanceValue.Text "2") None (Some(anchor "assay-table" (Some "assay-process") replicate [] [ "Output B" ]))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ]
                    [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-rep-1" ]
                        outputSet "output-b" "assay-table" outputHeader "Output B" [ "pv-output-b-rep-2" ]
                    ]
                    [
                        connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                        connection "connection-b" "assay-table" (Some "assay-process") "input-a" "output-b"
                    ]

            let groups =
                displayGroupsForAssignments
                    built
                    ProvenanceSide.Input
                    [ { Key = { Header = replicate }; Scope = GroupingScope.Both } ]

            let groupedValues =
                groups
                |> List.find (fun group -> group.Members |> List.exists (fun member' -> member'.SetId = "input-a"))
                |> fun group ->
                    group.GroupingValues
                    |> List.map (fun groupingValue -> groupingValue.Value)
                    |> List.sort

            Expect.equal groupedValues [ ProvenanceValue.Text "1"; ProvenanceValue.Text "2" ] "A missing input property should inherit direct connected output values as one combined value-set group."

        testCase "both-side grouping prefers input values over inherited output values" <| fun _ ->
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-input-a-rep-3" replicate (ProvenanceValue.Text "3") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] []))
                        propertyValue "pv-output-a-rep-1" replicate (ProvenanceValue.Text "1") None (Some(anchor "assay-table" (Some "assay-process") replicate [] [ "Output A" ]))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-rep-3" ]
                    ]
                    [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-rep-1" ]
                    ]
                    [
                        connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                    ]

            let groups =
                displayGroupsForAssignments
                    built
                    ProvenanceSide.Input
                    [ { Key = { Header = replicate }; Scope = GroupingScope.Both } ]

            let groupedValues =
                groups
                |> List.filter (fun group -> group.Members |> List.exists (fun member' -> member'.SetId = "input-a"))
                |> List.choose (fun group ->
                    group.GroupingValues
                    |> List.tryExactlyOne
                    |> Option.map (fun groupingValue -> groupingValue.Value))
                |> List.sort

            Expect.equal groupedValues [ ProvenanceValue.Text "3" ] "An input's own values should be used before direct connected output values are considered."

        testCase "displayGroups works for input-only loaded models" <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ]
                    []
                    []

            let groups = displayGroups built ProvenanceSide.Input []

            Expect.equal groups.Length 1 "Input-only models should still render input groups."
            Expect.equal groups.Head.Members.Head.Name "Input A" "The loaded input should still be visible."

        testCase "displayConnections expands to represented loaded set pairs only" <| fun _ ->
            let model = validModel ()
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputGroups = displayGroups model ProvenanceSide.Input [ { Header = species } ]
            let outputGroups = displayGroups model ProvenanceSide.Output []
            let connections = displayConnections model inputGroups outputGroups

            let representedIds =
                connections
                |> List.collect (fun connection -> connection.ConnectionIds)
                |> List.sort

            Expect.equal representedIds [ "connection-a"; "connection-b"; "connection-c"; "connection-d" ] "Display lines should represent real loaded connections only."

        testCase "displayConnections returns no lines when one side is absent" <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ]
                    []
                    []

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
        ]
        [
            outputSet setBId "assay-table" outputHeader "Output A" [ pvId ]
        ]
        [
            connection connId "assay-table" (Some "assay-process") setAId setBId
        ]

let editTests =
    testList "Edit" [
        testCase "updatePropertyValue preserves collapsed source anchor" <| fun _ ->
            let model = validModel ()

            match updatePropertyValue "pv-species-arabidopsis-a" (ProvenanceValue.Text "A. thaliana") None model with
            | Ok(nextModel, [ ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, source, _, newValue, _) ]) ->
                Expect.equal propertyValueId "pv-species-arabidopsis-a" "Patch should identify edited occurrence."
                Expect.equal source.InputNames [ "Input A" ] "Patch should preserve source input name."
                Expect.equal newValue (ProvenanceValue.Text "A. thaliana") "Patch should carry edited value."
                Expect.equal nextModel.PropertyValues.["pv-species-arabidopsis-a"].Value (ProvenanceValue.Text "A. thaliana") "Model should update the occurrence."
            | other ->
                failwithf "Expected one UpdatePropertyValue patch, got %A" other

        testCase "createLoadedPropertyValue adds occurrence to target loaded set" <| fun _ ->
            let model = validModel ()
            let treatment = propertyHeader FixtureKinds.characteristicProperty "Treatment"

            let command =
                {
                    Target = ProvenancePropertyTarget.InputSets [ "input-c" ]
                    CopiedFrom = None
                    Header = treatment
                    Value = ProvenanceValue.Text "Drought"
                    Unit = None
                }

            match createLoadedPropertyValue command model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, copiedFrom, header, value, _) ]) ->
                Expect.equal target (ProvenancePropertyTarget.InputSets [ "input-c" ]) "Patch should target the loaded input set."
                Expect.equal copiedFrom None "New value should not be copied from another occurrence."
                Expect.equal header treatment "Patch should carry the requested header."
                Expect.equal value (ProvenanceValue.Text "Drought") "Patch should carry the requested value."
                Expect.equal (nextModel.InputSets.["input-c"].PropertyValueIds.Length) 1 "Target input set should point to the new value."
            | other ->
                failwithf "Expected one AddLoadedPropertyValue patch, got %A" other

        testCase "copyPropertyValueToLoadedTarget copies previous value to existing loaded connection" <| fun _ ->
            let model = validModel ()

            match copyPropertyValueToLoadedTarget "pv-species-arabidopsis-a" (ProvenancePropertyTarget.Connections [ "connection-d" ]) model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, copiedFrom, _, value, _) ]) ->
                Expect.equal target (ProvenancePropertyTarget.Connections [ "connection-d" ]) "Patch should target the existing loaded connection."
                Expect.equal copiedFrom (Some "pv-species-arabidopsis-a") "Patch should preserve copied source occurrence."
                Expect.equal value (ProvenanceValue.Text "Arabidopsis") "Patch should copy the value."
                Expect.isTrue (nextModel.InputSets.["input-c"].PropertyValueIds.Length > model.InputSets.["input-c"].PropertyValueIds.Length) "Connection input set should point to the copied loaded occurrence."
                Expect.isTrue (nextModel.OutputSets.["output-c"].PropertyValueIds.Length > model.OutputSets.["output-c"].PropertyValueIds.Length) "Connection output set should point to the copied loaded occurrence."
            | other ->
                failwithf "Expected one AddLoadedPropertyValue patch, got %A" other

        testCase "createLoadedPropertyValue is a no-op when an identical loaded value already exists on the same target" <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-species-a" species (ProvenanceValue.Text "Arabidopsis") None (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a" ]
                    ]
                    []
                    []

            match createLoadedPropertyValue { Target = ProvenancePropertyTarget.InputSets [ "input-a" ]; CopiedFrom = None; Header = species; Value = ProvenanceValue.Text "Arabidopsis"; Unit = None } built with
            | Ok(nextModel, []) ->
                Expect.equal nextModel built "An exact duplicate loaded value should not create a second model value."
            | other ->
                failwithf "Expected a no-op duplicate create, got %A" other

        testCase "copyPropertyValueToLoadedTarget is a no-op when the target already has an identical value" <| fun _ ->
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    [
                        propertyValue "pv-species-a" species (ProvenanceValue.Text "Arabidopsis") None (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
                    ]
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a" ]
                    ]
                    []
                    []

            match copyPropertyValueToLoadedTarget "pv-species-a" (ProvenancePropertyTarget.InputSets [ "input-a" ]) built with
            | Ok(nextModel, []) ->
                Expect.equal nextModel built "Copying an identical loaded value onto the same target should be a no-op."
            | other ->
                failwithf "Expected a no-op duplicate copy, got %A" other

        testCase "createLoadedSet adds the missing output side to an input-only loaded model" <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ]
                    []
                    []

            match createLoadedSet { Side = ProvenanceSide.Output; Header = outputHeader; Name = "Output A" } built with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedSet(side, tableName, header, name) ]) ->
                Expect.equal side ProvenanceSide.Output "Patch should carry the created side."
                Expect.equal tableName "assay-table" "Patch should target the loaded table."
                Expect.equal header outputHeader "Patch should preserve the created header."
                Expect.equal name "Output A" "Patch should preserve the created name."
                Expect.equal nextModel.OutputSets.Count 1 "The missing output side should be created."
            | other ->
                failwithf "Expected AddLoadedSet patch, got %A" other

        testCase "createLoadedSet is a no-op when the same loaded endpoint already exists" <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ]
                    []
                    []

            match createLoadedSet { Side = ProvenanceSide.Input; Header = inputHeader; Name = "Input A" } built with
            | Ok(nextModel, []) ->
                Expect.equal nextModel built "Creating an already-existing loaded endpoint should be a no-op."
            | other ->
                failwithf "Expected no-op duplicate create, got %A" other

        testCase "connectSets works after the missing side was created on a partial model" <| fun _ ->
            let inputHeader = ioHeader FixtureKinds.sampleEndpoint "Input [Sample Name]"
            let outputHeader = ioHeader FixtureKinds.sampleEndpoint "Output [Sample Name]"

            let built =
                model
                    "assay-table"
                    []
                    [
                        inputSet "input-a" "assay-table" inputHeader "Input A" []
                    ]
                    []
                    []

            let created =
                match createLoadedSet { Side = ProvenanceSide.Output; Header = outputHeader; Name = "Output A" } built with
                | Ok(nextModel, _) -> nextModel
                | Error error -> failwithf "Unexpected createLoadedSet error: %A" error

            let createdOutputId =
                created.OutputSets
                |> Map.toList
                |> List.exactlyOne
                |> fst

            match connectSets "input-a" createdOutputId None created with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedConnection(_, _, inputSetId, outputSetId) ]) ->
                Expect.equal inputSetId "input-a" "Connection patch should target the existing loaded input."
                Expect.equal outputSetId createdOutputId "Connection patch should target the created loaded output."
                Expect.equal nextModel.Connections.Count 1 "The partial model should become connectable after creating the missing side."
            | other ->
                failwithf "Expected AddLoadedConnection patch, got %A" other

        testCase "connectSets creates a loaded input output connection" <| fun _ ->
            let model = validModel ()

            match connectSets "input-c" "output-a" None model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedConnection(tableName, processName, inputSetId, outputSetId) ]) ->
                Expect.equal tableName "assay-table" "Patch should target loaded table."
                Expect.equal processName None "Caller may create a connection without assigning a process name yet."
                Expect.equal inputSetId "input-c" "Patch should keep input set."
                Expect.equal outputSetId "output-a" "Patch should keep output set."
                Expect.isTrue (nextModel.Connections |> Map.exists (fun _ connection -> connection.InputSetId = "input-c" && connection.OutputSetId = "output-a")) "Model should contain new connection."
            | other ->
                failwithf "Expected one AddLoadedConnection patch, got %A" other

        testCase "updatePropertyValue finds anchor from loaded set membership when Source is None" <| fun _ ->
            let model = sourceFromLoadedMembershipModel ()

            match updatePropertyValue "pv-species-a" (ProvenanceValue.Text "A. thaliana") None model with
            | Ok(nextModel, [ ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, source, _, newValue, _) ]) ->
                Expect.equal propertyValueId "pv-species-a" "Patch should identify edited occurrence."
                Expect.equal source.InputNames [ "Input A" ] "Source should derive input names from loaded set membership."
                Expect.equal source.OutputNames [ "Output A" ] "Source should derive output names from loaded set membership."
                Expect.equal newValue (ProvenanceValue.Text "A. thaliana") "Patch should carry edited value."
                Expect.isSome nextModel.PropertyValues.["pv-species-a"].Source "Model should persist the derived source anchor."
            | other ->
                failwithf "Expected one UpdatePropertyValue patch, got %A" other
    ]

let fixtureTests =
    testList "Fixtures" [
        testCase "sampleModel includes loaded names and previous collapsed value" <| fun _ ->
            let model = sampleModel ()

            Expect.equal model.InputSets.["input-a"].Name "Input A" "Fixture should expose actual loaded input name."
            Expect.equal model.OutputSets.["output-b"].Name "Output B" "Fixture should expose actual loaded output name."

            let previousValues =
                model.InputSets.["input-a"].PropertyValueIds
                |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)
                |> List.filter (fun value -> value.Source |> Option.exists (fun source -> source.TableName = "previous-study-table"))

            Expect.isNonEmpty previousValues "Fixture should include collapsed previous-context property value."

        testCase "sampleModel connections are loaded set pairs" <| fun _ ->
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
        testCase "init exposes the loaded model as the first active pair" <| fun _ ->
            let initial = sampleModel ()
            let session = Session.init initial
            let active = Session.activePair session

            Expect.equal session.Layers.Length 2 "Initial session should render input and output layers."
            Expect.equal active.Model initial "Initial active pair should preserve the converted model."
            Expect.equal active.LeftLayerId "layer-1" "The first pair should start on layer 1."
            Expect.equal active.RightLayerId "layer-2" "The first pair should end on layer 2."

        testCase "selectPair switches display pairs without producing writeback patches" <| fun _ ->
            let session = Session.init (sampleModel ())

            match Session.selectPair "pair-1" session with
            | Ok(next, patches) ->
                Expect.equal next.ActivePairId "pair-1" "Known pair should become active."
                Expect.isEmpty patches "Navigation is view/session state only."
            | Error error ->
                failwithf "Expected pair selection success, got %A" error

        testCase "addLayer defaults to active outputs and creates an input-only next pair" <| fun _ ->
            let session = Session.init (sampleModel ())

            match Session.addLayer { SelectedSets = [] } session with
            | Ok(next, patches) ->
                let pair = Session.activePair next
                let names =
                    pair.Model.InputSets
                    |> Map.toList
                    |> List.map (fun (_, set) -> set.Name)
                    |> List.sort

                Expect.equal next.PairOrder [ "pair-1"; "pair-2" ] "A second adjacent pair should be created."
                Expect.equal names [ "Output A"; "Output B"; "Output C"; "Output D"; "Output E" ] "Outputs seed later inputs by default."
                Expect.isEmpty pair.Model.OutputSets "New pair starts as a legitimate input-only transition."
                Expect.isEmpty pair.Model.Connections "No connector exists until the user creates one."
                Expect.equal next.BoundaryLinks.Length 5 "Each carried output must be linked to its next input projection."
                Expect.isTrue
                    (pair.Model.PropertyValues
                     |> Map.forall (fun id _ ->
                         pair.Model.InputSets
                         |> Map.exists (fun _ set -> ProvenanceSet.effectivePropertyValueIds set |> List.contains id)))
                    "Derived pairs should retain only values referenced by their projected endpoints."
                Expect.isEmpty patches "Creating an empty editing layer does not write ARC data."
            | Error error ->
                failwithf "Expected layer addition success, got %A" error

        testCase "addLayer carries output properties inherited through loaded connections" <| fun _ ->
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
                    ]
                    [
                        outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-analysis" ]
                    ]
                    [
                        connection "connection-a" "assay-table" None "input-a" "output-a"
                    ]
                |> Session.init

            match Session.addLayer { SelectedSets = [ ProvenanceSide.Output, "output-a" ] } initial with
            | Ok(next, patches) ->
                Expect.isEmpty patches "Layer projection should not write ARC data."
                let projected =
                    (Session.activePair next).Model.InputSets
                    |> Map.toList
                    |> List.exactlyOne
                    |> snd

                Expect.equal
                    (ProvenanceSet.effectivePropertyValueIds projected |> List.sort)
                    [ "pv-input-a-species"; "pv-output-a-analysis" ]
                    "A projected output should keep properties inherited from its previously connected loaded input."
            | Error error ->
                failwithf "Expected layer addition success, got %A" error

        testCase "addLayer supports a mixed selected seed" <| fun _ ->
            let session = Session.init (sampleModel ())

            let selected =
                {
                    SelectedSets =
                        [
                            ProvenanceSide.Input, "input-a"
                            ProvenanceSide.Output, "output-b"
                        ]
                }

            match Session.addLayer selected session with
            | Ok(next, _) ->
                let pair = Session.activePair next
                let names =
                    pair.Model.InputSets
                    |> Map.toList
                    |> List.map (fun (_, set) -> set.Name)
                    |> List.sort

                Expect.equal names [ "Input A"; "Output B" ] "Mixed selection becomes the next input side."
                Expect.equal next.BoundaryLinks.Length 2 "Only seeded items should be linked."
                Expect.equal pair.LeftLayerId "selection-3" "Mixed input/output selections use a virtual selection layer."
                Expect.equal next.Layers.[2].Label "Selection 3" "Selection layer should be visible in navigation."
            | Error error ->
                failwithf "Expected mixed layer addition success, got %A" error

        testCase "updating a carried property from the next pair updates the previous view once" <| fun _ ->
            let first = Session.init (sampleModel ())
            let layered =
                match Session.addLayer { SelectedSets = [ ProvenanceSide.Output, "output-a" ] } first with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected addLayer error: %A" error

            let nextPair = Session.activePair layered
            let carriedInput = nextPair.Model.InputSets |> Map.toList |> List.exactlyOne |> snd
            let analysisId = carriedInput.PropertyValueIds |> List.head

            match Session.updatePropertyValue analysisId (ProvenanceValue.Text "Imaging") None layered with
            | Ok(next, [ ProvenanceTablePatch.UpdatePropertyValue(id, _, _, ProvenanceValue.Text "Imaging", _) ]) ->
                let originalValue = next.Pairs.["pair-1"].Model.PropertyValues.[analysisId].Value
                let carriedValue = next.Pairs.["pair-2"].Model.PropertyValues.[analysisId].Value

                Expect.equal id analysisId "Edit patch should identify the edited occurrence."
                Expect.equal originalValue (ProvenanceValue.Text "Imaging") "Previous output view should update."
                Expect.equal carriedValue (ProvenanceValue.Text "Imaging") "Later input view should update."
            | other ->
                failwithf "Expected one synchronized update patch, got %A" other

        testCase "assigning a value to a carried next input is reflected on its previous output" <| fun _ ->
            let first = Session.init (sampleModel ())
            let layered =
                match Session.addLayer { SelectedSets = [ ProvenanceSide.Output, "output-d" ] } first with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected addLayer error: %A" error

            let projectedId = (Session.activePair layered).Model.InputSets |> Map.toList |> List.exactlyOne |> fst
            let treatment = propertyHeader FixtureKinds.characteristicProperty "Treatment"
            let command =
                {
                    Target = ProvenancePropertyTarget.InputSets [ projectedId ]
                    CopiedFrom = None
                    Header = treatment
                    Value = ProvenanceValue.Text "Drought"
                    Unit = None
                }

            match Session.createLoadedPropertyValue command layered with
            | Ok(next, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, _, _, _, _) ]) ->
                let previousOutput = next.Pairs.["pair-1"].Model.OutputSets.["output-d"]
                let nextInput = next.Pairs.["pair-2"].Model.InputSets.[projectedId]

                Expect.equal
                    target
                    (ProvenancePropertyTarget.OutputSets [ "output-d" ])
                    "A carried pair-2 input must write through its native pair-1 output owner."
                Expect.equal previousOutput.PropertyValueIds nextInput.PropertyValueIds "Linked endpoint views should share new properties."
            | other ->
                failwithf "Expected one property-add patch, got %A" other

        testCase "assigning a palette value to a carried next input writes only to the active pair" <| fun _ ->
            let first = Session.init (sampleModel ())
            let layered =
                match Session.addLayer { SelectedSets = [ ProvenanceSide.Output, "output-d" ] } first with
                | Ok(next, _) -> next
                | Error error -> failwithf "Unexpected addLayer error: %A" error

            let projectedId = (Session.activePair layered).Model.InputSets |> Map.toList |> List.exactlyOne |> fst
            let previousBefore = layered.Pairs.["pair-1"].Model.OutputSets.["output-d"]
            let treatment = propertyHeader FixtureKinds.characteristicProperty "Treatment"
            let command =
                {
                    Target = ProvenancePropertyTarget.InputSets [ projectedId ]
                    CopiedFrom = None
                    Header = treatment
                    Value = ProvenanceValue.Text "Drought"
                    Unit = None
                }

            match Session.createCurrentLoadedPropertyValue command layered with
            | Ok(next, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, _, header, value, _) ]) ->
                let previousOutput = next.Pairs.["pair-1"].Model.OutputSets.["output-d"]
                let nextInput = next.Pairs.["pair-2"].Model.InputSets.[projectedId]

                Expect.equal target (ProvenancePropertyTarget.InputSets [ projectedId ]) "Palette drops should patch the active displayed target."
                Expect.equal header treatment "Patch should carry the palette property."
                Expect.equal value (ProvenanceValue.Text "Drought") "Patch should carry the palette value."
                Expect.equal previousOutput.PropertyValueIds previousBefore.PropertyValueIds "Palette additions must not write through to the native boundary owner."
                Expect.isGreaterThan nextInput.PropertyValueIds.Length previousBefore.PropertyValueIds.Length "The active pair input should receive the new property value."
            | other ->
                failwithf "Expected one active-pair property-add patch, got %A" other

        testCase "updating multiple property values uses the edit path for every value" <| fun _ ->
            let session = Session.init (validModel ())
            let updates =
                [
                    "pv-species-arabidopsis-a", ProvenanceValue.Text "A. thaliana", None
                    "pv-species-arabidopsis-b", ProvenanceValue.Text "A. thaliana", None
                ]

            match Session.updatePropertyValues updates session with
            | Ok(next, patches) ->
                let model = (Session.activePair next).Model
                Expect.equal patches.Length 2 "Every existing value should produce an edit patch."
                Expect.equal model.PropertyValues.["pv-species-arabidopsis-a"].Value (ProvenanceValue.Text "A. thaliana") "First value should be edited."
                Expect.equal model.PropertyValues.["pv-species-arabidopsis-b"].Value (ProvenanceValue.Text "A. thaliana") "Second value should be edited."
            | other ->
                failwithf "Expected batched edit success, got %A" other

        testCase "creating and connecting a later output modifies only the active pair" <| fun _ ->
            let layered =
                Session.init (sampleModel ())
                |> Session.addLayer { SelectedSets = [ ProvenanceSide.Output, "output-a" ] }
                |> function Ok(next, _) -> next | Error error -> failwithf "%A" error

            let outputHeader = ioHeader FixtureKinds.dataEndpoint "Output [Data]"
            let created =
                match Session.createLoadedSet { Side = ProvenanceSide.Output; Header = outputHeader; Name = "Raw file" } layered with
                | Ok(next, _) -> next
                | Error error -> failwithf "%A" error

            let pair = Session.activePair created
            let inputId = pair.Model.InputSets |> Map.toList |> List.exactlyOne |> fst
            let outputId = pair.Model.OutputSets |> Map.toList |> List.exactlyOne |> fst

            match Session.connectSets inputId outputId None created with
            | Ok(next, [ ProvenanceTablePatch.AddLoadedConnection _ ]) ->
                Expect.equal next.Pairs.["pair-1"].Model.Connections.Count 5 "Prior transition should remain unchanged."
                Expect.equal next.Pairs.["pair-2"].Model.Connections.Count 1 "Later transition gets its connection."
            | other ->
                failwithf "Expected a later connection patch, got %A" other

        testCase "init and addLayer work for an output-only model" <| fun _ ->
            let initial = Session.init (outputOnlyModel ())

            Expect.isEmpty (Session.activePair initial).Model.InputSets "Output-only input has no synthetic input endpoints."
            Expect.isNonEmpty (Session.activePair initial).Model.OutputSets "Real output endpoints remain visible."

            match Session.addLayer { SelectedSets = [] } initial with
            | Ok(next, patches) ->
                let pair = Session.activePair next
                Expect.isNonEmpty pair.Model.InputSets "Current outputs should seed the next displayed input side."
                Expect.isEmpty pair.Model.OutputSets "A newly displayed transition has no invented outputs."
                Expect.isEmpty patches "View-layer derivation is not a persistence edit."
            | Error error ->
                failwithf "Expected output-only layer derivation success, got %A" error

        testCase "adding a connection property synchronizes a carried boundary endpoint" <| fun _ ->
            let session = Session.init (sampleModel ())
            let layered =
                Session.addLayer { SelectedSets = [ ProvenanceSide.Output, "output-a" ] } session
                |> fun result ->
                    match result with
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected addLayer error: %A" error
            let pair1 =
                Session.selectPair "pair-1" layered
                |> fun result ->
                    match result with
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected selectPair error: %A" error
            let command =
                {
                    Target = ProvenancePropertyTarget.Connections [ "connection-a" ]
                    CopiedFrom = None
                    Header = propertyHeader FixtureKinds.parameterProperty "Analysis"
                    Value = ProvenanceValue.Text "Microscopy"
                    Unit = None
                }
            let edited =
                Session.createLoadedPropertyValue command pair1
                |> fun result ->
                    match result with
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected createLoadedPropertyValue error: %A" error
            let firstOutput = edited.Pairs.["pair-1"].Model.OutputSets.["output-a"]
            let nextInput = edited.Pairs.["pair-2"].Model.InputSets.["pair-2-from-output-0-output-a"]
            Expect.equal nextInput.PropertyValueIds firstOutput.PropertyValueIds "linked input mirrors the edited output"

        testCase "synchronizing a carried input back to an output recomputes output inherited pointers" <| fun _ ->
            let session = Session.init (sampleModel ())
            let layered =
                Session.addLayer { SelectedSets = [ ProvenanceSide.Output, "output-a" ] } session
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected addLayer error: %A" error

            let pair2 = Session.activePair layered
            let projectedId = pair2.Model.InputSets |> Map.toList |> List.exactlyOne |> fst
            let foreignHeader = propertyHeader FixtureKinds.parameterProperty "Foreign"
            let foreignValue =
                propertyValue "pv-foreign" foreignHeader (ProvenanceValue.Text "stale") None None
            let pollutedInput =
                {
                    pair2.Model.InputSets.[projectedId] with
                        InheritedPropertyValueIds =
                            pair2.Model.InputSets.[projectedId].InheritedPropertyValueIds
                            |> Map.add "foreign-connection" [ foreignValue.Id ]
                }
            let pollutedPair2 =
                {
                    pair2 with
                        Model =
                            {
                                pair2.Model with
                                    PropertyValues = pair2.Model.PropertyValues |> Map.add foreignValue.Id foreignValue
                                    InputSets = pair2.Model.InputSets |> Map.add projectedId pollutedInput
                            }
                }
            let polluted =
                {
                    layered with
                        Pairs = layered.Pairs |> Map.add pair2.Id pollutedPair2
                }

            let outputHeader = ioHeader FixtureKinds.dataEndpoint "Output [Data]"
            let withOutput =
                Session.createLoadedSet { Side = ProvenanceSide.Output; Header = outputHeader; Name = "Pair 2 Output" } polluted
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected createLoadedSet error: %A" error
            let pair2WithOutput = Session.activePair withOutput
            let outputId = pair2WithOutput.Model.OutputSets |> Map.toList |> List.exactlyOne |> fst
            let connected =
                Session.connectSets projectedId outputId None withOutput
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected connectSets error: %A" error
            let pair2ConnectionId =
                (Session.activePair connected).Model.Connections
                |> Map.toList
                |> List.exactlyOne
                |> fst
            let syncHeader = propertyHeader FixtureKinds.parameterProperty "Sync"
            let command =
                {
                    Target = ProvenancePropertyTarget.Connections [ pair2ConnectionId ]
                    CopiedFrom = None
                    Header = syncHeader
                    Value = ProvenanceValue.Text "pair-2"
                    Unit = None
                }

            let edited =
                Session.createLoadedPropertyValue command connected
                |> function
                    | Ok(next, _) -> next
                    | Error error -> failwithf "Unexpected createLoadedPropertyValue error: %A" error

            let previousOutput = edited.Pairs.["pair-1"].Model.OutputSets.["output-a"]

            Expect.isFalse
                (previousOutput.InheritedPropertyValueIds.ContainsKey "foreign-connection")
                "Synchronizing into an output must not copy inherited pointers from another pair verbatim."
            Expect.isTrue
                (previousOutput.InheritedPropertyValueIds.ContainsKey "connection-a")
                "The output should keep inherited pointers recomputed from its own loaded connections."

        testCase "adding a value to a removed connection returns a session error" <| fun _ ->
            let command =
                {
                    Target = ProvenancePropertyTarget.Connections [ "missing-connection" ]
                    CopiedFrom = None
                    Header = propertyHeader FixtureKinds.parameterProperty "Analysis"
                    Value = ProvenanceValue.Text "Microscopy"
                    Unit = None
                }

            match Session.createLoadedPropertyValue command (Session.init (sampleModel ())) with
            | Error(SessionError.EditFailed(EditError.ConnectionNotFound connectionId)) ->
                Expect.equal connectionId "missing-connection" "Removed target identity should be returned."
            | other ->
                failwithf "Expected missing-connection session error, got %A" other
    ]

let uiStateTests =
    testList "UI state" [
        testCase "toggleSideGrouping applies grouping only to the selected layer side" <| fun _ ->
            let session = Session.init (sampleModel ())
            let pair = Session.activePair session
            let state = ProvenanceGroupingState.init session
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"

            let next = ProvenanceGroupingState.GroupingAssignments.toggleSide pair.RightLayerId ProvenanceSide.Output replicate state

            Expect.equal
                (ProvenanceGroupingState.Layers.get pair.LeftLayerId next).GroupingAssignments
                []
                "Side-only output grouping should not change the input layer state."
            Expect.equal
                (ProvenanceGroupingState.Layers.get pair.RightLayerId next).GroupingAssignments
                [ { Key = { Header = replicate }; Scope = GroupingScope.Output } ]
                "Side-only output grouping should be stored as an output-scoped assignment."

        testCase "toggleBothGrouping applies grouping to both active layer states" <| fun _ ->
            let session = Session.init (sampleModel ())
            let pair = Session.activePair session
            let state = ProvenanceGroupingState.init session
            let replicate = propertyHeader FixtureKinds.parameterProperty "Replicate"

            let next = ProvenanceGroupingState.GroupingAssignments.toggleBoth pair.LeftLayerId pair.RightLayerId replicate state
            let expected = [ { Key = { Header = replicate }; Scope = GroupingScope.Both } ]

            Expect.equal
                (ProvenanceGroupingState.Layers.get pair.LeftLayerId next).GroupingAssignments
                expected
                "Both-side grouping should be visible from the input layer."
            Expect.equal
                (ProvenanceGroupingState.Layers.get pair.RightLayerId next).GroupingAssignments
                expected
                "Both-side grouping should be visible from the output layer."

        testCase "moveGrouping switches a property to the target side only" <| fun _ ->
            let session = Session.init (sampleModel ())
            let pair = Session.activePair session
            let state = ProvenanceGroupingState.init session
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"

            let selected =
                ProvenanceGroupingState.GroupingAssignments.toggleSide pair.LeftLayerId ProvenanceSide.Input species state

            let next =
                ProvenanceGroupingState.GroupingAssignments.move pair.Id pair.LeftLayerId pair.RightLayerId ProvenanceSide.Output species selected

            Expect.equal
                (ProvenanceGroupingState.Layers.get pair.LeftLayerId next).GroupingAssignments
                []
                "Dragging a property away should remove it from the source layer."
            Expect.equal
                (ProvenanceGroupingState.Layers.get pair.RightLayerId next).GroupingAssignments
                [ { Key = { Header = species }; Scope = GroupingScope.Output } ]
                "Dragging a property to output should make it output-only."
            Expect.equal
                (next.PropertyRailPlacements |> Map.tryFind (pair.Id, { Header = species }))
                (Some ProvenanceSide.Output)
                "Dragging a property to output should move its rail control to the output side."

        testCase "toggleBothGrouping removes only both-scope assignments from inconsistent state" <| fun _ ->
            let session = Session.init (sampleModel ())
            let pair = Session.activePair session
            let state = ProvenanceGroupingState.init session
            let species = propertyHeader FixtureKinds.characteristicProperty "Species"
            let key = { Header = species }
            let sideAssignment = { Key = key; Scope = GroupingScope.Input }
            let bothAssignment = { Key = key; Scope = GroupingScope.Both }
            let inconsistent =
                {
                    state with
                        LayerStates =
                            state.LayerStates
                            |> Map.add pair.LeftLayerId { GroupingAssignments = [ sideAssignment; bothAssignment ] }
                            |> Map.add pair.RightLayerId { GroupingAssignments = [ bothAssignment ] }
                }

            let next =
                ProvenanceGroupingState.GroupingAssignments.toggleBoth pair.LeftLayerId pair.RightLayerId species inconsistent

            Expect.equal
                (ProvenanceGroupingState.Layers.get pair.LeftLayerId next).GroupingAssignments
                [ sideAssignment ]
                "Removing a both-side grouping should not silently drop an existing side-specific assignment for the same key."
            Expect.equal
                (ProvenanceGroupingState.Layers.get pair.RightLayerId next).GroupingAssignments
                []
                "Only the both-side assignment should be removed from the opposite layer."

        testCase "property value drop plan adds only when every group member has no value for the property" <| fun _ ->
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
                    ]
                    []
                    []
            let group : DisplayGroup =
                {
                    Id = "manual"
                    TableName = "assay-table"
                    Side = ProvenanceSide.Input
                    GroupingValues = []
                    Members =
                        [
                            { SetId = "input-a"; Name = "Input A"; PropertyValueIds = [ "pv-input-a-species" ] }
                            { SetId = "input-b"; Name = "Input B"; PropertyValueIds = [] }
                        ]
                }
            let source : ProvenanceGroupingTypes.ValueAssignmentSource =
                {
                    CopiedFrom = None
                    Header = treatment
                    Value = ProvenanceValue.Text "Drought"
                    Unit = None
                }

            match ProvenanceGroupingValueAssignment.planPropertyValueDrop source group model with
            | Ok(ProvenanceGroupingTypes.ValueAssignmentPlan.AddCurrent command) ->
                Expect.equal command.Target (ProvenancePropertyTarget.InputSets [ "input-a"; "input-b" ]) "Add plan should target all current group members."
                Expect.equal command.Header treatment "Add plan should preserve the dropped property."
            | other ->
                failwithf "Expected an add plan, got %A" other

            let mixedSource : ProvenanceGroupingTypes.ValueAssignmentSource =
                { source with Header = species }

            match ProvenanceGroupingValueAssignment.planPropertyValueDrop mixedSource group model with
            | Error(ProvenanceGroupingTypes.ValueAssignmentError.MixedPropertyValueCounts header) ->
                Expect.equal header species "Mixed zero/one members should reject the drop for that property."
            | other ->
                failwithf "Expected a mixed-count rejection, got %A" other

        testCase "property value drop plan warns only when every group member has exactly one value" <| fun _ ->
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
                    ]
                    []
                    []
            let group : DisplayGroup =
                {
                    Id = "manual"
                    TableName = "assay-table"
                    Side = ProvenanceSide.Input
                    GroupingValues = []
                    Members =
                        [
                            { SetId = "input-a"; Name = "Input A"; PropertyValueIds = [ "pv-input-a-species" ] }
                            { SetId = "input-b"; Name = "Input B"; PropertyValueIds = [ "pv-input-b-species" ] }
                        ]
                }
            let source : ProvenanceGroupingTypes.ValueAssignmentSource =
                {
                    CopiedFrom = None
                    Header = species
                    Value = ProvenanceValue.Text "A. thaliana"
                    Unit = None
                }

            match ProvenanceGroupingValueAssignment.planPropertyValueDrop source group model with
            | Ok(ProvenanceGroupingTypes.ValueAssignmentPlan.ConfirmOverwrite warning) ->
                Expect.equal warning.ExistingValueIds [ "pv-input-a-species"; "pv-input-b-species" ] "Overwrite warning should target the one editable value per member."
                Expect.equal warning.Header species "Warning should keep the overwritten property."
            | other ->
                failwithf "Expected an overwrite warning, got %A" other

        testCase "property value drop plan rejects members with multiple values for the same property" <| fun _ ->
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
                        inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-input-b-species-a"; "pv-input-b-species-b" ]
                    ]
                    []
                    []
            let group : DisplayGroup =
                {
                    Id = "manual"
                    TableName = "assay-table"
                    Side = ProvenanceSide.Input
                    GroupingValues = []
                    Members =
                        [
                            { SetId = "input-a"; Name = "Input A"; PropertyValueIds = [ "pv-input-a-species" ] }
                            { SetId = "input-b"; Name = "Input B"; PropertyValueIds = [ "pv-input-b-species-a"; "pv-input-b-species-b" ] }
                        ]
                }
            let source : ProvenanceGroupingTypes.ValueAssignmentSource =
                {
                    CopiedFrom = None
                    Header = species
                    Value = ProvenanceValue.Text "A. thaliana"
                    Unit = None
                }

            match ProvenanceGroupingValueAssignment.planPropertyValueDrop source group model with
            | Error(ProvenanceGroupingTypes.ValueAssignmentError.MultiplePropertyValues(header, setIds)) ->
                Expect.equal header species "Multi-value members should reject the drop for that property."
                Expect.equal setIds [ "input-b" ] "The rejection should identify the multi-value member."
            | other ->
                failwithf "Expected a multiple-value rejection, got %A" other
    ]

let tests =
    testList "ProvenanceGrouping" [
        typeTests
        modelTests
        groupingTests
        editTests
        fixtureTests
        sessionTests
        uiStateTests
    ]
