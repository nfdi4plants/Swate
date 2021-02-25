namespace Shared

open System
//open ISADotNet

module URLs =

    [<LiteralAttribute>]
    let TermAccessionBaseUrl = @"http://purl.obolibrary.org/obo/"

    /// accession string needs to have format: PO:0007131
    let termAccessionUrlOfAccessionStr (accessionStr:string) =
        let replaced = accessionStr.Replace(":","_")
        TermAccessionBaseUrl + replaced

    [<LiteralAttribute>]
    let Nfdi4psoOntologyUrl = @"https://github.com/nfdi4plants/nfdi4plants_ontology/issues/new/choose"

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
    let NFDITwitterUrl = @"https://twitter.com/nfdi4plants"

    [<LiteralAttribute>]
    let CSBWebsiteUrl = @"https://csb.bio.uni-kl.de/"

module HelperFunctions =

    open System.Text.RegularExpressions

    /// (|Regex|_|) pattern input
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(m.Value)
        else None

    let isAccessionPattern = "^[a-zA-Z]+:[0-9]+$"

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

type OntologyInfo = {
    Name            : string
    TermAccession   : string
} with
    static member create name termAccession = {
        Name            = name
        TermAccession   = termAccession
    }

/// Used in OfficeInterop to effectively find possible Term names and search for them in db
type SearchTermI = {
    ColIndices      : int []
    /// This is the Ontology Name
    SearchString    : string
    /// This is the Ontology Term Accession 'XX:aaaaaa'
    TermAccession   : string
    IsA             : OntologyInfo option
    RowIndices      : int []
    TermOpt         : DbDomain.Term option
} with
    static member create colIndices searchString termAccession ontologyInfoOpt rowIndices = {
        ColIndices      = colIndices
        SearchString    = searchString
        TermAccession   = termAccession
        IsA             = ontologyInfoOpt
        RowIndices      = rowIndices
        TermOpt         = None
    }

type Protocol = {
    Name            : string
    Version         : string
    Created         : DateTime
    Author          : string
    Description     : string
    DocsLink        : string
    CustomXml       : string
    TableXml        : string
    Tags            : string []
    // WIP
    Used            : int
    Rating          : int  
} with
    static member create name version created author desc docs tags customXml tableXml used rating = {
        Name            = name
        Version         = version
        Created         = created 
        Author          = author
        Description     = desc
        DocsLink        = docs
        Tags            = tags
        CustomXml       = customXml
        TableXml        = tableXml
        // WIP          
        Used            = used
        Rating          = rating
    }

/// This type is used to define target for unit term search.
type UnitSearchRequest =
| Unit1
| Unit2

type ITestAPI = {
    // Development
    getTestNumber           : unit      -> Async<int>
}

type IISADotNetAPIv1 = {
    parseJsonToProcess      : string    -> Async<ISADotNet.Process> //Async<ISADotNet.Process>
}

type IServiceAPIv1 = {
    getAppVersion           : unit      -> Async<string>
}


type IAnnotatorAPIv1 = {
    // Development
    getTestNumber               : unit                                                  -> Async<int>
    getTestString               : string                                                -> Async<string option>
    // Ontology related requests
    /// (name,version,definition,created,user)
    testOntologyInsert          : (string*string*string*System.DateTime*string)         -> Async<DbDomain.Ontology>
    getAllOntologies            : unit                                                  -> Async<DbDomain.Ontology []>

    // Term related requests
    getTermSuggestions                  : (int*string)                                                  -> Async<DbDomain.Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByParentTerm      : (int*string*OntologyInfo)                                     -> Async<DbDomain.Term []>
    ///
    getAllTermsByParentTerm             : OntologyInfo                                                  -> Async<DbDomain.Term []>
    /// (ontOpt,searchName,mustContainName,searchDefinition,mustContainDefinition,keepObsolete)
    getTermsForAdvancedSearch           : (DbDomain.Ontology option*string*string*string*string*bool)   -> Async<DbDomain.Term []>

    getUnitTermSuggestions              : (int*string*UnitSearchRequest)                                -> Async<DbDomain.Term [] * UnitSearchRequest>

    getTermsByNames                     : SearchTermI []                                                -> Async<SearchTermI []>

    // Protocol apis
    getAllProtocols                 : unit                                                  -> Async<Protocol []>
    getProtocolBlocksForProtocol    : Protocol                                              -> Async<Protocol>
}

        