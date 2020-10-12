namespace Shared

type Counter = { Value : int }

module Route =
    /// Defines how routes are generated on server and mapped from client
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName


module Suggestion =
    
    let inline sorensenDice (x : Set<'T>) (y : Set<'T>) =
        match  (x.Count, y.Count) with
        | (0,0) -> 1.
        | (xCount,yCount) -> (2. * (Set.intersect x y |> Set.count |> float)) / ((xCount + yCount) |> float)
    
    let createBigrams (s:string) =
        s
            .ToUpperInvariant()
            .ToCharArray()
        |> Array.windowed 2
        |> Array.map (fun inner -> sprintf "%c%c" inner.[0] inner.[1])
        |> set

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

    // Development
    getTestNumber               : unit                                                  -> Async<int>

    // Ontology related requests
    testOntologyInsert          : (string*string*string*System.DateTime*string)         -> Async<DbDomain.Ontology>
    getAllOntologies            : unit                                                  -> Async<DbDomain.Ontology []>

    // Term related requests
    getTermSuggestions          : (int*string)                                          -> Async<DbDomain.Term []>
    getTermsForAdvancedSearch   : ((DbDomain.Ontology option)*string*string*string*bool*string)-> Async<DbDomain.Term []>

    getUnitTermSuggestions      : (int*string)                                          -> Async<DbDomain.Term []>
}