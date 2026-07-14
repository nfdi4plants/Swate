namespace Swate.Components.Primitive.Dialog

open Feliz
open Fable.Core
open Swate.Components.Primitive.BaseModal

[<Erase; Mangle(false)>]
type Dialog =

    [<ReactComponent>]
    static member Dialog
        (
            isOpen: bool,
            setIsOpen: bool -> unit,
            header: ReactElement,
            children: ReactElement,
            ?description: ReactElement,
            ?footer: ReactElement,
            ?debug: string,
            ?className: string
        ) =
        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = header,
            children = children,
            ?description = description,
            ?footer = footer,
            ?debug = debug,
            ?className = className
        )

    [<ReactComponent>]
    static member StringSubmissionDialog
        (
            isOpen: bool,
            title: string,
            description: string,
            fieldLabel: string,
            initialValue: string,
            close: unit -> unit,
            submit: string -> JS.Promise<unit>,
            validate: string -> Result<string, string>,
            submitLabel: string,
            validationMessage: string,
            ?busyLabel: string,
            ?debug: string
        ) =

        let value, setValue = React.useState initialValue
        let isRunning, setIsRunning = React.useState false
        let busyLabel = defaultArg busyLabel submitLabel
        let isDisabled = isRunning

        React.useEffect ((fun () -> setValue initialValue), [| box initialValue; box isOpen; box title |])

        let setIsOpen isOpen =
            if not isOpen && not isDisabled then
                close ()

        let validationResult = validate value
        let isValid = validationResult |> Result.isOk

        let submitIfValid () =
            match validationResult with
            | Ok normalizedValue when not isRunning ->
                promise {
                    setIsRunning true

                    try
                        do! submit normalizedValue
                    finally
                        setIsRunning false
                }
                |> Promise.start
            | _ -> ()

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.disabled isDisabled
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.disabled ((not isValid) || isDisabled)
                        prop.onClick (fun _ -> submitIfValid ())
                        prop.text (if isRunning then busyLabel else submitLabel)
                    ]
                ]
            ]

        let content =
            Html.fieldSet [
                prop.className "swt:fieldset"
                prop.children [
                    Html.legend [
                        prop.className "swt:fieldset-legend"
                        prop.text fieldLabel
                    ]
                    Html.label [
                        prop.className "swt:input swt:w-full"
                        prop.children [
                            Html.input [
                                prop.autoFocus true
                                prop.disabled isDisabled
                                prop.value value
                                prop.onChange setValue
                                prop.onKeyDown (key.enter, fun _ -> submitIfValid ())
                            ]
                        ]
                    ]
                    Html.p [
                        prop.hidden isValid
                        prop.className "swt:text-error swt:text-sm"
                        prop.text validationMessage
                    ]
                ]
            ]

        Dialog.Dialog(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text title,
            description = Html.text description,
            children = content,
            footer = footer,
            ?debug = debug
        )
