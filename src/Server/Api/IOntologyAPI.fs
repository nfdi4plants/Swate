module API.IOntologyAPI

open Shared
open TermTypes

open Database
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open ARCtrl

[<RequireQualifiedAccess>]
module V3 =
    open ARCtrl.ISA.Regex.ActivePatterns

    let ontologyApi (credentials : Helper.Neo4JCredentials) : IOntologyAPIv3 =
        {
            //Development
            getTestNumber = 
                fun () -> async { return 42 }
            searchTerms = 
                fun content -> 
                    async {
                        let dbSearchRes =
                            match content.query with
                            | TermAnnotationShort taninfo ->
                                Term.Term(credentials).getByAccession $"{taninfo.IDSpace}:{taninfo.LocalID}"
                            // This suggests we search for a term name
                            | notAnAccession ->
                                let searchTextLength = content.query.Length
                                let searchmode = if searchTextLength < 3 then Database.Helper.FullTextSearch.Exact else Database.Helper.FullTextSearch.PerformanceComplete
                                let ontologies =
                                    if content.ontologies.IsEmpty then
                                        None
                                    elif content.ontologies.Length = 1 then
                                        Term.AnyOfOntology.Single content.ontologies.Head |> Some
                                    else
                                        Term.AnyOfOntology.Multiples content.ontologies |> Some
                                Term.Term(credentials).getByName(notAnAccession, searchmode, ?sourceOntologyName = ontologies, limit=content.limit)
                            |> Array.ofSeq
                            //|> sorensenDiceSortTerms typedSoFar
                        //let arr = if dbSearchRes.Length <= content.n then dbSearchRes else Array.take inp.n dbSearchRes
                        //let arrSorted = sorensenDiceSortTerms inp.query arr 
                        return dbSearchRes
                    }
            
        }

    let createIOntologyApi credentials =
        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue (ontologyApi credentials)
        |> Remoting.withDiagnosticsLogger(printfn "%A")
        |> Remoting.withErrorHandler Helper.errorHandler
        |> Remoting.buildHttpHandler

