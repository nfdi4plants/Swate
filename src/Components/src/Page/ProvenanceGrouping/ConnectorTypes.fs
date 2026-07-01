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
