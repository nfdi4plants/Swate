namespace Pages

open Fable.Core
open Model.DataAnnotator
open Messages.DataAnnotator
open Model
open Messages
open Feliz
open Feliz.Bulma

type private DataAnnotatorState = {
    HasHeader: bool
    Seperator: string option
} with
    static member init() =
        {
            HasHeader = true
            Seperator = None
        }

module private DataAnnotatorHelper =

    let matchFileTypeToSeparator (fileType: string) =
        if fileType.Contains("csv") then
            ","
        elif fileType.Contains("tsv") then
            "\t"
        else
            ","

    let UploadComponent dispatch isLarge =
        Bulma.file [
            if isLarge then
                file.hasName
                file.isBoxed
            prop.children [
                Bulma.fileLabel.label [
                    Bulma.fileInput [
                        prop.onChange(fun (e: Browser.Types.File) ->
                            promise {
                                let! content = e.text()
                                DataFile.create(e.name, e.``type``, content, e.size) |> Some |> UpdateDataFile |> DataAnnotatorMsg |> dispatch
                            }
                            |> Async.AwaitPromise
                            |> Async.StartImmediate
                        )
                    ]
                    Bulma.fileCta [
                        Bulma.fileIcon [Html.i [prop.className "fa-solid fa-upload"]]
                        Bulma.fileLabel.span "Choose a file..."
                    ]
                ]
            ]
        ]

    let DataFileConfigComponent dispatch =
        Bulma.block [
            Bulma.buttons [
                Bulma.button.button [
                    prop.text "Reset"
                    prop.onClick (fun _ -> UpdateDataFile None |> DataAnnotatorMsg |> dispatch)
                ]
                UploadComponent dispatch false
            ]
        ]

    let FileMetadataComponent (file: DataFile) =
        Bulma.block [
            Html.textf "Length %i - %s - %f" file.DataContent.Length file.DataFileName file.DataSize
        ]

    let FileViewComponent (file: DataFile) (config: DataAnnotatorState) =
        let rows = file.DataContent.Split([|'\n'|])
        let separatedRows = rows |> Array.map (fun line -> line.Split([|config.Seperator.Value|], System.StringSplitOptions.RemoveEmptyEntries))
        Bulma.tableContainer [
            prop.style [style.maxHeight 400; style.overflowY.auto]
            prop.children [
                Bulma.table [
                    table.isNarrow
                    prop.children [
                        Html.thead [
                            Html.tr [
                                for header in separatedRows.[0] do
                                    Html.th header
                            ]
                        ]
                        Html.tbody [
                            for row in separatedRows.[1..30] do
                                Html.tr [
                                    for cell in row do
                                        Html.td cell
                                ]
                        ]
                    ]
                ]
            ]
        ]
        |> Bulma.block

open DataAnnotatorHelper

type DataAnnotator =

    [<ReactComponent>]
    static member Main(model: Model, dispatch: Msg -> unit) =
        let state, setState = React.useState(DataAnnotatorState.init)
        React.useEffect(
            (fun _ ->
                if model.DataAnnotatorModel.DataFile.IsSome then
                    let sep = matchFileTypeToSeparator model.DataAnnotatorModel.DataFile.Value.DataFileType
                    {state with Seperator = Some sep} |> setState
            ),
            [|box model.DataAnnotatorModel.DataFile|]
        )
        Html.div [
            match model.DataAnnotatorModel.DataFile with
            | Some file when state.Seperator.IsSome ->
                DataFileConfigComponent dispatch
                FileMetadataComponent file
                FileViewComponent file state
            | _ ->
                UploadComponent dispatch true
        ]

