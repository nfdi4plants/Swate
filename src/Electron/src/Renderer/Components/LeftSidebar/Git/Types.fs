module Renderer.Components.LeftSidebar.Git.Types

type EmptyStateAction = {
    Label: string
    IconClassName: string
    Disabled: bool
    OnClick: unit -> unit
}
