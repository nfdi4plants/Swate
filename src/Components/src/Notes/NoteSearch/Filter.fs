namespace Swate.Components


open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.NoteTypes



module FilterLogic = //filters the note list based on the search term and the selected filter options.
    [<ImportDefault("fuse.js")>]
    type Fuse(list: obj[], options: obj) =
        [<Emit "$0.search($1)">]
        member _.search(pattern: string) : obj[] = jsNative

    [<Erase>]
    type FuzzySearch =
        static member fuseOptions(filterOption: string) =
            createObj [ "keys" ==> [| filterOption |] ] // Default to searching in the title if no filters are selected
        // | _ -> createObj [ "keys" ==> (filterOptions |> List.toArray) ]

        static member fuse(filterOption, notes) =
            Fuse(notes |> List.toArray |> Array.map (fun x -> x :> obj), FuzzySearch.fuseOptions (filterOption))

        static member search
            (searchPattern: string, filterOption: string, notes)
            : Swate.Components.NoteTypes.Note list =
            FuzzySearch.fuse(filterOption, notes).search (searchPattern)
            |> Array.map (fun (result: obj) -> unbox<Swate.Components.NoteTypes.Note> (result?item))
            |> Array.toList

    type ExactMatchSearch =
        static member private containsIgnoreCase (needle: string) (haystack: string) = //
            haystack.ToLowerInvariant().Contains(needle.ToLowerInvariant())

        static member search
            (searchTerm: string, selectedIndices: Set<int>, notes: Swate.Components.NoteTypes.Note list)
            : Swate.Components.NoteTypes.Note list =
            notes
            |> List.filter (fun note ->

                // let inTitle = ExactMatchSearch.containsIgnoreCase (searchTerm.Trim()) note.Title
                let inContent = ExactMatchSearch.containsIgnoreCase (searchTerm.Trim()) note.Content

                let inTags =
                    match note.Tags with
                    | Some tags ->
                        tags
                        |> Seq.exists (fun tag -> ExactMatchSearch.containsIgnoreCase (searchTerm.Trim()) tag.NameText)
                    | None -> false

                if Set.isEmpty selectedIndices then
                    // Default behavior: no selected filter means search in all fields.
                    if (searchTerm.Trim()) = "" then
                        true
                    else
                        // inTitle ||
                        inContent || inTags
                else
                    // let matchesTitle = List.contains "Title" filterOptions && inTitle
                    let matchesContent = Set.contains 1 selectedIndices && inContent
                    let matchesTags = Set.contains 2 selectedIndices && inTags

                    // matchesTitle ||
                    matchesContent || matchesTags // If multiple filters are selected, a note matches if it satisfies at least one of the selected criteria.
            )

module FilterComponents =
    let noteSuggestions (searchTerm: string) (selectedIndices: Set<int>) (notes: Swate.Components.NoteTypes.Note list) =
        let notesFilteredAfterTitle =
            if Set.contains 0 selectedIndices || selectedIndices.IsEmpty then
                FilterLogic.FuzzySearch.search (searchTerm, "Title", notes)
            else
                []

        let notesFilteredAfterRest =
            FilterLogic.ExactMatchSearch.search (searchTerm, selectedIndices, notes)

        notesFilteredAfterTitle @ notesFilteredAfterRest

    let filterDropdown (filterOptions: string list, selectedIndices: Set<int>, setSelectedIndices: Set<int> -> unit) =

        let filterFields: SelectItem<string>[] = //
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
            selectedIndices, //
            setSelectedIndices,
            triggerRenderFn = TriggerRenderFn,
            dropdownPlacement = FloatingUI.Placement.BottomEnd,
            middleware = [|
                FloatingUI.Middleware.flip ()
                FloatingUI.Middleware.offset (10)
            |]
        )