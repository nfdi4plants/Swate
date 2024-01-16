namespace FsSpreadsheet.Exceljs


module JsCell =

    open Fable.Core
    open Fable.Core.JsInterop
    open FsSpreadsheet
    open Fable.ExcelJs

    [<Emit("console.log($0)")>]
    let private log (obj:obj) = jsNative

    let writeFromFsCell (fsCell: FsCell) = 
        match fsCell.DataType with
        | Boolean   -> 
            fsCell.ValueAsBool() |> box |> Some
        | Number    -> 
            fsCell.ValueAsFloat() |> box |> Some
        | Date      -> 
            /// Here it will actually show the correct DateTime. But when writing, exceljs will apply local offset. 
            let dt = fsCell.ValueAsDateTime() |> System.DateTimeOffset 
            /// Therefore we add offset and it should work.
            let dt = dt + dt.Offset |> box |> Some
            dt
        | String    -> 
            fsCell.Value |> Some
        | anyElse ->
            let msg = sprintf "ValueType '%A' is not fully implemented in FsSpreadsheet and is handled as string input." anyElse
            #if FABLE_COMPILER_JAVASCRIPT
            log msg
            #else
            printfn "%s" msg
            #endif
            fsCell.Value |> box |> Some 

    /// <summary>
    /// `worksheetName`, `rowIndex` and `columnIndex` are only used for debugging.
    /// </summary>
    /// <param name="worksheetName"></param>
    /// <param name="rowIndex"></param>
    /// <param name="columnIndex"></param>
    /// <param name="jsCell"></param>
    let readToFsCell worksheetName rowIndex columnIndex (jsCell: Cell) =
        let t = enum<Unions.ValueType>(jsCell.``type``)
        let fsadress = FsAddress(jsCell.address)
        let createFscell = fun dt v -> FsCell(v,dt,address = fsadress)
        let vTemp = string jsCell.value.Value
        let fscell =
            match t with
            | ValueType.Boolean -> 
                let b = System.Boolean.Parse vTemp
                createFscell DataType.Boolean b
            | ValueType.Number -> float vTemp |> createFscell DataType.Number
            | ValueType.Date -> 
                let dt = System.DateTime.Parse(vTemp).ToUniversalTime()
                /// Without this step universal time get changed to local time? Exceljs tests will hit this.
                ///
                /// Expected item (from test object): C2 : Sat Oct 14 2023 00:00:00 GMT+0200 (Mitteleuropäische Sommerzeit) | Date
                ///
                /// Actual item (created here): C2 : Sat Oct 14 2023 02:00:00 GMT+0200 (Mitteleuropäische Sommerzeit) | Date
                ///
                /// But logging hour minute showed that the values were given correctly and needed to be reinitialized.
                let dt = System.DateTime(dt.Year,dt.Month,dt.Day,dt.Hour,dt.Minute, dt.Second)
                dt |> createFscell DataType.Date
            | ValueType.String -> vTemp |> createFscell DataType.String
            | ValueType.Formula -> 
                match jsCell.formula with
                | "TRUE()" -> 
                    let b = true
                    createFscell DataType.Boolean b
                | "FALSE()" ->
                    let b = false
                    createFscell DataType.Boolean b
                | anyElse ->
                    let msg = sprintf "ValueType 'Format' (%s) is not fully implemented in FsSpreadsheet and is handled as string input. In %s: (%i,%i)" anyElse worksheetName rowIndex columnIndex
                    log msg
                    anyElse |> createFscell DataType.String
            | ValueType.Hyperlink ->
                //log (c.value.Value?text)
                jsCell.value.Value?hyperlink |> createFscell DataType.String
            | anyElse ->
                let msg = sprintf "ValueType `%A` (%s) is not fully implemented in FsSpreadsheet and is handled as string input. In %s: (%i,%i)" anyElse vTemp worksheetName rowIndex columnIndex
                log msg
                vTemp |> createFscell DataType.String
        fscell


