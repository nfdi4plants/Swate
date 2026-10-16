module ARCitISA.Components.ClipboardContract.Types

open global.ARCtrl
open ARCitISA.Components

type ClipboardContent = {
    Cells: CompositeCell[][] option
    PlainText: string
}

type MappedCell<'T> = { Source: 'T; Target: CellCoordinate }
