namespace Swate.Components

open Swate.Components.NoteTypes
open Fable.Core
open Fable.Core.JsInterop
open ARCtrl
open Feliz

module noteSearchTests =
    let notes: NoteSearch list = [
        {
            Title = "Grocery Planning"
            Date = System.DateTime(2026, 2, 10)
            Tags = Some(ResizeArray [ OntologyAnnotation("Planning", "http://example.com/ontology/planning"); OntologyAnnotation("Food", "http://example.com/ontology/food"); OntologyAnnotation("Weekly", "http://example.com/ontology/weekly") ])
            Content =
                "I need to prepare a proper grocery list for the week. We are running low on vegetables and fresh fruit. I also want to try cooking a new pasta recipe. Remember to check if we still have olive oil and spices. It might be worth buying extra rice in bulk. I should compare prices between the local store and the supermarket. Don’t forget snacks for movie night."
        }
        {
            Title = "Project Ideas for Side App"
            Date = System.DateTime(2026, 2, 12)
            Tags = Some(ResizeArray [ OntologyAnnotation("Development", "http://example.com/ontology/development"); OntologyAnnotation("Software", "http://example.com/ontology/software") ])
            Content =
                "I have been thinking about building a lightweight note search engine. The app should support tagging and full text search. It would be nice to experiment with fuzzy matching. Maybe I can implement ranking based on keyword frequency. I should also consider how to store notes efficiently. Performance testing will be important once the dataset grows. A small UI prototype in Feliz could help validate the concept."
        }
        {
            Title = "Workout Routine Update"
            Date = System.DateTime(2026, 2, 14)
            Tags = Some(ResizeArray [ OntologyAnnotation("Fitness", "http://example.com/ontology/fitness"); OntologyAnnotation("Health", "http://example.com/ontology/health"); OntologyAnnotation("Routine", "http://example.com/ontology/routine") ])
            Content =
                "This week I want to adjust my workout schedule. Strength training should be prioritized over cardio. I will focus on compound lifts like squats and deadlifts. Rest days are important for recovery. Tracking progress in a simple log could help. Nutrition also plays a major role in performance. I should increase protein intake slightly."
        }
        {
            Title = "Books to Read"
            Date = System.DateTime(2026, 2, 16)
            Tags = Some(ResizeArray [ OntologyAnnotation("Education", "http://example.com/ontology/education"); OntologyAnnotation("Reading", "http://example.com/ontology/reading") ])
            Content =
                "There are several books I want to read this year. I am especially interested in software architecture topics. Clean code practices are always worth revisiting. I also want to explore a few science fiction novels. Reading before bed helps reduce screen time. Maybe I should join an online book club. Keeping short summaries of each book would help retention."
        }
        {
            Title = "Travel Planning"
            Date = System.DateTime(2026, 2, 18)
            Tags = Some(ResizeArray [ OntologyAnnotation("Travel", "http://example.com/ontology/travel"); OntologyAnnotation("Leisure", "http://example.com/ontology/leisure"); OntologyAnnotation("Budget", "http://example.com/ontology/budget") ])
            Content =
                "I am considering a short trip during the summer. A quiet place near the ocean sounds relaxing. Budget planning needs to be done in advance. I should look for affordable flights soon. Packing light will make travel easier. It would be nice to explore local food markets. Taking plenty of photos is a must."
        }
        {
            Title = "Learning Goals"
            Date = System.DateTime(2026, 2, 20)
            Tags = Some(ResizeArray [ OntologyAnnotation("Learning", "http://example.com/ontology/learning"); OntologyAnnotation("FunctionalProgramming", "http://example.com/ontology/functional-programming"); OntologyAnnotation("Goals", "http://example.com/ontology/goals") ])
            Content =
                "This month I want to deepen my knowledge of functional programming. Practicing F# daily will help reinforce concepts. I should review discriminated unions and pattern matching. Building small sample projects is better than only reading theory. Understanding performance tradeoffs is also important. Writing blog posts about what I learn could clarify my thinking. Consistency matters more than intensity."
        }
        {
            Title = "Home Office Improvements"
            Date = System.DateTime(2026, 2, 22)
            Tags = Some(ResizeArray [ OntologyAnnotation("Productivity", "http://example.com/ontology/productivity"); OntologyAnnotation("HomeOffice", "http://example.com/ontology/home-office") ])
            Content =
                "My home office setup could use some improvements. A better chair would improve posture during long coding sessions. Cable management is currently a mess. Adding a small plant might make the space more inviting. Proper lighting reduces eye strain. I should reorganize the desk drawers this weekend. A second monitor might increase productivity."
        }
    ]

