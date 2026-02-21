namespace Renderer.components.Widgets

open Feliz
open ARCtrl


[<RequireQualifiedAccess>]
type StatusKind =
    | Info
    | Warning
    | Error

type StatusMessage = {
    Kind: StatusKind
    Text: string
}

type ActiveTableData = {
    ArcFile: ArcFiles
    Table: ArcTable
    TableName: string
    TableIndex: int
}

type ActiveDataMapData = {
    ArcFile: ArcFiles
    DataMap: DataMap
}

type StatusElement =

    [<ReactComponent>]
    static member Create (status: StatusMessage) =
        let classNames =
            match status.Kind with
            | StatusKind.Info -> [ "swt:alert-info"; "swt:text-info-content" ]
            | StatusKind.Warning -> [ "swt:alert-warning"; "swt:text-warning-content" ]
            | StatusKind.Error -> [ "swt:alert-error"; "swt:text-error-content" ]

        Html.div [
            prop.className ([ "swt:alert swt:py-2 swt:text-sm" ] @ classNames)
            prop.children [ Html.span status.Text ]
        ]
