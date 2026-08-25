namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz

[<Erase; Mangle(false)>]
type DraftTextField =

    [<ReactComponent>]
    static member DraftTextField
        (identity: string, value: string, onCommit: string -> unit, ?testId: string)
        : ReactElement =
        let draft, setDraft = React.useState value

        React.useEffect ((fun () -> setDraft value), [| box identity; box value |])

        Html.input [
            prop.key identity
            match testId with
            | Some testId -> prop.testId testId
            | None -> ()
            prop.className "swt:input swt:input-sm swt:w-full"
            prop.value draft
            prop.onChange setDraft
            prop.onBlur (fun _ -> onCommit draft)
        ]
