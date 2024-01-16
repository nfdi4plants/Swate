namespace ARCtrl.ISA.Json

open ValidationTypes

module JsonSchemaUrls =
    
    [<LiteralAttribute>]
    let Assay = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/assay_schema.json"

    [<LiteralAttribute>]
    let Comment = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/comment_schema.json"

    [<LiteralAttribute>]
    let Data = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/data_schema.json"

    [<LiteralAttribute>]
    let Factor = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/factor_schema.json"

    [<LiteralAttribute>]
    let FactorValue = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/factor_value_schema.json"

    [<LiteralAttribute>]
    let Investigation = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/investigation_schema.json"

    [<LiteralAttribute>]
    let MaterialAttribute = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/material_attribute_schema.json"

    [<LiteralAttribute>]
    let MaterialAttributeValue = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/material_attribute_value_schema.json"

    [<LiteralAttribute>]
    let Material = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/material_schema.json"

    [<LiteralAttribute>]
    let OntologyAnnotation = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/ontology_annotation_schema.json"

    [<LiteralAttribute>]
    let OntologySourceReference = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/ontology_source_reference_schema.json"

    [<LiteralAttribute>]
    let Person = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/person_schema.json"

    [<LiteralAttribute>]
    let ProcessParameterValue = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/process_parameter_value_schema.json"

    [<LiteralAttribute>]
    let Process = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/process_schema.json"

    [<LiteralAttribute>]
    let ProtocolParameter = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/protocol_parameter_schema.json"

    [<LiteralAttribute>]
    let Protocol = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/protocol_schema.json"

    [<LiteralAttribute>]
    let Publication = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/publication_schema.json"

    [<LiteralAttribute>]
    let Sample = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/sample_schema.json"

    [<LiteralAttribute>]
    let Source = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/source_schema.json"

    [<LiteralAttribute>]
    let Study = "https://raw.githubusercontent.com/HLWeil/isa-specs/anyof/source/_static/isajson/study_schema.json"

#if !FABLE_COMPILER
module JSchema = 
   
    let tryDownloadSchema (schemaURL : string) = 
        let rec download (tryNum) =
            try NJsonSchema.JsonSchema.FromUrlAsync(schemaURL) 
            with
            | err when tryNum <= 3 -> 
                System.Threading.Thread.Sleep(30)
                download (tryNum + 1)
            | err -> failwith $"Could not download schema from url {schemaURL}: \n{err.Message}"
        download 1
#endif

module Validation =

    let validate (schemaURL : string) (objectString : string) = 
        async {
            try 
                #if FABLE_COMPILER
                let! isValid, errorList = Fable.validate (schemaURL) (objectString)
                #else
                let settings = NJsonSchema.Validation.JsonSchemaValidatorSettings()
                let schema = JSchema.tryDownloadSchema schemaURL
                let r = schema.Result.Validate(objectString,settings)
                let isValid = (Seq.length r) = 0
                let errorList = 
                    r  
                    |> Seq.map (fun err -> err.ToString()) 
                    |> Seq.toArray
                #endif 
                // if you change isValid and errorList remember to check for fable compatibility.
                // for exmaple must use same name as in `let! isValid, errorList =...`
                return ValidationResult.OfJSchemaOutput(isValid, errorList)
            with
            | err -> 
                return Failed [|err.Message|]
        }

    let validateAssay (assayString : string) = validate JsonSchemaUrls.Assay assayString

    let validateComment (commentString : string) = validate JsonSchemaUrls.Comment commentString

    let validateData (dataString : string) =
        
        validate JsonSchemaUrls.Data dataString
    
    let validateFactor (factorString : string) =
        
        validate JsonSchemaUrls.Factor factorString

    let validateFactorValue (factorValueString : string) =
        
        validate JsonSchemaUrls.FactorValue factorValueString

    let validateInvestigation (investigationString : string) =
        validate JsonSchemaUrls.Investigation investigationString

    let validateMaterialAttribute (materialAttributeString : string) =
        validate JsonSchemaUrls.MaterialAttribute materialAttributeString

    let validateMaterialAttributeValue (materialAttributeValueString : string) =
        validate JsonSchemaUrls.MaterialAttributeValue materialAttributeValueString

    let validateMaterial (materialString : string) =
        validate JsonSchemaUrls.Material materialString

    let validateOntologyAnnotation (ontologyAnnotationString : string) =
        validate JsonSchemaUrls.OntologyAnnotation ontologyAnnotationString

    let validateOntologySourceReference (ontologySourceReferenceString : string) =
        validate JsonSchemaUrls.OntologySourceReference ontologySourceReferenceString
    
    let validatePerson (personString : string) =
        validate JsonSchemaUrls.Person personString

    let validateProcessParameterValue (processParameterValueString : string) =
        validate JsonSchemaUrls.ProcessParameterValue processParameterValueString

    let validateProcess (processString : string) =
        validate JsonSchemaUrls.Process processString

    let validateProtocolParameter (protocolParameterString : string) =
        validate JsonSchemaUrls.ProtocolParameter protocolParameterString

    let validateProtocol (protocolString : string) =
        validate JsonSchemaUrls.Protocol protocolString

    let validatePublication (publicationString : string) =
        validate JsonSchemaUrls.Publication publicationString

    let validateSample (sampleString : string) =
        validate JsonSchemaUrls.Sample sampleString

    let validateSource (sourceString : string) =
        validate JsonSchemaUrls.Source sourceString

    let validateStudy (studyString : string) =
        validate JsonSchemaUrls.Study studyString
