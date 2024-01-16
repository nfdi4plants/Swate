[<RequireQualifiedAccess>]
module ARCtrl.ISA.Fable

open Fable.Core

#if FABLE_COMPILER_PYTHON
open Fable.Core.PyInterop
#else
open Fable.Core.JsInterop
#endif

[<Emit("console.log($0)")>]
let print (msg:obj) : unit = 
    printfn "%O" msg

[<Emit("to_string(type($0))")>]
let getPyType (x: obj) : string = 
    failwith "this should never hit"


let isList_python l1 = 
    getPyType(l1).Contains("FSharpList")
    //Fable.Core.PyInterop.pyInstanceof(l1, typeof<List<_>>)

let isSeq_python l1 = 
    let t = getPyType(l1)
    t.Contains("Enumerator_Seq") || t.Contains("Enumerable")
    //Fable.Core.PyInterop.pyInstanceof(l1, typeof<seq<_>>)

let isArray_python l1 = 
    getPyType(l1).Contains("list")

let isMap_python l1 = 
    getPyType(l1).Contains("FSharpMap")

let isNone_python l1 = 
    //l1 = (box None)
    l1.ToString() = "None"


let isMap_json l1 = l1.ToString().StartsWith("map [")

let isSeq_json l1 = l1.ToString().StartsWith("seq [")

let isList_json l1 = 
    let s = l1.ToString()
    s.StartsWith("[") && (s.StartsWith "seq [" |> not)


let isNone_json l1 =
    !!isNull l1


let isList_generic l1 = 
    #if FABLE_COMPILER_PYTHON
        isList_python l1
    #else
        isList_json l1
    #endif

let isSeq_generic l1 =
    #if FABLE_COMPILER_PYTHON
        isSeq_python l1
    #else
        isSeq_json l1
    #endif

let isArray_generic l1 =
    #if FABLE_COMPILER_PYTHON
        isArray_python l1
    #else
        isList_json l1
    #endif

let isMap_generic l1 =
    #if FABLE_COMPILER_PYTHON
        isMap_python l1
    #else
        isMap_json l1
    #endif

let isNone_generic l1 =
    #if FABLE_COMPILER_PYTHON
        isNone_python l1
    #endif
    #if FABLE_COMPILER
        isNone_json l1
    #endif
    #if !FABLE_COMPILER 
        l1 = null
    #endif


let append_generic l1 l2 =
    // This isNull check is necessary because in the API.update functionality we only check the type of l1. 
    // There l1 can be a sequence (Some sequence in dotnet) and l2 would be an undefined (None in dotnet).
    // We need to check if l2 is null and if so return l1.
    if isNone_generic l2 then l1

    else
        if isSeq_generic l1 then 
            !!Seq.append l1 l2
        elif isList_generic l1 then 
            !!List.append l1 l2
        else
            !!Array.append l1 l2


let distinct_generic l1 =

    if isSeq_generic l1 then 
        !!Seq.distinct l1
    elif isList_generic l1 then 
        !!List.distinct l1
    else
        !!Array.distinct l1

let hashSeq (s:seq<'a>) = 
    s
    |> Seq.map (fun x -> x.GetHashCode())
    |> Seq.reduce (fun a b -> a + b)