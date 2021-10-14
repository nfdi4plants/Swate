module OfficeInterop.TemplateMetadataFunctions

open System

open Fable.Core
open ExcelJS.Fable
open Excel
open GlobalBindings

open Shared.OfficeInteropTypes
open Shared.ProtocolTemplateTypes

let private colorOuterBordersWhite (borderSeq:seq<RangeBorder>) =
    borderSeq
    |> Seq.iter (fun border ->
        if border.sideIndex = U2.Case1 BorderIndex.EdgeBottom || border.sideIndex = U2.Case1 BorderIndex.EdgeLeft || border.sideIndex = U2.Case1 BorderIndex.EdgeRight || border.sideIndex = U2.Case1 BorderIndex.EdgeTop then
            border.color <- NFDIColors.white
    )

open Fable.Core.JsInterop

let rec extendMetadataFields parentKey (metadatafields:MetadataField) =
    if metadatafields.Key = "comments" then
        []
    else
        let name        = $"{parentKey} {metadatafields.Key}".Trim()
        let description = metadatafields.Description |> Option.defaultValue ""
        let children    =
            if metadatafields.List.IsSome then
                metadatafields.List.Value.Children |> List.collect (extendMetadataFields name)
            else
                metadatafields.Children |> List.collect (extendMetadataFields name)
        let name' = if children <> [] then name.ToUpper() else name
        let name'' = if metadatafields.List.IsSome then $"{name'} list" else name'
        if name'' <> "ROOT" then
            // skip the first 5 characters of name'', to remove "ROOT "
            {|Name = name''.[5..]; Description = description|}::children
        else
            children


let createTemplateMetadataWorksheet (metadatafields:MetadataField option) =
    Excel.run (fun context ->
        promise {

            if metadatafields.IsNone then failwith "Could not parse json schema"

            let extended = extendMetadataFields "" metadatafields.Value |> Array.ofList

            let rowLength = float extended.Length

            let! newWorksheet = context.sync().``then``(fun e->
                context.workbook.worksheets.add TemplateMetadataWorksheetName
            )

            let! firstColumn, fstColumnCells, sndColumn, sndColumnCells = context.sync().``then``(fun e ->
                let fst = newWorksheet.getRangeByIndexes(0.,0.,rowLength,1.)
                let _ = fst.format.borders.load(propertyNames=U2.Case1 "items")
                let fstCells = [|
                    for i in 0. .. rowLength-1. do
                        let cell = fst.getCell (i,0.)
                        let _ = cell.format.borders.load(propertyNames=U2.Case1 "items")
                        yield cell
                |]
                let sndCells = [|
                    for i in 0. .. rowLength-1. do
                        let cell = fst.getCell (i,1.)
                        let _ = cell.format.borders.load(propertyNames=U2.Case1 "items")
                        yield cell
                |]
                let snd = newWorksheet.getRangeByIndexes(0.,1.,rowLength,1.)
                fst, fstCells, snd, sndCells
            )

            let newIdent = System.Guid.NewGuid()
            //let idValueIndex = TemplateMetadata.fieldToTableRowIndex MetadataFieldKeys.TemplateID |> int
            //let descriptionValueIndex = TemplateMetadata.fieldToTableRowIndex MetadataFieldKeys.Description |> int
            //let erIndex = TemplateMetadata.fieldToTableRowIndex MetadataFieldKeys.ER |> int
            let columnValues =
                ResizeArray [|
                    for i in 0 .. int rowLength - 1 do
                        yield ResizeArray [|Some <| box (extended.[i].Name)|]
                |]
            let! update = context.sync().``then``(fun e ->
                firstColumn.values                                  <- columnValues
                //sndColumnCells.[idValueIndex].values                <- ResizeArray [|ResizeArray [| newIdent |> box |> Some|]|]
                firstColumn.format.autofitColumns()
                firstColumn.format.autofitRows()
                firstColumn.format.font.bold                        <- true
                firstColumn.format.font.color                       <- "whitesmoke"
                firstColumn.format.borders.items |> colorOuterBordersWhite
                firstColumn.format.borders.items |> Seq.iter (fun border -> if border.sideIndex = U2.Case1 BorderIndex.EdgeRight then border.weight <- U2.Case1 BorderWeight.Thick)
                //fstColumnCells |> Array.iter (fun cell -> cell.format.borders.items |> colorOuterBordersWhite )
                sndColumnCells |> Array.iter (fun cell -> cell.format.borders.items |> colorOuterBordersWhite )
                firstColumn.format.fill.color                       <- ExcelColors.Excel.Primary
                firstColumn.format.verticalAlignment                <- U2.Case1 VerticalAlignment.Top
                sndColumn.format.verticalAlignment                  <- U2.Case1 VerticalAlignment.Top
                sndColumn.format.fill.color                         <- ExcelColors.Excel.Tint40
                //sndColumnCells.[idValueIndex].format.fill.color     <- NFDIColors.Red.Base
                //let newComment =
                //    let targetCellRange : U2<Range,string> = U2.Case1 sndColumnCells.[0]
                //    let content : U2<CommentRichContent,string> = U2.Case2 $"SwateMsg: Do not change this value! It is used as unique identifier for this template. {newIdent}"
                //    context.workbook.comments.add(targetCellRange, content, contentType =  ContentType.Plain)
                sndColumn.format.columnWidth                            <- 300.
                //sndColumnCells.[descriptionValueIndex].format.rowHeight <- 50.
                sndColumn.format.wrapText                               <- true
                newWorksheet.activate()
            )
            return "Info", "Created new template metadata sheet!"
        }
    )