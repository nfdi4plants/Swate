module Swate.Components.JsBindings.Clipboard

open Fable.Core

[<Erase>]
type IClipboard =
    abstract member writeText: string -> JS.Promise<unit>
    abstract member readText: unit -> JS.Promise<string>

[<Erase>]
type INavigator =
    abstract member clipboard: IClipboard

[<RequireQualifiedAccess>]
module Clipboard =

    [<Emit("navigator")>]
    let navigator: INavigator = jsNative
