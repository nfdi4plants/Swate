namespace Swate.Components.Composite.MarkdownText.Plugins

open Browser.Types
open Fable.Core
open Feliz

open Swate.Components
open Swate.Components.Primitive.BaseModal

[<RequireQualifiedAccess>]
module MarkdownPluginPromptModal =

    type ViewProps = {
        SetIsOpen: bool -> unit
        Prompt: MarkdownPromptPlugin
        FilePickerAdapter: MarkdownFilePickerAdapter option
        OnSubmitTextPrompt: MarkdownPromptPlugin -> string -> JS.Promise<unit>
        OnSubmitFilePrompt: MarkdownPromptPlugin -> MarkdownPromptFile list -> JS.Promise<unit>
    }

    [<ReactComponent>]
    let View (props: ViewProps) =
        let activePrompt = Some props.Prompt
        let filePickerAdapter = props.FilePickerAdapter
        let promptViewModel = PluginTextInputHelpers.promptViewModel activePrompt

        let promptInput, setPromptInput = React.useState ""
        let promptError, setPromptError = React.useState (None: string option)

        let promptFiles, setPromptFiles =
            React.useStateWithUpdater ([]: MarkdownPromptFile list)

        let promptFileDropActive, setPromptFileDropActive = React.useState false
        let promptInputRef = React.useInputRef ()
        let promptFileInputRef = React.useInputRef ()
        let isMountedRef = React.useRef true

        React.useEffectOnce (fun _ ->
            { new System.IDisposable with
                member _.Dispose() = isMountedRef.current <- false
            }
        )

        let resetPromptState () =
            setPromptInput ""
            setPromptError None
            setPromptFiles (fun _ -> [])
            setPromptFileDropActive false

        let setIsOpen isOpen =
            if not isOpen then
                resetPromptState ()

            props.SetIsOpen isOpen

        let appendPromptFilesAndClearError (files: MarkdownPromptFile list) =
            if isMountedRef.current then
                setPromptFiles (fun currentFiles ->
                    let combined = currentFiles @ files
                    PluginTextInputHelpers.normalizePromptFiles activePrompt combined
                )

                if promptError.IsSome then
                    setPromptError None

        let applyPickedPromptFiles (files: MarkdownPromptFile list) =
            let accepted, rejected =
                PluginTextInputHelpers.partitionFilesByAccept activePrompt files

            if not (List.isEmpty accepted) then
                appendPromptFilesAndClearError accepted

            if not (List.isEmpty rejected) && isMountedRef.current then
                setPromptError (Some(PluginTextInputHelpers.rejectedFilesMessage activePrompt rejected))

        let removePromptFileAtIndex (indexToRemove: int) =
            setPromptFiles (fun currentFiles ->
                currentFiles
                |> List.indexed
                |> List.choose (fun (index, file) -> if index = indexToRemove then None else Some file)
            )

        let promptFilePickerOptions () = {
            AcceptTypes = PluginTextInputHelpers.activePromptAcceptTypes activePrompt
            AllowMultiple = Some(PluginTextInputHelpers.activePromptAllowsMultipleFiles activePrompt)
        }

        let triggerPromptFileSelection () =
            promise {
                match filePickerAdapter with
                | Some adapter ->
                    let! files = adapter.PickFiles(promptFilePickerOptions ())

                    applyPickedPromptFiles files
                | None -> promptFileInputRef.current |> Option.iter (fun input -> input.click ())
            }
            |> Promise.catch (fun err ->
                if isMountedRef.current then
                    setPromptError (Some $"File selection failed: {string err}")
            )
            |> Promise.start

        let handlePromptFileChange =
            fun (files: File list) ->
                let selected = files |> List.map PluginTextInputHelpers.toPromptFile
                applyPickedPromptFiles selected

                // Reset the input value so selecting the same file triggers onChange.
                promptFileInputRef.current |> Option.iter (fun input -> input.value <- "")

        let handlePromptDrop =
            fun (e: DragEvent) ->
                e.preventDefault ()
                e.stopPropagation ()
                setPromptFileDropActive false

                let files =
                    if isNull e.dataTransfer || isNull e.dataTransfer.files then
                        []
                    else
                        [
                            for i in 0 .. int e.dataTransfer.files.length - 1 do
                                let file = e.dataTransfer.files.item i

                                if not (isNull file) then
                                    yield PluginTextInputHelpers.toPromptFile file
                        ]

                if List.isEmpty files then
                    setPromptError (Some "No files were dropped.")
                else
                    applyPickedPromptFiles files

        let handlePromptInputChange =
            fun (text: string) ->
                setPromptInput text

                if promptError.IsSome then
                    setPromptError None

        let submitPrompt submit =
            promise {
                try
                    do! submit

                    if isMountedRef.current then
                        resetPromptState ()
                with exn ->
                    if isMountedRef.current then
                        setPromptError (Some(string exn))
            }
            |> Promise.start

        let submitPromptDialog () =
            match PluginTextInputHelpers.activePromptInputMode activePrompt with
            | MarkdownPromptInputMode.Text ->
                match props.Prompt.Validate promptInput with
                | Error message -> setPromptError (Some message)
                | Ok() -> submitPrompt (props.OnSubmitTextPrompt props.Prompt promptInput)

            | MarkdownPromptInputMode.File ->
                let selectedFiles =
                    promptFiles |> PluginTextInputHelpers.normalizePromptFiles activePrompt

                if List.isEmpty selectedFiles then
                    setPromptError (Some "Select at least one file.")
                else
                    match props.Prompt.ApplyFiles with
                    | None -> setPromptError (Some "This plugin does not support file input.")
                    | Some _ -> submitPrompt (props.OnSubmitFilePrompt props.Prompt selectedFiles)

        let isFilePrompt = promptViewModel.InputMode = MarkdownPromptInputMode.File

        let promptDescription = promptViewModel.Description |> Option.map Html.text

        let promptFileKey (file: MarkdownPromptFile) =
            let sourceId = file.SourceId |> Option.defaultValue ""
            let mimeType = file.MimeType |> Option.defaultValue ""
            $"{sourceId}|{file.Name}|{mimeType}"

        BaseModal.Modal(
            isOpen = true,
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
                                prop.onChange handlePromptFileChange
                            ]

                            let fileDropButtonText = "Drop files here or click to upload"

                            Html.button [
                                prop.type'.button
                                prop.testId "markdown-plugin-file-dropzone"
                                prop.className [
                                    "swt:btn swt:btn-outline swt:w-full swt:h-auto swt:min-h-0 swt:py-3 swt:border-2 swt:border-dashed swt:rounded-box swt:text-sm swt:text-center swt:normal-case"
                                    if promptFileDropActive then
                                        "swt:border-primary swt:bg-primary/10"
                                    else
                                        "swt:border-base-300"
                                ]
                                prop.onClick (fun _ -> triggerPromptFileSelection ())
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
                                prop.onDrop handlePromptDrop
                                prop.text fileDropButtonText
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
                                                prop.key $"prompt-file-{promptFileKey file}"
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
                                                        prop.onClick (fun _ -> removePromptFileAtIndex index)
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
                                prop.onChange handlePromptInputChange
                                prop.onKeyDown (fun (e: KeyboardEvent) ->
                                    if e.key = "Enter" then
                                        e.preventDefault ()
                                        submitPromptDialog ()
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
                        prop.onClick (fun _ -> submitPromptDialog ())
                    ]
                ],
            initialFocusRef = unbox promptInputRef,
            className = "swt:bg-base-100 swt:text-base-content"
        )
