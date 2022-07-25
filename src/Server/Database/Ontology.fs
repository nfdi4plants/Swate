module Database.Ontology

open Neo4j.Driver
open Shared.TermTypes
open Helper

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
        Neo4j.runQuery(
            query,
            None,
            Ontology.asOntology,
            credentials
        )

    member this.getByName(name: string) = 
        let query = 
            @"MATCH (o:Ontology {name: $Name})
            RETURN o.name, o.lastUpdated, o.version, o.author" 
        let param =
            Map ["Name",name] |> Some
        Neo4j.runQuery(
            query,
            param,
            Ontology.asOntology,
            credentials
        )
