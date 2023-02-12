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
        