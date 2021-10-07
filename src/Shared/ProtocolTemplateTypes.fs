namespace Shared

module ProtocolTemplateTypes =

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