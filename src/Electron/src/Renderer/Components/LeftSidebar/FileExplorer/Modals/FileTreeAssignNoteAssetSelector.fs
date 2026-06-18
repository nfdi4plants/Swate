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
            assetDestinations: Map<string, AssignNoteAssetDestination>,
            setAssetDestination: string -> AssignNoteAssetDestination option -> unit,
            ?isAssigning: bool
        ) =

        let isAssigning = defaultArg isAssigning false

        let destinationOptions = [
            "", "Do not assign", None
            "protocol", "Protocol", Some AssignNoteAssetDestination.Protocol
            "dataset", "Datasets", Some AssignNoteAssetDestination.Dataset
            "resource", "Resources", Some AssignNoteAssetDestination.Resource
        ]

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
                            |> Option.bind (fun selectedDestination ->
                                destinationOptions
                                |> List.tryFind (fun (_, _, destination) -> destination = Some selectedDestination)
                                |> Option.map (fun (value, _, _) -> value)
                            )
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
                                        destinationOptions
                                        |> List.tryFind (fun (optionValue, _, _) -> optionValue = value)
                                        |> Option.map (fun (_, _, destination) -> destination)
                                        |> Option.defaultValue None
                                        |> setAssetDestination asset.SourceRelativePath
                                    )
                                    prop.children [
                                        for value, label, _ in destinationOptions do
                                            Html.option [ prop.key value; prop.value value; prop.text label ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
