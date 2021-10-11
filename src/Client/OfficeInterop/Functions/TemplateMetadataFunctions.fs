module OfficeInterop.TemplateMetadataFunctions

open System

open Fable.Core
open ExcelJS.Fable
open Excel
open GlobalBindings

open Shared.OfficeInteropTypes
open Shared.TermTypes

type TemplateMetadata = {
    TemplateID      : Guid
    TemplateName    : string
    Version         : string
    Author          : string list
    Description     : string
    DocsLink        : string
    ER              : string list
    Labels          : string list
    Tags            : string list
}

[<Literal>]
let TemplateMetadataWorksheetName = "SwateTemplateMetadata" 

let NumberOfRows = 9.

let private colorOuterBordersWhite (borderSeq:seq<RangeBorder>) =
    borderSeq
    |> Seq.iter (fun border ->
        if border.sideIndex = U2.Case1 BorderIndex.EdgeBottom || border.sideIndex = U2.Case1 BorderIndex.EdgeLeft || border.sideIndex = U2.Case1 BorderIndex.EdgeRight || border.sideIndex = U2.Case1 BorderIndex.EdgeTop then
            border.color <- NFDIColors.white
    )

open Fable.Core.JsInterop

let createTemplateMetadataWorksheet() =
    Excel.run (fun context ->
        promise {
            let! newWorksheet = context.sync().``then``(fun e->
                context.workbook.worksheets.add TemplateMetadataWorksheetName
            )

            let! firstColumn, fstColumnCells, sndColumn, sndColumnCells = context.sync().``then``(fun e ->
                let fst = newWorksheet.getRangeByIndexes(0.,0.,NumberOfRows,1.)
                let _ = fst.format.borders.load(propertyNames=U2.Case1 "items")
                let fstCells = [|
                    for i in 0. .. NumberOfRows-1. do
                        let cell = fst.getCell (i,0.)
                        let _ = cell.format.borders.load(propertyNames=U2.Case1 "items")
                        yield cell
                |]
                let sndCells = [|
                    for i in 0. .. NumberOfRows-1. do
                        let cell = fst.getCell (i,1.)
                        let _ = cell.format.borders.load(propertyNames=U2.Case1 "items")
                        yield cell
                |]
                let snd = newWorksheet.getRangeByIndexes(0.,1.,NumberOfRows,1.)
                fst, fstCells, snd, sndCells
            )

            let! update = context.sync().``then``(fun e ->
                let newIdent = System.Guid.NewGuid()
                let columnValues =
                    ResizeArray [|
                        ResizeArray [|Some <| box "TemplateID"  |]
                        ResizeArray [|Some <| box "TemplateName"|]
                        ResizeArray [|Some <| box "Version"     |]
                        ResizeArray [|Some <| box "Author"      |]
                        ResizeArray [|Some <| box "Description" |]
                        ResizeArray [|Some <| box "DocsLink"    |]
                        ResizeArray [|Some <| box "ER"          |]
                        ResizeArray [|Some <| box "Labels"      |]
                        ResizeArray [|Some <| box "Tags"        |]
                    |]
                firstColumn.format.font.bold            <- true
                firstColumn.format.font.color           <- "whitesmoke"
                firstColumn.format.borders.items |> colorOuterBordersWhite
                firstColumn.format.borders.items |> Seq.iter (fun border -> if border.sideIndex = U2.Case1 BorderIndex.EdgeRight then border.weight <- U2.Case1 BorderWeight.Thick)
                //fstColumnCells |> Array.iter (fun cell -> cell.format.borders.items |> colorOuterBordersWhite )
                sndColumnCells |> Array.iter (fun cell -> cell.format.borders.items |> colorOuterBordersWhite )
                firstColumn.format.fill.color           <- ExcelColors.Excel.Primary
                sndColumn.format.fill.color             <- ExcelColors.Excel.Tint40
                sndColumnCells.[0].format.fill.color    <- NFDIColors.Red.Base
                firstColumn.values                      <- columnValues
                sndColumnCells.[0].values               <- ResizeArray [|ResizeArray [| newIdent |> box |> Some|]|]
                let newComment =
                    let targetCellRange : U2<Range,string> = U2.Case1 sndColumnCells.[0]
                    let content : U2<CommentRichContent,string> = U2.Case2 $"SwateMsg: Do not change this value! It is used as unique identifier for this template. {newIdent}"
                    context.workbook.comments.add(targetCellRange, content, contentType =  ContentType.Plain)
                firstColumn.format.autofitColumns()
                sndColumn.format.columnWidth            <- 300.
                newWorksheet.activate()
            )
            return "Info", "TESTING!"
        }
    )