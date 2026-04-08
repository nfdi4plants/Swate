namespace Swate.Components

open Feliz
open Fable.Core

[<Erase; Mangle(false)>]
type CancelableErrorModal =

    static member private actionButton (action: ErrorModalAction) =
        let className =
            match action.Style with
            | ErrorModalActionStyle.Primary -> "swt:btn-primary"
            | ErrorModalActionStyle.Error -> "swt:btn-error"
            | ErrorModalActionStyle.Neutral -> "swt:btn-neutral"

        Html.button [
            prop.className $"swt:btn {className}"
            prop.onClick (fun _ -> action.OnClick ())
            prop.children [
                if action.IconClassName.IsSome then
                    Html.i [
                        prop.className [ "swt:iconify"; action.IconClassName.Value ]
                    ]
                Html.span action.Label
            ]
        ]

    static member private messageBlock (message: string) =
        Html.div [
            prop.className "swt:whitespace-pre-wrap"
            prop.children (
                message.Split('\n')
                |> Array.collect (fun line -> [| Html.text line; Html.br [] |])
            )
        ]

    static member private detailsBlock (details: string option) =
        match details with
        | None -> Html.none
        | Some details ->
            Html.details [
                prop.className "swt:collapse swt:collapse-arrow swt:border swt:border-base-300 swt:bg-base-200"
                prop.children [
                    Html.summary [
                        prop.className "swt:collapse-title swt:min-h-0 swt:py-3 swt:font-medium"
                        prop.text "Technical details"
                    ]
                    Html.div [
                        prop.className "swt:collapse-content"
                        prop.children [
                            Html.pre [
                                prop.className "swt:whitespace-pre-wrap swt:text-sm"
                                prop.text details
                            ]
                        ]
                    ]
                ]
            ]

    [<ReactComponent>]
    static member Modal
        (
            isOpen: bool,
            setIsOpen: bool -> unit,
            request: CancelableErrorModalRequest,
            onDismiss: unit -> unit,
            onCancel: unit -> unit,
            ?appendix: ReactElement,
            ?footerExtraButtons: ReactElement,
            ?debug: string,
            ?className: string
        ) =

        let resolvedClassName = defaultArg className ""
        let modalClassName = $"swt:max-w-2xl {resolvedClassName}".Trim()

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header =
                Html.div [
                    prop.className "swt:flex swt:items-center swt:gap-2"
                    prop.children [
                        Html.i [
                            prop.className "swt:iconify swt:fluent--warning-24-filled swt:size-6"
                        ]
                        Html.span request.Title
                    ]
                ],
            children =
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-3"
                    prop.children [
                        CancelableErrorModal.messageBlock request.Message
                        CancelableErrorModal.detailsBlock request.Details
                        if appendix.IsSome then
                            appendix.Value
                    ]
                ],
            footer =
                Html.div [
                    prop.className "swt:flex swt:w-full swt:flex-wrap swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.className "swt:btn"
                            prop.text request.CancelLabel
                            prop.onClick (fun _ -> onCancel ())
                        ]
                        Html.div [
                            prop.className "swt:ml-auto swt:flex swt:flex-wrap swt:justify-end swt:gap-2"
                            prop.children [
                                for action in request.Actions do
                                    CancelableErrorModal.actionButton action
                                if footerExtraButtons.IsSome then
                                    footerExtraButtons.Value
                                Html.button [
                                    prop.className "swt:btn swt:btn-primary"
                                    prop.text request.DismissLabel
                                    prop.onClick (fun _ -> onDismiss ())
                                ]
                            ]
                        ]
                    ]
                ],
            ?debug = debug,
            className = modalClassName
        )
