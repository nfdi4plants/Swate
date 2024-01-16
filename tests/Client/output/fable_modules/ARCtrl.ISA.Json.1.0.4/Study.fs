namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ARCtrl.ISA
open System.IO

module StudyMaterials = 

    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            GEncode.tryInclude "sources" (Source.encoder options) (oa |> GEncode.tryGetPropertyValue "Sources")
            GEncode.tryInclude "samples" (Sample.encoder options) (oa |> GEncode.tryGetPropertyValue "Samples")
            GEncode.tryInclude "otherMaterials" (Material.encoder options) (oa |> GEncode.tryGetPropertyValue "OtherMaterials")
        ]
        |> GEncode.choose
        |> Encode.object
    
    let decoder (options : ConverterOptions) : Decoder<StudyMaterials> =
        Decode.object (fun get ->
            {
                Sources = get.Optional.Field "sources" (Decode.list (Source.decoder options))
                Samples = get.Optional.Field "samples" (Decode.list (Sample.decoder options))
                OtherMaterials = get.Optional.Field "otherMaterials" (Decode.list (Material.decoder options))
            }
        )


module Study =
    
    let genID (s:Study) : string = 
        match s.ID with
        | Some id -> URI.toString id
        | None -> match s.FileName with
                  | Some n -> "#Study" + n.Replace(" ","_")
                  | None -> match s.Identifier with
                            | Some id -> "#Study_" + id.Replace(" ","_")
                            | None -> match s.Title with
                                      | Some t -> "#Study_" + t.Replace(" ","_")
                                      | None -> "#EmptyStudy"
    
    let encoder (options : ConverterOptions) (oa : obj) = 
        [
            if options.SetID then "@id", GEncode.toJsonString (oa :?> Study |> genID)
                else GEncode.tryInclude "@id" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "ID")
            if options.IncludeType then "@type", GEncode.toJsonString "Study"
            GEncode.tryInclude "filename" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "FileName")
            GEncode.tryInclude "identifier" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Identifier")
            GEncode.tryInclude "title" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Title")
            GEncode.tryInclude "description" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "Description")
            GEncode.tryInclude "submissionDate" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "SubmissionDate")
            GEncode.tryInclude "publicReleaseDate" GEncode.toJsonString (oa |> GEncode.tryGetPropertyValue "PublicReleaseDate")
            GEncode.tryInclude "publications" (Publication.encoder options) (oa |> GEncode.tryGetPropertyValue "Publications")
            GEncode.tryInclude "people" (Person.encoder options) (oa |> GEncode.tryGetPropertyValue "Contacts")
            GEncode.tryInclude "studyDesignDescriptors" (OntologyAnnotation.encoder options) (oa |> GEncode.tryGetPropertyValue "StudyDesignDescriptors")
            GEncode.tryInclude "protocols" (Protocol.encoder options) (oa |> GEncode.tryGetPropertyValue "Protocols")
            GEncode.tryInclude "materials" (StudyMaterials.encoder options) (oa |> GEncode.tryGetPropertyValue "Materials")
            GEncode.tryInclude "processSequence" (Process.encoder options) (oa |> GEncode.tryGetPropertyValue "ProcessSequence")
            GEncode.tryInclude "assays" (Assay.encoder options) (oa |> GEncode.tryGetPropertyValue "Assays")            
            GEncode.tryInclude "factors" (Factor.encoder options) (oa |> GEncode.tryGetPropertyValue "Factors")
            GEncode.tryInclude "characteristicCategories" (MaterialAttribute.encoder options) (oa |> GEncode.tryGetPropertyValue "CharacteristicCategories")            
            GEncode.tryInclude "unitCategories" (OntologyAnnotation.encoder options) (oa |> GEncode.tryGetPropertyValue "UnitCategories")
            GEncode.tryInclude "comments" (Comment.encoder options) (oa |> GEncode.tryGetPropertyValue "Comments")
        ]
        |> GEncode.choose
        |> Encode.object

    let decoder (options : ConverterOptions) : Decoder<Study> =
        Decode.object (fun get ->
            {
                ID = get.Optional.Field "@id" GDecode.uri
                FileName = get.Optional.Field "filename" Decode.string
                Identifier = get.Optional.Field "identifier" Decode.string
                Title = get.Optional.Field "title" Decode.string
                Description = get.Optional.Field "description" Decode.string
                SubmissionDate = get.Optional.Field "submissionDate" Decode.string
                PublicReleaseDate = get.Optional.Field "publicReleaseDate" Decode.string
                Publications = get.Optional.Field "publications" (Decode.list (Publication.decoder options))
                Contacts = get.Optional.Field "people" (Decode.list (Person.decoder options))
                StudyDesignDescriptors = get.Optional.Field "studyDesignDescriptors" (Decode.list (OntologyAnnotation.decoder options))
                Protocols = get.Optional.Field "protocols" (Decode.list (Protocol.decoder options))
                Materials = get.Optional.Field "materials" (StudyMaterials.decoder options)
                Assays = get.Optional.Field "assays" (Decode.list (Assay.decoder options))
                Factors = get.Optional.Field "factors" (Decode.list (Factor.decoder options))
                CharacteristicCategories = get.Optional.Field "characteristicCategories" (Decode.list (MaterialAttribute.decoder options))
                UnitCategories = get.Optional.Field "unitCategories" (Decode.list (OntologyAnnotation.decoder options))
                ProcessSequence = get.Optional.Field "processSequence" (Decode.list (Process.decoder options))
                Comments = get.Optional.Field "comments" (Decode.list (Comment.decoder options))
            }
        )

    let fromJsonString (s:string) = 
        GDecode.fromJsonString (decoder (ConverterOptions())) s

    let toJsonString (p:Study) = 
        encoder (ConverterOptions()) p
        |> Encode.toString 2

    /// exports in json-ld format
    let toStringLD (s:Study) = 
        encoder (ConverterOptions(SetID=true,IncludeType=true)) s
        |> Encode.toString 2
