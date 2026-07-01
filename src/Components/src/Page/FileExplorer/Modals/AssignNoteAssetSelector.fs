namespace Swate.Components.Page.FileExplorer.Modals

open Fable.Core
open Feliz
open Swate.Components.Page.FileExplorer.Types

module private AssignNoteAssetSelectorHelper =

    [<Literal>]
    let MixedHeaderValue = "__mixed__"

    let selectedValueForAsset (assetDestinations: Map<string, AssignNoteAssetDestination>) asset =
        assetDestinations
        |> Map.tryFind asset.SourceRelativePath
        |> Option.map string
        |> Option.defaultValue ""

    let tryParseDestinationValue (availableDestinations: AssignNoteAssetDestination list) value =
        if value = MixedHeaderValue then
            None
        elif value = "" then
            Some None
        else
            availableDestinations
            |> List.tryFind (fun destination -> string destination = value)
            |> Option.map Some

    let headerSelectedValue assets assetDestinations =
        let distinctSelectedValues =
            assets
            |> Seq.map (selectedValueForAsset assetDestinations)
            |> Seq.distinct
            |> Seq.toList

        match distinctSelectedValues with
        | [ selectedValue ] -> selectedValue
        | _ -> MixedHeaderValue

[<Erase; Mangle(false)>]
type AssignNoteAssetSelector =

    [<ReactComponent>]
    static member DestinationOptions(availableDestinations: AssignNoteAssetDestination list, includeMixedOption: bool) =
        React.Fragment [
            if includeMixedOption then
                Html.option [
                    prop.value AssignNoteAssetSelectorHelper.MixedHeaderValue
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

    [<ReactComponent>]
    static member Header
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
            let headerSelectedValue =
                AssignNoteAssetSelectorHelper.headerSelectedValue assets assetDestinations

            let tryParseDestinationValue =
                AssignNoteAssetSelectorHelper.tryParseDestinationValue availableDestinations

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
                        prop.children [
                            AssignNoteAssetSelector.DestinationOptions(
                                availableDestinations,
                                (headerSelectedValue = AssignNoteAssetSelectorHelper.MixedHeaderValue)
                            )
                        ]
                    ]
                ]
            ]

    [<ReactComponent>]
    static member Rows
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
            let selectedValueForAsset =
                AssignNoteAssetSelectorHelper.selectedValueForAsset assetDestinations

            let tryParseDestinationValue =
                AssignNoteAssetSelectorHelper.tryParseDestinationValue availableDestinations

            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2"
                prop.children [
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
                                    prop.children [
                                        AssignNoteAssetSelector.DestinationOptions(availableDestinations, false)
                                    ]
                                ]
                            ]
                        ]
                ]
            ]

    [<ReactComponent>]
    static member AssignNoteAssetSelector
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
                    AssignNoteAssetSelector.Header(
                        assets,
                        availableDestinations,
                        assetDestinations,
                        setAssetDestination,
                        isAssigning
                    )

                    AssignNoteAssetSelector.Rows(
                        assets,
                        availableDestinations,
                        assetDestinations,
                        setAssetDestination,
                        isAssigning
                    )
                ]
            ]
