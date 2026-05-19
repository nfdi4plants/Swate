module Swate.Components.Shared.ProvenanceGrouping.Fixtures

open Swate.Components.Shared.ProvenanceGrouping.Types

let term name =
    {
        Name = name
        TermSource = None
        TermAccession = None
    }

let ioHeader kind text =
    {
        Kind = kind
        Text = text
    }

let propertyHeader kind name =
    {
        Kind = kind
        Category = term name
    }

let private source tableName processName header inputNames outputNames : ProvenanceWritebackAnchor =
    {
        TableName = tableName
        ProcessName = processName
        Header = header
        InputNames = inputNames
        OutputNames = outputNames
    }

let private propertyValue id header value tableName processName inputNames outputNames : ProvenancePropertyValue =
    {
        Id = id
        Header = header
        Value = ProvenanceValue.Text value
        Unit = None
        Source = Some(source tableName processName header inputNames outputNames)
    }

let private set id header name propertyValueIds : ProvenanceSet =
    {
        Id = id
        TableName = "assay-table"
        Header = header
        Name = name
        PropertyValueIds = propertyValueIds
    }

let private connection id inputSetId outputSetId : ProvenanceConnection =
    {
        Id = id
        TableName = "assay-table"
        ProcessName = Some "assay-process"
        InputSetId = inputSetId
        OutputSetId = outputSetId
    }

let sampleModel () : ProvenanceModel =
    let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
    let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"
    let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
    let temperature = propertyHeader ProvenancePropertyKind.Parameter "Temperature"
    let analysis = propertyHeader ProvenancePropertyKind.Parameter "Analysis"
    let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
    let previousTreatment = propertyHeader ProvenancePropertyKind.Characteristic "Previous Treatment"

    let propertyValues =
        [
            propertyValue "pv-input-a-species" species "Arabidopsis" "assay-table" (Some "assay-process") [ "Input A" ] []
            propertyValue "pv-input-b-species" species "Arabidopsis" "assay-table" (Some "assay-process") [ "Input B" ] []
            propertyValue "pv-input-c-species" species "Arabidopsis" "assay-table" (Some "assay-process") [ "Input C" ] []
            propertyValue "pv-input-d-species" species "Chlamydomonas" "assay-table" (Some "assay-process") [ "Input D" ] []
            propertyValue "pv-input-a-temperature" temperature "12 C" "assay-table" (Some "assay-process") [ "Input A" ] []
            propertyValue "pv-input-b-temperature" temperature "12 C" "assay-table" (Some "assay-process") [ "Input B" ] []
            propertyValue "pv-input-c-temperature" temperature "24 C" "assay-table" (Some "assay-process") [ "Input C" ] []
            propertyValue "pv-output-a-analysis" analysis "Mass Spectrometry" "assay-table" (Some "assay-process") [] [ "Output A" ]
            propertyValue "pv-output-b-analysis" analysis "Mass Spectrometry" "assay-table" (Some "assay-process") [] [ "Output B" ]
            propertyValue "pv-output-c-analysis" analysis "LC-MS" "assay-table" (Some "assay-process") [] [ "Output C" ]
            propertyValue "pv-output-b-replicate-1" replicate "1" "assay-table" (Some "assay-process") [ "Input A" ] [ "Output B" ]
            propertyValue "pv-output-b-replicate-2" replicate "2" "assay-table" (Some "assay-process") [ "Input B" ] [ "Output B" ]
            propertyValue "pv-previous-treatment-a" previousTreatment "Drought" "previous-study-table" (Some "previous-process") [ "Ancestor A" ] []
        ]

    let inputSets =
        [
            set "input-a" inputHeader "Input A" [ "pv-input-a-species"; "pv-input-a-temperature"; "pv-previous-treatment-a" ]
            set "input-b" inputHeader "Input B" [ "pv-input-b-species"; "pv-input-b-temperature" ]
            set "input-c" inputHeader "Input C" [ "pv-input-c-species"; "pv-input-c-temperature" ]
            set "input-d" inputHeader "Input D" [ "pv-input-d-species" ]
        ]

    let outputSets =
        [
            set "output-a" outputHeader "Output A" [ "pv-output-a-analysis" ]
            set "output-b" outputHeader "Output B" [ "pv-output-b-analysis"; "pv-output-b-replicate-1"; "pv-output-b-replicate-2" ]
            set "output-c" outputHeader "Output C" [ "pv-output-c-analysis" ]
            set "output-d" outputHeader "Output D" []
            set "output-e" outputHeader "Output E" []
        ]

    let connections =
        [
            connection "connection-a" "input-a" "output-a"
            connection "connection-b" "input-a" "output-b"
            connection "connection-c" "input-b" "output-b"
            connection "connection-d" "input-c" "output-c"
            connection "connection-e" "input-d" "output-d"
        ]

    {
        LoadedTableName = "assay-table"
        PropertyValues = propertyValues |> List.map (fun value -> value.Id, value) |> Map.ofList
        InputSets = inputSets |> List.map (fun set -> set.Id, set) |> Map.ofList
        OutputSets = outputSets |> List.map (fun set -> set.Id, set) |> Map.ofList
        Connections = connections |> List.map (fun connection -> connection.Id, connection) |> Map.ofList
    }
