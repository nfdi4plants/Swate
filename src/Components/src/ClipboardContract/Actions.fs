module Swate.Components.ClipboardContract.Actions

open Swate.Components.ClipboardContract.Types

let cut (content: ClipboardContent) (clearSource: unit -> unit) = promise {
    do! Swate.Components.ClipboardCodec.write content.PlainText content.Cells
    clearSource ()
}
