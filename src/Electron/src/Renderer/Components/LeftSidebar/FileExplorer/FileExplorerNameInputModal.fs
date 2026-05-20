namespace Renderer.Components.LeftSidebar.FileExplorer

open Swate.Components.Primitive.BaseModal
open Feliz

type FileExplorerNameInputModal =

    [<ReactComponent>]
    static member Main
        (
            isOpen: bool,
            title: string,
            description: string,
            fieldLabel: string,
            initialValue: string,
            close: unit -> unit,
            submit: string -> unit,
            validate: string -> Result<string, string>,
            submitLabel: string,
            validationMessage: string,
            ?isBusy: bool,
            ?busyLabel: string,
            ?debug: string
        ) =

        let value, setValue = React.useState initialValue
        let isBusy = defaultArg isBusy false
        let busyLabel = defaultArg busyLabel submitLabel
        let debug = defaultArg debug "file-explorer-name-input"

        React.useEffect ((fun () -> setValue initialValue), [| box initialValue; box isOpen; box title |])

        let setIsOpen isOpen =
            if not isOpen then
                close ()

        let validationResult = validate value
        let isValid = validationResult |> Result.isOk

        let submitIfValid () =
            match validationResult with
            | Ok normalizedValue -> submit normalizedValue
            | Error _ -> ()

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.disabled isBusy
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.disabled ((not isValid) || isBusy)
                        prop.onClick (fun _ -> submitIfValid ())
                        prop.text (if isBusy then busyLabel else submitLabel)
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
                                prop.disabled isBusy
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

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text title,
            description = Html.text description,
            children = content,
            footer = footer,
            debug = debug
        )
