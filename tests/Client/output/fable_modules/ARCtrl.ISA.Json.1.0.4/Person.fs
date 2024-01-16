namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ARCtrl.ISA
open System.IO

module Person =   
    
    let genID (p:Person) : string = 
        match p.ID with
        | Some id -> URI.toString id
        | None -> 
            let orcid = match p.Comments with
                        | Some cl -> cl |> Array.tryPick (fun c ->
                            match (c.Name,c.Value) with
                            | (Some n,Some v) -> if (n="orcid" || n="Orcid" || n="ORCID") then Some v else None
                            | _ -> None )
                        | None -> None
            match orcid with
            | Some o -> o
            | None -> match p.EMail with
                      | Some e -> e.ToString()
                      | None -> match (p.FirstName,p.MidInitials,p.LastName) with 
                                | (Some fn,Some mn,Some ln) -> "#" + fn.Replace(" ","_") + "_" + mn.Replace(" ","_") + "_" + ln.Replace(" ","_")
                                | (Some fn,None,Some ln) -> "#" + fn.Replace(" ","_") + "_" + ln.Replace(" ","_")
                                | (None,None,Some ln) -> "#" + ln.Replace(" ","_")
                                | (Some fn,None,None) -> "#" + fn.Replace(" ","_")
                                | _ -> "#EmptyPerson"

    let rec encoder (options : ConverterOptions) (oa : obj) = 
        let oa = oa :?> Person |> Person.setCommentFromORCID
        [
            if options.SetID then "@id", GEncode.toJsonString (oa |> genID)
                else GEncode.tryInclude "@id" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.toJsonString "Person"
            GEncode.tryInclude "firstName" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "FirstName")
            GEncode.tryInclude "lastName" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "LastName")
            GEncode.tryInclude "midInitials" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "MidInitials")
            GEncode.tryInclude "email" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "EMail")
            GEncode.tryInclude "phone" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Phone")
            GEncode.tryInclude "fax" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Fax")
            GEncode.tryInclude "address" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Address")
            GEncode.tryInclude "affiliation" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Affiliation")
            GEncode.tryInclude "roles" (OntologyAnnotation.encoder options) (oa |> GEncode.tryGetPropertyValue "Roles")
            GEncode.tryInclude "comments" (Comment.encoder options) (oa |> GEncode.tryGetPropertyValue "Comments")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<Person> =
        Decode.object (fun get ->
            {
                ID = get.Optional.Field "@id" GDecode.uri
                ORCID = None
                FirstName = get.Optional.Field "firstName" Decode.string
                LastName = get.Optional.Field "lastName" Decode.string
                MidInitials = get.Optional.Field "midInitials" Decode.string
                EMail = get.Optional.Field "email" Decode.string
                Phone = get.Optional.Field "phone" Decode.string
                Fax = get.Optional.Field "fax" Decode.string
                Address = get.Optional.Field "address" Decode.string
                Affiliation = get.Optional.Field "affiliation" Decode.string
                Roles = get.Optional.Field "roles" (Decode.array (OntologyAnnotation.decoder options))
                Comments = get.Optional.Field "comments" (Decode.array (Comment.decoder options))
            }
            |> Person.setOrcidFromComments
            
        )

    let fromJsonString (s:string) = 
        GDecode.fromJsonString (decoder (ConverterOptions())) s

    let toJsonString (p:Person) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2

    /// exports in json-ld format
    let toStringLD (p:Person) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) p
        |> Encode.toString 2

    //let fromFile (path : string) = 
    //    File.ReadAllText path 
    //    |> fromString

    //let toFile (path : string) (p:Person) = 
    //    File.WriteAllText(path,toString p)