namespace Swate.Components

open System
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Swate.Components.NoteTypes

module noteSearchTests =

    let notes: NoteSearch list = [
        {
            RelativePath = "notes/10_02_2026/Grocery_Planning.md"
            Title = "Grocery Planning"
            Date = DateTime(2026, 2, 10)
            Tags = [| "Planning"; "Food"; "Weekly" |]
            Content =
                "I need to prepare a proper grocery list for the week. We are running low on vegetables and fresh fruit. I also want to try cooking a new pasta recipe. Remember to check if we still have olive oil and spices. It might be worth buying extra rice in bulk. I should compare prices between the local store and the supermarket. Don't forget snacks for movie night."
        }
        {
            RelativePath = "notes/12_02_2026/Project_Ideas_for_Side_App.md"
            Title = "Project Ideas for Side App"
            Date = DateTime(2026, 2, 12)
            Tags = [| "Development"; "Software" |]
            Content =
                "I have been thinking about building a lightweight note search engine. The app should support tagging and full text search. It would be nice to experiment with fuzzy matching. Maybe I can implement ranking based on keyword frequency. I should also consider how to store notes efficiently. Performance testing will be important once the dataset grows. A small UI prototype in Feliz could help validate the concept."
        }
        {
            RelativePath = "notes/14_02_2026/Workout_Routine_Update.md"
            Title = "Workout Routine Update"
            Date = DateTime(2026, 2, 14)
            Tags = [| "Fitness"; "Health"; "Routine" |]
            Content =
                "This week I want to adjust my workout schedule. Strength training should be prioritized over cardio. I will focus on compound lifts like squats and deadlifts. Rest days are important for recovery. Tracking progress in a simple log could help. Nutrition also plays a major role in performance. I should increase protein intake slightly."
        }
        {
            RelativePath = "notes/16_02_2026/Books_to_Read.md"
            Title = "Books to Read"
            Date = DateTime(2026, 2, 16)
            Tags = [| "Education"; "Reading" |]
            Content =
                "There are several books I want to read this year. I am especially interested in software architecture topics. Clean code practices are always worth revisiting. I also want to explore a few science fiction novels. Reading before bed helps reduce screen time. Maybe I should join an online book club. Keeping short summaries of each book would help retention."
        }
        {
            RelativePath = "notes/18_02_2026/Travel_Planning.md"
            Title = "Travel Planning"
            Date = DateTime(2026, 2, 18)
            Tags = [| "Travel"; "Leisure"; "Budget" |]
            Content =
                "I am considering a short trip during the summer. A quiet place near the ocean sounds relaxing. Budget planning needs to be done in advance. I should look for affordable flights soon. Packing light will make travel easier. It would be nice to explore local food markets. Taking plenty of photos is a must."
        }
        {
            RelativePath = "notes/20_02_2026/Learning_Goals.md"
            Title = "Learning Goals"
            Date = DateTime(2026, 2, 20)
            Tags = [| "Learning"; "FunctionalProgramming"; "Goals" |]
            Content =
                "This month I want to deepen my knowledge of functional programming. Practicing F# daily will help reinforce concepts. I should review discriminated unions and pattern matching. Building small sample projects is better than only reading theory. Understanding performance tradeoffs is also important. Writing blog posts about what I learn could clarify my thinking. Consistency matters more than intensity."
        }
        {
            RelativePath = "notes/22_02_2026/Home_Office_Improvements.md"
            Title = "Home Office Improvements"
            Date = DateTime(2026, 2, 22)
            Tags = [| "Productivity"; "HomeOffice" |]
            Content =
                "My home office setup could use some improvements. A better chair would improve posture during long coding sessions. Cable management is currently a mess. Adding a small plant might make the space more inviting. Proper lighting reduces eye strain. I should reorganize the desk drawers this weekend. A second monitor might increase productivity."
        }
    ]

