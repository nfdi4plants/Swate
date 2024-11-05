namespace Shared

open System
open Shared
open Database
open DTO

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

type IOntologyAPIv3 = {
    // Development
    getTestNumber : unit -> Async<int>
    searchTerm: TermQuery -> Async<Term []>
    searchTerms: TermQuery[] -> Async<TermQueryResults[]>
    getTermById: string -> Async<Term option>
}

/// Development api
type ITestAPI = {
    test    : unit      -> Async<string*string>
    postTest: string    -> Async<string*string>
}

type IServiceAPIv1 = {
    getAppVersion           : unit      -> Async<string>
}

type IDagAPIv1 = {
    parseAnnotationTablesToDagHtml          : (string * obj []) [] -> Async<string>
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
    parseAnnotationTableToAssayJson         : string * obj []      -> Async<string>
    parseAnnotationTableToProcessSeqJson    : string * obj []      -> Async<string>
    //parseAnnotationTableToTableJson         : string * OfficeInteropTypes.BuildingBlock []      -> Async<string>
    parseAnnotationTablesToAssayJson        : (string * obj []) [] -> Async<string>
    parseAnnotationTablesToProcessSeqJson   : (string * obj []) [] -> Async<string>
    //parseAnnotationTablesToTableJson        : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
    parseAssayJsonToBuildingBlocks          : string -> Async<(string * obj []) []>
    //parseTableJsonToBuildingBlocks          : string -> Async<(string * OfficeInteropTypes.InsertBuildingBlock []) []>
    parseProcessSeqToBuildingBlocks         : string -> Async<(string * obj []) []>
}

type IExportAPIv1 = {
    toAssayXlsx                             : (string * obj []) []         -> Async<byte []>
}

module SwateObsolete =

    type TermMinimal = {
        Name            : string
        /// This is the Ontology Term Accession 'XX:aaaaaa'
        TermAccession   : string
    }

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
        SearchResultTerm    : Term option
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
    getTermSuggestionsByParentTerm      : (int*string*SwateObsolete.TermMinimal)                                      -> Async<Term []>
    getAllTermsByParentTerm             : SwateObsolete.TermMinimal                                                   -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByChildTerm       : (int*string*SwateObsolete.TermMinimal)                                      -> Async<Term []>
    getAllTermsByChildTerm              : SwateObsolete.TermMinimal                                                   -> Async<Term []>
    getTermsForAdvancedSearch           : (AdvancedSearchTypes.AdvancedSearchOptions)                   -> Async<Term []>
    getUnitTermSuggestions              : (int*string)                                                  -> Async<Term []>
    getTermsByNames                     : SwateObsolete.TermSearchable []                                             -> Async<SwateObsolete.TermSearchable []>

    // Tree related requests
    getTreeByAccession                  : string                                                        -> Async<TreeTypes.Tree>
}


/// <summary>
/// This is used for MIAPPE Wizard external tool. Before removing this contact them.
/// </summary>
type IOntologyAPIv2 = {
    // Development
    getTestNumber                       : unit                                                          -> Async<int>

    // Ontology related requests
    getAllOntologies                    : unit                                                          -> Async<Ontology []>

    // Term related requests
    ///
    getTermSuggestions                  : {| n: int; query: string; ontology: string option|} -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByParentTerm      : {| n: int; query: string; parent_term: SwateObsolete.TermMinimal |} -> Async<Term []>
    getAllTermsByParentTerm             : SwateObsolete.TermMinimal -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByChildTerm       : {| n: int; query: string; child_term: SwateObsolete.TermMinimal |} -> Async<Term []>
    getAllTermsByChildTerm              : SwateObsolete.TermMinimal -> Async<Term []>
    getTermsForAdvancedSearch           : (AdvancedSearchTypes.AdvancedSearchOptions) -> Async<Term []>
    getUnitTermSuggestions              : {| n: int; query: string|} -> Async<Term []>
    getTermsByNames                     : SwateObsolete.TermSearchable []   -> Async<SwateObsolete.TermSearchable []>

    // Tree related requests
    getTreeByAccession                  : string                            -> Async<TreeTypes.Tree>
}


type ITemplateAPIv1 = {
    // must return template as string, fable remoting cannot do conversion automatically
    getTemplates                    : unit      -> Async<string>
    getTemplateById                 : string    -> Async<string>
}
