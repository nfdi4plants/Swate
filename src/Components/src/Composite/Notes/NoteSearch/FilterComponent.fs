namespace ARCitISA.Components.Composite.NoteSearch.FilterComponent

open Feliz
open ARCitISA.Components
open ARCitISA.Components.Primitive
open ARCitISA.Components.Primitive.Select
open ARCitISA.Components.Primitive.Select.Types
open ARCitISA.Components.Composite.Notes.Types


module Main =
    let noteSuggestionsList (searchTerm: string, selectedIndices: Set<int>, notes: Note list) =
        let notesFilteredAfterTitle =
            if Set.contains 0 selectedIndices || Set.isEmpty selectedIndices then
                ARCitISA.Components.Composite.NoteSearch.FilterLogic.FuzzySearch.search (searchTerm, "Title", notes)
            else
                []

        let notesFilteredAfterRest =
            ARCitISA.Components.Composite.NoteSearch.FilterLogic.ExactMatchSearch.search (
                searchTerm,
                selectedIndices,
                notes
            )

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

        MultiSelect.MultiSelect(
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
