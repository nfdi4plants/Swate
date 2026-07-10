module Swate.Components.Composite.Workspace.Helper.DropTarget

open Browser.Types
open Swate.Components.Composite.Workspace.Types

type DropTarget =
    | TabBarDrop of paneId: string
    | EdgeDrop of paneId: string * direction: EdgeDirection

let resolveDropTarget
    (element: HTMLElement)
    (pointerX: float)
    (pointerY: float)
    (workspaceEl: HTMLElement)
    (sourcePaneId: string)
    : DropTarget option
    =

    let mutable current : HTMLElement option = Some element
    let mutable targetPaneId : string option = None
    let mutable isTabBar = false
    let mutable paneElement : HTMLElement option = None

    while current.IsSome do
        let el = current.Value

        if obj.ReferenceEquals(el, workspaceEl) then
            current <- None
        else
            let paneAttr = el.getAttribute "data-workspace-pane"

            if not (isNull paneAttr) && targetPaneId.IsNone then
                targetPaneId <- Some paneAttr
                paneElement <- Some el

            let tabBarAttr = el.getAttribute "data-workspace-tabbar"

            if not (isNull tabBarAttr) then
                isTabBar <- true

            current <- Option.ofObj el.parentElement

    match targetPaneId with
    | None -> None
    | Some paneId when isTabBar && paneId = sourcePaneId -> None
    | Some paneId when isTabBar -> Some(TabBarDrop paneId)
    | Some paneId ->
        let paneEl =
            Option.orElseWith
                (fun () ->
                    let found = workspaceEl.querySelector ($"[data-workspace-pane=\"{paneId}\"]")

                    match found with
                    | :? HTMLElement as h -> Some h
                    | _ -> None
                )
                paneElement

        match paneEl with
        | Some el ->
            let rect = el.getBoundingClientRect()

            let relX = (pointerX - rect.left) / rect.width
            let relY = (pointerY - rect.top) / rect.height

            let distances =
                [ EdgeDirection.Top, relY
                  EdgeDirection.Bottom, 1.0 - relY
                  EdgeDirection.Left, relX
                  EdgeDirection.Right, 1.0 - relX ]

            let closest = distances |> List.minBy snd
            Some(EdgeDrop(paneId, fst closest))

        | None -> None
