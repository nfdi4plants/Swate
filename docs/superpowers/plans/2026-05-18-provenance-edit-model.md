# Provenance Edit Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Swate-owned F#/Fable provenance edit model where loaded input/output sets are first-class named items, sets point to property value occurrences, and collapsed previous-context values keep writeback anchors.

**Architecture:** Add pure F# modules under `src/Components/src/ProvenanceGrouping` for types, import validation, grouping projection, edit patches, and fixtures. Keep ARCtrl out of public signatures. A later adapter may translate ARCtrl `ProcessInput.Name`, `ProcessOutput.Name`, process names, and headers into these plain Swate records.

**Tech Stack:** F# / Fable, `Map`, `Result`, Expecto shared tests, existing Swate Components project.

**Spec:** `docs/superpowers/specs/2026-05-18-provenance-edit-model-design.md`

---

## File Structure

| File | Responsibility | Action |
|---|---|---|
| `src/Components/src/ProvenanceGrouping/Types.fs` | Public loaded-set/property-value model types | Create |
| `src/Components/src/ProvenanceGrouping/Import.fs` | Plain DTO import and validation into `ProvenanceModel` | Create |
| `src/Components/src/ProvenanceGrouping/Grouping.fs` | Pure grouping and display connection projection | Create |
| `src/Components/src/ProvenanceGrouping/Edit.fs` | Edit commands and writeback patch production | Create |
| `src/Components/src/ProvenanceGrouping/Fixtures.fs` | Reusable sample provenance model for tests and preview adapters | Create |
| `src/Components/src/Swate.Components.fsproj` | Compile new F# files in dependency order | Modify |
| `tests/Shared/ProvenanceGrouping.Tests.fs` | Shared tests for import, grouping, edit, and fixtures | Create |
| `tests/Shared/Shared.Tests.fsproj` | Compile the new shared test file | Modify |
| `tests/Shared/Shared.Tests.fs` | Add the new test list to the suite | Modify |

## Scope Boundaries

- This plan implements the F# model and fixtures only.
- This plan does not replace `src/Components/src/ProvenanceGrouping/ProvenanceGrouping.tsx` or `ProvenanceGrouping.stories.tsx`.
- This plan does not add ARCtrl adapter code.
- This plan does not add public process IDs, row indices, column indices, previous-table set lists, or previous-table connection lists.

## Rules To Preserve

- Loaded input/output sets are actual loaded input/output names.
- Loaded input/output role is implied by `InputSets` or `OutputSets`; `ProvenanceSet` does not store `Side`.
- `ProvenanceSet.Name` is the source for UI labels and grouping members.
- Sets point to property value occurrence IDs.
- Property value occurrences may keep optional writeback anchors.
- Collapsed previous-context values keep enough table/process/header/input/output-name metadata for writeback.
- Previous context never creates first-class sets or connections.
- Repeated property values are preserved as separate occurrences.
- Grouping uses actual property values only.
- Missing grouped values do not create a shared missing-value group.
- Loaded-table connections are exact input-set/output-set pairs.
- New values and connections are created only for `model.LoadedTableName`.

---

## Chunk 1: Core Types

### Task 1: Add the loaded-set public type model

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/Types.fs`
- Modify: `src/Components/src/Swate.Components.fsproj`
- Create: `tests/Shared/ProvenanceGrouping.Tests.fs`
- Modify: `tests/Shared/Shared.Tests.fsproj`
- Modify: `tests/Shared/Shared.Tests.fs`

- [ ] **Step 1: Create the failing shared test file**

Create `tests/Shared/ProvenanceGrouping.Tests.fs` with this content:

```fsharp
module ProvenanceGroupingTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Swate.Components.ProvenanceGrouping.Types

let private term name =
    {
        Name = name
        TermSource = None
        TermAccession = None
    }

let private ioHeader kind text =
    {
        Kind = kind
        Text = text
    }

let private propertyHeader kind name =
    {
        Kind = kind
        Category = term name
    }

let typeTests =
    testList "Types" [
        testCase "loaded input set carries the actual input name" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputSet =
                {
                    Id = "input-a"
                    TableName = "assay-table"
                    Header = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
                    Name = "Input A"
                    PropertyValueIds = [ "pv-species-a" ]
                }
            let propertyValue =
                {
                    Id = "pv-species-a"
                    Header = species
                    Value = ProvenanceValue.Text "Arabidopsis"
                    Unit = None
                    Source =
                        Some
                            {
                                TableName = "previous-table"
                                ProcessName = Some "previous-process"
                                Header = species
                                InputNames = [ "Ancestor A" ]
                                OutputNames = []
                            }
                }

            Expect.equal inputSet.Name "Input A" "Loaded input name should live on the set."
            Expect.equal inputSet.PropertyValueIds [ propertyValue.Id ] "Set should point to property value occurrence."
            match propertyValue.Source with
            | Some source ->
                Expect.equal source.TableName "previous-table" "Collapsed value should keep writeback table metadata."
            | None ->
                failwith "Expected collapsed value source."
    ]

let tests =
    testList "ProvenanceGrouping" [
        typeTests
    ]
