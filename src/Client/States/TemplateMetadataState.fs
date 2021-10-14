namespace TemplateMetadata

open Shared

type Model = {
    Default: obj
    MetadataFields : ProtocolTemplateTypes.MetadataField option
} with
    static member init() = {
        Default     = ""
        MetadataFields  = None
    }

type Msg =
| CreateTemplateMetadataWorksheet of ProtocolTemplateTypes.MetadataField option
| GetTemplateMetadataJsonSchemaRequest
| GetTemplateMetadataJsonSchemaResponse of string