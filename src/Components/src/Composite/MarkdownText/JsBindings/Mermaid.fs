namespace Swate.Components.Composite.MarkdownText.JsBindings

open Fable.Core

[<AllowNullLiteral>]
type IMermaidRenderResult =
    abstract member svg: string
    abstract member bindFunctions: obj

[<AllowNullLiteral>]
type IMermaid =
    abstract member initialize: obj -> unit
    abstract member render: string * string -> JS.Promise<IMermaidRenderResult>

[<Erase>]
type Mermaid =

    [<ImportDefault("mermaid")>]
    static member instance: IMermaid = jsNative
