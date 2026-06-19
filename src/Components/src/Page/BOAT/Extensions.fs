[<AutoOpen>]
module ExtensionsJSON

open System
open Fable.Core
open Fable.Core.JsInterop
open Types
open ARCtrl.Json
open Thoth.Json.Core

let log o = Browser.Dom.console.log o

module Jsonkeys = 
    [<Literal>]
    let Key = "key"
    [<Literal>]
    let KeyType = "KeyType"
    [<Literal>]
    let Body = "Body"
    [<Literal>]
    let IsOpen = "isOpen"
    [<Literal>]
    let Height = "height"
 
    

let encoderAnno (anno: Annotation) = //encodes annotation to json         
    [
        Encode.tryInclude Jsonkeys.Key OntologyAnnotation.encoder (Some anno.Search.Key)
        Encode.tryInclude Jsonkeys.KeyType Encode.string (
             match anno.Search.KeyType with
                | CompositeHeaderDiscriminate.Input ->  Some "Input"
                | CompositeHeaderDiscriminate.Output -> Some "Output"
                | CompositeHeaderDiscriminate.Parameter -> Some "Parameter"
                | CompositeHeaderDiscriminate.Component -> Some "Component"
                | CompositeHeaderDiscriminate.Characteristic -> Some "Characteristic"
                | CompositeHeaderDiscriminate.Factor -> Some "Factor"
                | CompositeHeaderDiscriminate.Date -> Some "Date"
                | CompositeHeaderDiscriminate.Performer -> Some "Performer"
                | CompositeHeaderDiscriminate.ProtocolDescription -> Some "ProtocolDescription"
                | CompositeHeaderDiscriminate.ProtocolType -> Some "ProtocolType"
                | CompositeHeaderDiscriminate.ProtocolUri -> Some "ProtocolUri"
                | CompositeHeaderDiscriminate.ProtocolVersion -> Some "ProtocolVersion"
                | _ -> Some "freetext" //default case for ProtocolREF

        )
        Encode.tryInclude Jsonkeys.Body CompositeCell.encoder (Some anno.Search.Body)
        Encode.tryInclude Jsonkeys.IsOpen Encode.bool (Some anno.IsOpen)
        Encode.tryInclude Jsonkeys.Height Encode.float (Some anno.Height)
    ]
    |> Encode.choose
    |> Encode.object


let keyType: Decoder<CompositeHeaderDiscriminate> =
    { new Decoder<CompositeHeaderDiscriminate> with
        member _.Decode(helpers, value) =
            if helpers.isString value then
                Ok(helpers.asString value |> CompositeHeaderDiscriminate.fromString)
            else
                ("", BadPrimitive("a composite header", value)) |> Error
    }
let decoderAnno : Decoder<Annotation list> = //decodes json to annotation  
    Decode.list (
        Decode.object (fun get ->
            {
            IsOpen = get.Required.Field Jsonkeys.IsOpen Decode.bool
            Search = {
                Key = get.Required.Field  Jsonkeys.Key OntologyAnnotation.decoder
                KeyType = get.Required.Field 
                    Jsonkeys.KeyType
                    keyType

                Body = get.Required.Field  Jsonkeys.Body CompositeCell.decoder
                }
            Height = get.Required.Field Jsonkeys.Height Decode.float
            }
        )
    )
    
type URL = 
  abstract member createObjectURL: Browser.Types.File -> string

[<Emit("URL")>]
let URL: URL = jsNative

[<RequireQualifiedAccess>]
module StaticFile =

    /// Function that imports a static file by it's relative path.
    let inline import (path: string) : string = importDefault<string> path

/// Stylesheet API
/// let private stylesheet = Stylesheet.load "./fancy.module.css"
/// stylesheet.["fancy-class-name"] which returns a string
module Stylesheet =

    type IStylesheet =
        [<Emit "$0[$1]">]
        abstract Item : className:string -> string

    /// Loads a CSS module and makes the classes within available
    let inline load (path: string) = importDefault<IStylesheet> path