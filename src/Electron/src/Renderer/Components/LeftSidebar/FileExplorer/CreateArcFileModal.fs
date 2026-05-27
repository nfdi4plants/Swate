namespace Renderer.Components.LeftSidebar.FileExplorer

open Swate.Components
open Swate.Components.Primitive.BaseModal
open Swate.Components.Page.FileExplorer.Types
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Feliz
open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Helper

[<Erase; Mangle(false)>]
type CreateArcFileModal =

    [<ReactComponent>]
    static member Main
        (isOpen: bool, kind: ArcExplorerNodeKind, close: unit -> unit, submit: ArcExplorerNodeKind -> string -> unit)
        =

        let identifier, setIdentifier = React.useState (arcCreateKindDefaultIdentifier kind)

        React.useEffect ((fun () -> setIdentifier (arcCreateKindDefaultIdentifier kind)), [| box kind |])

        let setIsOpen isOpen =
            if not isOpen then
                close ()

        let label = ArcExplorerNodeKind.label kind
        let isValid = isArcCreateIdentifierValid identifier

        let submitIfValid () =
            if isValid then
                submit kind identifier

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.disabled (not isValid)
                        prop.onClick (fun _ -> submitIfValid ())
                        prop.text $"Create {label}"
                    ]
                ]
            ]

        let content =
            Html.fieldSet [
                prop.className "swt:fieldset"
                prop.children [
                    Html.legend [
                        prop.className "swt:fieldset-legend"
                        prop.text "Identifier"
                    ]
                    Html.label [
                        prop.className "swt:input swt:w-full"
                        prop.children [
                            Html.input [
                                prop.autoFocus true
                                prop.value identifier
                                prop.onChange setIdentifier
                                prop.onKeyDown (key.enter, fun _ -> submitIfValid ())
                            ]
                        ]
                    ]
                    Html.p [
                        prop.hidden isValid
                        prop.className "swt:text-error swt:text-sm"
                        prop.text arcCreateIdentifierError
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text $"Add {label}",
            description = Html.text $"Create a new {label.ToLowerInvariant()} in the current ARC.",
            children = content,
            footer = footer,
            debug = "arc-create"
        )