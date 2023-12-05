namespace Shared

module AdvancedSearchTypes =

    type AdvancedSearchOptions = {
        OntologyName            : string option
        TermName                : string
        TermDefinition          : string
        KeepObsolete            : bool
    } with
        static member init() = {
            OntologyName            = None
            TermName                = ""
            TermDefinition          = ""
            KeepObsolete            = false
        }