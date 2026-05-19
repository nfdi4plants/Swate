module Swate.Components.Shared.ProvenanceGrouping.Edit

open Swate.Components.Shared.ProvenanceGrouping.Types

[<RequireQualifiedAccess>]
type EditError =
    | PropertyNotFound of ProvenancePropertyValueId
    | SetNotFound of ProvenanceSetId
    | ConnectionNotFound of ProvenanceConnectionId
    | TableNotLoaded of ProvenanceTableName
    | MissingSourceAnchor of ProvenancePropertyValueId
    | DuplicateHeader of tableName: ProvenanceTableName * header: ProvenancePropertyHeader
    | PreviousContextCreationNotAllowed of tableName: ProvenanceTableName
    | PreviousContextConnectionCreationNotAllowed of tableName: ProvenanceTableName

[<RequireQualifiedAccess>]
type ProvenancePropertyTarget =
    | InputSets of ProvenanceSetId list
    | OutputSets of ProvenanceSetId list
    | Connections of ProvenanceConnectionId list

[<RequireQualifiedAccess>]
type ProvenanceTablePatch =
    | UpdatePropertyValue of
        propertyValueId: ProvenancePropertyValueId *
        source: ProvenanceWritebackAnchor *
        oldValue: ProvenanceValue *
        newValue: ProvenanceValue *
        unit: ProvenanceTerm option
    | AddLoadedPropertyValue of
        target: ProvenancePropertyTarget *
        copiedFrom: ProvenancePropertyValueId option *
        header: ProvenancePropertyHeader *
        value: ProvenanceValue *
        unit: ProvenanceTerm option
    | AddLoadedConnection of
        tableName: ProvenanceTableName *
        processName: ProvenanceProcessName option *
        inputSetId: ProvenanceSetId *
        outputSetId: ProvenanceSetId

type CreateLoadedPropertyValueCommand =
    {
        Target: ProvenancePropertyTarget
        CopiedFrom: ProvenancePropertyValueId option
        Header: ProvenancePropertyHeader
        Value: ProvenanceValue
        Unit: ProvenanceTerm option
    }

type EditResult =
    Result<ProvenanceModel * ProvenanceTablePatch list, EditError>

let private mapValues map =
    map |> Map.toList |> List.map snd

let private nextPropertyValueId (model: ProvenanceModel) =
    let rec loop index =
        let id = sprintf "property-value-%i" index
        if model.PropertyValues.ContainsKey id then loop (index + 1) else id

    loop (model.PropertyValues.Count + 1)

let private nextConnectionId (model: ProvenanceModel) =
    let rec loop index =
        let id = sprintf "connection-%i" index
        if model.Connections.ContainsKey id then loop (index + 1) else id

    loop (model.Connections.Count + 1)

let private loadedSet (model: ProvenanceModel) (set: ProvenanceSet) =
    if set.TableName = model.LoadedTableName then
        Ok set
    else
        Error(EditError.PreviousContextCreationNotAllowed set.TableName)

let private addPropertyValueId propertyValueId (set: ProvenanceSet) =
    if set.PropertyValueIds |> List.contains propertyValueId then
        set
    else
        { set with PropertyValueIds = set.PropertyValueIds @ [ propertyValueId ] }

let private updateSets propertyValueId targetSetIds sets =
    targetSetIds
    |> List.fold (fun state setId ->
        state
        |> Map.change setId (Option.map (addPropertyValueId propertyValueId))) sets

let private chooseInputSet (model: ProvenanceModel) setId =
    match model.InputSets.TryFind setId with
    | Some set -> loadedSet model set
    | None -> Error(EditError.SetNotFound setId)

let private chooseOutputSet (model: ProvenanceModel) setId =
    match model.OutputSets.TryFind setId with
    | Some set -> loadedSet model set
    | None -> Error(EditError.SetNotFound setId)

let private chooseConnection (model: ProvenanceModel) connectionId =
    match model.Connections.TryFind connectionId with
    | Some connection when connection.TableName = model.LoadedTableName -> Ok connection
    | Some connection -> Error(EditError.PreviousContextConnectionCreationNotAllowed connection.TableName)
    | None -> Error(EditError.ConnectionNotFound connectionId)

let private collectResults results =
    let rec loop collected remaining =
        match remaining with
        | [] -> Ok(List.rev collected)
        | Ok value :: tail -> loop (value :: collected) tail
        | Error error :: _ -> Error error

    loop [] results

type private ResolvedPropertyTarget =
    {
        InputSets: ProvenanceSet list
        OutputSets: ProvenanceSet list
    }

let private targetSets model target =
    match target with
    | ProvenancePropertyTarget.InputSets setIds ->
        match setIds |> List.map (chooseInputSet model) |> collectResults with
        | Ok inputSets -> Ok { InputSets = inputSets; OutputSets = [] }
        | Error error -> Error error
    | ProvenancePropertyTarget.OutputSets setIds ->
        match setIds |> List.map (chooseOutputSet model) |> collectResults with
        | Ok outputSets -> Ok { InputSets = []; OutputSets = outputSets }
        | Error error -> Error error
    | ProvenancePropertyTarget.Connections connectionIds ->
        match connectionIds |> List.map (chooseConnection model) |> collectResults with
        | Error error -> Error error
        | Ok connections ->
            let inputResults = connections |> List.map (fun connection -> chooseInputSet model connection.InputSetId)
            let outputResults = connections |> List.map (fun connection -> chooseOutputSet model connection.OutputSetId)

            match collectResults inputResults, collectResults outputResults with
            | Ok inputSets, Ok outputSets -> Ok { InputSets = inputSets; OutputSets = outputSets }
            | Error error, _ -> Error error
            | _, Error error -> Error error

