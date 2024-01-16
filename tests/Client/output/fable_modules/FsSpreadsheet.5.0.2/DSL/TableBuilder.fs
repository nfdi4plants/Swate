namespace FsSpreadsheet.DSL

open FsSpreadsheet
open FsSpreadsheet.DSL

open Microsoft.FSharp.Quotations
open Expression

type TableBuilder(name : string) =

    static member Empty : SheetEntity<TableElement list> = SheetEntity.some []

    // -- Computation Expression methods --> 

    member inline this.Zero() : SheetEntity<TableElement list> = SheetEntity.some []

    member this.Name = name

    member this.SignMessages (messages : Message list) : Message list =
        messages
        |> List.map (fun m -> m.MapText (sprintf "In Sheet %s: %s" name))

    member inline _.Yield(se: TableElement) =
        SheetEntity.some [se]

    member inline _.Yield(cs: TableElement list) =
        SheetEntity.some cs
   
    member inline _.Yield(cs: SheetEntity<TableElement list>) =
        cs

    member inline _.Yield(se: SheetEntity<TableElement>) =
        match se with 
        | Some (s,messages) -> 
            SheetEntity.Some ([s], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(c: SheetEntity<RowElement list>) =
        match c with 
        | Some ((re),messages) -> 
            SheetEntity.Some ([TableElement.UnindexedRow re], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(cs: RowElement list) =
        SheetEntity.some [TableElement.UnindexedRow cs]

    member inline _.Yield(cs: RowBuilder) =
        SheetEntity.some [TableElement.UnindexedRow []]

    member inline _.Yield(c: SheetEntity<ColumnElement list>) =
        match c with 
        | Some ((re),messages) -> 
            SheetEntity.Some ([TableElement.UnindexedColumn re], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(cs: ColumnElement list) =
        SheetEntity.some [TableElement.UnindexedColumn cs]

    member inline _.Yield(cs: ColumnBuilder) =
        SheetEntity.some [TableElement.UnindexedColumn []]

    member inline this.YieldFrom(ns: SheetEntity<TableElement list> seq) =   
        ns
        |> Seq.fold (fun state we ->
            this.Combine(state,we)

        ) TableBuilder.Empty

    member inline this.For(vs : seq<'T>, f : 'T -> SheetEntity<TableElement list>) =
        vs
        |> Seq.map f
        |> this.YieldFrom

    member this.Run(children: SheetEntity<TableElement list>) =
        match children with 
        | Some (se,messages) -> 
            SheetEntity.Some ((name,se), messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages


    member this.Combine(wx1: SheetEntity<TableElement list>, wx2: SheetEntity<TableElement list>) : SheetEntity<TableElement list>=
        match wx1,wx2 with
        // If both contain content, combine the content
        | Some (l1,messages1), Some (l2,messages2) ->
            Some (List.append l1 l2
            ,List.append messages1 messages2)

        // If any of the two is missing and was required, return a missing required
        | _, NoneRequired messages2 ->
            NoneRequired (List.append wx1.Messages messages2)

        | NoneRequired messages1, _ ->
            NoneRequired (List.append messages1 wx2.Messages)

        // If only one of the two is missing and was optional, take the content of the functioning one
        | Some (f1,messages1), NoneOptional messages2 ->
            Some (f1
            ,List.append messages1 messages2)

        | NoneOptional messages1, Some (f2,messages2) ->
            Some (f2
            ,List.append messages1 messages2)

        // If both are missing and were optional, return a missing optional
        | NoneOptional messages1, NoneOptional messages2 ->
            NoneOptional (List.append messages1 messages2)
        
    member inline _.Delay(n: unit -> SheetEntity<TableElement list>) = n()
