namespace ARCtrl.ISA.Json

#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif
open ARCtrl.ISA

open JsonHelper

module ArcStudy = 
    let encoder (study:ArcStudy) = 
        Encode.object [ 
            "Identifier", Encode.string study.Identifier
            if study.Title.IsSome then
                "Title", Encode.string study.Title.Value
            if study.Description.IsSome then
                "Description", Encode.string study.Description.Value
            if study.SubmissionDate.IsSome then
                "SubmissionDate", Encode.string study.SubmissionDate.Value
            if study.PublicReleaseDate.IsSome then
                "PublicReleaseDate", Encode.string study.PublicReleaseDate.Value
            if study.Publications.Length <> 0 then
                "Publications", EncoderPublications study.Publications
            if study.Contacts.Length <> 0 then
                "Contacts", EncoderPersons study.Contacts
            if study.StudyDesignDescriptors.Length <> 0 then
                "StudyDesignDescriptors", EncoderOAs study.StudyDesignDescriptors
            if study.TableCount <> 0 then
                "Tables", EncoderTables study.Tables
            if study.RegisteredAssayIdentifiers.Count <> 0 then
                "RegisteredAssayIdentifiers", Encode.seq (Seq.map Encode.string study.RegisteredAssayIdentifiers)
            if study.Factors.Length <> 0 then
                "Factors", EncoderFactors study.Factors
            if study.Comments.Length <> 0 then
                "Comments", EncoderComments study.Comments
        ]
  
    let decoder : Decoder<ArcStudy> =
        Decode.object (fun get ->
            ArcStudy.make 
                (get.Required.Field("Identifier") Decode.string)
                (get.Optional.Field("Title") Decode.string)
                (get.Optional.Field("Description") Decode.string)
                (get.Optional.Field("SubmissionDate") Decode.string)
                (get.Optional.Field("PublicReleaseDate") Decode.string)
                (tryGetPublications get "Publications")
                (tryGetPersons get "Contacts")
                (tryGetOAs get "StudyDesignDescriptors")
                (tryGetTables get "Tables")
                (tryGetStringResizeArray get "RegisteredAssayIdentifiers")
                (tryGetFactors get "Factors")
                (tryGetComments get "Comments")
    )

    /// exports in json-ld format
    let toStringLD (a:ArcStudy) (assays: ResizeArray<ArcAssay>) = 
        Study.encoder (ConverterOptions(SetID=true,IncludeType=true)) (a.ToStudy(assays))
        |> Encode.toString 2

    let fromJsonString (s:string) = 
        GDecode.fromJsonString (Study.decoder (ConverterOptions())) s
        |> ArcStudy.fromStudy

    let toJsonString (a:ArcStudy) (assays: ResizeArray<ArcAssay>) = 
        Study.encoder (ConverterOptions()) (a.ToStudy(assays))
        |> Encode.toString 2

    let toArcJsonString (a:ArcStudy) : string =
        let spaces = 0
        Encode.toString spaces (encoder a)

    let fromArcJsonString (jsonString: string) =
        match Decode.fromString decoder jsonString with
        | Ok a -> a
        | Error e -> failwithf "Error. Unable to parse json string to ArcStudy: %s" e

[<AutoOpen>]
module ArcStudyExtensions =

    type ArcStudy with
        static member fromArcJsonString (jsonString: string) : ArcStudy = 
            match Decode.fromString ArcStudy.decoder jsonString with
            | Ok r -> r
            | Error e -> failwithf "Error. Unable to parse json string to ArcStudy: %s" e

        member this.ToArcJsonString(?spaces) : string =
            let spaces = defaultArg spaces 0
            Encode.toString spaces (ArcStudy.encoder this)

        static member toArcJsonString(a:ArcStudy) = a.ToArcJsonString()