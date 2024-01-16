namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update


type Study = 
    {
        ID : URI option
        FileName : string option
        Identifier : string option
        Title : string option
        Description : string option
        SubmissionDate : string option
        PublicReleaseDate : string option
        Publications : Publication list option
        Contacts : Person list option
        StudyDesignDescriptors : OntologyAnnotation list option
        Protocols : Protocol list option
        Materials : StudyMaterials option
        ProcessSequence : Process list option
        Assays : Assay list option
        Factors : Factor list option
        /// List of all the characteristics categories (or material attributes) defined in the study, used to avoid duplication of their declaration when each material_attribute_value is created. 
        CharacteristicCategories : MaterialAttribute list option
        /// List of all the unitsdefined in the study, used to avoid duplication of their declaration when each value is created.
        UnitCategories : OntologyAnnotation list option
        Comments : Comment list option
    }

    static member make id filename identifier title description submissionDate publicReleaseDate publications contacts studyDesignDescriptors protocols materials processSequence assays factors characteristicCategories unitCategories comments : Study=
        {
            ID                          = id
            FileName                    = filename
            Identifier                  = identifier
            Title                       = title
            Description                 = description
            SubmissionDate              = submissionDate
            PublicReleaseDate           = publicReleaseDate
            Publications                = publications
            Contacts                    = contacts
            StudyDesignDescriptors      = studyDesignDescriptors
            Protocols                   = protocols
            Materials                   = materials
            ProcessSequence             = processSequence
            Assays                      = assays
            Factors                     = factors
            CharacteristicCategories    = characteristicCategories
            UnitCategories              = unitCategories
            Comments                    = comments
        }

    static member create(?Id,?FileName,?Identifier,?Title,?Description,?SubmissionDate,?PublicReleaseDate,?Publications,?Contacts,?StudyDesignDescriptors,?Protocols,?Materials,?ProcessSequence,?Assays,?Factors,?CharacteristicCategories,?UnitCategories,?Comments) : Study=
        Study.make Id FileName Identifier Title Description SubmissionDate PublicReleaseDate Publications Contacts StudyDesignDescriptors Protocols Materials ProcessSequence Assays Factors CharacteristicCategories UnitCategories Comments

    static member empty =
        Study.create ()

    
    /// If an study with the given identfier exists in the list, returns true
    static member existsByIdentifier (identifier : string) (studies : Study list) =
        List.exists (fun (s:Study) -> s.Identifier = Some identifier) studies

    /// Adds the given study to the studies  
    static member add (studies : Study list) (study : Study) =
        List.append studies [study]

    /// Updates all studies for which the predicate returns true with the given study values
    static member updateBy (predicate : Study -> bool) (updateOption:UpdateOptions) (study : Study) (studies : Study list) =
        if List.exists predicate studies then
            List.map (fun a -> if predicate a then updateOption.updateRecordType a study else a) studies
        else 
            studies

    /// Updates all studies with the same identifier as the given study with its values
    static member updateByIdentifier (updateOption:UpdateOptions) (study : Study) (studies : Study list) =
        Study.updateBy (fun (s:Study) -> s.Identifier = study.Identifier) updateOption study studies

    /// If a study with the given identifier exists in the list, removes it
    static member removeByIdentifier (identifier : string) (studies : Study list) = 
        List.filter (fun (s:Study) -> s.Identifier = Some identifier |> not) studies
    

    /// Returns assays of a study
    static member getAssays (study : Study) =
        study.Assays |> Option.defaultValue []

    /// Applies function f to the assays of a study
    static member mapAssays (f : Assay list -> Assay list) (study : Study) =
        { study with 
            Assays = Option.mapDefault [] f study.Assays }

    /// Replaces study assays with the given assay list
    static member setAssays (study : Study) (assays : Assay list) =
        { study with
            Assays = Some assays }
   

    /// Applies function f to the factors of a study
    static member mapFactors (f : Factor list -> Factor list) (study : Study) =
        { study with 
            Factors = Option.mapDefault [] f study.Factors }

    /// Replaces study factors with the given assay list
    static member setFactors (study : Study) (factors : Factor list) =
        { study with
            Factors = Some factors }

    /// Applies function f to the protocols of a study
    static member mapProtocols (f : Protocol list -> Protocol list) (study : Study) =
        { study with 
            Protocols = Option.mapDefault [] f study.Protocols }

    /// Replaces study protocols with the given assay list
    static member setProtocols (study : Study) (protocols : Protocol list) =
        { study with
            Protocols = Some protocols }

    /// Returns all contacts of a study
    static member getContacts (study : Study) =
        study.Contacts |> Option.defaultValue []

    /// Applies function f to contacts of a study
    static member mapContacts (f : Person list -> Person list) (study : Study) =
        { study with 
            Contacts = Option.mapDefault [] f study.Contacts }

    /// Replaces contacts of a study with the given person list
    static member setContacts (study : Study) (persons : Person list) =
        { study with
            Contacts = Some persons }

    /// Returns publications of a study
    static member getPublications (study : Study) =
        study.Publications |> Option.defaultValue []

    /// Applies function f to publications of the study
    static member mapPublications (f : Publication list -> Publication list) (study : Study) =
        { study with 
            Publications = Option.mapDefault [] f study.Publications }

    /// Replaces publications of a study with the given publication list
    static member setPublications (study : Study) (publications : Publication list) =
        { study with
            Publications = Some publications }

    /// Returns study design descriptors of a study
    static member getDescriptors (study : Study) =
        study.StudyDesignDescriptors |> Option.defaultValue []

    /// Applies function f to to study design descriptors of a study
    static member mapDescriptors (f : OntologyAnnotation list -> OntologyAnnotation list) (study : Study) =
        { study with 
            StudyDesignDescriptors = Option.mapDefault [] f study.StudyDesignDescriptors }

    /// Replaces study design descriptors with the given ontology annotation list
    static member setDescriptors (study : Study) (descriptors : OntologyAnnotation list) =
        { study with
            StudyDesignDescriptors = Some descriptors }

    /// Returns processSequence of study
    static member getProcesses  (study : Study) =
        study.ProcessSequence |> Option.defaultValue []

    /// Returns protocols of a study
    static member getProtocols (study : Study) =
        let processSequenceProtocols = 
            Study.getProcesses study
            |> ProcessSequence.getProtocols
        let assaysProtocols = 
            Study.getAssays study
            |> List.collect Assay.getProtocols            
        let studyProtocols = 
            study.Protocols
            |> Option.defaultValue []
        Update.mergeUpdateLists UpdateByExistingAppendLists (fun (p : Protocol) -> p.Name) assaysProtocols processSequenceProtocols
        |> Update.mergeUpdateLists UpdateByExistingAppendLists (fun (p : Protocol) -> p.Name) studyProtocols
    
    /// Returns Characteristics of the study
    static member getCharacteristics (study : Study) =
        let processSequenceCharacteristics = 
            Study.getProcesses study
            |> ProcessSequence.getCharacteristics
        let assaysCharacteristics = 
            Study.getAssays study
            |> List.collect Assay.getCharacteristics            
        let studyCharacteristics = 
            study.CharacteristicCategories
            |> Option.defaultValue []
        processSequenceCharacteristics @ assaysCharacteristics @ studyCharacteristics 
        |> List.distinct
        //Update.mergeUpdateLists UpdateByExistingAppendLists (fun (f : MaterialAttribute) -> f.CharacteristicType |> Option.defaultValue OntologyAnnotation.empty) assaysCharacteristics processSequenceCharacteristics
        //|> Update.mergeUpdateLists UpdateByExistingAppendLists (fun (f : MaterialAttribute) -> f.CharacteristicType |> Option.defaultValue OntologyAnnotation.empty) studyCharacteristics

    /// Returns factors of the study
    static member getFactors (study : Study) =
        let processSequenceFactors = 
            Study.getProcesses study
            |> ProcessSequence.getFactors
        let assaysFactors = 
            Study.getAssays study
            |> List.collect Assay.getFactors            
        let studyFactors = 
            study.Factors
            |> Option.defaultValue []
        processSequenceFactors @ assaysFactors @ studyFactors 
        |> List.distinct
        //Update.mergeUpdateLists UpdateByExistingAppendLists (fun (f : Factor) -> f.FactorType |> Option.defaultValue OntologyAnnotation.empty) assaysFactors processSequenceFactors
        //|> Update.mergeUpdateLists UpdateByExistingAppendLists (fun (f : Factor) -> f.FactorType |> Option.defaultValue OntologyAnnotation.empty) studyFactors

    /// Returns unit categories of the study
    static member getUnitCategories (study : Study) =
        let processSequenceUnits = 
            Study.getProcesses study
            |> ProcessSequence.getUnits
        let assaysUnits = 
            Study.getAssays study
            |> List.collect Assay.getUnitCategories            
        let studyUnits = 
            study.UnitCategories
            |> Option.defaultValue []
        processSequenceUnits @ assaysUnits @ studyUnits 
        |> List.distinct
        //Update.mergeUpdateLists UpdateByExistingAppendLists (fun (d : OntologyAnnotation) -> d.Name) assaysUnits processSequenceUnits
        //|> Update.mergeUpdateLists UpdateByExistingAppendLists (fun (d : OntologyAnnotation) -> d.Name) studyUnits

    /// Returns sources of the study
    static member getSources (study : Study) =
        let processSequenceSources = 
            Study.getProcesses study
            |> ProcessSequence.getSources
        let assaysSources = 
            Study.getAssays study
            |> List.collect Assay.getSources   
        let studySources = 
            match study.Materials with
            | Some mat -> mat.Sources |> Option.defaultValue []
            | None -> []
        Update.mergeUpdateLists UpdateByExistingAppendLists (fun (s : Source) -> s.Name |> Option.defaultValue "") assaysSources processSequenceSources
        |> Update.mergeUpdateLists UpdateByExistingAppendLists (fun (s : Source) -> s.Name |> Option.defaultValue "") studySources

    /// Returns sources of the study
    static member getSamples (study : Study) =
        let processSequenceSamples = 
            Study.getProcesses study
            |> ProcessSequence.getSamples
        let assaysSamples = 
            Study.getAssays study
            |> List.collect Assay.getSamples   
        let studySamples = 
            match study.Materials with
            | Some mat -> mat.Samples |> Option.defaultValue []
            | None -> []
        Update.mergeUpdateLists UpdateByExistingAppendLists (fun (s : Sample) -> s.Name |> Option.defaultValue "") assaysSamples processSequenceSamples
        |> Update.mergeUpdateLists UpdateByExistingAppendLists (fun (s : Sample) -> s.Name |> Option.defaultValue "") studySamples

    /// Returns materials of the study
    static member getMaterials (study : Study) =
        let processSequenceMaterials = 
            Study.getProcesses study
            |> ProcessSequence.getMaterials
        let assaysMaterials = 
            Study.getAssays study
            |> List.collect (Assay.getMaterials >> AssayMaterials.getMaterials)           
        let studyMaterials = 
            match study.Materials with
            | Some mat -> mat.OtherMaterials |> Option.defaultValue []
            | None -> []
        let materials = 
            Update.mergeUpdateLists UpdateByExistingAppendLists (fun (s : Material) -> s.Name) assaysMaterials processSequenceMaterials
            |> Update.mergeUpdateLists UpdateByExistingAppendLists (fun (s : Material) -> s.Name) studyMaterials
        let sources = Study.getSources study
        let samples = Study.getSamples study
        StudyMaterials.make (Option.fromValueWithDefault [] sources)
                            (Option.fromValueWithDefault [] samples)
                            (Option.fromValueWithDefault [] materials)

    static member update (study : Study) =
        try
            let protocols = Study.getProtocols study 
            {study with 
                        Materials  = Study.getMaterials study |> Option.fromValueWithDefault StudyMaterials.empty
                        Assays = study.Assays |> Option.map (List.map (Assay.update >> Assay.updateProtocols protocols))
                        Protocols = protocols |> Option.fromValueWithDefault []
                        Factors = Study.getFactors study |> Option.fromValueWithDefault []
                        CharacteristicCategories = Study.getCharacteristics study |> Option.fromValueWithDefault []
                        UnitCategories = Study.getUnitCategories study |> Option.fromValueWithDefault []
                        ProcessSequence = study.ProcessSequence |> Option.map (ProcessSequence.updateProtocols protocols)
            }
        with
        | err -> failwithf $"Could not update study {study.Identifier}: \n{err.Message}"