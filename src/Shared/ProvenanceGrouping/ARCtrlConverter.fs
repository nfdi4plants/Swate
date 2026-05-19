module Swate.Components.Shared.ProvenanceGrouping.ARCtrlConverter

open System
open ARCtrl
open Swate.Components.Shared.ProvenanceGrouping.Types

/// ARC table container that owns the selected or source table.
/// The converter uses this with `ParentIdentifier` and `TableName` because table names alone are not unique across a full ARC.
[<RequireQualifiedAccess>]
type ArcTableScope =
    /// Table belongs to `ARC.Studies`.
    | Study
    /// Table belongs to `ARC.Assays`.
    | Assay
    /// Table belongs to `ARC.Runs`.
    | Run

/// Stable, serializable location of an ARCtrl table inside a loaded ARC.
/// This is the adapter-side replacement for holding an ARCtrl object pointer.
type ArcTableLocation =
    {
        /// Whether the table is inside a study, assay, or run collection.
        Scope: ArcTableScope
        /// Identifier of the owning study, assay, or run.
        ParentIdentifier: string
        /// `ArcTable.Name` of the source table.
        TableName: string
    }

/// Options for converting one loaded table from a loaded `ARC`.
type ArcProvenanceConverterOptions =
    {
        /// The study, assay, or run table currently loaded into the provenance editor.
        LoadedTable: ArcTableLocation
        /// When true, previous tables that point into loaded inputs are collapsed into attached property values.
        IncludePreviousContext: bool
    }

/// Conversion failures that prevent a usable `ProvenanceModel` from being created.
[<RequireQualifiedAccess>]
type ArcProvenanceConversionError =
    /// No table matched the requested location.
    | LoadedTableNotFound of ArcTableLocation
    /// More than one table matched the requested location.
    | LoadedTableAmbiguous of ArcTableLocation * int
    /// The selected table has no populated input endpoint cells.
    | LoadedTableHasNoInputs of ArcTableLocation
    /// The selected table has no populated output endpoint cells.
    | LoadedTableHasNoOutputs of ArcTableLocation

/// ARCtrl location of a loaded endpoint.
/// The endpoint role is resolved by checking whether the ID lives in `model.InputSets` or `model.OutputSets`.
type ArcEndpointLocation =
    {
        /// Table containing the endpoint cell.
        Table: ArcTableLocation
        /// Converted input/output header.
        Header: ProvenanceIOHeader
        /// Actual input/output name from the loaded table cell.
        Name: string
    }

/// ARCtrl location of a property value occurrence for later writeback.
/// This mirrors `ProvenanceWritebackAnchor` and adds full ARC table location.
type ArcWritebackLocation =
    {
        /// Study, assay, or run table containing this property value.
        Table: ArcTableLocation
        /// Process name derived from ARCtrl table semantics.
        ProcessName: ProvenanceProcessName option
        /// Converted property header.
        Header: ProvenancePropertyHeader
        /// Input names that identify the row/process context.
        InputNames: string list
        /// Output names that identify the row/process context.
        OutputNames: string list
    }

/// ARCtrl location of a loaded input-to-output connection.
/// Connections exist only for the selected loaded table, not for collapsed previous context.
type ArcConnectionLocation =
    {
        /// Loaded table containing this input/output connection.
        Table: ArcTableLocation
        /// Process name derived from the row that produced this connection.
        ProcessName: ProvenanceProcessName option
        /// Loaded input set ID in the core model.
        InputSetId: ProvenanceSetId
        /// Loaded output set ID in the core model.
        OutputSetId: ProvenanceSetId
        /// Actual loaded input name.
        InputName: string
        /// Actual loaded output name.
        OutputName: string
    }

