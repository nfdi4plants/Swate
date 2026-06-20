module Swate.Electron.Shared.DTOs.ProvenanceGroupingDto

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.ARCtrlConverter

[<RequireQualifiedAccess>]
type ProvenanceTableScopeDto =
    | Study
    | Assay
    | Run

type ProvenanceTableSelectionDto = {
    Scope: ProvenanceTableScopeDto
    ParentIdentifier: string
    TableName: string
    DisplayLabel: string
}

type ProvenanceMapEntryDto<'T> = { Key: string; Value: 'T }

type ProvenanceTermDto = {
    Name: string
    TermSource: string option
    TermAccession: string option
}

type ProvenanceKindDto = { Id: string; Label: string }

type ProvenanceIOHeaderDto = {
    Kind: ProvenanceKindDto
    Text: string
}

type ProvenancePropertyHeaderDto = {
    Kind: ProvenanceKindDto
    Category: ProvenanceTermDto
}

type ProvenanceValueDto = {
    Kind: string
    Text: string option
    Integer: int option
    Float: float option
    Term: ProvenanceTermDto option
}

type ProvenanceWritebackAnchorDto = {
    TableName: ProvenanceTableName
    ProcessName: ProvenanceProcessName option
    Header: ProvenancePropertyHeaderDto
    InputNames: string[]
    OutputNames: string[]
}

type ProvenancePropertyValueDto = {
    Id: ProvenancePropertyValueId
    Header: ProvenancePropertyHeaderDto
    Value: ProvenanceValueDto
    Unit: ProvenanceTermDto option
    Source: ProvenanceWritebackAnchorDto option
}

type ProvenanceSetDto = {
    Id: ProvenanceSetId
    TableName: ProvenanceTableName
    Header: ProvenanceIOHeaderDto
    Name: string
    PropertyValueIds: ProvenancePropertyValueId[]
    InheritedPropertyValueIds: ProvenanceMapEntryDto<ProvenancePropertyValueId[]>[]
}

type ProvenanceConnectionDto = {
    Id: ProvenanceConnectionId
    TableName: ProvenanceTableName
    ProcessName: ProvenanceProcessName option
    InputSetId: ProvenanceSetId
    OutputSetId: ProvenanceSetId
}

type ProvenanceModelDto = {
    LoadedTableName: ProvenanceTableName
    PropertyValues: ProvenanceMapEntryDto<ProvenancePropertyValueDto>[]
    InputSets: ProvenanceMapEntryDto<ProvenanceSetDto>[]
    OutputSets: ProvenanceMapEntryDto<ProvenanceSetDto>[]
    Connections: ProvenanceMapEntryDto<ProvenanceConnectionDto>[]
}

type ProvenanceLoadResultDto = {
    Selection: ProvenanceTableSelectionDto
    Model: ProvenanceModelDto
    Warnings: string[]
}

