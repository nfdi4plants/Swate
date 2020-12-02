namespace Shared

module URLs =

    [<LiteralAttribute>]
    let TermAccessionBaseUrl = @"http://purl.obolibrary.org/obo/"

    [<LiteralAttribute>]
    let AnnotationPrinciplesUrl = @"https://nfdi4plants.github.io/AnnotationPrinciples/"

    [<LiteralAttribute>]
    let DocsFeatureUrl = @"https://github.com/nfdi4plants/Swate#swate"

    [<LiteralAttribute>]
    let DocsApiUrl = @"/api/IAnnotatorAPIv1/docs"

    /// This will only be needed as long there is no documentation on where to find all api docs.
    /// As soon as that link exists it will replace DocsApiUrl and DocsApiUrl2
    [<LiteralAttribute>]
    let DocsApiUrl2 = @"/api/IServiceAPIv1/docs"

    [<LiteralAttribute>]
    let CSBTwitterUrl = @"https://twitter.com/cs_biology"

    [<LiteralAttribute>]
    let CSBWebsiteUrl = @"https://csb.bio.uni-kl.de/"

type Counter = { Value : int }

module Route =
    /// Defines how routes are generated on server and mapped from client
    //let builder typeName methodName =
    //    sprintf "/api/%s/%s" typeName methodName

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

/// used in OfficeInterop to effectively find possible Term names and search for them in db
type InsertTerm = {
    ColIndices      : int []
    SearchString    : string
    RowIndices      : int []
    TermOpt         : DbDomain.Term option
} with
    static member create colIndices searchString rowIndices = {
        ColIndices      = colIndices
        SearchString    = searchString
        RowIndices      = rowIndices
        TermOpt         = None
    }

type ITestAPI = {
    // Development
    getTestNumber               : unit                                                  -> Async<int>
}

type IServiceAPIv1 = {
    getAppVersion : unit -> Async<string>
}

type IAnnotatorAPIv1 = {
    // Development
    getTestNumber               : unit                                                  -> Async<int>
    getTestString               : System.DateTime                                       -> Async<string>
    // Ontology related requests
    /// (name,version,definition,created,user)
    testOntologyInsert          : (string*string*string*System.DateTime*string)         -> Async<DbDomain.Ontology>
    getAllOntologies            : unit                                                  -> Async<DbDomain.Ontology []>

    // Term related requests
    getTermSuggestions                  : (int*string)                                                  -> Async<DbDomain.Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByParentTerm      : (int*string*string)                                           -> Async<DbDomain.Term []>
    /// (ontOpt,searchName,mustContainName,searchDefinition,mustContainDefinition,keepObsolete)
    getTermsForAdvancedSearch           : (DbDomain.Ontology option*string*string*string*string*bool)   -> Async<DbDomain.Term []>

    getUnitTermSuggestions              : (int*string)                                                  -> Async<DbDomain.Term []>

    getTermsByNames                     : InsertTerm []                                                 -> Async<InsertTerm []>
}

        