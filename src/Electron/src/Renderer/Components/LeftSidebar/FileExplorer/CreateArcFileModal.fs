namespace Renderer.Components.LeftSidebar.FileExplorer

open Swate.Components.Primitive.BaseModal
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules
open Feliz
open Fable.Core
open ARCtrl
open Helper

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

[<Erase; Mangle(false)>]
type CreateArcFileModal =

    [<ReactComponent>]
    static member Main
        (isOpen: bool, kind: ArcExplorerNodeKind, close: unit -> unit, submit: ArcExplorerNodeKind -> string -> unit)
        =

        let label = ArcExplorerNodeKind.label kind

        FileExplorerNameInputModal.Main(
            isOpen = isOpen,
            title = $"Add {label}",
            description = $"Create a new {label.ToLowerInvariant()} in the current ARC.",
            fieldLabel = "Identifier",
            initialValue = (arcCreateKindDefaultIdentifier kind),
            close = close,
            submit = (fun identifier -> submit kind identifier),
            validate = (fun identifier ->
                if isArcCreateIdentifierValid identifier then
                    Ok identifier
                else
                    Error arcCreateIdentifierError),
            submitLabel = $"Create {label}",
            validationMessage = arcCreateIdentifierError,
            debug = "arc-create"
        )

type CreateFileSystemItemModal =

    [<ReactComponent>]
    static member Main
        (
            isOpen: bool,
            kind: FileSystemItemKind,
            parentName: string option,
            close: unit -> unit,
            submit: string -> unit,
            ?isCreating: bool
        ) =

        let isCreating = defaultArg isCreating false

        let label = fileSystemCreateKindLabel kind
        let actionLabel = $"Create {label}"
        let parentDisplayName = parentName |> Option.defaultValue "this folder"

        FileExplorerNameInputModal.Main(
            isOpen = isOpen,
            title = actionLabel,
            description = $"Create a new {label.ToLowerInvariant()} in '{parentDisplayName}'.",
            fieldLabel = "Name",
            initialValue = "",
            close = close,
            submit = submit,
            validate = validateRenameName,
            submitLabel = actionLabel,
            validationMessage = "Name is required and must not contain path separators.",
            isBusy = isCreating,
            busyLabel = "Creating...",
            debug = "file-system-create"
        )
