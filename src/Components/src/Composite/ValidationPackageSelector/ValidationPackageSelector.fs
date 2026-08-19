namespace Swate.Components.Composite.ValidationPackageSelector

open Fable.Core
open Feliz
open ARCtrl
open ARCtrl.ValidationPackages
open Types
open Elmish
open Feliz.UseElmish

module private ValidationPackageSelectorModel =

    type State = 
        | Init 
        | Loading
        | Loaded of ValidationPackageDTO []
        | Error of exn

open ValidationPackageSelectorModel

[<Erase; Mangle(false)>]
type ValidationPackageSelector =

    static member private PackagePagination() =
        Html.div "Pagination component goes here"

    [<ReactComponent(true)>]
    static member ValidationPackageSelector
        (
            config: ValidationPackagesConfig,
            writeConfig: ValidationPackagesConfig -> JS.Promise<Result<unit, exn>>,
            fetchValidationPackages: unit -> JS.Promise<ValidationPackageDTO []>,
            ?onError: exn -> unit
        ) =
        
        let state, setState = React.useState(fun () -> Init)
        let input, setInput = React.useState(fun () -> "")

        React.useEffectOnce(fun () ->
            setState Loading
            fetchValidationPackages()
            |> Promise.map (fun packages -> setState (Loaded packages))
            |> Promise.catch (fun ex ->
                setState (Error ex)
                onError |> Option.iter (fun f -> f ex)
            )
            |> Promise.start
        )

        let items =
            match state with
            | Init -> [||]
            | Loading -> [||]
            | Loaded packages -> packages
            | Error _ -> [||]

        let isLoading =
            match state with
            | Loading -> true
            | _ -> false

        let searchFn = fun (search: {| item: ValidationPackageDTO; search: string |}) ->
            let query = search.search.ToLower()
            search.item.Name.ToLower().Contains query

        React.Fragment [
            Html.div [
                match state with
                | Init -> Html.div [ prop.text "Initializing..." ]
                | Loading -> Html.div [ prop.text "Loading validation packages..." ]
                | Loaded packages ->
                    Html.p [
                        prop.text $"Loaded {packages.Length} validation packages."
                    ]
                | Error ex -> Html.div [ prop.text $"Error loading validation packages: {ex.Message}" ]
            ]


        ]
