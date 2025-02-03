namespace Pages.Settings

open Fable
open Feliz
open Messages

type private Catalogues = {
    FoundOnTIB: Set<string>
    Selected: Set<string>
} with
    /// Found on TIB but not selected
    member this.UnusedCatalogues = Set.difference this.FoundOnTIB this.Selected

    /// Selected but not found on TIB
    member this.DisconnectedCatalogues = Set.difference this.Selected this.FoundOnTIB

    member this.AllCatalogues = Set.union this.FoundOnTIB this.Selected

type SearchConfig =
    static member private SwateDefaultSearch(model: Model.Model, dispatch) =
        Html.div [
            Html.h2 [ prop.text "Swate Default Search" ]
            Html.p [ prop.text "Enables search through the community build DPBO ontology and fast updates through our GitHub contribution model." ]
            Html.div [
                prop.className "form-control lg:max-w-md"
                prop.children [
                    Html.label [
                        prop.className "label cursor-pointer"
                        prop.children [
                            Html.span [
                                prop.className "label-text"
                                prop.text "Use Swate Default Search"
                            ]
                            Html.input [
                                prop.onChange (fun (b:bool) -> Messages.PersistentStorage.UpdateSwateDefaultSearch b |> PersistentStorageMsg |> dispatch)
                                prop.className "checkbox checkbox-primary"
                                prop.type'.checkbox
                                prop.isChecked model.PersistentStorageState.SwateDefaultSearch
                                prop.id "swateDefaultSearch"
                            ]
                        ]
                    ]
                ]
            ]
        ]

    /// Element with label and select adding support for search through a single TIB catalogue.
    static member private TIBSearchCatalogueElement(chosen: string, catalogues: Catalogues, select: string -> unit, rmv: unit -> unit) =
        let disconnected = catalogues.DisconnectedCatalogues.Contains chosen
        let disconnectedMsg = "This catalogue is not found on TIB. Get in contact with the support team and remove it for now!"
        Html.div [
            prop.className "flex flex-row gap-2 items-end"
            prop.children [
                Html.label [
                    prop.className "form-control w-full max-w-xs"
                    prop.children [
                        Html.div [
                            prop.className "label"
                            prop.children [
                                Html.span [
                                    prop.className "label-text"
                                    prop.text "Choose your catalogue"
                                ]
                            ]
                        ]
                        Html.select [
                            prop.value chosen
                            prop.onChange (fun e -> select(e))
                            prop.className "select select-bordered"
                            prop.children [
                                for catalogue in catalogues.AllCatalogues do
                                    let disabled = catalogues.DisconnectedCatalogues.Contains catalogue || catalogues.Selected.Contains catalogue
                                    let disconnected = catalogues.DisconnectedCatalogues.Contains catalogue
                                    Html.option [
                                        prop.disabled (disconnected || disabled)
                                        prop.value catalogue
                                        prop.text catalogue
                                        if disconnected then
                                            prop.title disconnectedMsg
                                        prop.className [
                                            if disabled || disconnected then
                                                "bg-base-300 cursor-not-allowed"
                                            if disconnected then
                                                "text-error"
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
                Html.button [
                    prop.className "btn btn-error btn-square"
                    prop.onClick (fun _ -> rmv())
                    prop.children [
                        Html.i [prop.className "fa-solid fa-trash-can"]
                    ]
                ]
                if disconnected then
                    Html.div [
                        prop.className "tooltip"
                        prop.custom("data-tip", disconnectedMsg)
                        prop.children [
                            Html.div [
                                prop.className "relative"
                                prop.children [
                                    Html.div [
                                        prop.className "absolute top-0 right-0 left-0 bottom-0 animate-ping bg-yellow-400"
                                    ]
                                    Html.button [
                                        prop.className "btn btn-warning btn-active btn-square no-animation"
                                        prop.children [
                                            Html.i [prop.className "fa-solid fa-triangle-exclamation"]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
            ]
        ]

    [<ReactComponent>]
    static member private TIBSearch(model: Model.Model, dispatch: Messages.Msg -> unit) =
        let selectedCatalogues = model.PersistentStorageState.TIBSearchCatalogues
        let catalogues, setCatalogues = React.useState([||])
        React.useEffectOnce(fun _ -> // get all currently supported catalogues
            promise {
                let! catalogues = Swate.Components.Api.TIBApi.getCollections()
                setCatalogues catalogues.content
            } |> ignore
        )
        let catalogues: Catalogues =
            React.useMemo((fun () ->
                {
                    FoundOnTIB = catalogues |> Set.ofArray
                    Selected = selectedCatalogues
                }
            ), [|catalogues; selectedCatalogues|])
        Html.div [
            Html.h2 [ prop.text "TIB Search" ]
            Html.p [ prop.text "Adds support for high performance TIB term search. Choose a catalogue of terms to search through." ]
            Html.p [ prop.text "Selecting multiple catalogues may impact search performance." ]
            Html.p [ prop.textf "Selected: %A" (selectedCatalogues |> String.concat ", ") ]
            Html.div [
                prop.className "flex flex-row gap-2"
                prop.children [
                    Html.button [
                        prop.className "btn btn-primary btn-sm"
                        prop.disabled (catalogues.UnusedCatalogues.Count = 0)
                        prop.onClick (fun _ ->
                            if catalogues.UnusedCatalogues.Count <> 0 then
                                Messages.PersistentStorage.AddTIBSearchCatalogue catalogues.UnusedCatalogues.MinimumElement
                                |> PersistentStorageMsg
                                |> dispatch
                        )
                        prop.children [
                            Html.i [prop.className "fa-solid fa-plus"]
                            Html.text "Add"
                        ]
                    ]
                    Html.button [
                        prop.className "btn btn-error btn-sm"
                        prop.disabled (selectedCatalogues.Count = 0)
                        prop.onClick (fun _ ->
                            Messages.PersistentStorage.SetTIBSearchCatalogues Set.empty
                            |> PersistentStorageMsg
                            |> dispatch
                        )
                        prop.children [
                            Html.i [prop.className "fa-solid fa-trash-can"]
                            Html.text "Clear"
                        ]
                    ]
                ]
            ]
            for catalogue in selectedCatalogues do
                let rmv = fun () ->
                    Messages.PersistentStorage.RemoveTIBSearchCatalogue catalogue
                    |> PersistentStorageMsg
                    |> dispatch
                let setter = fun (s: string) ->
                    (selectedCatalogues |> Set.remove catalogue |> Set.add s)
                    |> PersistentStorage.SetTIBSearchCatalogues
                    |> PersistentStorageMsg
                    |> dispatch
                SearchConfig.TIBSearchCatalogueElement (catalogue, catalogues, setter, rmv)
        ]

    static member Main(model, dispatch) =
        React.fragment [
            SearchConfig.SwateDefaultSearch(model, dispatch)
            SearchConfig.TIBSearch(model, dispatch)
        ]