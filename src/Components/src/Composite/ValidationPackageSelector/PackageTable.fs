namespace Swate.Components.Composite.ValidationPackageSelector

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.ValidationPackageSelector.Context
open Swate.Components.Primitive
open Swate.Components.Primitive.LoadingSpinner
open Swate.Components.Primitive.Select
open Types

[<Erase; Mangle(false)>]
type PackageTable =

    [<ReactComponent>]
    static member private HeaderSelect
        (
            label: string,
            optionNames: string[],
            selectedIndices: Set<int>,
            setSelectedIndices: Set<int> -> unit,
            testId: string
        ) =
        let selected =
            selectedIndices
            |> Set.toList
            |> List.tryHead
            |> Option.map (fun i -> optionNames.[i])

        Select.Select(
            optionNames |> Array.map (fun name -> {| item = name; label = name |}),
            selectedIndices,
            setSelectedIndices,
            showSelectAll = false,
            triggerRenderFn =
                (fun (_: {| isOpen: bool |}) ->
                    Html.div [
                        prop.testId testId
                        prop.className [
                            "swt:flex swt:items-center swt:gap-1 swt:text-xs swt:font-medium swt:uppercase"
                            if selected.IsSome then "swt:text-accent"
                        ]
                        prop.children [
                            Html.span [ prop.text label ]
                            Html.span [
                                prop.className "swt:iconify swt:fluent--filter-20-regular"
                            ]
                            match selected with
                            | Some name ->
                                Html.span [
                                    prop.className "swt:badge swt:badge-xs swt:badge-primary swt:max-md:hidden"
                                    prop.text name
                                ]
                            | None -> Html.none
                        ]
                    ]
                )
        )

    [<ReactComponent(true)>]
    static member PackageTable(items: ValidationPackageDTO[], renderBody: ValidationPackageDTO[] -> ReactElement) =
        let ctx = useValidationPackageSelectorCtx ()

        let selectedTags, setSelectedTags = React.useStateWithUpdater Set.empty<int>
        let selectedAuthors, setSelectedAuthors = React.useStateWithUpdater Set.empty<int>

        // Constrain Select (multi-select) to at most one selected index.
        let setSingleSelection (setter: (Set<int> -> Set<int>) -> unit) (next: Set<int>) =
            setter (fun prev ->
                let added = Set.difference next prev

                if Set.isEmpty added then next else added
            )

        let tagOptions =
            React.useMemo ((fun () -> Helper.distinctTags items), [| box items |])

        let authorOptions =
            React.useMemo ((fun () -> Helper.distinctAuthors items), [| box items |])

        let tagFilterName =
            selectedTags
            |> Set.toList
            |> List.tryHead
            |> Option.map (fun i -> tagOptions.[i])

        let authorFilterName =
            selectedAuthors
            |> Set.toList
            |> List.tryHead
            |> Option.map (fun i -> authorOptions.[i])

        let filtered =
            React.useMemo (
                (fun () ->
                    items
                    |> Helper.filterByTag tagFilterName
                    |> Helper.filterByAuthor authorFilterName
                ),
                [| box items; box tagFilterName; box authorFilterName |]
            )

        Html.table [
            prop.className "swt:table swt:md:table-fixed swt:w-full swt:table-pin-rows"
            prop.children [
                Html.thead [
                    Html.tr [
                        Html.th [ prop.className "swt:w-12" ]
                        Html.th "Name"
                        Html.th [
                            prop.className "swt:max-md:hidden"
                            prop.text "Summary"
                        ]
                        Html.th "Version"
                        Html.th [
                            PackageTable.HeaderSelect(
                                "Tags",
                                tagOptions,
                                selectedTags,
                                setSingleSelection setSelectedTags,
                                "validation-package-selector-tag-filter"
                            )
                        ]
                        Html.th [
                            PackageTable.HeaderSelect(
                                "Authors",
                                authorOptions,
                                selectedAuthors,
                                setSingleSelection setSelectedAuthors,
                                "validation-package-selector-author-filter"
                            )
                        ]
                        Html.th [
                            prop.className "swt:max-md:hidden"
                            prop.text "Released"
                        ]
                        Html.th "Info"
                    ]
                ]
                match ctx.FetchState with
                | SelectorState.Loading
                | SelectorState.Idle ->
                    Html.tbody [
                        Html.tr [
                            Html.td [
                                prop.colSpan 8
                                prop.style [ style.textAlign.center ]
                                prop.children [
                                    LoadingSpinner.LoadingSpinner(
                                        "Loading validation packages...",
                                        size = DaisyuiSize.XL
                                    )
                                ]
                            ]
                        ]
                    ]
                | SelectorState.Error e ->
                    Html.tbody [
                        Html.tr [
                            Html.td [
                                prop.colSpan 8
                                prop.style [ style.textAlign.center ]
                                prop.text "Error loading packages :("
                            ]
                            Html.details [
                                prop.children [
                                    Html.summary "Details"
                                    Html.pre [ prop.text e.Message ]
                                ]
                            ]
                        ]
                    ]
                | SelectorState.Loaded _ when Array.isEmpty filtered ->
                    Html.tbody [
                        Html.tr [
                            Html.td [
                                prop.colSpan 8
                                prop.style [ style.textAlign.center ]
                                prop.text "No packages found."
                            ]
                        ]
                    ]
                | SelectorState.Loaded _ -> renderBody filtered
            ]
        ]
