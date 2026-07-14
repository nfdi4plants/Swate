namespace Swate.Components.Page.FileExplorer.Modals

open Fable.Core
open Feliz
open Swate.Components.Primitive.BaseModal
open Swate.Components.Page.FileExplorer.Types

module private AssignNoteModalHelper =

    type AssetDestinationState = {
        NoteKey: string option
        AssetDestinations: Map<string, AssignNoteAssetDestination>
    }

    let selectedNoteKey (selectedNote: AssignableNoteRef option) =
        selectedNote |> Option.map _.SourceFolderPath

    let defaultAssetDestinations
        (availableDestinations: AssignNoteAssetDestination list)
        (assets: seq<AssignableNoteAssetRef>)
        =
        match availableDestinations |> List.tryHead with
        | None -> Map.empty
        | Some defaultDestination ->
            assets
            |> Seq.map (fun asset -> asset.SourceRelativePath, defaultDestination)
            |> Map.ofSeq

    let updateAssetDestination noteKey currentAssetDestinations assetPath destination = {
        NoteKey = noteKey
        AssetDestinations =
            match destination with
            | Some destination -> currentAssetDestinations |> Map.add assetPath destination
            | None -> currentAssetDestinations |> Map.remove assetPath
    }

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
            close: unit -> unit,
            submit: AssignableNoteRef -> Map<string, AssignNoteAssetDestination> -> JS.Promise<unit>
        ) =

        let isRunning, setIsRunning = React.useState false

        let selectedNoteKey = AssignNoteModalHelper.selectedNoteKey selectedNote

        let defaultAssetDestinations =
            AssignNoteModalHelper.defaultAssetDestinations availableAssetDestinations availableAssets

        let initialAssetDestinationState: AssignNoteModalHelper.AssetDestinationState = {
            NoteKey = selectedNoteKey
            AssetDestinations = defaultAssetDestinations
        }

        let assetDestinationState, setAssetDestinationState =
            React.useStateWithUpdater initialAssetDestinationState

        let isDisabled = isRunning
        let displayName = itemName |> Option.defaultValue "this target"
        let hasNotes = availableNotes.Count > 0

        let assetDestinations =
            if assetDestinationState.NoteKey = selectedNoteKey then
                assetDestinationState.AssetDestinations
            else
                setAssetDestinationState (fun _ -> {
                    NoteKey = selectedNoteKey
                    AssetDestinations = defaultAssetDestinations
                })

                defaultAssetDestinations

        let setAssetDestination assetPath destination =
            setAssetDestinationState (fun currentState ->
                let currentAssetDestinations =
                    if currentState.NoteKey = selectedNoteKey then
                        currentState.AssetDestinations
                    else
                        defaultAssetDestinations

                AssignNoteModalHelper.updateAssetDestination
                    selectedNoteKey
                    currentAssetDestinations
                    assetPath
                    destination
            )

        let selectedValue =
            selectedNote |> Option.map _.SourceFolderPath |> Option.defaultValue ""

        let setClose isOpen =
            if not isOpen && not isDisabled then
                close ()

        let onSubmit () =
            promise {
                match selectedNote with
                | None -> ()
                | Some note when not isRunning ->
                    setIsRunning true

                    try
                        do! submit note assetDestinations
                    finally
                        setIsRunning false
                | Some _ -> ()
            }
            |> Promise.start

        let noteSelector =
            Html.select [
                prop.className "swt:select swt:select-bordered swt:w-full"
                prop.disabled (isDisabled || not hasNotes)
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
                        prop.disabled isDisabled
                        prop.onClick (fun _ -> close ())
                        prop.text "Cancel"
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary swt:btn-sm"
                        prop.disabled (isDisabled || selectedNote.IsNone)
                        prop.onClick (fun _ -> onSubmit ())
                        prop.text (if isRunning then "Assigning..." else "Assign")
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
                        isDisabled
                    )
                ]
            ]

        let assetRows =
            AssignNoteAssetSelector.Rows(
                availableAssets,
                availableAssetDestinations,
                assetDestinations,
                setAssetDestination,
                isDisabled
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
