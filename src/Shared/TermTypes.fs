namespace Shared

open ARCtrl

module TermTypes =

    open System

    type Ontology = {
        Name        : string
        Version     : string
        LastUpdated : DateTime
        Author      : string
    } with
        static member create name version lastUpdated authors = {     
            Name         = name          
            Version      = version
            LastUpdated  = lastUpdated   
            Author       = authors        
        }

    type Term = {
        Accession       : string
        Name            : string
        Description     : string
        IsObsolete      : bool
        FK_Ontology     : string
    }

    let createTerm accession name description isObsolete ontologyName= {          
        Accession   = accession    
        Name        = name         
        Description = description   
        IsObsolete  = isObsolete   
        FK_Ontology = ontologyName
    }

    type TermRelationship = {
        TermID          : int64
        Relationship    : string
        RelatedTermID   : int64
    }

    type TermMinimal = {
        /// This is the Ontology Term Name
        Name            : string
        /// This is the Ontology Term Accession 'XX:aaaaaa'
        TermAccession   : string
    } with
        static member create name termAccession = {
            Name            = name
            TermAccession   = termAccession
        }

        static member ofTerm (term:Term) = {
            Name            = term.Name
            TermAccession   = term.Accession
        }

        static member empty = {
            Name            = ""
            TermAccession   = ""
        }

        static member fromOntologyAnnotation (oa: OntologyAnnotation) =
            TermMinimal.create oa.NameText oa.TermAccessionShort

        /// The numberFormat attribute in Excel allows to create automatic unit extensions.
        /// It uses a special input format which is created by this function and should be used for unit terms.
        member this.toNumberFormat = $"0.00 \"{this.Name}\""

        /// This still returns only minimal information, but in term format
        member this.toTerm = createTerm this.TermAccession this.Name "" false ""

        /// The numberFormat attribute in Excel allows to create automatic unit extensions.
        /// The format is created as $"0.00 \"{MinimalTerm.Name}\"", this function is meant to reverse this, altough term accession is lost.
        static member ofNumberFormat (formatStr:string) =
            let unitNameOpt = Regex.parseDoubleQuotes formatStr
            let unitName =
                if unitNameOpt.IsNone then
                    failwith $"Unable to parse given string {formatStr} to TermMinimal.Name in numberFormat."
                else
                    unitNameOpt.Value
            TermMinimal.create unitName ""

        /// Returns empty string if no accession is found
        member this.accessionToTSR =
            if this.TermAccession = "" then
                ""
            else
                try 
                    this.TermAccession.Split(@":").[0]
                with
                    | exn -> ""

        /// Returns empty string if no accession is found
        member this.accessionToTAN =
            if this.TermAccession = "" then
                ""
            else
                URLs.TermAccessionBaseUrl + this.TermAccession.Replace(@":",@"_")

    type TermSearchable = {
        // Contains information about the term to search itself. If term accession is known, search result is 100% correct.
        Term                : TermMinimal
        // If ParentTerm isSome, then the term name is first searched in a is_a directed search
        ParentTerm          : TermMinimal option
        // Is term ist used as unit, unit ontology is searched first.
        IsUnit              : bool
        // ColIndex in table
        ColIndex            : int
        // RowIndex in table
        RowIndices          : int []
        // Search result
        SearchResultTerm    : Term option
    } with
        static member create term parentTerm isUnit colInd rowIndices= {
            Term                = term
            ParentTerm          = parentTerm
            IsUnit              = isUnit
            ColIndex            = colInd
            RowIndices          = rowIndices
            SearchResultTerm    = None
        }

        member this.hasEmptyTerm =
            this.Term.Name = "" && this.Term.TermAccession = ""

module TreeTypes =

    type TreeTerm = {
        NodeId: int64
        Term: TermTypes.Term
    }

    type TreeRelationship = {
        RelationshipId: int64
        StartNodeId: int64
        EndNodeId: int64
        Type: string
    }

    type Tree = {
        Nodes: TreeTerm list
        Relationships: TreeRelationship list
    }