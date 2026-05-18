namespace Swate.Components.Composite.MarkdownText

[<RequireQualifiedAccess>]
type PreviewMode =
    | Live
    | Edit
    | Preview

type MarkdownOptions =
    {
        Height: int
        Mode: PreviewMode
        PreviewClassName: string option
    }

[<RequireQualifiedAccess>]
module MarkdownOptions =

    let defaults =
        {
            Height = 360
            Mode = PreviewMode.Live
            PreviewClassName = None
        }
