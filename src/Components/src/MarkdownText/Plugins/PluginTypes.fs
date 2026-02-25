namespace Swate.Components.MarkdownText.Plugins

open Swate.Components.MarkdownText.JsBindings

type MarkdownToolbarPlugin =
    {
        Id: string
        Command: ICommand
        Enabled: bool
    }
