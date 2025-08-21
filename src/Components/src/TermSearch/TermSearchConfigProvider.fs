namespace Swate.Components

open Swate.Components.Shared
open Swate.Components
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Feliz.DaisyUI


module private TermSearchConfigProviderHelper =

    [<Literal>]
    let TIB_PREFIX = "TIB_"

    [<Literal>]
    let TIB_DATAPLANT_COLLECTION_KEY = "DataPLANT"

    let mkTIBQueries (collections: Set<string>) = {|
        TermSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: Swate.Components.Types.SearchCall =
                        fun (q: string) -> Swate.Components.Api.TIBApi.defaultSearch (q, 10, c)

                    yield (n, query)
            ]
        ParentSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: Swate.Components.Types.ParentSearchCall =
                        fun (parent: string, query: string) ->
                            Swate.Components.Api.TIBApi.searchChildrenOf (query, parent, 10, c)

                    yield (n, query)
            ]
        AllChildrenSearch =
            ResizeArray [
                for c in collections do
                    let n = TIB_PREFIX + c

                    let query: Swate.Components.Types.AllChildrenSearchCall =
                        fun (p: string) -> Swate.Components.Api.TIBApi.searchAllChildrenOf (p, 300, collection = c)

                    yield (n, query)
            ]
    |}

open TermSearchConfigProviderHelper

[<Erase; Mangle(false)>]
type TermSearchConfigProvider =

    [<ReactComponent>]
    static member TIBQueryProvider(children: ReactElement) =

        let allTermSearchQueries, setAllTermSearchQueries =
            React.useState<ResizeArray<string * SearchCall>> (fun () -> ResizeArray())

        let allParentSearchQueries, setAllParentSearchQueries =
            React.useState<ResizeArray<string * ParentSearchCall>> (fun () -> ResizeArray())

        let allAllChildrenSearchQueries, setAllAllChildrenSearchQueries =
            React.useState<ResizeArray<string * AllChildrenSearchCall>> (fun () -> ResizeArray())

        React.useEffect (
            (fun _ -> // get all currently supported catalogues
                promise {
                    console.log "Fetching TIB collections..."

                    let! collections =
                        Api.TIBApi.getCollections ()
                        |> Promise.catch (fun ex -> console.error "Error fetching TIB collections:" ex)

                    let collectionSet = Set.ofArray collections.content
                    let tibQueries = TermSearchConfigProviderHelper.mkTIBQueries collectionSet
                    setAllTermSearchQueries (ResizeArray(Seq.append allTermSearchQueries tibQueries.TermSearch))

                    setAllParentSearchQueries (ResizeArray(Seq.append allParentSearchQueries tibQueries.ParentSearch))

                    setAllAllChildrenSearchQueries (
                        ResizeArray(Seq.append allAllChildrenSearchQueries tibQueries.AllChildrenSearch)
                    )
                }
                |> Promise.start
            ),
            [||]
        )

        TermSearchConfigProvider.TermSearchConfigProvider(
            children,
            allTermSearchQueries,
            allParentSearchQueries,
            allAllChildrenSearchQueries,
            defaultActive = Set [ TIB_PREFIX + TIB_DATAPLANT_COLLECTION_KEY ]
        )

    // TermSearchConfigProvider.TermSearchConfigProvider(
    //     children,
    //     allTermSearchQueries,
    //     allParentSearchQueries,
    //     allAllChildrenSearchQueries,
    //     defaultActive = Set [ TIB_PREFIX + TIB_DATAPLANT_COLLECTION_KEY ]
    // )


    [<ReactComponent(true)>]
    static member TermSearchConfigProvider
        (
            children: ReactElement,
            allTermSearchQueries: ResizeArray<string * SearchCall>,
            allParentSearchQueries: ResizeArray<string * ParentSearchCall>,
            allAllChildrenSearchQueries: ResizeArray<string * AllChildrenSearchCall>,
            ?defaultActive: Set<string>,
            ?localStorageKey: string
        ) =
        let localStorageKey = defaultArg localStorageKey "swate-termsearchconfig-ctx"

        let (activeKeys: TermSearchConfigLocalStorageActiveKeysCtx), setActiveKeys =
            React.useLocalStorage (
                localStorageKey,
                TermSearchConfigLocalStorageActiveKeysCtx.init (?defaultActive = defaultActive)
            )

        let allKeys =
            React.useMemo (
                (fun () ->

                    console.log ("calculate all keys")

                    let allKeys =
                        [
                            yield! allTermSearchQueries |> Seq.map fst
                            yield! allParentSearchQueries |> Seq.map fst
                            yield! allAllChildrenSearchQueries |> Seq.map fst
                        ]
                        |> Set.ofList

                    allKeys
                ),
                [|
                    box allTermSearchQueries
                    box allParentSearchQueries
                    box allAllChildrenSearchQueries
                |]
            )

        /// This is used for memoization
        let activeKeysString = activeKeys.aktiveKeys |> Array.sort |> String.concat "; "

        let queries =
            React.useMemo (
                (fun () ->

                    console.log ("calculate queries")

                    let termSearchQueries =
                        allTermSearchQueries
                        |> Seq.filter (fun (key, _) ->
                            let isActive = activeKeys.aktiveKeys |> Set.ofSeq |> Set.contains key

                            isActive
                        )
                        |> ResizeArray

                    let parentSearchQueries =
                        allParentSearchQueries
                        |> Seq.filter (fun (key, _) -> activeKeys.aktiveKeys |> Set.ofSeq |> Set.contains key)
                        |> ResizeArray

                    let allChildrenSearchQueries =
                        allAllChildrenSearchQueries
                        |> Seq.filter (fun (key, _) -> activeKeys.aktiveKeys |> Set.ofSeq |> Set.contains key)
                        |> ResizeArray

                    {
                        hasProvider = true
                        disableDefault = activeKeys.disableDefault
                        termSearchQueries = termSearchQueries
                        parentSearchQueries = parentSearchQueries
                        allChildrenSearchQueries = allChildrenSearchQueries
                    }
                ),
                [|
                    activeKeysString
                    activeKeys.disableDefault
                    allTermSearchQueries
                    allParentSearchQueries
                    allAllChildrenSearchQueries
                |]
            )

        React.contextProvider (
            Contexts.TermSearch.TermSearchActiveKeysCtx,
            {
                data = activeKeys
                setData = setActiveKeys
            },
            React.contextProvider (
                Contexts.TermSearch.TermSearchConfigCtx,
                queries,
                React.contextProvider (Contexts.TermSearch.TermSearchAllKeysCtx, allKeys, children)
            )
        )