```

- [ ] **Step 2: Register the shared test file**

In `tests/Shared/Shared.Tests.fsproj`, add the new compile item before `Shared.Tests.fs`:

```xml
<Compile Include="ProvenanceGrouping.Tests.fs"/>
```

The first item group should be:

```xml
<ItemGroup>
  <Compile Include="Landing.Tests.fs"/>
  <Compile Include="PathHelpers.Tests.fs"/>
  <Compile Include="ProvenanceGrouping.Tests.fs"/>
  <Compile Include="Shared.Tests.fs"/>
</ItemGroup>
```

- [ ] **Step 3: Add the test list to the shared suite**

In `tests/Shared/Shared.Tests.fs`, add `ProvenanceGroupingTests.tests` to the `shared` list:

```fsharp
let shared = testList "Shared" [
    example_tests
    LandingTests.tests
    PathHelpersTests.tests
    ProvenanceGroupingTests.tests
]
```

- [ ] **Step 4: Run the failing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build fails with an error like `FS0039: The namespace 'ProvenanceGrouping' is not defined`.

- [ ] **Step 5: Create `Types.fs`**

Create `src/Components/src/ProvenanceGrouping/Types.fs`:

```fsharp
module Swate.Components.ProvenanceGrouping.Types

/// Name of a study, assay, or run table as known by the caller.
type ProvenanceTableName = string

/// Optional process name from the source model.
/// This is metadata for writeback/disambiguation, not a public process ID.
type ProvenanceProcessName = string

/// Swate-local stable ID for one loaded input or output endpoint.
/// The actual user-facing input/output name lives on `ProvenanceSet.Name`.
type ProvenanceSetId = string

/// Swate-local stable ID for one loaded input-to-output connection.
type ProvenanceConnectionId = string

/// Swate-local stable ID for one property value occurrence.
/// Repeated equal values must have separate IDs.
type ProvenancePropertyValueId = string

/// Which collection or display side a projection/helper should use.
/// This is not stored on `ProvenanceSet`; loaded role is implied by `InputSets`/`OutputSets`.
[<RequireQualifiedAccess>]
type ProvenanceSide =
    /// Select loaded input endpoints or render an input-side display group.
    | Input
    /// Select loaded output endpoints or render an output-side display group.
    | Output

/// Normalized kind of an input or output table header.
/// Mirrors the relevant ARCtrl `IOType` cases without exposing ARCtrl types.
[<RequireQualifiedAccess>]
type ProvenanceIOKind =
    /// Source-like endpoint.
    | Source
    /// Sample-like endpoint.
    | Sample
    /// Data/file-like endpoint.
    | Data
    /// Material-like endpoint.
    | Material
    /// Source model provided a custom input/output header kind.
    | FreeText of string
    /// Adapter could not classify the endpoint kind.
    | Unknown

/// Header metadata for a loaded input/output endpoint.
type ProvenanceIOHeader =
    {
        /// Normalized input/output kind used for behavior decisions.
        Kind: ProvenanceIOKind
        /// Original or display-ready header text, such as `Input [Sample Name]`.
        Text: string
    }

/// Normalized kind of editable provenance property.
[<RequireQualifiedAccess>]
type ProvenancePropertyKind =
    /// Characteristic value on an input or output material-like endpoint.
    | Characteristic
    /// Factor value, normally on outputs.
    | Factor
    /// Process parameter value.
    | Parameter
    /// Component value when source tables expose components.
    | Component

/// Small ontology term projection used for property categories, units, and term values.
type ProvenanceTerm =
    {
        /// Human-readable term name.
        Name: string
        /// Optional ontology source name/reference.
        TermSource: string option
        /// Optional ontology accession/IRI.
        TermAccession: string option
    }

/// Value projection that is simple to serialize and use from Fable.
[<RequireQualifiedAccess>]
type ProvenanceValue =
    /// Plain string or free text value.
    | Text of string
    /// Integer value.
    | Integer of int
    /// Floating point value.
    | Float of float
    /// Ontology-backed term value.
    | Term of ProvenanceTerm

/// Property key used for grouping, editing, and writeback.
type ProvenancePropertyHeader =
    {
        /// Category family, such as characteristic, factor, or parameter.
        Kind: ProvenancePropertyKind
        /// Category term, such as Species, Temperature, or Replicate.
        Category: ProvenanceTerm
    }

/// Source metadata needed to update an existing property value in its source model.
/// This is a writeback anchor, not a graph edge and not an ARCtrl object reference.
type ProvenanceWritebackAnchor =
    {
        /// Source table containing the property value occurrence.
        TableName: ProvenanceTableName
        /// Optional source process name when the adapter can provide one.
        ProcessName: ProvenanceProcessName option
        /// Property header to locate the source column/value family.
        Header: ProvenancePropertyHeader
        /// Source input names that identify the owning context.
        InputNames: string list
        /// Source output names that identify the owning context.
        OutputNames: string list
    }

/// One concrete key/value occurrence.
/// Sets point to these occurrences; repeated equal values remain separate records.
type ProvenancePropertyValue =
    {
        /// Stable Swate-local ID for this occurrence.
        Id: ProvenancePropertyValueId
        /// Property key/category for grouping and writeback.
        Header: ProvenancePropertyHeader
        /// Stored property value.
        Value: ProvenanceValue
        /// Optional unit term.
        Unit: ProvenanceTerm option
        /// Optional writeback anchor.
        /// Loaded values may omit this when the target can be derived from loaded set membership.
        /// Collapsed previous-context values must keep this when they should be editable.
        Source: ProvenanceWritebackAnchor option
    }

