namespace FsSpreadsheet.DSL

open Microsoft.FSharp.Quotations
open FsSpreadsheet
open Expression

[<AutoOpen>]
type DSL =
    
    /// Create a cell from a value
    static member inline cell = CellBuilder()

    /// Create a row from cells
    static member inline row = RowBuilder()
    
    /// Create a column from cells
    static member inline column = ColumnBuilder()

    /// Create a table from either exclusively rows or exclusively columns. 
    static member inline table name = TableBuilder(name)

    /// Create a sheet from rows, tables and columns
    static member inline sheet name = SheetBuilder(name)

    /// Create a workbook from sheets
    static member inline workbook = WorkbookBuilder()

    /// Transforms any given missing element to an optional.
    static member opt (elem : SheetEntity<'T list>) = 
        match elem with
        | Some (f,messages) -> elem
        | NoneOptional (messages) -> NoneOptional(messages)
        | NoneRequired (messages) -> NoneOptional(messages)

    #if FABLE_COMPILER
    #else
    /// Transforms any given missing element expression to an optional.
    static member opt (elem : Expr<SheetEntity<'T list>>) = 
        try 
            let elem = eval<SheetEntity<'T list>> elem
            DSL.opt elem
        with
        | err -> 
            NoneOptional([message err.Message])
    #endif

    /// Drops the cell with the given message
    static member dropCell message : SheetEntity<Value> = NoneRequired [message]

    /// Drops the row with the given message
    static member dropRow message : SheetEntity<RowElement> = NoneRequired [message]

    /// Drops the column with the given message
    static member dropColumn message : SheetEntity<ColumnElement> = NoneRequired [message]

    /// Drops the sheet with the given message
    static member dropSheet message : SheetEntity<SheetElement> = NoneRequired [message]

    /// Drops the workbook with the given message
    static member dropWorkbook message : SheetEntity<WorkbookElement> = NoneRequired [message]

