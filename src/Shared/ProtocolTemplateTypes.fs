namespace Shared

module ProtocolTemplateTypes =

    open System

    [<Literal>]
    let TemplateMetadataWorksheetName = "SwateTemplateMetadata" 

    type MetadataField = {
        Key         : string
        Description : string option
        List        : MetadataField option
        Children    : MetadataField list
    } with
        static member create key islist desc children = {
            Key         = key
            Description = desc
            List        = islist
            Children    = children
        }

    //[<RequireQualifiedAccess>]
    //module MetadataFieldKeys =

    //    [<LiteralAttribute>]
    //    let TemplateID = "TemplateId"
    //    [<LiteralAttribute>]
    //    let TemplateName = "TemplateName"
    //    [<LiteralAttribute>]
    //    let Version = "Version"
    //    [<LiteralAttribute>]
    //    let Author = "Author"
    //    [<LiteralAttribute>]
    //    let Description = "Description"
    //    [<LiteralAttribute>]
    //    let DocsLink = "DocsLink"
    //    [<LiteralAttribute>]
    //    let ER = "ER"
    //    [<LiteralAttribute>]
    //    let Organisation = "Organisation"
    //    [<LiteralAttribute>]
    //    let Tags = "Tags"

    //    let MetadataFieldKeysArray = [|TemplateID; TemplateName; Version; Author; Description; DocsLink; ER; Organisation; Tags|]

    //    let NumberOfRows = float MetadataFieldKeysArray.Length

    //module TemplateMetadata =

    //    let fieldToTableRowIndex fieldNameStr =
    //        match fieldNameStr with
    //        | MetadataFieldKeys.TemplateID          -> 0.
    //        | MetadataFieldKeys.TemplateName        -> 1.
    //        | MetadataFieldKeys.Version             -> 2.
    //        | MetadataFieldKeys.Description         -> 3.
    //        | MetadataFieldKeys.DocsLink            -> 4.
    //        | MetadataFieldKeys.Organisation        -> 5.
    //        | MetadataFieldKeys.ER                  -> 6.
    //        | MetadataFieldKeys.Tags                -> 7.
    //        | MetadataFieldKeys.Author              -> 8.
    //        | anythingElse      -> failwith $"Unknown template metadata field: {anythingElse}"

    //    let tableRowIndexToFieldName index =
    //        match index with
    //        | 0.                                    -> MetadataFieldKeys.TemplateID           
    //        | 1.                                    -> MetadataFieldKeys.TemplateName         
    //        | 2.                                    -> MetadataFieldKeys.Version              
    //        | 3.                                    -> MetadataFieldKeys.Description          
    //        | 4.                                    -> MetadataFieldKeys.DocsLink             
    //        | 5.                                    -> MetadataFieldKeys.Organisation         
    //        | 6.                                    -> MetadataFieldKeys.ER                   
    //        | 7.                                    -> MetadataFieldKeys.Tags                 
    //        | 8.                                    -> MetadataFieldKeys.Author                 
    //        | anythingElse      -> failwith $"Unknown template metadata field: {anythingElse}"

    type ProtocolTemplate = {
        Name                    : string
        Version                 : string
        Created                 : System.DateTime
        Author                  : string
        Description             : string
        DocsLink                : string
        Tags                    : string []
        TemplateBuildingBlocks  : OfficeInteropTypes.InsertBuildingBlock list
        Used                    : int
        // WIP
        Rating                  : int  
    } with
        static member create name version created author desc docs tags templateBuildingBlocks used rating  = {
            Name                    = name
            Version                 = version
            Created                 = created 
            Author                  = author
            Description             = desc
            DocsLink                = docs
            Tags                    = tags
            TemplateBuildingBlocks  = templateBuildingBlocks
            Used                    = used
            // WIP                  
            Rating                  = rating
    }