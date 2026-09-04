namespace Renderer.Components.LeftSidebar.Git

open Fable.Core
open Feliz
open Renderer.Components.LeftSidebar.Git.Types

[<Erase; Mangle(false)>]
type GitSidebarOpenArcEmptyState =

    [<ReactComponent(true)>]
    static member Main
        (arcOpening: Renderer.Context.ArcOpeningContext.ArcOpeningController, onDownloadArc: unit -> unit)
        =
        GitSidebarEmptyState.Main(
            title = "Open an ARC to use Git features",
            description = "Source control becomes available after you open or download an ARC.",
            iconClassName = "swt:fluent--folder-open-24-regular",
            primaryAction = {
                Label = "Open ARC"
                IconClassName = "swt:fluent--folder-open-24-regular"
                Disabled = arcOpening.isOpeningArc
                OnClick = arcOpening.openArc
            },
            secondaryAction = {
                Label = "Download ARC"
                IconClassName = "swt:fluent--cloud-arrow-down-24-regular"
                Disabled = false
                OnClick = onDownloadArc
            }
        )
