namespace FsSpreadsheet

module SheetBuilder =
   
    module internal Dictionary =

        let tryGetValue k (dict : System.Collections.Generic.Dictionary<'K,'V>) = 
            let b,v = dict.TryGetValue(k)
            // Only get value if 
            if b 
            then 
                Some v
            else 
                None

        let length (dict : System.Collections.Generic.Dictionary<'K,'V>) = 
            dict.Count

    type FieldMap<'T> =
        {
            CellTransformers : ('T -> FsCell -> FsCell) list
            HeaderTransformers : ('T -> FsCell -> FsCell) list
            ColumnWidth : float option
            RowHeight : ('T -> float option) option
            AdjustToContents: bool
            Hash : string
        }
        with
            static member empty<'T>() = {
                CellTransformers = []
                HeaderTransformers = []
                ColumnWidth = None
                RowHeight = None
                AdjustToContents = false
                Hash = System.Guid.NewGuid().ToString()
            }

            static member create<'T>(mapRow : 'T -> FsCell -> FsCell) =
                let empty = FieldMap<'T>.empty()
                { empty with 
                    CellTransformers = List.append empty.CellTransformers [mapRow] 
                }

            member self.header(name : string) =
                let transformer _ (cell : FsCell) = cell.SetValueAs(name); cell
                { self with HeaderTransformers = List.append self.HeaderTransformers [transformer] }

            member self.header(mapHeader : 'T -> string) =
                let transformer (value : 'T) (cell : FsCell) = cell.SetValueAs(mapHeader value); cell
                { self with HeaderTransformers = List.append self.HeaderTransformers [transformer] }

            member self.adjustToContents() =
                { self with AdjustToContents = true }

            static member field<'T>(map: 'T -> int) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(map row); cell)
            static member field<'T>(map: 'T -> string) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(map row); cell)
            static member field<'T>(map: 'T -> System.DateTime) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(map row); cell)
            static member field<'T>(map: 'T -> bool) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(map row); cell)
            static member field<'T>(map: 'T -> double) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(map row); cell)
            static member field<'T>(map: 'T -> int option) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(Option.toNullable (map row)); cell)
            static member field<'T>(map: 'T -> System.DateTime option) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(Option.toNullable (map row)); cell)
            static member field<'T>(map: 'T -> bool option) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(Option.toNullable (map row)); cell)
            static member field<'T>(map: 'T -> double option) = FieldMap<'T>.create(fun row cell -> cell.SetValueAs(Option.toNullable (map row)); cell)
            static member field<'T>(map: 'T -> string option) = FieldMap<'T>.create(fun row cell ->
                match map row with
                | None -> cell
                | Some text -> cell.SetValueAs(text); cell
            )

    type FsTable with 

        member self.Populate(cells : FsCellsCollection, data : seq<'T>, fields : FieldMap<'T> list) =
            let headerTransformerGroups = fields |> List.map (fun field -> field.HeaderTransformers)
            let noHeadersAvailable =
                headerTransformerGroups
                |> List.concat
                |> List.isEmpty

            let headersAvailable = not noHeadersAvailable
       
            if headersAvailable && (self.ShowHeaderRow = false) then self.ShowHeaderRow <- headersAvailable
            
            let startAddress = self.RangeAddress.FirstAddress

            let startRowIndex = if headersAvailable then startAddress.RowNumber + 1 else startAddress.RowNumber
            
            for (rowIndex, row) in Seq.indexed data do
                let activeRowIndex = rowIndex + startRowIndex
                for field in fields do

                    let headerCell = FsCell.createEmpty()
                    for header in field.HeaderTransformers do ignore (header row headerCell)

                    let headerString = 
                        if headerCell.ValueAsString() = "" then 
                            field.Hash 
                        else 
                            headerCell.ValueAsString()

                    let tableField = self.Field(headerString,cells)

                    let activeCell = tableField.Column.Cell(activeRowIndex,cells) // .Cell(index,self.CellCollection)
                    for transformer in field.CellTransformers do
                        ignore (transformer row activeCell)

                    //if field.AdjustToContents then
                    //    let currentColumn = activeCell.WorksheetColumn()
                    //    currentColumn.AdjustToContents() |> ignore
                    //    activeRow.AdjustToContents() |> ignore

                    //match field.ColumnWidth with
                    //| Some givenWidth ->
                    //    let currentColumn = activeCell.WorksheetColumn()
                    //    currentColumn.Width <- givenWidth
                    //| None -> ()

                    //match field.RowHeight with
                    //| Some givenHeightFn ->
                    //    match givenHeightFn row with
                    //    | Some givenHeight ->
                    //        activeRow.Height <- givenHeight
                    //    | None ->
                    //        ()
                    //| None ->
                    //    ()

        static member populate<'T> (table : FsTable, cells : FsCellsCollection, data : seq<'T>, fields : FieldMap<'T> list) : unit =
            table.Populate(cells,data,fields)

    type FsWorksheet with
        
        member self.Populate(data : seq<'T>, fields : FieldMap<'T> list) =
            let headerTransformerGroups = fields |> List.map (fun field -> field.HeaderTransformers)
            let noHeadersAvailable =
                headerTransformerGroups
                |> List.concat
                |> List.isEmpty

            let headersAvailable = not noHeadersAvailable
       
            let headers : System.Collections.Generic.Dictionary<string,int> = System.Collections.Generic.Dictionary()

            for (rowIndex, row) in Seq.indexed data do
                let startRowIndex = if headersAvailable then 2 else 1
                let activeRow = self.Row(rowIndex + startRowIndex)
                for field in fields do

                    let headerCell = FsCell.createEmpty()
                    for header in field.HeaderTransformers do ignore (header row headerCell)
                
                    let index = 
                        let hasHeader, headerString = 
                            if headerCell.Value = "" then 
                                false, field.Hash 
                            else true, headerCell.ValueAsString() 

                        match Dictionary.tryGetValue (headerString) headers with
                        | Some int -> int
                        | None ->
                            let v = headerString
                            let i = headers.Count + 1
                            headers.Add(v,i)
                            if hasHeader then
                                self.Row(1).[i].CopyFrom(headerCell) |> ignore
                            i

                    let activeCell = activeRow.[index]
                    for transformer in field.CellTransformers do
                        ignore (transformer row activeCell)

                    //if field.AdjustToContents then
                    //    let currentColumn = activeCell.WorksheetColumn()
                    //    currentColumn.AdjustToContents() |> ignore
                    //    activeRow.AdjustToContents() |> ignore

                    //match field.ColumnWidth with
                    //| Some givenWidth ->
                    //    let currentColumn = activeCell.WorksheetColumn()
                    //    currentColumn.Width <- givenWidth
                    //| None -> ()

                    //match field.RowHeight with
                    //| Some givenHeightFn ->
                    //    match givenHeightFn row with
                    //    | Some givenHeight ->
                    //        activeRow.Height <- givenHeight
                    //    | None ->
                    //        ()
                    //| None ->
                    //    ()

            self.SortRows()

        static member populate<'T>(sheet : FsWorksheet, data : seq<'T>, fields : FieldMap<'T> list) : unit =
            sheet.Populate(data,fields)

        static member createFrom (name : string, data : seq<'T>, fields : FieldMap<'T> list) (*: byte[]*) =          
            let sheet = FsWorksheet(name)
            FsWorksheet.populate(sheet, data, fields)
            sheet

        static member createFrom (data : seq<'T>, fields : FieldMap<'T> list) (*: byte[]*) =
            FsWorksheet.createFrom("Sheet1", data, fields)

        member self.PopulateTable(tableName : string, startAddress : FsAddress, data : seq<'T>, fields : FieldMap<'T> list) =
            let headerTransformerGroups = fields |> List.map (fun field -> field.HeaderTransformers)
            let noHeadersAvailable =
                headerTransformerGroups
                |> List.concat
                |> List.isEmpty

            let headersAvailable = not noHeadersAvailable

            let table = self.Table(tableName,FsRangeAddress(startAddress,startAddress),headersAvailable)
            
            table.Populate(self.CellCollection,data,fields)

            self.SortRows()

        static member createTableFrom (name : string, tableName : string, data : seq<'T>, fields : FieldMap<'T> list) (*: byte[]*) =          
            let sheet = FsWorksheet(name)
            sheet.PopulateTable(tableName, FsAddress (1,1), data, fields)
            sheet



    type FsWorkbook with

        member self.Populate<'T>(name : string, data : seq<'T>, fields : FieldMap<'T> list) : unit =
            self.InitWorksheet(name) |> ignore
            let sheet = self.GetWorksheets() |> Seq.find (fun s -> s.Name = name)
            FsWorksheet.populate(sheet, data, fields)

        static member populate<'T> (workbook : FsWorkbook, name : string, data : seq<'T>, fields : FieldMap<'T> list) : unit =
            workbook.Populate(name, data, fields)

        static member createFrom (name : string, data : seq<'T>, fields : FieldMap<'T> list) (*: byte[]*) =
            let workbook = new FsWorkbook()
            FsWorkbook.populate(workbook, name, data, fields)
            workbook

        static member createFrom (data: seq<'T>, fields: FieldMap<'T> list) (*: byte[]*) =
            FsWorkbook.createFrom("Sheet1", data, fields)

        static member createFrom (sheets : FsWorksheet list)=
            let workbook = new FsWorkbook()
            sheets
            |> List.iter (fun sheet -> workbook.AddWorksheet(sheet) |> ignore)
            workbook