module private MapDto =

    let ofMap convert (map: Map<string, 'T>) =
        map
        |> Map.toArray
        |> Array.map (fun (key, value) -> { Key = key; Value = convert value })

    let toMap convert entries =
        entries
        |> Array.map (fun entry -> entry.Key, convert entry.Value)
        |> Map.ofArray

module ProvenanceTermDto =

    let ofModel (term: ProvenanceTerm) = {
        Name = term.Name
        TermSource = term.TermSource
        TermAccession = term.TermAccession
    }

    let toModel (term: ProvenanceTermDto) : ProvenanceTerm = {
        Name = term.Name
        TermSource = term.TermSource
        TermAccession = term.TermAccession
    }

module ProvenanceKindDto =

    let ofModel (kind: ProvenanceKind) = { Id = kind.Id; Label = kind.Label }

    let toModel (kind: ProvenanceKindDto) : ProvenanceKind = { Id = kind.Id; Label = kind.Label }

module ProvenanceIOHeaderDto =

    let ofModel (header: ProvenanceIOHeader) = {
        Kind = ProvenanceKindDto.ofModel header.Kind
        Text = header.Text
    }

    let toModel (header: ProvenanceIOHeaderDto) : ProvenanceIOHeader = {
        Kind = ProvenanceKindDto.toModel header.Kind
        Text = header.Text
    }

module ProvenancePropertyHeaderDto =

    let ofModel (header: ProvenancePropertyHeader) = {
        Kind = ProvenanceKindDto.ofModel header.Kind
        Category = ProvenanceTermDto.ofModel header.Category
    }

    let toModel (header: ProvenancePropertyHeaderDto) : ProvenancePropertyHeader = {
        Kind = ProvenanceKindDto.toModel header.Kind
        Category = ProvenanceTermDto.toModel header.Category
    }

module ProvenanceValueDto =

    let private empty kind = {
        Kind = kind
        Text = None
        Integer = None
        Float = None
        Term = None
    }

    let ofModel value =
        match value with
        | ProvenanceValue.Text text -> { empty "Text" with Text = Some text }
        | ProvenanceValue.Integer integer -> {
            empty "Integer" with
                Integer = Some integer
          }
        | ProvenanceValue.Float float -> {
            empty "Float" with
                Float = Some float
          }
        | ProvenanceValue.Term term -> {
            empty "Term" with
                Term = Some(ProvenanceTermDto.ofModel term)
          }

    let toModel value =
        match value.Kind with
        | "Integer" ->
            value.Integer
            |> Option.map ProvenanceValue.Integer
            |> Option.defaultValue (ProvenanceValue.Text "")
        | "Float" ->
            value.Float
            |> Option.map ProvenanceValue.Float
            |> Option.defaultValue (ProvenanceValue.Text "")
        | "Term" ->
            value.Term
            |> Option.map (ProvenanceTermDto.toModel >> ProvenanceValue.Term)
            |> Option.defaultValue (ProvenanceValue.Text "")
        | "Text"
        | _ -> value.Text |> Option.defaultValue "" |> ProvenanceValue.Text

module ProvenanceWritebackAnchorDto =

    let ofModel (source: ProvenanceWritebackAnchor) = {
        TableName = source.TableName
        ProcessName = source.ProcessName
        Header = ProvenancePropertyHeaderDto.ofModel source.Header
        InputNames = source.InputNames |> List.toArray
        OutputNames = source.OutputNames |> List.toArray
    }

    let toModel (source: ProvenanceWritebackAnchorDto) : ProvenanceWritebackAnchor = {
        TableName = source.TableName
        ProcessName = source.ProcessName
        Header = ProvenancePropertyHeaderDto.toModel source.Header
        InputNames = source.InputNames |> Array.toList
        OutputNames = source.OutputNames |> Array.toList
    }

module ProvenancePropertyValueDto =

    let ofModel (propertyValue: ProvenancePropertyValue) = {
        Id = propertyValue.Id
        Header = ProvenancePropertyHeaderDto.ofModel propertyValue.Header
        Value = ProvenanceValueDto.ofModel propertyValue.Value
        Unit = propertyValue.Unit |> Option.map ProvenanceTermDto.ofModel
        Source = propertyValue.Source |> Option.map ProvenanceWritebackAnchorDto.ofModel
    }

    let toModel (propertyValue: ProvenancePropertyValueDto) : ProvenancePropertyValue = {
        Id = propertyValue.Id
        Header = ProvenancePropertyHeaderDto.toModel propertyValue.Header
        Value = ProvenanceValueDto.toModel propertyValue.Value
        Unit = propertyValue.Unit |> Option.map ProvenanceTermDto.toModel
        Source = propertyValue.Source |> Option.map ProvenanceWritebackAnchorDto.toModel
    }

module ProvenanceSetDto =

    let ofModel (set: ProvenanceSet) = {
        Id = set.Id
        TableName = set.TableName
        Header = ProvenanceIOHeaderDto.ofModel set.Header
        Name = set.Name
        PropertyValueIds = set.PropertyValueIds |> List.toArray
        InheritedPropertyValueIds = set.InheritedPropertyValueIds |> MapDto.ofMap List.toArray
    }

    let toModel (set: ProvenanceSetDto) : ProvenanceSet = {
        Id = set.Id
        TableName = set.TableName
        Header = ProvenanceIOHeaderDto.toModel set.Header
        Name = set.Name
        PropertyValueIds = set.PropertyValueIds |> Array.toList
        InheritedPropertyValueIds = set.InheritedPropertyValueIds |> MapDto.toMap Array.toList
    }

module ProvenanceConnectionDto =

    let ofModel (connection: ProvenanceConnection) = {
        Id = connection.Id
        TableName = connection.TableName
        ProcessName = connection.ProcessName
        InputSetId = connection.InputSetId
        OutputSetId = connection.OutputSetId
    }

    let toModel (connection: ProvenanceConnectionDto) : ProvenanceConnection = {
        Id = connection.Id
        TableName = connection.TableName
        ProcessName = connection.ProcessName
        InputSetId = connection.InputSetId
        OutputSetId = connection.OutputSetId
    }

module ProvenanceModelDto =

    let ofModel (model: ProvenanceModel) = {
        LoadedTableName = model.LoadedTableName
        PropertyValues = model.PropertyValues |> MapDto.ofMap ProvenancePropertyValueDto.ofModel
        InputSets = model.InputSets |> MapDto.ofMap ProvenanceSetDto.ofModel
        OutputSets = model.OutputSets |> MapDto.ofMap ProvenanceSetDto.ofModel
        Connections = model.Connections |> MapDto.ofMap ProvenanceConnectionDto.ofModel
    }

    let toModel (model: ProvenanceModelDto) : ProvenanceModel = {
        LoadedTableName = model.LoadedTableName
        PropertyValues = model.PropertyValues |> MapDto.toMap ProvenancePropertyValueDto.toModel
        InputSets = model.InputSets |> MapDto.toMap ProvenanceSetDto.toModel
        OutputSets = model.OutputSets |> MapDto.toMap ProvenanceSetDto.toModel
        Connections = model.Connections |> MapDto.toMap ProvenanceConnectionDto.toModel
    }

module ProvenanceTableSelectionDto =

    let private scopeText scope =
        match scope with
        | ProvenanceTableScopeDto.Study -> "Study"
        | ProvenanceTableScopeDto.Assay -> "Assay"
        | ProvenanceTableScopeDto.Run -> "Run"

    let private toArcScope scope =
        match scope with
        | ProvenanceTableScopeDto.Study -> ArcTableScope.Study
        | ProvenanceTableScopeDto.Assay -> ArcTableScope.Assay
        | ProvenanceTableScopeDto.Run -> ArcTableScope.Run

    let private ofArcScope scope =
        match scope with
        | ArcTableScope.Study -> ProvenanceTableScopeDto.Study
        | ArcTableScope.Assay -> ProvenanceTableScopeDto.Assay
        | ArcTableScope.Run -> ProvenanceTableScopeDto.Run

    let create scope parentIdentifier tableName = {
        Scope = scope
        ParentIdentifier = parentIdentifier
        TableName = tableName
        DisplayLabel = $"{scopeText scope} {parentIdentifier} / {tableName}"
    }

    let toArcLocation selection : ArcTableLocation = {
        Scope = toArcScope selection.Scope
        ParentIdentifier = selection.ParentIdentifier
        TableName = selection.TableName
    }

    let fromArcLocation (location: ArcTableLocation) =
        create (ofArcScope location.Scope) location.ParentIdentifier location.TableName
