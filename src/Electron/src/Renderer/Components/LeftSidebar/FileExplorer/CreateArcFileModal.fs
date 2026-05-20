namespace Renderer.Components.LeftSidebar.FileExplorer

open Swate.Components.Shared
open Swate.Electron.Shared.FileIOTypes
open Swate.Electron.Shared.RenamePathRules
open Feliz
open Fable.Core
open ARCtrl
open Helper

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
