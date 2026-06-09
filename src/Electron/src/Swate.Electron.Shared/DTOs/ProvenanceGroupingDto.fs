module Swate.Electron.Shared.DTOs.ProvenanceGroupingDto

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.ARCtrlConverter

[<RequireQualifiedAccess>]
type ProvenanceTableScopeDto =
    | Study
    | Assay
    | Run

type ProvenanceTableSelectionDto =
    {
        Scope: ProvenanceTableScopeDto
        ParentIdentifier: string
        TableName: string
        DisplayLabel: string
    }

type ProvenanceLoadResultDto =
    {
        Selection: ProvenanceTableSelectionDto
        Model: ProvenanceModel
        Warnings: string[]
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

    let create scope parentIdentifier tableName =
        {
            Scope = scope
            ParentIdentifier = parentIdentifier
            TableName = tableName
            DisplayLabel = $"{scopeText scope} {parentIdentifier} / {tableName}"
        }

    let toArcLocation selection : ArcTableLocation =
        {
            Scope = toArcScope selection.Scope
            ParentIdentifier = selection.ParentIdentifier
            TableName = selection.TableName
        }

    let fromArcLocation (location: ArcTableLocation) =
        create (ofArcScope location.Scope) location.ParentIdentifier location.TableName
