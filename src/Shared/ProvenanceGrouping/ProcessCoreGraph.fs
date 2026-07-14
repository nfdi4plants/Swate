module internal Swate.Components.Shared.ProvenanceGrouping.ProcessCoreGraph

open System.Globalization
open System.Text
open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes

type DatasetEntry = { Path: string list; Dataset: Dataset }

/// Length-prefixed encoding for an optional string field so concatenated
/// fingerprint segments cannot collide across different field boundaries.
let private field (value: string option) =
    match value with
    | None -> "-1:"
    | Some text -> string text.Length + ":" + text

let datasetEntries (arc: ARC) : DatasetEntry list =
    let rec walk (path: string list) (dataset: Dataset) : DatasetEntry list =
        let currentPath = path @ [ dataset.Identifier ]

        {
            Path = currentPath
            Dataset = dataset
        }
        :: (dataset.HasPart |> Seq.toList |> List.collect (walk currentPath))

    walk [] (arc :> Dataset)

let resolveDatasetMatches (path: string list) (arc: ARC) : Dataset list =
    datasetEntries arc
    |> List.filter (fun entry -> entry.Path = path)
    |> List.map (fun entry -> entry.Dataset)

let tryResolveDataset (path: string list) (arc: ARC) : Dataset option =
    match resolveDatasetMatches path arc with
    | [ dataset ] -> Some dataset
    | _ -> None

let tryDatasetPath (dataset: Dataset) (arc: ARC) : string list option =
    datasetEntries arc
    |> List.tryFind (fun entry -> obj.ReferenceEquals(entry.Dataset, dataset))
    |> Option.map (fun entry -> entry.Path)

let processLocation (datasetPath: string list) (index: int) (proc: Process) : ProcessCoreProcessLocation = {
    DatasetPath = datasetPath
    ProcessIndex = index
    ExpectedName = proc.Name
}

let tryResolveProcess (location: ProcessCoreProcessLocation) (arc: ARC) : Process option =
    tryResolveDataset location.DatasetPath arc
    |> Option.bind (fun dataset ->
        if location.ProcessIndex >= 0 && location.ProcessIndex < dataset.Processes.Count then
            let proc = dataset.Processes.[location.ProcessIndex]

            if proc.Name = location.ExpectedName then
                Some proc
            else
                None
        else
            None
    )

let annotationFingerprint (annotation: Annotation) : ProcessCoreAnnotationFingerprint = {
    Name = annotation.Name
    Value = annotation.Value
    Unit = annotation.Unit
    NameTAN = annotation.NameTAN
    ValueTAN = annotation.ValueTAN
    UnitTAN = annotation.UnitTAN
    AdditionalType = annotation.AdditionalType
}

/// Mirrors ProcessCore's own `Annotation.Equals` (Name, Value, Unit, NameTAN).
/// Used only to detect public-API deduplication collisions, never as a
/// substitute for the full fingerprint when deciding round-trip identity.
let annotationsEqualByProcessCoreKey (left: Annotation) (right: Annotation) : bool =
    left.Name = right.Name
    && left.Value = right.Value
    && left.Unit = right.Unit
    && left.NameTAN = right.NameTAN

let private appendAnnotation (sb: StringBuilder) (annotation: Annotation) =
    sb.Append(field (Some annotation.Name)) |> ignore
    sb.Append(field annotation.Value) |> ignore
    sb.Append(field annotation.Unit) |> ignore
    sb.Append(field annotation.NameTAN) |> ignore
    sb.Append(field annotation.ValueTAN) |> ignore
    sb.Append(field annotation.UnitTAN) |> ignore
    sb.Append(field annotation.AdditionalType) |> ignore

let private nodeAdditionalType (node: IONode) =
    match node with
    | SampleNode sample -> sample.AdditionalType
    | DataNode data -> data.AdditionalType

let nodeAdditionalProperties (node: IONode) : Annotation seq =
    match node with
    | SampleNode sample -> sample.AdditionalProperty :> seq<Annotation>
    | DataNode data -> data.AdditionalProperty :> seq<Annotation>

