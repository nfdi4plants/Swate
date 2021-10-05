namespace Shared

module OfficeInteropTypes =

    open Shared.TermTypes

    type TryFindAnnoTableResult =
    | Success of string
    | Error of string 

        with
            static member
                /// This function is used on an array of table names (string []). If the length of the array is <> 1 it will trough the correct error.
                /// Only returns success if annoTables.Length = 1. Does not check if the existing table names are correct/okay.
                exactlyOneAnnotationTable (annoTables:string [])=
                    match annoTables.Length with
                    | x when x < 1 ->
                        Error "Could not find annotationTable in active worksheet. Please create one before trying to execute this function."
                    | x when x > 1 ->
                        Error "The active worksheet contains more than one annotationTable. Please move one of them to another worksheet."
                    | 1 ->
                        annoTables |> Array.exactlyOne |> Success
                    | _ ->
                        Error "Could not process message. Swate was not able to identify the given annotation tables with a known case."

    [<RequireQualifiedAccess>]
    type BuildingBlockType =
        | Parameter         
        | Factor            
        | Characteristics
        | Source
        | Sample            
        | Data

        static member listAll = [ Parameter; Factor; Characteristics; Source; Sample; Data ]

        member this.isInputColumn =
            match this with | Source -> true | anythingElse -> false

        member this.isOutputColumn =
            match this with | Data | Sample -> true | anythingElse -> false

        static member ofString str =
            match str with
            | "Parameter"       -> Parameter
            | "Factor"          -> Factor         
            | "Characteristics" -> Characteristics
            | "Sample Name"     -> Sample         
            | "Data File Name"  -> Data           
            | "Source Name"     -> Source
            | anythingElse      -> failwith $"Error: Unable to parse {anythingElse} to BuildingBlockType!"

        static member tryOfString str =
            match str with
            | "Parameter"       -> Some Parameter
            | "Factor"          -> Some Factor         
            | "Characteristics" -> Some Characteristics
            | "Sample Name"     -> Some Sample         
            | "Data File Name"  -> Some Data           
            | "Source Name"     -> Some Source
            | anythingElse      -> None

        member this.toString =
            match this with
            | Parameter         -> "Parameter"
            | Factor            -> "Factor"
            | Characteristics   -> "Characteristics"
            | Sample            -> "Sample Name"
            | Data              -> "Data File Name"
            | Source            -> "Source Name"

        static member toShortExplanation = function
            | Parameter         -> "Use parameter columns to annotate your experimental workflow. multiple parameters form a protocol. Example: centrifugation time, precipitate agent, ..."
            | Factor            -> "Use factor columns to track the experimental conditions that govern your study. Example: temperature,light,..."
            | Characteristics   -> "Use characteristics columns to annotate interesting properties of your organism. Example: strain,phenotype,... "
            | Sample            -> "Use sample columns to mark the name of the sample that your experimental workflow produced."
            | Data              -> "Use data columns to mark the data file name that your computational analysis produced"
            | Source            -> "Attention: you normally dont have to add this manually if you initialize an annotation table. The Source column defines the organism that is subject to your study. It is the first column of every study file."

        static member toLongExplanation = function
            | Parameter         ->
                "Use parameters to annotate your experimental workflow. You can group parameters to create a protocol."
            | Factor            ->
                "Use factor columns to track the experimental conditions that govern your study.
                Most of the time, factors are the most important building blocks for downstream computational analysis."
            | Characteristics   ->
                "Use characteristics columns to annotate interesting properties of the source material.
                You can use any number of characteristics columns."
            | Sample            ->
                "The Sample Name column defines the resulting biological material of the annotated workflow.
                The name used must be a unique identifier.
                Samples can again be sources for further experimental workflows."
            | Data              ->
                "The Data column describes data files that results from your experiments.
                Additionally to the type of data, the annotated files must have a unique name.
                Data files can be sources for computational workflows."
            | Source            ->
                "The Source Name column defines the source of biological material used for your experiments.
                The name used must be a unique identifier. It can be an organism, a sample, or both.
                Every annotation table must start with the Source Name column"

        /// Checks if a string matches one of the single column core names exactly.
        member this.isSingleColumn =
            match this with
            | BuildingBlockType.Sample| BuildingBlockType.Source | BuildingBlockType.Data -> true
            | _ -> false

    type BuildingBlockNamePrePrint = {
        Type : BuildingBlockType
        Name : string
    } with
        static member init (t : BuildingBlockType) = {
            Type = t
            Name = ""
        }

        static member create t name = {
            Type = t
            Name = name
        }

        member this.toAnnotationTableHeader() =
            match this.Type with
            | BuildingBlockType.Parameter         -> sprintf "Parameter [%s]" this.Name
            | BuildingBlockType.Factor            -> sprintf "Factor [%s]" this.Name
            | BuildingBlockType.Characteristics   -> sprintf "Characteristics [%s]" this.Name
            | BuildingBlockType.Sample            -> "Sample Name"
            | BuildingBlockType.Data              -> "Data File Name"
            | BuildingBlockType.Source            -> "Source Name"

        member this.toAnnotationTableHeader(id) =
            match this.Type with
            | BuildingBlockType.Parameter         -> $"Parameter [{this.Name}#{id}]"
            | BuildingBlockType.Factor            -> $"Factor [{this.Name}#{id}]"
            | BuildingBlockType.Characteristics   -> $"Characteristics [{this.Name}#{id}]"
            | BuildingBlockType.Sample            -> "Sample Name"
            | BuildingBlockType.Data              -> "Data File Name"
            | BuildingBlockType.Source            -> "Source Name"

        /// Check if .Type is single column type
        member this.isSingleColumn = this.Type.isSingleColumn
        /// Check if .Type is input column type
        member this.isInputColumn = this.Type.isInputColumn
        /// Check if .Type is output column type
        member this.isOutputColumn = this.Type.isOutputColumn

    type ColumnCoreNames =
        | TermSourceRef
        | TermAccessionNumber
        | Unit

            member this.toString =
                match this with
                | TermSourceRef         -> "Term Source REF"
                | TermAccessionNumber   -> "Term Accession Number"
                | Unit                  -> "Unit"

    /// Used to fill in TSR and TAN if main column contains free-text input
    [<Literal>]
    let FreeTextInput = "user-specific"

    open Shared
    open Regex

    type SwateColumnHeader = {
        SwateColumnHeader: string
    } with
        static member create headerString = { SwateColumnHeader = headerString }
        member this.isMainColumn =
            let isExistingType = BuildingBlockType.listAll |> List.tryFind (fun t -> this.SwateColumnHeader.StartsWith t.toString)
            match isExistingType with
            | Some t    -> true
            | None      -> false
        member this.isSingleCol =
            if this.isMainColumn then
                let bbType = parseCoreName this.SwateColumnHeader
                match bbType with
                | Some t    -> BuildingBlockType.ofString (t.Trim()) |> fun x -> x.isSingleColumn
                | None      -> failwith $"Cannot get ColumnCoreName from {this.SwateColumnHeader}"
            else
                false
        member this.isUnitCol =
            this.SwateColumnHeader.StartsWith ColumnCoreNames.Unit.toString
        member this.isTANCol =
            this.SwateColumnHeader.StartsWith ColumnCoreNames.TermAccessionNumber.toString
        member this.isTSRCol =
            this.SwateColumnHeader.StartsWith ColumnCoreNames.TermSourceRef.toString
        member this.isReference =
            this.isTSRCol
            || this.isTANCol
            || this.isUnitCol
        member this.getColumnCoreName = parseCoreName this.SwateColumnHeader
        member this.toBuildingBlockNamePrePrint =
            match this.getColumnCoreName, this.tryGetOntologyTerm with
            | Some swatecore, None ->
                let t = BuildingBlockType.tryOfString swatecore
                if t.IsSome then BuildingBlockNamePrePrint.create t.Value "" |> Some else None
            | Some swatecore, Some term ->
                let t = BuildingBlockType.tryOfString swatecore
                if t.IsSome then BuildingBlockNamePrePrint.create t.Value term |> Some else None
            | None, _ -> None
        member this.isSwateColumnHeader =         
            match this with
            | isMainCol when isMainCol.isMainColumn -> true
            | isRefCol when isRefCol.isReference    -> true
            | anythingelse                          -> false
        /// Use this function to extract ontology term name from inside square brackets in the main column header
        member this.tryGetOntologyTerm =
            let sqBrackets = parseSquaredBrackets this.SwateColumnHeader
            match sqBrackets with
            | Some str -> removeId str |> Some
            | None -> None
        /// Get term Accession in TSR or TAN from column header
        member this.tryGetTermAccession =
            let brackets = parseBrackets this.SwateColumnHeader
            match brackets with
            | Some str ->
                // this step is optional as "parseTermAccession should be able to get term accession even with appended #id"
                removeId str
                |> parseTermAccession
            | None -> None
        /// Get column header hash id from main column. E.g. Parameter [Instrument Model#2]
        member this.tryGetMainColumnHeaderId =
            let brackets = parseSquaredBrackets this.SwateColumnHeader
            match brackets with
            | Some str  -> getId str
            | None      -> None
        /// Get column header hash id from main column. E.g. Term Source REF (MS:1000031#2)
        member this.tryGetRefColumnHeaderId =
            let brackets = parseBrackets this.SwateColumnHeader
            match brackets with
            | Some str  -> getId str
            | None      -> None


    /// This module contains types to handle value search for TSR and TAN columns.
    /// The types help to summarize and collect needed information about the column partitions (~ building block, e.g. 1 col for `Source Name`,
    /// 3 cols for standard `Parameter`, 6 cols for `Parameter` with unit). As excel allows to drag 'n drop values down for a column we need these types
    /// to find such occurrences and fill in the missing TSR, TAN and unit cols.
    module BuildingBlockTypes =

        type Cell = {
            Index: int
            Value: string option
            Unit: TermMinimal option
        } with
            static member create ind value unit= {
                Index = ind
                Value = value
                Unit = unit
            }

        type Column = {
            Index: int
            Header: SwateColumnHeader
            Cells: Cell []
        } with
            static member create ind headerOpt cellsArr = {
                Index   =  ind
                Header  = headerOpt
                Cells   = cellsArr
            } 

        type BuildingBlock = {
            MainColumn      : Column
            MainColumnTerm  : TermMinimal option
            Unit            : Column option
            /// Term Source REF
            TSR             : Column option
            /// Term Accession Number
            TAN             : Column option
        } with
            static member create mainCol tsr tan unit mainColTerm = {
                MainColumn      = mainCol
                MainColumnTerm  = mainColTerm
                TSR             = tsr
                TAN             = tan
                Unit            = unit
            }

            member this.hasCompleteTSRTAN =
                match this.TAN, this.TSR with
                | Some tan, Some tsr ->
                    true
                | None, None ->
                    false
                | _, _ ->
                    failwith (sprintf "Swate found unknown building block pattern in building block %s. Found only TSR or TAN." this.MainColumn.Header.SwateColumnHeader)

            member this.hasUnit = this.Unit.IsSome
            member this.hasTerm = this.MainColumnTerm.IsSome
            member this.hasCompleteTerm = this.MainColumnTerm.IsSome && this.MainColumnTerm.Value.Name <> "" && this.MainColumnTerm.Value.TermAccession <> ""

        type InsertBuildingBlock = {
            Column      : BuildingBlockNamePrePrint
            ColumnTerm  : TermMinimal option
            UnitTerm    : TermMinimal option 
        } with
            static member create column columnTerm unitTerm= {
                Column      = column
                ColumnTerm  = columnTerm
                UnitTerm    = unitTerm
            }

            member this.HasUnit = this.UnitTerm.IsSome
            member this.HasExistingTerm = this.ColumnTerm.IsSome
            member this.HasCompleteTerm = this.ColumnTerm.IsSome && this.ColumnTerm.Value.Name <> "" && this.ColumnTerm.Value.TermAccession <> ""