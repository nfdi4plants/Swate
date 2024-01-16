namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ARCtrl.ISA

module ArcTable =
   let encoder (table: ArcTable) =
      let keyEncoder : Encoder<int*int> = Encode.tuple2 Encode.int Encode.int
      let valueEncoder = CompositeCell.encoder
      Encode.object [
        "name", Encode.string table.Name
        if table.Headers.Count <> 0 then
          "header", Encode.list [
              for h in table.Headers do yield CompositeHeader.encoder h
          ]
        if table.Values.Count <> 0 then
          "values", Encode.map keyEncoder valueEncoder ([for KeyValue(k,v) in table.Values do yield k, v] |> Map)
      ] 

   let decoder : Decoder<ArcTable> =
       Decode.object(fun get ->
           let decodedHeader = get.Optional.Field "header" (Decode.list CompositeHeader.decoder) |> Option.defaultValue List.empty |> ResizeArray 
           let keyDecoder : Decoder<int*int> = Decode.tuple2 Decode.int Decode.int
           let valueDecoder = CompositeCell.decoder
           let decodedValues = get.Optional.Field "values" (Decode.map' keyDecoder valueDecoder) |> Option.defaultValue Map.empty |> System.Collections.Generic.Dictionary
           ArcTable.create(
               get.Required.Field "name" Decode.string,
               decodedHeader,
               decodedValues
           )
       )

[<AutoOpen>]
module ArcTableExtensions =

    type ArcTable with
        static member fromJsonString (jsonString: string) : ArcTable = 
            match Decode.fromString ArcTable.decoder jsonString with
            | Ok r -> r
            | Error e -> failwithf "Error. Unable to parse json string to ArcTable: %s" e

        member this.ToJsonString(?spaces) : string =
            let spaces = defaultArg spaces 0
            Encode.toString spaces (ArcTable.encoder this)

        static member toJsonString(a:ArcTable) = a.ToJsonString()