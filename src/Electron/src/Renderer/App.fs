module Renderer.App

open Feliz

let inline jsx ([<System.Diagnostics.CodeAnalysis.StringSyntax "jsx">] jsx: string) = Feliz.JSX.Html.jsx jsx

[<ReactComponent>]
let Main () =
    let counter, setCounter = React.useState (0)

    jsx
        $"""
    <div>
        <div>{counter}</div>
        <button className="btn" onClick={fun () -> setCounter (counter + 1)}>+</button>
        <button className="btn" onClick={fun () -> setCounter (counter - 1)}>-</button>
    </div>
"""