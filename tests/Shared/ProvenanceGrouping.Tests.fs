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

let typeTests =
    testList "Types" [
        testCase "loaded input set carries the actual input name" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputSet =
                {
                    Id = "input-a"
                    TableName = "assay-table"
                    Header = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
                    Name = "Input A"
                    PropertyValueIds = [ "pv-species-a" ]
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
    ]

let modelTests =
    testList "Model" [
        testCase "direct model builder preserves loaded names and repeated distinct property values" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
            let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"

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
            let previousTreatment = propertyHeader ProvenancePropertyKind.Characteristic "Previous Treatment"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"

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
    let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
    let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
    let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
    let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"

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

        testCase "multi-value grouping duplicates the loaded set into each value group" <| fun _ ->
            let model = validModel ()
            let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
            let groups = displayGroups model ProvenanceSide.Output [ { Header = replicate } ]

            let outputBGroupCount =
                groups
                |> List.filter (fun group -> group.Members |> List.exists (fun member' -> member'.SetId = "output-b"))
                |> List.length

            Expect.equal outputBGroupCount 2 "Output B should appear once for each repeated replicate value."

        testCase "displayConnections expands to represented loaded set pairs only" <| fun _ ->
            let model = validModel ()
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputGroups = displayGroups model ProvenanceSide.Input [ { Header = species } ]
            let outputGroups = displayGroups model ProvenanceSide.Output []
            let connections = displayConnections model inputGroups outputGroups

            let representedIds =
                connections
                |> List.collect (fun connection -> connection.ConnectionIds)
                |> List.sort

            Expect.equal representedIds [ "connection-a"; "connection-b"; "connection-c"; "connection-d" ] "Display lines should represent real loaded connections only."
    ]

let private sourceFromLoadedMembershipModel () =
    let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
    let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
    let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"

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
            let treatment = propertyHeader ProvenancePropertyKind.Characteristic "Treatment"

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
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"

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
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"

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

let tests =
    testList "ProvenanceGrouping" [
        typeTests
        modelTests
        groupingTests
        editTests
        fixtureTests
    ]
