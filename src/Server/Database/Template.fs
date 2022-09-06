module Database.Template

open Neo4j.Driver
open System

open Shared.TemplateTypes
open Helper

open ISADotNet
open Newtonsoft.Json

type Author = {
    [<JsonProperty("First Name")>]
    FirstName: string
    [<JsonProperty("Mid Initials")>]
    MidInitials: string
    [<JsonProperty("Last Name")>]
    LastName: string
    Email: string
    Phone: string
    Address: string
    Affiliation: string
    ORCID: string
    Role: string
    [<JsonProperty("Role Term Accession Number")>]
    RoleTermAccessionNumber: string
    [<JsonProperty("Role Term Source REF")>]
    RoleTermSourceREF: string
}

type Tag = {
    [<JsonProperty("#")>]
    Term: string
    [<JsonProperty("Term Accession Number")>]
    TermAccessionNumber: string
    [<JsonProperty("Term Source REF")>]
    TermSourceREF: string
}

module Queries =

    type Template(credentials:Neo4JCredentials) =

        /// This function tries not to parse templateJson to building blocks, but instead ignores the templateJson
        static member private asTemplateMinimal: IRecord -> Shared.TemplateTypes.Template =
            fun (record:IRecord) -> 
                let erTags =
                    let dbEntry = record.["t.erTags"] |> defaultOutputWith<string> ""
                    if dbEntry = "" then Array.empty else JsonConvert.DeserializeObject<Tag[]> dbEntry |> Array.map (fun x -> x.Term)
                let tags =
                    let dbEntry = record.["t.tags"] |> defaultOutputWith<string> ""
                    if dbEntry = "" then Array.empty else JsonConvert.DeserializeObject<Tag[]> dbEntry |> Array.map (fun x -> x.Term)
                let authors =
                    let dbEntry = record.["t.authors"] |> defaultOutputWith<string> ""
                    if dbEntry = "" then ""
                    else
                        JsonConvert.DeserializeObject<Author[]> dbEntry
                        |> Array.map (fun x ->
                            // This syntax allows flexible scaling without much more logic.
                            [x.FirstName; x.MidInitials; x.LastName]
                            |> List.filter (fun x -> x <> "")
                            |> String.concat " " 
                        )
                        |> String.concat ", "
                let (template:Shared.TemplateTypes.Template) = {
                    Id                      = record.["t.id"].As<string>()
                    Name                    = record.["t.name"].As<string>()
                    Description             = record.["t.description"] |> defaultOutputWith<string> ""
                    Organisation            = record.["t.organisation"] |> defaultOutputWith<string> ""
                    Version                 = record.["t.version"].As<string>()
                    Authors                 = authors
                    Er_Tags                 = erTags
                    Tags                    = tags
                    TemplateBuildingBlocks  = []
                    LastUpdated             = 
                        let r = record.["t.lastUpdated"].As<string>()
                        if isNull r then DateTime.MinValue else DateTime.Parse(r)
                    Used                    = record.["t.timesUsed"] |> defaultOutputWith<int> 0
                    // WIP                  
                    Rating                  = 0
                }
                template

        static member private asTemplate: IRecord -> Shared.TemplateTypes.Template =
            fun record ->
                let template =
                    let dbJson = record.["t.templateJson"].As<string>()
                    (dbJson |> rowMajorOfTemplateJson).headerToInsertBuildingBlockList
                    // Remove values from template, not wanted for this function
                    |> List.map (fun ibb -> {ibb with Rows = [||]})
                { Template.asTemplateMinimal record
                    with TemplateBuildingBlocks = template }

        member this.getAll() =
            let query =
                """MATCH (t:Template)
                RETURN t.id, t.name, t.description, t.organisation, t.version, t.authors, t.erTags, t.tags, t.lastUpdated, t.timesUsed"""
            Neo4j.runQuery(
                query,
                None,
                Template.asTemplateMinimal,
                credentials
            )

        member this.getById(id:string) =
            let query =
                """MATCH (t:Template {id: $Id})
                RETURN t.id, t.name, t.description, t.organisation, t.version, t.authors, t.erTags, t.tags, t.lastUpdated, t.timesUsed, t.templateJson"""
            let param = Map [ "Id", id ] |> Some
            let dbResults =
                Neo4j.runQuery(
                    query,
                    param,
                    Template.asTemplate,
                    credentials
                )
            if Array.ofSeq dbResults |> Array.length = 0 then failwith "Error. Could not find requested template in database, please try again or open a bug report."
            // Id is unique and should always only result in one found template
            Seq.head dbResults

        member this.increaseTimesUsed(id:string) =
            let query =
                """MATCH (t:Template {id: $Id})
                SET t.timesUsed = COALESCE(t.timesUsed,0) + 1
                RETURN t.id, t.name, t.description, t.organisation, t.version, t.authors, t.erTags, t.tags, t.lastUpdated, t.timesUsed"""
            let param = Map [ "Id", id ] |> Some
            Neo4j.runQuery(
                query,
                param,
                Template.asTemplate,
                credentials
            )