[<RequireQualifiedAccess>]
module V1 =

    /// <summary>Deprecated</summary>
    let ontologyApi (credentials : Helper.Neo4JCredentials) : IOntologyAPIv1 =
        /// We use sorensen dice to avoid scoring mutliple occassions of the same word (Issue https://github.com/nfdi4plants/Swate/issues/247)
        let sorensenDiceSortTerms (searchStr:string) (terms: Term []) =
            terms |> SorensenDice.sortBySimilarity searchStr (fun term -> term.Name)
    
        {
            //Development

            getTestNumber = fun () -> async { return 42 }

            //Ontology related requests

            getAllOntologies = fun () ->
                async {
                    let results = Ontology.Ontology(credentials).getAll() |> Array.ofSeq
                    return results
                }

            // Term related requests

            getTermSuggestions = fun (max: int, typedSoFar: string) ->
                async {
                    let dbSearchRes =
                        match typedSoFar with
                        | Regex.Aux.Regex Regex.Pattern.TermAnnotationShortPattern foundAccession ->
                            Term.Term(credentials).getByAccession foundAccession.Value
                        // This suggests we search for a term name
                        | notAnAccession ->
                            let searchTextLength = typedSoFar.Length
                            let searchmode = if searchTextLength < 3 then Database.Helper.FullTextSearch.Exact else Database.Helper.FullTextSearch.PerformanceComplete
                            Term.Term(credentials).getByName(notAnAccession, searchmode)
                        |> Array.ofSeq
                    let arr = if dbSearchRes.Length <= max then dbSearchRes else Array.take max dbSearchRes
                    return arr
                }

            getTermSuggestionsByParentTerm = fun (max:int,typedSoFar:string,parentTerm:TermMinimal) ->
                async {
                    let dbSearchRes =
                        match typedSoFar with
                        | Regex.Aux.Regex Regex.Pattern.TermAnnotationShortPattern foundAccession ->
                            Database.Term.Term(credentials).getByAccession foundAccession.Value
                        | _ ->
                            let searchmode = if typedSoFar.Length < 3 then Database.Helper.FullTextSearch.Exact else Database.Helper.FullTextSearch.PerformanceComplete
                            if parentTerm.TermAccession = ""
                            then
                                Term.Term(credentials).getByNameAndParent_Name(typedSoFar, parentTerm.Name, searchmode)
                            else
                                Term.Term(credentials).getByNameAndParent(typedSoFar, parentTerm, searchmode)
                        |> Array.ofSeq
                    let arr = if dbSearchRes.Length <= max then dbSearchRes else Array.take max dbSearchRes
                    return arr
                }

            getAllTermsByParentTerm = fun (parentTerm:TermMinimal) ->
                async {
                    let searchRes = Database.Term.Term(credentials).getAllByParent(parentTerm,limit=500) |> Array.ofSeq
                    return searchRes  
                }

            getTermSuggestionsByChildTerm = fun (max:int,typedSoFar:string,childTerm:TermMinimal) ->
                async {

                    let dbSearchRes =
                        match typedSoFar with
                        | Regex.Aux.Regex Regex.Pattern.TermAnnotationShortPattern foundAccession ->
                            Term.Term(credentials).getByAccession foundAccession.Value
                        | _ ->
                            if childTerm.TermAccession = ""
                            then
                                Term.Term(credentials).getByNameAndChild_Name (typedSoFar,childTerm.Name,Helper.FullTextSearch.PerformanceComplete)
                            else
                                Term.Term(credentials).getByNameAndChild(typedSoFar,childTerm.TermAccession,Helper.FullTextSearch.PerformanceComplete)
                        |> Array.ofSeq
                        //|> sorensenDiceSortTerms typedSoFar
                    let res = if dbSearchRes.Length <= max then dbSearchRes else Array.take max dbSearchRes
                    return res
                }

            getAllTermsByChildTerm = fun (childTerm:TermMinimal) ->
                async {
                    let searchRes = Term.Term(credentials).getAllByChild (childTerm) |> Array.ofSeq
                    return searchRes  
                }

            getTermsForAdvancedSearch = fun advancedSearchOption ->
                async {
                    let result = Term.Term(credentials).getByAdvancedTermSearch(advancedSearchOption) |> Array.ofSeq
                    let filteredResult =
                        if advancedSearchOption.KeepObsolete then
                            result
                        else
                            result |> Array.filter (fun x -> x.IsObsolete |> not)
                    return filteredResult
                }

            getUnitTermSuggestions = fun (max:int,typedSoFar:string) ->
                async {
                    let dbSearchRes =
                        match typedSoFar with
                        | Regex.Aux.Regex Regex.Pattern.TermAnnotationShortPattern foundAccession ->
                            Term.Term(credentials).getByAccession foundAccession.Value
                        | notAnAccession ->
                            Term.Term(credentials).getByName(notAnAccession,sourceOntologyName= Term.AnyOfOntology.Single "uo")
                        |> Array.ofSeq
                        //|> sorensenDiceSortTerms typedSoFar
                    let res = if dbSearchRes.Length <= max then dbSearchRes else Array.take max dbSearchRes
                    return res
                }

            getTermsByNames = fun (queryArr) ->
                async {
                    // check if search string is empty. This case should delete TAN- and TSR- values in table
                    let filteredQueries = queryArr |> Array.filter (fun x -> x.Term.Name <> "" || x.Term.TermAccession <> "")
                    let queries =
                        filteredQueries |> Array.map (fun searchTerm ->
                            // check if term accession was found. If so search also by this as it is unique
                            if searchTerm.Term.TermAccession <> "" then
                                Term.TermQuery.getByAccession searchTerm.Term.TermAccession
                            // if term is a unit it should be contained inside the unit ontology, if not it is most likely free text input.
                            elif searchTerm.IsUnit then
                                Term.TermQuery.getByName(searchTerm.Term.Name, searchType=Helper.FullTextSearch.Exact, sourceOntologyName= Term.AnyOfOntology.Single "uo")
                            // if none of the above apply we do a standard term search
                            else
                                Term.TermQuery.getByName(searchTerm.Term.Name, searchType=Helper.FullTextSearch.Exact)
                        )
                    let result =
                        Helper.Neo4j.runQueries(queries,credentials)
                        |> Array.map2 (fun termSearchable dbResults ->
                            // replicate if..elif..else conditions from 'queries'
                            if termSearchable.Term.TermAccession <> "" then
                                let result =
                                    if Array.isEmpty dbResults then
                                        None
                                    else
                                        // search by accession must be unique, and has unique restriction in database, so there can only be 0 or 1 result
                                        let r = dbResults |> Array.exactlyOne
                                        if r.Name <> termSearchable.Term.Name then 
                                            failwith $"""Found mismatch between Term Accession and Term Name. Term name "{termSearchable.Term.Name}" and term accession "{termSearchable.Term.TermAccession}",
                                            but accession belongs to name "{r.Name}" (ontology: {r.FK_Ontology})"""
                                        Some r
                                { termSearchable with SearchResultTerm = result }
                            // search is done by name and only in the unit ontology. Therefore unit term must be unique.
                            // This might need future work, as we might want to support types of unit outside of the unit ontology
                            elif termSearchable.IsUnit then
                                { termSearchable with SearchResultTerm = if dbResults |> Array.isEmpty then None else Some dbResults.[0] }
                            else
                                { termSearchable with SearchResultTerm = if dbResults |> Array.isEmpty then None else Some dbResults.[0] }
                        ) filteredQueries
                    return result
                }

            // Tree related requests
            getTreeByAccession = fun accession -> async {
                let tree = Database.TreeSearch.Tree(credentials).getByAccession(accession)
                return tree
            }
        }

    open Helper

    let createIOntologyApi credentials =
        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue (ontologyApi credentials)
        |> Remoting.withDiagnosticsLogger(printfn "%A")
        |> Remoting.withErrorHandler errorHandler
        |> Remoting.buildHttpHandler

