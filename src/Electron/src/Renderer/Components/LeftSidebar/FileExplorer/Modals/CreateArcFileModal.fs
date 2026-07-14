namespace Renderer.Components.LeftSidebar.FileExplorer.Modals

open Fable.Core
open Feliz
open Swate.Components.Shared
open Swate.Components.Primitive.Dialog
open Renderer.Components.LeftSidebar.FileExplorer.Helper

[<Erase; Mangle(false)>]
type CreateArcFileModal =

    [<ReactComponent>]
    static member Main
        (
            isOpen: bool,
            kind: ArcExplorerNodeKind,
            close: unit -> unit,
            submit: ArcExplorerNodeKind -> string -> JS.Promise<unit>
        ) =

        let label = ArcExplorerNodeKind.label kind

        Dialog.StringSubmissionDialog(
            isOpen = isOpen,
            title = $"Add {label}",
            description = $"Create a new {label.ToLowerInvariant()} in the current ARC.",
            fieldLabel = "Identifier",
            initialValue = (arcCreateKindDefaultIdentifier kind),
            close = close,
            submit = (fun identifier -> submit kind identifier),
            validate =
                (fun identifier ->
                    if isArcCreateIdentifierValid identifier then
                        Ok identifier
                    else
                        Error arcCreateIdentifierError
                ),
            submitLabel = $"Create {label}",
            validationMessage = arcCreateIdentifierError,
            busyLabel = "Creating...",
            debug = "arc-create"
        )
