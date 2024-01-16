namespace FsSpreadsheet.DSL

open FsSpreadsheet

[<AutoOpen>]
type Message = 
    | Text of string
    | Exception of exn

    static member message (s : string) = Text s

    static member message (e : #exn) = Exception e

    member this.MapText(m : string -> string) =
        match this with
        | Text s -> Text (m s)
        | Exception e -> this

    member this.AsString() = 
        match this with
        | Text s -> s
        | Exception e -> e.Message

    member this.TryText() = 
        match this with
        | Text s -> Some s
        | _ -> None

    member this.TryException() = 
        match this with
        | Exception e -> Some e
        | _ -> None

    member this.IsTxt = 
        match this with
        | Text s -> true
        | _ -> false

    member this.IsExc = 
        match this with
        | Text s -> true
        | _ -> false


module Messages =
    
    let format (ms : Message list) =
        ms
        |> List.map (fun m -> m.AsString())
        |> List.reduce (fun a b -> a + ";" + b)

    let fail (ms : Message list) =
        let s = format ms
        if ms |> List.exists (fun m -> m.IsExc) then
            printfn "s"
            raise (ms |> List.pick (fun m -> m.TryException()))
        else
            failwith s


[<AutoOpen>]
type SheetEntity<'T> =

    | Some of 'T * Message list
    | NoneOptional of Message list
    | NoneRequired of Message list

    static member some (v : 'T) : SheetEntity<'T> = SheetEntity.Some (v,[])

    /// Get messages
    member this.Messages =

        match this with 
        | Some (f,errs) -> errs
        | NoneOptional errs -> errs
        | NoneRequired errs -> errs

[<AutoOpen>]
module SheetEntityExtensions =
    type SheetEntity<'T> with
        member inline this.Value =
            match this with 
            | Some (f,errs) -> f
            | NoneOptional ms | NoneRequired ms when ms = [] -> 
                #if FABLE_COMPILER
                failwith $"SheetEntity does not contain Value."
                #else
                failwith $"SheetEntity of type {typeof<'T>.Name} does not contain Value."
                #endif
            | NoneOptional ms | NoneRequired ms -> 
                let appendedMessages = Messages.format ms
                #if FABLE_COMPILER
                failwith $"SheetEntity does not contain Value: \n\t{appendedMessages}"
                #else
                failwith $"SheetEntity of type {typeof<'T>.Name} does not contain Value: \n\t{appendedMessages}"
                #endif

type Value = DataType * obj

type CellElement = Value * int option

type ColumnIndex = 
    
    | Col of int 

    member self.Index = match self with | Col i -> i

type RowIndex = 
    
    | Row of int

    member self.Index = match self with | Row i -> i

type ColumnElement =
    | IndexedCell of RowIndex * Value
    | UnindexedCell of Value

type RowElement =
    | IndexedCell of ColumnIndex * Value
    | UnindexedCell of Value

type TableElement = 
    | UnindexedRow of RowElement list
    | UnindexedColumn of ColumnElement list

    member this.IsRow =
        match this with 
        | UnindexedRow _ -> true
        | _ -> false

    member this.IsColumn =
        match this with 
        | UnindexedColumn _ -> true
        | _ -> false

type SheetElement = 
    | Table of string * TableElement list
    | IndexedRow of RowIndex * RowElement list
    | UnindexedRow of RowElement list
    | IndexedColumn of ColumnIndex * ColumnElement list
    | UnindexedColumn of ColumnElement list
    | IndexedCell of RowIndex * ColumnIndex * Value
    | UnindexedCell of Value


type WorkbookElement =
    | UnnamedSheet of SheetElement list
    | NamedSheet of string * SheetElement list

type Workbook =
    | Workbook of WorkbookElement list