/// One actual loaded input or output endpoint.
/// This is the first-class UI item before grouping; it is not a collapsed graph node.
type ProvenanceSet =
    {
        /// Swate-local stable endpoint ID used by connections and display groups.
        Id: ProvenanceSetId
        /// Loaded table this endpoint belongs to.
        TableName: ProvenanceTableName
        /// Loaded input/output header this endpoint came from.
        Header: ProvenanceIOHeader
        /// Actual loaded input/output name from the table cell or source adapter.
        Name: string
        /// Property value occurrences attached to this loaded endpoint.
        PropertyValueIds: ProvenancePropertyValueId list
    }

/// One exact connection between a loaded input endpoint and a loaded output endpoint.
/// Previous-table connections are intentionally not represented here.
type ProvenanceConnection =
    {
        /// Swate-local stable connection ID.
        Id: ProvenanceConnectionId
        /// Loaded table containing this editable connection.
        TableName: ProvenanceTableName
        /// Optional process name associated with this connection.
        ProcessName: ProvenanceProcessName option
        /// Loaded input endpoint ID.
        InputSetId: ProvenanceSetId
        /// Loaded output endpoint ID.
        OutputSetId: ProvenanceSetId
    }

/// Complete provenance projection for one loaded table session.
/// The model stores loaded endpoints, their property value pointers, and loaded connections.
type ProvenanceModel =
    {
        /// The table currently opened and editable in the viewer.
        LoadedTableName: ProvenanceTableName
        /// Shared property value occurrence store.
        PropertyValues: Map<ProvenancePropertyValueId, ProvenancePropertyValue>
        /// First-class loaded input endpoints, keyed by `ProvenanceSet.Id`.
        InputSets: Map<ProvenanceSetId, ProvenanceSet>
        /// First-class loaded output endpoints, keyed by `ProvenanceSet.Id`.
        OutputSets: Map<ProvenanceSetId, ProvenanceSet>
        /// Editable loaded-table connections.
        Connections: Map<ProvenanceConnectionId, ProvenanceConnection>
    }
```

- [ ] **Step 6: Register `Types.fs` in the component project**

In `src/Components/src/Swate.Components.fsproj`, insert this compile item after `GenericComponents\Navbar.fs` and before `ARCSelector\Selector.fs`:

```xml
<Compile Include="ProvenanceGrouping\Types.fs" />
```

- [ ] **Step 7: Run the passing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build succeeds.

- [ ] **Step 8: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/Types.fs src/Components/src/Swate.Components.fsproj tests/Shared/ProvenanceGrouping.Tests.fs tests/Shared/Shared.Tests.fsproj tests/Shared/Shared.Tests.fs
git commit -m "feat(provenance): add loaded endpoint edit types"
```

---

## Chunk 2: Import Builder

### Task 2: Import plain DTOs into the loaded-set model

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/Import.fs`
- Modify: `src/Components/src/Swate.Components.fsproj`
- Modify: `tests/Shared/ProvenanceGrouping.Tests.fs`

- [ ] **Step 1: Add failing import tests**

In `tests/Shared/ProvenanceGrouping.Tests.fs`, add this `open` after the existing `open Swate.Components.ProvenanceGrouping.Types`:

```fsharp
open Swate.Components.ProvenanceGrouping.Import
```

Then add these helpers and tests before `let tests =`:

```fsharp
let private anchor tableName processName header inputNames outputNames =
    {
        TableName = tableName
        ProcessName = processName
        Header = header
        InputNames = inputNames
        OutputNames = outputNames
    }

let private propertyValue id header value source =
    {
        Id = id
        Header = header
        Value = value
        Unit = None
        Source = source
    }

let private importedSet id tableName header name propertyValueIds =
    {
        Id = id
        TableName = tableName
        Header = header
        Name = name
        PropertyValueIds = propertyValueIds
    }

let private importedConnection id tableName processName inputSetId outputSetId =
    {
        Id = id
        TableName = tableName
        ProcessName = processName
        InputSetId = inputSetId
        OutputSetId = outputSetId
    }

