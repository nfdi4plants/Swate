module ProvenanceGroupingTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Import
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Fixtures

let private term name =
    {
        Name = name
        TermSource = None
        TermAccession = None
    }

let private ioHeader kind text =
    {
        Kind = kind
        Text = text
    }

let private propertyHeader kind name =
    {
        Kind = kind
        Category = term name
    }

let private anchor tableName processName header inputNames outputNames =
    {
        TableName = tableName
        ProcessName = processName
        Header = header
        InputNames = inputNames
        OutputNames = outputNames
    }

let private propertyValue id header value source =
    {
        Id = id
        Header = header
        Value = value
        Unit = None
        Source = source
    }

let private importedSet id tableName header name propertyValueIds =
    {
        Id = id
        TableName = tableName
        Header = header
        Name = name
        PropertyValueIds = propertyValueIds
    }

let private importedConnection id tableName processName inputSetId outputSetId =
    {
        Id = id
        TableName = tableName
        ProcessName = processName
        InputSetId = inputSetId
        OutputSetId = outputSetId
    }

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

let importTests =
    testList "Import" [
        testCase "fromImportedProvenance preserves loaded names and repeated property values" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
            let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"

            let imported =
                {
                    LoadedTableName = "assay-table"
                    PropertyValues =
                        [
                            propertyValue "pv-species-a" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "previous-table" (Some "previous-process") species [ "Ancestor A" ] []))
                            propertyValue "pv-rep-1" replicate (ProvenanceValue.Text "1") (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                            propertyValue "pv-rep-2" replicate (ProvenanceValue.Text "2") (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                        ]
                    InputSets =
                        [
                            importedSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a"; "pv-rep-1"; "pv-rep-2" ]
                        ]
                    OutputSets =
                        [
                            importedSet "output-a" "assay-table" outputHeader "Output A" [ "pv-rep-1"; "pv-rep-2" ]
                        ]
                    Connections =
                        [
                            importedConnection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                        ]
                }

            let result = fromImportedProvenance imported

            Expect.equal result.Warnings [] "Valid import should not warn."
            Expect.equal result.Model.InputSets.["input-a"].Name "Input A" "Input set should carry the loaded input name."
            Expect.equal result.Model.OutputSets.["output-a"].Name "Output A" "Output set should carry the loaded output name."
            Expect.equal result.Model.PropertyValues.Count 3 "Repeated values should remain separate occurrences."
            Expect.equal result.Model.InputSets.["input-a"].PropertyValueIds [ "pv-species-a"; "pv-rep-1"; "pv-rep-2" ] "Set should point to all property value occurrences."

        testCase "fromImportedProvenance warns and skips non-loaded sets and connections" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
            let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"

            let imported =
                {
                    LoadedTableName = "assay-table"
                    PropertyValues =
                        [
                            propertyValue "pv-species-a" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "previous-table" (Some "previous-process") species [ "Ancestor A" ] []))
                        ]
                    InputSets =
                        [
                            importedSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a"; "missing-pv" ]
                            importedSet "previous-input" "previous-table" inputHeader "Ancestor A" [ "pv-species-a" ]
                        ]
                    OutputSets =
                        [
                            importedSet "output-a" "assay-table" outputHeader "Output A" []
                        ]
                    Connections =
                        [
                            importedConnection "previous-connection" "previous-table" (Some "previous-process") "previous-input" "output-a"
                            importedConnection "dangling-connection" "assay-table" (Some "assay-process") "missing-input" "output-a"
                        ]
                }

            let result = fromImportedProvenance imported

            Expect.isFalse (result.Model.InputSets.ContainsKey "previous-input") "Previous-table set should not become a first-class set."
            Expect.equal result.Model.Connections.Count 1 "Loaded connection map keeps only loaded-table connection IDs, even when they warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "previous-input")) "Skipped previous set should warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "missing-pv")) "Missing property value should warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "previous-connection")) "Skipped previous connection should warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "missing-input")) "Dangling loaded connection should warn."

        testCase "fromImportedProvenance warns on duplicate IDs and empty loaded table name" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"

            let imported =
                {
                    LoadedTableName = ""
                    PropertyValues =
                        [
                            propertyValue "pv-dup" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
                            propertyValue "pv-dup" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
                        ]
                    InputSets =
                        [
                            importedSet "dup-set" "assay-table" inputHeader "Input A" [ "pv-dup" ]
                            importedSet "dup-set" "assay-table" inputHeader "Input B" [ "pv-dup" ]
                        ]
                    OutputSets = []
                    Connections = []
                }

            let result = fromImportedProvenance imported

            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "LoadedTableName is empty")) "Empty LoadedTableName should warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "Duplicate property value id 'pv-dup'")) "Duplicate property value ID should warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "Duplicate input set id 'dup-set'")) "Duplicate input set ID should warn."
    ]

