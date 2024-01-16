namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ARCtrl.ISA
open System.IO

module Publication =    
    
    let genID (p:Publication) = 
        match p.DOI with
        | Some doi -> doi
        | None -> match p.PubMedID with
                  | Some id -> id
                  | None -> match p.Title with
                            | Some t -> "#Pub_" + t.Replace(" ","_")
                            | None -> "#EmptyPublication"

    let rec encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.toJsonString (oa :?> Publication |> genID)
            if options.IncludeType then "@type", GEncode.toJsonString "Publication"
            GEncode.tryInclude "pubMedID" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "PubMedID")
            GEncode.tryInclude "doi" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "DOI")
            GEncode.tryInclude "authorList" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Authors")
            GEncode.tryInclude "title" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Title")
            GEncode.tryInclude "status" (OntologyAnnotation.encoder options) (oa |> GEncode.tryGetPropertyValue "Status")
            GEncode.tryInclude "comments" (Comment.encoder options) (oa |> GEncode.tryGetPropertyValue "Comments")
        ]
        |> GEncode.choose
        |> Encode.object

    let rec decoder (options : ConverterOptions) : Decoder<Publication> =
        Decode.object (fun get ->
            {
                PubMedID = get.Optional.Field "pubMedID" GDecode.uri
                DOI = get.Optional.Field "doi" Decode.string
                Authors = get.Optional.Field "authorList" Decode.string
                Title = get.Optional.Field "title" Decode.string
                Status = get.Optional.Field "status" (OntologyAnnotation.decoder options)
                Comments = get.Optional.Field "comments" (Decode.array (Comment.decoder options))
            }
            
        )

    let fromJsonString (s:string) = 
        GDecode.fromJsonString (decoder (ConverterOptions())) s

    let toJsonString (p:Publication) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2

    /// exports in json-ld format
    let toStringLD (p:Publication) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) p
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (p:Publication) = 
    //    File.WriteAllText(path,toString p)