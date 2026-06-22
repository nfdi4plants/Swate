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
            let mixedHeaderValue = "__mixed__"

            let selectedValueForAsset asset =
                assetDestinations
                |> Map.tryFind asset.SourceRelativePath
                |> Option.map string
                |> Option.defaultValue ""

            let tryParseDestinationValue value =
                if value = mixedHeaderValue then
                    None
                elif value = "" then
                    Some None
                else
                    availableDestinations
                    |> List.tryFind (fun destination -> string destination = value)
                    |> Option.map Some

            let headerSelectedValue =
                let distinctSelectedValues =
                    assets |> Seq.map selectedValueForAsset |> Seq.distinct |> Seq.toList

                match distinctSelectedValues with
                | [ selectedValue ] -> selectedValue
                | _ -> mixedHeaderValue

            let destinationOptions includeMixedOption = [
                if includeMixedOption then
                    Html.option [
                        prop.value mixedHeaderValue
                        prop.disabled true
                        prop.text "Mixed"
                    ]

                Html.option [ prop.value ""; prop.text "Do not assign" ]

                for destination in availableDestinations do
                    Html.option [
                        prop.key (string destination)
                        prop.value (string destination)
                        prop.text (destination.ToString())
                    ]
            ]

            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    Html.div [
                        prop.className "swt:grid swt:grid-cols-[minmax(0,1fr)_9rem] swt:gap-2 swt:items-center"
                        prop.children [
                            Html.div [
                                prop.className "swt:text-sm swt:font-medium"
                                prop.text "Assets"
                            ]

                            Html.select [
                                prop.testId "assign-note-assets-all-destination"
                                prop.ariaLabel "Set all asset destinations"
                                prop.className "swt:select swt:select-bordered swt:select-sm swt:w-full"
                                prop.disabled isAssigning
                                prop.valueOrDefault headerSelectedValue
                                prop.onChange (fun value ->
                                    match tryParseDestinationValue value with
                                    | Some selectedDestination ->
                                        for asset in assets do
                                            setAssetDestination asset.SourceRelativePath selectedDestination
                                    | None -> ()
                                )
                                prop.children (destinationOptions (headerSelectedValue = mixedHeaderValue))
                            ]
                        ]
                    ]

                    for asset in assets do
                        let selectedValue = selectedValueForAsset asset

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
                                    prop.ariaLabel $"Set destination for {asset.RelativeAssetPath}"
                                    prop.className "swt:select swt:select-bordered swt:select-sm swt:w-full"
                                    prop.disabled isAssigning
                                    prop.valueOrDefault selectedValue
                                    prop.onChange (fun value ->
                                        match tryParseDestinationValue value with
                                        | Some selectedDestination ->
                                            setAssetDestination asset.SourceRelativePath selectedDestination
                                        | None -> ()
                                    )
                                    prop.children (destinationOptions false)
                                ]
                            ]
                        ]
                ]
            ]