/// Canonical, Fable-friendly, length-prefixed encoding of the reachable
/// graph state used for round-trip and staleness detection. Deliberately not
/// `GetHashCode()`, which is unstable across runs and does not distinguish
/// content changes from unrelated objects.
let graphFingerprint (arc: ARC) : string =
    let sb = StringBuilder()

    for entry in datasetEntries arc do
        sb.Append(field (Some(String.concat "/" entry.Path))) |> ignore

        for index in 0 .. entry.Dataset.Processes.Count - 1 do
            let proc = entry.Dataset.Processes.[index]
            sb.Append(field (Some(string index))) |> ignore
            sb.Append(field (Some proc.Name)) |> ignore
            sb.Append(field proc.AdditionalType) |> ignore

            for node in Seq.append proc.Inputs proc.Outputs do
                let kind =
                    match node with
                    | SampleNode _ -> "S"
                    | DataNode _ -> "D"

                sb.Append(field (Some kind)) |> ignore
                sb.Append(field (Some(node.Key()))) |> ignore
                sb.Append(field (nodeAdditionalType node)) |> ignore

                for annotation in nodeAdditionalProperties node do
                    appendAnnotation sb annotation

            for parameterValue in proc.ParameterValue do
                appendAnnotation sb parameterValue

            match proc.ExecutesProtocol with
            | Some recipe ->
                sb.Append(field recipe.Name) |> ignore
                sb.Append(field recipe.Version) |> ignore
                sb.Append(field recipe.Description) |> ignore
                sb.Append(field recipe.Url) |> ignore

                for recipeComponent in recipe.Components do
                    appendAnnotation sb recipeComponent
            | None -> sb.Append("-1:") |> ignore

    sb.ToString()

let nodeLocation (node: IONode) : ProcessCoreNodeLocation =
    match node with
    | SampleNode _ -> {
        Kind = ProcessCoreNodeKind.Sample
        Key = node.Key()
      }
    | DataNode _ -> {
        Kind = ProcessCoreNodeKind.Data
        Key = node.Key()
      }

let nodeDisplayName (node: IONode) =
    match node with
    | SampleNode sample -> sample.Name
    | DataNode data -> data.Name

/// `ValueTAN` present means the value is ontology-backed. ProcessCore has no
/// separate ontology-source field, so converted terms always use
/// `TermSource = None`; writeback stores only the TAN.
let valueFromAnnotation (annotation: Annotation) : ProvenanceValue =
    match annotation.ValueTAN with
    | Some accession ->
        ProvenanceValue.Term {
            Name = annotation.ValueText
            TermSource = None
            TermAccession = Some accession
        }
    | None -> ProvenanceValue.Text annotation.ValueText

let unitFromAnnotation (annotation: Annotation) : ProvenanceTerm option =
    match annotation.Unit with
    | Some unitText ->
        Some {
            Name = unitText
            TermSource = None
            TermAccession = annotation.UnitTAN
        }
    | None -> None

let sourceRef (location: ProcessCoreTableLocation) : ProvenanceSourceRef = {
    Id = String.concat "/" (location.DatasetPath @ [ location.TableName ])
    Name = location.TableName
}

let processId (location: ProcessCoreProcessLocation) : ProvenanceProcessId =
    String.concat "/" (location.DatasetPath @ [ string location.ProcessIndex; location.ExpectedName ])

let tryResolveNode (location: ProcessCoreNodeLocation) (arc: ARC) : IONode option =
    arc.AllNodes() |> Seq.tryFind (fun node -> node.Key() = location.Key)

let tryResolveAnnotation (location: ProcessCoreAnnotationLocation) (arc: ARC) : Annotation option =
    let atPosition (position: int) (annotations: Annotation seq) =
        let list = annotations |> Seq.toList

        if position >= 0 && position < list.Length then
            Some list.[position]
        else
            None

    match location.Owner with
    | ProcessCoreAnnotationOwner.NodeAdditionalProperty nodeLocation ->
        tryResolveNode nodeLocation arc
        |> Option.bind (fun node -> nodeAdditionalProperties node |> atPosition location.Position)
    | ProcessCoreAnnotationOwner.ProcessParameterValue procLocation ->
        tryResolveProcess procLocation arc
        |> Option.bind (fun proc -> proc.ParameterValue :> Annotation seq |> atPosition location.Position)
    | ProcessCoreAnnotationOwner.RecipeComponent procLocation ->
        tryResolveProcess procLocation arc
        |> Option.bind (fun proc -> proc.ExecutesProtocol)
        |> Option.bind (fun recipe -> recipe.Components :> Annotation seq |> atPosition location.Position)

