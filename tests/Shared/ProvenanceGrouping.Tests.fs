module ProvenanceGroupingTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Import

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
    ]

let tests =
    testList "ProvenanceGrouping" [
        typeTests
        importTests
    ]