let importTests =
    testList "Import" [
        testCase "fromImportedProvenance preserves loaded names and repeated property values" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
            let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"
            let imported =
                {
                    LoadedTableName = "assay-table"
                    PropertyValues = [
                        propertyValue "pv-species-a" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "previous-table" (Some "previous-process") species [ "Ancestor A" ] []))
                        propertyValue "pv-rep-1" replicate (ProvenanceValue.Text "1") (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                        propertyValue "pv-rep-2" replicate (ProvenanceValue.Text "2") (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output A" ]))
                    ]
                    InputSets = [
                        importedSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a"; "pv-rep-1"; "pv-rep-2" ]
                    ]
                    OutputSets = [
                        importedSet "output-a" "assay-table" outputHeader "Output A" [ "pv-rep-1"; "pv-rep-2" ]
                    ]
                    Connections = [
                        importedConnection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                    ]
                }

            let result = fromImportedProvenance imported

            Expect.equal result.Warnings [] "Valid import should not warn."
            Expect.equal result.Model.InputSets.["input-a"].Name "Input A" "Input set should carry the loaded input name."
            Expect.equal result.Model.OutputSets.["output-a"].Name "Output A" "Output set should carry the loaded output name."
            Expect.equal result.Model.PropertyValues.Count 3 "Repeated values should remain separate occurrences."
            Expect.equal result.Model.InputSets.["input-a"].PropertyValueIds [ "pv-species-a"; "pv-rep-1"; "pv-rep-2" ] "Set should point to all property value occurrences."

        testCase "fromImportedProvenance warns and skips previous-table sets and connections" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
            let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"
            let imported =
                {
                    LoadedTableName = "assay-table"
                    PropertyValues = [
                        propertyValue "pv-species-a" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "previous-table" (Some "previous-process") species [ "Ancestor A" ] []))
                    ]
                    InputSets = [
                        importedSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-a"; "missing-pv" ]
                        importedSet "previous-input" "previous-table" inputHeader "Ancestor A" [ "pv-species-a" ]
                    ]
                    OutputSets = [
                        importedSet "output-a" "assay-table" outputHeader "Output A" []
                    ]
                    Connections = [
                        importedConnection "previous-connection" "previous-table" (Some "previous-process") "previous-input" "output-a"
                        importedConnection "dangling-connection" "assay-table" (Some "assay-process") "missing-input" "output-a"
                    ]
                }

            let result = fromImportedProvenance imported

            Expect.isFalse (result.Model.InputSets.ContainsKey "previous-input") "Previous-table set should not become a first-class set."
            Expect.equal result.Model.Connections.Count 1 "Loaded connection map keeps only loaded-table connection IDs, even when they warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "previous-input")) "Skipped previous set should warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "missing-pv")) "Missing property value should warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "previous-connection")) "Skipped previous connection should warn."
            Expect.isTrue (result.Warnings |> List.exists (fun warning -> warning.Contains "missing-input")) "Dangling loaded connection should warn."
    ]
```

Add `importTests` to the final test list:

```fsharp
let tests =
    testList "ProvenanceGrouping" [
        typeTests
        importTests
    ]
```

- [ ] **Step 2: Run the failing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build fails with an error like `FS0039: The namespace or module 'Import' is not defined`.

- [ ] **Step 3: Create `Import.fs`**

Create `src/Components/src/ProvenanceGrouping/Import.fs`:

```fsharp
module Swate.Components.ProvenanceGrouping.Import

open Swate.Components.ProvenanceGrouping.Types

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

let private toPropertyValue (propertyValue: ImportedPropertyValue) =
    {
        Id = propertyValue.Id
        Header = propertyValue.Header
        Value = propertyValue.Value
        Unit = propertyValue.Unit
        Source = propertyValue.Source
    }

let private toSet (set: ImportedSet) =
    {
        Id = set.Id
        TableName = set.TableName
        Header = set.Header
        Name = set.Name
        PropertyValueIds = set.PropertyValueIds
    }

let private mapImportedSets loadedTableName sets =
    sets
    |> List.filter (fun set -> set.TableName = loadedTableName)
    |> List.map (fun set -> set.Id, toSet set)
    |> Map.ofList

let private skippedSetWarnings loadedTableName label sets =
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
    let propertyValues =
        imported.PropertyValues
        |> List.map (fun propertyValue -> propertyValue.Id, toPropertyValue propertyValue)
        |> Map.ofList

    let inputSets = mapImportedSets imported.LoadedTableName imported.InputSets
    let outputSets = mapImportedSets imported.LoadedTableName imported.OutputSets

    let loadedConnections, skippedConnections =
        imported.Connections
        |> List.partition (fun connection -> connection.TableName = imported.LoadedTableName)

    let connections =
        loadedConnections
        |> List.map (fun connection ->
            connection.Id,
            {
                Id = connection.Id
                TableName = connection.TableName
                ProcessName = connection.ProcessName
                InputSetId = connection.InputSetId
                OutputSetId = connection.OutputSetId
            })
        |> Map.ofList

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
```

- [ ] **Step 4: Register `Import.fs`**

In `src/Components/src/Swate.Components.fsproj`, add this line immediately after `ProvenanceGrouping\Types.fs`:

```xml
<Compile Include="ProvenanceGrouping\Import.fs" />
```

- [ ] **Step 5: Run the passing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/Import.fs src/Components/src/Swate.Components.fsproj tests/Shared/ProvenanceGrouping.Tests.fs
git commit -m "feat(provenance): import loaded provenance sets"
```

---

## Chunk 3: Grouping Projection

