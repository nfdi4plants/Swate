module Swate.Components.Shared.ProvenanceGrouping.Grouping

open Swate.Components.Shared.ProvenanceGrouping.Types

type GroupingKey =
    {
        Header: ProvenancePropertyHeader
    }

type DisplayMember =
    {
        SetId: ProvenanceSetId
        Name: string
        PropertyValueIds: ProvenancePropertyValueId list
    }

type DisplayGroupingValue =
    {
        Key: GroupingKey
        Value: ProvenanceValue
        Unit: ProvenanceTerm option
    }

type DisplayGroup =
    {
        Id: string
        TableName: ProvenanceTableName
        Side: ProvenanceSide
        GroupingValues: DisplayGroupingValue list
        Members: DisplayMember list
    }

type DisplayConnection =
    {
        Id: string
        SourceGroupId: string
        TargetGroupId: string
        ConnectionIds: ProvenanceConnectionId list
    }

let private mapValues map =
    map |> Map.toList |> List.map snd

let private valueText (value: ProvenanceValue) (unit: ProvenanceTerm option) =
    let text =
        match value with
        | ProvenanceValue.Text value -> value
        | ProvenanceValue.Integer value -> string value
        | ProvenanceValue.Float value -> string value
        | ProvenanceValue.Term term -> term.Name

    match unit with
    | Some unit -> $"{text} {unit.Name}"
    | None -> text

let private groupingValueText (groupingValue: DisplayGroupingValue) =
    sprintf "%s=%s" groupingValue.Key.Header.Category.Name (valueText groupingValue.Value groupingValue.Unit)

let private sideText side =
    match side with
    | ProvenanceSide.Input -> "input"
    | ProvenanceSide.Output -> "output"

let private loadedSets (model: ProvenanceModel) side =
    let source =
        match side with
        | ProvenanceSide.Input -> model.InputSets
        | ProvenanceSide.Output -> model.OutputSets

    source
    |> mapValues
    |> List.filter (fun set -> set.TableName = model.LoadedTableName)
    |> List.sortBy (fun set -> set.Name, set.Id)

let private setPropertyValues (model: ProvenanceModel) (set: ProvenanceSet) =
    set.PropertyValueIds
    |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)

let private valuesForKey model set key =
    setPropertyValues model set
    |> List.filter (fun propertyValue -> propertyValue.Header = key.Header)
    |> List.groupBy (fun propertyValue -> propertyValue.Value, propertyValue.Unit)
    |> List.map (fun ((value, unit), propertyValues) ->
        key,
        value,
        unit,
        propertyValues
        |> List.map (fun propertyValue -> propertyValue.Id)
        |> List.distinct
        |> List.sort)
    |> List.sortBy (fun (_, value, unit, _) -> valueText value unit)

let private combinations values =
    let rec loop collected remaining =
        match remaining with
        | [] -> [ List.rev collected ]
        | head :: tail ->
            head
            |> List.collect (fun value -> loop (value :: collected) tail)

    loop [] values

let private displayMember (set: ProvenanceSet) propertyValueIds =
    {
        SetId = set.Id
        Name = set.Name
        PropertyValueIds = propertyValueIds |> List.distinct
    }

let private groupId side (values: DisplayGroupingValue list) fallbackSetId =
    match values with
    | [] -> sprintf "%s:%s" (sideText side) fallbackSetId
    | _ ->
        values
        |> List.map groupingValueText
        |> String.concat "|"
        |> sprintf "%s:%s" (sideText side)

let displayGroups (model: ProvenanceModel) side groupingKeys =
    let sets = loadedSets model side

    match groupingKeys with
    | [] ->
        sets
        |> List.map (fun set ->
            {
                Id = groupId side [] set.Id
                TableName = set.TableName
                Side = side
                GroupingValues = []
                Members = [ displayMember set set.PropertyValueIds ]
            })
    | keys ->
        let grouped =
            [
                for set in sets do
                    let keyValues =
                        keys
                        |> List.map (valuesForKey model set)

                    if keyValues |> List.exists List.isEmpty then
                        yield groupId side [] set.Id, set.TableName, [], displayMember set set.PropertyValueIds
                    else
                        for combination in combinations keyValues do
                            let groupingValues =
                                combination
                                |> List.map (fun (key, value, unit, _) ->
                                    {
                                        Key = key
                                        Value = value
                                        Unit = unit
                                    })

                            yield groupId side groupingValues set.Id, set.TableName, groupingValues, displayMember set set.PropertyValueIds
            ]

        grouped
        |> List.groupBy (fun (groupId, _, _, _) -> groupId)
        |> List.map (fun (id, groupedMembers) ->
            let _, tableName, groupingValues, _ = groupedMembers.Head

            {
                Id = id
                TableName = tableName
                Side = side
                GroupingValues = groupingValues
                Members =
                    groupedMembers
                    |> List.map (fun (_, _, _, member') -> member')
                    |> List.sortBy (fun member' -> member'.Name, member'.SetId)
            })
        |> List.sortBy (fun group -> group.Id)

let private groupIdsBySetId groups =
    groups
    |> List.collect (fun group -> group.Members |> List.map (fun member' -> member'.SetId, group.Id))
    |> List.groupBy fst
    |> List.map (fun (setId, grouped) -> setId, grouped |> List.map snd)
    |> Map.ofList

let displayConnections (model: ProvenanceModel) inputGroups outputGroups =
    if List.isEmpty inputGroups || List.isEmpty outputGroups then
        []
    else
        let inputGroupBySetId = groupIdsBySetId inputGroups
        let outputGroupBySetId = groupIdsBySetId outputGroups

        model.Connections
        |> mapValues
        |> List.filter (fun connection -> connection.TableName = model.LoadedTableName)
        |> List.collect (fun connection ->
            match inputGroupBySetId.TryFind connection.InputSetId, outputGroupBySetId.TryFind connection.OutputSetId with
            | Some inputGroupIds, Some outputGroupIds ->
                [
                    for inputGroupId in inputGroupIds do
                        for outputGroupId in outputGroupIds do
                            yield (inputGroupId, outputGroupId), connection.Id
                ]
            | _ -> [])
        |> List.groupBy fst
        |> List.map (fun ((inputGroupId, outputGroupId), grouped) ->
            {
                Id = sprintf "%s-to-%s" inputGroupId outputGroupId
                SourceGroupId = inputGroupId
                TargetGroupId = outputGroupId
                ConnectionIds = grouped |> List.map snd |> List.distinct |> List.sort
            })
        |> List.sortBy (fun connection -> connection.Id)