let private validImportedModel () =
    let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
    let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
    let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
    let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"

    fromImportedProvenance
        {
            LoadedTableName = "assay-table"
            PropertyValues =
                [
                    propertyValue "pv-species-arabidopsis-a" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
                    propertyValue "pv-species-arabidopsis-b" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "assay-table" (Some "assay-process") species [ "Input B" ] []))
                    propertyValue "pv-rep-output-b-1" replicate (ProvenanceValue.Text "1") (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output B" ]))
                    propertyValue "pv-rep-output-b-2" replicate (ProvenanceValue.Text "2") (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input B" ] [ "Output B" ]))
                ]
            InputSets =
                [
                    importedSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-arabidopsis-a" ]
                    importedSet "input-b" "assay-table" inputHeader "Input B" [ "pv-species-arabidopsis-b" ]
                    importedSet "input-c" "assay-table" inputHeader "Input C" []
                ]
            OutputSets =
                [
                    importedSet "output-a" "assay-table" outputHeader "Output A" []
                    importedSet "output-b" "assay-table" outputHeader "Output B" [ "pv-rep-output-b-1"; "pv-rep-output-b-2" ]
                    importedSet "output-c" "assay-table" outputHeader "Output C" []
                ]
            Connections =
                [
                    importedConnection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                    importedConnection "connection-b" "assay-table" (Some "assay-process") "input-a" "output-b"
                    importedConnection "connection-c" "assay-table" (Some "assay-process") "input-b" "output-b"
                    importedConnection "connection-d" "assay-table" (Some "assay-process") "input-c" "output-c"
                ]
        }
        |> fun result -> result.Model

let groupingTests =
    testList "Grouping" [
        testCase "no grouping displays each loaded set by name" <| fun _ ->
            let model = validImportedModel ()
            let groups = displayGroups model ProvenanceSide.Input []

            Expect.equal (groups |> List.map (fun group -> group.Members.Head.Name)) [ "Input A"; "Input B"; "Input C" ] "No grouping should preserve loaded input names."

        testCase "multi-value grouping duplicates the loaded set into each value group" <| fun _ ->
            let model = validImportedModel ()
            let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
            let groups = displayGroups model ProvenanceSide.Output [ { Header = replicate } ]

            let outputBGroupCount =
                groups
                |> List.filter (fun group -> group.Members |> List.exists (fun member' -> member'.SetId = "output-b"))
                |> List.length

            Expect.equal outputBGroupCount 2 "Output B should appear once for each repeated replicate value."

        testCase "displayConnections expands to represented loaded set pairs only" <| fun _ ->
            let model = validImportedModel ()
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

let editTests =
    testList "Edit" [
        testCase "updatePropertyValue preserves collapsed source anchor" <| fun _ ->
            let model = validImportedModel ()

            match updatePropertyValue "pv-species-arabidopsis-a" (ProvenanceValue.Text "A. thaliana") None model with
            | Ok(nextModel, [ ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, source, _, newValue, _) ]) ->
                Expect.equal propertyValueId "pv-species-arabidopsis-a" "Patch should identify edited occurrence."
                Expect.equal source.InputNames [ "Input A" ] "Patch should preserve source input name."
                Expect.equal newValue (ProvenanceValue.Text "A. thaliana") "Patch should carry edited value."
                Expect.equal nextModel.PropertyValues.["pv-species-arabidopsis-a"].Value (ProvenanceValue.Text "A. thaliana") "Model should update the occurrence."
            | other ->
                failwithf "Expected one UpdatePropertyValue patch, got %A" other

        testCase "createLoadedPropertyValue adds occurrence to target loaded set" <| fun _ ->
            let model = validImportedModel ()
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
            let model = validImportedModel ()

            match copyPropertyValueToLoadedTarget "pv-species-arabidopsis-a" (ProvenancePropertyTarget.Connections [ "connection-d" ]) model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, copiedFrom, _, value, _) ]) ->
                Expect.equal target (ProvenancePropertyTarget.Connections [ "connection-d" ]) "Patch should target the existing loaded connection."
                Expect.equal copiedFrom (Some "pv-species-arabidopsis-a") "Patch should preserve copied source occurrence."
                Expect.equal value (ProvenanceValue.Text "Arabidopsis") "Patch should copy the value."
                Expect.isTrue (nextModel.InputSets.["input-c"].PropertyValueIds.Length > model.InputSets.["input-c"].PropertyValueIds.Length) "Connection input set should point to the copied loaded occurrence."
                Expect.isTrue (nextModel.OutputSets.["output-c"].PropertyValueIds.Length > model.OutputSets.["output-c"].PropertyValueIds.Length) "Connection output set should point to the copied loaded occurrence."
            | other ->
                failwithf "Expected one AddLoadedPropertyValue patch, got %A" other

        testCase "connectSets creates a loaded input output connection" <| fun _ ->
            let model = validImportedModel ()

            match connectSets "input-c" "output-a" None model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedConnection(tableName, processName, inputSetId, outputSetId) ]) ->
                Expect.equal tableName "assay-table" "Patch should target loaded table."
                Expect.equal processName None "Caller may create a connection without assigning a process name yet."
                Expect.equal inputSetId "input-c" "Patch should keep input set."
                Expect.equal outputSetId "output-a" "Patch should keep output set."
                Expect.isTrue (nextModel.Connections |> Map.exists (fun _ connection -> connection.InputSetId = "input-c" && connection.OutputSetId = "output-a")) "Model should contain new connection."
            | other ->
                failwithf "Expected one AddLoadedConnection patch, got %A" other
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
        importTests
        groupingTests
        editTests
        fixtureTests
    ]