/// ARCtrl-specific lookup data returned beside the source-agnostic core model.
/// It contains enough information for a later writeback adapter to find source tables and contexts again.
type ArcProvenanceIndex =
    {
        /// Location of the selected loaded table.
        LoadedTable: ArcTableLocation
        /// Locations of first-class loaded inputs and outputs.
        EndpointLocations: Map<ProvenanceSetId, ArcEndpointLocation>
        /// Locations of loaded and collapsed property value occurrences.
        PropertyValueLocations: Map<ProvenancePropertyValueId, ArcWritebackLocation>
        /// Locations of loaded-table input/output connections.
        ConnectionLocations: Map<ProvenanceConnectionId, ArcConnectionLocation>
    }

/// Successful conversion output.
type ArcProvenanceConversionResult =
    {
        /// Source-agnostic provenance model consumed by grouping, editing, and display modules.
        Model: ProvenanceModel
        /// ARCtrl-specific lookup data for later writeback.
        Index: ArcProvenanceIndex
        /// Non-fatal conversion notes, such as skipped empty endpoint cells.
        Warnings: string list
    }

[<RequireQualifiedAccess>]
type private LoadedRole =
    | Input
    | Output

[<RequireQualifiedAccess>]
type private PropertyTarget =
    | Inputs
    | Outputs
    | Both

type private TableRef =
    {
        Location: ArcTableLocation
        Table: ArcTable
    }

type private EndpointColumn =
    {
        Index: int
        Header: ProvenanceIOHeader
        Role: LoadedRole
    }

type private PropertyColumn =
    {
        Index: int
        Header: ProvenancePropertyHeader
        Target: PropertyTarget
    }

type private RowContext =
    {
        Key: string
        RowIndex: int
        Location: ArcTableLocation
        TableName: string
        ProcessName: string option
        InputNames: string list
        OutputNames: string list
        InputSetIds: ProvenanceSetId list
        OutputSetIds: ProvenanceSetId list
    }

type private ConvertState =
    {
        PropertyValues: Map<ProvenancePropertyValueId, ProvenancePropertyValue>
        InputSets: Map<ProvenanceSetId, ProvenanceSet>
        OutputSets: Map<ProvenanceSetId, ProvenanceSet>
        Connections: Map<ProvenanceConnectionId, ProvenanceConnection>
        EndpointLocations: Map<ProvenanceSetId, ArcEndpointLocation>
        PropertyValueLocations: Map<ProvenancePropertyValueId, ArcWritebackLocation>
        ConnectionLocations: Map<ProvenanceConnectionId, ArcConnectionLocation>
        PropertyCounters: Map<string, int>
        Warnings: string list
    }

let private emptyState =
    {
        PropertyValues = Map.empty
        InputSets = Map.empty
        OutputSets = Map.empty
        Connections = Map.empty
        EndpointLocations = Map.empty
        PropertyValueLocations = Map.empty
        ConnectionLocations = Map.empty
        PropertyCounters = Map.empty
        Warnings = []
    }

let private trimToOption (value: string) =
    if isNull value then
        None
    else
        let trimmed = value.Trim()
        if String.IsNullOrWhiteSpace trimmed then None else Some trimmed

let private optionText (value: string option) =
    value |> Option.bind trimToOption

let private slug (value: string) =
    value.ToLowerInvariant()
    |> Seq.map (fun ch -> if Char.IsLetterOrDigit ch then ch else '-')
    |> Seq.toArray
    |> String
    |> fun text -> text.Trim('-')

let private stableId parts =
    parts
    |> List.choose trimToOption
    |> List.map slug
    |> String.concat "--"

let private scopeText scope =
    match scope with
    | ArcTableScope.Study -> "study"
    | ArcTableScope.Assay -> "assay"
    | ArcTableScope.Run -> "run"

let private locationParts location =
    [
        scopeText location.Scope
        location.ParentIdentifier
        location.TableName
    ]

let private tableRefs (arc: ARC) =
    [
        for study in arc.Studies do
            for table in study.Tables do
                yield
                    {
                        Location =
                            {
                                Scope = ArcTableScope.Study
                                ParentIdentifier = study.Identifier
                                TableName = table.Name
                            }
                        Table = table
                    }

        for assay in arc.Assays do
            for table in assay.Tables do
                yield
                    {
                        Location =
                            {
                                Scope = ArcTableScope.Assay
                                ParentIdentifier = assay.Identifier
                                TableName = table.Name
                            }
                        Table = table
                    }

        for run in arc.Runs do
            for table in run.Tables do
                yield
                    {
                        Location =
                            {
                                Scope = ArcTableScope.Run
                                ParentIdentifier = run.Identifier
                                TableName = table.Name
                            }
                        Table = table
                    }
    ]

