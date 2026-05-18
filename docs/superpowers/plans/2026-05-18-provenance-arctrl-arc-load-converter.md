# Provenance ARCtrl ARC Load Converter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an ARCtrl adapter that converts an already loaded `ARCtrl.ARC` from `ARC.load` into the Swate provenance core model plus ARCtrl-specific writeback lookup metadata.

**Architecture:** Keep `Swate.Components.ProvenanceGrouping.Types` source-agnostic. Add one ARCtrl-specific converter module that accepts `ARCtrl.ARC` and a selected study/assay/run table location, builds first-class loaded input/output sets, loaded input-to-output connections, loaded property values, and collapsed previous-context property values. Return a sidecar index containing only stable ARCtrl table locations and source names needed for later writeback; do not store ARCtrl object references, row indices, or column indices in the core model or sidecar API.

**Tech Stack:** F# / Fable-compatible Components project, ARCtrl `ARC`, `ArcStudy`, `ArcAssay`, `ArcRun`, `ArcTable`, `CompositeHeader`, `CompositeCell`, `Map`, `Result`, Expecto shared tests.

**Depends On:** `docs/superpowers/plans/2026-05-18-provenance-edit-model.md` must be implemented first because this plan consumes `Swate.Components.ProvenanceGrouping.Types.ProvenanceModel`.

**Reference Read:** Local ARCtrl reference at `C:\Users\jonat\source\repos\ARCtrl`.

---

## File Structure

| File | Responsibility | Action |
|---|---|---|
| `src/Components/src/ProvenanceGrouping/ARCtrlConverter.fs` | Convert a loaded `ARCtrl.ARC` and selected table into `ProvenanceModel` plus ARCtrl writeback index | Create |
| `src/Components/src/Swate.Components.fsproj` | Compile converter after provenance core model types | Modify |
| `tests/Shared/ProvenanceGrouping.ARCtrlConverter.Tests.fs` | Shared tests for loaded endpoints, loaded connections, property values, collapsed previous context, and writeback locations | Create |
| `tests/Shared/Shared.Tests.fsproj` | Compile the ARCtrl converter test file | Modify |
| `tests/Shared/Shared.Tests.fs` | Add the ARCtrl converter tests to the shared suite | Modify |

## ARCtrl Facts This Plan Uses

- `ARC.load` returns an `ARCtrl.ARC` from `src\ARCtrl\ARC.fs`.
- `ARC` inherits `ArcInvestigation`, so loaded data is available through `arc.Studies`, `arc.Assays`, and `arc.Runs`.
- `ArcStudy`, `ArcAssay`, and `ArcRun` inherit `ArcTables`, so each exposes `Tables`.
- `ArcTable` exposes `Name`, `Headers`, `RowCount`, `GetCellAt(column, row)`, `TryGetInputColumn()`, and `TryGetOutputColumn()`.
- `CompositeHeader.Input` and `CompositeHeader.Output` identify endpoint columns.
- `CompositeHeader.Characteristic`, `CompositeHeader.Factor`, `CompositeHeader.Parameter`, and `CompositeHeader.Component` identify property value columns.
- `CompositeCell.FreeText`, `Term`, `Unitized`, and `Data` contain the actual table cell values.
- ARCtrl process naming composes multi-row table process names as `"{table.Name}_{rowIndex}"` with zero-based `rowIndex`; a single-row table uses `table.Name`.

## Model Boundary

- The public core model remains the reduced provenance model from `Types.fs`.
- Loaded input and output names come from loaded table input/output cells and are stored as `ProvenanceSet.Name`.
- Property values never provide loaded input/output names.
- Loaded endpoint role is still implied by membership in `InputSets` or `OutputSets`; no `Side` field is added.
- The converter sidecar is ARCtrl-specific and may mention ARCtrl selection concepts, but it must not expose ARCtrl object references.
- The sidecar stores table scope, parent identifier, table name, process name, property header, input names, and output names.
- The sidecar does not store row indices or column indices.
- Collapsed previous context produces property values attached to loaded input sets only.
- Collapsed previous context does not create previous-table `ProvenanceSet` records or previous-table `ProvenanceConnection` records.

