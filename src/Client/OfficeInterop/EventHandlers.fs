module OfficeInterop.EventHandlers

open System

open Fable.Core
open Fable.Core.JsInterop
open OfficeJS
open Excel
open System.Collections.Generic
open System.Text.RegularExpressions

open OfficeInterop.Regex
open OfficeInterop.Types
open OfficeInterop.HelperFunctions
open AutoFillTypes

//open Elmish
//open Browser

//let testSubscription (message:string) (tableName:string) =
//    let sub dispatch =
//        let m = sprintf "%s Table: %s " message tableName
//        Subscription.TestSubscription m |> dispatch
//    Cmd.ofSub sub

module EventHandlerStates =

    let mutable adaptHiddenColsHandlerList: Map<string,OfficeExtension.EventHandlerResult<TableChangedEventArgs>> = Map.empty 


let adaptHiddenColsHandler (tableChangeArgs:TableChangedEventArgs, tableName) = 
    Excel.run(fun context ->

        let worksheet = context.workbook.worksheets.getActiveWorksheet()
        let table = worksheet.tables.getItem(tableName)
        let tableHeader = table.getHeaderRowRange()
        let _ = tableHeader.load(U2.Case2 (ResizeArray[|"values"; "columnIndex"|]))

        let tableRange = table.getRange()
        let _ = tableRange.load(U2.Case2 (ResizeArray[|"values"; "rowIndex"|]))

        let changedRange = tableChangeArgs.getRange(context)
        let _ = changedRange.load(U2.Case2 (ResizeArray[|"columnIndex"; "rowIndex"; "rowCount"|]))
        let r = context.runtime.load(U2.Case1 "enableEvents")

        //let _ = Types.Subscription.TestSubscription ("Test") |> dispatch

        context.sync()
            .``then``(fun t ->

                r.enableEvents <- false
                /// This is necessary to place find the correct table index for changed cell
                let recalcChangedTableColIndex =
                    let tableHeaderRangeColIndex = tableHeader.columnIndex
                    let selectColIndex = changedRange.columnIndex
                    selectColIndex - tableHeaderRangeColIndex

                let recalcChangedTableRowIndex =
                    let tableRangedRowIndex = tableRange.rowIndex
                    let selectRowIndex = changedRange.rowIndex
                    selectRowIndex - tableRangedRowIndex

                let headerVals = tableHeader.values.[0] |> Array.ofSeq

                let nextNonHiddenColForward = findIndexNextNotHiddenCol headerVals (recalcChangedTableColIndex+1.)

                //printfn "Try access fields at row: %.0f for column: %.0f - %.0f" recalcChangedTableRowIndex (recalcChangedTableColIndex+1.) (nextNonHiddenColForward-1.)

                let header = tableHeader.values.[0].[int recalcChangedTableColIndex]

                let parsedHeader =
                    let h = string header.Value
                    parseColHeader h

                let changeHidden () =
                    // We cannot work with the tableChangeArgs.details.valueAfter to see if we delete the hidden cols or adapt to user specific. 
                    // tableChangeArgs.details.valueAfter works only on single cell changes
                    let input =
                        ResizeArray([
                            ResizeArray([
                                "" |> box |> Some
                            ])
                        ])
                    for rowInd in recalcChangedTableRowIndex .. 1. .. (recalcChangedTableRowIndex + changedRange.rowCount - 1.) do
                        
                        for colInd in recalcChangedTableColIndex+1. .. 1. .. nextNonHiddenColForward-1. do
                            let c = tableRange.getCell(rowInd, colInd)
                            c.values <- input

                match parsedHeader.TagArr with
                | Some tagArr ->
                    if tagArr |> Array.contains ColumnTags.HiddenTag then () else changeHidden()
                | None ->
                    changeHidden()

                r.enableEvents <- true
                t
            )
    )