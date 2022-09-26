namespace TemplateMetadata

open Shared
open TemplateTypes.Metadata

type Model = {
    Default: obj
    MetadataFields : MetadataField option
} with
    static member init() = {
        Default     = ""
        MetadataFields  = None
    }

type Msg =
| CreateTemplateMetadataWorksheet of MetadataField
//| GetTemplateMetadataJsonSchemaRequest
//| GetTemplateMetadataJsonSchemaResponse of string