namespace Renderer.Components.LeftSidebar.FileExplorer.Modals

open Fable.Core
open Feliz
open Swate.Components.Primitive.BaseModal
open Renderer.Components.LeftSidebar.FileExplorer.Types

[<Erase; Mangle(false)>]
type FileTreeAssignNoteModal =

    [<ReactComponent>]
    static member Main
        (
            isOpen: bool,
            itemName: string option,
            selectedNote: AssignableNoteRef option,
            setSelectedNote: AssignableNoteRef option -> unit,
            availableNotes: ResizeArray<AssignableNoteRef>,
            availableAssets: ResizeArray<AssignableNoteAssetRef>,
            assetDestinations: Map<string, AssignNoteAssetDestination>,
            setAssetDestination: string -> AssignNoteAssetDestination option -> unit,
            close: unit -> unit,
            submit: unit -> unit,
            ?isAssigning: bool
        ) =

        let isAssigning = defaultArg isAssigning false
        let displayName = itemName |> Option.defaultValue "this target"
        let hasNotes = availableNotes.Count > 0

        let selectedValue =
            selectedNote |> Option.map _.SourceFolderPath |> Option.defaultValue ""

        let setIsOpen isOpen =
            if not isOpen then
                close ()

        let noteSelector =
            Html.select [
                prop.className "swt:select swt:select-bordered swt:w-full"
                prop.disabled (isAssigning || not hasNotes)
                prop.valueOrDefault selectedValue
                prop.onChange (fun (value: string) ->
                    availableNotes
                    |> Seq.tryFind (fun note -> note.SourceFolderPath = value)
                    |> setSelectedNote
                )
                prop.children [
                    if not hasNotes then
                        Html.option [ prop.value ""; prop.text "No notes available" ]
                    else
                        Html.option [ prop.value ""; prop.text "Select note" ]

                    for note in availableNotes do
                        Html.option [
                            prop.key note.SourceFolderPath
                            prop.value note.SourceFolderPath
                            prop.text note.Label
                        ]
                ]
            ]

        let footer =
            Html.div [
                prop.className "swt:flex swt:gap-2 swt:justify-end swt:w-full"
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-ghost"
                        prop.disabled isAssigning
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.disabled (isAssigning || selectedNote.IsNone)
                        prop.onClick (fun _ -> submit ())
                        prop.text (if isAssigning then "Assigning..." else "Assign")
                    ]
                ]
            ]

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setIsOpen,
            header = Html.text "Assign Note",
            description = Html.text $"Assign a note to '{displayName}'.",
            children =
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-4"
                    prop.children [
                        noteSelector
                        FileTreeAssignNoteAssetSelector.Main(
                            availableAssets,
                            assetDestinations,
                            setAssetDestination,
                            isAssigning
                        )
                    ]
                ],
            footer = footer,
            debug = "arc-assign-note"
        )