module NoteSearchComponent =
    let searchInput (setSearchTerm, setStartSearch, dropdownOpen: bool, setDropdownOpen: bool -> unit, filterOptions: string, setFilterOptions) =
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
                            prop.className "swt:absolute swt:right-3 swt:top-1/2 swt:-translate-y-1/2 swt:pointer-events-none"
                            prop.children [ Icons.MagnifyingClass() ]
                        ]
                    ]
                ]

                Html.div [
                    prop.className "swt:join-item swt:relative swt:w-20"
                    prop.children [
                        Html.button [
                            prop.text filterOptions
                            prop.className ("swt:btn swt:btn-primary swt:join-item swt:border swt:border-current swt:w-full" + if dropdownOpen then " swt:rounded-b-none" else "")
                            prop.onClick (fun e ->
                                e.stopPropagation()
                                setDropdownOpen (not dropdownOpen)
                            )
                        ]
                        if dropdownOpen then
                            Html.div [
                                prop.className "swt:absolute swt:right-0 swt:top-full swt:bg-base-100 swt:border swt:border-current swt:rounded-b swt:z-10 swt:min-w-full swt:flex swt:flex-col"
                                prop.children [
                                    Html.button [
                                        prop.className "swt:px-4 swt:py-2 swt:text-sm swt:text-left swt:hover:bg-base-200"
                                        prop.text "All"
                                        prop.onClick (fun _ -> setDropdownOpen false; setFilterOptions "All")
                                    ]
                                    Html.button [
                                        prop.className "swt:px-4 swt:py-2 swt:text-sm swt:text-left swt:hover:bg-base-200"
                                        prop.text "Title"
                                        prop.onClick (fun _ -> setDropdownOpen false; setFilterOptions "Title")
                                    ]
                                    Html.button [
                                        prop.className "swt:px-4 swt:py-2 swt:text-sm swt:text-left swt:hover:bg-base-200"
                                        prop.text "Tags"
                                        prop.onClick (fun _ -> setDropdownOpen false; setFilterOptions "Tags")
                                    ]
                                    Html.button [
                                        prop.className "swt:px-4 swt:py-2 swt:text-sm swt:text-left swt:hover:bg-base-200"
                                        prop.text "Content"
                                        prop.onClick (fun _ -> setDropdownOpen false; setFilterOptions "Content")
                                    ]
                                ]
                            ]
                    ]
                ]
            ]
        ]
    let searchSuggestion (note: NoteSearch, contentPreview: string) =
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
                            prop.onClick (fun _ ->
                                Browser.Dom.window.alert (
                                    sprintf
                                        "Title: %s\nDate: %s\nTags: %s\nContent:\n\n%s"
                                        note.Title
                                        (note.Date.ToString("yyyy-MM-dd"))
                                        (note.Tags
                                        |> Option.map (fun tags -> tags |> Seq.map (fun tag -> tag.NameText) |> String.concat ", ")
                                        |> Option.defaultValue "")
                                        note.Content
                                )
                            //replace with actual note opening logic / opening note editing component
                            )
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
                        for tag in (note.Tags |> Option.defaultValue (ResizeArray [])) do
                            Html.span [
                                prop.className "swt:text-sm swt:text-gray-500 swt:border swt:border-current swt:inline-block swt:px-2 swt:py-1 swt:rounded"
                                prop.text tag.NameText
                            ]
                    ]
                ]
                Html.p [
                    prop.className "swt:mt-2 ";
                    prop.text contentPreview ]

            ]
        ]


[<Erase; Mangle(false)>]

type SearchComponent =

    [<ReactComponent>]
    static member Entry() =

        let startSearch, setStartSearch = React.useState (false)
        let searchTerm, setSearchTerm = React.useState ("")
        let dropdownOpen, setDropdownOpen = React.useState (false)
        let filterOptions, setFilterOptions = React.useState ("All")

        Html.div [
            prop.className "swt:flex swt:flex-col swt:items-center swt:pt-8 swt:min-h-screen"
            prop.onClick (fun _ ->
                setStartSearch false
                setDropdownOpen false
            )
            prop.children [
                Html.div [
                    prop.className "swt:w-full swt:max-w-md"
                    prop.onClick (fun e -> e.stopPropagation())
                    prop.children [
                        NoteSearchComponent.searchInput (setSearchTerm, setStartSearch, dropdownOpen, setDropdownOpen, filterOptions, setFilterOptions)
                        if startSearch then
                            let searchResults =
                                match filterOptions with
                                | "Title" ->
                                    noteSearchTests.notes
                                    |> List.filter (fun note -> note.Title.ToLower().Contains(searchTerm.ToLower()))
                                | "Content" ->
                                    noteSearchTests.notes
                                    |> List.filter (fun note -> note.Content.ToLower().Contains(searchTerm.ToLower()))
                                | "Tags" ->
                                    noteSearchTests.notes
                                    |> List.filter (fun note ->
                                        note.Tags
                                        |> Option.exists (fun tags ->
                                            tags
                                            |> Seq.exists (fun tag -> tag.NameText.ToLower().Contains(searchTerm.ToLower()))
                                        )
                                    )
                                | _ ->
                                    noteSearchTests.notes
                                    |> List.filter (fun note ->
                                        note.Title.ToLower().Contains(searchTerm.ToLower())
                                        || note.Content.ToLower().Contains(searchTerm.ToLower())
                                        || (note.Tags
                                            |> Option.exists (fun tags ->
                                                tags
                                                |> Seq.exists (fun tag -> tag.NameText.ToLower().Contains(searchTerm.ToLower()))
                                            )
                                        )
                                    )

                            if startSearch && not searchResults.IsEmpty then
                                Html.div [
                                    prop.className
                                        "swt:border-2 swt:border-current swt:rounded-md swt:mt-2 swt:bg-base-100 swt:shadow-md swt:divide-y swt:divide-current"
                                    prop.children [
                                        for note in searchResults do
                                            let contentPreview =
                                                if note.Content.Length > 45 then
                                                    note.Content.Substring(0, 45) + "..."
                                                else
                                                    note.Content

                                            NoteSearchComponent.searchSuggestion (note, contentPreview)
                                    ]
                                ]
                            else
                                Html.div [
                                    prop.className "swt:mt-2 swt:text-center"
                                    prop.text "no results found"
                                ]
                    ]
                ]
            ]
        ]