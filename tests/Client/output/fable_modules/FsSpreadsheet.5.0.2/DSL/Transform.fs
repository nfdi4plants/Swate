namespace FsSpreadsheet.DSL

open FsSpreadsheet

[<AutoOpen>]
module Transform = 

    let splitRowsAndColumns (els : SheetElement list) =
        let rec loop inRows inColumns current (remaining : SheetElement list) agg =
            match remaining with

            | [] when inRows    ->  ("Rows",current|> List.rev) :: agg
            | [] when inColumns ->  ("Columns",current|> List.rev) :: agg
            | []                ->  agg

            | UnindexedColumn c :: tail when inColumns      -> loop false true (UnindexedColumn c :: current) tail agg
            | IndexedColumn (i,c) :: tail when inColumns    -> loop false true (IndexedColumn (i,c) :: current) tail agg
            | UnindexedColumn c :: tail when inRows         -> loop false true [UnindexedColumn c] tail (("Rows",current|> List.rev) :: agg)
            | IndexedColumn (i,c) :: tail when inRows       -> loop false true [IndexedColumn (i,c)] tail (("Rows",current|> List.rev) :: agg)
            | UnindexedColumn c :: tail                     -> loop false true [UnindexedColumn c] tail agg
            | IndexedColumn (i,c) :: tail                   -> loop false true [IndexedColumn (i,c)] tail agg

            | UnindexedRow r :: tail when inRows            -> loop true false (UnindexedRow r :: current) tail agg
            | IndexedRow (i,r) :: tail when inRows          -> loop true false (IndexedRow (i,r) :: current) tail agg
            | UnindexedRow r :: tail when inColumns         -> loop true false [UnindexedRow r] tail (("Columns",current|> List.rev) :: agg)
            | IndexedRow (i,r) :: tail when inColumns       -> loop true false [IndexedRow (i,r)] tail (("Columns",current|> List.rev) :: agg)
            | UnindexedRow r :: tail                        -> loop true false [UnindexedRow r] tail agg
            | IndexedRow (i,r) :: tail                      -> loop true false [IndexedRow (i,r)] tail agg
            
            | Table (t,n) :: tail when inRows               -> loop false false [] tail (("Table",[Table (t,n)]) :: ("Rows",current|> List.rev) :: agg)
            | Table (t,n) :: tail when inColumns            -> loop false false [] tail (("Table",[Table (t,n)]) :: ("Columns",current|> List.rev) :: agg)
            | Table (t,n) :: tail                           -> loop false false [] tail (("Table",[Table (t,n)]) :: agg)

            | _ -> failwith "Unknown element combination when grouping Sheet elements"

        loop false false [] els []
        |> List.rev

    type Workbook with    

        static member internal parseTable (cellCollection : FsCellsCollection) (table : FsTable) (els : TableElement list) =
            let cols = 
                if els.Head.IsColumn then
                    els
                    |> List.map (fun col ->
                        match col with
                        | TableElement.UnindexedColumn col -> 
                            col
                            |> List.map (fun cell ->
                                match cell with 
                                | ColumnElement.UnindexedCell cell -> cell      
                                | _ -> failwith "Indexed cells not supported in column transformation"
                            )
                        | _ -> failwith "Indexed columns not supported in table transformation"
                    )
                else 
                    els
                    |> List.map (fun row ->
                        match row with
                        | TableElement.UnindexedRow row -> 
                            row
                            |> List.map (fun cell ->
                                match cell with 
                                | RowElement.UnindexedCell cell -> cell    
                                | _ -> failwith "Indexed cells not supported in row transformation"
                            )
                        | _ -> failwith "Indexed rows not supported in table transformation"
                    )
                    |> List.transpose
            cols
            |> List.iter (fun col ->
                match col with
                | [] -> failwith "Empty column"
                | header :: fields ->
                    let field = table.Field(snd >> string <| header, cellCollection)
                    fields
                    |> List.iteri (fun i (dataType,value) ->
                        let cell = field.Column.Cell(i + 2,cellCollection)
                        cell.DataType <- dataType
                        cell.Value <- value
                    )
                
            )

            

        static member internal parseRow (cellCollection : FsCellsCollection) (row : FsRow) (els : RowElement list) =
            let mutable cellIndexSet = 
                els 
                |> List.choose (fun el -> match el with | RowElement.IndexedCell(i,_) -> Option.Some i.Index | _ -> None)
                |> set
            let getNextIndex () = 
                let mutable i = 1 
                while cellIndexSet.Contains i do
                    i <- i + 1
                cellIndexSet <- Set.add i cellIndexSet
                i
            els
            |> List.iter (fun el ->
                match el with 
                | RowElement.IndexedCell(i,(datatype,value)) -> 
                    let cell = row.[i.Index]
                    cell.DataType <- datatype
                    cell.Value <- value
                | RowElement.UnindexedCell(datatype,value) -> 
                    let cell = row.[getNextIndex()]
                    cell.DataType <- datatype
                    cell.Value <- value
            )
   

        static member internal parseSheet (sheet : FsWorksheet) (els : SheetElement list) =
            let mutable rowIndexSet = 
                els 
                |> List.choose (fun el -> match el with | IndexedRow(i,_) -> Option.Some i.Index | _ -> None)
                |> set
                |> Set.add 0 

            let getFillRowIndex () = 
                let mutable i = 1 
                while rowIndexSet.Contains i do
                    i <- i + 1
                rowIndexSet <- Set.add i rowIndexSet
                i
            
            let getNextRowIndex () =
                rowIndexSet
                |> Seq.max
                |> (+) 1

            let parseColumns (columns : SheetElement list) = 

                let baseRowIndex = getNextRowIndex()

                let mutable columnIndexSet = 
                    columns 
                    |> List.choose (fun col -> match col with | IndexedColumn(i,_) -> Option.Some i.Index | _ -> None)
                    |> set
    
                let getNextColumnIndex () = 
                    let mutable i = 1 
                    while columnIndexSet.Contains i do
                        i <- i + 1
                    columnIndexSet <- Set.add i columnIndexSet
                    i

                columns
                |> List.iter (fun col ->
                    let colI,elements = 
                        match col with
                        | IndexedColumn(i,colElements) -> 
                            i.Index,colElements
                        | UnindexedColumn(colElements) -> 
                            getNextColumnIndex(),colElements
                        | _ -> failwith "Expected column elements"
                    let mutable cellIndexSet = 
                        elements 
                        |> List.choose (fun el -> match el with | ColumnElement.IndexedCell(i,_) -> Option.Some i.Index | _ -> None)
                        |> set
                    let getNextIndex () = 
                        let mutable i = 1 
                        while cellIndexSet.Contains i do
                            i <- i + 1
                        cellIndexSet <- Set.add i cellIndexSet
                        i
                   
                    elements
                    |> List.iter (fun el ->
                        
                        match el with 
                        | ColumnElement.IndexedCell(i,(datatype,value)) -> 
                            let row = sheet.Row(i.Index + baseRowIndex - 1)
                            rowIndexSet <- Set.add (i.Index) rowIndexSet
                            let cell = row.[colI]
                            cell.DataType <- datatype
                            cell.Value <- value
                        | ColumnElement.UnindexedCell(datatype,value) -> 
                            let row = sheet.Row(getNextIndex () + baseRowIndex - 1)
                            rowIndexSet <- Set.add row.Index rowIndexSet
                            let cell = row.[colI]
                            cell.DataType <- datatype
                            cell.Value <- value
                    )
                  
                )


            els
            |> splitRowsAndColumns
            |> List.iter (function
                | "Columns", l -> parseColumns l
                | "Table", [SheetElement.Table (name,tableElements)] -> 
                    let maxRow = sheet.CellCollection.MaxRowNumber + 1
                    let range = FsRangeAddress(FsAddress(maxRow,1),FsAddress(maxRow,1))
                    let table = sheet.Table(name,range)
                    Workbook.parseTable sheet.CellCollection table tableElements
                | "Rows", l ->
                    l
                    |> List.iter (function
                        | IndexedRow(i,rowElements) -> 
                            let row = sheet.Row(i.Index)
                            Workbook.parseRow sheet.CellCollection row rowElements                
                
                        | UnindexedRow(rowElements) -> 
                            let row = sheet.Row(getFillRowIndex())
                            Workbook.parseRow sheet.CellCollection row rowElements        
                        | _ -> failwith "Expected row elements"
                    )
                | s, _  -> failwithf "Invalid sheet element %s" s
            )

        member self.Parse() =
            match self with
            | Workbook wbEls -> 
                let workbook = new FsWorkbook()
                wbEls
                |> List.iteri (fun i wbEl ->
                    match wbEl with
                    | UnnamedSheet sheetEls ->
                        let worksheet = FsWorksheet(sprintf "Sheet%i" (i+1))
                        Workbook.parseSheet worksheet sheetEls
                        workbook.AddWorksheet(worksheet) |> ignore

                    | NamedSheet (name,sheetEls) ->
                        let worksheet = FsWorksheet(name)
                        Workbook.parseSheet worksheet sheetEls
                        workbook.AddWorksheet(worksheet) |> ignore            
                )
                workbook