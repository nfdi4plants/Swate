module internal Swate.Components.Hooks.UseKeyedStateSample

open Fable.Core
open Feliz
open Swate.Components
open Swate.Components.Hooks.UseKeyedState

let private rnd = System.Random()

let private fruitPool = [|
    "Apple"
    "Banana"
    "Cherry"
    "Dragonfruit"
    "Elderberry"
    "Fig"
    "Grape"
    "Honeydew"
    "Kiwi"
    "Lemon"
    "Mango"
    "Nectarine"
    "Orange"
    "Papaya"
    "Quince"
    "Raspberry"
    "Strawberry"
    "Tangerine"
    "Ugli fruit"
    "Vanilla bean"
    "Watermelon"
    "Xigua"
    "Yellow passion fruit"
    "Zucchini"
|]

// Keyed on the whole dataset: whenever the dataset is replaced (new random list
// of random length), pagination resets to page 1.
[<ReactComponent>]
let SamplePaginated () =

    let pageSize = 3
    let items, setItems = React.useState (fruitPool)
    let page, setPage = React.useKeyedState (0, items)

    let pageCount = max 1 ((items.Length + pageSize - 1) / pageSize)
    let safePage = min page (pageCount - 1)
    let visible = items |> Array.skip (safePage * pageSize) |> Array.truncate pageSize

    Html.div [
        Html.table [
            prop.className "swt:table swt:table-sm"
            prop.children [
                Html.thead [
                    Html.tr [ Html.th [ prop.text "#" ]; Html.th [ prop.text "Item" ] ]
                ]
                Html.tbody [
                    for i, item in visible |> Array.indexed do
                        Html.tr [
                            Html.td [ prop.text (string (safePage * pageSize + i + 1)) ]
                            Html.td [ prop.text item ]
                        ]
                ]
            ]
        ]
        Html.div [
            Html.button [
                prop.className "swt:btn swt:btn-sm"
                prop.text "Prev"
                prop.disabled ((safePage = 0))
                prop.onClick (fun _ -> setPage (max 0 (page - 1)))
            ]
            Html.span [ prop.testid "page"; prop.text (string (safePage + 1)) ]
            Html.span [ prop.text $" / {pageCount}" ]
            Html.button [
                prop.className "swt:btn swt:btn-sm"
                prop.text "Next"
                prop.disabled ((safePage = pageCount - 1))
                prop.onClick (fun _ -> setPage (min (pageCount - 1) (page + 1)))
            ]
            Html.button [
                prop.className "swt:btn swt:btn-sm"
                prop.text "New Random Dataset"
                prop.onClick (fun _ ->
                    let len = rnd.Next(1, 9)
                    let next = Array.init len (fun _ -> fruitPool.[rnd.Next(fruitPool.Length)])

                    if next = items then
                        setItems (Array.append next [| "Zucchini" |])
                    else
                        setItems next
                )
            ]
        ]
    ]

type DataSet = {|
    id: int
    name: string
    gen: int
    rows: string[]
|}

// Keyed on the dataset id: the dataset object is recreated on every interaction,
// but row selection only resets when the id actually changes.
[<ReactComponent>]
let SampleDatasetSelector () =

    let dataset, setDataset =
        React.useState<DataSet> (
            {|
                id = 1
                name = "Dataset A"
                gen = 0
                rows = [| "Alpha"; "Beta"; "Gamma" |]
            |}
        )

    let selectedRow, setSelectedRow =
        React.useKeyedState<string option, int> (None, dataset.id)

    Html.div [
        Html.div [
            prop.testid "dataset-name"
            prop.text $"Dataset {dataset.name} (v{dataset.gen})"
        ]
        Html.div [
            for row in dataset.rows do
                Html.button [
                    prop.key row
                    prop.className [
                        "swt:btn swt:btn-sm"
                        if Some row = selectedRow then
                            "swt:btn-primary"
                    ]
                    prop.text row
                    prop.onClick (fun _ -> setSelectedRow (Some row))
                ]
        ]
        Html.div [
            prop.testid "selected-row"
            prop.text $"Selected row: {selectedRow}"
        ]
        Html.div [
            Html.button [
                prop.className "swt:btn swt:btn-sm"
                prop.text "Load Dataset A"
                prop.onClick (fun _ ->
                    setDataset {|
                        id = 1
                        name = "Dataset A"
                        gen = 0
                        rows = [| "Alpha"; "Beta"; "Gamma" |]
                    |}
                )
            ]
            Html.button [
                prop.className "swt:btn swt:btn-sm"
                prop.text "Reload Dataset A"
                prop.onClick (fun _ ->
                    setDataset {|
                        id = 1
                        name = "Dataset A"
                        gen = dataset.gen + 1
                        rows = [| "Alpha"; "Beta"; "Gamma" |]
                    |}
                )
            ]
            Html.button [
                prop.className "swt:btn swt:btn-sm"
                prop.text "Load Dataset B"
                prop.onClick (fun _ ->
                    setDataset {|
                        id = 2
                        name = "Dataset B"
                        gen = 0
                        rows = [| "Delta"; "Epsilon" |]
                    |}
                )
            ]
        ]
    ]
