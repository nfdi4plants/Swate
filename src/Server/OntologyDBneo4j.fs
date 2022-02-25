module OntologyDB

open System
open System.IO
open Neo4j

module Types =

    type Term = {
        TermAccession: string
        FK_Ontology: string
        Name: string
        Description: string
        IsObsolete: bool
    }

    type TermRelationship = {
        ChildTerm: string
        Relationship: string
        ParentTerm: string
    }

open Types
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
/// <param name="prdocutionCredentials">Username, password, bolt-url and database name to create session with database.</param>
let runNeo4JQuery (query:string) (parameters:Map<string,'a> option) (resultAs:IRecord -> 'T) prdocutionCredentials=
    use session = establishConnection(prdocutionCredentials)
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

        static member private asOntology : IRecord -> DbDomain.Ontology =
            fun (record:IRecord) ->
                {
                    Name        = record.["o.name"].As<string>()
                    Version     = 
                        let r = record.["o.version"].As<string>()
                        if isNull r then "" else r
                    LastUpdated = record.["o.lastUpdated"].As<string>() |> System.DateTime.Parse
                    Author      = 
                        let r = record.["o.author"].As<string>()
                        if isNull r then "" else r
                }

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
            {
                TermAccession = accession
                FK_Ontology = Term.parseAccessionToOntologyName accession
                Name = 
                    let r = record.[$"{termParamName}.name"].As<string>()
                    if isNull r then "" else r
                Description = 
                    let r = record.[$"{termParamName}.description"].As<string>()
                    if isNull r then "" else r
                IsObsolete = record.[$"{termParamName}.isObsolete"].As<bool>()
            }

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

        /// getAllTermsByParentTermOntologyInfo
        member this.getAllByParentAccession(parentAccession:string) =
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

        member this.getAllByParentAccession(parentAccession:TermMinimal) =
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
        member this.getByNameAndParentAccession(termName:string,parentAccession:string,?searchType:FullTextSearch) =
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
        member this.getByNameAndParentAccession(termName:TermMinimal,parentAccession:TermMinimal,?searchType:FullTextSearch) =
            let fulltextSearchStr =
                if searchType.IsSome then
                    searchType.Value.ofQueryString termName.Name
                else
                    FullTextSearch.Complete.ofQueryString termName.Name
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