### Task 3: Derive display groups and exact display connections

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/Grouping.fs`
- Modify: `src/Components/src/Swate.Components.fsproj`
- Modify: `tests/Shared/ProvenanceGrouping.Tests.fs`

- [ ] **Step 1: Add failing grouping tests**

Add this `open` near the top of `tests/Shared/ProvenanceGrouping.Tests.fs`:

```fsharp
open Swate.Components.ProvenanceGrouping.Grouping
```

Add this helper and test list before `let tests =`:

```fsharp
let private validImportedModel () =
    let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
    let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
    let inputHeader = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
    let outputHeader = ioHeader ProvenanceIOKind.Sample "Output [Sample Name]"
    fromImportedProvenance
        {
            LoadedTableName = "assay-table"
            PropertyValues = [
                propertyValue "pv-species-arabidopsis-a" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "assay-table" (Some "assay-process") species [ "Input A" ] []))
                propertyValue "pv-species-arabidopsis-b" species (ProvenanceValue.Text "Arabidopsis") (Some(anchor "assay-table" (Some "assay-process") species [ "Input B" ] []))
                propertyValue "pv-rep-output-b-1" replicate (ProvenanceValue.Text "1") (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input A" ] [ "Output B" ]))
                propertyValue "pv-rep-output-b-2" replicate (ProvenanceValue.Text "2") (Some(anchor "assay-table" (Some "assay-process") replicate [ "Input B" ] [ "Output B" ]))
            ]
            InputSets = [
                importedSet "input-a" "assay-table" inputHeader "Input A" [ "pv-species-arabidopsis-a" ]
                importedSet "input-b" "assay-table" inputHeader "Input B" [ "pv-species-arabidopsis-b" ]
                importedSet "input-c" "assay-table" inputHeader "Input C" []
            ]
            OutputSets = [
                importedSet "output-a" "assay-table" outputHeader "Output A" []
                importedSet "output-b" "assay-table" outputHeader "Output B" [ "pv-rep-output-b-1"; "pv-rep-output-b-2" ]
                importedSet "output-c" "assay-table" outputHeader "Output C" []
            ]
            Connections = [
                importedConnection "connection-a" "assay-table" (Some "assay-process") "input-a" "output-a"
                importedConnection "connection-b" "assay-table" (Some "assay-process") "input-a" "output-b"
                importedConnection "connection-c" "assay-table" (Some "assay-process") "input-b" "output-b"
                importedConnection "connection-d" "assay-table" (Some "assay-process") "input-c" "output-c"
            ]
        }
        |> fun result -> result.Model

let groupingTests =
    testList "Grouping" [
        testCase "no grouping displays each loaded set by name" <| fun _ ->
            let model = validImportedModel ()
            let groups = displayGroups model ProvenanceSide.Input []

            Expect.equal (groups |> List.map (fun group -> group.Members.Head.Name)) [ "Input A"; "Input B"; "Input C" ] "No grouping should preserve loaded input names."

        testCase "multi-value grouping duplicates the loaded set into each value group" <| fun _ ->
            let model = validImportedModel ()
            let replicate = propertyHeader ProvenancePropertyKind.Parameter "Replicate"
            let groups = displayGroups model ProvenanceSide.Output [ { Header = replicate } ]

            let outputBGroupCount =
                groups
                |> List.filter (fun group -> group.Members |> List.exists (fun member' -> member'.SetId = "output-b"))
                |> List.length

            Expect.equal outputBGroupCount 2 "Output B should appear once for each repeated replicate value."

        testCase "displayConnections expands to represented loaded set pairs only" <| fun _ ->
            let model = validImportedModel ()
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputGroups = displayGroups model ProvenanceSide.Input [ { Header = species } ]
            let outputGroups = displayGroups model ProvenanceSide.Output []
            let connections = displayConnections model inputGroups outputGroups

            let representedIds =
                connections
                |> List.collect (fun connection -> connection.ConnectionIds)
                |> List.sort

            Expect.equal representedIds [ "connection-a"; "connection-b"; "connection-c"; "connection-d" ] "Display lines should represent real loaded connections only."
    ]
```

Add `groupingTests` to the final test list:

```fsharp
let tests =
    testList "ProvenanceGrouping" [
        typeTests
        importTests
        groupingTests
    ]
```

- [ ] **Step 2: Run the failing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build fails with an error like `FS0039: The namespace or module 'Grouping' is not defined`.

- [ ] **Step 3: Create `Grouping.fs`**

Create `src/Components/src/ProvenanceGrouping/Grouping.fs`:

```fsharp
module Swate.Components.ProvenanceGrouping.Grouping

open Swate.Components.ProvenanceGrouping.Types

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

