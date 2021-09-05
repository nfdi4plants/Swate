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
    let DocsFeatureUrl = @"https://github.com/nfdi4plants/Swate/wiki"

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
        Name            : string
        CurrentVersion  : string
        Definition      : string
        DateCreated     : System.DateTime
        UserID          : string
    }

    let createOntology name currentVersion definition dateCreated userID = {     
        Name            = name          
        CurrentVersion  = currentVersion
        Definition      = definition    
        DateCreated     = dateCreated   
        UserID          = userID        
    }

    type Term = {
        OntologyName    : string
        Accession       : string
        Name            : string
        Definition      : string
        XRefValueType   : string option
        IsObsolete      : bool
    }

    let createTerm accession ontologyName name definition xrefvaluetype isObsolete = {          
        OntologyName  = ontologyName
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

type TermMinimal = {
    /// This is the Ontology Term Name
    Name            : string
    /// This is the Ontology Term Accession 'XX:aaaaaa'
    TermAccession   : string
} with
    static member create name termAccession = {
        Name            = name
        TermAccession   = termAccession
    }

    static member ofTerm (term:DbDomain.Term) = {
        Name            = term.Name
        TermAccession   = term.Accession
    }

    /// The numberFormat attribute in Excel allows to create automatic unit extensions.
    /// It uses a special input format which is created by this function and should be used for unit terms.
    member this.toNumberFormat = $"0.00 \"{this.Name}\""

    /// The numberFormat attribute in Excel allows to create automatic unit extensions.
    /// The format is created as $"0.00 \"{MinimalTerm.Name}\"", this function is meant to reverse this, altough term accession is lost.
    static member ofNumberFormat (formatStr:string) =
        let unitNameOpt = Regex.parseDoubleQuotes formatStr
        try
            TermMinimal.create unitNameOpt.Value ""
        with
            | :? NullReferenceException -> failwith $"Unable to parse given string {formatStr} to TermMinimal.Name."

    member this.accessionToTSR = this.TermAccession.Split(@":").[0] 
    member this.accessionToTAN = URLs.TermAccessionBaseUrl + this.TermAccession.Replace(@":",@"_")

type TermSearchable = {
    // Contains information about the term to search itself. If term accession is known, search result is 100% correct.
    Term                : TermMinimal
    // If ParentTerm isSome, then the term name is first searched in a is_a directed search
    ParentTerm          : TermMinimal option
    // Is term ist used as unit, unit ontology is searched first.
    IsUnit              : bool
    // ColIndex in table
    ColIndex            : int
    // RowIndex in table
    RowIndices          : int []
    // Search result
    SearchResultTerm    : DbDomain.Term option
} with
    static member create term parentTerm isUnit colInd rowIndices= {
        Term                = term
        ParentTerm          = parentTerm
        IsUnit              = isUnit
        ColIndex            = colInd
        RowIndices          = rowIndices
        SearchResultTerm    = None
    }

    member this.hasEmptyTerm =
        this.Term.Name = "" && this.Term.TermAccession = ""

type AnnotationTable = {
    Name            : string
    Worksheet       : string
} with
    static member create name worksheet = {
        Name        = name
        Worksheet   = worksheet
    }

[<Obsolete>]
/// Used in OfficeInterop to effectively find possible Term names and search for them in db
type SearchTermI = {
    // ColIndex in table
    ColIndices      : int []
    // RowIndex in table
    RowIndices      : int []
    // Query Term
    SearchQuery     : TermMinimal
    // Parent Term
    IsA             : TermMinimal option
    // This attribute displays a found ontology term, if any
    TermOpt         : DbDomain.Term option
} with
    static member create colIndices searchString termAccession ontologyInfoOpt rowIndices = {
        ColIndices      = colIndices
        RowIndices      = rowIndices
        SearchQuery     = TermMinimal.create searchString termAccession
        IsA             = ontologyInfoOpt
        TermOpt         = None
    }

type ProtocolTemplate = {
    Name            : string
    Version         : string
    Created         : DateTime
    Author          : string
    Description     : string
    DocsLink        : string
    CustomXml       : string
    TableXml        : string
    Tags            : string []
    Used            : int
    // WIP
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
        Used            = used
        // WIP          
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

//type IISADotNetAPIv1 = {
//    parseJsonToProcess      : string    -> Async<ISADotNet.Process> //Async<ISADotNet.Process>
//}

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
    getTermSuggestionsByParentTerm      : (int*string*TermMinimal)                                     -> Async<DbDomain.Term []>
    ///
    getAllTermsByParentTerm             : TermMinimal                                                  -> Async<DbDomain.Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByChildTerm       : (int*string*TermMinimal)                                     -> Async<DbDomain.Term []>
    ///
    getAllTermsByChildTerm              : TermMinimal                                                  -> Async<DbDomain.Term []>
    /// (ontOpt,searchName,mustContainName,searchDefinition,mustContainDefinition,keepObsolete)
    getTermsForAdvancedSearch           : (DbDomain.Ontology option*string*string*string*string*bool)   -> Async<DbDomain.Term []>

    getUnitTermSuggestions              : (int*string*UnitSearchRequest)                                -> Async<DbDomain.Term [] * UnitSearchRequest>

    getTermsByNames                     : TermSearchable []                                             -> Async<TermSearchable []>

    // Protocol apis
    getAllProtocolsWithoutXml       : unit                      -> Async<ProtocolTemplate []>
    getProtocolsByName              : string []                 -> Async<ProtocolTemplate []>
    getProtocolXmlForProtocol       : ProtocolTemplate          -> Async<ProtocolTemplate>
    increaseTimesUsed               : string                    -> Async<unit>
}

        