---

## Task 1: Add ARCtrl Converter Tests

**Files:**
- Create: `tests/Shared/ProvenanceGrouping.ARCtrlConverter.Tests.fs`
- Modify: `tests/Shared/Shared.Tests.fsproj`
- Modify: `tests/Shared/Shared.Tests.fs`

- [ ] **Step 1: Create the failing ARCtrl converter test file**

Create `tests/Shared/ProvenanceGrouping.ARCtrlConverter.Tests.fs` with this content:

```fsharp
module ProvenanceGroupingARCtrlConverterTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open ARCtrl
open Swate.Components.ProvenanceGrouping.Types
open Swate.Components.ProvenanceGrouping.ARCtrlConverter

let private oa name =
    OntologyAnnotation.create(name = name)

let private text value =
    CompositeCell.createFreeText value

let private term value =
    CompositeCell.createTermFromString(name = value)

let private table name headers rows =
    let rows =
        rows
        |> List.map ResizeArray
        |> ResizeArray

    ArcTable.createFromRows(name, ResizeArray headers, rows)

let private loadedAssayTable () =
    table
        "assay-table"
        [
            CompositeHeader.Input IOType.Sample
            CompositeHeader.Characteristic(oa "Species")
            CompositeHeader.Parameter(oa "Temperature")
            CompositeHeader.Output IOType.Sample
            CompositeHeader.Factor(oa "Replicate")
        ]
        [
            [ text "sample-a"; term "Arabidopsis"; text "22"; text "extract-a"; text "R1" ]
            [ text "sample-b"; term "Arabidopsis"; text "23"; text "extract-b"; text "R2" ]
        ]

let private previousStudyTable () =
    table
        "study-table"
        [
            CompositeHeader.Input IOType.Source
            CompositeHeader.Characteristic(oa "Organism")
            CompositeHeader.Output IOType.Sample
        ]
        [
            [ text "source-a"; term "Plant"; text "sample-a" ]
            [ text "source-b"; term "Plant"; text "sample-b" ]
        ]

let private arcFixture () =
    let study =
        ArcStudy.create(
            identifier = "study-1",
            tables = ResizeArray [ previousStudyTable () ],
            registeredAssayIdentifiers = ResizeArray [ "assay-1" ]
        )

    let assay =
        ArcAssay.create(
            identifier = "assay-1",
            tables = ResizeArray [ loadedAssayTable () ]
        )

    ARC(
        identifier = "arc-1",
        studies = ResizeArray [ study ],
        assays = ResizeArray [ assay ]
    )

let private loadedTable =
    {
        Scope = ArcTableScope.Assay
        ParentIdentifier = "assay-1"
        TableName = "assay-table"
    }

let private convertWithPreviousContext () =
    fromLoadedArc
        {
            LoadedTable = loadedTable
            IncludePreviousContext = true
        }
        (arcFixture ())

let private expectOk result =
    match result with
    | Ok value -> value
    | Error error -> failwithf "Expected Ok, got %A" error

let private expectText expected value =
    match value with
    | ProvenanceValue.Text actual -> Expect.equal actual expected "Expected text value."
    | ProvenanceValue.Term term -> Expect.equal term.Name expected "Expected term value."
    | ProvenanceValue.Integer actual -> Expect.equal (string actual) expected "Expected integer value."
    | ProvenanceValue.Float actual -> Expect.equal (string actual) expected "Expected float value."

let tests =
    testList "ProvenanceGrouping ARCtrl converter" [
        testCase "converts loaded input and output names into first-class sets" <| fun _ ->
            let result = convertWithPreviousContext () |> expectOk

            let inputNames =
                result.Model.InputSets
                |> Map.toList
                |> List.map (fun (_, set) -> set.Name)
                |> List.sort

            let outputNames =
                result.Model.OutputSets
                |> Map.toList
                |> List.map (fun (_, set) -> set.Name)
                |> List.sort

            Expect.equal inputNames [ "sample-a"; "sample-b" ] "Loaded inputs should come from loaded input cells."
            Expect.equal outputNames [ "extract-a"; "extract-b" ] "Loaded outputs should come from loaded output cells."

        testCase "converts loaded row input-to-output connections" <| fun _ ->
            let result = convertWithPreviousContext () |> expectOk

            let connectionPairs =
                result.Model.Connections
                |> Map.toList
                |> List.map (fun (_, connection) ->
                    result.Model.InputSets.[connection.InputSetId].Name,
                    result.Model.OutputSets.[connection.OutputSetId].Name,
                    connection.ProcessName
                )
                |> List.sort

            Expect.equal
                connectionPairs
                [
                    ("sample-a", "extract-a", Some "assay-table_0")
                    ("sample-b", "extract-b", Some "assay-table_1")
                ]
                "Each loaded row should produce a loaded input/output connection."

        testCase "attaches loaded property values to loaded sets" <| fun _ ->
            let result = convertWithPreviousContext () |> expectOk

            let sampleA =
                result.Model.InputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "sample-a")

            let extractA =
                result.Model.OutputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "extract-a")

            let species =
                sampleA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Species")

            let temperature =
                sampleA.PropertyValueIds
                |> List.append extractA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Temperature")

            let replicate =
                extractA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Replicate")

            expectText "Arabidopsis" species.Value
            expectText "22" temperature.Value
            expectText "R1" replicate.Value

        testCase "collapses previous table context into property values on loaded inputs" <| fun _ ->
            let result = convertWithPreviousContext () |> expectOk

            let sampleA =
                result.Model.InputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "sample-a")

            let organism =
                sampleA.PropertyValueIds
                |> List.map (fun id -> result.Model.PropertyValues.[id])
                |> List.find (fun value -> value.Header.Category.Name = "Organism")

            let location = result.Index.PropertyValueLocations.[organism.Id]

            expectText "Plant" organism.Value
            Expect.equal location.Table.Scope ArcTableScope.Study "Collapsed previous value should remember study scope."
            Expect.equal location.Table.ParentIdentifier "study-1" "Collapsed previous value should remember parent identifier."
            Expect.equal location.Table.TableName "study-table" "Collapsed previous value should remember source table."
            Expect.equal location.OutputNames [ "sample-a" ] "Collapsed previous value should remember where it pointed to."

        testCase "keeps ARCtrl table locations in the sidecar index" <| fun _ ->
            let result = convertWithPreviousContext () |> expectOk

            let sampleA =
                result.Model.InputSets
                |> Map.toSeq
                |> Seq.map snd
                |> Seq.find (fun set -> set.Name = "sample-a")

            let endpointLocation = result.Index.EndpointLocations.[sampleA.Id]

            Expect.equal endpointLocation.Table.Scope ArcTableScope.Assay "Loaded endpoint should remember assay scope."
            Expect.equal endpointLocation.Table.ParentIdentifier "assay-1" "Loaded endpoint should remember assay identifier."
            Expect.equal endpointLocation.Table.TableName "assay-table" "Loaded endpoint should remember table name."
            Expect.equal endpointLocation.Name "sample-a" "Endpoint location should keep the actual loaded input name."

        testCase "returns a conversion error when the selected table is missing" <| fun _ ->
            let missing =
                {
                    Scope = ArcTableScope.Assay
                    ParentIdentifier = "assay-1"
                    TableName = "missing-table"
                }

            let result =
                fromLoadedArc
                    {
                        LoadedTable = missing
                        IncludePreviousContext = true
                    }
                    (arcFixture ())

            match result with
            | Error(ArcProvenanceConversionError.LoadedTableNotFound location) ->
                Expect.equal location missing "Missing table error should echo the requested location."
            | other ->
                failwithf "Expected LoadedTableNotFound, got %A" other
    ]
```

