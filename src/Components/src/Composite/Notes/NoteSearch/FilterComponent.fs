namespace Swate.Components.Composite.NoteSearch.FilterComponent

open Feliz
open Swate.Components
open Swate.Components.Primitive
open Swate.Components.Primitive.Select
open Swate.Components.Primitive.Select.Types


module Main =
    let noteSuggestionsList (searchTerm: string, selectedIndices: Set<int>, notes: Swate.Components.Composite.NoteTypes.Note list) =
        let notesFilteredAfterTitle =
            if Set.contains 0 selectedIndices || Set.isEmpty selectedIndices then
                Swate.Components.Composite.NoteSearch.FilterLogic.FuzzySearch.search (searchTerm, "Title", notes)
            else
                []

        let notesFilteredAfterRest =
            Swate.Components.Composite.NoteSearch.FilterLogic.ExactMatchSearch.search (searchTerm, selectedIndices, notes)

        notesFilteredAfterTitle @ notesFilteredAfterRest

    [<ReactComponent>]
    let FilterDropdown (filterOptions: string list, selectedIndices: Set<int>, setSelectedIndices: Set<int> -> unit) =

        let filterFields: SelectItem<string>[] =
            React.useMemo (
                (fun () ->
                    filterOptions
                    |> List.map (fun opt -> {| label = opt; item = opt |})
                    |> List.toArray
                ),
                [| filterOptions |]
            )

        let TriggerRenderFn =
            fun _ ->
                Html.button [
                    prop.title "Filter Notes"
                    prop.type'.button
                    prop.tabIndex -1
                    prop.className "swt:btn swt:btn-square swt:btn-neutral swt:shadow-none swt:pointer-events-none"
                    prop.children [
                        Html.div selectedIndices.Count
                        Icons.Filter(className = "swt:size-4")
                    ]
                ]

        Select.Select(
            filterFields,
            selectedIndices,
            setSelectedIndices,
            triggerRenderFn = TriggerRenderFn,
            dropdownPlacement = FloatingUI.Placement.BottomStart,
            middleware = [|
                FloatingUI.Middleware.flip ()
                FloatingUI.Middleware.offset (10)
            |]
        )

