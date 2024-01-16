namespace ARCtrl.ISA.Spreadsheet

        
module Seq = 
    
    /// If at least i values exist in seq a, builds a new array that contains the elements of the given seq, exluding the first i elements
    let trySkip i s =
        try
            Seq.skip i s
            |> Some
        with
        | _ -> None

module internal Array = 
    
    let ofIndexedSeq (s : seq<int*string>) = 
        Array.init 
            (Seq.maxBy fst s |> fst |> (+) 1)
            (fun i -> 
                match Seq.tryFind (fst >> (=) i) s with
                | Some (i,x) -> x
                | None -> ""               
            )

    /// If at least i values exist in array a, builds a new array that contains the elements of the given array, exluding the first i elements
    let trySkip i a =
        try
            Array.skip i a
            |> Some
        with
        | _ -> None

    /// Returns Item of array at index i if existing, else returns default value
    let tryItemDefault i d a = 
        match Array.tryItem i a with
        | Some v -> v
        | None -> d

    let map4 (f: 'A -> 'B -> 'C -> 'D -> 'T) (aa:'A []) (ba:'B []) (ca:'C []) (da:'D []) =
        if not (aa.Length = ba.Length && ba.Length = ca.Length && ca.Length = da.Length) then
            failwith ""
        Array.init aa.Length (fun i -> f aa.[i] ba.[i] ca.[i] da.[i])


module internal List =

    let tryPickDefault (chooser : 'T -> 'U Option) (d : 'U) (list : 'T list) =
        match List.tryPick chooser list with
        | Some v -> v
        | None -> d

    let unzip4 (l : ('A*'B*'C*'D) list ) =
        let rec loop la lb lc ld l =
            match l with
            | (a,b,c,d) :: l -> loop (a::la) (b::lb) (c::lc) (d::ld) l
            | [] -> List.rev la, List.rev lb, List.rev lc, List.rev ld
        loop [] [] [] [] l


module internal Dictionary =

    let tryGetValue k (dict:System.Collections.Generic.Dictionary<'K,'V>) = 
        let b,v = dict.TryGetValue(k)
        // Only get value if 
        if b then Some v
        else 
            None

    let tryGetString k (dict:System.Collections.Generic.Dictionary<'K,string>) = 
        let b,v = dict.TryGetValue(k)
        // Only get value if 
        if b && v.Trim() <> ""
        then 
            v.Trim() |> Some
        else 
            None