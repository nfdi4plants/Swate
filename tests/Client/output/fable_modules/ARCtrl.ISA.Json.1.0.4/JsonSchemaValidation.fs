namespace ARCtrl.ISA.Validation

module JSchema = 
   
#if FABLE_COMPILER
    let validate (schemaURL : string) (objectString : string) = 
        ValidationResult.Ok
#else

    let tryDownloadSchema (schemaURL : string) = 
            let rec download (tryNum) =
                try NJsonSchema.JsonSchema.FromUrlAsync(schemaURL) 
                with
                | err when tryNum <= 3 -> 
                    System.Threading.Thread.Sleep(30)
                    download (tryNum + 1)
                | err -> failwith $"Could not download schema from url {schemaURL}: \n{err.Message}"
            download 1

    let validate (schemaURL : string) (objectString : string) = 
        
        try 
            let settings = NJsonSchema.Validation.JsonSchemaValidatorSettings()
            let schema = tryDownloadSchema schemaURL
            let r = schema.Result.Validate(objectString,settings)

            ValidationResult.OfJSchemaOutput(r |> Seq.length |> (=) 0,r |> Seq.map (fun err -> err.ToString()) |> Seq.toArray)
        with
        | err -> Failed [|err.Message|]
#endif 
    let validateAssay (assayString : string) =
        let assayUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/assay_schema.json"
        validate assayUrl assayString

    let validateComment (commentString : string) =
        let commentUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/comment_schema.json"
        validate commentUrl commentString

    let validateData (dataString : string) =
        let dataUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/data_schema.json"
        validate dataUrl dataString
    
    let validateFactor (factorString : string) =
        let factorUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/factor_schema.json"
        validate factorUrl factorString

    let validateFactorValue (factorValueString : string) =
        let factorValueUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/factor_value_schema.json"
        validate factorValueUrl factorValueString

    let validateInvestigation (investigationString : string) =
        let investigationUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/investigation_schema.json"
        validate investigationUrl investigationString

    let validateMaterialAttribute (materialAttributeString : string) =
        let materialAttributeUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/material_attribute_schema.json"
        validate materialAttributeUrl materialAttributeString

    let validateMaterialAttributeValue (materialAttributeValueString : string) =
        let materialAttributeValueUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/material_attribute_value_schema.json"
        validate materialAttributeValueUrl materialAttributeValueString

    let validateMaterial (materialString : string) =
        let materialUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/material_schema.json"
        validate materialUrl materialString

    let validateOntologyAnnotation (ontologyAnnotationString : string) =
        let ontologyAnnotationUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/ontology_annotation_schema.json"
        validate ontologyAnnotationUrl ontologyAnnotationString

    let validateOntologySourceReference (ontologySourceReferenceString : string) =
        let ontologySourceReferenceUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/ontology_source_reference_schema.json"
        validate ontologySourceReferenceUrl ontologySourceReferenceString
    
    let validatePerson (personString : string) =
        let personUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/person_schema.json"
        validate personUrl personString

    let validateProcessParameterValue (processParameterValueString : string) =
        let processParameterValueUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/process_parameter_value_schema.json"
        validate processParameterValueUrl processParameterValueString

    let validateProcess (processString : string) =
        let processUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/process_schema.json"
        validate processUrl processString

    let validateProtocolParameter (protocolParameterString : string) =
        let protocolParameterUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/protocol_parameter_schema.json"
        validate protocolParameterUrl protocolParameterString

    let validateProtocol (protocolString : string) =
        let protocolUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/protocol_schema.json"
        validate protocolUrl protocolString

    let validatePublication (publicationString : string) =
        let publicationUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/publication_schema.json"
        validate publicationUrl publicationString

    let validateSample (sampleString : string) =
        let sampleUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/sample_schema.json"
        validate sampleUrl sampleString

    let validateSource (sourceString : string) =
        let sourceUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/source_schema.json"
        validate sourceUrl sourceString

    let validateStudy (studyString : string) =
        let studyUrl = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/study_schema.json"
        validate studyUrl studyString
