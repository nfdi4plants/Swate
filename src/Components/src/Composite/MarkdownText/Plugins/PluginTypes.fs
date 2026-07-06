namespace Swate.Components.Composite.MarkdownText.Plugins

open Browser.Types
open Fable.Core
open Swate.Components.Composite.MarkdownText.JsBindings

[<RequireQualifiedAccess>]
type MarkdownPromptInputMode =
    | Text
    | File

type MarkdownPromptFile = {
    Name: string
    MimeType: string option
    // Opaque runtime-specific identifier. This is not a filesystem path.
    SourceId: string option
    BrowserFile: File option
}

// AcceptTypes uses the same comma-separated format as an HTML file input accept attribute.
type MarkdownFilePickerOptions = {
    AcceptTypes: string option
    AllowMultiple: bool option
}

// Host/runtime extension point for dialog picking and markdown path resolution.
// When provided, file selection and drop handling are delegated to the adapter.
type MarkdownFilePickerAdapter = {
    PickFiles: MarkdownFilePickerOptions -> JS.Promise<MarkdownPromptFile list>
    ResolveMarkdownPath: MarkdownPromptFile -> JS.Promise<string>
}

type MarkdownPromptPlugin = {
    Title: string
    Description: string option
    Placeholder: string
    SubmitButtonText: string
    Validate: string -> Result<unit, string>
    Apply: string -> int -> int -> string -> string * (int * int)
    // None defaults to text prompt mode for backwards compatibility.
    InputMode: MarkdownPromptInputMode option
    // Applies to file mode only.
    Accept: string option
    AllowMultiple: bool option
    // Applies to file mode only.
    ApplyFiles: (string -> int -> int -> (MarkdownPromptFile * string) list -> string * (int * int)) option
}

type MarkdownToolbarPlugin = {
    Id: string
    Command: ICommand
    Enabled: bool
    Prompt: MarkdownPromptPlugin option
}
