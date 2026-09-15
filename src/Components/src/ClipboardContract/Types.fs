module Swate.Components.ClipboardContract.Types

open global.ARCtrl
open Swate.Components

type ClipboardContent = {
    Cells: CompositeCell[][] option
    PlainText: string
}

type MappedCell = {
    Source: CompositeCell
    Target: CellCoordinate
}