type DisplayGroup =
    {
        Id: string
        TableName: ProvenanceTableName
        Side: ProvenanceSide
        GroupingValues: (GroupingKey * ProvenanceValue) list
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

let private valueText value =
    match value with
    | ProvenanceValue.Text value -> value
    | ProvenanceValue.Integer value -> string value
    | ProvenanceValue.Float value -> string value
    | ProvenanceValue.Term term -> term.Name

let private sideText side =
    match side with
    | ProvenanceSide.Input -> "input"
    | ProvenanceSide.Output -> "output"

let private loadedSets model side =
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
    |> List.map (fun propertyValue -> key, propertyValue.Value, propertyValue.Id)

let private combinations values =
    let rec loop collected remaining =
        match remaining with
        | [] -> [ List.rev collected ]
        | head :: tail ->
            head
            |> List.collect (fun value -> loop (value :: collected) tail)

    loop [] values

let private displayMember set propertyValueIds =
    {
        SetId = set.Id
        Name = set.Name
        PropertyValueIds = propertyValueIds
    }

let private groupId side values fallbackSetId =
    match values with
    | [] -> sprintf "%s:%s" (sideText side) fallbackSetId
    | _ ->
        values
        |> List.map (fun (key, value) -> sprintf "%s=%s" key.Header.Category.Name (valueText value))
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
                                |> List.map (fun (key, value, _) -> key, value)
                            let propertyValueIds =
                                combination
                                |> List.map (fun (_, _, propertyValueId) -> propertyValueId)
                            yield groupId side groupingValues set.Id, set.TableName, groupingValues, displayMember set propertyValueIds
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
```

- [ ] **Step 4: Register `Grouping.fs`**

In `src/Components/src/Swate.Components.fsproj`, add this line immediately after `ProvenanceGrouping\Import.fs`:

```xml
<Compile Include="ProvenanceGrouping\Grouping.fs" />
```

- [ ] **Step 5: Run the passing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/Grouping.fs src/Components/src/Swate.Components.fsproj tests/Shared/ProvenanceGrouping.Tests.fs
git commit -m "feat(provenance): group loaded sets by property values"
```

---

## Chunk 4: Edit Commands And Patches

### Task 4: Add loaded edits and collapsed value update patches

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/Edit.fs`
- Modify: `src/Components/src/Swate.Components.fsproj`
- Modify: `tests/Shared/ProvenanceGrouping.Tests.fs`

- [ ] **Step 1: Add failing edit tests**

Add this `open` near the top of `tests/Shared/ProvenanceGrouping.Tests.fs`:

```fsharp
open Swate.Components.ProvenanceGrouping.Edit
```

Add this test list before `let tests =`:

```fsharp
let editTests =
    testList "Edit" [
        testCase "updatePropertyValue preserves collapsed source anchor" <| fun _ ->
            let model = validImportedModel ()

            match updatePropertyValue "pv-species-arabidopsis-a" (ProvenanceValue.Text "A. thaliana") None model with
            | Ok(nextModel, [ ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, source, _, newValue, _) ]) ->
                Expect.equal propertyValueId "pv-species-arabidopsis-a" "Patch should identify edited occurrence."
                Expect.equal source.InputNames [ "Input A" ] "Patch should preserve source input name."
                Expect.equal newValue (ProvenanceValue.Text "A. thaliana") "Patch should carry edited value."
                Expect.equal nextModel.PropertyValues.["pv-species-arabidopsis-a"].Value (ProvenanceValue.Text "A. thaliana") "Model should update the occurrence."
            | other ->
                failwithf "Expected one UpdatePropertyValue patch, got %A" other

        testCase "createLoadedPropertyValue adds occurrence to target loaded set" <| fun _ ->
            let model = validImportedModel ()
            let treatment = propertyHeader ProvenancePropertyKind.Characteristic "Treatment"
            let command =
                {
                    Target = ProvenancePropertyTarget.InputSets [ "input-c" ]
                    CopiedFrom = None
                    Header = treatment
                    Value = ProvenanceValue.Text "Drought"
                    Unit = None
                }

            match createLoadedPropertyValue command model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, copiedFrom, header, value, _) ]) ->
                Expect.equal target (ProvenancePropertyTarget.InputSets [ "input-c" ]) "Patch should target the loaded input set."
                Expect.equal copiedFrom None "New value should not be copied from another occurrence."
                Expect.equal header treatment "Patch should carry the requested header."
                Expect.equal value (ProvenanceValue.Text "Drought") "Patch should carry the requested value."
                Expect.equal (nextModel.InputSets.["input-c"].PropertyValueIds.Length) 1 "Target input set should point to the new value."
            | other ->
                failwithf "Expected one AddLoadedPropertyValue patch, got %A" other

        testCase "copyPropertyValueToLoadedTarget copies previous value to existing loaded connection" <| fun _ ->
            let model = validImportedModel ()

            match copyPropertyValueToLoadedTarget "pv-species-arabidopsis-a" (ProvenancePropertyTarget.Connections [ "connection-d" ]) model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedPropertyValue(target, copiedFrom, _, value, _) ]) ->
                Expect.equal target (ProvenancePropertyTarget.Connections [ "connection-d" ]) "Patch should target the existing loaded connection."
                Expect.equal copiedFrom (Some "pv-species-arabidopsis-a") "Patch should preserve copied source occurrence."
                Expect.equal value (ProvenanceValue.Text "Arabidopsis") "Patch should copy the value."
                Expect.isTrue (nextModel.InputSets.["input-c"].PropertyValueIds.Length > model.InputSets.["input-c"].PropertyValueIds.Length) "Connection input set should point to the copied loaded occurrence."
                Expect.isTrue (nextModel.OutputSets.["output-c"].PropertyValueIds.Length > model.OutputSets.["output-c"].PropertyValueIds.Length) "Connection output set should point to the copied loaded occurrence."
            | other ->
                failwithf "Expected one AddLoadedPropertyValue patch, got %A" other

        testCase "connectSets creates a loaded input output connection" <| fun _ ->
            let model = validImportedModel ()

            match connectSets "input-c" "output-a" None model with
            | Ok(nextModel, [ ProvenanceTablePatch.AddLoadedConnection(tableName, processName, inputSetId, outputSetId) ]) ->
                Expect.equal tableName "assay-table" "Patch should target loaded table."
                Expect.equal processName None "Caller may create a connection without assigning a process name yet."
                Expect.equal inputSetId "input-c" "Patch should keep input set."
                Expect.equal outputSetId "output-a" "Patch should keep output set."
                Expect.isTrue (nextModel.Connections |> Map.exists (fun _ connection -> connection.InputSetId = "input-c" && connection.OutputSetId = "output-a")) "Model should contain new connection."
            | other ->
                failwithf "Expected one AddLoadedConnection patch, got %A" other
    ]