/// Mutates only `Value`/`ValueTAN`/`Unit`/`UnitTAN`. Category (`Name`/`NameTAN`)
/// is set once at annotation creation and is never changed by a value update.
let applyValue (value: ProvenanceValue) (unit: ProvenanceTerm option) (annotation: Annotation) : unit =
    match value with
    | ProvenanceValue.Text text ->
        annotation.Value <- Some text
        annotation.ValueTAN <- None
    | ProvenanceValue.Integer intValue ->
        annotation.Value <- Some(intValue.ToString(CultureInfo.InvariantCulture))
        annotation.ValueTAN <- None
    | ProvenanceValue.Float floatValue ->
        annotation.Value <- Some(floatValue.ToString("R", CultureInfo.InvariantCulture))
        annotation.ValueTAN <- None
    | ProvenanceValue.Term term ->
        annotation.Value <- Some term.Name
        annotation.ValueTAN <- term.TermAccession

    match unit with
    | Some unitTerm ->
        annotation.Unit <- Some unitTerm.Name
        annotation.UnitTAN <- unitTerm.TermAccession
    | None ->
        annotation.Unit <- None
        annotation.UnitTAN <- None

/// Creates a brand-new annotation for a value/unit created in the editor.
/// `additionalType` carries the ProcessCore discriminator (e.g.
/// `CharacteristicValue`, `ParameterValue`, `Component`); `None` leaves it
/// unset for the generic node-annotation kind.
let annotationFromValue
    (additionalType: string option)
    (header: ProvenancePropertyHeader)
    (value: ProvenanceValue)
    (unit: ProvenanceTerm option)
    : Annotation =
    let annotation =
        Annotation(header.Category.Name, ?nameTAN = header.Category.TermAccession, ?additionalType = additionalType)

    applyValue value unit annotation
    annotation

// ── Graph mutation primitives ───────────────────────────────────────────────

/// Builds a fresh `Sample`/`Data` node from a set's final editor name.
/// ProcessCore canonicalizes by key when the node is later added to a
/// process via `AddInput`/`AddOutput`, so a freshly constructed node
/// converges onto any already-registered node with the same key.
let nodeFromSet (set: ProvenanceSet) : Result<IONode, ProcessCoreWritebackError> =
    let additionalType =
        if
            System.String.IsNullOrWhiteSpace set.Header.Text
            || set.Header.Text = set.Header.Kind.Label
        then
            None
        else
            Some set.Header.Text

    if set.Header.Kind.Id = ProcessCoreKinds.sampleEndpoint.Id then
        Ok(SampleNode(Sample(set.Name, ?additionalType = additionalType)))
    elif set.Header.Kind.Id = ProcessCoreKinds.dataEndpoint.Id then
        let path, selector =
            match set.Name.LastIndexOf '#' with
            | -1 -> set.Name, None
            | index -> set.Name.Substring(0, index), Some(set.Name.Substring(index + 1))

        Ok(DataNode(Data(path, ?selector = selector, ?additionalType = additionalType)))
    else
        Error(ProcessCoreWritebackError.UnsupportedEndpointKind set.Header.Kind.Id)

/// A distinct container so a connection-targeted component created on one
/// split row never leaks into sibling rows, while sharing the same
/// pre-existing component/parameter annotation references keeps updates to
/// those pre-existing values reaching every cloned occurrence.
let cloneRecipeShell (recipe: Recipe) : Recipe =
    let clone =
        Recipe(
            ?name = recipe.Name,
            ?description = recipe.Description,
            ?version = recipe.Version,
            ?url = recipe.Url,
            ?intendedUse = recipe.IntendedUse,
            ?additionalType = recipe.AdditionalType
        )

    for formalParameter in recipe.Parameters do
        clone.AddParameter formalParameter

    for recipeComponent in recipe.Components do
        clone.AddComponent recipeComponent

    for additionalProperty in recipe.AdditionalProperty do
        clone.AddAdditionalProperty additionalProperty

    clone

let cloneProcessShell (proc: Process) : Process =
    let clone = Process(proc.Name, ?additionalType = proc.AdditionalType)

    match proc.ExecutesProtocol with
    | Some recipe -> clone.ExecutesProtocol <- Some(cloneRecipeShell recipe)
    | None -> ()

    for parameterValue in proc.ParameterValue do
        clone.AddParameterValue parameterValue

    clone

/// Never edits the backing input/output arrays directly; the public
/// `RemoveInput`/`RemoveOutput`/`AddInput`/`AddOutput` APIs maintain
/// back-edges and canonicalization.
let replaceProcessIO (inputs: IONode list) (outputs: IONode list) (proc: Process) : unit =
    for node in proc.Inputs |> Seq.toList do
        proc.RemoveInput node

    for node in proc.Outputs |> Seq.toList do
        proc.RemoveOutput node

    for node in inputs do
        proc.AddInput node

    for node in outputs do
        proc.AddOutput node

let addProcess (dataset: Dataset) (proc: Process) : unit = dataset.AddProcess proc

let removeProcess (dataset: Dataset) (proc: Process) : unit = dataset.RemoveProcess proc
