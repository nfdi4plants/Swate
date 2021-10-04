namespace Shared

open System
open TermTypes
open ProtocolTemplateTypes

module Route =

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

type IISADotNetCommonAPIv1 = {
    toAssayJSON                 : byte [] -> Async<string>
    toAssayJSONWithCustomXml    : byte [] -> Async<string>
    toInvestigationJSON         : byte [] -> Async<string>
    toProcessSeqJSON            : byte [] -> Async<string>
    toSimplifiedRowMajorJSON    : byte [] -> Async<string>
    testPostNumber              : int -> Async<string>
    getTestNumber               : unit -> Async<string>
}

type IAnnotatorAPIv1 = {
    // Development
    getTestNumber               : unit                                                  -> Async<int>

    // Ontology related requests
    /// (name,version,definition,created,user)
    testOntologyInsert          : (string*string*string*System.DateTime*string)         -> Async<DbDomain.Ontology>
    getAllOntologies            : unit                                                  -> Async<DbDomain.Ontology []>

    // Term related requests
    getTermSuggestions                  : (int*string)                                                  -> Async<DbDomain.Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByParentTerm      : (int*string*TermMinimal)                                      -> Async<DbDomain.Term []>
    ///
    getAllTermsByParentTerm             : TermMinimal                                                   -> Async<DbDomain.Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByChildTerm       : (int*string*TermMinimal)                                      -> Async<DbDomain.Term []>
    ///
    getAllTermsByChildTerm              : TermMinimal                                                   -> Async<DbDomain.Term []>
    /// (ontOpt,searchName,mustContainName,searchDefinition,mustContainDefinition,keepObsolete)
    getTermsForAdvancedSearch           : (DbDomain.Ontology option*string*string*string*string*bool)   -> Async<DbDomain.Term []>

    getUnitTermSuggestions              : (int*string*UnitSearchRequest)                                -> Async<DbDomain.Term [] * UnitSearchRequest>

    getTermsByNames                     : TermSearchable []                                             -> Async<TermSearchable []>

    // Protocol apis
    getAllProtocolsWithoutXml       : unit                      -> Async<ProtocolTemplate []>
    getProtocolByName               : string                    -> Async<ProtocolTemplate>
    getProtocolsByName              : string []                 -> Async<ProtocolTemplate []>
    increaseTimesUsed               : string                    -> Async<unit>
}

        