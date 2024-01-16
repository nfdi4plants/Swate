namespace ARCtrl.ISA

open ARCtrl.ISA.Aux

type Investigation = 
    {
        ID : URI option
        FileName : string option
        Identifier : string option
        Title : string option
        Description : string option
        SubmissionDate : string option
        PublicReleaseDate : string option
        OntologySourceReferences : OntologySourceReference list option
        Publications : Publication list option
        Contacts : Person list option
        Studies : Study list option
        Comments : Comment list option
        Remarks     : Remark list
    }
    
    static member make id filename identifier title description submissionDate publicReleaseDate ontologySourceReference publications contacts studies comments remarks : Investigation=
        {
            ID                          = id
            FileName                    = filename
            Identifier                  = identifier
            Title                       = title
            Description                 = description
            SubmissionDate              = submissionDate
            PublicReleaseDate           = publicReleaseDate
            OntologySourceReferences    = ontologySourceReference
            Publications                = publications
            Contacts                    = contacts
            Studies                     = studies
            Comments                    = comments
            Remarks                     = remarks
        }

    static member create(?Id,?FileName,?Identifier,?Title,?Description,?SubmissionDate,?PublicReleaseDate,?OntologySourceReferences,?Publications,?Contacts,?Studies,?Comments,?Remarks) : Investigation=
        Investigation.make Id FileName Identifier Title Description SubmissionDate PublicReleaseDate OntologySourceReferences Publications Contacts Studies Comments (Remarks |> Option.defaultValue [])

    static member empty =
        Investigation.create ()

    /// Returns contacts of an investigation
    static member getContacts (investigation : Investigation) =
        investigation.Contacts

    /// Applies function f on person of an investigation
    static member mapContacts (f:Person list -> Person list) (investigation: Investigation) =
        { investigation with 
            Contacts = Option.mapDefault [] f investigation.Contacts }

    /// Replaces persons of an investigation with the given person list
    static member setContacts (investigation:Investigation) (persons:Person list) =
        { investigation with
            Contacts = Some persons }

    /// Returns publications of an investigation
    static member getPublications (investigation : Investigation) =
        investigation.Publications

    /// Applies function f on publications of an investigation
    static member mapPublications (f:Publication list -> Publication list) (investigation: Investigation) =
        { investigation with 
            Publications = Option.mapDefault [] f investigation.Publications }

    /// Replaces publications of an investigation with the given publication list
    static member setPublications (investigation:Investigation) (publications:Publication list) =
        { investigation with
            Publications = Some publications }

    /// Returns ontology source ref of an investigation
    static member getOntologies (investigation : Investigation) =
        investigation.OntologySourceReferences

    /// Applies function f on ontology source ref of an investigation
    static member mapOntologies (f:OntologySourceReference list -> OntologySourceReference list) (investigation: Investigation) =
        { investigation with 
            OntologySourceReferences = Option.mapDefault []  f investigation.OntologySourceReferences }

    /// Replaces ontology source ref of an investigation with the given ontology source ref list
    static member setOntologies (investigation:Investigation) (ontologies:OntologySourceReference list) =
        { investigation with
            OntologySourceReferences = Some ontologies }

    /// Returns studies of an investigation
    static member getStudies (investigation : Investigation) =
        investigation.Studies |> Option.defaultValue []

    /// Applies function f on studies of an investigation
    static member mapStudies (f:Study list -> Study list) (investigation: Investigation) =
        { investigation with 
            Studies = Option.mapDefault []  f investigation.Studies }

    /// Replaces studies of an investigation with the given study list
    static member setStudies (investigation:Investigation) (studies:Study list) =
        { investigation with
            Studies = Some studies }

    /// Returns comments of an investigation
    static member getComments (investigation : Investigation) =
        investigation.Comments

    /// Applies function f on comments of an investigation
    static member mapComments (f:Comment list -> Comment list) (investigation: Investigation) =
        { investigation with 
            Comments = Option.mapDefault [] f investigation.Comments }

    /// Replaces comments of an investigation with the given comment list
    static member setComments (investigation:Investigation) (comments:Comment list) =
        { investigation with
            Comments = Some comments }

    /// Returns remarks of an investigation
    static member getRemarks (investigation : Investigation) =
        investigation.Remarks

    /// Applies function f on remarks of an investigation
    static member mapRemarks (f:Remark list -> Remark list) (investigation: Investigation) =
        { investigation with 
            Remarks = f investigation.Remarks }

    /// Replaces remarks of an investigation with the given remark list
    static member setRemarks (investigation:Investigation) (remarks:Remark list) =
        { investigation with
            Remarks = remarks }

    /// Update the investigation with the values of the given newInvestigation
    static member updateBy (updateOption: Update.UpdateOptions) (investigation : Investigation) newInvestigation =
        updateOption.updateRecordType investigation newInvestigation

    static member update (investigation : Investigation) =
        try 
        {investigation with 
            Studies = investigation.Studies |> Option.map (List.map Study.update)
        }
        with
        | err -> failwithf $"Could not update investigation {investigation.Identifier}: \n{err.Message}"