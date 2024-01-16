namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ARCtrl.ISA

module IOType =
  let encoder (io:IOType) = Encode.string <| io.ToString()

  let decoder : Decoder<IOType> = Decode.string |> Decode.andThen (
      fun s -> IOType.ofString s |> Decode.succeed
    )

[<AutoOpen>]
module IOTypeExtensions =

    type IOType with
        static member fromJsonString (jsonString: string) : IOType = 
            match Decode.fromString IOType.decoder jsonString with
            | Ok r -> r
            | Error e -> failwithf "Error. Unable to parse json string to IOType: %s" e

        member this.ToJsonString(?spaces) : string =
            let spaces = defaultArg spaces 0
            Encode.toString spaces (IOType.encoder this)

        static member toJsonString(a:IOType) = a.ToJsonString()