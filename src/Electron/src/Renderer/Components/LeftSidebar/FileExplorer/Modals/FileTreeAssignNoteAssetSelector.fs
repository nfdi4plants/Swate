namespace Renderer.Components.LeftSidebar.FileExplorer.Modals

open Fable.Core
open Feliz
open Renderer.Components.LeftSidebar.FileExplorer.Types

[<Erase; Mangle(false)>]
type FileTreeAssignNoteAssetSelector =

    [<ReactComponent>]
    static member Main
        (
            assets: ResizeArray<AssignableNoteAssetRef>,
            availableDestinations: AssignNoteAssetDestination list,
            assetDestinations: Map<string, AssignNoteAssetDestination>,
            setAssetDestination: string -> AssignNoteAssetDestination option -> unit,
            ?isAssigning: bool
        ) =

        let isAssigning = defaultArg isAssigning false

        if assets.Count = 0 then
            Html.none
        else
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    Html.div [
                        prop.className "swt:text-sm swt:font-medium"
                        prop.text "Assets"
                    ]

                    for asset in assets do
                        let selectedValue =
                            assetDestinations
                            |> Map.tryFind asset.SourceRelativePath
                            |> Option.map string
                            |> Option.defaultValue ""

                        Html.div [
                            prop.key asset.SourceRelativePath
                            prop.className "swt:grid swt:grid-cols-[minmax(0,1fr)_9rem] swt:gap-2 swt:items-center"
                            prop.children [
                                Html.div [
                                    prop.className "swt:truncate swt:text-sm"
                                    prop.title asset.RelativeAssetPath
                                    prop.text asset.RelativeAssetPath
                                ]

                                Html.select [
                                    prop.className "swt:select swt:select-bordered swt:select-sm swt:w-full"
                                    prop.disabled isAssigning
                                    prop.valueOrDefault selectedValue
                                    prop.onChange (fun value ->
                                        let selectedDestination =
                                            if value = "" then
                                                None
                                            else
                                                availableDestinations
                                                |> List.tryFind (fun destination -> string destination = value)

                                        setAssetDestination asset.SourceRelativePath selectedDestination
                                    )
                                    prop.children [
                                        Html.option [ prop.value ""; prop.text "Do not assign" ]

                                        for destination in availableDestinations do
                                            Html.option [
                                                prop.key (string destination)
                                                prop.value (string destination)
                                                prop.text (destination.ToString())
                                            ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
