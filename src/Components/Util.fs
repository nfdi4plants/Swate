namespace Components

open Fable.Core
open Feliz

/// https://fable.io/blog/2022/2022-10-12-react-jsx.html
[<AutoOpen>]
module Util =

    let inline toJsx (el: ReactElement) : JSX.Element = unbox el
    let inline toReact (el: JSX.Element) : ReactElement = unbox el

    /// Enables use of Feliz styles within a JSX hole
    let inline toStyle (styles: IStyleAttribute list) : obj = JsInterop.createObj (unbox styles)
    let toClass (classes: (string * bool) list) : string =
        classes
        |> List.choose (fun (c, b) ->
            match c.Trim(), b with
            | "", _
            | _, false -> None
            | c, true -> Some c)
        |> String.concat " "