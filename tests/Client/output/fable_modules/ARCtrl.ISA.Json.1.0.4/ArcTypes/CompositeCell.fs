namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ARCtrl.ISA

module CompositeCell =

  let [<Literal>] CellType = "celltype"
  let [<Literal>] CellValues = "values"

  let encoder (cc: CompositeCell) =
    let oaToJsonString (oa:OntologyAnnotation) = OntologyAnnotation.encoder (ConverterOptions()) oa
    let t, v =
      match cc with
      | CompositeCell.FreeText s-> "FreeText", [Encode.string s]
      | CompositeCell.Term t -> "Term", [oaToJsonString t]
      | CompositeCell.Unitized (v, unit) -> "Unitized", [Encode.string v; oaToJsonString unit]
    Encode.object [
      CellType, Encode.string t
      CellValues, v |> Encode.list
    ]

  let decoder : Decoder<CompositeCell> =
    Decode.object (fun get ->
      match get.Required.Field (CellType) Decode.string with
      | "FreeText" -> 
        let s = get.Required.Field (CellValues) (Decode.index 0 Decode.string)
        CompositeCell.FreeText s
      | "Term" -> 
        let oa = get.Required.Field (CellValues) (Decode.index 0 <| OntologyAnnotation.decoder (ConverterOptions()) )
        CompositeCell.Term oa
      | "Unitized" -> 
        let v = get.Required.Field (CellValues) (Decode.index 0 <| Decode.string )
        let oa = get.Required.Field (CellValues) (Decode.index 1 <| OntologyAnnotation.decoder (ConverterOptions()) )
        CompositeCell.Unitized (v, oa)
      | anyelse -> failwithf "Error reading CompositeCell from json string: %A" anyelse 
    ) 

[<AutoOpen>]
module CompositeCellExtensions =

    type CompositeCell with
        static member fromJsonString (jsonString: string) : CompositeCell = 
            match Decode.fromString CompositeCell.decoder jsonString with
            | Ok r -> r
            | Error e -> failwithf "Error. Unable to parse json string to CompositeCell: %s" e

        member this.ToJsonString(?spaces) : string =
            let spaces = defaultArg spaces 0
            Encode.toString spaces (CompositeCell.encoder this)

        static member toJsonString(a:CompositeCell) = a.ToJsonString()