namespace Shared

open System
open Shared
open TermTypes

module Route =

    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

module SorensenDice =
    
    let inline calculateDistance (x : Set<'T>) (y : Set<'T>) =
        match  (x.Count, y.Count) with
        | (0, 0) -> 1.
        | (xCount,yCount) -> (2. * (Set.intersect x y |> Set.count |> float)) / ((xCount + yCount) |> float)
    
    let createBigrams (s:string) =
        s
            .ToUpperInvariant()
            .ToCharArray()
        |> Array.windowed 2
        |> Array.map (fun inner -> sprintf "%c%c" inner.[0] inner.[1])
        |> set

    let sortBySimilarity (searchStr:string) (f: 'a -> string) (arrayToSort:'a []) =
        let searchSet = searchStr |> createBigrams
        arrayToSort
        |> Array.sortByDescending (fun result ->
            let resultSet = f result |> createBigrams
            calculateDistance resultSet searchSet
        )

/// Development api
type ITestAPI = {
    test    : unit      -> Async<string*string>
    postTest: string    -> Async<string*string>
}

type IServiceAPIv1 = {
    getAppVersion           : unit      -> Async<string>
}

type IDagAPIv1 = {
    parseAnnotationTablesToDagHtml          : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
}

type IISADotNetCommonAPIv1 = {
    toAssayJson                 : byte []   -> Async<obj>
    toSwateTemplateJson         : byte []   -> Async<obj>
    toInvestigationJson         : byte []   -> Async<obj>
    toProcessSeqJson            : byte []   -> Async<obj>
    //toTableJson                 : byte [] -> Async<obj>
    toAssayJsonStr              : byte []   -> Async<string>
    toSwateTemplateJsonStr      : byte []   -> Async<string>
    toInvestigationJsonStr      : byte []   -> Async<string>
    toProcessSeqJsonStr         : byte []   -> Async<string>
    //toTableJsonStr              : byte [] -> Async<string>
    testPostNumber              : int       -> Async<string>
    getTestNumber               : unit      -> Async<string>
}

type ISwateJsonAPIv1 = {
    parseAnnotationTableToAssayJson         : string * OfficeInteropTypes.BuildingBlock []      -> Async<string>
    parseAnnotationTableToProcessSeqJson    : string * OfficeInteropTypes.BuildingBlock []      -> Async<string>
    //parseAnnotationTableToTableJson         : string * OfficeInteropTypes.BuildingBlock []      -> Async<string>
    parseAnnotationTablesToAssayJson        : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
    parseAnnotationTablesToProcessSeqJson   : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
    //parseAnnotationTablesToTableJson        : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
    parseAssayJsonToBuildingBlocks          : string -> Async<(string * OfficeInteropTypes.InsertBuildingBlock []) []>
    //parseTableJsonToBuildingBlocks          : string -> Async<(string * OfficeInteropTypes.InsertBuildingBlock []) []>
    parseProcessSeqToBuildingBlocks         : string -> Async<(string * OfficeInteropTypes.InsertBuildingBlock []) []>
}

type IExportAPIv1 = {
    toAssayXlsx                             : (string * OfficeInteropTypes.BuildingBlock []) []         -> Async<byte []>
}

/// <summary>Deprecated</summary>
type IOntologyAPIv1 = {
    // Development
    getTestNumber               : unit                                                                  -> Async<int>

    // Ontology related requests
    getAllOntologies            : unit                                                                  -> Async<Ontology []>

    // Term related requests
    ///
    getTermSuggestions                  : (int*string)                                                  -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByParentTerm      : (int*string*TermMinimal)                                      -> Async<Term []>
    getAllTermsByParentTerm             : TermMinimal                                                   -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByChildTerm       : (int*string*TermMinimal)                                      -> Async<Term []>
    getAllTermsByChildTerm              : TermMinimal                                                   -> Async<Term []>
    getTermsForAdvancedSearch           : (AdvancedSearchTypes.AdvancedSearchOptions)                   -> Async<Term []>
    getUnitTermSuggestions              : (int*string)                                                  -> Async<Term []>
    getTermsByNames                     : TermSearchable []                                             -> Async<TermSearchable []>

    // Tree related requests
    getTreeByAccession                  : string                                                        -> Async<TreeTypes.Tree>
}

type IOntologyAPIv2 = {
    // Development
    getTestNumber                       : unit                                                          -> Async<int>

    // Ontology related requests
    getAllOntologies                    : unit                                                          -> Async<Ontology []>

    // Term related requests
    ///
    getTermSuggestions                  : {| n: int; query: string; ontology: string option|}           -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByParentTerm      : {| n: int; query: string; parent_term: TermMinimal |}         -> Async<Term []>
    getAllTermsByParentTerm             : TermMinimal                                                   -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByChildTerm       : {| n: int; query: string; child_term: TermMinimal |}          -> Async<Term []>
    getAllTermsByChildTerm              : TermMinimal                                                   -> Async<Term []>
    getTermsForAdvancedSearch           : (AdvancedSearchTypes.AdvancedSearchOptions)                   -> Async<Term []>
    getUnitTermSuggestions              : {| n: int; query: string|} -> Async<Term []>
    getTermsByNames                     : TermSearchable []                                             -> Async<TermSearchable []>

    // Tree related requests
    getTreeByAccession                  : string                                                        -> Async<TreeTypes.Tree>
}

type IOntologyAPIv3 = {
    // Development
    getTestNumber : unit -> Async<int>
    searchTerm: TermQuery -> Async<Term []>
    searchTerms: TermQuery[] -> Async<TermQueryResults[]>
    getTermById: string -> Async<Term option>
}

type ITemplateAPIv1 = {
    // must return template as string, fable remoting cannot do conversion automatically
    getTemplates                    : unit      -> Async<string>
    getTemplateById                 : string    -> Async<string>
}
