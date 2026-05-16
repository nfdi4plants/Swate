namespace Swate.Components.Metadata.FormComponents

open Fable.Core
open System
open Swate.Components
open Feliz

[<Erase; Mangle(false)>]
type DateTimeInput =

    [<ReactComponent>]
    static member DateTimeInput(inputValue: string, setter: string -> unit, ?label: string) =
        let inputRef = React.useInputRef ()
        let debouncedSetter = React.useDebouncedCallback setter

        React.useEffect (
            (fun () ->
                if inputRef.current.IsSome then
                    inputRef.current.Value.value <- inputValue
            ),
            [| box inputValue |]
        )

        Html.div [
            prop.className "swt:grow"
            prop.children [
                if label.IsSome then
                    LayoutComponents.FieldTitle label.Value
                Html.input [
                    prop.className "swt:input"
                    prop.type'.dateTimeLocal
                    prop.ref inputRef
                    prop.onChange (fun (dateValue: DateTime) ->
                        dateValue.ToString("yyyy-MM-ddTHH:mm") |> debouncedSetter
                    )
                ]
            ]
        ]

    [<ReactComponent>]
    static member DateTimeInput(inputValue: DateTime, setter: DateTime -> unit, ?label: string) =
        DateTimeInput.DateTimeInput(
            inputValue.ToString("yyyy-MM-ddTHH:mm"),
            (fun value -> DateTime.Parse(value) |> setter),
            ?label = label
        )
