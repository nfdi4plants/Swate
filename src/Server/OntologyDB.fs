module OntologyDB

open System
open System.IO
open Neo4j

open Neo4j.Driver
open System.Threading.Tasks
open Shared.TermTypes

type FullTextSearch =
| Exact
| Complete
| Fuzzy
with
    member this.ofQueryString(queryString:string) =
        match this with
        | Exact         -> queryString
        | Complete      -> queryString + "*"
        | Fuzzy         -> queryString.Replace(" ","~ ") + "~"
            
type Neo4JCredentials = {
    User        : string
    Pw          : string
    BoltUrl     : string
    DatabaseName: string
}

let establishConnection(c: Neo4JCredentials) =
    let driver = Neo4j.Driver.GraphDatabase.Driver(c.BoltUrl, Neo4j.Driver.AuthTokens.Basic(c.User,c.Pw))
    driver.AsyncSession(SessionConfigBuilder.ForDatabase c.DatabaseName)

/// <summary>Standardized function to easily execute neo4j cypher query.</summary>
/// <param name="query">The cypher query string</param>
/// <param name="parameters">Map of key value pairs. Only use this if you used parameters, for example '$Name' in your query. In which case you need to provide `Map ["Name", value]`.</param>
/// <param name="resultAs">How to return query results. In the format of `(fun (record:IRecord) -> parsingFunction record)`.</param>
/// <param name="credentials">Username, password, bolt-url and database name to create session with database.</param>
let runNeo4JQuery (query:string) (parameters:Map<string,'a> option) (resultAs:IRecord -> 'T) credentials=
    use session = establishConnection(credentials)
    async {
        let! executeReadQuery =
            if parameters.IsSome then
                // Cast a whole lot of types to expected types by neo4j driver
                let param =
                    parameters.Value 
                    |> Map.fold (fun s k v ->  
                        let kvp = Collections.Generic.KeyValuePair.Create(k, box v)
                        kvp::s
                    ) []
                    |> fun x -> Collections.Generic.Dictionary<string,obj>(x :> Collections.Generic.IEnumerable<_>)
                session.RunAsync(Query(query,param))
            else
                session.RunAsync(query)
            |> Async.AwaitTask
        let! dbValues = 
            executeReadQuery.ToListAsync()
            |> Async.AwaitTask
        let parsedDbValues = dbValues |> Seq.map resultAs
        session.Dispose()
        return parsedDbValues
    } |> Async.RunSynchronously

