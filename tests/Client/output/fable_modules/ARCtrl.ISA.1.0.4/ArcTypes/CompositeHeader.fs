namespace ARCtrl.ISA

open Fable.Core
open Fable.Core.JsInterop

[<AttachMembers>]
[<RequireQualifiedAccess>]
type IOType =
    | Source
    | Sample
    | RawDataFile
    | DerivedDataFile
    | ImageFile
    | Material
    | FreeText of string

    // This is used for example in Swate to programmatically create Options for adding building blocks.
    // Having this member here, guarantees the any new IOTypes are implemented in tools.
    /// This member is used to iterate over all existing `IOType`s (excluding FreeText case).
    static member All = [|
        Source
        Sample
        RawDataFile
        DerivedDataFile
        ImageFile
        Material
    |]

    static member Cases = 
        Microsoft.FSharp.Reflection.FSharpType.GetUnionCases(typeof<IOType>) 
        |> Array.map (fun x -> x.Tag, x.Name)

    member this.asInput = 
        let stringCreate x = $"Input [{x.ToString()}]"
        match this with
        | FreeText s -> stringCreate s
        | anyelse -> stringCreate anyelse
    member this.asOutput = 
        let stringCreate x = $"Output [{x.ToString()}]"
        match this with
        | FreeText s -> stringCreate s
        | anyelse -> stringCreate anyelse

    /// Given two IOTypes, tries to return the one with a higher specificity. If both are equally specific, fail.
    ///
    /// E.g. RawDataFile is more specific than Source, but less specific than DerivedDataFile.
    ///
    /// E.g. Sample is equally specific to RawDataFile.
    member this.Merge(other) = 
        match this, other with
        | FreeText s1, FreeText s2 when s1 = s2 -> FreeText (s1)
        | FreeText s1, FreeText s2 -> failwith $"FreeText IO column names {s1} and {s2} do differ"
        | FreeText s, _ -> failwith $"FreeText IO column and {other} can not be merged"
        | ImageFile, Source -> ImageFile
        | ImageFile, RawDataFile -> ImageFile
        | ImageFile, DerivedDataFile -> ImageFile
        | ImageFile, ImageFile -> ImageFile
        | ImageFile, _ -> failwith $"ImageFile IO column and {other} can not be merged"
        | DerivedDataFile, Source -> DerivedDataFile
        | DerivedDataFile, RawDataFile -> DerivedDataFile
        | DerivedDataFile, DerivedDataFile -> DerivedDataFile
        | DerivedDataFile, ImageFile -> ImageFile
        | DerivedDataFile, _ -> failwith $"DerivedDataFile IO column and {other} can not be merged"
        | RawDataFile, Source -> RawDataFile
        | RawDataFile, RawDataFile -> RawDataFile
        | RawDataFile, DerivedDataFile -> DerivedDataFile
        | RawDataFile, ImageFile -> ImageFile
        | RawDataFile, _ -> failwith $"RawDataFile IO column and {other} can not be merged"
        | Sample, Source -> Sample
        | Sample, Sample -> Sample
        | Sample, _ -> failwith $"Sample IO column and {other} can not be merged"
        | Source, Source -> Source
        | Source, _ -> other
        | Material, Source -> Material
        | Material, Material -> Material
        | Material, _ -> failwith $"Material IO column and {other} can not be merged"
        

    override this.ToString() =
        match this with
        | Source            -> "Source Name" 
        | Sample            -> "Sample Name" 
        | RawDataFile       -> "Raw Data File"
        | DerivedDataFile   -> "Derived Data File"
        | ImageFile         -> "Image File"
        | Material          -> "Material" 
        | FreeText s        -> s

    /// Used to match only(!) IOType string to IOType (without Input/Output). This matching is case sensitive.
    ///
    /// Exmp. 1: "Source" --> Source
    ///
    /// Exmp. 2: "Raw Data File" | "RawDataFile" -> RawDataFile
    static member ofString (str: string) =  
        match str with
        | "Source" | "Source Name"                  -> Source
        | "Sample" | "Sample Name"                  -> Sample
        | "RawDataFile" | "Raw Data File"           -> RawDataFile
        | "DerivedDataFile" | "Derived Data File"   -> DerivedDataFile
        | "ImageFile" | "Image File"                -> ImageFile
        | "Material"                                -> Material
        | _                                         -> FreeText str // use str to not store `str.ToLower()`

    /// Used to match Input/Output annotation table header to IOType
    ///
    /// Exmp. 1: "Input [Source]" --> Some Source
    static member tryOfHeaderString (str: string) =
        match Regex.tryParseIOTypeHeader str with
        | Some s -> IOType.ofString s |> Some
        | None -> None

    member this.GetUITooltip() = IOType.getUITooltip(U2.Case1 this)

    static member getUITooltip(iotype: U2<IOType,string>) =
        match iotype with
        | U2.Case1 Source | U2.Case2 "Source" -> 
            "The source value must be a unique identifier for an organism or a sample." 
        | U2.Case1 Sample | U2.Case2 "Sample" -> 
            "The Sample Name column describes specifc laboratory samples with a unique identifier." 
        | U2.Case1 RawDataFile | U2.Case2 "RawDataFile" -> 
            "The Raw Data File column defines untransformed and unprocessed data files."
        | U2.Case1 DerivedDataFile | U2.Case2 "DerivedDataFile" -> 
            "The Derived Data File column defines transformed and/or processed data files."
        | U2.Case1 ImageFile | U2.Case2 "ImageFile" -> 
            "Placeholder"
        | U2.Case1 Material | U2.Case2 "Material" -> 
            "Placeholder" 
        | U2.Case1 (FreeText _) | U2.Case2 "FreeText" -> 
            "Placeholder"
        | _ -> failwith $"Unable to parse combination to existing IOType: `{iotype}`"