let private findTable (location: ArcTableLocation) (refs: TableRef list) : Result<TableRef, ArcProvenanceConversionError> =
    let matches =
        refs
        |> List.filter (fun tableRef -> tableRef.Location = location)

    match matches with
    | [] -> Error(ArcProvenanceConversionError.LoadedTableNotFound location)
    | [ tableRef ] -> Ok tableRef
    | many -> Error(ArcProvenanceConversionError.LoadedTableAmbiguous(location, many.Length))

let private processName (table: ArcTable) rowIndex =
    if table.RowCount = 1 then
        Some table.Name
    else
        Some $"{table.Name}_{rowIndex}"

let private termFromOntology (oa: OntologyAnnotation) : ProvenanceTerm =
    {
        Name = oa.NameText
        TermSource = optionText oa.TermSourceREF
        TermAccession = optionText oa.TermAccessionNumber
    }

let private termIsEmpty (term: ProvenanceTerm) =
    String.IsNullOrWhiteSpace term.Name
    && term.TermSource.IsNone
    && term.TermAccession.IsNone

let private ioKindFromARCtrl (ioType: IOType) =
    match ioType with
    | IOType.Source -> ProvenanceIOKind.Source
    | IOType.Sample -> ProvenanceIOKind.Sample
    | IOType.Data -> ProvenanceIOKind.Data
    | IOType.Material -> ProvenanceIOKind.Material
    | IOType.FreeText text -> ProvenanceIOKind.FreeText text
    | _ -> ProvenanceIOKind.Unknown

let private ioHeaderFromARCtrl role (ioType: IOType) : ProvenanceIOHeader =
    let text =
        match role with
        | LoadedRole.Input -> ioType.asInput
        | LoadedRole.Output -> ioType.asOutput

    {
        Kind = ioKindFromARCtrl ioType
        Text = text
    }

let private propertyHeaderFromARCtrl (header: CompositeHeader) : ProvenancePropertyHeader option =
    match header with
    | CompositeHeader.Characteristic category ->
        Some
            {
                Kind = ProvenancePropertyKind.Characteristic
                Category = termFromOntology category
            }
    | CompositeHeader.Factor category ->
        Some
            {
                Kind = ProvenancePropertyKind.Factor
                Category = termFromOntology category
            }
    | CompositeHeader.Parameter category ->
        Some
            {
                Kind = ProvenancePropertyKind.Parameter
                Category = termFromOntology category
            }
    | CompositeHeader.Component category ->
        Some
            {
                Kind = ProvenancePropertyKind.Component
                Category = termFromOntology category
            }
    | _ ->
        None

let private cellText (cell: CompositeCell) =
    match cell with
    | CompositeCell.FreeText value -> trimToOption value
    | CompositeCell.Term term -> trimToOption term.NameText
    | CompositeCell.Unitized(value, _) -> trimToOption value
    | CompositeCell.Data data -> trimToOption data.NameText

let private propertyValueFromCell (cell: CompositeCell) : (ProvenanceValue * ProvenanceTerm option) option =
    match cell with
    | CompositeCell.FreeText value ->
        trimToOption value
        |> Option.map (fun value -> ProvenanceValue.Text value, None)
    | CompositeCell.Term term ->
        let converted = termFromOntology term
        if termIsEmpty converted then
            None
        else
            Some(ProvenanceValue.Term converted, None)
    | CompositeCell.Unitized(value, unit) ->
        trimToOption value
        |> Option.map (fun value ->
            let unitTerm = termFromOntology unit
            let normalizedUnit = if termIsEmpty unitTerm then None else Some unitTerm
            ProvenanceValue.Text value, normalizedUnit
        )
    | CompositeCell.Data data ->
        trimToOption data.NameText
        |> Option.map (fun value -> ProvenanceValue.Text value, None)

