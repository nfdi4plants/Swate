namespace Swate.Components

open ARCtrl
open Swate.Components
open Swate.Components.Shared

[<RequireQualifiedAccess>]
type ActiveView =
    | Table of index: int
    | DataMap
    | Metadata

    member this.ViewIndex =
        match this with
        | Table i -> i
        | DataMap -> -1
        | Metadata -> -2

    member this.TryTableIndex =
        match this with
        | Table i -> Some i
        | _ -> None

    member this.ToWidgetHostView() =
        match this with
        | Table _ -> WidgetHostView.TableView
        | DataMap -> WidgetHostView.DataMapView
        | Metadata -> WidgetHostView.MetadataView

    member this.IsAvailableOn(arcFile: ArcFiles) =
        match this with
        | Table i -> i >= 0 && i < arcFile.Tables().Count
        | DataMap -> arcFile.CanRenderDataMapView()
        | Metadata -> arcFile.HasMetadata()

    static member Normalize(arcFile: ArcFiles, current: ActiveView) =
        if current.IsAvailableOn(arcFile) then
            current
        elif arcFile.Tables().Count > 0 then
            ActiveView.Table 0
        elif arcFile.CanRenderDataMapView() then
            ActiveView.DataMap
        else
            ActiveView.Metadata

[<AutoOpen>]
module ActivePattern =

    let (|IsTable|IsDataMap|IsMetadata|) (input: ActiveView) =
        match input with
        | ActiveView.Table _ -> IsTable
        | ActiveView.DataMap -> IsDataMap
        | ActiveView.Metadata -> IsMetadata