#if FABLE_COMPILER

    //[<CompiledName("Source")>]
    static member source() = IOType.Source

    //[<CompiledName("Sample")>]
    static member sample() = IOType.Sample

    //[<CompiledName("RawDataFile")>]
    static member rawDataFile() = IOType.RawDataFile

    //[<CompiledName("DerivedDataFile")>]
    static member derivedDataFile() = IOType.DerivedDataFile

    //[<CompiledName("ImageFile")>]
    static member imageFile() = IOType.ImageFile

    //[<CompiledName("Material")>]
    static member material() = IOType.Material

    //[<CompiledName("FreeText")>]
    static member freeText(s:string) = IOType.FreeText s

#else
#endif

/// <summary>
/// Model of the different types of Building Blocks in an ARC Annotation Table.
/// </summary>
[<AttachMembers>]
[<RequireQualifiedAccess>]
type CompositeHeader = 
    // term
    | Component         of OntologyAnnotation
    | Characteristic    of OntologyAnnotation
    | Factor            of OntologyAnnotation
    | Parameter         of OntologyAnnotation
    // featured
    | ProtocolType
    // single
    | ProtocolDescription
    | ProtocolUri
    | ProtocolVersion
    | ProtocolREF
    | Performer
    | Date
    // single - io type
    | Input of IOType
    | Output of IOType
    // single - fallback
    | FreeText of string

    with 

    static member Cases = 
        Microsoft.FSharp.Reflection.FSharpType.GetUnionCases(typeof<CompositeHeader>) 
        |> Array.map (fun x -> x.Tag, x.Name)

    /// <summary>
    /// This function is used to programmatically create `CompositeHeaders` in JavaScript. Returns integer code representative of input type.
    ///
    /// 0: Expects no input
    ///
    /// 1: Expects OntologyAnnotation as input
    ///
    /// 2: Expects IOType as input
    ///
    /// 3: Expects string as input
    /// </summary>
    /// <param name="inp">Can be accessed from `CompositeHeader.Cases`</param>
    static member jsGetColumnMetaType(inp:int) =
        match inp with
        // no input
        | 4 | 5 | 6 | 7 | 8 | 9 | 10 -> 0
        // OntologyAnnotation as input
        | 0 | 1 | 2 | 3 -> 1
        // iotype as input
        | 11 | 12 -> 2
        // string as input
        | 13 -> 3
        | anyElse -> failwithf "Cannot assign input `Tag` (%i) to `CompositeHeader`" anyElse

    override this.ToString() =
        match this with
        | Parameter oa          -> $"Parameter [{oa.NameText}]"
        | Factor oa             -> $"Factor [{oa.NameText}]"
        | Characteristic oa     -> $"Characteristic [{oa.NameText}]"
        | Component oa          -> $"Component [{oa.NameText}]"
        | ProtocolType          -> "Protocol Type" 
        | ProtocolREF           -> "Protocol REF"
        | ProtocolDescription   -> "Protocol Description"
        | ProtocolUri           -> "Protocol Uri"
        | ProtocolVersion       -> "Protocol Version"
        | Performer             -> "Performer"
        | Date                  -> "Date"
        | Input io              -> io.asInput
        | Output io             -> io.asOutput
        | FreeText str          -> str

    /// If the column is a term column, returns the term as `OntologyAnnotation`. Otherwise returns an `OntologyAnnotation` with only the name.
    member this.ToTerm() =
        match this with
        | Parameter oa          -> oa
        | Factor oa             -> oa
        | Characteristic oa     -> oa
        | Component oa          -> oa
        | ProtocolType          -> OntologyAnnotation.fromString(this.ToString(), tan=this.GetFeaturedColumnAccession) 
        | ProtocolREF           -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        | ProtocolDescription   -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        | ProtocolUri           -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        | ProtocolVersion       -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        | Performer             -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        | Date                  -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        | Input _               -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        | Output _              -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        | FreeText _            -> OntologyAnnotation.fromString (this.ToString())  // use owl ontology in the future
        // owl ontology: https://github.com/nfdi4plants/ARC_ontology/blob/main/ARC_v2.0.owl

    /// <summary>
    /// Tries to create a `CompositeHeader` from a given string.
    /// </summary>
    static member OfHeaderString (str: string) =
        match str.Trim() with
        // Input/Output have similiar naming as Term, but are more specific. 
        // So they have to be called first.
        | Regex.ActivePatterns.Regex Regex.Pattern.InputPattern r ->
            let iotype = r.Groups.[Regex.Pattern.MatchGroups.iotype].Value
            Input <| IOType.ofString (iotype)
        | Regex.ActivePatterns.Regex Regex.Pattern.OutputPattern r ->
            let iotype = r.Groups.[Regex.Pattern.MatchGroups.iotype].Value
            Output <| IOType.ofString (iotype)
        // Is term column
        | Regex.ActivePatterns.TermColumn r ->
            match r.TermColumnType with
            | "Parameter" 
            | "Parameter Value"             -> Parameter (OntologyAnnotation.fromString r.TermName)
            | "Factor" 
            | "Factor Value"                -> Factor (OntologyAnnotation.fromString r.TermName)
            | "Characteristic" 
            | "Characteristics"
            | "Characteristics Value"       -> Characteristic (OntologyAnnotation.fromString r.TermName)
            | "Component"                   -> Component (OntologyAnnotation.fromString r.TermName)
            // TODO: Is this what we intend?
            | _                             -> FreeText str
        | "Date"                    -> Date
        | "Performer"               -> Performer
        | "Protocol Description"    -> ProtocolDescription
        | "Protocol Uri"            -> ProtocolUri
        | "Protocol Version"        -> ProtocolVersion
        | "Protocol Type"           -> ProtocolType
        | "Protocol REF"            -> ProtocolREF
        | anyelse                   -> FreeText anyelse


    /// Returns true if column is deprecated
    member this.IsDeprecated = 
        match this with 
        | FreeText s when s.ToLower() = "sample name" -> true
        | FreeText s when s.ToLower() = "source name" -> true
        | FreeText s when s.ToLower() = "data file name" -> true
        | FreeText s when s.ToLower() = "derived data file" -> true
        | _ -> false   

    /// <summary>
    /// Is true if this Building Block type is a CvParamColumn.
    ///
    /// The name "CvParamColumn" refers to all columns with the syntax "Parameter/Factor/etc [TERM-NAME]".
    ///
    /// Does return false for featured columns such as Protocol Type.
    /// </summary>
    member this.IsCvParamColumn =
        match this with 
        | Parameter _ | Factor _| Characteristic _| Component _ -> true
        | anythingElse -> false

    /// <summary>
    /// Is true if this Building Block type is a TermColumn.
    ///
    /// The name "TermColumn" refers to all columns with the syntax "Parameter/Factor/etc [TERM-NAME]" and featured columns
    /// such as Protocol Type as these are also represented as a triplet of MainColumn-TSR-TAN.
    /// </summary>
    member this.IsTermColumn =
        match this with 
        | Parameter _ | Factor _| Characteristic _| Component _
        | ProtocolType -> true 
        | anythingElse -> false

    /// <summary>
    /// Is true if the Building Block type is a FeaturedColumn. 
    ///
    /// A FeaturedColumn can be abstracted by Parameter/Factor/Characteristic and describes one common usecase of either.
    /// Such a block will contain TSR and TAN and can be used for directed Term search.
    /// </summary>
    member this.IsFeaturedColumn =
        match this with | ProtocolType -> true | anythingElse -> false

    /// <summary>
    /// This function gets the associated term accession for featured columns. 
    /// 
    /// It contains the hardcoded term accessions.
    /// </summary>
    member this.GetFeaturedColumnAccession =
        match this with
        | ProtocolType -> "DPBO:1000161"
        | anyelse -> failwith $"Tried matching {anyelse} in getFeaturedColumnAccession, but is not a featured column."

    /// <summary>
    /// This function gets the associated term accession for term columns. 
    /// </summary>
    member this.GetColumnAccessionShort =
        match this with
        | ProtocolType -> this.GetFeaturedColumnAccession
        | Parameter oa -> oa.TermAccessionShort
        | Factor oa -> oa.TermAccessionShort
        | Characteristic oa -> oa.TermAccessionShort
        | Component oa -> oa.TermAccessionShort
        | anyelse -> failwith $"Tried matching {anyelse}, but is not a column with an accession."

    /// <summary>
    /// Is true if the Building Block type is parsed to a single column. 
    ///
    /// This can be any input, output column, as well as for example: `ProtocolREF` and `Performer` with FreeText body cells.
    /// </summary>
    member this.IsSingleColumn =
        match this with 
        | FreeText _
        | Input _ | Output _ 
        | ProtocolREF | ProtocolDescription | ProtocolUri | ProtocolVersion | Performer | Date -> true 
        | anythingElse -> false

    ///
    member this.IsIOType =
        match this with 
        | Input io | Output io -> true 
        | anythingElse -> false

    // lower case "i" because of clashing naming: 
    // Issue: https://github.com/dotnet/fsharp/issues/10359
    // Proposed design: https://github.com/fsharp/fslang-design/blob/main/RFCs/FS-1079-union-properties-visible.md
    member this.isInput =
        match this with 
        | Input io -> true 
        | anythingElse -> false

    member this.isOutput =
        match this with 
        | Output io -> true 
        | anythingElse -> false

    member this.isParameter =
        match this with 
        | Parameter _ -> true 
        | anythingElse -> false

    member this.isFactor =
        match this with 
        | Factor _ -> true 
        | anythingElse -> false

    member this.isCharacteristic =
        match this with 
        | Characteristic _ -> true 
        | anythingElse -> false

    member this.isComponent =
        match this with
        | Component _ -> true
        | anythingElse -> false

    member this.isProtocolType =
        match this with
        | ProtocolType -> true
        | anythingElse -> false

    member this.isProtocolREF =
        match this with
        | ProtocolREF -> true
        | anythingElse -> false

    member this.isProtocolDescription =
        match this with
        | ProtocolDescription -> true
        | anythingElse -> false

    member this.isProtocolUri =
        match this with
        | ProtocolUri -> true
        | anythingElse -> false

    member this.isProtocolVersion =
        match this with
        | ProtocolVersion -> true
        | anythingElse -> false

    member this.isProtocolColumn =
        match this with
        | ProtocolREF | ProtocolDescription | ProtocolUri | ProtocolVersion | ProtocolType -> true
        | anythingElse -> false

    member this.isPerformer =
        match this with
        | Performer -> true
        | anythingElse -> false

    member this.isDate =
        match this with
        | Date -> true
        | anythingElse -> false

    member this.isFreeText =
        match this with
        | FreeText _ -> true
        | anythingElse -> false

    member this.TryInput() =
        match this with
        | Input io -> Some io
        | _ -> None

    member this.TryOutput() =
        match this with
        | Output io -> Some io
        | _ -> None

    member this.TryIOType() =
        match this with
        | Output io | Input io -> Some io
        | _ -> None

    member this.TryParameter() = 
        match this with 
        | Parameter oa -> Some (ProtocolParameter.create(ParameterName = oa))
        | _ -> None

    member this.TryFactor() =
        match this with
        | Factor oa -> Some (Factor.create(FactorType = oa))
        | _ -> None

    member this.TryCharacteristic() =
        match this with
        | Characteristic oa -> Some (MaterialAttribute.create(CharacteristicType = oa))
        | _ -> None

    member this.TryComponent() =
        match this with
        | Component oa -> Some (Component.create(ComponentType = oa))
        | _ -> None

    member this.GetUITooltip() =
        // https://fable.io/docs/javascript/features.html#u2-u3--u9
        CompositeHeader.getUITooltip (U2.Case1
        this)

    // https://fable.io/docs/javascript/features.html#u2-u3--u9
    // U2 is an erased union type, allowing seemless integration into js syntax
    /// <summary>
    /// Can pass header as `U2.Case1 compositeHeader` or `U2.Case2 string` or (requires `open Fable.Core.JsInterop`) `!^compositeHeader` or `!^string`
    /// </summary>
    /// <param name="header"></param>
    static member getUITooltip(header:U2<CompositeHeader,string>) =
        match header with
        | U2.Case1 (Component _) | U2.Case2 "Component" -> 
            "Component columns are used to describe physical components of a experiment, e.g. instrument names, software names, and reagents names."
        | U2.Case1 (Characteristic _) | U2.Case2 "Characteristic" ->
            "Characteristic columns are used for study descriptions and describe inherent properties of the source material, e.g. a certain strain or organism part."
        | U2.Case1 (Factor _)| U2.Case2 "Factor" ->
            "Use Factor columns to describe independent variables that result in a specific output of your experiment, e.g. the light intensity under which an organism was grown."
        | U2.Case1 (Parameter _) | U2.Case2 "Parameter" ->
            "Parameter columns describe steps in your experimental workflow, e.g. the centrifugation time or the temperature used for your assay."
        | U2.Case1 (ProtocolType) | U2.Case2 "ProtocolType" ->
            "Defines the protocol type according to your preferred endpoint repository." 
        | U2.Case1 (ProtocolDescription) | U2.Case2 "ProtocolDescription" ->
            "Describe the protocol in free text."
        | U2.Case1 (ProtocolUri) | U2.Case2 "ProtocolUri" ->
            "Web or local address where the in-depth protocol is stored."
        | U2.Case1 (ProtocolVersion) | U2.Case2 "ProtocolVersion" ->
            "Defines the protocol version."
        | U2.Case1 (ProtocolREF) | U2.Case2 "ProtocolREF" ->
            "Defines the protocol name."
        | U2.Case1 (Performer) | U2.Case2 "Performer" ->
            "Defines the protocol performer."
        | U2.Case1 (Date) | U2.Case2 "Date" ->
            "Defines the date the protocol was performed."
        | U2.Case1 (Input _) | U2.Case2 "Input" ->
            "Only one input column per table. E.g. experimental samples or files."
        | U2.Case1 (Output _) |U2.Case2 "Output" ->
            "Only one output column per table. E.g. experimental samples or files."
        | U2.Case1 (FreeText _) | U2.Case2 "FreeText" ->
            "Placeholder"
        | _ -> failwith $"Unable to parse combination to existing CompositeHeader: `{header}`"

