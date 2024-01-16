namespace FsSpreadsheet.DSL

open FsSpreadsheet
open FsSpreadsheet.DSL

open Microsoft.FSharp.Quotations
open Expression

type WorkbookBuilder() =

    static member Empty : SheetEntity<WorkbookElement list> = SheetEntity.some []

    // -- Computation Expression methods --> 

    member inline this.Zero() : SheetEntity<WorkbookElement list> = SheetEntity.some []

    member this.SignMessages (messages : Message list) : Message list =
        messages
        |> List.map (fun m -> m.MapText (sprintf "In Workbook: %s"))

    member inline _.Yield(c: WorkbookElement) =
        SheetEntity.some [c]

    member inline _.Yield(w: SheetEntity<WorkbookElement>) =
        match w with 
        | Some (w, messages) -> 
            SheetEntity.Some ([w], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(cs: WorkbookElement list) =
        SheetEntity.some cs
    
    member inline _.Yield(cs: SheetEntity<WorkbookElement list> ) =
        cs

    member inline _.Yield(c: SheetEntity<SheetElement list>) =
        match c with 
        | Some (re,messages) -> 
            SheetEntity.Some ([WorkbookElement.UnnamedSheet re], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(cs: SheetElement list) =
        SheetEntity.some [WorkbookElement.UnnamedSheet cs]

    member inline _.Yield(c: SheetEntity<string * SheetElement list>) =
        match c with 
        | Some ((name,re),messages) -> 
            SheetEntity.Some ([WorkbookElement.NamedSheet (name,re)], messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member inline _.Yield(cs: string * SheetElement list) =
        SheetEntity.some [WorkbookElement.NamedSheet (cs)]


    member inline this.YieldFrom(ns: SheetEntity<WorkbookElement list> seq) =   
        ns
        |> Seq.fold (fun state we ->
            this.Combine(state,we)

        ) WorkbookBuilder.Empty


    member inline this.For(vs : seq<'T>, f : 'T -> SheetEntity<WorkbookElement list>) =
        vs
        |> Seq.map f
        |> this.YieldFrom


    member inline this.Run(children: SheetEntity<WorkbookElement list>) =
        match children with 
        | Some (children,messages) -> 
            SheetEntity.Some (Workbook children, messages)
        | NoneOptional messages -> 
            NoneOptional messages
        | NoneRequired messages -> 
            NoneRequired messages

    member this.Combine(wx1: SheetEntity<WorkbookElement list>, wx2: SheetEntity<WorkbookElement list>) : SheetEntity<WorkbookElement list>=
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
        
    member inline _.Delay(n: unit -> SheetEntity<WorkbookElement list>) = n()
