namespace Swate.Components.Composite.ValidationPackageSelector

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Composite.ValidationPackageSelector.Context
open Swate.Components.Primitive
open Swate.Components.Primitive.LoadingSpinner
open Swate.Components.Primitive.Select
open Types
open Swate.Components.Hooks.UseKeyedState

[<Erase; Mangle(false)>]
type PackageTable =

    [<ReactComponent>]
    static member private HeaderSelect
        (label: string, optionNames: string[], selectedIndex: int option, setSelectedIndex: int option -> unit)
        =
        let selected = selectedIndex |> Option.map (fun i -> optionNames.[i])

        SingleSelect.SingleSelect(
            optionNames |> Array.map (fun name -> {| item = name; label = name |}),
            selectedIndex,
            setSelectedIndex,
            triggerRenderFn =
                (fun (_: {| isOpen: bool |}) ->
                    Html.div [
                        prop.testId $"validation-package-selector-{label.ToLower()}-filter"
                        prop.className [
                            "swt:flex swt:items-center swt:gap-1 swt:text-xs swt:font-medium swt:uppercase"
                            if selected.IsSome then
                                "swt:text-accent"
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

        let authorOptions = Helper.distinctAuthors items

        let authorKey =
            authorOptions |> String.concat ","

        let selectedAuthor, setSelectedAuthor =
            React.useKeyedState<int option, string> (None, authorKey)

        let authorFilterName = selectedAuthor |> Option.map (fun i -> authorOptions.[i])

        let tagOptions = Helper.distinctTags items
        let tagsKey = tagOptions |> String.concat ","

        let selectedTag, setSelectedTag =
            React.useKeyedState<int option, string> (None, tagsKey)

        let tagFilterName = selectedTag |> Option.map (fun i -> tagOptions.[i])

        let checkedSort, setCheckedSort = React.useState CheckedSort.None

        let filtered =
            React.useMemo (
                (fun () ->
                    items
                    |> Helper.filterByTag tagFilterName
                    |> Helper.filterByAuthor authorFilterName
                    |> Helper.sortByChecked checkedSort ctx.RowStateOf
                ),
                [|
                    box items
                    box tagFilterName
                    box authorFilterName
                    box checkedSort
                    box ctx.RowStateOf
                |]
            )

        Html.table [
            prop.className "swt:table swt:md:table-fixed swt:w-full swt:table-pin-rows"
            prop.children [
                Html.thead [
                    Html.tr [
                        Html.th [
                            prop.className "swt:w-12"
                            prop.children [
                                Html.button [
                                    prop.type' "button"
                                    prop.testId "validation-package-selector-sort-checked"
                                    prop.title (
                                        match checkedSort with
                                        | CheckedSort.None -> "Sort: checked state"
                                        | CheckedSort.CheckedFirst -> "Sort: checked first"
                                        | CheckedSort.CheckedLast -> "Sort: checked last"
                                    )
                                    prop.className [
                                        "swt:btn swt:btn-xs swt:btn-ghost swt:shrink-0"
                                        if checkedSort <> CheckedSort.None then
                                            "swt:text-accent"
                                    ]
                                    prop.onClick (fun _ -> setCheckedSort (Helper.nextCheckedSort checkedSort))
                                    prop.children [
                                        Html.i [
                                            prop.className [
                                                "swt:iconify"
                                                match checkedSort with
                                                | CheckedSort.None -> "swt:fluent--filter-20-regular"
                                                | CheckedSort.CheckedFirst ->
                                                    "swt:fluent--arrow-sort-up-lines-16-regular swt:size-4"
                                                | CheckedSort.CheckedLast ->
                                                    "swt:fluent--arrow-sort-down-lines-16-regular swt:size-4"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.th "Name"
                        Html.th [ prop.className "swt:max-md:hidden"; prop.text "Summary" ]
                        Html.th "Version"
                        Html.th [
                            PackageTable.HeaderSelect("Tags", tagOptions, selectedTag, setSelectedTag)
                        ]
                        Html.th [
                            PackageTable.HeaderSelect("Authors", authorOptions, selectedAuthor, setSelectedAuthor)
                        ]
                        Html.th [ prop.className "swt:max-md:hidden"; prop.text "Released" ]
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
                                prop.children [ Html.summary "Details"; Html.pre [ prop.text e.Message ] ]
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