- [ ] **Step 2: Register the test file in the shared test project**

In `tests/Shared/Shared.Tests.fsproj`, add the converter test file after `ProvenanceGrouping.Tests.fs` and before `Shared.Tests.fs`.

The compile item group should be:

```xml
<ItemGroup>
  <Compile Include="Landing.Tests.fs"/>
  <Compile Include="PathHelpers.Tests.fs"/>
  <Compile Include="ProvenanceGrouping.Tests.fs"/>
  <Compile Include="ProvenanceGrouping.ARCtrlConverter.Tests.fs"/>
  <Compile Include="Shared.Tests.fs"/>
</ItemGroup>
```

- [ ] **Step 3: Register the test list in the shared suite**

In `tests/Shared/Shared.Tests.fs`, add `ProvenanceGroupingARCtrlConverterTests.tests` to the `shared` test list:

```fsharp
let shared = testList "Shared" [
    example_tests
    LandingTests.tests
    PathHelpersTests.tests
    ProvenanceGroupingTests.tests
    ProvenanceGroupingARCtrlConverterTests.tests
]
```

- [ ] **Step 4: Run the failing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected result: build fails with `FS0039` because `Swate.Components.ProvenanceGrouping.ARCtrlConverter` does not exist yet.

---

## Task 2: Add the ARCtrl Converter Module

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/ARCtrlConverter.fs`
- Modify: `src/Components/src/Swate.Components.fsproj`

- [ ] **Step 1: Add the converter compile item**

In `src/Components/src/Swate.Components.fsproj`, add the converter after the provenance core model files from the previous plan:

```xml
<Compile Include="ProvenanceGrouping\Types.fs" />
<Compile Include="ProvenanceGrouping\Import.fs" />
<Compile Include="ProvenanceGrouping\Grouping.fs" />
<Compile Include="ProvenanceGrouping\Edit.fs" />
<Compile Include="ProvenanceGrouping\ARCtrlConverter.fs" />
<Compile Include="ProvenanceGrouping\Fixtures.fs" />
```

- [ ] **Step 2: Create the complete converter module**

Create `src/Components/src/ProvenanceGrouping/ARCtrlConverter.fs` with this content:

```fsharp
module Swate.Components.ProvenanceGrouping.ARCtrlConverter

