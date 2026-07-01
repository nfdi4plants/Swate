namespace Swate.Components.Composite.Workspace

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.Workspace.Types
open Swate.Components.Composite.Workspace.Context

[<Erase; Mangle(false)>]
type PaneNode =

    [<ReactMemoComponent(AreEqualFn.FsEqualsButFunctions)>]
    static member PaneNode(pane: Pane, panePath: string, ?key: string) =
        let workspaceCtx = useWorkspaceCtx ()

        match pane with
        | Pane.Leaf paneId ->
            let paneState =
                workspaceCtx.panes
                |> Map.tryFind paneId
                |> Option.defaultValue {
                    tabs = [||]
                    tabOrder = [||]
                    activeTabId = None
                }

            let paneCtxValue: PaneCtxValue = {
                paneId = paneId
                tabs = paneState.tabs
                tabOrder = paneState.tabOrder
                activateTab = workspaceCtx.setActiveTabId << Some
                closeTab = workspaceCtx.closeTab
            }

            PaneCtx.Provider(
                paneCtxValue,
                Html.div [
                    prop.key (defaultArg key paneId)
                    prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:flex-1 swt:overflow-hidden"
                    if workspaceCtx.debug then
                        prop.testId $"workspace-pane-{paneId}"
                    prop.children [
                        TabBar.TabBar(paneId)
                        ContentArea.ContentArea(paneId)
                    ]
                ]
            )

        | Pane.Split(direction, first, second, ratio) ->
            let onRatioChange (newRatio: float) =
                let rec updateByPath (currentPath: string) (p: Pane) : Pane =
                    if currentPath = panePath then
                        match p with
                        | Pane.Split(d, f, s, _) -> Pane.Split(d, f, s, newRatio)
                        | _ -> p
                    else
                        match p with
                        | Pane.Split(d, f, s, r) ->
                            let newF = updateByPath (currentPath + "/first") f
                            let newS = updateByPath (currentPath + "/second") s
                            Pane.Split(d, newF, newS, r)
                        | _ -> p
                workspaceCtx.setLayout (updateByPath "" workspaceCtx.layout)

            let flexDir =
                match direction with
                | SplitDirection.Horizontal -> "swt:flex-row"
                | SplitDirection.Vertical -> "swt:flex-col"

            Html.div [
                prop.key (defaultArg key panePath)
                prop.className $"swt:flex {flexDir} swt:min-w-0 swt:min-h-0 swt:flex-1 swt:overflow-hidden"
                if workspaceCtx.debug then
                    prop.testId $"workspace-split-{panePath}"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:overflow-hidden"
                        prop.style [ style.custom ("flex", string (ratio * 100.0)) ]
                        prop.children [ PaneNode.PaneNode(first, panePath + "/first") ]
                    ]
                    SplitDivider.SplitDivider(direction, ratio, onRatioChange, panePath)
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:min-w-0 swt:min-h-0 swt:overflow-hidden"
                        prop.style [ style.custom ("flex", string ((1.0 - ratio) * 100.0)) ]
                        prop.children [ PaneNode.PaneNode(second, panePath + "/second") ]
                    ]
                ]
            ]
