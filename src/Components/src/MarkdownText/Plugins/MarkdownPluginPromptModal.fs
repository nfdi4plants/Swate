namespace Swate.Components.MarkdownText.Plugins

open Browser.Types
open Feliz

open Swate.Components

[<RequireQualifiedAccess>]
module MarkdownPluginPromptModal =

    [<ReactComponent>]
    let View
        (
            isOpen: bool,
            setIsOpen: bool -> unit,
            promptViewModel: PluginTextInputHelpers.PromptViewModel,
            promptInput: string,
            promptError: string option,
            promptFiles: MarkdownPromptFile list,
            promptFileDropActive: bool,
            promptInputRef: IRefValue<option<HTMLInputElement>>,
            promptFileInputRef: IRefValue<option<HTMLInputElement>>,
            setPromptFileDropActive: bool -> unit,
            onPromptInputChange: string -> unit,
            onPromptFileChange: File list -> unit,
            onTriggerPromptFileSelection: unit -> unit,
            onPromptDrop: DragEvent -> unit,
            onRemovePromptFileAtIndex: int -> unit,
            onSubmitPromptDialog: unit -> unit
        ) =
        let isFilePrompt = promptViewModel.InputMode = MarkdownPromptInputMode.File

        let promptDescription =
            promptViewModel.Description
            |> Option.map Html.text

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text promptViewModel.Title,
            ?description = promptDescription,
            children =
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2"
                    prop.children [
                        if isFilePrompt then
                            Html.input [
                                prop.testId "markdown-plugin-file-input"
                                prop.ref promptFileInputRef
                                prop.type'.file
                                prop.className "swt:hidden"
                                prop.multiple promptViewModel.AllowMultipleFiles
                                if promptViewModel.AcceptTypes.IsSome then
                                    prop.accept promptViewModel.AcceptTypes.Value
                                prop.onChange onPromptFileChange
                            ]

                            Html.button [
                                prop.type'.button
                                prop.className "swt:btn swt:btn-outline swt:w-full"
                                prop.text "Choose file"
                                prop.onClick (fun _ -> onTriggerPromptFileSelection ())
                            ]

                            Html.div [
                                prop.testId "markdown-plugin-file-dropzone"
                                prop.className [
                                    "swt:border-2 swt:border-dashed swt:rounded-box swt:p-3 swt:text-sm swt:text-center"
                                    if promptFileDropActive then
                                        "swt:border-primary swt:bg-primary/10"
                                    else
                                        "swt:border-base-300"
                                ]
                                prop.onDragEnter (fun (e: DragEvent) ->
                                    e.preventDefault ()
                                    e.stopPropagation ()
                                    setPromptFileDropActive true
                                )
                                prop.onDragLeave (fun (e: DragEvent) ->
                                    e.preventDefault ()
                                    e.stopPropagation ()
                                    setPromptFileDropActive false
                                )
                                prop.onDragOver (fun (e: DragEvent) ->
                                    e.preventDefault ()
                                    e.stopPropagation ()
                                )
                                prop.onDrop onPromptDrop
                                prop.text "Drop files here"
                            ]

                            if List.isEmpty promptFiles then
                                Html.div [
                                    prop.className "swt:text-xs swt:opacity-70"
                                    prop.text "No files selected."
                                ]
                            else
                                Html.ul [
                                    prop.className "swt:flex swt:flex-col swt:gap-1"
                                    prop.children [
                                        for index, file in promptFiles |> List.indexed do
                                            Html.li [
                                                prop.key $"prompt-file-{index}-{file.Name}"
                                                prop.className "swt:flex swt:items-center swt:gap-2"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "swt:grow swt:text-sm swt:truncate swt:text-left"
                                                        prop.title file.Name
                                                        prop.text file.Name
                                                    ]
                                                    Html.button [
                                                        prop.type'.button
                                                        prop.className "swt:btn swt:btn-xs swt:btn-ghost"
                                                        prop.ariaLabel $"Remove {file.Name}"
                                                        prop.text "x"
                                                        prop.onClick (fun _ -> onRemovePromptFileAtIndex index)
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                        else
                            Html.input [
                                prop.ref promptInputRef
                                prop.className [
                                    "swt:input swt:input-bordered swt:w-full"
                                    if promptError.IsSome then
                                        "swt:input-error"
                                ]
                                prop.placeholder promptViewModel.Placeholder
                                prop.value promptInput
                                prop.onChange onPromptInputChange
                                prop.onKeyDown (fun (e: KeyboardEvent) ->
                                    if e.key = "Enter" then
                                        e.preventDefault ()
                                        onSubmitPromptDialog ()
                                )
                            ]
                        if promptError.IsSome then
                            Html.p [
                                prop.className "swt:text-error swt:text-sm"
                                prop.text promptError.Value
                            ]
                    ]
                ],
            footer =
                React.Fragment [
                    Html.button [
                        prop.className "swt:btn"
                        prop.text "Cancel"
                        prop.onClick (fun _ -> setIsOpen false)
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary swt:ml-auto"
                        prop.text promptViewModel.SubmitButtonText
                        prop.onClick (fun _ -> onSubmitPromptDialog ())
                    ]
                ],
            initialFocusRef = unbox promptInputRef,
            className = "swt:bg-base-100 swt:text-base-content"
        )
