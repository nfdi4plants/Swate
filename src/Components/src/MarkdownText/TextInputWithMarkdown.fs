namespace Swate.Components.MarkdownText

open System
open Browser.Types
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz

open Swate.Components
open Swate.Components.Metadata
open Swate.Components.MarkdownText.JsBindings
open Swate.Components.MarkdownText.Plugins

[<RequireQualifiedAccess>]
module private MarkdownTheme =

    let private normalize (value: string) =
        let trimmed = value.Trim().ToLowerInvariant()
        if trimmed = "" then None else Some trimmed

    let private isDarkThemeName = function
        | "dark" -> true
        | "finster"
        | "planti"
        | "viola" -> true
        | _ -> false

    let private tryGetThemeElements () =
        [|
            document.documentElement |> Option.ofObj
            document.body |> Option.ofObj
            document.querySelector ("[data-theme]")
            |> Option.ofObj
            |> Option.map (fun element -> element :?> HTMLElement)
        |]
        |> Array.choose id

    let private tryGetDataTheme (element: HTMLElement) =
        element.getAttribute ("data-theme")
        |> Option.ofObj
        |> Option.bind normalize

    let resolveColorMode () =
        let themeElements = tryGetThemeElements ()

        match themeElements |> Array.tryPick tryGetDataTheme with
        | Some dataTheme when isDarkThemeName dataTheme -> "dark"
        | Some _ -> "light"
        | None ->
            let mediaQuery: obj = window?matchMedia ("(prefers-color-scheme: dark)")
            if mediaQuery?matches |> unbox<bool> then "dark" else "light"

[<RequireQualifiedAccess>]
module private MarkdownCommands =

    let private bold: ICommand = ReactMDEditor.commands?bold |> unbox<ICommand>
    let private italic: ICommand = ReactMDEditor.commands?italic |> unbox<ICommand>
    let private strikethrough: ICommand = ReactMDEditor.commands?strikethrough |> unbox<ICommand>
    let private link: ICommand = ReactMDEditor.commands?link |> unbox<ICommand>
    let private quote: ICommand = ReactMDEditor.commands?quote |> unbox<ICommand>
    let private code: ICommand = ReactMDEditor.commands?code |> unbox<ICommand>
    let private codeBlock: ICommand = ReactMDEditor.commands?codeBlock |> unbox<ICommand>
    let private unorderedListCommand: ICommand = ReactMDEditor.commands?unorderedListCommand |> unbox<ICommand>
    let private orderedListCommand: ICommand = ReactMDEditor.commands?orderedListCommand |> unbox<ICommand>
    let private checkedListCommand: ICommand = ReactMDEditor.commands?checkedListCommand |> unbox<ICommand>

    let defaultToolbarGroups: ICommand[][] =
        [|
            [| bold; italic; strikethrough |]
            [| link; quote; code; codeBlock |]
            [| unorderedListCommand; orderedListCommand; checkedListCommand |]
        |]

    let toolbarGroupsWithPlugins (pluginCommands: ICommand[]) =
        if pluginCommands.Length = 0 then
            defaultToolbarGroups
        else
            Array.append defaultToolbarGroups [| pluginCommands |]

    let shortcutCommands (pluginCommands: ICommand[]) = toolbarGroupsWithPlugins pluginCommands |> Array.concat

