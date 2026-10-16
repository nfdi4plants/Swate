namespace Modals

open Feliz
open Messages
open ARCitISA.Components.Composite.AnnotationTable

type ResetTable =

    [<ReactComponent>]
    static member Main(isOpen, setIsOpen, dispatch) =

        let reset () =
            Spreadsheet.Reset |> SpreadsheetMsg |> dispatch

        ResetTableConfirmationModal.ResetTableConfirmationModal(isOpen, setIsOpen, reset)
