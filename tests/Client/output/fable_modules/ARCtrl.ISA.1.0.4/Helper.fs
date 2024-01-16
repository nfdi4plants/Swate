module ARCtrl.ISA.Aux

let inline compareSeq (a: seq<'a>) (b: seq<'a>) =
    if Seq.length a = Seq.length b then
        [ for i in 0 .. (Seq.length a) - 1 do yield (Seq.item i a = Seq.item i b) ]
        |> Seq.fold (fun acc e -> acc && e) true
    else 
        false


module ResizeArray =

    let map  f (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            b.Add(f i)
        b       

    let choose f (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            match f i with
            | Some x -> b.Add(x)
            | None -> ()
        b

    let filter f (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            if f i then b.Add(i)
        b

    let fold f s (a : ResizeArray<_>) =
        let mutable state = s
        for i in a do
            state <- f state i
        state

    let foldBack f (a : ResizeArray<_>) s =
        let mutable state = s
        for i in a do
            state <- f i state
        state

    let iter f (a : ResizeArray<_>) =
        for i in a do
            f i

    let reduce f (a : ResizeArray<_>) =
        match a with
        | a when a.Count = 0 -> failwith "ResizeArray.reduce: empty array"
        | a when a.Count = 1 -> a.[0]
        | a -> 
            let mutable state = a.[0]
            for i in 1 .. a.Count - 1 do
                state <- f state a.[i]
            state

    let collect f (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            let c = f i
            for j in c do
                b.Add(j)
        b

    let distinct (a : ResizeArray<_>) =
        let b = ResizeArray<_>()
        for i in a do
            if not (b.Contains(i)) then
                b.Add(i)
        b

    let isEmpty (a : ResizeArray<_>) =
        a.Count = 0

module HashCodes =

    let boxHashOption (a: 'a option) : obj =
        if a.IsSome then a.Value.GetHashCode() else (0).GetHashCode()
        |> box

    let boxHashArray (a: 'a []) : obj =
        a 
        // from https://stackoverflow.com/a/53507559
        |> Array.fold (fun acc o -> 0x9e3779b9 + o.GetHashCode() + (acc <<< 6) + (acc >>> 2) ) 0
        |> box

    let boxHashSeq (a: seq<'a>) : obj =
        a 
        // from https://stackoverflow.com/a/53507559
        |> Seq.fold (fun acc o -> 0x9e3779b9 + o.GetHashCode() + (acc <<< 6) + (acc >>> 2) ) 0
        |> box

[<RequireQualifiedAccess>]
module Option =
 
    /// If the value matches the default, a None is returned, else a Some is returned
    let fromValueWithDefault d v =
        if d = v then None
        else Some v

    /// Applies the function f on the value of the option if it exists, else applies it on the default value. If the result value matches the default, a None is returned
    let mapDefault (d : 'T) (f: 'T -> 'T) (o : 'T option) =
        match o with
        | Some v -> f v
        | None   -> f d
        |> fromValueWithDefault d

    /// Applies the function f on the value of the option if it exists, else returns the default value. 
    let mapOrDefault (d : 'T Option) (f: 'U -> 'T) (o : 'U option) =
        match o with
        | Some v -> Some (f v)
        | None   -> d

module internal List = 
    
    let tryPickAndRemove (f : 'T -> 'U option) (lst : 'T list) =
        let rec loop newList remainingList = 
            match remainingList with
            | h::t -> 
                match f h with
                | Some v -> Some v, newList @ t
                | None -> loop (newList @ [h]) t
            | _ -> None, newList
        loop [] lst

module Dict = 

    open System.Collections.Generic

    let ofSeq (s : seq<'Key*'T>) = 
        let dict = Dictionary()
        s
        |> Seq.iter dict.Add
        dict


    let tryFind (key : 'Key) (dict : Dictionary<'Key,'T>) =
        let b,v = dict.TryGetValue key
        if b then Some v 
        else None

    let ofSeqWithMerge (merge : 'T -> 'T -> 'T) (s : seq<'Key*'T>) = 
        let dict = Dictionary()
        s
        |> Seq.iter (fun (k,v) -> 
            match tryFind k dict with
            | Some v' ->                
                dict.Remove(k) |> ignore
                dict.Add(k,merge v' v)
            | None ->
                dict.Add(k,v)
            )
        dict

open Fable.Core
open Fable.Core.JsInterop

module Update =

    open System
    open Microsoft.FSharp.Reflection

    module ValueOfGenericObj =

        [<Emit("$1")>]
        let get (a:obj) = 
            let ty = typedefof<option<_>>
            let aty = a.GetType()
            // Get option'.Value
            let v = aty.GetProperty("Value")
            if aty.IsGenericType && aty.GetGenericTypeDefinition() = ty then
                // return value if existing
                Some(v.GetValue(a, [| |]))
            else 
                None

    /// matches if the matched object can be parsed to Some 'a and returns it.
    let inline (|SomeObj|_|) (a:obj) =
        // create generalized option type
        // Check for nulls otherwise 'a.GetType()' would fail
        if isNull a 
        then 
            None 
        else
            ValueOfGenericObj.get a
    
    /// Get the type of the IEnumerable elements. E.g. for Array<'T> it would be 'T
    let isMapType (v:obj) =
        #if FABLE_COMPILER
            ARCtrl.ISA.Fable.isMap_generic v
        #else 
        let t = v.GetType()
        // Maps are IEnumerables but are not easily to append. TODO(?)
        let genericMap = typeof<Map<_,_>>
        t.Name = genericMap.Name
        #endif

    let isListType (v:obj) =
        #if FABLE_COMPILER
            ARCtrl.ISA.Fable.isList_generic v
        #else 
        v.GetType().Name.StartsWith "FSharpList`1"
        #endif

    /// Get the type of the IEnumerable elements. E.g. for Array<'T> it would be 'T
    let enumGetInnerType (v:obj) =
        #if FABLE_COMPILER
            typeof<obj>
        #else 
        let t = v.GetType()
        if t.IsArray then t.GetElementType() else
            t.GetGenericArguments() |> Array.head
        #endif

    /// Create an option value of the given type. E.g. for int it would be Some 1
    let inline makeOptionValue typey v isSome =
        let optionType = typeof<unit option>.GetGenericTypeDefinition().MakeGenericType([|typey|])
        let cases = FSharp.Reflection.FSharpType.GetUnionCases(optionType)
        let cases = cases |> Array.partition (fun x -> x.Name = "Some")
        let someCase = fst cases |> Array.exactlyOne
        let noneCase = snd cases |> Array.exactlyOne
        let relevantCase, args =
            match isSome with
            | true -> someCase, [| v |]
            | false -> noneCase, [| |]
        FSharp.Reflection.FSharpValue.MakeUnion(relevantCase, args)

    /// This function accesses the append method of the list/array module and applies it accordingly to the element type.
    let inline appendGenericListsByType l1 l2 (t:Type) =
        #if FABLE_COMPILER
            ARCtrl.ISA.Fable.append_generic l1 l2
        #else
        let fieldT = l1.GetType()
        // https://stackoverflow.com/questions/41253131/how-to-create-an-empty-list-of-a-specific-runtime-type
        System.Reflection.Assembly
            .GetAssembly(typeof<_ list>)
            .GetType(
                if fieldT.IsArray then "Microsoft.FSharp.Collections.ArrayModule" 
                elif fieldT.Name.Contains "FSharpList" then "Microsoft.FSharp.Collections.ListModule"
                else "Microsoft.FSharp.Collections.SeqModule"
                )
            .GetMethod("Append")
            .MakeGenericMethod(t)
            .Invoke(null, [|l1;l2|])
        #endif

    /// This function accesses the distinct method of the list/array module and applies it accordingly to the element type.
    let inline distinctGenericList l1 (t:Type) : obj =
        #if FABLE_COMPILER
            ARCtrl.ISA.Fable.distinct_generic l1
        #else
        let fieldT = l1.GetType()
        // https://stackoverflow.com/questions/41253131/how-to-create-an-empty-list-of-a-specific-runtime-type
        System.Reflection.Assembly
            .GetAssembly(typeof<_ list>)
            .GetType(
                if fieldT.IsArray then "Microsoft.FSharp.Collections.ArrayModule" 
                elif fieldT.Name.Contains "FSharpList" then "Microsoft.FSharp.Collections.ListModule"
                else "Microsoft.FSharp.Collections.SeqModule"
                )
            .GetMethod("Distinct")
            .MakeGenericMethod(t)
            .Invoke(null, [|l1|])
        #endif

    /// updates oldRT with newRT by replacing all values, but appending all lists.
    ///
    /// newRTList@oldRTList
    let rec updateAppend (oldVal: obj) (newVal:obj) = 
        // match all field Values and try to cast them to types.
        match oldVal with
        // Strings are IEnumerable Chars but should not be appenden. So these have to be handled first.
        | :? String ->
            newVal
        | SomeObj(oldInternal) ->
            match oldInternal with
            | :? String -> 
                newVal
            | :? System.Collections.IEnumerable ->
                let newOpt = newVal
                match newOpt with
                | SomeObj newInternal ->
                    updateAppend oldInternal newInternal
                    |> fun v -> makeOptionValue (oldInternal.GetType()) v true
                | _ -> oldVal
            | _ -> 
                newVal
        // Match all IEnumarables, like list, array, seq. These should be appended.
        | :? System.Collections.IEnumerable -> 
            let oldSeq = oldVal :?> System.Collections.IEnumerable
            let newSeq = newVal :?> System.Collections.IEnumerable
            // Maps are IEnumerables but are not easily to append. TODO(?)
            // t is the type of the IEnumerable elements.
            let innerType = enumGetInnerType oldVal
            // If the IEnumerable is a map then we just replace with the new entry.
            if isMapType oldVal then 
                newVal
            else
                appendGenericListsByType oldSeq newSeq innerType
                |> fun l -> distinctGenericList l innerType
        // All others do not need to be appended and can be replaced.
        | others -> 
            newVal


    /// updates oldRT with newRT by replacing all values, but only if the new value is not empty.
    let rec updateOnlyByExisting (oldVal: obj) (newVal:obj) =      
        
        // try to cast values to types to check for isEmpty according to type.
        match oldVal with 
        // Check if newValue isNull = isEmpty
        | _ when Fable.isNone_generic newVal ->
            oldVal
        // Handle OptionTypes
        // https://stackoverflow.com/questions/6289761/how-to-downcast-from-obj-to-optionobj
        // Check of value is option, then check if new value isNone = isEmpty
        | SomeObj(oldInternal) ->
            let newOpt = newVal
            match newOpt with
            | SomeObj newInternal ->
                updateOnlyByExisting oldInternal newInternal
                |> fun v -> makeOptionValue (oldInternal.GetType()) v true
            | _ -> 
                oldVal
        // Check if value is string, then check if new value is "" = isEmpty
        | :? String ->
            let newStr = newVal
            if string newStr = "" then oldVal else newStr
        // https://stackoverflow.com/questions/47280544/determine-if-any-kind-of-list-sequence-array-or-ienumerable-is-empty
        // Check if value is IEnumarable, then cast newValue to Seq and check if isEmpty
        | :? System.Collections.IEnumerable -> 
            let newSeq = newVal
            if newSeq :?> System.Collections.IEnumerable |> Seq.cast |> Seq.isEmpty
            then oldVal 
            else newSeq
        // Others don't need to be checked as they have no clearly enough defined "empty" state
        | _ ->
            newVal
        
    /// updates oldRT with newRT by replacing all values, but only if the new value is not empty.
    let rec updateOnlyByExistingAppend (oldVal: obj) (newVal:obj) =      
        
        // try to cast values to types to check for isEmpty according to type.
        match oldVal with 
        // Check if newValue isNull = isEmpty
        | _ when Fable.isNone_generic newVal ->
            oldVal
        // Handle OptionTypes
        // https://stackoverflow.com/questions/6289761/how-to-downcast-from-obj-to-optionobj
        // Check of value is option, then check if new value isNone = isEmpty
        | SomeObj(oldInternal) ->
            let newOpt = newVal
            match newOpt with
            | SomeObj newInternal ->
                updateOnlyByExistingAppend oldInternal newInternal
                |> fun v -> makeOptionValue (oldInternal.GetType()) v true
            | _ -> 
                oldVal
        // Check if value is string, then check if new value is "" = isEmpty
        | :? String ->
            let newStr = newVal
            if string newStr = "" then oldVal else newStr
        // https://stackoverflow.com/questions/47280544/determine-if-any-kind-of-list-sequence-array-or-ienumerable-is-empty
        // Check if value is IEnumarable, then cast newValue to Seq and check if isEmpty
        | :? System.Collections.IEnumerable -> 
            let oldSeq = oldVal
            let newSeq = newVal

            // innterType is the type of the IEnumerable elements.
            let innerType = enumGetInnerType oldVal
            // If the IEnumerable is a map then we just replace with the new entry.
            if isMapType oldVal then 
                newVal
            else
                let r = 
                    appendGenericListsByType oldSeq newSeq innerType
                    |> fun l -> distinctGenericList l innerType
                r |> box
        // Others don't need to be checked as they have no clearly enough defined "empty" state
        | _ ->
            newVal  

    /// This type specifies the exact manner on how complex types will be updated
    type UpdateOptions = 
        /// Updates all existing fields by replacing them with the corresponding new fields
        | UpdateAll
        /// Updates all existing fields by replacing them with the corresponding new fields if the new field is not empty.
        ///
        /// Empty string = ""; Empty IEnumerable (Set, List, Array, Seq, Map); Empty Option = None; null
        | UpdateByExisting
        /// Updates all existing fields by replacing them with the corresponding new fields, except any lists which will be appended
        ///
        /// Maps are currently not appended
        | UpdateAllAppendLists
        /// Updates all existing fields by replacing them with the corresponding new fields if the new field is not empty, except any lists which will be appended
        ///
        /// Empty string = ""; Empty IEnumerable (Set, List, Array, Seq, Map); Empty Option = None; null
        | UpdateByExistingAppendLists


        /// This function will update recordType_1 with the values given in recordType_2 as specified by UpdateOption.
        member inline this.updateRecordType (recordType_1:'a) (recordType_2:'a) =    
            match this with
            | UpdateAll ->
                recordType_2
            | UpdateAllAppendLists ->
                (FSharp.Reflection.FSharpValue.GetRecordFields recordType_1,FSharp.Reflection.FSharpValue.GetRecordFields recordType_2)
                ||> Array.map2 updateAppend
                |> fun fields -> FSharpValue.MakeRecord(typeof<'a>, fields) :?> 'a
            | UpdateByExisting ->
                (FSharp.Reflection.FSharpValue.GetRecordFields recordType_1,FSharp.Reflection.FSharpValue.GetRecordFields recordType_2)
                ||> Array.map2 updateOnlyByExisting
                |> fun fields -> FSharpValue.MakeRecord(typeof<'a>, fields) :?> 'a
            | UpdateByExistingAppendLists ->
                (FSharp.Reflection.FSharpValue.GetRecordFields recordType_1,FSharp.Reflection.FSharpValue.GetRecordFields recordType_2)
                ||> Array.map2 updateOnlyByExistingAppend
                |> fun fields -> FSharpValue.MakeRecord(typeof<'a>, fields) :?> 'a

    /// Creates a union of the items of the two given lists, merges items whose keys exist in both lists using the given update function.
    let inline mergeUpdateLists (updateOptions : UpdateOptions) (mapping : 'T -> 'Key) (list1 : 'T list) (list2 : 'T list) = 
        try
            let map1 = list1 |> List.map (fun v -> mapping v, v) |> Dict.ofSeqWithMerge updateOptions.updateRecordType
            let map2 = list2 |> List.map (fun v -> mapping v, v) |> Dict.ofSeqWithMerge updateOptions.updateRecordType
            List.append (list1 |> List.map mapping) (list2 |> List.map mapping)
            |> List.distinct
            |> List.map (fun k ->
                match Dict.tryFind k map1, Dict.tryFind k map2 with
                | Some v1, Some v2 -> updateOptions.updateRecordType v1 v2
                | Some v1, None -> v1
                | None, Some v2 -> v2
                | None, None -> failwith "If this fails, then I don't know how to program"
            )
        with
        | err -> failwith $"Could not mergeUpdate {typeof<'T>.Name} list: \n{err.Message}"