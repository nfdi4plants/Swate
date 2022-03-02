namespace Shared

open System
open Shared
open TermTypes
open ProtocolTemplateTypes

module Route =

    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

module SorensenDice =
    
    let inline calculateDistance (x : Set<'T>) (y : Set<'T>) =
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

    let sortBySimilarity (searchStr:string) (f: 'a -> string) (arrayToSort:'a []) =
        let searchSet = searchStr |> createBigrams
        arrayToSort
        |> Array.sortByDescending (fun result ->
            let resultSet = f result |> createBigrams
            calculateDistance resultSet searchSet
        )

[<RequireQualifiedAccess>]
type JsonExportType =
| ProcessSeq
| Assay
| Table
| ProtocolTemplate
    member this.toExplanation =
        match this with
        | ProcessSeq        -> "Sequence of ISA process.json."
        | Assay             -> "ISA assay.json"
        | Table             -> "Access layer type. table.json utilizes minor ISA-JSON types in a more accessible model."
        | ProtocolTemplate  -> "Schema for Swate protocol template, with template metadata and table json."

/// This type is used to define target for unit term search.
type UnitSearchRequest =
| Unit1
| Unit2

/// Development api
type ITestAPI = {
    test : unit      -> Async<string*string>
    postTest: string -> Async<string*string>
}

//type IISADotNetAPIv1 = {
//    parseJsonToProcess      : string    -> Async<ISADotNet.Process> //Async<ISADotNet.Process>
//}

type IServiceAPIv1 = {
    getAppVersion           : unit      -> Async<string>
}

type IDagAPIv1 = {
    parseAnnotationTablesToDagHtml          : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
}

type IISADotNetCommonAPIv1 = {
    toAssayJson                 : byte [] -> Async<obj>
    toSwateTemplateJson         : byte [] -> Async<obj>
    toInvestigationJson         : byte [] -> Async<obj>
    toProcessSeqJson            : byte [] -> Async<obj>
    toTableJson                 : byte [] -> Async<obj>
    toAssayJsonStr              : byte [] -> Async<string>
    toSwateTemplateJsonStr      : byte [] -> Async<string>
    toInvestigationJsonStr      : byte [] -> Async<string>
    toProcessSeqJsonStr         : byte [] -> Async<string>
    toTableJsonStr              : byte [] -> Async<string>
    testPostNumber              : int   -> Async<string>
    getTestNumber               : unit  -> Async<string>
}

type ISwateJsonAPIv1 = {
    parseAnnotationTableToAssayJson         : string * OfficeInteropTypes.BuildingBlock []      -> Async<string>
    parseAnnotationTableToProcessSeqJson    : string * OfficeInteropTypes.BuildingBlock []      -> Async<string>
    parseAnnotationTableToTableJson         : string * OfficeInteropTypes.BuildingBlock []      -> Async<string>
    parseAnnotationTablesToAssayJson        : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
    parseAnnotationTablesToProcessSeqJson   : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
    parseAnnotationTablesToTableJson        : (string * OfficeInteropTypes.BuildingBlock []) [] -> Async<string>
    parseAssayJsonToBuildingBlocks          : string -> Async<(string * OfficeInteropTypes.InsertBuildingBlock []) []>
    parseTableJsonToBuildingBlocks          : string -> Async<(string * OfficeInteropTypes.InsertBuildingBlock []) []>
    parseProcessSeqToBuildingBlocks         : string -> Async<(string * OfficeInteropTypes.InsertBuildingBlock []) []>
}

type IOntologyAPIv1 = {
    // Development
    getTestNumber               : unit                                          -> Async<int>

    // Ontology related requests
    getAllOntologies            : unit                                          -> Async<Ontology []>

    // Term related requests
    ///
    getTermSuggestions                  : (int*string)                                                  -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByParentTerm      : (int*string*TermMinimal)                                      -> Async<Term []>
    getAllTermsByParentTerm             : TermMinimal                                                   -> Async<Term []>
    /// (nOfReturnedResults*queryString*parentOntology). If parentOntology = "" then isNull -> Error.
    getTermSuggestionsByChildTerm       : (int*string*TermMinimal)                                      -> Async<Term []>
    getAllTermsByChildTerm              : TermMinimal                                                   -> Async<Term []>
    /// (ontOpt,searchName,mustContainName,searchDefinition,mustContainDefinition,keepObsolete)
    getTermsForAdvancedSearch           : (Ontology option*string*string*string*string*bool)   -> Async<Term []>
    getUnitTermSuggestions              : (int*string*UnitSearchRequest)                                -> Async<Term [] * UnitSearchRequest>
    getTermsByNames                     : TermSearchable []                                             -> Async<TermSearchable []>
}

type IProtocolAPIv1 = {
    getAllProtocolsWithoutXml       : unit                      -> Async<ProtocolTemplate []>
    getProtocolByName               : string                    -> Async<ProtocolTemplate>
    getProtocolsByName              : string []                 -> Async<ProtocolTemplate []>
    increaseTimesUsed               : string                    -> Async<unit>
}

        