let private nearestEndpointRoleBefore (headers: ResizeArray<CompositeHeader>) columnIndex =
    headers
    |> Seq.indexed
    |> Seq.takeWhile (fun (index, _) -> index < columnIndex)
    |> Seq.choose (fun (_, header) ->
        match header with
        | CompositeHeader.Input _ -> Some LoadedRole.Input
        | CompositeHeader.Output _ -> Some LoadedRole.Output
        | _ -> None
    )
    |> Seq.tryLast

let private propertyTarget (headers: ResizeArray<CompositeHeader>) columnIndex (propertyHeader: ProvenancePropertyHeader) =
    match propertyHeader.Kind with
    | ProvenancePropertyKind.Characteristic ->
        match nearestEndpointRoleBefore headers columnIndex with
        | Some LoadedRole.Input -> PropertyTarget.Inputs
        | Some LoadedRole.Output -> PropertyTarget.Outputs
        | None -> PropertyTarget.Both
    | ProvenancePropertyKind.Factor ->
        PropertyTarget.Outputs
    | ProvenancePropertyKind.Parameter
    | ProvenancePropertyKind.Component ->
        PropertyTarget.Both

let private endpointColumns (table: ArcTable) : EndpointColumn list =
    table.Headers
    |> Seq.indexed
    |> Seq.choose (fun (index, header) ->
        match header with
        | CompositeHeader.Input ioType ->
            Some
                {
                    Index = index
                    Header = ioHeaderFromARCtrl LoadedRole.Input ioType
                    Role = LoadedRole.Input
                }
        | CompositeHeader.Output ioType ->
            Some
                {
                    Index = index
                    Header = ioHeaderFromARCtrl LoadedRole.Output ioType
                    Role = LoadedRole.Output
                }
        | _ ->
            None
    )
    |> Seq.toList

let private propertyColumns (table: ArcTable) : PropertyColumn list =
    table.Headers
    |> Seq.indexed
    |> Seq.choose (fun (index, header) ->
        propertyHeaderFromARCtrl header
        |> Option.map (fun propertyHeader ->
            {
                Index = index
                Header = propertyHeader
                Target = propertyTarget table.Headers index propertyHeader
            }
        )
    )
    |> Seq.toList

let private rowEndpointNames (table: ArcTable) role rowIndex (endpointColumns: EndpointColumn list) =
    endpointColumns
    |> List.choose (fun column ->
        if column.Role = role then
            table.GetCellAt(column.Index, rowIndex)
            |> cellText
            |> Option.map (fun name -> column.Header, name)
        else
            None
    )

let private addEndpoint (location: ArcTableLocation) role (header: ProvenanceIOHeader) name (state: ConvertState) =
    let roleText =
        match role with
        | LoadedRole.Input -> "input"
        | LoadedRole.Output -> "output"

    let id =
        stableId (
            [
                "set"
                roleText
            ]
            @ locationParts location
            @ [
                header.Text
                name
            ]
        )

    let set : ProvenanceSet =
        {
            Id = id
            TableName = location.TableName
            Header = header
            Name = name
            PropertyValueIds = []
        }

    let endpointLocation : ArcEndpointLocation =
        {
            Table = location
            Header = header
            Name = name
        }

    match role with
    | LoadedRole.Input ->
        id,
        {
            state with
                InputSets = state.InputSets |> Map.add id (state.InputSets |> Map.tryFind id |> Option.defaultValue set)
                EndpointLocations = state.EndpointLocations |> Map.add id endpointLocation
        }
    | LoadedRole.Output ->
        id,
        {
            state with
                OutputSets = state.OutputSets |> Map.add id (state.OutputSets |> Map.tryFind id |> Option.defaultValue set)
                EndpointLocations = state.EndpointLocations |> Map.add id endpointLocation
        }

