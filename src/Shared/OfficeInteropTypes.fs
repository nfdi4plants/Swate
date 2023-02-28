namespace Shared

open System

module OfficeInteropTypes =

    open Shared.TermTypes

    type TryFindAnnoTableResult =
    | Success of string
    | Error of string 
        with
        ///<summary>This function is used on an array of table names (string []). If the length of the array is <> 1 it will trough the correct error.
        /// Only returns success if annoTables.Length = 1. Does not check if the existing table names are correct/okay.</summary>
        static member exactlyOneAnnotationTable (annoTables:string [])=
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
        // Term columns
        | Parameter         
        | Factor            
        | Characteristic
        | Component
        // Source columns
        | Source
        // Output columns
        | Sample
        | Data // DEPRECATED at v0.6.0 [<ObsoleteAttribute>] 
        | RawDataFile
        | DerivedDataFile
        // Featured Columns
        | ProtocolType
        // Single Columns
        | ProtocolREF
        // everything else
        | Freetext of string

        static member All = [
            Parameter; Factor; Characteristic; Component
            //input
            Source;
            //output
            Sample; RawDataFile; DerivedDataFile;
            Data // deprecated
            // Featured
            ProtocolType
            // Single
            ProtocolREF
        ]

        member this.isInputColumn =
            match this with | Source -> true | anythingElse -> false

        member this.isOutputColumn =
            match this with | Data | Sample | RawDataFile | DerivedDataFile -> true | anythingElse -> false

        ///<summary>The name "TermColumn" refers to all columns with the syntax "Parameter/Factor/etc [TERM-NAME]"</summary>
        member this.isTermColumn =
            match this with | Parameter | Factor | Characteristic | Component | ProtocolType -> true | anythingElse -> false

        static member TermColumns = BuildingBlockType.All |> List.filter (fun x -> x.isTermColumn)
        static member InputColumns = BuildingBlockType.All |> List.filter (fun x -> x.isInputColumn)
        static member OutputColumns = BuildingBlockType.All |> List.filter (fun x -> x.isOutputColumn)

        /// <summary>This function returns true if the BuildingBlockType is a featured column. A featured column can
        /// be abstracted by Parameter/Factor/Characteristics and describes one common usecase of either.
        /// Such a block will contain TSR and TAN and can be used for directed Term search.</summary>
        member this.isFeaturedColumn =
            match this with | ProtocolType -> true | anythingElse -> false

        member this.getFeaturedColumnAccession =
            if this.isFeaturedColumn then
                match this with
                | ProtocolType -> "DPBO:1000161"
                | _ -> failwith "This cannot happen"
            else
                failwith $"'{this}' is not listed as featured column type! No referenced accession available."

        member this.getFeaturedColumnTermMinimal =
            if this.isFeaturedColumn then
                match this with
                | ProtocolType -> TermMinimal.create "protocol type" this.getFeaturedColumnAccession
                | _ -> failwith "This cannot happen"
            else
                failwith $"'{this}' is not listed as featured column type! No referenced accession available."

        /// Checks if a string matches one of the single column core names exactly.
        member this.isSingleColumn =
            match this with
            // Input & Output columns
            | BuildingBlockType.Sample| BuildingBlockType.Source | BuildingBlockType.Data | BuildingBlockType.RawDataFile | BuildingBlockType.DerivedDataFile | BuildingBlockType.ProtocolREF | Freetext _ -> true
            | _ -> false

        static member tryOfString str =
            match str with
            | "Parameter" | "Parameter Value"   -> Some Parameter
            | "Factor" | "Factor Value"         -> Some Factor
            // "Characteristics" deprecated in v0.6.0
            | "Characteristics" | "Characteristic" | "Characteristics Value" -> Some Characteristic
            | "Component" -> Some Component
            | "Sample Name"     -> Some Sample         
            | "Data File Name"  -> Some Data
            | "Raw Data File"       -> Some RawDataFile
            | "Derived Data File"   -> Some DerivedDataFile
            | "Source Name"     -> Some Source
            | "Protocol Type"   -> Some ProtocolType
            | "Protocol REF"    -> Some ProtocolREF
            | anythingElse      -> Some <| Freetext anythingElse

        static member ofString str =
            BuildingBlockType.tryOfString str
            |> function Some bbt -> bbt | None -> failwith $"Error: Unable to parse '{str}' to BuildingBlockType!"

        member this.toString =
            match this with
            | Parameter         -> "Parameter"
            | Factor            -> "Factor"
            | Characteristic    -> "Characteristic"
            | Component         -> "Component"
            | Sample            -> "Sample Name"
            | Data              -> "Data File Name"
            | RawDataFile       -> "Raw Data File"
            | DerivedDataFile   -> "Derived Data File"
            | ProtocolType      -> "Protocol Type" 
            | Source            -> "Source Name"
            | ProtocolREF       -> "Protocol REF"
            | Freetext str      -> str

        /// By Martin Kuhl 04.08.2022, https://github.com/Martin-Kuhl
        member this.toShortExplanation =
            match this with
            | Parameter         -> "Parameter columns describe steps in your experimental workflow, e.g. the centrifugation time or the temperature used for your assay. Multiple Parameter columns form a protocol."
            | Factor            -> "Use Factor columns to describe independent variables that result in a specific output of your experiment, e.g. the light intensity under which an organism was grown."
            | Characteristic    -> "Characteristics columns are used for study descriptions and describe inherent properties of the source material, e.g. a certain strain or the temperature the organism was exposed to. "
            | Component         -> "Use these columns to list the components of a protocol, e.g. instrument names, software names, and reagents names."
            | Sample            -> "The Sample Name column defines the resulting biological material and thereby, the output of the annotated workflow. The value must be a unique identifier."
            | Data              -> "DEPRECATED: Use data columns to mark the data file name that your computational analysis produced."
            | RawDataFile       -> "The Raw Data File column defines untransformed and unprocessed data files"
            | DerivedDataFile   -> "The Derived Data File column defines transformed and/or processed data files"
            | Source            -> "The Source column efines the input of your table. This input value must be a unique identifier for an organism or a sample. The number of Source Name columns per table is limited to one."
            | ProtocolType      -> "Defines the protocol type according to your preferred endpoint repository."
            | ProtocolREF       -> "Defines the protocol name."
            | Freetext _        -> failwith "Freetext BuildingBlockType should not be parsed"

        /// By Martin Kuhl 04.08.2022, https://github.com/Martin-Kuhl
        member this.toLongExplanation =
            match this with
            | Parameter         ->
                "Parameter columns describe steps in your experimental workflow, e.g. the centrifugation time or the temperature used for your assay.
                Multiple Parameter columns form a protocol.There is no limitation for the number of Parameter columns per table."
            | Factor            ->
                "Use Factor columns to describe independent variables that result in a specific output of your experiment, 
                e.g. the light intensity under which an organism was grown. Factor columns are very important building blocks for your downstream computational analysis.
                The combination of a container ontology (Characteristics, Parameter, Factor) and a biological or technological ontology (e.g. temperature, light intensity) gives
                the flexibility to display a term as a regular process parameter or as the factor your study is based on (Parameter [temperature] or Factor [temperature])."
            | Characteristic   ->
                "Characteristics columns are used for study descriptions and describe inherent properties of the source material, e.g. a certain strain or ecotype, but also the temperature an organism was exposed to.
                There is no limitation for the number of Characteristics columns per table.  "
            | Component   ->
                "Use these columns to list the components of a protocol, e.g. instrument names, software names, and reagents names."
            | Sample            ->
                "The Sample Name column defines the resulting biological material and thereby, the output of the annotated workflow. The value must be a unique identifier. The output of a table (Sample Name, Raw Data File, Derived Data File) can be used again as Source Name of a new table to illustrate an entire experimental workflow. The number of Output columns per table is limited to one."
            | Data              ->
                "DEPRECATED: The Data column describes data files that results from your experiments.
                Additionally to the type of data, the annotated files must have a unique name.
                Data files can be sources for computational workflows."
            | RawDataFile       -> 
                "Use Raw Data File columns to define untransformed and unprocessed data files. The output of a table
                (Sample Name, Raw Data File, Derived Data File) can be used again as Source Name of a new table
                to illustrate an entire experimental workflow. The number of Output columns per table is limited to one."
            | DerivedDataFile   ->
                "Use Derived Data File columns to define transformed and/or processed data files. The output of a table
                (Sample Name, Raw Data File, Derived Data File) can be used again as Source Name of a new table to illustrate an
                entire experimental workflow. The number of Output columns per table is limited to one"
            | Source            ->
                "The Source Name column  defines the input of your table. This input value must be a unique identifier for an organism or a sample.
                The number of Source Name columns per table is limited to one. Usually, you don’t have to add this column as it is automatically
                generated when you add a table to the worksheet. The output of a previous table can be used as Source Name of a new one to illustrate an entire workflow."
            | ProtocolType      ->
                "Use this column type to define the protocol type according to your preferred endpoint repository.
                You can use the term search, to search through all available protocol types."
            | ProtocolREF       ->
                "Use this column type to define your protocol name. Normally the Excel worksheet name is used, but it is limited to ~32 characters."
            | Freetext _        -> failwith "Freetext BuildingBlockType should not be parsed"

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
            | BuildingBlockType.Parameter           -> sprintf "Parameter [%s]" this.Name
            | BuildingBlockType.Factor              -> sprintf "Factor [%s]" this.Name
            | BuildingBlockType.Characteristic      -> sprintf "Characteristic [%s]" this.Name
            | BuildingBlockType.Component           -> sprintf "Component [%s]" this.Name
            | BuildingBlockType.Sample              -> BuildingBlockType.Sample.toString
            | BuildingBlockType.Data                -> BuildingBlockType.Data.toString
            | BuildingBlockType.RawDataFile         -> BuildingBlockType.RawDataFile.toString
            | BuildingBlockType.DerivedDataFile     -> BuildingBlockType.DerivedDataFile.toString
            | BuildingBlockType.Source              -> BuildingBlockType.Source.toString
            | BuildingBlockType.ProtocolType        -> BuildingBlockType.ProtocolType.toString
            | BuildingBlockType.ProtocolREF         -> BuildingBlockType.ProtocolREF.toString
            | BuildingBlockType.Freetext str        -> (BuildingBlockType.Freetext str).toString

        /// Check if .Type is single column type
        member this.isSingleColumn = this.Type.isSingleColumn
        /// Check if .Type is input column type
        member this.isInputColumn = this.Type.isInputColumn
        /// Check if .Type is output column type
        member this.isOutputColumn = this.Type.isOutputColumn
        /// Check if .Type is featured column type
        member this.isFeaturedColumn = this.Type.isFeaturedColumn
        /// Check if .Type is term column type
        member this.isTermColumn = this.Type.isTermColumn

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
        /// This field was added exclusively for the use in Swates own spreadsheet functionality. It is not used in ExcelInterop.
        Term: TermMinimal option
        HasUnit: bool
    } with
        static member create headerString = { SwateColumnHeader = headerString; Term = None; HasUnit = false }
        static member init(headerString:string, ?term: TermMinimal, ?hasUnit: bool) = { SwateColumnHeader = headerString; Term = term; HasUnit = Option.defaultValue false hasUnit}
        static member init(headerString:SwateColumnHeader, ?term: TermMinimal, ?hasUnit: bool) = { SwateColumnHeader = headerString.SwateColumnHeader; Term = term; HasUnit = Option.defaultValue false hasUnit }
        member this.isMainColumn =
            let isExistingType = BuildingBlockType.All |> List.tryFind (fun t -> this.SwateColumnHeader.StartsWith t.toString)
            match isExistingType with
            | Some t    -> true
            | None      -> false
        member this.isSingleCol =
            if this.isMainColumn then
                let bbType = this.getColumnCoreName (*parseCoreName this.SwateColumnHeader *)
                match bbType with
                | Some (t: string)  -> BuildingBlockType.ofString t |> fun x -> x.isSingleColumn
                | None      -> failwith $"Cannot get ColumnCoreName from {this.SwateColumnHeader}"
            else
                false
        member this.isOutputCol = 
            if this.isMainColumn then
                let bbType = this.getColumnCoreName //parseCoreName this.SwateColumnHeader
                match bbType with
                | Some t    -> BuildingBlockType.ofString t |> fun x -> x.isOutputColumn
                | None      -> failwith $"Cannot get ColumnCoreName from {this.SwateColumnHeader}"
            else
                false
        member this.isInputCol = 
            if this.isMainColumn then
                let bbType = this.getColumnCoreName // parseCoreName this.SwateColumnHeader
                match bbType with
                | Some t    -> BuildingBlockType.ofString t |> fun x -> x.isInputColumn
                | None      -> failwith $"Cannot get ColumnCoreName from {this.SwateColumnHeader}"
            else
                false
        /// <summary>This function returns true if the SwateColumnHeader can be parsed to a featured column. A featured column can
        /// be abstracted by Parameter/Factor/Characteristics and describes one common usecase of either.
        /// Such a block will contain TSR and TAN and can be used for directed Term search.</summary>
        member this.isFeaturedCol =
            if this.isMainColumn then
                let bbType = this.getColumnCoreName //parseCoreName this.SwateColumnHeader
                match bbType with
                | Some t    -> BuildingBlockType.ofString t |> fun x -> x.isFeaturedColumn
                | None      -> failwith $"Cannot get ColumnCoreName from {this.SwateColumnHeader}"
            else
                false
        /// <summary>The name "TermColumn" refers to all columns with the syntax "Parameter/Factor/etc [TERM-NAME]"</summary>
        member this.isTermColumn =
            if this.isMainColumn then
                let bbType = this.getColumnCoreName //parseCoreName this.SwateColumnHeader
                match bbType with
                | Some t    -> BuildingBlockType.ofString t |> fun x -> x.isTermColumn
                | None      -> failwith $"Cannot get ColumnCoreName from {this.SwateColumnHeader}"
            else
                false
        member this.isUnitCol =
            this.SwateColumnHeader.StartsWith ColumnCoreNames.Unit.toString
        member this.isTANCol =
            this.SwateColumnHeader.StartsWith ColumnCoreNames.TermAccessionNumber.toString
        member this.isTSRCol =
            this.SwateColumnHeader.StartsWith ColumnCoreNames.TermSourceRef.toString
        /// TSR, TAN or Unit
        member this.isReference =
            this.isTSRCol
            || this.isTANCol
            || this.isUnitCol
        member this.getColumnCoreName = parseCoreName this.SwateColumnHeader |> Option.map (fun x -> x.Trim())
        member this.toBuildingBlockNamePrePrint =
            match this.getColumnCoreName, this.tryGetOntologyTerm with
            | Some swatecore, None ->
                let t = BuildingBlockType.tryOfString swatecore
                if t.IsSome then BuildingBlockNamePrePrint.create t.Value "" |> Some
                else None
            | Some swatecore, Some term ->
                let t = BuildingBlockType.tryOfString swatecore
                if t.IsSome then
                    let tv = BuildingBlockNamePrePrint.create t.Value term
                    BuildingBlockNamePrePrint.create t.Value term |> Some
                else None
            | None, _ -> None
        /// <summary>This member returns true if the header is either a main column header ("Source Name", "Protocol Type", "Parameter [xxxx]")
        /// or a reference column ("TSR", "TAN", "Unit").</summary>
        member this.isSwateColumnHeader =
            match this with
            | isMainCol when isMainCol.isMainColumn -> true
            | isRefCol when isRefCol.isReference    -> true
            | anythingelse                          -> false
        /// <summary>Use this function to extract ontology term name from inside square brackets in the main column header</summary>
        member this.tryGetOntologyTerm = parseSquaredTermNameBrackets this.SwateColumnHeader
        /// <summary>Get term Accession in TSR or TAN from column header</summary>
        member this.tryGetTermAccession = parseTermAccession this.SwateColumnHeader
        /// <summary>Get column header hash id from main column. E.g. Parameter [Instrument Model#2]</summary>
        [<Obsolete("Swate no longer uses #id pattern, instead expands column name with whitespaces. As this is less invasive.")>]
        member this.tryGetHeaderId =
            let brackets = parseSquaredTermNameBrackets this.SwateColumnHeader
            match brackets with
            | Some str  -> getId str |> Option.bind (fun x -> "#" + x |> Some)
            | None      -> None
        member this.getFeaturedColAccession =
            let bbType = this.getColumnCoreName 
            match bbType with
            | Some t    -> BuildingBlockType.ofString t |> fun x -> x.getFeaturedColumnAccession
            | None      -> failwith $"Cannot get ColumnCoreName from {this.SwateColumnHeader}"
        member this.getFeaturedColTermMinimal =
            let bbType = this.getColumnCoreName 
            match bbType with
            | Some t    -> BuildingBlockType.ofString t |> fun x -> x.getFeaturedColumnTermMinimal
            | None      -> failwith $"Cannot get ColumnCoreName from {this.SwateColumnHeader}"

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
        static member init(index:int, ?v: string, ?unit: TermMinimal) = {
            Index = index
            Value = v
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
        ColumnHeader    : BuildingBlockNamePrePrint
        ColumnTerm      : TermMinimal option
        UnitTerm        : TermMinimal option
        Rows            : TermMinimal []
    } with
        static member create header columnTerm unitTerm rows = {
            ColumnHeader    = header
            ColumnTerm      = columnTerm
            UnitTerm        = unitTerm
            Rows            = rows
        }

        member this.HasUnit         = this.UnitTerm.IsSome
        member this.HasExistingTerm = this.ColumnTerm.IsSome
        member this.HasCompleteTerm = this.ColumnTerm.IsSome && this.ColumnTerm.Value.Name <> "" && this.ColumnTerm.Value.TermAccession <> ""
        member this.HasValues       = this.Rows <> Array.empty
