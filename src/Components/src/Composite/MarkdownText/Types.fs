module Swate.Components.Composite.MarkdownText.Types

[<RequireQualifiedAccess>]
type PreviewMode =
    | Live
    | Edit
    | Preview

type MarkdownOptions = {
    MinHeight: int
    MaxHeight: int
    Mode: PreviewMode
    PreviewClassName: string option
}

[<RequireQualifiedAccess>]
module MarkdownOptions =

    let defaults = {
        MinHeight = 240
        MaxHeight = 360
        Mode = PreviewMode.Live
        PreviewClassName = None
    }
