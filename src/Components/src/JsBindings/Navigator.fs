namespace Swate.Components

open Fable.Core


[<Erase>]
type Navigator =
    abstract member connection: obj option with get
    abstract member clipboard: Clipboard

[<AutoOpen>]
module GlobalBindings =

    [<Emit("navigator")>]
    let navigator: Navigator = jsNative

    [<Emit("$0")>]
    let resizeArrayAsArray<'T> (items: ResizeArray<'T>) : 'T[] = jsNative
