namespace Swate.Components.MarkdownText.Plugins

open Swate.Components.MarkdownText.JsBindings

type MarkdownPromptPlugin =
    {
        Title: string
        Description: string option
        Placeholder: string
        SubmitButtonText: string
        Validate: string -> Result<unit, string>
        Apply: string -> int -> int -> string -> string * (int * int)
    }

type MarkdownToolbarPlugin =
    {
        Id: string
        Command: ICommand
        Enabled: bool
        Prompt: MarkdownPromptPlugin option
    }
