namespace Swate.Components

open Swate.Components.Shared
open Swate.Components.NoteTypes
open Fable.Core
open Fable.Core.JsInterop
open Feliz

module tests =
    let notes: NoteSearch list = [
        {
            Id = 1
            Title = "Grocery Planning"
            Date = "2026-02-10"
            Content =
                "I need to prepare a proper grocery list for the week. We are running low on vegetables and fresh fruit. I also want to try cooking a new pasta recipe. Remember to check if we still have olive oil and spices. It might be worth buying extra rice in bulk. I should compare prices between the local store and the supermarket. Don’t forget snacks for movie night."
        }
        {
            Id = 2
            Title = "Project Ideas for Side App"
            Date = "2026-02-12"
            Content =
                "I have been thinking about building a lightweight note search engine. The app should support tagging and full text search. It would be nice to experiment with fuzzy matching. Maybe I can implement ranking based on keyword frequency. I should also consider how to store notes efficiently. Performance testing will be important once the dataset grows. A small UI prototype in Feliz could help validate the concept."
        }
        {
            Id = 3
            Title = "Workout Routine Update"
            Date = "2026-02-14"
            Content =
                "This week I want to adjust my workout schedule. Strength training should be prioritized over cardio. I will focus on compound lifts like squats and deadlifts. Rest days are important for recovery. Tracking progress in a simple log could help. Nutrition also plays a major role in performance. I should increase protein intake slightly."
        }
        {
            Id = 4
            Title = "Books to Read"
            Date = "2026-02-16"
            Content =
                "There are several books I want to read this year. I am especially interested in software architecture topics. Clean code practices are always worth revisiting. I also want to explore a few science fiction novels. Reading before bed helps reduce screen time. Maybe I should join an online book club. Keeping short summaries of each book would help retention."
        }
        {
            Id = 5
            Title = "Travel Planning"
            Date = "2026-02-18"
            Content =
                "I am considering a short trip during the summer. A quiet place near the ocean sounds relaxing. Budget planning needs to be done in advance. I should look for affordable flights soon. Packing light will make travel easier. It would be nice to explore local food markets. Taking plenty of photos is a must."
        }
        {
            Id = 6
            Title = "Learning Goals"
            Date = "2026-02-20"
            Content =
                "This month I want to deepen my knowledge of functional programming. Practicing F# daily will help reinforce concepts. I should review discriminated unions and pattern matching. Building small sample projects is better than only reading theory. Understanding performance tradeoffs is also important. Writing blog posts about what I learn could clarify my thinking. Consistency matters more than intensity."
        }
        {
            Id = 7
            Title = "Home Office Improvements"
            Date = "2026-02-22"
            Content =
                "My home office setup could use some improvements. A better chair would improve posture during long coding sessions. Cable management is currently a mess. Adding a small plant might make the space more inviting. Proper lighting reduces eye strain. I should reorganize the desk drawers this weekend. A second monitor might increase productivity."
        }
    ]

module NoteSearchComponent =

    let searchSuggestion (note: NoteSearch, contentPreview: string) =
        Html.div [
            prop.className "swt:bg-base-100 swt:shadow-md swt:rounded-md swt:p-3 swt:border swt:border-current"
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
                            prop.onClick (fun _ ->
                                Browser.Dom.window.alert (
                                    sprintf "Title: %s\nDate: %s\nContent:\n\n%s" note.Title note.Date note.Content
                                )
                            //replace with actual note opening logic / opening note editing component
                            )
                        ]
                    ]
                ]
                Html.p [
                    prop.className "swt:text-sm swt:text-gray-600"
                    prop.text note.Date
                ]
                Html.p [ prop.className "swt:mt-2 "; prop.text contentPreview ]

            ]
        ]

    let searchInput (searchTerm, setSearchTerm, setStartSearch) =
        Html.div [
            prop.className "swt:w-full swt:mt-4 swt:relative"
            prop.children [
                Html.input [
                    prop.className "swt:input swt:border-current swt:w-full swt:pr-12"
                    prop.placeholder "Search Notes..."
                    prop.onClick (fun _ -> setStartSearch true)
                    prop.onChange (fun (ev: Browser.Types.Event) ->
                        let value: string = ev.target?value
                        setSearchTerm value
                        setStartSearch true
                        printf "Searching for: %s" searchTerm

                    )
                    prop.value searchTerm
                ]
                Html.div [
                    prop.className "swt:absolute swt:right-3 swt:top-1/2 swt:-translate-y-1/2"

                    prop.children [ Icons.MagnifyingClass() ]
                ]
            ]
        ]

[<Erase; Mangle(false)>]

type SearchComponent =

    [<ReactComponent>]
    static member Entry() =

        let showSearch, setShowSearch = React.useState (false)
        let startSearch, setStartSearch = React.useState (false)
        let searchTerm, setSearchTerm = React.useState ("")

        Html.div [
            prop.className "swt:flex swt:flex-col swt:items-center swt:pt-8 swt:min-h-screen"
            prop.children [
                Html.button [
                    prop.className "swt:btn swt:btn-primary"
                    prop.onClick (fun _ ->
                        setShowSearch (not showSearch)
                        if showSearch = true then
                            setStartSearch false
                        setSearchTerm ""
                    )
                    prop.text "Toggle Search"
                ]


                if showSearch then
                    Html.div [
                        prop.className "swt:w-full swt:max-w-md"
                        prop.children [
                            NoteSearchComponent.searchInput (searchTerm, setSearchTerm, setStartSearch)
                            if startSearch then
                                let searchResults =
                                    tests.notes
                                    |> List.filter (fun note ->
                                        note.Title.ToLower().Contains(searchTerm.ToLower())
                                        || note.Content.ToLower().Contains(searchTerm.ToLower())
                                    )

                                for note in searchResults do

                                    let contentPreview =
                                        if note.Content.Length > 45 then
                                            note.Content.Substring(0, 45) + "..."
                                        else
                                            note.Content

                                    NoteSearchComponent.searchSuggestion (note, contentPreview)
                        ]
                    ]
            ]
        ]