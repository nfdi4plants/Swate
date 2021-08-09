module OfficeInterop.EventHandlers

open System

open Fable.Core
open Fable.Core.JsInterop
open OfficeJS.Fable
open OfficeJS.Fable.Excel
open OfficeJS.Fable.GlobalBindings
open System.Collections.Generic
open System.Text.RegularExpressions

open OfficeInterop.Regex
open OfficeInterop.Types
open OfficeInterop.HelperFunctions
open BuildingBlockTypes

//open Elmish
//open Browser

//let testSubscription (message:string) (tableName:string) =
//    let sub dispatch =
//        let m = sprintf "%s Table: %s " message tableName
//        Subscription.TestSubscription m |> dispatch
//    Cmd.ofSub sub

///// This module is loaded client side and is meant to work as a storage for office information.
///// This could possible be refractured into a model type design.
//module EventHandlerStates =

//    /// This mutable variable contains the information of which table currently has an existing eventhandler for assisted deleting from hidden columns.
//    /// In addition the 'OfficeExtension.EventHandlerResult<TableChangedEventArgs>' object is needed to access the specific handler again and to individually remove it.
//    let mutable adaptHiddenColsHandlerList: Map<string,OfficeExtension.EventHandlerResult<TableChangedEventArgs>> = Map.empty 

///// This functions works as event handler that can be added to tables and triggers on OnChanged event.
///// It is used to delete anything written in the hidden columns (referenced by '#h' in the column header tag array).
//let adaptHiddenColsHandler (tableChangeArgs:TableChangedEventArgs, tableName) = 
//    Excel.run(fun context ->

//        /// get active worksheet to execute function on
//        let worksheet = context.workbook.worksheets.getActiveWorksheet()
//        /// As we found out the getItem() function does not only operate on the sheet it is executed on therefore we need the annotationTable-name of the active sheet.
//        /// The table name is passed by a previous function and allows us to access a specific annotation table on any worksheet in the excel workbook 
//        let table = worksheet.tables.getItem(tableName)

//        // The next part loads relevant information from the excel objects and allows us to access them after 'context.sync()'

//        let tableHeader = table.getHeaderRowRange()
//        let _ = tableHeader.load(U2.Case2 (ResizeArray[|"values"; "columnIndex"|]))

//        let tableRange = table.getRange()
//        let _ = tableRange.load(U2.Case2 (ResizeArray[|"values"; "rowIndex"|]))

//        let changedRange = tableChangeArgs.getRange(context)
//        let _ = changedRange.load(U2.Case2 (ResizeArray[|"columnIndex"; "rowIndex"; "rowCount"|]))

//        let r = context.runtime.load(U2.Case1 "enableEvents")

//        context.sync()
//            .``then``(fun t ->

//                /// during our function we want all eventHandlers to be deactivated to prevent any cross reactions.
//                r.enableEvents <- false

//                // This is necessary to find the correct table index for changed cell
//                // As the Range in which the change occured is always referenced from the worksheet and not from the table we need to calculate the table index
//                // e.g. If a table starts at cell 'C5' then the table index is 0 but the worksheet index is 2.#

//                /// Calculate the table column index for the changed range
//                let recalcChangedTableColIndex =
//                    let tableHeaderRangeColIndex = tableHeader.columnIndex
//                    let selectColIndex = changedRange.columnIndex
//                    selectColIndex - tableHeaderRangeColIndex

//                /// Calculate the table row index for the changed range
//                let recalcChangedTableRowIndex =
//                    let tableRangedRowIndex = tableRange.rowIndex
//                    let selectRowIndex = changedRange.rowIndex
//                    selectRowIndex - tableRangedRowIndex

//                /// Get an array of all headers. We have a lot of information in our headers, e.g. tag array
//                let headerVals = tableHeader.values.[0] |> Array.ofSeq

//                /// find the index of the next non hidden column. We assume, that all columns in between are part of the building block that got changed.
//                let nextNonHiddenColForward = findIndexNextNotHiddenCol headerVals (recalcChangedTableColIndex+1.)

//                //printfn "Try access fields at row: %.0f for column: %.0f - %.0f" recalcChangedTableRowIndex (recalcChangedTableColIndex+1.) (nextNonHiddenColForward-1.)

//                /// This gives us the header of the column in which something was changed.
//                let header = tableHeader.values.[0].[int recalcChangedTableColIndex]

//                /// Parse header to allow for easy access on any relevant information in form of the 'ColHeader' record type.
//                let parsedHeader = parseColHeader (string header.Value)

//                /// This function will change the value of all cells of the same row and the same building block as the cells changed.
//                /// E.g. changed cells C5 to C8, which have 5 hidden columns as part of the building block. Then it will delete D6:H8.
//                let changeHidden () =
//                    // We cannot work with the tableChangeArgs.details.valueAfter to see if we delete the hidden cols or adapt to user specific. 
//                    // tableChangeArgs.details.valueAfter works only on single cell changes

//                    /// This creates a one cell range with an empty input. We use this as insert to simulate a delete.
//                    let input =
//                        ResizeArray([
//                            ResizeArray([
//                                "" |> box |> Some
//                            ])
//                        ])

//                    /// Iterate over all rows starting from our table index of the rows changed (this will always be the index of the first row changed)
//                    /// and ending with the same index plus the number of rows changed.
//                    /// tl;dr iterate over all rows with a changed cell
//                    for rowInd in recalcChangedTableRowIndex .. 1. .. (recalcChangedTableRowIndex + changedRange.rowCount - 1.) do

//                        /// Iterate over all columns starting from our table index of the columns changed (this will always be the index of the first col changed) + 1
//                        /// and ending with the index of the next non-hidden col - 1, so with the last hidden col.
//                        /// tl;dr iterate over all hidden cols
//                        for colInd in recalcChangedTableColIndex+1. .. 1. .. nextNonHiddenColForward-1. do

//                            /// for all these combinations get the cell object for these indices and insert our empty input.
//                            /// Effectively deleting their previous value.
//                            let c = tableRange.getCell(rowInd, colInd)
//                            c.values <- input

//                /// This is a failsafe to prevent firing the event when a reference (hidden) column is changed.
//                match parsedHeader.TagArr with
//                | Some tagArr ->
//                    if tagArr |> Array.contains ColumnTags.HiddenTag then () else changeHidden()
//                | None ->
//                    changeHidden()

//                /// activate events again
//                r.enableEvents <- true

//                /// This is not accessed and could very well be anything
//                t
//            )
//    )