namespace Swate.Components.Page.FileExplorer.Modals

open Fable.Core
open Feliz
open Swate.Components.Primitive.BaseModal
open Swate.Components.Page.FileExplorer.Types

[<Erase; Mangle(false)>]
type AssignNoteModal =

    [<ReactComponent>]
    static member AssignNoteModal
        (
            isOpen: bool,
            itemName: string option,
            selectedNote: AssignableNoteRef option,
            setSelectedNote: AssignableNoteRef option -> unit,
            availableNotes: ResizeArray<AssignableNoteRef>,
            availableAssets: ResizeArray<AssignableNoteAssetRef>,
            availableAssetDestinations: AssignNoteAssetDestination list,
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

        let setClose isOpen =
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
                        prop.className "swt:btn swt:btn-ghost swt:btn-sm"
                        prop.disabled isAssigning
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary swt:btn-sm"
                        prop.disabled (isAssigning || selectedNote.IsNone)
                        prop.onClick (fun _ -> submit ())
                        prop.text (if isAssigning then "Assigning..." else "Assign")
                    ]
                ]
            ]

        let modalActions =
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-4 swt:w-full"
                prop.children [
                    noteSelector
                    AssignNoteAssetSelector.Header(
                        availableAssets,
                        availableAssetDestinations,
                        assetDestinations,
                        setAssetDestination,
                        isAssigning
                    )
                ]
            ]

        let assetRows =
            AssignNoteAssetSelector.Rows(
                availableAssets,
                availableAssetDestinations,
                assetDestinations,
                setAssetDestination,
                isAssigning
            )

        BaseModal.Modal(
            isOpen = isOpen,
            setIsOpen = setClose,
            header = Html.text "Assign Note",
            description = Html.text $"Assign a note to '{displayName}'.",
            modalActions = modalActions,
            modalActionsClassName =
                "swt:w-full swt:flex swt:flex-col swt:items-stretch swt:justify-start swt:gap-4 swt:p-2",
            children = assetRows,
            footer = footer,
            debug = "arc-assign-note",
            className = "swt:!flex swt:flex-col swt:max-h-[calc(100vh_-_5rem)] swt:!overflow-hidden"
        )