let private addConnection (location: ArcTableLocation) (row: RowContext) inputSetId inputName outputSetId outputName (state: ConvertState) =
    let id =
        stableId (
            [
                "connection"
            ]
            @ locationParts location
            @ [
                row.ProcessName |> Option.defaultValue ""
                inputSetId
                outputSetId
            ]
        )

    let connection : ProvenanceConnection =
        {
            Id = id
            TableName = location.TableName
            ProcessName = row.ProcessName
            InputSetId = inputSetId
            OutputSetId = outputSetId
        }

    let connectionLocation : ArcConnectionLocation =
        {
            Table = location
            ProcessName = row.ProcessName
            InputSetId = inputSetId
            OutputSetId = outputSetId
            InputName = inputName
            OutputName = outputName
        }

    {
        state with
            Connections = state.Connections |> Map.add id connection
            ConnectionLocations = state.ConnectionLocations |> Map.add id connectionLocation
    }

let private attachProperty setId propertyValueId (state: ConvertState) =
    match state.InputSets |> Map.tryFind setId with
    | Some set ->
        let nextSet =
            { set with PropertyValueIds = set.PropertyValueIds @ [ propertyValueId ] }

        { state with InputSets = state.InputSets |> Map.add setId nextSet }
    | None ->
        match state.OutputSets |> Map.tryFind setId with
        | Some set ->
            let nextSet =
                { set with PropertyValueIds = set.PropertyValueIds @ [ propertyValueId ] }

            { state with OutputSets = state.OutputSets |> Map.add setId nextSet }
        | None ->
            { state with Warnings = $"Skipped property attachment for missing set '{setId}'." :: state.Warnings }

let private nextPropertyId (location: ArcTableLocation) (row: RowContext) (header: ProvenancePropertyHeader) value (state: ConvertState) =
    let valueText =
        match value with
        | ProvenanceValue.Text text -> text
        | ProvenanceValue.Integer number -> string number
        | ProvenanceValue.Float number -> string number
        | ProvenanceValue.Term term -> term.Name

    let key =
        stableId (
            [
                "property"
            ]
            @ locationParts location
            @ [
                row.ProcessName |> Option.defaultValue ""
                header.Kind.ToString()
                header.Category.Name
                String.concat "|" row.InputNames
                String.concat "|" row.OutputNames
                valueText
            ]
        )

    let index =
        state.PropertyCounters
        |> Map.tryFind key
        |> Option.defaultValue 0

    let id = stableId [ key; string index ]

    id,
    {
        state with
            PropertyCounters = state.PropertyCounters |> Map.add key (index + 1)
    }

let private addProperty (location: ArcTableLocation) (row: RowContext) (header: ProvenancePropertyHeader) value unit (targetSetIds: ProvenanceSetId list) (state: ConvertState) =
    match targetSetIds with
    | [] ->
        { state with Warnings = $"Skipped property '{header.Category.Name}' because no loaded endpoint target was available." :: state.Warnings }
    | _ ->
        let id, state = nextPropertyId location row header value state

        let anchor : ProvenanceWritebackAnchor =
            {
                TableName = location.TableName
                ProcessName = row.ProcessName
                Header = header
                InputNames = row.InputNames
                OutputNames = row.OutputNames
            }

        let propertyValue : ProvenancePropertyValue =
            {
                Id = id
                Header = header
                Value = value
                Unit = unit
                Source = Some anchor
            }

        let writebackLocation : ArcWritebackLocation =
            {
                Table = location
                ProcessName = row.ProcessName
                Header = header
                InputNames = row.InputNames
                OutputNames = row.OutputNames
            }

        let state =
            {
                state with
                    PropertyValues = state.PropertyValues |> Map.add id propertyValue
                    PropertyValueLocations = state.PropertyValueLocations |> Map.add id writebackLocation
            }

        targetSetIds
        |> List.distinct
        |> List.fold (fun nextState setId -> attachProperty setId id nextState) state

let private targetSetIds (row: RowContext) target =
    match target with
    | PropertyTarget.Inputs -> row.InputSetIds
    | PropertyTarget.Outputs -> row.OutputSetIds
    | PropertyTarget.Both -> row.InputSetIds @ row.OutputSetIds

