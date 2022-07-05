module OfficeInterop.TemplateMetadataFunctions

open System

open Fable.Core
open ExcelJS.Fable
open Excel
open GlobalBindings

open Shared.OfficeInteropTypes
open Shared.TemplateTypes.Metadata

let private colorOuterBordersWhite (borderSeq:seq<RangeBorder>) =
    borderSeq
    |> Seq.iter (fun border ->
        if border.sideIndex = U2.Case1 BorderIndex.EdgeBottom || border.sideIndex = U2.Case1 BorderIndex.EdgeLeft || border.sideIndex = U2.Case1 BorderIndex.EdgeRight || border.sideIndex = U2.Case1 BorderIndex.EdgeTop then
            border.color <- NFDIColors.white
    )

let private colorTopBottomBordersWhite (borderSeq:seq<RangeBorder>) =
    borderSeq
    |> Seq.iter (fun border ->
        if border.sideIndex = U2.Case1 BorderIndex.EdgeBottom || border.sideIndex = U2.Case1 BorderIndex.EdgeTop then
            border.color <- NFDIColors.white
    )

let rec extendMetadataFields (metadatafields:MetadataField) =
    let children = metadatafields.Children |> List.collect extendMetadataFields
    if metadatafields.Key <> "" && metadatafields.Children.IsEmpty |> not && metadatafields.List then
        let metadatafields' = {metadatafields with ExtendedNameKey = $"#{metadatafields.ExtendedNameKey.ToUpper()} list"}
        metadatafields'::children
    elif metadatafields.Key <> "" && metadatafields.Children.IsEmpty |> not then
        let metadatafields' = {metadatafields with ExtendedNameKey = "#" + metadatafields.ExtendedNameKey.ToUpper()}
        metadatafields'::children
    elif metadatafields.Key <> "" then
        metadatafields::children
    else
        children

let createTemplateMetadataWorksheet (metadatafields:MetadataField) =
    Excel.run (fun context ->
        promise {

            let extended = extendMetadataFields metadatafields |> Array.ofList

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
            let idValueIndex = extended |> Array.findIndex (fun x -> x.Key = RowKeys.TemplateIdKey )
            let descriptionValueIndex = extended |> Array.findIndex (fun x -> x.Key = RowKeys.DescriptionKey )
            let columnValues =
                ResizeArray [|
                    for i in 0 .. int rowLength - 1 do
                        yield ResizeArray [|Some <| box (extended.[i].ExtendedNameKey)|]
                |]
            let! update = context.sync().``then``(fun e ->
                firstColumn.values                                  <- columnValues
                sndColumnCells.[idValueIndex].values                <- ResizeArray [|ResizeArray [| newIdent |> box |> Some|]|]
                firstColumn.format.autofitColumns()
                firstColumn.format.autofitRows()
                firstColumn.format.font.bold                        <- true
                firstColumn.format.font.color                       <- "whitesmoke"
                firstColumn.format.borders.items |> colorOuterBordersWhite
                firstColumn.format.borders.items |> Seq.iter (fun border -> if border.sideIndex = U2.Case1 BorderIndex.EdgeRight then border.weight <- U2.Case1 BorderWeight.Thick)
                sndColumnCells |> Array.iter (fun cell -> cell.format.borders.items |> colorOuterBordersWhite )
                firstColumn.format.verticalAlignment                <- U2.Case1 VerticalAlignment.Top
                sndColumn.format.verticalAlignment                  <- U2.Case1 VerticalAlignment.Top
                let sndColStyling =
                    extended
                    |> Array.iteri (fun i info ->
                        if info.Children.IsEmpty then
                            fstColumnCells.[i].format.fill.color <- ExcelColors.Excel.Primary
                            sndColumnCells.[i].format.fill.color <- ExcelColors.Excel.Tint40
                        else
                            fstColumnCells.[i].format.fill.color <- ExcelColors.Excel.Shade10
                            sndColumnCells.[i].format.borders.items |> colorTopBottomBordersWhite
                            sndColumnCells.[i].format.fill.color <- ExcelColors.Excel.Shade10
                )
                //sndColumn.format.fill.color                         <- ExcelColors.Excel.Tint40
                sndColumnCells.[idValueIndex].format.fill.color     <- NFDIColors.Red.Base
                let newComments =
                    extended
                    |> Array.iteri (fun i info ->
                        if info.Description.IsSome && info.Description.Value <> "" then
                            let targetCellRange : U2<Range,string> = U2.Case1 fstColumnCells.[i]
                            let content : U2<CommentRichContent,string> = U2.Case2 info.Description.Value
                            // WARNING!
                            // If you use "let comment = ..." outside of this if-else case ONLY the comment with reply will be added
                            if i = idValueIndex then
                                let comment = context.workbook.comments.add(targetCellRange, content, contentType =  ContentType.Plain)
                                let reply : U2<CommentRichContent,string> = U2.Case2 $"id={newIdent.ToString()}"
                                let _ = comment.replies.add(reply, contentType =  ContentType.Plain)
                                ()
                            else
                                let comment = context.workbook.comments.add(targetCellRange, content, contentType =  ContentType.Plain)
                                ()
                        else
                            ()
                    )
                sndColumn.format.columnWidth                            <- 300.
                sndColumnCells.[descriptionValueIndex].format.rowHeight <- 50.
                sndColumn.format.wrapText                               <- true
                newWorksheet.activate()
            )
            return "Info", "Created new template metadata sheet!"
        }
    )