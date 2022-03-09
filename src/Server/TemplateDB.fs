module TemplateDB

open Neo4j.Driver
open System.Data
open System

open Shared.TermTypes
open Shared.TemplateTypes

open ISADotNet

module Queries =

    type Template(credentials:OntologyDB.Neo4JCredentials) =

        /// This function tries not to parse templateJson to building blocks, but instead ignores the templateJson
        static member private asTemplateMinimal: IRecord -> Shared.TemplateTypes.Template =
            fun (record:IRecord) ->
                let erTags = record.["t.erTags"].As<string>() |> fun x -> x.Split(", ", StringSplitOptions.RemoveEmptyEntries)
                let tags = record.["t.tags"].As<string>() |> fun x -> x.Split(", ", StringSplitOptions.RemoveEmptyEntries)
                let (template:Shared.TemplateTypes.Template) = {
                    Id                      = record.["t.id"].As<string>()
                    Name                    = record.["t.name"].As<string>()
                    Description             = record.["t.description"].As<string>()
                    Organisation            = record.["t.organisation"].As<string>()
                    Version                 = record.["t.version"].As<string>()
                    Authors                 = record.["t.authors"].As<string>()
                    Er_Tags                 = erTags
                    Tags                    = tags
                    TemplateBuildingBlocks  = []
                    LastUpdated             =
                        let r = record.["t.lastUpdated"].As<string>()
                        if isNull r then DateTime.MinValue else DateTime.Parse(r)
                    Used                    =
                        let r = record.["t.timesUsed"].As<string>()
                        if isNull r then 0 else int r
                    // WIP                  
                    Rating                  = 0
                }
                template

        static member private asTemplate: IRecord -> Shared.TemplateTypes.Template =
            fun record ->
                let template =
                    let dbJson = record.["t.templateJson"].As<string>()
                    (dbJson |> rowMajorOfTemplateJson).headerToInsertBuildingBlockList
                    /// Remove values from template, not wanted for this function
                    |> List.map (fun ibb -> {ibb with Rows = [||]})
                { Template.asTemplateMinimal record
                    with TemplateBuildingBlocks = template }

        member this.getAll() =
            let query =
                """MATCH (t:Template)
                RETURN t.id, t.name, t.description, t.organisation, t.version, t.authors, t.erTags, t.tags, t.lastUpdated, t.timesUsed"""
            OntologyDB.runNeo4JQuery
                query
                None
                Template.asTemplateMinimal
                credentials

        member this.getById(id:string) =
            let query =
                """MATCH (t:Template {id: $Id})
                RETURN t.id, t.name, t.description, t.organisation, t.version, t.authors, t.erTags, t.tags, t.lastUpdated, t.timesUsed, t.templateJson"""
            let param = Map [ "Id", id ] |> Some
            let dbResults =
                OntologyDB.runNeo4JQuery
                    query
                    param
                    Template.asTemplate
                    credentials
            if Seq.length dbResults = 0 then failwith "Error. Could not find requested template in database, please try again or open a bug report."
            /// Id is unique and should always only result in one found template
            Seq.head dbResults

        member this.increaseTimesUsed(id:string) =
            let query =
                """MATCH (t:Template {id: $Id})
                SET t.timesUsed = COALESCE(t.timesUsed,0) + 1
                RETURN t.id, t.name, t.description, t.organisation, t.version, t.authors, t.erTags, t.tags, t.lastUpdated, t.timesUsed"""
            let param = Map [ "Id", id ] |> Some
            OntologyDB.runNeo4JQuery
                query
                param
                Template.asTemplate
                credentials