let private loadedRows (location: ArcTableLocation) (table: ArcTable) (endpointColumns: EndpointColumn list) (initialState: ConvertState) =
    [ 0 .. table.RowCount - 1 ]
    |> List.mapFold (fun state rowIndex ->
        let inputNames = rowEndpointNames table LoadedRole.Input rowIndex endpointColumns
        let outputNames = rowEndpointNames table LoadedRole.Output rowIndex endpointColumns

        let inputIds, state =
            inputNames
            |> List.mapFold (fun currentState (header, name) -> addEndpoint location LoadedRole.Input header name currentState) state

        let outputIds, state =
            outputNames
            |> List.mapFold (fun currentState (header, name) -> addEndpoint location LoadedRole.Output header name currentState) state

        let row : RowContext =
            {
                Key = stableId (locationParts location @ [ string rowIndex ])
                RowIndex = rowIndex
                Location = location
                TableName = location.TableName
                ProcessName = processName table rowIndex
                InputNames = inputNames |> List.map snd
                OutputNames = outputNames |> List.map snd
                InputSetIds = inputIds
                OutputSetIds = outputIds
            }

        row, state
    ) initialState

let private addLoadedConnections (location: ArcTableLocation) (rows: RowContext list) (state: ConvertState) =
    rows
    |> List.fold (fun currentState row ->
        let inputPairs = List.zip row.InputSetIds row.InputNames
        let outputPairs = List.zip row.OutputSetIds row.OutputNames

        [ for inputSetId, inputName in inputPairs do
            for outputSetId, outputName in outputPairs do
                inputSetId, inputName, outputSetId, outputName ]
        |> List.fold (fun nestedState (inputSetId, inputName, outputSetId, outputName) ->
            addConnection location row inputSetId inputName outputSetId outputName nestedState
        ) currentState
    ) state

let private addPropertiesFromTable (location: ArcTableLocation) (table: ArcTable) (rows: RowContext list) (propertyColumns: PropertyColumn list) (state: ConvertState) =
    rows
    |> List.fold (fun currentState row ->
        propertyColumns
        |> List.fold (fun nestedState column ->
            table.GetCellAt(column.Index, row.RowIndex)
            |> propertyValueFromCell
            |> Option.map (fun (value, unitTerm) ->
                let applicableSetIds = targetSetIds row column.Target
                addProperty location row column.Header value unitTerm applicableSetIds nestedState
            )
            |> Option.defaultValue nestedState
        ) currentState
    ) state

type private PreviousRow =
    {
        Key: string
        Location: ArcTableLocation
        ProcessName: string option
        InputNames: string list
        OutputNames: string list
        Values: (ProvenancePropertyHeader * ProvenanceValue * ProvenanceTerm option) list
    }

let private previousRows (loadedLocation: ArcTableLocation) (tableRefs: TableRef list) : PreviousRow list =
    tableRefs
    |> List.filter (fun tableRef -> tableRef.Location <> loadedLocation)
    |> List.collect (fun (tableRef: TableRef) ->
        let table = tableRef.Table
        let endpoints = endpointColumns table
        let properties = propertyColumns table

        [ for rowIndex in 0 .. table.RowCount - 1 do
            let inputNames =
                rowEndpointNames table LoadedRole.Input rowIndex endpoints
                |> List.map snd

            let outputNames =
                rowEndpointNames table LoadedRole.Output rowIndex endpoints
                |> List.map snd

            let values =
                properties
                |> List.choose (fun column ->
                    table.GetCellAt(column.Index, rowIndex)
                    |> propertyValueFromCell
                    |> Option.map (fun (value, unitTerm) -> column.Header, value, unitTerm)
                )

            if values.Length > 0 && outputNames.Length > 0 then
                yield
                    {
                        Key = stableId (locationParts tableRef.Location @ [ string rowIndex ])
                        Location = tableRef.Location
                        ProcessName = processName table rowIndex
                        InputNames = inputNames
                        OutputNames = outputNames
                        Values = values
                    }
        ]
    )

