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

module private ConnectorDom =

    [<Emit("Array.from($0.querySelectorAll($1))")>]
    let querySelectorAll (_container: HTMLElement) (_selector: string) : HTMLElement[] = jsNative

    let connectionNodes (container: HTMLElement) =
        querySelectorAll container "[data-provenance-connection-node]"
        |> Array.choose (fun node ->
            let id = node.getAttribute "data-provenance-connection-node"
            if isNull id then None else Some(id, node)
        )
        |> Map.ofArray

/// Measures connection handles and builds SVG connector paths between them.
module ConnectorMeasure =

    let createContext container = {
        Container = container
        Origin = container.getBoundingClientRect ()
        Nodes = ConnectorDom.connectionNodes container
    }

    let private tryHandle (context: ConnectorMeasureContext) handle =
        context.Nodes |> Map.tryFind (DragDrop.connectionHandleNodeId handle)

    let private center (context: ConnectorMeasureContext) (node: HTMLElement) =
        let origin = context.Origin
        let rect = node.getBoundingClientRect ()

        {
            X = rect.left - origin.left + float context.Container.scrollLeft + rect.width / 2.
            Y = rect.top - origin.top + float context.Container.scrollTop + rect.height / 2.
        }

    let pathBetweenPoints start finish =
        let deltaX = finish.X - start.X
        let direction = if deltaX >= 0. then 1. else -1.
        // The bend scales with the horizontal span, so connectors between nearby
        // endpoints stay short stubs instead of looping past their targets.
        let bend = max 8. (abs deltaX / 2.)
        let firstControlX = start.X + direction * bend
        let secondControlX = finish.X - direction * bend
        Some $"M {start.X} {start.Y} C {firstControlX} {start.Y}, {secondControlX} {finish.Y}, {finish.X} {finish.Y}"

    let pathBetweenHandles context source target =
        match tryHandle context source, tryHandle context target with
        | Some sourceNode, Some targetNode -> pathBetweenPoints (center context sourceNode) (center context targetNode)
        | _ -> None

    /// Rail connectors shorter than this are skipped entirely so dense layouts do not
    /// fill the rail gutters with overlapping stubs.
    let minimumConnectorDistance = 24.

    let private distanceBetween start finish =
        let deltaX = finish.X - start.X
        let deltaY = finish.Y - start.Y
        sqrt (deltaX * deltaX + deltaY * deltaY)

    let pathBetweenDistantHandles context source target =
        match tryHandle context source, tryHandle context target with
        | Some sourceNode, Some targetNode ->
            let start = center context sourceNode
            let finish = center context targetNode

            if distanceBetween start finish < minimumConnectorDistance then
                None
            else
                pathBetweenPoints start finish
        | _ -> None

/// Builds handle references used by overlay measurements.
module ConnectorHandles =

    let group side groupId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.GroupCard
        Side = side
        Id = groupId
        ParentGroupId = None
    }

    let member' side groupId setId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.GroupMember
        Side = side
        Id = setId
        ParentGroupId = Some groupId
    }

    let memberPropertyAnchor side groupId setId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.GroupMemberPropertyAnchor
        Side = side
        Id = setId
        ParentGroupId = Some groupId
    }

    let propertyHeader side header : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.PropertyHeader
        Side = side
        Id = DragDrop.propertyHeaderIdentity header
        ParentGroupId = None
    }

    let propertyValue side propertyValueId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.PropertyValue
        Side = side
        Id = propertyValueId
        ParentGroupId = None
    }

    /// Measurement-only anchor on the property-facing card edge; rail connectors end here
    /// instead of at the draggable group handle on the opposite edge.
    let propertyAnchor side groupId : ConnectionHandleRef = {
        Kind = ConnectionHandleKind.GroupPropertyAnchor
        Side = side
        Id = groupId
        ParentGroupId = None
    }
