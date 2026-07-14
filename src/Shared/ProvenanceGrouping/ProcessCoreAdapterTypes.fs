module Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes

open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Session

type ProcessCoreTableLocation = {
    DatasetPath: string list
    TableName: string
}

[<RequireQualifiedAccess>]
type ProcessCoreNodeKind =
    | Sample
    | Data

type ProcessCoreProcessLocation = {
    DatasetPath: string list
    ProcessIndex: int
    ExpectedName: string
}

type ProcessCoreNodeLocation = {
    Kind: ProcessCoreNodeKind
    Key: string
}

type ProcessCoreEndpointOccurrence = {
    Process: ProcessCoreProcessLocation
    Side: ProvenanceSide
    Position: int
    Node: ProcessCoreNodeLocation
}

type ProcessCoreEndpointLocation = {
    Header: ProvenanceIOHeader
    Occurrences: ProcessCoreEndpointOccurrence list
}

type ProcessCoreConnectionLocation = {
    Process: ProcessCoreProcessLocation
    InputPosition: int
    OutputPosition: int
    InputSetId: ProvenanceSetId
    OutputSetId: ProvenanceSetId
}

[<RequireQualifiedAccess>]
type ProcessCoreAnnotationOwner =
    | NodeAdditionalProperty of ProcessCoreNodeLocation
    | ProcessParameterValue of ProcessCoreProcessLocation
    | RecipeComponent of ProcessCoreProcessLocation

type ProcessCoreAnnotationFingerprint = {
    Name: string
    Value: string option
    Unit: string option
    NameTAN: string option
    ValueTAN: string option
    UnitTAN: string option
    AdditionalType: string option
}

type ProcessCoreAnnotationLocation = {
    Owner: ProcessCoreAnnotationOwner
    Position: int
    Fingerprint: ProcessCoreAnnotationFingerprint
}

type ProcessCoreWritebackIndex = {
    LoadedTable: ProcessCoreTableLocation
    InitialSourceId: ProvenanceSourceId
    ArcFingerprint: string
    EndpointLocations: Map<ProvenanceSetId, ProcessCoreEndpointLocation>
    PropertyValueLocations: Map<ProvenancePropertyValueId, ProcessCoreAnnotationLocation list>
    ConnectionLocations: Map<ProvenanceConnectionId, ProcessCoreConnectionLocation>
}

[<RequireQualifiedAccess>]
type ProcessCoreConversionWarning =
    | BlankEndpoint of ProcessCoreProcessLocation * ProvenanceSide * int
    | BlankAnnotationName of ProcessCoreAnnotationOwner * int
    | PropertyWithoutEndpoint of ProcessCoreProcessLocation * string

[<RequireQualifiedAccess>]
type ProcessCoreConversionError =
    | EmptyDatasetPath
    | DatasetNotFound of string list
    | AmbiguousDatasetPath of string list
    | ProcessGroupNotFound of ProcessCoreTableLocation

type ProcessCoreConversionResult = {
    Model: ProvenanceModel
    Index: ProcessCoreWritebackIndex
    Warnings: ProcessCoreConversionWarning list
}

[<RequireQualifiedAccess>]
type ProcessCoreWritebackError =
    | StaleArc
    | InitialLayerNotFound of ProvenanceSourceId
    | InvalidLayerOrder of ProvenanceLayerId list
    | LayerNotFound of ProvenanceLayerId
    | SetNotFound of ProvenanceSetId
    | ConnectionNotFound of ProvenanceConnectionId
    | PropertyNotFound of ProvenancePropertyValueId
    | SourceLocationNotFound of string
    | AmbiguousSourceLocation of string
    | BlankLayerName of ProvenanceLayerId
    | DuplicateLayerName of string
    | ConflictingNodeIdentity of nodeKey: string * setIds: ProvenanceSetId list
    | ConflictingAnnotationIdentity of
        owner: string *
        existing: ProcessCoreAnnotationFingerprint *
        requested: ProcessCoreAnnotationFingerprint
    | GeneratedIdFormatChanged of id: string * expectedPrefix: string
    | UnsupportedEndpointKind of string
    | UnsupportedPropertyKind of string
    | StructuralPreviousContextEdit of ProvenanceSourceId
    | InvalidReferenceLink of ProvenanceReferenceLink
    | InvalidPatchTarget of string

type ProcessCoreWritebackSummary = {
    UpdatedAnnotations: int
    AddedAnnotations: int
    AddedNodes: int
    AddedProcesses: int
    RemovedProcesses: int
}

module ProcessCoreKinds =
    let sampleEndpoint = ProvenanceKind.create "process-core:endpoint:sample" "Sample"
    let dataEndpoint = ProvenanceKind.create "process-core:endpoint:data" "Data"

    let characteristic =
        ProvenanceKind.create "process-core:property:characteristic" "Characteristic"

    let factor = ProvenanceKind.create "process-core:property:factor" "Factor"
    let parameter = ProvenanceKind.create "process-core:property:parameter" "Parameter"

    let componentKind =
        ProvenanceKind.create "process-core:property:component" "Component"

    let additionalProperty =
        ProvenanceKind.create "process-core:property:additional" "Additional property"