module NoteSearchComponent =

    let private containsIgnoreCase (needle: string) (haystack: string) =
        haystack.ToLowerInvariant().Contains(needle.ToLowerInvariant())

    let filterNotes (searchTerm: string) (filterOptions: string list) (notes: NoteSearch list) =
        match filterOptions with
        | "Title" -> notes |> List.filter (fun note -> containsIgnoreCase searchTerm note.Title)
        | "Content" -> notes |> List.filter (fun note -> containsIgnoreCase searchTerm note.Content)
        | "Tags" ->
            notes
            |> List.filter (fun note -> note.Tags |> Array.exists (containsIgnoreCase searchTerm))
        | _ ->
            notes
            |> List.filter (fun note ->
                containsIgnoreCase searchTerm note.Title
                || containsIgnoreCase searchTerm note.Content
                || (note.Tags |> Array.exists (containsIgnoreCase searchTerm))
            )

    let private createContentPreview (note: NoteSearch) =
        if note.Content.Length > 45 then
            note.Content.Substring(0, 45) + "..."
        else
            note.Content

    let searchInput
        (
            setSearchTerm,
            setStartSearch,
            dropdownOpen: bool,
            setDropdownOpen: bool -> unit,
            filterOptions: list<string>,
            setFilterOptions
        ) =
        Html.div [
            prop.className "swt:w-full swt:mt-4 swt:join"
            prop.children [
                Html.div [
                    prop.className "swt:relative swt:flex-1 swt:join-item"
                    prop.children [
                        Html.input [
                            prop.className "swt:input swt:border-current swt:w-full swt:pr-9 swt:join-item"
                            prop.placeholder "Search Notes..."
                            prop.onClick (fun _ -> setStartSearch true)
                            prop.onChange (fun (ev: Browser.Types.Event) ->
                                let value: string = ev.target?value
                                setSearchTerm value
                                setStartSearch true
                            )
                        ]
                        Html.div [
                            prop.className
                                "swt:absolute swt:right-3 swt:top-1/2 swt:-translate-y-1/2 swt:pointer-events-none"
                            prop.children [ Icons.MagnifyingClass() ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "swt:join-item swt:relative swt:w-20"
                    prop.children [
                        Html.button [
                            prop.text ("Search in ")
                            prop.className (
                                "swt:btn swt:btn-primary swt:join-item swt:border swt:border-current"
                                + if dropdownOpen then " swt:rounded-b-none" else ""
                            )
                            prop.onClick (fun e ->
                                e.stopPropagation ()
                                setDropdownOpen (not dropdownOpen)
                            )
                        ]
                        if dropdownOpen then
                            Html.div [
                                prop.className
                                    "swt:absolute swt:right-0 swt:top-full swt:bg-base-100 swt:border swt:border-current swt:rounded-b swt:z-10 swt:min-w-full swt:flex swt:flex-col"
                                prop.children [
                                    Html.button [
                                        prop.className
                                            "swt:px-4 swt:py-2 swt:text-sm swt:text-left swt:hover:bg-base-200"
                                        prop.text "All"
                                        prop.onClick (fun _ ->
                                            setDropdownOpen false
                                            setFilterOptions "all"
                                        )
                                    ]
                                    Html.button [
                                        prop.className
                                            "swt:px-4 swt:py-2 swt:text-sm swt:text-left swt:hover:bg-base-200"
                                        prop.text "Title"
                                        prop.onClick (fun _ ->
                                            setDropdownOpen false
                                            setFilterOptions "title"
                                        )
                                    ]
                                    Html.button [
                                        prop.className
                                            "swt:px-4 swt:py-2 swt:text-sm swt:text-left swt:hover:bg-base-200"
                                        prop.text "Tags"
                                        prop.onClick (fun _ ->
                                            setDropdownOpen false
                                            setFilterOptions "tags"
                                        )
                                    ]
                                    Html.button [
                                        prop.className
                                            "swt:px-4 swt:py-2 swt:text-sm swt:text-left swt:hover:bg-base-200"
                                        prop.text "Content"
                                        prop.onClick (fun _ ->
                                            setDropdownOpen false
                                            setFilterOptions "content"
                                        )
                                    ]
                                ]
                            ]
                    ]
                ]
            ]
        ]

    let searchSuggestion (note: NoteSearch, onOpen: string -> unit) =
        Html.div [
            prop.className "swt:p-3"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:justify-between swt:items-center swt:gap-2"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-lg swt:font-bold"
                            prop.text note.Title
                        ]
                        Html.button [
                            prop.className "swt:btn swt:btn-sm swt:btn-primary"
                            prop.text "Open"
                            prop.onClick (fun _ -> onOpen note.RelativePath)
                        ]
                    ]
                ]
                Html.p [
                    prop.className "swt:text-sm swt:text-gray-600"
                    prop.text (note.Date.ToString("yyyy-MM-dd"))
                ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-1 swt:mt-1"
                    prop.children [
                        for tag in note.Tags do
                            Html.span [
                                prop.className "swt:text-sm swt:text-gray-500 swt:border swt:border-current swt:inline-block swt:px-2 swt:py-1 swt:rounded"
                                prop.text tag
                            ]
                    ]
                ]
                Html.p [
                    prop.className "swt:mt-2"
                    prop.text (createContentPreview note)
                ]
            ]
        ]

[<Erase; Mangle(false)>]
type SearchComponent =

    [<ReactComponent>]
    static member Main(notes: NoteSearch list, isLoading: bool, error: string option, onOpen: string -> unit) =
        let startSearch, setStartSearch = React.useState false
        let searchTerm, setSearchTerm = React.useState ""
        let dropdownOpen, setDropdownOpen = React.useState false
        let filterOptions, setFilterOptions = React.useState []

        let searchResults =
            if startSearch then
                NoteSearchComponent.filterNotes searchTerm filterOptions notes
            else
                []

        Html.div [
            prop.className "swt:flex swt:flex-col swt:items-center swt:pt-8 swt:min-h-screen"
            prop.onClick (fun _ ->
                setStartSearch false
                setDropdownOpen false
            )
            prop.children [
                Html.div [
                    prop.className "swt:w-full swt:max-w-md"
                    prop.onClick (fun e -> e.stopPropagation ())
                    prop.children [
                        NoteSearchComponent.searchInput (
                            setSearchTerm,
                            setStartSearch,
                            dropdownOpen,
                            setDropdownOpen,
                            filterOptions,
                            setFilterOptions
                        )
                        if startSearch then
                            if isLoading then
                                Html.div [
                                    prop.className "swt:mt-2 swt:text-center"
                                    prop.text "Loading notes..."
                                ]
                            elif error.IsSome then
                                Html.div [
                                    prop.className "swt:mt-2 swt:text-center swt:text-error"
                                    prop.text error.Value
                                ]
                            elif not searchResults.IsEmpty then
                                Html.div [
                                    prop.className
                                        "swt:border swt:border-current swt:rounded-md swt:mt-2 swt:bg-base-100 swt:shadow-md swt:divide-y swt:divide-current"
                                    prop.children [
                                        for note in searchResults do
                                            NoteSearchComponent.searchSuggestion (note, onOpen)
                                    ]
                                ]
                            else
                                Html.div [
                                    prop.className "swt:mt-2 swt:text-center"
                                    prop.text "No results found."
                                ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member Entry() =
        SearchComponent.Main(
            noteSearchTests.notes,
            false,
            None,
            (fun relativePath -> window.alert $"Open note: {relativePath}")
        )
