namespace Swate.Components.Page.ProvenanceGrouping

open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Grouping
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Primitive.ContextMenu
open Swate.Components.Primitive.ContextMenu.Types
open Swate.Components.Page.ProvenanceGrouping.Types

type MeasuredConnector = {
    Key: string
    Path: string
    TestId: string
    ClassName: string
    StrokeWidth: float
    StrokeDasharray: string option
    InteractiveConnection: DisplayConnection option
    AriaLabel: string option
    Color: string option
    /// Curve midpoint, measured so summary badges (e.g. underlying-connection
    /// counts) can sit on the line without re-deriving the bezier.
    Midpoint: ConnectionPoint option
}

/// Logical connector between two handles, derived purely from model/UI state.
/// Measuring specs against the DOM produces MeasuredConnector values; keeping the
/// derivation out of the measure path lets observers remeasure without re-scanning
/// the model.
type ConnectorSpec = {
    Key: string
    TestId: string
    ClassName: string
    StrokeWidth: float
    StrokeDasharray: string option
    InteractiveConnection: DisplayConnection option
    AriaLabel: string option
    Color: string option
    Source: ConnectionHandleRef
    Target: ConnectionHandleRef
    /// Rail connectors drop out entirely when their endpoints are too close.
    SkipWhenClose: bool
}

type ConnectorOverlayState = {
    ExpandedGroup: (ProvenanceSide * string) option
    SelectedConnectionId: string option
    ExpandedProperties: Set<ProvenanceLayerId * ProvenanceSide * GroupingKey>
}

module ConnectorOverlayState =

    let fromUiState (uiState: UiState) =
        let selectedConnectionId =
            match uiState.Detail with
            | Some(ProvenanceDetail.Connection connectionId) -> Some connectionId
            | _ -> None

        {
            ExpandedGroup = uiState.ExpandedGroup
            SelectedConnectionId = selectedConnectionId
            ExpandedProperties = uiState.ExpandedProperties
        }

    let isGroupExpanded side groupId state =
        state.ExpandedGroup = Some(side, groupId)

    let isPropertyExpanded layerId side header state =
        state.ExpandedProperties.Contains(layerId, side, { Header = header })

type ConnectorMeasureContext = {
    Container: HTMLElement
    Origin: ClientRect
    Nodes: Map<string, HTMLElement>
}
