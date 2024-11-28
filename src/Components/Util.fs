namespace Components

open Fable.Core
open Feliz


[<Import("ReactElement", "react")>]
type ReactElementType =
    interface end

/// https://fable.io/blog/2022/2022-10-12-react-jsx.html
[<AutoOpen>]
module Util =

    let inline toNative (el: ReactElement) : ReactElementType = unbox<ReactElementType> el
    let inline toReact (el: ReactElementType) : ReactElement = unbox el