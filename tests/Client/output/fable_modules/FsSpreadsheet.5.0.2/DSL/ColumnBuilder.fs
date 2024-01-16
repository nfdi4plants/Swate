namespace FsSpreadsheet.DSL

open FsSpreadsheet
open FsSpreadsheet.DSL

open Microsoft.FSharp.Quotations
open Expression


type ColumnBuilder() =

    static member Empty : SheetEntity<ColumnElement list> = SheetEntity.some []

    // -- Computation Expression methods --> 
    #if FABLE_COMPILER
    #else
    member _.Quote  (quotation: Quotations.Expr<'T>) =
        quotation
    #endif

    member inline this.Zero() : SheetEntity<ColumnElement list> = SheetEntity.some []

    member this.SignMessages (messages : Message list) : Message list =
        messages
        |> List.map (fun m -> m.MapText (sprintf "In Column: %s"))

    member inline _.Yield(c: ColumnElement) =
        SheetEntity.some [c]

    member inline _.Yield(cs: ColumnElement list) =
        SheetEntity.some cs

    member inline _.Yield(c: SheetEntity<ColumnElement>) =
        match c with 
        | Some (c,messages) -> 
            SheetEntity.Some ([c], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(cs: SheetEntity<ColumnElement list>) =
        cs

    member inline _.Yield(c: SheetEntity<CellElement>) =
        match c with 
        | Some ((v,Option.Some i),messages) -> 
            SheetEntity.Some ([ColumnElement.IndexedCell (Row i,v)], messages)
        | Some ((v,None),messages) -> 
            SheetEntity.Some ([ColumnElement.UnindexedCell v], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(c: CellElement) =
        let re = 
            match c with
            | v, Option.Some i -> ColumnElement.IndexedCell (Row i, v)
            | v, None -> ColumnElement.UnindexedCell v
        SheetEntity.some [re]

    member inline _.Yield(c: SheetEntity<Value>) =
        match c with 
        | Some ((v),messages) -> 
            SheetEntity.Some ([ColumnElement.UnindexedCell v], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(cs: CellElement list) =
        let res = 
            cs 
            |> List.map (function
                | v, Option.Some i -> ColumnElement.IndexedCell (Row i, v)
                | v, None -> ColumnElement.UnindexedCell v
            )
        SheetEntity.some res

    member inline this.Yield(cs: seq<SheetEntity<CellElement>>) : SheetEntity<ColumnElement list>=
        cs
        |> Seq.map this.Yield
        |> Seq.reduce (fun a b -> this.Combine(a,b))


    member inline _.Yield(s : string) = 
        let v = DataType.InferCellValue s
        SheetEntity.some [ColumnElement.UnindexedCell v]

    member inline this.Yield(n: RequiredSource<unit>) = 
        n

    member inline this.Yield(n: OptionalSource<unit>) = 
        n

    member inline this.Yield(n: 'a when 'a :> System.IFormattable) = 
        let v = DataType.InferCellValue n
        SheetEntity.some [ColumnElement.UnindexedCell v]       

    member inline this.YieldFrom(ns: SheetEntity<ColumnElement list> seq) =   
        ns
        |> Seq.fold (fun (state : SheetEntity<ColumnElement list>) we ->
            this.Combine(state,we)

        ) ColumnBuilder.Empty


    member inline this.For(vs : seq<'T>, f : 'T -> SheetEntity<ColumnElement list>) =
        vs
        |> Seq.map f
        |> this.YieldFrom


    member this.Combine(wx1: SheetEntity<ColumnElement list>, wx2: SheetEntity<ColumnElement list>) : SheetEntity<ColumnElement list>=
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
       
    member this.Combine(wx1: RequiredSource<unit>, wx2: SheetEntity<ColumnElement list>) =
        RequiredSource (wx2)
        
    member this.Combine(wx1: SheetEntity<ColumnElement list>, wx2: RequiredSource<unit>) =
        RequiredSource (wx1)

    member this.Combine(wx1: OptionalSource<unit>, wx2: SheetEntity<ColumnElement list>) =
        OptionalSource wx2

    member this.Combine(wx1: SheetEntity<ColumnElement list>, wx2: OptionalSource<unit>) =
        OptionalSource wx1

    member this.Combine(wx1: RequiredSource<SheetEntity<ColumnElement list>>, wx2: SheetEntity<ColumnElement list>) =
        this.Combine(wx1.Source,wx2) 
        |> RequiredSource

    member this.Combine(wx1: SheetEntity<ColumnElement list>, wx2: RequiredSource<SheetEntity<ColumnElement list>>) =
        this.Combine(wx1,wx2.Source) 
        |> RequiredSource

    member this.Combine(wx1: OptionalSource<SheetEntity<ColumnElement list>>, wx2: SheetEntity<ColumnElement list>) =
        this.Combine(wx1.Source,wx2) 
        |> OptionalSource

    member this.Combine(wx1: SheetEntity<ColumnElement list>, wx2: OptionalSource<SheetEntity<ColumnElement list>>) =
        this.Combine(wx1,wx2.Source) 
        |> OptionalSource

    #if FABLE_COMPILER
    member inline this.Run(children: OptionalSource<SheetEntity<ColumnElement list>>) =
        try 
            match children.Source with
            | NoneRequired m -> NoneOptional m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: RequiredSource<SheetEntity<ColumnElement list>>) =
        try 
            match children.Source with
            | NoneOptional m -> NoneRequired m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: SheetEntity<ColumnElement list>) =
        children.Value
    #else
    member inline this.Run(children: Expr<OptionalSource<SheetEntity<ColumnElement list>>>) =
        try 
            match (eval<OptionalSource<SheetEntity<ColumnElement list>>> children).Source with
            | NoneRequired m -> NoneOptional m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]
    
    member inline this.Run(children: Expr<RequiredSource<SheetEntity<ColumnElement list>>>) =
        try 
            match (eval<RequiredSource<SheetEntity<ColumnElement list>>> children).Source with
            | NoneOptional m -> NoneRequired m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: Expr<SheetEntity<ColumnElement list>>) =
        (eval<SheetEntity<ColumnElement list>> children).Value
    #endif
    member inline _.Delay(n: unit -> 'T) = n()