let private sourceFromTarget (model: ProvenanceModel) header target resolvedTarget =
    let processName =
        match target with
        | ProvenancePropertyTarget.Connections connectionIds ->
            connectionIds
            |> List.tryPick (fun connectionId -> model.Connections.TryFind connectionId |> Option.bind (fun connection -> connection.ProcessName))
        | _ -> None

    {
        TableName = model.LoadedTableName
        ProcessName = processName
        Header = header
        InputNames =
            resolvedTarget.InputSets
            |> List.map (fun set -> set.Name)
            |> List.distinct
        OutputNames =
            resolvedTarget.OutputSets
            |> List.map (fun set -> set.Name)
            |> List.distinct
    }

let private sourceFromLoadedMembership (model: ProvenanceModel) (propertyValue: ProvenancePropertyValue) =
    let inputSets =
        model.InputSets
        |> mapValues
        |> List.filter (fun set -> set.PropertyValueIds |> List.contains propertyValue.Id)

    let outputSets =
        model.OutputSets
        |> mapValues
        |> List.filter (fun set -> set.PropertyValueIds |> List.contains propertyValue.Id)

    match inputSets, outputSets with
    | [], [] -> None
    | _ ->
        Some(
            sourceFromTarget
                model
                propertyValue.Header
                (ProvenancePropertyTarget.InputSets [])
                { InputSets = inputSets; OutputSets = outputSets })

let updatePropertyValue propertyValueId newValue newUnit (model: ProvenanceModel) : EditResult =
    match model.PropertyValues.TryFind propertyValueId with
    | None -> Error(EditError.PropertyNotFound propertyValueId)
    | Some propertyValue ->
        match propertyValue.Source |> Option.orElseWith (fun () -> sourceFromLoadedMembership model propertyValue) with
        | None -> Error(EditError.MissingSourceAnchor propertyValueId)
        | Some source ->
            let nextPropertyValue : ProvenancePropertyValue =
                {
                    propertyValue with
                        Value = newValue
                        Unit = newUnit
                        Source = Some source
                }

            let nextModel =
                {
                    model with
                        PropertyValues = model.PropertyValues |> Map.add propertyValueId nextPropertyValue
                }

            Ok(
                nextModel,
                [
                    ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, source, propertyValue.Value, newValue, newUnit)
                ])

let createLoadedPropertyValue (command: CreateLoadedPropertyValueCommand) (model: ProvenanceModel) : EditResult =
    match targetSets model command.Target with
    | Error error -> Error error
    | Ok resolvedTarget ->
        let propertyValueId = nextPropertyValueId model
        let source = sourceFromTarget model command.Header command.Target resolvedTarget

        let propertyValue : ProvenancePropertyValue =
            {
                Id = propertyValueId
                Header = command.Header
                Value = command.Value
                Unit = command.Unit
                Source = Some source
            }

        let inputSetIds =
            resolvedTarget.InputSets
            |> List.map (fun set -> set.Id)

        let outputSetIds =
            resolvedTarget.OutputSets
            |> List.map (fun set -> set.Id)

        let nextModel =
            {
                model with
                    PropertyValues = model.PropertyValues |> Map.add propertyValueId propertyValue
                    InputSets = model.InputSets |> updateSets propertyValueId inputSetIds
                    OutputSets = model.OutputSets |> updateSets propertyValueId outputSetIds
            }

        Ok(
            nextModel,
            [
                ProvenanceTablePatch.AddLoadedPropertyValue(command.Target, command.CopiedFrom, command.Header, command.Value, command.Unit)
            ])

let copyPropertyValueToLoadedTarget propertyValueId target (model: ProvenanceModel) : EditResult =
    match model.PropertyValues.TryFind propertyValueId with
    | None -> Error(EditError.PropertyNotFound propertyValueId)
    | Some propertyValue ->
        createLoadedPropertyValue
            {
                Target = target
                CopiedFrom = Some propertyValueId
                Header = propertyValue.Header
                Value = propertyValue.Value
                Unit = propertyValue.Unit
            }
            model

let connectSets inputSetId outputSetId processName (model: ProvenanceModel) : EditResult =
    match chooseInputSet model inputSetId, chooseOutputSet model outputSetId with
    | Error error, _ -> Error error
    | _, Error error -> Error error
    | Ok _, Ok _ ->
        let connectionId = nextConnectionId model

        let connection : ProvenanceConnection =
            {
                Id = connectionId
                TableName = model.LoadedTableName
                ProcessName = processName
                InputSetId = inputSetId
                OutputSetId = outputSetId
            }

        let nextModel =
            {
                model with
                    Connections = model.Connections |> Map.add connectionId connection
            }

        Ok(
            nextModel,
            [
                ProvenanceTablePatch.AddLoadedConnection(model.LoadedTableName, processName, inputSetId, outputSetId)
            ])
