module ARCitISA.Components.ClipboardContract.Actions

open ARCitISA.Components.ClipboardContract.Types

let cut (content: ClipboardContent) (clearSource: unit -> unit) = promise {
    do! ARCitISA.Components.ClipboardCodec.write content.PlainText content.Cells
    clearSource ()
}
