namespace FsSpreadsheet.DSL

open FsSpreadsheet
open Microsoft.FSharp.Quotations
open Expression

type ReduceOperation =
    | Concat of char

    member this.Reduce (values : Value list) : Value =
        match this with
        | Concat separator -> 
            DataType.String,
            values 
            |> List.map (snd >> string)
            |> List.reduce (fun a b -> $"{a}{separator}{b}")
            |> box

type CellBuilder() =

    let mutable reducer = Concat ','

    static member Empty : SheetEntity<Value list> = SheetEntity.NoneOptional []

    // -- Computation Expression methods --> 
    #if FABLE_COMPILER
    #else
    member _.Quote  (quotation: Quotations.Expr<'T>) =
        quotation
    #endif

    member inline this.Zero() : SheetEntity<Value list> = SheetEntity.NoneOptional []

    member this.SignMessages (messages : Message list) : Message list =
        messages
        |> List.map (fun m -> m.MapText (sprintf "In Cell: %s"))

    member inline this.Yield(n: RequiredSource<unit>) = 
        n

    member inline this.Yield(n: OptionalSource<unit>) = 
        n

    member this.Yield(ro : ReduceOperation) : SheetEntity<Value list> =
        reducer <- ro
        SheetEntity.NoneOptional []

    member inline this.Yield(s : string) : SheetEntity<Value list> =
        SheetEntity.some [DataType.String,s]

    member inline this.Yield(value: Value) : SheetEntity<Value list> =
        SheetEntity.some [value]

    member inline this.Yield(value: SheetEntity<Value>) : SheetEntity<Value list> =
        match value with 
        | Some (v,messages) -> 
            SheetEntity.Some ([v], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline this.Yield(n: 'a when 'a :> System.IFormattable) = 
        let v = DataType.InferCellValue n
        SheetEntity.some [v]

    member inline this.Yield(s : string option) : SheetEntity<Value list> =
        match s with
        | Option.Some s -> this.Yield s
        | None -> NoneRequired [message "Value is missing"]

    member inline this.Yield(n: 'a option when 'a :> System.IFormattable) = 
        match n with
        | Option.Some s -> this.Yield s
        | None -> NoneRequired [message "Value is missing"]

    member inline this.YieldFrom(ns: SheetEntity<Value list> seq) =   
        ns
        |> Seq.fold (fun (state : SheetEntity<Value list>) we ->
            this.Combine(state,we)

        ) CellBuilder.Empty

    member inline this.For(vs : seq<'T>, f : 'T -> SheetEntity<Value list>) =
        vs
        |> Seq.map f
        |> this.YieldFrom

    member this.Combine(wx1: SheetEntity<Value list>, wx2: SheetEntity<Value list>) : SheetEntity<Value list>=
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
        
    member this.Combine(wx1: RequiredSource<unit>, wx2: SheetEntity<Value list>) =
        RequiredSource (wx2)
        
    member this.Combine(wx1: SheetEntity<Value list>, wx2: RequiredSource<unit>) =
        RequiredSource (wx1)

    member this.Combine(wx1: OptionalSource<unit>, wx2: SheetEntity<Value list>) =
        OptionalSource wx2

    member this.Combine(wx1: SheetEntity<Value list>, wx2: OptionalSource<unit>) =
        OptionalSource wx1

    member this.Combine(wx1: RequiredSource<SheetEntity<Value list>>, wx2: SheetEntity<Value list>) =
        this.Combine(wx1.Source,wx2) 
        |> RequiredSource

    member this.Combine(wx1: SheetEntity<Value list>, wx2: RequiredSource<SheetEntity<Value list>>) =
        this.Combine(wx1,wx2.Source) 
        |> RequiredSource

    member this.Combine(wx1: OptionalSource<SheetEntity<Value list>>, wx2: SheetEntity<Value list>) =
        this.Combine(wx1.Source,wx2) 
        |> OptionalSource

    member this.Combine(wx1: SheetEntity<Value list>, wx2: OptionalSource<SheetEntity<Value list>>) =
        this.Combine(wx1,wx2.Source) 
        |> OptionalSource

    member this.AsCellElement(children: SheetEntity<Value list>) : SheetEntity<CellElement> =
        match children with
        | Some (v :: [],messages) ->
            let cellElement = v, None
            SheetEntity.Some(cellElement, messages)
        | Some (vals,messages) ->
            let cellElement = reducer.Reduce (vals), None
            SheetEntity.Some(cellElement, messages)
        | NoneRequired messages -> NoneRequired messages
        | NoneOptional messages -> NoneOptional messages

    #if FABLE_COMPILER
    member inline this.Run(children: OptionalSource<SheetEntity<Value list>>) =
        try 
            match this.AsCellElement (children.Source) with
            | NoneRequired m -> NoneOptional m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: RequiredSource<SheetEntity<Value list>>) =
        try 
            match this.AsCellElement (children.Source) with
            | NoneOptional m -> NoneRequired m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: SheetEntity<Value list>) =
        this.AsCellElement (children)
        |> fun v -> v.Value
    #else
    member inline this.Run(children: Expr<OptionalSource<SheetEntity<Value list>>>) =
        try 
            match this.AsCellElement ((eval<OptionalSource<SheetEntity<Value list>>> children).Source) with
            | NoneRequired m -> NoneOptional m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: Expr<RequiredSource<SheetEntity<Value list>>>) =
        try 
            match this.AsCellElement ((eval<RequiredSource<SheetEntity<Value list>>> children).Source) with
            | NoneOptional m -> NoneRequired m
            | se -> se
        with
        | err -> NoneOptional [message err.Message]

    member inline this.Run(children: Expr<SheetEntity<Value list>>) =
        this.AsCellElement (eval<SheetEntity<Value list>> children)
        |> fun v -> v.Value
    #endif
    member inline _.Delay(n: unit -> 'T) = n()
