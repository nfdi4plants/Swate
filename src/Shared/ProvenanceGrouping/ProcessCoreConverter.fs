module Swate.Components.Shared.ProvenanceGrouping.ProcessCoreConverter

open ProcessCore
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreAdapterTypes
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreGraph

let fromArc
    (location: ProcessCoreTableLocation)
    (arc: ARC)
    : Result<ProcessCoreConversionResult, ProcessCoreConversionError> =
    if location.DatasetPath.IsEmpty then
        Error ProcessCoreConversionError.EmptyDatasetPath
    else
        match resolveDatasetMatches location.DatasetPath arc with
        | [] -> Error(ProcessCoreConversionError.DatasetNotFound location.DatasetPath)
        | _ :: _ :: _ -> Error(ProcessCoreConversionError.AmbiguousDatasetPath location.DatasetPath)
        | [ dataset ] ->
            let selected =
                dataset.Processes
                |> Seq.mapi (fun index proc -> index, proc)
                |> Seq.filter (fun (_, proc) -> proc.Name = location.TableName)
                |> Seq.toList

            if selected.IsEmpty then
                Error(ProcessCoreConversionError.ProcessGroupNotFound location)
            else
                let source = sourceRef location

                let model = {
                    Source = source
                    PropertyValues = Map.empty
                    InputSets = Map.empty
                    OutputSets = Map.empty
                    Connections = Map.empty
                }

                Ok {
                    Model = model
                    Index = {
                        LoadedTable = location
                        InitialSourceId = source.Id
                        ArcFingerprint = graphFingerprint arc
                        EndpointLocations = Map.empty
                        PropertyValueLocations = Map.empty
                        ConnectionLocations = Map.empty
                    }
                    Warnings = []
                }