```

Add `editTests` to the final test list:

```fsharp
let tests =
    testList "ProvenanceGrouping" [
        typeTests
        importTests
        groupingTests
        editTests
    ]
```

- [ ] **Step 2: Run the failing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build fails with an error like `FS0039: The namespace or module 'Edit' is not defined`.

- [ ] **Step 3: Create `Edit.fs`**

Create `src/Components/src/ProvenanceGrouping/Edit.fs`:

```fsharp
module Swate.Components.ProvenanceGrouping.Edit

open Swate.Components.ProvenanceGrouping.Types

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

let private loadedSet model set =
    if set.TableName = model.LoadedTableName then
        Ok set
    else
        Error(EditError.PreviousContextCreationNotAllowed set.TableName)

let private addPropertyValueId propertyValueId set =
    if set.PropertyValueIds |> List.contains propertyValueId then
        set
    else
        { set with PropertyValueIds = set.PropertyValueIds @ [ propertyValueId ] }

let private updateSets propertyValueId targetSetIds sets =
    targetSetIds
    |> List.fold (fun state setId ->
        state
        |> Map.change setId (Option.map (addPropertyValueId propertyValueId))) sets

let private chooseInputSet model setId =
    match model.InputSets.TryFind setId with
    | Some set -> loadedSet model set
    | None -> Error(EditError.SetNotFound setId)

let private chooseOutputSet model setId =
    match model.OutputSets.TryFind setId with
    | Some set -> loadedSet model set
    | None -> Error(EditError.SetNotFound setId)

let private chooseConnection model connectionId =
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

let private sourceFromTarget model header target resolvedTarget =
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

let private sourceFromLoadedMembership model propertyValue =
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

let updatePropertyValue propertyValueId newValue newUnit model : EditResult =
    match model.PropertyValues.TryFind propertyValueId with
    | None -> Error(EditError.PropertyNotFound propertyValueId)
    | Some propertyValue ->
        match propertyValue.Source |> Option.orElseWith (fun () -> sourceFromLoadedMembership model propertyValue) with
        | None -> Error(EditError.MissingSourceAnchor propertyValueId)
        | Some source ->
            let nextPropertyValue =
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

let createLoadedPropertyValue command model : EditResult =
    match targetSets model command.Target with
    | Error error -> Error error
    | Ok resolvedTarget ->
        let propertyValueId = nextPropertyValueId model
        let source = sourceFromTarget model command.Header command.Target resolvedTarget
        let propertyValue =
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

let copyPropertyValueToLoadedTarget propertyValueId target model : EditResult =
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

let connectSets inputSetId outputSetId processName model : EditResult =
    match chooseInputSet model inputSetId, chooseOutputSet model outputSetId with
    | Error error, _ -> Error error
    | _, Error error -> Error error
    | Ok _, Ok _ ->
        let connectionId = nextConnectionId model
        let connection =
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
```

- [ ] **Step 4: Register `Edit.fs`**

In `src/Components/src/Swate.Components.fsproj`, add this line immediately after `ProvenanceGrouping\Grouping.fs`:

```xml
<Compile Include="ProvenanceGrouping\Edit.fs" />
```

- [ ] **Step 5: Run the passing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build succeeds.

- [ ] **Step 6: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/Edit.fs src/Components/src/Swate.Components.fsproj tests/Shared/ProvenanceGrouping.Tests.fs
git commit -m "feat(provenance): edit loaded sets and collapsed values"
```

---

## Chunk 5: Fixtures And Project Verification

### Task 5: Add a reusable fixture and verify F# and Fable builds

**Files:**
- Create: `src/Components/src/ProvenanceGrouping/Fixtures.fs`
- Modify: `src/Components/src/Swate.Components.fsproj`
- Modify: `tests/Shared/ProvenanceGrouping.Tests.fs`

- [ ] **Step 1: Add failing fixture tests**

Add this `open` near the top of `tests/Shared/ProvenanceGrouping.Tests.fs`:

```fsharp
open Swate.Components.ProvenanceGrouping.Fixtures
```

Add this test list before `let tests =`:

```fsharp
let fixtureTests =
    testList "Fixtures" [
        testCase "sampleModel includes loaded names and previous collapsed value" <| fun _ ->
            let model = sampleModel ()

            Expect.equal model.InputSets.["input-a"].Name "Input A" "Fixture should expose actual loaded input name."
            Expect.equal model.OutputSets.["output-b"].Name "Output B" "Fixture should expose actual loaded output name."

            let previousValues =
                model.InputSets.["input-a"].PropertyValueIds
                |> List.choose (fun propertyValueId -> model.PropertyValues.TryFind propertyValueId)
                |> List.filter (fun value -> value.Source |> Option.exists (fun source -> source.TableName = "previous-study-table"))

            Expect.isNonEmpty previousValues "Fixture should include collapsed previous-context property value."

        testCase "sampleModel connections are loaded set pairs" <| fun _ ->
            let model = sampleModel ()
            let pairs =
                model.Connections
                |> Map.toList
                |> List.map (fun (_, connection) -> connection.InputSetId, connection.OutputSetId)
                |> List.sort

            Expect.equal
                pairs
                [
                    "input-a", "output-a"
                    "input-a", "output-b"
                    "input-b", "output-b"
                    "input-c", "output-c"
                    "input-d", "output-d"
                ]
                "Fixture should preserve exact loaded input/output set connections."
    ]
```