#if FABLE_COMPILER
    
    //[<CompiledName("Component")>]
    static member component(oa:OntologyAnnotation) = Component oa

    //[<CompiledName("Characteristic")>]
    static member characteristic(oa:OntologyAnnotation) = Characteristic oa

    //[<CompiledName("Factor")>]
    static member factor(oa:OntologyAnnotation) = Factor oa

    //[<CompiledName("Parameter")>]
    static member parameter(oa:OntologyAnnotation) = Parameter oa

    //[<CompiledName("ProtocolType")>]
    static member protocolType() = ProtocolType

    //[<CompiledName("ProtocolDescription")>]
    static member protocolDescription() = ProtocolDescription

    //[<CompiledName("ProtocolUri")>]
    static member protocolUri() = ProtocolUri

    //[<CompiledName("ProtocolVersion")>]
    static member protocolVersion() = ProtocolVersion

    //[<CompiledName("ProtocolREF")>]
    static member protocolREF() = ProtocolREF

    //[<CompiledName("Performer")>]
    static member performer() = Performer

    //[<CompiledName("Date")>]
    static member date() = Date

    //[<CompiledName("Input")>]
    static member input(io:IOType) = Input io

    //[<CompiledName("Output")>]
    static member output(io:IOType) = Output io

    //[<CompiledName("FreeText")>]
    static member freeText(s:string) = FreeText s

#else
#endif