module Queries =

    type Ontology(credentials:Neo4JCredentials) =

        static member private asOntology : IRecord -> Shared.TermTypes.Ontology =
            fun (record:IRecord) ->
                let (ontology:Shared.TermTypes.Ontology) = {
                    Name        = record.["o.name"].As<string>()
                    Version     = 
                        let r = record.["o.version"].As<string>()
                        if isNull r then "" else r
                    LastUpdated = record.["o.lastUpdated"].As<string>() |> System.DateTime.Parse
                    Author      = 
                        let r = record.["o.author"].As<string>()
                        if isNull r then "" else r
                }
                ontology

        member this.getAll() = 
            let query = 
                @"MATCH (o:Ontology)
                RETURN o.name, o.lastUpdated, o.version, o.author" 
            runNeo4JQuery
                query
                None
                Ontology.asOntology
                credentials

        member this.getByName(name: string) = 
            let query = 
                @"MATCH (o:Ontology {name: $Name})
                RETURN o.name, o.lastUpdated, o.version, o.author" 
            let param =
                Map ["Name",name]
            runNeo4JQuery
                query
                (Some param)
                Ontology.asOntology
                credentials

    type Term(credentials:Neo4JCredentials) =

        static member private parseAccessionToOntologyName (accession:string) =
            let i = accession.IndexOfAny [|':'; '_'|]
            if i>0 then accession.[..i-1] else failwith $"Cannot parse accession: '{accession}' to ontology name."

        /// "termParamName": the query function tries to map properties of the "termParamName" to this function so depending on how the node was called in the query this needs to adapt.
        static member private asTerm(termParamName) = fun (record:IRecord) ->
            let accession = record.[$"{termParamName}.accession"].As<string>()
            let (term:Shared.TermTypes.Term) = {
                Accession = accession
                FK_Ontology = Term.parseAccessionToOntologyName accession
                Name = 
                    let r = record.[$"{termParamName}.name"].As<string>()
                    if isNull r then "" else r
                Description = 
                    let r = record.[$"{termParamName}.description"].As<string>()
                    if isNull r then "" else r
                IsObsolete = record.[$"{termParamName}.isObsolete"].As<bool>()
            }
            term

        /// Searchtype defaults to "get term suggestions with auto complete".
        member this.getByName(termName:string, ?searchType:FullTextSearch, ?sourceOntologyName:string) =
            let fulltextSearchStr =
                if searchType.IsSome then
                    searchType.Value.ofQueryString termName
                else
                    FullTextSearch.Complete.ofQueryString termName
            let query =
                if sourceOntologyName.IsSome then
                    """CALL db.index.fulltext.queryNodes("TermName",$Name) 
                    YIELD node
                    WHERE EXISTS((:Term {accession: node.accession})-[:CONTAINED_IN]->(:Ontology {name: $OntologyName}))
                    RETURN node.accession, node.name, node.description, node.isObsolete"""
                else
                    """CALL db.index.fulltext.queryNodes("TermName",$Name) YIELD node
                    RETURN node.accession, node.name, node.description, node.isObsolete"""
            let param =
                Map [
                    "Name",fulltextSearchStr
                    if sourceOntologyName.IsSome then "OntologyName", sourceOntologyName.Value
                ]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("node"))
                credentials

        /// This function will allow for raw apache lucene input. It is possible to search either term name or description or both.
        /// The function will error if both term name and term description are None.
        member this.getByAdvancedTermSearch(advancedSearchOptions:Shared.AdvancedSearchTypes.AdvancedSearchOptions) =
            let termName = if advancedSearchOptions.TermName = "" then None else Some advancedSearchOptions.TermName
            let termDescription = if advancedSearchOptions.TermDescription = "" then None else Some advancedSearchOptions.TermDescription
            let indexName, queryInsert =
                match termName,termDescription with
                | None, None -> failwith "Cannot execute term search without any term name or term description."
                | Some _, None -> "TermName", termName.Value
                | Some name, Some desc -> "TermNameAndDescription", sprintf """name: "%s", description: "%s" """ name desc
                | None, Some _ -> "TermDescription", termDescription.Value
            let query =
                if advancedSearchOptions.OntologyName.IsSome then
                    sprintf
                        """CALL db.index.fulltext.queryNodes("%s", $Query) 
                        YIELD node
                        WHERE EXISTS((:Term {accession: node.accession})-[:CONTAINED_IN]->(:Ontology {name: $OntologyName}))
                        RETURN node.accession, node.name, node.description, node.isObsolete"""
                        indexName
                else
                    sprintf
                        """CALL db.index.fulltext.queryNodes("%s", $Query) YIELD node
                        RETURN node.accession, node.name, node.description, node.isObsolete"""
                        indexName
            let param =
                Map [
                    if advancedSearchOptions.OntologyName.IsSome then "OntologyName", advancedSearchOptions.OntologyName.Value
                    "Query", queryInsert
                ]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("node"))
                credentials


        /// Exact match for unique identifier term accession.
        member this.getByAccession(termAccession:string) =
            let query = 
                """MATCH (term:Term {accession: $Accession})
                RETURN term.accession, term.name, term.description, term.isObsolete"""
            let param =
                Map ["Accession",termAccession]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("term"))
                credentials

        member this.getAllByParent(parentAccession:string) =
            let query = 
                """MATCH (child)-[*1..]->(:Term {accession: $Accession})
                RETURN child.accession, child.name, child.description, child.isObsolete
                """
            let param =
                Map ["Accession",parentAccession]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("child"))
                credentials

        /// This function uses only the parent term accession
        member this.getAllByParent(parentAccession:TermMinimal) =
            let query = 
                """MATCH (child)-[*1..]->(:Term {accession: $Accession})
                RETURN child.accession, child.name, child.description, child.isObsolete
                """
            let param =
                Map ["Accession",parentAccession.TermAccession]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("child"))
                credentials

        /// Searchtype defaults to "get child term suggestions with auto complete"
        member this.getByNameAndParent(termName:string,parentAccession:string,?searchType:FullTextSearch) =
            let fulltextSearchStr =
                if searchType.IsSome then
                    searchType.Value.ofQueryString termName
                else
                    FullTextSearch.Complete.ofQueryString termName
            let query =
                """CALL db.index.fulltext.queryNodes("TermName", $Search) 
                YIELD node
                WITH node
                MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {accession: $Accession}) 
                RETURN child.accession, child.name, child.description, child.isObsolete"""
            let param =
                Map [
                    "Accession", parentAccession; 
                    "Search", fulltextSearchStr
                ]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("child"))
                credentials

        /// Searchtype defaults to "get child term suggestions with auto complete"
        member this.getByNameAndParent(term:TermMinimal,parent:TermMinimal,?searchType:FullTextSearch) =
            let fulltextSearchStr =
                if searchType.IsSome then
                    searchType.Value.ofQueryString term.Name
                else
                    FullTextSearch.Complete.ofQueryString term.Name
            let query =
                """CALL db.index.fulltext.queryNodes("TermName", $Search) 
                YIELD node
                WITH node
                MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {accession: $Accession}) 
                RETURN child.accession, child.name, child.description, child.isObsolete"""
            let param =
                Map [
                    "Accession", parent.TermAccession; 
                    "Search", fulltextSearchStr
                ]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("child"))
                credentials

        // Searchtype defaults to "get child term suggestions with auto complete"
        member this.getByNameAndParent(termName:string,parentAccession:TermMinimal,?searchType:FullTextSearch) =
            let fulltextSearchStr =
                if searchType.IsSome then
                    searchType.Value.ofQueryString termName
                else
                    FullTextSearch.Complete.ofQueryString termName
            let query =
                """CALL db.index.fulltext.queryNodes("TermName", $Search) 
                YIELD node
                WITH node
                MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {accession: $Accession}) 
                RETURN child.accession, child.name, child.description, child.isObsolete"""
            let param =
                Map [
                    "Accession", parentAccession.TermAccession; 
                    "Search", fulltextSearchStr
                ]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("child"))
                credentials

        member this.getByNameAndParent_Name(termName:string,parentName:string,?searchType:FullTextSearch) =
            let fulltextSearchStr =
                if searchType.IsSome then
                    searchType.Value.ofQueryString termName
                else
                    FullTextSearch.Complete.ofQueryString termName
            let query =
                """CALL db.index.fulltext.queryNodes("TermName", $Search) 
                YIELD node
                WITH node
                MATCH (child:Term {accession: node.accession})-[*1..]->(a:Term {name: $Name}) 
                RETURN child.accession, child.name, child.description, child.isObsolete"""
            let param =
                Map [
                    "Name", parentName; 
                    "Search", fulltextSearchStr
                ]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("child"))
                credentials

        /// This function uses only the parent term accession
        member this.getAllByChild(childAccession:TermMinimal) =
            let query = 
                """MATCH (:Term {accession: $Accession})-[*1..]->(parent:Term)
                RETURN parent.accession, parent.name, parent.description, parent.isObsolete
                """
            let param =
                Map ["Accession",childAccession.TermAccession]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("parent"))
                credentials

        /// Searchtype defaults to "get child term suggestions with auto complete"
        member this.getByNameAndChild(termName:string,childAccession:string,?searchType:FullTextSearch) =
            let fulltextSearchStr =
                if searchType.IsSome then
                    searchType.Value.ofQueryString termName
                else
                    FullTextSearch.Complete.ofQueryString termName
            let query =
                """CALL db.index.fulltext.queryNodes("TermName", $Search) 
                YIELD node
                WITH node
                MATCH (:Term {accession: $Accession})-[*1..]->(parent:Term {accession: node.accession})
                RETURN parent.accession, parent.name, parent.description, parent.isObsolete"""
            let param =
                Map [
                    "Accession", childAccession; 
                    "Search", fulltextSearchStr
                ]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("parent"))
                credentials

        /// Searchtype defaults to "get child term suggestions with auto complete"
        member this.getByNameAndChild_Name(termName:string,childName:string,?searchType:FullTextSearch) =
            let fulltextSearchStr =
                if searchType.IsSome then
                    searchType.Value.ofQueryString termName
                else
                    FullTextSearch.Complete.ofQueryString termName
            let query =
                """CALL db.index.fulltext.queryNodes("TermName", $Search) 
                YIELD node
                WITH node
                MATCH (:Term {name: $Name})-[*1..]->(parent:Term {accession: node.accession})
                RETURN parent.accession, parent.name, parent.description, parent.isObsolete"""
            let param =
                Map [
                    "Name", childName; 
                    "Search", fulltextSearchStr
                ]
            runNeo4JQuery
                query
                (Some param)
                (Term.asTerm("parent"))
                credentials