namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

open Fable.Core
open Fable.Core.JsInterop

[<RequireQualifiedAccess>]
module GEncode = 

    [<Emit("$1[$0]")>]
    let getFieldFable (name : string) (object : 'T) = jsNative

    /// Try to get a property value from a record by its name 
    let inline tryGetPropertyValue (name : string) (object : 'T) =
        #if FABLE_COMPILER
            getFieldFable name object
        #else
        let property = 
            FSharp.Reflection.FSharpType.GetRecordFields(object.GetType())
            |> Array.tryFind (fun property -> property.Name.Contains name)
        property
        |> Option.bind (fun property -> 
            match FSharp.Reflection.FSharpValue.GetRecordField (object,property) with
            | ARCtrl.ISA.Aux.Update.SomeObj o -> 
                Some o
            | o when isNull o -> 
                None
            | o -> 
                Some o
        )
        #endif

    let inline toJsonString (value : obj) = 
        match value with
        | :? string as s -> Encode.string s
        | _ -> Encode.nil

    let inline choose (kvs : (string * JsonValue) list) = 
        kvs
        |> List.choose (fun (k,v) -> 
            if v = Encode.nil then None
            else Some (k,v)
        )

    /// Try to encode the given object using the given encoder, or return Encode.nil if the object is null
    ///
    /// If the object is a sequence, encode each element using the given encoder and return the resulting sequence
    let tryInclude name (encoder : obj -> JsonValue) (value : obj option) = 
        name,
        match value with
        #if FABLE_COMPILER
        | Some (:? System.Collections.IEnumerable as v) ->                  
            !!Seq.map encoder v |> Encode.seq
        #else
        | Some(:? seq<obj> as os) ->                 
            Seq.map encoder os |> Encode.seq
        #endif
        | Some(o) -> encoder o
        | _ -> Encode.nil


    // This seems to fail because due to dotnet not able to match the boxed lists against nongeneric System.Collections.IEnumerable
    //
    // ->    ///// Try to encode the given object using the given encoder, or return Encode.nil if the object is null
    // ->    /////
    // ->    ///// If the object is a sequence, encode each element using the given encoder and return the resulting sequence
    // ->    //let tryInclude name (encoder : obj -> JsonValue) (value : obj option) = 
    // ->    //    name,
    // ->    //    match value with
    // ->    //    | Some(:? System.Collections.IEnumerable as v) ->          
    // ->    //        let os = 
    // ->    //            #if FABLE_COMPILER
    // ->    //            !!v
    // ->    //            #else
    // ->    //            v |> Seq.cast<obj>
    // ->    //            #endif
    // ->    //        Seq.map encoder os |> Encode.seq
    // ->    //    | Some(o) -> encoder o
    // ->    //    | _ -> Encode.nil