let private attachPreviousContext (loadedLocation: ArcTableLocation) (tableRefs: TableRef list) (state: ConvertState) =
    let loadedInputFrontier =
        state.InputSets
        |> Map.toList
        |> List.groupBy (fun (_, set) -> set.Name)
        |> List.map (fun (name, sets) -> name, sets |> List.map fst |> Set.ofList)
        |> Map.ofList

    let rows = previousRows loadedLocation tableRefs

    let rec walk frontier visited currentState =
        let matches =
            rows
            |> List.choose (fun row ->
                if Set.contains row.Key visited then
                    None
                else
                    let matchingTargets =
                        row.OutputNames
                        |> List.choose (fun outputName -> frontier |> Map.tryFind outputName)

                    let targetIds =
                        match matchingTargets with
                        | [] -> Set.empty
                        | targets -> Set.unionMany targets

                    if Set.isEmpty targetIds then
                        None
                    else
                        Some(row, targetIds)
            )

        match matches with
        | [] ->
            currentState
        | _ ->
            let nextState, nextFrontier, nextVisited =
                matches
                |> List.fold (fun (foldState, foldFrontier, foldVisited) (row, targetIds) ->
                    let rowContext : RowContext =
                        {
                            Key = row.Key
                            RowIndex = -1
                            Location = row.Location
                            TableName = row.Location.TableName
                            ProcessName = row.ProcessName
                            InputNames = row.InputNames
                            OutputNames = row.OutputNames
                            InputSetIds = Set.toList targetIds
                            OutputSetIds = []
                        }

                    let updatedState =
                        row.Values
                        |> List.fold (fun rowState (header, value, unitTerm) ->
                            addProperty row.Location rowContext header value unitTerm (Set.toList targetIds) rowState
                        ) foldState

                    let updatedFrontier =
                        row.InputNames
                        |> List.fold (fun frontierState inputName ->
                            let existing =
                                frontierState
                                |> Map.tryFind inputName
                                |> Option.defaultValue Set.empty

                            frontierState |> Map.add inputName (Set.union existing targetIds)
                        ) foldFrontier

                    updatedState, updatedFrontier, Set.add row.Key foldVisited
                ) (currentState, frontier, visited)

            walk nextFrontier nextVisited nextState

    walk loadedInputFrontier Set.empty state

let fromLoadedArc (options: ArcProvenanceConverterOptions) (arc: ARC) : Result<ArcProvenanceConversionResult, ArcProvenanceConversionError> =
    let refs = tableRefs arc

    match findTable options.LoadedTable refs with
    | Error error ->
        Error error
    | Ok loaded ->
        let loadedEndpointColumns = endpointColumns loaded.Table
        let loadedPropertyColumns = propertyColumns loaded.Table

        let rows, state = loadedRows loaded.Location loaded.Table loadedEndpointColumns emptyState
        let state = addLoadedConnections loaded.Location rows state
        let state = addPropertiesFromTable loaded.Location loaded.Table rows loadedPropertyColumns state

        if state.InputSets.IsEmpty then
            Error(ArcProvenanceConversionError.LoadedTableHasNoInputs loaded.Location)
        elif state.OutputSets.IsEmpty then
            Error(ArcProvenanceConversionError.LoadedTableHasNoOutputs loaded.Location)
        else
            let state =
                if options.IncludePreviousContext then
                    attachPreviousContext loaded.Location refs state
                else
                    state

            let model : ProvenanceModel =
                {
                    LoadedTableName = loaded.Location.TableName
                    PropertyValues = state.PropertyValues
                    InputSets = state.InputSets
                    OutputSets = state.OutputSets
                    Connections = state.Connections
                }

            let index : ArcProvenanceIndex =
                {
                    LoadedTable = loaded.Location
                    EndpointLocations = state.EndpointLocations
                    PropertyValueLocations = state.PropertyValueLocations
                    ConnectionLocations = state.ConnectionLocations
                }

            Ok
                {
                    Model = model
                    Index = index
                    Warnings = List.rev state.Warnings
                }
