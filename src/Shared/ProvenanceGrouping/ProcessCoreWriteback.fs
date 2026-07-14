module Swate.Components.Shared.ProvenanceGrouping.ProcessCoreWriteback

open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Edit
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreGraph

type private ExistingAnnotationUpdate = {
    PropertyValueId: ProvenancePropertyValueId
    Annotations: Annotation list
    Value: ProvenanceValue
    Unit: ProvenanceTerm option
}

type private Plan = {
    Updates: ExistingAnnotationUpdate list
}

let private anchorOfOrigin =
    function
    | ProvenancePropertyOrigin.Real anchor
    | ProvenancePropertyOrigin.Virtual anchor -> anchor

let private findPropertyValue (session: ProvenanceSession) (propertyValueId: ProvenancePropertyValueId) =
    session.Layers
    |> List.tryPick (fun layer -> layer.Model.PropertyValues.TryFind propertyValueId)

let private validateGraph (index: ProcessCoreWritebackIndex) (arc: ARC) : ProcessCoreWritebackError list =
    if graphFingerprint arc <> index.ArcFingerprint then
        [ ProcessCoreWritebackError.StaleArc ]
    else
        []

let private validateLayers
    (index: ProcessCoreWritebackIndex)
    (session: ProvenanceSession)
    : ProcessCoreWritebackError list =
    let hasInitialLayer =
        session.Layers
        |> List.exists (fun layer -> layer.Model.Source.Id = index.InitialSourceId)

    let layerIds = session.Layers |> List.map (fun layer -> layer.Id) |> List.sort
    let orderIds = session.LayerOrder |> List.sort

    [
        if not hasInitialLayer then
            yield ProcessCoreWritebackError.InitialLayerNotFound index.InitialSourceId
        if
            layerIds <> orderIds
            || (session.LayerOrder |> List.distinct |> List.length)
               <> session.LayerOrder.Length
        then
            yield ProcessCoreWritebackError.InvalidLayerOrder session.LayerOrder
    ]

/// Resolves one `UpdatePropertyValue` patch. A property absent from the
/// conversion index but present in the final session is editor-created
/// (`Virtual`) in this session; its value update is absorbed here because
/// its owning `AddLoadedPropertyValue` materialization (Task 7) always
/// writes the property's final session value, not the add-patch payload.
let private resolveUpdatePatch
    (index: ProcessCoreWritebackIndex)
    (session: ProvenanceSession)
    (arc: ARC)
    (propertyValueId: ProvenancePropertyValueId)
    (patchAnchor: ProvenanceWritebackAnchor)
    : Result<ExistingAnnotationUpdate option, ProcessCoreWritebackError list> =
    match index.PropertyValueLocations.TryFind propertyValueId with
    | None ->
        match findPropertyValue session propertyValueId with
        | None ->
            Error [
                ProcessCoreWritebackError.PropertyNotFound propertyValueId
            ]
        | Some _ -> Ok None
    | Some locations ->
        match findPropertyValue session propertyValueId with
        | None ->
            Error [
                ProcessCoreWritebackError.PropertyNotFound propertyValueId
            ]
        | Some finalValue ->
            let finalAnchor = anchorOfOrigin finalValue.Origin

            if finalAnchor.Source.Id <> patchAnchor.Source.Id then
                Error [
                    ProcessCoreWritebackError.SourceLocationNotFound propertyValueId
                ]
            else
                let resolutions =
                    locations
                    |> List.map (fun location ->
                        match tryResolveAnnotation location arc with
                        | Some annotation when annotationFingerprint annotation = location.Fingerprint -> Ok annotation
                        | Some _ -> Error(ProcessCoreWritebackError.SourceLocationNotFound propertyValueId)
                        | None -> Error(ProcessCoreWritebackError.SourceLocationNotFound propertyValueId)
                    )

                let errors =
                    resolutions
                    |> List.choose (
                        function
                        | Error e -> Some e
                        | Ok _ -> None
                    )

                if not errors.IsEmpty then
                    Error(errors |> List.distinct)
                else
                    let annotations =
                        resolutions
                        |> List.choose (
                            function
                            | Ok a -> Some a
                            | Error _ -> None
                        )

                    Ok(
                        Some {
                            PropertyValueId = propertyValueId
                            Annotations = annotations
                            Value = finalValue.Value
                            Unit = finalValue.Unit
                        }
                    )

let private preflight
    (index: ProcessCoreWritebackIndex)
    (session: ProvenanceSession)
    (arc: ARC)
    : Result<Plan, ProcessCoreWritebackError list> =
    let structuralErrors = validateGraph index arc @ validateLayers index session

    if not structuralErrors.IsEmpty then
        Error structuralErrors
    else
        let results =
            session.PatchLog
            |> List.map (fun patch ->
                match patch with
                | ProvenanceTablePatch.UpdatePropertyValue(propertyValueId, anchor, _, _, _) ->
                    resolveUpdatePatch index session arc propertyValueId anchor
                | other ->
                    Error [
                        ProcessCoreWritebackError.InvalidPatchTarget(sprintf "%A" other)
                    ]
            )

        let errors =
            results
            |> List.collect (
                function
                | Error e -> e
                | Ok _ -> []
            )

        if not errors.IsEmpty then
            Error(errors |> List.distinct)
        else
            let updates =
                results
                |> List.choose (
                    function
                    | Ok(Some update) -> Some update
                    | _ -> None
                )
                |> List.distinctBy (fun update -> update.PropertyValueId)

            Ok { Updates = updates }

let private apply (arc: ARC) (plan: Plan) : ProcessCoreWritebackSummary =
    let touchedAnnotations =
        System.Collections.Generic.HashSet<Annotation>(HashIdentity.Reference)

    for update in plan.Updates do
        for annotation in update.Annotations do
            applyValue update.Value update.Unit annotation
            touchedAnnotations.Add annotation |> ignore

    {
        UpdatedAnnotations = touchedAnnotations.Count
        AddedAnnotations = 0
        AddedNodes = 0
        AddedProcesses = 0
        RemovedProcesses = 0
    }

let writeBack
    (index: ProcessCoreWritebackIndex)
    (session: ProvenanceSession)
    (arc: ARC)
    : Result<ProcessCoreWritebackSummary, ProcessCoreWritebackError list> =
    preflight index session arc |> Result.map (apply arc)
