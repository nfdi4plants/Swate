namespace Swate.Components.MarkdownText

[<RequireQualifiedAccess>]
type PreviewMode =
    | Live
    | Edit
    | Preview

[<RequireQualifiedAccess>]
module PreviewMode =

    let toEditorValue = function
        | PreviewMode.Live -> "live"
        | PreviewMode.Edit -> "edit"
        | PreviewMode.Preview -> "preview"

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
