module Swate.Components.Shared.ProvenanceGrouping.Import

open Swate.Components.Shared.ProvenanceGrouping.Types

type ImportedPropertyValue =
    {
        Id: ProvenancePropertyValueId
        Header: ProvenancePropertyHeader
        Value: ProvenanceValue
        Unit: ProvenanceTerm option
        Source: ProvenanceWritebackAnchor option
    }

type ImportedSet =
    {
        Id: ProvenanceSetId
        TableName: ProvenanceTableName
        Header: ProvenanceIOHeader
        Name: string
        PropertyValueIds: ProvenancePropertyValueId list
    }

type ImportedConnection =
    {
        Id: ProvenanceConnectionId
        TableName: ProvenanceTableName
        ProcessName: ProvenanceProcessName option
        InputSetId: ProvenanceSetId
        OutputSetId: ProvenanceSetId
    }

type ImportedProvenance =
    {
        LoadedTableName: ProvenanceTableName
        PropertyValues: ImportedPropertyValue list
        InputSets: ImportedSet list
        OutputSets: ImportedSet list
        Connections: ImportedConnection list
    }

type ImportResult =
    {
        Model: ProvenanceModel
        Warnings: string list
    }

let private duplicateWarnings label values getId =
    values
    |> List.countBy getId
    |> List.choose (fun (id, count) ->
        if count > 1 then
            Some(sprintf "Duplicate %s id '%s' imported %i times; a single map entry is required for that id." label id count)
        else
            None)

let private toPropertyValue (propertyValue: ImportedPropertyValue) : ProvenancePropertyValue =
    {
        Id = propertyValue.Id
        Header = propertyValue.Header
        Value = propertyValue.Value
        Unit = propertyValue.Unit
        Source = propertyValue.Source
    }

let private toSet (set: ImportedSet) : ProvenanceSet =
    {
        Id = set.Id
        TableName = set.TableName
        Header = set.Header
        Name = set.Name
        PropertyValueIds = set.PropertyValueIds
    }

let private mapImportedSets (loadedTableName: ProvenanceTableName) (sets: ImportedSet list) : Map<ProvenanceSetId, ProvenanceSet> =
    sets
    |> List.fold (fun map set ->
        if set.TableName = loadedTableName then map |> Map.add set.Id (toSet set)
        else map
    ) Map.empty

let private skippedSetWarnings loadedTableName label (sets: ImportedSet list) =
    sets
    |> List.choose (fun (set: ImportedSet) ->
        if set.TableName = loadedTableName then
            None
        else
            Some(sprintf "%s set '%s' belongs to non-loaded table '%s' and was skipped." label set.Id set.TableName))

let private validateSetReferences (propertyValues: Map<_, _>) (set: ProvenanceSet) =
    [
        if System.String.IsNullOrWhiteSpace set.Name then
            sprintf "Set '%s' has an empty loaded input/output name." set.Id

        for propertyValueId in set.PropertyValueIds do
            if not (propertyValues.ContainsKey propertyValueId) then
                sprintf "Set '%s' references missing property value '%s'." set.Id propertyValueId
    ]

let private validateConnection (inputSets: Map<_, ProvenanceSet>) (outputSets: Map<_, ProvenanceSet>) (connection: ImportedConnection) =
    [
        if not (inputSets.ContainsKey connection.InputSetId) then
            sprintf "Connection '%s' references missing input set '%s'." connection.Id connection.InputSetId

        if not (outputSets.ContainsKey connection.OutputSetId) then
            sprintf "Connection '%s' references missing output set '%s'." connection.Id connection.OutputSetId
    ]

let fromImportedProvenance (imported: ImportedProvenance) : ImportResult =
    let propertyValues : Map<ProvenancePropertyValueId, ProvenancePropertyValue> =
        imported.PropertyValues
        |> List.fold (fun map propertyValue -> map |> Map.add propertyValue.Id (toPropertyValue propertyValue)) Map.empty

    let inputSets : Map<ProvenanceSetId, ProvenanceSet> = mapImportedSets imported.LoadedTableName imported.InputSets
    let outputSets : Map<ProvenanceSetId, ProvenanceSet> = mapImportedSets imported.LoadedTableName imported.OutputSets

    let loadedConnections, skippedConnections : ImportedConnection list * ImportedConnection list =
        imported.Connections
        |> List.partition (fun connection -> connection.TableName = imported.LoadedTableName)

    let connections : Map<ProvenanceConnectionId, ProvenanceConnection> =
        loadedConnections
        |> List.fold (fun map connection ->
            let nextConnection : ProvenanceConnection =
                {
                    Id = connection.Id
                    TableName = connection.TableName
                    ProcessName = connection.ProcessName
                    InputSetId = connection.InputSetId
                    OutputSetId = connection.OutputSetId
                }

            map |> Map.add connection.Id nextConnection
        ) Map.empty

    let warnings =
        [
            if System.String.IsNullOrWhiteSpace imported.LoadedTableName then
                "LoadedTableName is empty."

            yield! duplicateWarnings "property value" imported.PropertyValues (fun value -> value.Id)
            yield! duplicateWarnings "input set" imported.InputSets (fun set -> set.Id)
            yield! duplicateWarnings "output set" imported.OutputSets (fun set -> set.Id)
            yield! duplicateWarnings "connection" imported.Connections (fun connection -> connection.Id)

            yield! skippedSetWarnings imported.LoadedTableName "Input" imported.InputSets
            yield! skippedSetWarnings imported.LoadedTableName "Output" imported.OutputSets

            for _, set in inputSets |> Map.toList do
                yield! validateSetReferences propertyValues set

            for _, set in outputSets |> Map.toList do
                yield! validateSetReferences propertyValues set

            for connection in loadedConnections do
                yield! validateConnection inputSets outputSets connection

            for connection in skippedConnections do
                sprintf "Connection '%s' belongs to non-loaded table '%s' and was skipped." connection.Id connection.TableName
        ]

    {
        Model =
            {
                LoadedTableName = imported.LoadedTableName
                PropertyValues = propertyValues
                InputSets = inputSets
                OutputSets = outputSets
                Connections = connections
            }
        Warnings = warnings
    }
