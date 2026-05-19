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

let anchor tableName processName header inputNames outputNames : ProvenanceWritebackAnchor =
    {
        TableName = tableName
        ProcessName = processName
        Header = header
        InputNames = inputNames
        OutputNames = outputNames
    }

let propertyValue id header value unit source : ProvenancePropertyValue =
    {
        Id = id
        Header = header
        Value = value
        Unit = unit
        Source = source
    }

let inputSet id tableName header name propertyValueIds : ProvenanceSet =
    {
        Id = id
        TableName = tableName
        Header = header
        Name = name
        PropertyValueIds = propertyValueIds
    }

let outputSet id tableName header name propertyValueIds : ProvenanceSet =
    {
        Id = id
        TableName = tableName
        Header = header
        Name = name
        PropertyValueIds = propertyValueIds
    }

let connection id tableName processName inputSetId outputSetId : ProvenanceConnection =
    {
        Id = id
        TableName = tableName
        ProcessName = processName
        InputSetId = inputSetId
        OutputSetId = outputSetId
    }

let model
    (loadedTableName: ProvenanceTableName)
    (propertyValues: ProvenancePropertyValue list)
    (inputSets: ProvenanceSet list)
    (outputSets: ProvenanceSet list)
    (connections: ProvenanceConnection list)
    : ProvenanceModel =
    {
        LoadedTableName = loadedTableName
        PropertyValues = propertyValues |> List.map (fun value -> value.Id, value) |> Map.ofList
        InputSets = inputSets |> List.map (fun set -> set.Id, set) |> Map.ofList
        OutputSets = outputSets |> List.map (fun set -> set.Id, set) |> Map.ofList
        Connections = connections |> List.map (fun connection -> connection.Id, connection) |> Map.ofList
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
            propertyValue "pv-input-a-species" species (ProvenanceValue.Text "Arabidopsis") None (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
            propertyValue "pv-input-b-species" species (ProvenanceValue.Text "Arabidopsis") None (Some(anchor "assay-table" (Some "assay-process") species [ "Input B" ] []))
            propertyValue "pv-input-c-species" species (ProvenanceValue.Text "Arabidopsis") None (Some(anchor "assay-table" (Some "assay-process") species [ "Input C" ] []))
            propertyValue "pv-input-d-species" species (ProvenanceValue.Text "Chlamydomonas") None (Some(anchor "assay-table" (Some "assay-process") species [ "Input D" ] []))
            propertyValue "pv-input-a-temperature" temperature (ProvenanceValue.Text "12 C") None (Some(anchor "assay-table" (Some "assay-process") temperature [ "Input A" ] []))
            propertyValue "pv-input-b-temperature" temperature (ProvenanceValue.Text "12 C") None (Some(anchor "assay-table" (Some "assay-process") temperature [ "Input B" ] []))
            propertyValue "pv-input-c-temperature" temperature (ProvenanceValue.Text "24 C") None (Some(anchor "assay-table" (Some "assay-process") temperature [ "Input C" ] []))
            propertyValue "pv-output-a-analysis" analysis (ProvenanceValue.Text "Mass Spectrometry") None (Some(anchor "assay-table" (Some "assay-process") analysis [] [ "Output A" ]))
            propertyValue "pv-output-b-analysis" analysis (ProvenanceValue.Text "Mass Spectrometry") None (Some(anchor "assay-table" (Some "assay-process") analysis [] [ "Output B" ]))
            propertyValue "pv-output-c-analysis" analysis (ProvenanceValue.Text "LC-MS") None (Some(anchor "assay-table" (Some "assay-process") analysis [] [ "Output C" ]))
            propertyValue "pv-output-b-replicate-1" replicate (ProvenanceValue.Text "1") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output B" ]))
            propertyValue "pv-output-b-replicate-2" replicate (ProvenanceValue.Text "2") None (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input B" ] [ "Output B" ]))
            propertyValue "pv-previous-treatment-a" previousTreatment (ProvenanceValue.Text "Drought") None (Some(anchor "previous-study-table" (Some "previous-process") previousTreatment [ "Ancestor A" ] []))
        ]

    let inputSets =
        [
            inputSet "input-a" "assay-table" inputHeader "Input A" [ "pv-input-a-species"; "pv-input-a-temperature"; "pv-previous-treatment-a" ]
            inputSet "input-b" "assay-table" inputHeader "Input B" [ "pv-input-b-species"; "pv-input-b-temperature" ]
            inputSet "input-c" "assay-table" inputHeader "Input C" [ "pv-input-c-species"; "pv-input-c-temperature" ]
            inputSet "input-d" "assay-table" inputHeader "Input D" [ "pv-input-d-species" ]
        ]

    let outputSets =
        [
            outputSet "output-a" "assay-table" outputHeader "Output A" [ "pv-output-a-analysis" ]
            outputSet "output-b" "assay-table" outputHeader "Output B" [ "pv-output-b-analysis"; "pv-output-b-replicate-1"; "pv-output-b-replicate-2" ]
            outputSet "output-c" "assay-table" outputHeader "Output C" [ "pv-output-c-analysis" ]
            outputSet "output-d" "assay-table" outputHeader "Output D" []
            outputSet "output-e" "assay-table" outputHeader "Output E" []
        ]

    let connections =
        [
            connection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
            connection "connection-b" "assay-table" (Some "assay-process") "input-a" "output-b"
            connection "connection-c" "assay-table" (Some "assay-process") "input-b" "output-b"
            connection "connection-d" "assay-table" (Some "assay-process") "input-c" "output-c"
            connection "connection-e" "assay-table" (Some "assay-process") "input-d" "output-d"
        ]

    model "assay-table" propertyValues inputSets outputSets connections