[<Erase; Mangle(false)>]
type TextInputWithMarkdown =

    [<ReactComponent(true)>]
    static member TextInputWithMarkdown
        (
            value: string,
            setValue: string -> unit,
            ?label: string,
            ?placeholder: string,
            ?disabled: bool,
            ?classes: string,
            ?isJoin: bool,
            ?rmv: MouseEvent -> unit,
            ?validator: string -> Result<unit, string>,
            ?height: int,
            ?mode: PreviewMode,
            ?previewClassName: string,
            ?plugins: MarkdownToolbarPlugin list
        ) =
        let disabled = defaultArg disabled false
        let isJoin = defaultArg isJoin false

        let options =
            {
                MarkdownOptions.defaults with
                    Height = defaultArg height MarkdownOptions.defaults.Height
                    Mode = defaultArg mode MarkdownOptions.defaults.Mode
                    PreviewClassName =
                        match previewClassName with
                        | Some value -> Some value
                        | None -> MarkdownOptions.defaults.PreviewClassName
            }

        let startedChange = React.useRef false
        let tempValue, setTempValue = React.useState value
        let activeMode, setActiveMode = React.useState options.Mode
        let debouncedValue = React.useDebounce (tempValue, 300)
        let validationError, setValidationError = React.useState (None: string option)
        let addStepDialogOpen, setAddStepDialogOpen = React.useState false
        let addStepText, setAddStepText = React.useState ""
        let addStepDialogError, setAddStepDialogError = React.useState (None: string option)

        let textareaRef = React.useElementRef ()
        let addStepInputRef = React.useInputRef ()
        let commandOrchestratorRef = React.useRef<TextAreaCommandOrchestrator option> None
        let addStepSelectionRef = React.useRef (None: (int * int) option)

        React.useEffect (
            (fun () ->
                if startedChange.current then
                    match validator with
                    | Some validate ->
                        match validate debouncedValue with
                        | Ok() ->
                            setValidationError None
                            setValue debouncedValue
                        | Error message -> setValidationError (Some message)
                    | None ->
                        setValidationError None
                        setValue debouncedValue

                    startedChange.current <- false),
            [| box debouncedValue |]
        )

        React.useEffect ((fun () -> setTempValue value), [| box value |])

        React.useEffect (
            (fun () ->
                match mode with
                | Some value -> setActiveMode value
                | None -> ()),
            [| box mode |]
        )

        React.useEffect (
            (fun () ->
                if not addStepDialogOpen then
                    match addStepSelectionRef.current, textareaRef.current with
                    | Some(startIndex, endIndex), Some element ->
                        let textarea = element :?> HTMLTextAreaElement
                        textarea.focus ()
                        textarea?setSelectionRange (startIndex, endIndex)
                        addStepSelectionRef.current <- None
                    | _ -> ()),
            [| box addStepDialogOpen; box tempValue |]
        )

        let tryGetTextarea () =
            textareaRef.current
            |> Option.map (fun element -> element :?> HTMLTextAreaElement)

        let ensureCommandOrchestrator () =
            match commandOrchestratorRef.current, tryGetTextarea () with
            | Some orchestrator, Some textarea when obj.ReferenceEquals (orchestrator.textArea, textarea) ->
                Some orchestrator
            | _, Some textarea ->
                let orchestrator = TextAreaCommandOrchestrator(textarea)
                commandOrchestratorRef.current <- Some orchestrator
                Some orchestrator
            | _ -> None

        let syncTextFromTextarea () =
            match tryGetTextarea () with
            | Some textarea when textarea.value <> tempValue ->
                setTempValue textarea.value
                startedChange.current <- true
            | _ -> ()

        let handleTextChange =
            fun (text: string) ->
                setTempValue text
                startedChange.current <- true

        let getSelectionOrEnd () =
            match tryGetTextarea () with
            | Some textarea -> textarea.selectionStart, textarea.selectionEnd
            | None -> tempValue.Length, tempValue.Length

        let openAddStepDialog () =
            let startIndex, endIndex = getSelectionOrEnd ()
            addStepSelectionRef.current <- Some(startIndex, endIndex)
            setAddStepText ""
            setAddStepDialogError None
            setAddStepDialogOpen true

        let insertAddStep () =
            if String.IsNullOrWhiteSpace addStepText then
                setAddStepDialogError (Some "Step text is required.")
            else
                let startIndex, endIndex =
                    match addStepSelectionRef.current with
                    | Some(startIndex, endIndex) -> startIndex, endIndex
                    | None -> getSelectionOrEnd ()

                let nextValue, caretIndex =
                    AddStep.insertAtSelection tempValue startIndex endIndex addStepText

                setTempValue nextValue
                startedChange.current <- true
                addStepSelectionRef.current <- Some(caretIndex, caretIndex)
                setAddStepDialogOpen false
                setAddStepText ""
                setAddStepDialogError None

        let closeAddStepDialog (_: bool) =
            setAddStepDialogOpen false
            setAddStepDialogError None
            setAddStepText ""

        let pluginCommands = PluginRegistry.activeCommands plugins
        let toolbarGroups = MarkdownCommands.toolbarGroupsWithPlugins pluginCommands
        let shortcutCommands = MarkdownCommands.shortcutCommands pluginCommands
        let editorColorMode = MarkdownTheme.resolveColorMode ()

        let commandAriaLabel (command: ICommand) =
            let fallback =
                if String.IsNullOrWhiteSpace command.name then
                    "Markdown command"
                else
                    command.name

            if isNullOrUndefined command.buttonProps then
                fallback
            else
                let ariaLabel: obj = command.buttonProps?``aria-label``
                if isNullOrUndefined ariaLabel then fallback else string ariaLabel

        let executeCommand (command: ICommand) =
            fun (_: MouseEvent) ->
                if command.keyCommand = AddStep.keyCommand then
                    openAddStepDialog ()
                else
                    ensureCommandOrchestrator ()
                    |> Option.iter (fun orchestrator ->
                        orchestrator.executeCommand command
                        syncTextFromTextarea ()
                    )

        let handleTextAreaKeyDown =
            fun (e: KeyboardEvent) ->
                match ensureCommandOrchestrator (), tryGetTextarea () with
                | Some orchestrator, Some textarea ->
                    let before = textarea.value
                    ReactMDEditor.handleKeyDown (e, tabSize = 2, defaultTabEnable = false)
                    ReactMDEditor.shortcuts (e, shortcutCommands, orchestrator)

                    if textarea.value <> before then
                        syncTextFromTextarea ()
                | _ -> ()

        let toolbarCommandDisabled = disabled || activeMode = PreviewMode.Preview

        let modeButton (targetMode: PreviewMode, label: string) =
            Html.button [
                prop.type'.button
                prop.className [
                    "swt:btn swt:btn-xs swt:join-item"
                    if activeMode = targetMode then
                        "swt:btn-primary"
                    else
                        "swt:btn-ghost"
                ]
                prop.text label
                prop.disabled disabled
                prop.onClick (fun _ -> setActiveMode targetMode)
            ]

        let previewWrapperElement = createObj [ "data-color-mode" ==> editorColorMode ]

        let previewClassName =
            match options.PreviewClassName with
            | Some className -> $"swt:p-4 {className}"
            | None -> "swt:p-4"

        let editorWrapperClasses = [
            "wmde-markdown-var swt:w-full swt:max-w-none swt:p-0 swt:overflow-hidden swt:min-h-0 swt:rounded-field swt:border swt:border-base-300 swt:bg-base-100"
            if validationError.IsSome then
                "swt:border-error"
            if disabled then
                "swt:opacity-70"
            if classes.IsSome then
                classes.Value
        ]

        Html.div [
            prop.className (
                if isJoin then
                    "swt:grow swt:join-item"
                else
                    "swt:fieldset swt:grow"
            )
            prop.children [
                if label.IsSome && not isJoin then
                    Generic.FieldTitle label.Value

                Html.div [
                    prop.className "swt:flex swt:gap-2 swt:items-start swt:w-full"
                    prop.children [
                        Html.div [
                            prop.className editorWrapperClasses
                            prop.custom ("data-color-mode", editorColorMode)
                            prop.children [
                                Html.div [
                                    prop.className
                                        "swt:flex swt:flex-wrap swt:items-center swt:gap-2 swt:px-2 swt:py-1 swt:border-b swt:border-base-300 swt:bg-base-100"
                                    prop.children [
                                        Html.div [
                                            prop.className "swt:flex swt:flex-wrap swt:items-center swt:gap-1"
                                            prop.children [
                                                for groupIndex, group in toolbarGroups |> Array.indexed do
                                                    Html.div [
                                                        prop.key $"toolbar-group-{groupIndex}"
                                                        prop.className "swt:flex swt:items-center swt:gap-1"
                                                        prop.children [
                                                            for commandIndex, command in group |> Array.indexed do
                                                                let label = commandAriaLabel command

                                                                Html.button [
                                                                    prop.key
                                                                        $"toolbar-command-{groupIndex}-{commandIndex}-{command.keyCommand}-{command.name}"
                                                                    prop.type'.button
                                                                    prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                                                    prop.ariaLabel label
                                                                    prop.title label
                                                                    prop.disabled toolbarCommandDisabled
                                                                    prop.onClick (executeCommand command)
                                                                    prop.children [ command.icon ]
                                                                ]
                                                        ]
                                                    ]
                                            ]
                                        ]

                                        Html.div [
                                            prop.className "swt:join swt:ml-auto"
                                            prop.children [
                                                modeButton (PreviewMode.Edit, "Edit")
                                                modeButton (PreviewMode.Live, "Live")
                                                modeButton (PreviewMode.Preview, "Preview")
                                            ]
                                        ]
                                    ]
                                ]

                                Html.div [
                                    prop.className [
                                        if activeMode = PreviewMode.Live then
                                            "swt:grid swt:grid-cols-1 swt:lg:grid-cols-2"
                                        else
                                            "swt:block"
                                    ]
                                    prop.children [
                                        if activeMode <> PreviewMode.Preview then
                                            Html.div [
                                                prop.className [
                                                    "swt:min-w-0"
                                                    if activeMode = PreviewMode.Live then
                                                        "swt:border-b swt:border-base-300 swt:lg:border-b-0 swt:lg:border-r"
                                                ]
                                                prop.style [ style.height options.Height ]
                                                prop.children [
                                                    Html.textarea [
                                                        prop.ref textareaRef
                                                        prop.className
                                                            "swt:w-full swt:h-full swt:border-0 swt:bg-transparent swt:resize-none swt:px-3 swt:py-2 swt:focus:outline-hidden"
                                                        prop.disabled disabled
                                                        prop.readOnly disabled
                                                        prop.value tempValue
                                                        prop.onChange handleTextChange
                                                        prop.onKeyDown handleTextAreaKeyDown
                                                        if placeholder.IsSome then
                                                            prop.placeholder placeholder.Value
                                                    ]
                                                ]
                                            ]

                                        if activeMode <> PreviewMode.Edit then
                                            Html.div [
                                                prop.className "swt:min-w-0 swt:overflow-auto swt:bg-base-100"
                                                prop.style [ style.height options.Height ]
                                                prop.children [
                                                    ReactMDEditor.MarkdownPreview(
                                                        tempValue,
                                                        className = previewClassName,
                                                        components = Preview.components,
                                                        rehypePlugins = Preview.rehypePlugins,
                                                        wrapperElement = previewWrapperElement
                                                    )
                                                ]
                                            ]
                                    ]
                                ]
                            ]
                        ]

                        if rmv.IsSome then
                            Html.button [
                                prop.className "swt:btn swt:btn-error swt:grow-0"
                                prop.text "Delete"
                                prop.onClick rmv.Value
                            ]
                    ]
                ]

                if validationError.IsSome then
                    Html.p [
                        prop.className "swt:text-error swt:text-sm swt:mt-1"
                        prop.text validationError.Value
                    ]

                BaseModal.Modal(
                    isOpen = addStepDialogOpen,
                    setIsOpen = closeAddStepDialog,
                    header = Html.text "Add Step",
                    description = Html.text "Insert a markdown checklist item at the current cursor position.",
                    children =
                        Html.div [
                            prop.className "swt:flex swt:flex-col swt:gap-2"
                            prop.children [
                                Html.input [
                                    prop.ref addStepInputRef
                                    prop.className [
                                        "swt:input swt:input-bordered swt:w-full"
                                        if addStepDialogError.IsSome then
                                            "swt:input-error"
                                    ]
                                    prop.placeholder "Step text"
                                    prop.value addStepText
                                    prop.onChange (fun text ->
                                        setAddStepText text
                                        if addStepDialogError.IsSome then
                                            setAddStepDialogError None
                                    )
                                    prop.onKeyDown (fun (e: KeyboardEvent) ->
                                        if e.key = "Enter" then
                                            e.preventDefault ()
                                            insertAddStep ()
                                    )
                                ]
                                if addStepDialogError.IsSome then
                                    Html.p [
                                        prop.className "swt:text-error swt:text-sm"
                                        prop.text addStepDialogError.Value
                                    ]
                            ]
                        ],
                    footer =
                        React.Fragment [
                            Html.button [
                                prop.className "swt:btn"
                                prop.text "Cancel"
                                prop.onClick (fun _ -> closeAddStepDialog false)
                            ]
                            Html.button [
                                prop.className "swt:btn swt:btn-primary swt:ml-auto"
                                prop.text "Add"
                                prop.onClick (fun _ -> insertAddStep ())
                            ]
                        ],
                    initialFocusRef = unbox addStepInputRef,
                    className = "swt:bg-base-100 swt:text-base-content"
                )
            ]
        ]

[<Erase; Mangle(false)>]
type TextInputWithMarkdownEntry =

    [<ReactComponent>]
    static member Entry() =
        let entryInitialValue =
            """# Markdown Notes

Use this editor for protocol notes and checklists.

```mermaid
flowchart TD
  A[Draft] --> B{Review}
  B -->|Approved| C[Execute]
  B -->|Changes| A
```
"""

        let value, setValue = React.useState entryInitialValue

        TextInputWithMarkdown.TextInputWithMarkdown(
            value,
            setValue,
            placeholder = "Write markdown...",
            height = 440
        )
