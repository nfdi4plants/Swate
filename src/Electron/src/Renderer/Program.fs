module Renderer.Main

open Browser.Dom
open Feliz

Fable.Core.JsInterop.importSideEffects "./tailwind.css"

let root = ReactDOM.createRoot (document.getElementById "root")
root.render (Renderer.App.Main())