namespace Shared

type Counter = { Value : int }

module Route =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

module DbDomain =
    
    type Ontology = {
        ID              : int64
        Name            : string
        CurrentVersion  : string
        Definition      : string
        DateCreated     : System.DateTime
        UserID          : string
    }

    let createOntology id name currentVersion definition dateCreated userID = {
        ID              = id            
        Name            = name          
        CurrentVersion  = currentVersion
        Definition      = definition    
        DateCreated     = dateCreated   
        UserID          = userID        
    }

    type Term = {
        ID              : int64
        OntologyId      : int64
        Accession       : string
        Name            : string
        Definition      : string
        XRefValueType   : string option
        IsObsolete      : bool
    }

    let createTerm id accession ontologyID name definition xrefvaluetype isObsolete = {
        ID            = id           
        OntologyId    = ontologyID   
        Accession     = accession    
        Name          = name         
        Definition    = definition   
        XRefValueType = xrefvaluetype
        IsObsolete    = isObsolete   
    }

    type TermRelationship = {
        TermID              : int64
        RelationshipType    : string
        RelatedTermID       : int64
    }

type IAnnotatorAPI = {
    testOntologyInsert: (string*string*string*System.DateTime*string) -> Async<DbDomain.Ontology>
    getTermSuggestions: (int*string) -> Async<DbDomain.Term []>
}