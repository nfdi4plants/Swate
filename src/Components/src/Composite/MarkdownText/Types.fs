module Swate.Components.Composite.MarkdownText.Types

[<RequireQualifiedAccess>]
type PreviewMode =
    | Live
    | Edit
    | Preview

type MarkdownOptions = {
    CreatedHeight: int
    EditorHeight: int
    Mode: PreviewMode
    PreviewClassName: string option
}

[<RequireQualifiedAccess>]
type MarkdownParent =
    | Editor
    | Created

[<RequireQualifiedAccess>]
module MarkdownOptions =
    let defaults = {
        CreatedHeight = 560
        EditorHeight = 360
        Mode = PreviewMode.Live
        PreviewClassName = None
    }
