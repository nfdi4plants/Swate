namespace FsSpreadsheet.DSL

open FsSpreadsheet
open FsSpreadsheet.DSL

open Microsoft.FSharp.Quotations
open Expression

type RowBuilder() =

    static member Empty : SheetEntity<RowElement list> = SheetEntity.some []

    // -- Computation Expression methods --> 
    #if FABLE_COMPILER
    #else
    member _.Quote  (quotation: Quotations.Expr<'T>) =
        quotation
    #endif

    member inline this.Zero() : SheetEntity<RowElement list> = SheetEntity.some []

    member this.SignMessages (messages : Message list) : Message list =
        messages
        |> List.map (fun m -> m.MapText (sprintf "In Row: %s"))

    member inline this.Yield(n: RequiredSource<unit>) = 
        n

    member inline this.Yield(n: OptionalSource<unit>) = 
        n

    member inline _.Yield(c: RowElement) =
        SheetEntity.some [c]

    member inline _.Yield(cs: RowElement list) =
        SheetEntity.some cs

    member inline _.Yield(c: SheetEntity<RowElement>) =
        match c with 
        | Some (re,messages) -> 
            SheetEntity.Some ([re], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(cs: SheetEntity<RowElement list>) =
        cs

    member inline _.Yield(c: SheetEntity<CellElement>) =
        match c with 
        | Some ((v,Option.Some i),messages) -> 
            SheetEntity.Some ([RowElement.IndexedCell (Col i,v)], messages)
        | Some ((v,None),messages) -> 
            SheetEntity.Some ([RowElement.UnindexedCell v], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(c: CellElement) =
        let re = 
            match c with
            | v, Option.Some i -> RowElement.IndexedCell (Col i, v)
            | v, None -> RowElement.UnindexedCell v
        SheetEntity.some [re]

    member inline _.Yield(c: SheetEntity<Value>) =
        match c with 
        | Some ((v),messages) -> 
            SheetEntity.Some ([RowElement.UnindexedCell v], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline this.Yield(cs: seq<SheetEntity<CellElement>>) : SheetEntity<RowElement list>=
        cs
        |> Seq.map this.Yield
        |> Seq.reduce (fun a b -> this.Combine(a,b))

    member inline _.Yield(c: Value) =
        SheetEntity.some [RowElement.UnindexedCell c]

    member inline this.Yield(n: 'a when 'a :> System.IFormattable) = 
        let v = DataType.InferCellValue n
        SheetEntity.some [RowElement.UnindexedCell v]

    member inline _.Yield(s : string) = 
        let v = DataType.InferCellValue s
        SheetEntity.some [RowElement.UnindexedCell v]

    member inline this.YieldFrom(ns: SheetEntity<RowElement list> seq) =   
        ns
        |> Seq.fold (fun (state : SheetEntity<RowElement list>) we ->
            this.Combine(state,we)

        ) RowBuilder.Empty

    member inline this.For(vs : seq<'T>, f : 'T -> SheetEntity<RowElement list>) =
        vs
        |> Seq.map f
        |> this.YieldFrom

    member this.Combine(wx1: SheetEntity<RowElement list>, wx2: SheetEntity<RowElement list>) : SheetEntity<RowElement list>=
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
        
    member this.Combine(wx1: RequiredSource<unit>, wx2: SheetEntity<RowElement list>) =
        RequiredSource (wx2)
        
    member this.Combine(wx1: SheetEntity<RowElement list>, wx2: RequiredSource<unit>) =
        RequiredSource (wx1)

    member this.Combine(wx1: OptionalSource<unit>, wx2: SheetEntity<RowElement list>) =
        OptionalSource wx2

    member this.Combine(wx1: SheetEntity<RowElement list>, wx2: OptionalSource<unit>) =
        OptionalSource wx1

    member this.Combine(wx1: RequiredSource<SheetEntity<RowElement list>>, wx2: SheetEntity<RowElement list>) =
        this.Combine(wx1.Source,wx2) 
        |> RequiredSource

    member this.Combine(wx1: SheetEntity<RowElement list>, wx2: RequiredSource<SheetEntity<RowElement list>>) =
        this.Combine(wx1,wx2.Source) 
        |> RequiredSource

    member this.Combine(wx1: OptionalSource<SheetEntity<RowElement list>>, wx2: SheetEntity<RowElement list>) =
        this.Combine(wx1.Source,wx2) 
        |> OptionalSource

    member this.Combine(wx1: SheetEntity<RowElement list>, wx2: OptionalSource<SheetEntity<RowElement list>>) =
        this.Combine(wx1,wx2.Source) 
        |> OptionalSource

    #if FABLE_COMPILER
    member inline this.Run(children: OptionalSource<SheetEntity<RowElement list>>) =
        try 
            match children.Source with
            | NoneRequired m -> NoneOptional m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: RequiredSource<SheetEntity<RowElement list>>) =
        try 
            match children.Source with
            | NoneOptional m -> NoneRequired m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: SheetEntity<RowElement list>) =
        children.Value
    #else
    member inline this.Run(children: Expr<OptionalSource<SheetEntity<RowElement list>>>) =
        try 
            match (eval<OptionalSource<SheetEntity<RowElement list>>> children).Source with
            | NoneRequired m -> NoneOptional m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: Expr<RequiredSource<SheetEntity<RowElement list>>>) =
        try 
            match (eval<RequiredSource<SheetEntity<RowElement list>>> children).Source with
            | NoneOptional m -> NoneRequired m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: Expr<SheetEntity<RowElement list>>) =
        (eval<SheetEntity<RowElement list>> children).Value
    #endif

    member inline _.Delay(n: unit -> 'T) = n()