[<RequireQualifiedAccess>]
module V2 =

    let ontologyApi (credentials : Helper.Neo4JCredentials) : IOntologyAPIv2 =
        /// <summary>We use sorensen dice to avoid scoring mutliple occassions of the same word (Issue https://github.com/nfdi4plants/Swate/issues/247)</summary>
        let sorensenDiceSortTerms (searchStr:string) (terms: Term []) =
            terms |> SorensenDice.sortBySimilarity searchStr (fun term -> term.Name)
    
        {
            //Development

            getTestNumber = fun () -> async { return 42 }

            //Ontology related requests

            getAllOntologies = fun () ->
                async {
                    let results = Ontology.Ontology(credentials).getAll() |> Array.ofSeq
                    return results
                }

            // Term related requests

            getTermSuggestions = fun inp ->
                async {
                    let dbSearchRes =
                        match inp.query with
                        | Regex.Aux.Regex Regex.Pattern.TermAnnotationShortPattern foundAccession ->
                            Term.Term(credentials).getByAccession foundAccession.Value
                        // This suggests we search for a term name
                        | notAnAccession ->
                            let searchTextLength = inp.query.Length
                            let searchmode = if searchTextLength < 3 then Database.Helper.FullTextSearch.Exact else Database.Helper.FullTextSearch.PerformanceComplete
                            Term.Term(credentials).getByName(notAnAccession, searchmode, ?sourceOntologyName = Option.map Term.AnyOfOntology.Single inp.ontology)
                        |> Array.ofSeq
                        //|> sorensenDiceSortTerms typedSoFar
                    let arr = if dbSearchRes.Length <= inp.n then dbSearchRes else Array.take inp.n dbSearchRes
                    let arrSorted = sorensenDiceSortTerms inp.query arr 
                    return arrSorted
                }

            getTermSuggestionsByParentTerm = fun inp ->
                printfn "[getTermSuggestionsByParentTerm] start"
                async {
                    let dbSearchRes =
                        match inp.query with
                        | Regex.Aux.Regex Regex.Pattern.TermAnnotationShortPattern foundAccession ->
                            Database.Term.Term(credentials).getByAccession foundAccession.Value
                        | _ ->
                            printfn "[getTermSuggestionsByParentTerm] Hit default search"
                            let searchmode = if inp.query.Length < 3 then Database.Helper.FullTextSearch.Exact else Database.Helper.FullTextSearch.PerformanceComplete
                            printfn "[getTermSuggestionsByParentTerm] searchmode: %A" searchmode
                            if inp.parent_term.TermAccession = ""
                            then
                                printfn "[getTermSuggestionsByParentTerm] 1"
                                Term.Term(credentials).getByNameAndParent_Name(inp.query, inp.parent_term.Name, searchmode)
                            else
                                printfn "[getTermSuggestionsByParentTerm] 2"
                                Term.Term(credentials).getByNameAndParent(inp.query, inp.parent_term, searchmode)
                        |> Array.ofSeq
                        //|> sorensenDiceSortTerms inp.query
                    let arr = if dbSearchRes.Length <= inp.n then dbSearchRes else Array.take inp.n dbSearchRes
                    let arrSorted = sorensenDiceSortTerms inp.query arr 
                    return arrSorted
                }

            getAllTermsByParentTerm = fun (parentTerm:TermMinimal) ->
                async {
                    let searchRes = Database.Term.Term(credentials).getAllByParent(parentTerm,limit=500) |> Array.ofSeq
                    return searchRes  
                }

            getTermSuggestionsByChildTerm = fun inp ->
                async {

                    let dbSearchRes =
                        match inp.query with
                        | Regex.Aux.Regex Regex.Pattern.TermAnnotationShortPattern foundAccession ->
                            Term.Term(credentials).getByAccession foundAccession.Value
                        | _ ->
                            let searchmode = if inp.query.Length < 3 then Database.Helper.FullTextSearch.Exact else Database.Helper.FullTextSearch.PerformanceComplete
                            if inp.child_term.TermAccession = ""
                            then
                                Term.Term(credentials).getByNameAndChild_Name (inp.query,inp.child_term.Name,searchmode)
                            else
                                Term.Term(credentials).getByNameAndChild(inp.query,inp.child_term.TermAccession,searchmode)
                        |> Array.ofSeq
                        //|> sorensenDiceSortTerms inp.query
                    let arr = if dbSearchRes.Length <= inp.n then dbSearchRes else Array.take inp.n dbSearchRes
                    let arrSorted = sorensenDiceSortTerms inp.query arr 
                    return arrSorted
                }

            getAllTermsByChildTerm = fun (childTerm:TermMinimal) ->
                async {
                    let searchRes = Term.Term(credentials).getAllByChild (childTerm) |> Array.ofSeq
                    return searchRes  
                }

            getTermsForAdvancedSearch = fun advancedSearchOption ->
                async {
                    let result = Term.Term(credentials).getByAdvancedTermSearch(advancedSearchOption) |> Array.ofSeq
                    let filteredResult =
                        if advancedSearchOption.KeepObsolete then
                            result
                        else
                            result |> Array.filter (fun x -> x.IsObsolete |> not)
                    return filteredResult
                }

            getUnitTermSuggestions = fun inp (*(max:int,typedSoFar:string, unit:UnitSearchRequest)*) ->
                async {
                    let dbSearchRes =
                        match inp.query with
                        | Regex.Aux.Regex Regex.Pattern.TermAnnotationShortPattern foundAccession ->
                            Term.Term(credentials).getByAccession foundAccession.Value
                        | notAnAccession ->
                            Term.Term(credentials).getByName(notAnAccession, sourceOntologyName = Term.AnyOfOntology.Multiples ["uo"; "dpbo"])
                        |> Array.ofSeq
                        //|> sorensenDiceSortTerms typedSoFar
                    let res = if dbSearchRes.Length <= inp.n then dbSearchRes else Array.take inp.n dbSearchRes
                    return (res)
                }

            getTermsByNames = fun (queryArr) ->
                async {
                    // check if search string is empty. This case should delete TAN- and TSR- values in table
                    let filteredQueries = queryArr |> Array.filter (fun x -> x.Term.Name <> "" || x.Term.TermAccession <> "")
                    let queries =
                        filteredQueries |> Array.map (fun searchTerm ->
                            // check if term accession was found. If so search also by this as it is unique
                            if searchTerm.Term.TermAccession <> "" then
                                Term.TermQuery.getByAccession searchTerm.Term.TermAccession
                            // if term is a unit it should be contained inside the unit ontology, if not it is most likely free text input.
                            elif searchTerm.IsUnit then
                                Term.TermQuery.getByName(searchTerm.Term.Name, searchType=Helper.FullTextSearch.Exact, sourceOntologyName = Term.AnyOfOntology.Multiples ["uo"; "dpbo"])
                            // if none of the above apply we do a standard term search
                            else
                                Term.TermQuery.getByName(searchTerm.Term.Name, searchType=Helper.FullTextSearch.Exact)
                        )
                    let result =
                        Helper.Neo4j.runQueries(queries,credentials)
                        |> Array.map2 (fun termSearchable dbResults ->
                            // replicate if..elif..else conditions from 'queries'
                            if termSearchable.Term.TermAccession <> "" then
                                let result =
                                    if Array.isEmpty dbResults then
                                        None
                                    else
                                        // search by accession must be unique, and has unique restriction in database, so there can only be 0 or 1 result
                                        let r = dbResults |> Array.exactlyOne
                                        if termSearchable.Term.Name <> "" && r.Name <> termSearchable.Term.Name then 
                                            failwith $"""Found mismatch between Term Accession and Term Name. Term name "{termSearchable.Term.Name}" and term accession "{termSearchable.Term.TermAccession}",
                                            but accession belongs to name "{r.Name}" (ontology: {r.FK_Ontology})"""
                                        Some r
                                { termSearchable with SearchResultTerm = result }
                            // search is done by name and only in the unit ontology. Therefore unit term must be unique.
                            // This might need future work, as we might want to support types of unit outside of the unit ontology
                            elif termSearchable.IsUnit then
                                { termSearchable with SearchResultTerm = if dbResults |> Array.isEmpty then None else Some dbResults.[0] }
                            else
                                { termSearchable with SearchResultTerm = if dbResults |> Array.isEmpty then None else Some dbResults.[0] }
                        ) filteredQueries
                    return result
                }

            // Tree related requests
            getTreeByAccession = fun accession -> async {
                let tree = Database.TreeSearch.Tree(credentials).getByAccession(accession)
                return tree
            }
        }

    open Helper

    let createIOntologyApi credentials =
        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue (ontologyApi credentials)
        |> Remoting.withDiagnosticsLogger(printfn "%A")
        |> Remoting.withErrorHandler errorHandler
        |> Remoting.buildHttpHandler