namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ARCtrl.ISA

module ProtocolParameter =
    
    let genID (pp:ProtocolParameter) : string = 
        match pp.ID with
        | Some id -> URI.toString id
        | None -> match pp.ParameterName with
                  | Some n when not n.ID.IsNone -> "#Param_" + n.ID.Value
                  | _ -> "#EmptyProtocolParameter"

    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.toJsonString (oa :?> ProtocolParameter |> genID)
                else GEncode.tryInclude "@id" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.toJsonString "ProtocolParameter"
            GEncode.tryInclude "parameterName" (OntologyAnnotation.encoder options) (oa |> GEncode.tryGetPropertyValue "ParameterName")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<ProtocolParameter> =
        Decode.object (fun get ->
            {
                ID = get.Optional.Field "@id" GDecode.uri
                ParameterName = get.Optional.Field "parameterName" (OntologyAnnotation.decoder options)
            }
        )

    let fromJsonString (s:string) = 
        GDecode.fromJsonString (decoder (ConverterOptions())) s

    let toString (p:ProtocolParameter) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (p:ProtocolParameter) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) p
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (p:ProtocolParameter) = 
    //    File.WriteAllText(path,toString p)

module Component =
    
    let genID (c:Component) = 
        match c.ComponentName with
        | Some cn -> "#Component_" + cn.Replace(" ","_")
        | None -> "#EmptyComponent"

    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.toJsonString (oa :?> Component |> genID)
            if options.IncludeType then "@type", GEncode.toJsonString "Component"
            GEncode.tryInclude "componentName" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "ComponentName")
            GEncode.tryInclude "componentType" (OntologyAnnotation.encoder options) (oa |> GEncode.tryGetPropertyValue "ComponentType")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<Component> =
        fun s json ->           
            Decode.object (fun get ->
                {
                    ComponentName = get.Optional.Field "componentName" GDecode.uri
                    ComponentValue = None
                    ComponentUnit = None
                    ComponentType = get.Optional.Field "componentType" (OntologyAnnotation.decoder options)
                }
            ) s json
            |> Result.map (fun c ->
                let v, unit =  
                    match c.ComponentName with
                    | Some c -> Component.decomposeName c |> fun (a,b) -> Some a,b
                    | Option.None -> Option.None, Option.None
                {c with ComponentValue = v; ComponentUnit = unit}    
            )


    let fromJsonString (s:string) = 
        GDecode.fromJsonString (decoder (ConverterOptions())) s

    let toJsonString (p:Component) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (p:Component) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) p
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (p:Component) = 
    //    File.WriteAllText(path,toString p)

module Protocol =   
    
    let genID (p:Protocol) : string = 
        match p.ID with
        | Some id -> URI.toString id 
        | None -> match p.Uri with
                  | Some u -> u
                  | None -> match p.Name with
                            | Some n -> "#Protocol_" + n.Replace(" ","_")
                            | None -> "#EmptyProtocol" 

    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.toJsonString (oa :?> Protocol |> genID)
                else GEncode.tryInclude "@id" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.toJsonString "Protocol"
            GEncode.tryInclude "name" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Name")
            GEncode.tryInclude "protocolType" (OntologyAnnotation.encoder options) (oa |> GEncode.tryGetPropertyValue "ProtocolType")
            GEncode.tryInclude "description" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Description")
            GEncode.tryInclude "uri" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Uri")
            GEncode.tryInclude "version" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Version")
            GEncode.tryInclude "parameters" (ProtocolParameter.encoder options) (oa |> GEncode.tryGetPropertyValue "Parameters")
            GEncode.tryInclude "components" (Component.encoder options) (oa |> GEncode.tryGetPropertyValue "Components")
            GEncode.tryInclude "comments" (Comment.encoder options) (oa |> GEncode.tryGetPropertyValue "Comments")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<Protocol> =
        Decode.object (fun get ->
            {
                ID = get.Optional.Field "@id" GDecode.uri
                Name = get.Optional.Field "name" Decode.string
                ProtocolType = get.Optional.Field "protocolType" (OntologyAnnotation.decoder options)
                Description = get.Optional.Field "description" Decode.string
                Uri = get.Optional.Field "uri" GDecode.uri
                Version = get.Optional.Field "version" Decode.string
                Parameters = get.Optional.Field "parameters" (Decode.list (ProtocolParameter.decoder options))
                Components = get.Optional.Field "components" (Decode.list (Component.decoder options))
                Comments = get.Optional.Field "comments" (Decode.list (Comment.decoder options))
            }
        )

    let fromJsonString (s:string) = 
        GDecode.fromJsonString (decoder (ConverterOptions())) s

    let toJsonString (p:Protocol) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2
    
    /// exports in json-ld format
    let toStringLD (p:Protocol) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) p
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (p:Protocol) = 
    //    File.WriteAllText(path,toString p)