open System
open ARCtrl
open Swate.Components.ProvenanceGrouping.Types

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
                yield {
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
                yield {
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
                yield {
                    Location =
                        {
                            Scope = ArcTableScope.Run
                            ParentIdentifier = run.Identifier
                            TableName = table.Name
                        }
                    Table = table
                }
    ]

let private findTable location refs =
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

let private termFromOntology (oa: OntologyAnnotation) =
    {
        Name = oa.NameText
        TermSource = optionText oa.TermSourceREF
        TermAccession = optionText oa.TermAccessionNumber
    }

let private termIsEmpty term =
    String.IsNullOrWhiteSpace term.Name
    && term.TermSource.IsNone
    && term.TermAccession.IsNone

let private ioKindFromARCtrl ioType =
    match ioType with
    | IOType.Source -> ProvenanceIOKind.Source
    | IOType.Sample -> ProvenanceIOKind.Sample
    | IOType.Data -> ProvenanceIOKind.Data
    | IOType.Material -> ProvenanceIOKind.Material
    | IOType.FreeText text -> ProvenanceIOKind.FreeText text

let private ioHeaderFromARCtrl role ioType =
    let text =
        match role with
        | LoadedRole.Input -> ioType.asInput
        | LoadedRole.Output -> ioType.asOutput

    {
        Kind = ioKindFromARCtrl ioType
        Text = text
    }

let private propertyHeaderFromARCtrl header =
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

let private propertyValueFromCell (cell: CompositeCell) =
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
            let unit = termFromOntology unit
            let unit = if termIsEmpty unit then None else Some unit
            ProvenanceValue.Text value, unit
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

let private propertyTarget headers columnIndex propertyHeader =
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

let private endpointColumns (table: ArcTable) =
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

let private propertyColumns (table: ArcTable) =
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

let private rowEndpointNames (table: ArcTable) role rowIndex endpointColumns =
    endpointColumns
    |> List.choose (fun column ->
        if column.Role = role then
            table.GetCellAt(column.Index, rowIndex)
            |> cellText
            |> Option.map (fun name -> column.Header, name)
        else
            None
    )

let private addEndpoint location role header name state =
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

    let set =
        {
            Id = id
            TableName = location.TableName
            Header = header
            Name = name
            PropertyValueIds = []
        }

    let endpointLocation =
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

let private addConnection location row inputSetId inputName outputSetId outputName state =
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

    let connection =
        {
            Id = id
            TableName = location.TableName
            ProcessName = row.ProcessName
            InputSetId = inputSetId
            OutputSetId = outputSetId
        }

    let connectionLocation =
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

let private attachProperty setId propertyValueId state =
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

let private nextPropertyId location row header value state =
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

let private addProperty location row header value unit targetSetIds state =
    match targetSetIds with
    | [] ->
        { state with Warnings = $"Skipped property '{header.Category.Name}' because no loaded endpoint target was available." :: state.Warnings }
    | _ ->
        let id, state = nextPropertyId location row header value state

        let anchor =
            {
                TableName = location.TableName
                ProcessName = row.ProcessName
                Header = header
                InputNames = row.InputNames
                OutputNames = row.OutputNames
            }

        let propertyValue =
            {
                Id = id
                Header = header
                Value = value
                Unit = unit
                Source = Some anchor
            }

        let writebackLocation =
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
        |> List.fold (fun state setId -> attachProperty setId id state) state

let private targetSetIds row target =
    match target with
    | PropertyTarget.Inputs -> row.InputSetIds
    | PropertyTarget.Outputs -> row.OutputSetIds
    | PropertyTarget.Both -> row.InputSetIds @ row.OutputSetIds

let private loadedRows location table endpointColumns initialState =
    [ 0 .. table.RowCount - 1 ]
    |> List.mapFold (fun state rowIndex ->
        let inputNames = rowEndpointNames table LoadedRole.Input rowIndex endpointColumns
        let outputNames = rowEndpointNames table LoadedRole.Output rowIndex endpointColumns

        let inputIds, state =
            inputNames
            |> List.mapFold (fun state (header, name) -> addEndpoint location LoadedRole.Input header name state) state

        let outputIds, state =
            outputNames
            |> List.mapFold (fun state (header, name) -> addEndpoint location LoadedRole.Output header name state) state

        let row =
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

let private addLoadedConnections location rows state =
    rows
    |> List.fold (fun state row ->
        let inputPairs = List.zip row.InputSetIds row.InputNames
        let outputPairs = List.zip row.OutputSetIds row.OutputNames

        [ for inputSetId, inputName in inputPairs do
            for outputSetId, outputName in outputPairs do
                inputSetId, inputName, outputSetId, outputName ]
        |> List.fold (fun state (inputSetId, inputName, outputSetId, outputName) ->
            addConnection location row inputSetId inputName outputSetId outputName state
        ) state
    ) state

let private addPropertiesFromTable location table rows propertyColumns state =
    rows
    |> List.fold (fun state row ->
        propertyColumns
        |> List.fold (fun state column ->
            table.GetCellAt(column.Index, row.RowIndex)
            |> propertyValueFromCell
            |> Option.map (fun (value, unit) ->
                let targetSetIds = targetSetIds row column.Target
                addProperty location row column.Header value unit targetSetIds state
            )
            |> Option.defaultValue state
        ) state
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

let private previousRows loadedLocation tableRefs =
    tableRefs
    |> List.filter (fun tableRef -> tableRef.Location <> loadedLocation)
    |> List.collect (fun tableRef ->
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
                    |> Option.map (fun (value, unit) -> column.Header, value, unit)
                )

            if values.Length > 0 && outputNames.Length > 0 then
                yield {
                    Key = stableId (locationParts tableRef.Location @ [ string rowIndex ])
                    Location = tableRef.Location
                    ProcessName = processName table rowIndex
                    InputNames = inputNames
                    OutputNames = outputNames
                    Values = values
                }
        ]
    )

let private attachPreviousContext loadedLocation tableRefs state =
    let loadedInputFrontier =
        state.InputSets
        |> Map.toList
        |> List.groupBy (fun (_, set) -> set.Name)
        |> List.map (fun (name, sets) -> name, sets |> List.map fst |> Set.ofList)
        |> Map.ofList

    let rows = previousRows loadedLocation tableRefs

    let rec walk frontier visited state =
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
            state
        | _ ->
            let state, nextFrontier, visited =
                matches
                |> List.fold (fun (state, frontier, visited) (row, targetIds) ->
                    let rowContext =
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

                    let state =
                        row.Values
                        |> List.fold (fun state (header, value, unit) ->
                            addProperty row.Location rowContext header value unit (Set.toList targetIds) state
                        ) state

                    let frontier =
                        row.InputNames
                        |> List.fold (fun frontier inputName ->
                            let existing =
                                frontier
                                |> Map.tryFind inputName
                                |> Option.defaultValue Set.empty

                            frontier |> Map.add inputName (Set.union existing targetIds)
                        ) frontier

                    state, frontier, Set.add row.Key visited
                ) (state, frontier, visited)

            walk nextFrontier visited state

    walk loadedInputFrontier Set.empty state

let fromLoadedArc options (arc: ARC) : Result<ArcProvenanceConversionResult, ArcProvenanceConversionError> =
    let refs = tableRefs arc

    match findTable options.LoadedTable refs with
    | Error error ->
        Error error
    | Ok loaded ->
        let endpointColumns = endpointColumns loaded.Table
        let propertyColumns = propertyColumns loaded.Table

        let rows, state = loadedRows loaded.Location loaded.Table endpointColumns emptyState
        let state = addLoadedConnections loaded.Location rows state
        let state = addPropertiesFromTable loaded.Location loaded.Table rows propertyColumns state

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

            let model =
                {
                    LoadedTableName = loaded.Location.TableName
                    PropertyValues = state.PropertyValues
                    InputSets = state.InputSets
                    OutputSets = state.OutputSets
                    Connections = state.Connections
                }

            let index =
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
```

- [ ] **Step 3: Run the converter build**

Run:

```powershell
dotnet build src\Components\src\Swate.Components.fsproj
```

Expected result: if the previous core model plan has already been applied, the build succeeds for `Swate.Components.fsproj`.

---

## Task 3: Verify Private Row Coordinate Scope

**Files:**
- Inspect: `src/Components/src/ProvenanceGrouping/ARCtrlConverter.fs`

- [ ] **Step 1: Confirm the internal row index is private**

Run:

```powershell
rg -n "RowIndex" src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs
```

Expected result: matches appear only in the private `RowContext` type, loaded row construction, property extraction, and collapsed previous-context row construction.

- [ ] **Step 2: Confirm no public row or column fields exist**

Run:

```powershell
rg -n "RowIndex:|ColumnIndex:|Row:|Column:" src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs
```

Expected result: the only match is `RowIndex:` inside `type private RowContext`.

- [ ] **Step 3: Run the component build again after inspection**

Run:

```powershell
dotnet build src\Components\src\Swate.Components.fsproj
```

Expected result: build succeeds for `Swate.Components.fsproj`.

---

## Task 4: Run the Shared Tests

**Files:**
- Test: `tests/Shared/Shared.Tests.fsproj`

- [ ] **Step 1: Build shared tests**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected result: build succeeds.

- [ ] **Step 2: Run shared tests**

Run:

```powershell
dotnet run --project tests\Shared\Shared.Tests.fsproj
```

Expected result: all shared tests pass, including `ProvenanceGrouping ARCtrl converter`.

- [ ] **Step 3: If test execution is not available, run the narrower build checks**

Run:

```powershell
dotnet build src\Components\src\Swate.Components.fsproj
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected result: both builds succeed.

---

## Task 5: Review Converter Behavior Against the Model Rules

**Files:**
- Inspect: `src/Components/src/ProvenanceGrouping/ARCtrlConverter.fs`
- Inspect: `tests/Shared/ProvenanceGrouping.ARCtrlConverter.Tests.fs`

- [ ] **Step 1: Verify loaded names come from loaded input/output cells**

Run:

```powershell
rg -n "ProvenanceSet|Name = name|InputSets|OutputSets|PropertyValueIds" src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs
```

Expected result: `ProvenanceSet.Name` is assigned from endpoint cell `name`, and property values only attach via `PropertyValueIds`.

- [ ] **Step 2: Verify no side field was added**

Run:

```powershell
rg -n "\bSide\b|ProvenanceSide" src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs tests\Shared\ProvenanceGrouping.ARCtrlConverter.Tests.fs
```

Expected result: no matches.

- [ ] **Step 3: Verify no ARCtrl object references are stored in public converter output**

Run:

```powershell
rg -n "ArcTable:|ArcTable option|ARC option|ArcStudy|ArcAssay|ArcRun" src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs
```

Expected result: matches appear only in private helper types/functions or in the `fromLoadedArc` input signature. Public output records use `ArcTableLocation`, names, IDs, headers, and maps.

- [ ] **Step 4: Verify row and column coordinates are not in public output**

Run:

```powershell
rg -n "RowIndex|ColumnIndex|row index|column index" src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs
```

Expected result: `RowIndex` appears only on private `RowContext`, in `loadedRows`, in `addPropertiesFromTable`, and as `-1` for collapsed context. There are no public record fields named row or column.

- [ ] **Step 5: Run a placeholder scan on this plan**

Run:

```powershell
$plan = "docs\superpowers\plans\2026-05-18-provenance-arctrl-arc-load-converter.md"
$patterns = @(
  ('T' + 'ODO'),
  ('T' + 'BD'),
  ('implement ' + 'later'),
  ('fill in ' + 'details'),
  ('add ' + 'appropriate'),
  ('handle ' + 'edge cases'),
  ('write tests ' + 'for the above'),
  ('similar ' + 'to')
)
foreach ($pattern in $patterns) {
  Select-String -Path $plan -Pattern $pattern -SimpleMatch
}
```

Expected result: no output.

---

## Task 6: Commit

**Files:**
- Commit all files changed by this plan.

- [ ] **Step 1: Inspect the diff**

Run:

```powershell
git diff -- src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs src\Components\src\Swate.Components.fsproj tests\Shared\ProvenanceGrouping.ARCtrlConverter.Tests.fs tests\Shared\Shared.Tests.fsproj tests\Shared\Shared.Tests.fs
```

Expected result: diff contains only the ARCtrl converter, project registration, and tests from this plan.

- [ ] **Step 2: Check whitespace**

Run:

```powershell
git diff --check -- src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs src\Components\src\Swate.Components.fsproj tests\Shared\ProvenanceGrouping.ARCtrlConverter.Tests.fs tests\Shared\Shared.Tests.fsproj tests\Shared\Shared.Tests.fs
```

Expected result: no whitespace errors.

- [ ] **Step 3: Commit the converter**

Run:

```powershell
git add src\Components\src\ProvenanceGrouping\ARCtrlConverter.fs src\Components\src\Swate.Components.fsproj tests\Shared\ProvenanceGrouping.ARCtrlConverter.Tests.fs tests\Shared\Shared.Tests.fsproj tests\Shared\Shared.Tests.fs
git commit -m "feat: convert loaded ARC provenance to core model"
```

Expected result: git creates a commit with the converter and tests.
