namespace Shared

module AdvancedSearchTypes =

    open TermTypes

    type AdvancedSearchOptions = {
        OntologyName            : string option
        TermName                : string
        TermDescription         : string
        KeepObsolete            : bool
        } with
            static member init() = {
                OntologyName            = None
                TermName                = ""
                TermDescription         = ""
                KeepObsolete            = false
            }