Add `fixtureTests` to the final test list:

```fsharp
let tests =
    testList "ProvenanceGrouping" [
        typeTests
        importTests
        groupingTests
        editTests
        fixtureTests
    ]
```

- [ ] **Step 2: Run the failing build**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build fails with an error like `FS0039: The namespace or module 'Fixtures' is not defined`.

- [ ] **Step 3: Create `Fixtures.fs`**

Create `src/Components/src/ProvenanceGrouping/Fixtures.fs`:

```fsharp
module Swate.Components.ProvenanceGrouping.Fixtures

open Swate.Components.ProvenanceGrouping.Types

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

let private source tableName processName header inputNames outputNames =
    {
        TableName = tableName
        ProcessName = processName
        Header = header
        InputNames = inputNames
        OutputNames = outputNames
    }

let private propertyValue id header value tableName processName inputNames outputNames =
    {
        Id = id
        Header = header
        Value = ProvenanceValue.Text value
        Unit = None
        Source = Some(source tableName processName header inputNames outputNames)
    }

let private set id header name propertyValueIds =
    {
        Id = id
        TableName = "assay-table"
        Header = header
        Name = name
        PropertyValueIds = propertyValueIds
    }

let private connection id inputSetId outputSetId =
    {
        Id = id
        TableName = "assay-table"
        ProcessName = Some "assay-process"
        InputSetId = inputSetId
        OutputSetId = outputSetId
    }

let sampleModel () =
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
```

- [ ] **Step 4: Register `Fixtures.fs`**

In `src/Components/src/Swate.Components.fsproj`, add this line immediately after `ProvenanceGrouping\Edit.fs`:

```xml
<Compile Include="ProvenanceGrouping\Fixtures.fs" />
```

- [ ] **Step 5: Run shared verification**

Run:

```powershell
dotnet build tests\Shared\Shared.Tests.fsproj
```

Expected: build succeeds.

- [ ] **Step 6: Run component verification**

Run:

```powershell
dotnet build src\Components\src\Swate.Components.fsproj
```

Expected: build succeeds.

- [ ] **Step 7: Run Fable generation verification**

Run:

```powershell
dotnet fable src\Components\src\Swate.Components.fsproj --outDir src\Components\dist\fable
```

Expected: Fable completes and generated files for `ProvenanceGrouping/Types.fs`, `Import.fs`, `Grouping.fs`, `Edit.fs`, and `Fixtures.fs` appear under the generated output used by the command.

- [ ] **Step 8: Commit**

```powershell
git add src/Components/src/ProvenanceGrouping/Fixtures.fs src/Components/src/Swate.Components.fsproj tests/Shared/ProvenanceGrouping.Tests.fs
git commit -m "test(provenance): add loaded provenance fixture"
```

---

## Final Acceptance Criteria

- `ProvenanceSet.Name` is the actual loaded input/output name and is used by grouping members.
- `ProvenanceSet` and `ImportedSet` have no `Side` field.
- `ProvenanceModel` has no `Entries` map and no previous-table set/connection graph.
- `ProvenanceConnection` connects exact loaded input/output set IDs.
- Property values are stored once and sets point to property value IDs.
- Previous-context property values keep optional `ProvenanceWritebackAnchor` metadata.
- Loaded input/output sets support full property addition and connection creation.
- Previous-context values support editing through their anchor and copying onto existing loaded targets.
- There is no `ProvenanceOrigin` field in the core model; new/imported state is represented by patches and caller session state.
- There is no `ProcessId`, row index, or column index in public model types.
- `dotnet build tests\Shared\Shared.Tests.fsproj` succeeds.
- `dotnet build src\Components\src\Swate.Components.fsproj` succeeds.
- Fable generation succeeds for the new modules.

## Self-Review Commands

Run these before marking the plan implemented:

```powershell
rg -n "T[B]D|T[O]DO|implement[ ]later|fill[ ]in|appropriate[ ]error|handle[ ]edge|Write[ ]tests[ ]for[ ]the[ ]above|Similar[ ]to" docs\superpowers\plans\2026-05-18-provenance-edit-model.md
```

Expected: no output.

```powershell
rg -n "Provenance[O]rigin|O[r]igin[ ]=|O[r]igin:|Entry[I]ds|Entry[P]airs|Process[I]d|row[ ]index|column[ ]index|previous-table[ ]set|previous-table[ ]connection" src\Components\src\ProvenanceGrouping tests\Shared\ProvenanceGrouping.Tests.fs
```

Expected: no output except prose comments if a later worker added explanatory comments.

```powershell
git diff --check
```

Expected: no whitespace errors.
