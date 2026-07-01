module Swate.Components.Composite.Workspace.Helper.PaneState

open Swate.Components.Composite.Workspace.Types

type WorkspacePaneState with

    static member addTab(state: WorkspacePaneState) (tab: WorkspaceTab) : WorkspacePaneState =
        let newTabs = Array.append state.tabs [| tab |]
        let newOrder = Array.append state.tabOrder [| tab.Id |]
        { state with
            tabs = newTabs
            tabOrder = newOrder
            activeTabId = Some tab.Id
        }

    static member removeTab(state: WorkspacePaneState) (tabId: string) : WorkspacePaneState =
        let newTabs = state.tabs |> Array.filter (fun t -> t.Id <> tabId)
        let newOrder = state.tabOrder |> Array.filter (fun id -> id <> tabId)
        let newActive =
            if state.activeTabId = Some tabId then
                match newOrder with
                | [||] -> None
                | _ ->
                    let oldIndex = state.tabOrder |> Array.tryFindIndex (fun id -> id = tabId)
                    match oldIndex with
                    | Some idx ->
                        let newIdx = min idx (newOrder.Length - 1)
                        Some newOrder.[newIdx]
                    | None -> newOrder |> Array.tryLast
            else
                state.activeTabId
        { state with
            tabs = newTabs
            tabOrder = newOrder
            activeTabId = newActive
        }

    static member removeAllExcept(state: WorkspacePaneState) (tabId: string) : WorkspacePaneState =
        let newTabs = state.tabs |> Array.filter (fun t -> t.Id = tabId)
        let newOrder = [| tabId |]
        { state with
            tabs = newTabs
            tabOrder = newOrder
            activeTabId = Some tabId
        }

    static member removeAll(state: WorkspacePaneState) : WorkspacePaneState =
        { state with
            tabs = [||]
            tabOrder = [||]
            activeTabId = None
        }

    static member reorderTab(state: WorkspacePaneState) (fromIndex: int) (toIndex: int) : WorkspacePaneState =
        let newOrder =
            Swate.Components.JsBindings.DndKit.arrayMove(
                ResizeArray(state.tabOrder),
                fromIndex,
                toIndex
            )
            |> Array.ofSeq
        { state with tabOrder = newOrder }
