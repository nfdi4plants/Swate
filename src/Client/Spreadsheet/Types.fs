module Spreadsheet.Types

open Fable.SimpleJson

type FooterReorderData = {
    OriginOrder: int
    OriginId: string
} with
    static member create o_order o_id= {
        OriginOrder = o_order
        OriginId = o_id
    }
    member this.toJson() = Json.serialize this
    static member ofJson (json:string) = Json.tryParseAs<FooterReorderData>(json)

[<System.Obsolete("Not sure, just marked for now")>]
module Map =
    let maxKey (m:Map<'Key,'Value>) =
        m.Keys |> Seq.max

    ///<summary>This function operates on a integer tuple as map key. It will return the highest int for fst and highest int for snd.</summary>
    let maxKeys (m:Map<int*int,_>) =
        let maxColumnKey = m.Keys |> Seq.maxBy fst |> fst 
        let maxRowKey = m.Keys |> Seq.maxBy snd |> snd
        maxColumnKey, maxRowKey

