namespace Components

open Fable.Core
open Feliz
open Feliz.DaisyUI

type Term = {
    Name: string
    Id: string
    IsObsolete: bool
    Href: string option
} with
    static member init(?name, ?id, ?obsolete: bool, ?href) = {
        Name = defaultArg name ""
        Id = defaultArg id ""
        IsObsolete = defaultArg obsolete false
        Href = defaultArg href None
    }

type SearchCalls = ResizeArray<string -> JS.Promise<ResizeArray<Term>>>

type TermSearchResult = {
    Term: Term
    IsDirectedSearchResult: bool
} with
    static member addSearchResults (prevResults: ResizeArray<TermSearchResult>) (newResults: ResizeArray<TermSearchResult>) =
        for newResult in newResults do
            // check if new result is already in the list by id
            let index = prevResults.FindIndex(fun x -> x.Term.Id = newResult.Term.Id)
            // if it exists and the newResult is result of directedSearch, we update the item
            // Directed search normally takes longer to complete but is additional information
            // so we update non-directed search results with the directed search results
            if index >= 0 && newResult.IsDirectedSearchResult then
                prevResults.[index] <- newResult
            else
                // if it does not exist, we add it to the results
                prevResults.Add(newResult)
        prevResults

module private API =

    module Mocks =

        // default search, should always be runnable
        let callSearch = fun (query: string) ->
            promise {
                do! Promise.sleep 1500
                //Init mock data for about 10 items
                return ResizeArray [|
                    { Term = Term.init("Term 1", "1", false, Some "/term/1"); IsDirectedSearchResult = false }
                    { Term = Term.init("Term 2", "2", false, Some "/term/2"); IsDirectedSearchResult = false }
                    { Term = Term.init("Term 3", "3", false, Some "/term/3"); IsDirectedSearchResult = false }
                    { Term = Term.init("Term 4", "4", false, Some "/term/4"); IsDirectedSearchResult = false }
                    { Term = Term.init("Term 5", "5", true, Some "/term/5"); IsDirectedSearchResult = false }
                |]
            }

        // search with parent, is run in parallel to default search,
        // better results, but slower and requires parent
        let callParentSearch = fun (parent: string option) (query: string) ->
            if parent.IsSome then
                promise {
                    do! Promise.sleep 1500
                    //Init mock data for about 10 items
                    return ResizeArray [|
                        { Term = Term.init("Term 1", "1", false, Some "/term/1"); IsDirectedSearchResult = true }
                    |]
                }
            else
                promise {
                    return ResizeArray()
                }

        // search all children of parent without actual query. Quite fast
        // Only triggered onDoubleClick into empty input
        let callAllChildSearch = fun (parent: string) ->
            promise {
                do! Promise.sleep 1500
                //Init mock data for about 10 items
                return [|
                    for i in 0 .. 100 do
                        { Term = Term.init(sprintf "Child %d" i, i.ToString(), i % 5 = 0, Some(sprintf "/term/%d" i)); IsDirectedSearchResult = true }
                |]
            }

[<Mangle(false); Erase>]
type TermSearchV2 =

    static member private TermDropdown(searchResults: ResizeArray<TermSearchResult>, setSearchResults) =
        Html.div [
            prop.className "flex flex-col w-full absolute z-10 top-[100%] left-0 right-0 bg-white shadow-lg rounded-md divide-y-2"
            prop.children [
                Html.button [
                    prop.className "w-full"
                    prop.onClick(fun _ ->
                        setSearchResults(fun _ -> ResizeArray())
                    )
                    prop.text "Clear"
                ]
                for res in searchResults do
                    Html.div [
                        Html.text res.Term.Name
                        Html.text res.Term.Id
                        if res.Term.IsObsolete then
                            Html.text "Obsolete"
                        if res.IsDirectedSearchResult then
                            Html.text "Directed"
                    ]
            ]
        ]

    [<ExportDefaultAttribute; NamedParams>]
    static member TermSearch(?parentId: string, ?termSearchQueries: SearchCalls) =
        let (searchResults: ResizeArray<TermSearchResult>), setSearchResults = React.useStateWithUpdater(ResizeArray())
        let loading, setLoading = React.useState(false)
        let termSearchFunc = fun (query: string) ->
            promise {
                // default search
                setLoading(true)
                let! defaultTermSearchResults = API.Mocks.callSearch query
                setSearchResults(fun prevResults -> TermSearchResult.addSearchResults prevResults defaultTermSearchResults)
                if termSearchQueries.IsSome then
                    for termSearch in termSearchQueries.Value do
                        let! termSearchResults = termSearch query |> Promise.map (fun t -> t.ConvertAll(fun t0 -> {Term = t0; IsDirectedSearchResult = false}))
                        setSearchResults(fun prevResults -> TermSearchResult.addSearchResults prevResults termSearchResults)
                setLoading(false)
            }
            |> Promise.start

        let cancelSearch, search = React.useDebouncedCallbackWithCancel(termSearchFunc, 500)
        let cancelParentSearch, parentSearch = React.useDebouncedCallbackWithCancel((fun _ -> ()), 500)
        let cancelAllChildSearch, allChildSearch = React.useDebouncedCallbackWithCancel((fun _ -> ()), 500)
        let cancel() =
            cancelSearch()
            cancelParentSearch()
            cancelAllChildSearch()
        let startSearch = fun (query: string) ->
            search query
            parentSearch query

        Html.label [
            prop.className "input input-bordered flex flex-row items-center relative"
            prop.children [
                Html.input [
                    prop.placeholder "..."
                    prop.onChange (fun (e: string) ->
                        log e
                        search e
                    )
                ]
                Daisy.loading [
                    prop.className [
                        if not loading then "invisible";
                    ]
                ]
                TermSearchV2.TermDropdown(searchResults, setSearchResults)
            ]
        ]