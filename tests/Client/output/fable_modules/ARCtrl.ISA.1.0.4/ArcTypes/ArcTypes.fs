namespace rec ARCtrl.ISA

open Fable.Core
open ARCtrl.ISA.Aux

module ArcTypesAux =

    open System.Collections.Generic

    module ErrorMsgs =

        let unableToFindAssayIdentifier assayIdentifier investigationIdentifier = 
            $"Error. Unable to find assay with identifier '{assayIdentifier}' in investigation {investigationIdentifier}."

        let unableToFindStudyIdentifier studyIdentifer investigationIdentifier =
            $"Error. Unable to find study with identifier '{studyIdentifer}' in investigation {investigationIdentifier}."

    module SanityChecks = 

        let inline validateRegisteredInvestigation (investigation: ArcInvestigation option) =
            match investigation with
            | None -> failwith "Cannot execute this function. Object is not part of ArcInvestigation."
            | Some i -> i

        let inline validateAssayRegisterInInvestigation (assayIdent: string) (existingAssayIdents: seq<string>) =
            match existingAssayIdents |> Seq.tryFind (fun x -> x = assayIdent)  with
            | None ->
                failwith $"The given assay must be added to Investigation before it can be registered."
            | Some _ ->
                ()

        let inline validateExistingStudyRegisterInInvestigation (studyIdent: string) (existingStudyIdents: seq<string>) =
            match existingStudyIdents |> Seq.tryFind (fun x -> x = studyIdent)  with
            | None ->
                failwith $"The given study with identifier '{studyIdent}' must be added to Investigation before it can be registered."
            | Some _ ->
                ()
        
        let inline validateUniqueRegisteredStudyIdentifiers (studyIdent: string) (studyIdents: seq<string>) =
            match studyIdents |> Seq.contains studyIdent with
            | true ->
                failwith $"Study with identifier '{studyIdent}' is already registered!"
            | false ->
                ()

        let inline validateUniqueAssayIdentifier (assayIdent: string) (existingAssayIdents: seq<string>) =
            match existingAssayIdents |> Seq.tryFindIndex (fun x -> x = assayIdent)  with
            | Some i ->
                failwith $"Cannot create assay with name {assayIdent}, as assay names must be unique and assay at index {i} has the same name."
            | None ->
                ()

        let inline validateUniqueStudyIdentifier (study: ArcStudy) (existingStudies: seq<ArcStudy>) =
            match existingStudies |> Seq.tryFindIndex (fun x -> x.Identifier = study.Identifier) with
            | Some i ->
                failwith $"Cannot create study with name {study.Identifier}, as study names must be unique and study at index {i} has the same name."
            | None ->
                ()

    /// <summary>
    /// Some functions can change ArcInvestigation.Assays elements. After these functions we must remove all registered assays which might have gone lost.
    /// </summary>
    /// <param name="inv"></param>
    let inline removeMissingRegisteredAssays (inv: ArcInvestigation) : unit =
        let existingAssays = inv.AssayIdentifiers
        for study in inv.Studies do
            let rai : ResizeArray<string> = study.RegisteredAssayIdentifiers
            let registeredAssays = ResizeArray(rai)
            for registeredAssay in registeredAssays do
                if Seq.contains registeredAssay existingAssays |> not then
                    study.DeregisterAssay registeredAssay |> ignore

    let inline updateAppendArray (append:bool) (origin: 'A []) (next: 'A []) = 
        if not append then
            next
        else 
            Array.append origin next
            |> Array.distinct

    let inline updateAppendResizeArray (append:bool) (origin: ResizeArray<'A>) (next: ResizeArray<'A>) = 
        if not append then
            next
        else
            for e in next do
                if origin.Contains e |> not then
                    origin.Add(e)
            origin
        

[<AttachMembers>]
type ArcAssay(identifier: string, ?measurementType : OntologyAnnotation, ?technologyType : OntologyAnnotation, ?technologyPlatform : OntologyAnnotation, ?tables: ResizeArray<ArcTable>, ?performers : Person [], ?comments : Comment []) = 
    inherit ArcTables(defaultArg tables <| ResizeArray())
    
    let performers = defaultArg performers [||]
    let comments = defaultArg comments [||]
    let mutable identifier : string = identifier
    let mutable investigation : ArcInvestigation option = None
    let mutable measurementType : OntologyAnnotation option = measurementType
    let mutable technologyType : OntologyAnnotation option = technologyType
    let mutable technologyPlatform : OntologyAnnotation option = technologyPlatform
    let mutable performers : Person [] = performers
    let mutable comments : Comment [] = comments

    /// Must be unique in one study
    member this.Identifier with get() = identifier and internal set(i) = identifier <- i
    // read-online
    member this.Investigation with get() = investigation and internal set(i) = investigation <- i
    member this.MeasurementType with get() = measurementType and set(n) = measurementType <- n
    member this.TechnologyType with get() = technologyType and set(n) = technologyType <- n
    member this.TechnologyPlatform with get() = technologyPlatform and set(n) = technologyPlatform <- n
    member this.Performers with get() = performers and set(n) = performers <- n
    member this.Comments with get() = comments and set(n) = comments <- n

    static member init (identifier : string) = ArcAssay(identifier)
    static member create (identifier: string, ?measurementType : OntologyAnnotation, ?technologyType : OntologyAnnotation, ?technologyPlatform : OntologyAnnotation, ?tables: ResizeArray<ArcTable>, ?performers : Person [], ?comments : Comment []) = 
        ArcAssay(identifier = identifier, ?measurementType = measurementType, ?technologyType = technologyType, ?technologyPlatform = technologyPlatform, ?tables =tables, ?performers = performers, ?comments = comments)

    static member make 
        (identifier : string)
        (measurementType : OntologyAnnotation option)
        (technologyType : OntologyAnnotation option)
        (technologyPlatform : OntologyAnnotation option)
        (tables : ResizeArray<ArcTable>)
        (performers : Person [])
        (comments : Comment []) = 
        ArcAssay(identifier = identifier, ?measurementType = measurementType, ?technologyType = technologyType, ?technologyPlatform = technologyPlatform, tables =tables, performers = performers, comments = comments)

    static member FileName = ARCtrl.Path.AssayFileName
    member this.StudiesRegisteredIn
        with get () = 
            match this.Investigation with
            | Some i -> 
                i.Studies
                |> Seq.filter (fun s -> s.RegisteredAssayIdentifiers |> Seq.contains this.Identifier)
                |> Seq.toArray
            | None -> [||]
        
    // - Table API - //
    static member addTable(table:ArcTable, ?index: int) =
        fun (assay:ArcAssay) ->
            let c = assay.Copy()
            c.AddTable(table, ?index = index)
            c

    // - Table API - //
    static member addTables(tables:seq<ArcTable>, ?index: int) =
        fun (assay:ArcAssay) ->
            let c = assay.Copy()
            c.AddTables(tables, ?index = index)
            c

    // - Table API - //
    static member initTable(tableName: string, ?index: int) =
        fun (assay:ArcAssay) ->
            let c = assay.Copy()
            c,c.InitTable(tableName, ?index=index)
            
    // - Table API - //
    static member initTables(tableNames:seq<string>, ?index: int) =
        fun (assay:ArcAssay) ->
            let c = assay.Copy()
            c.InitTables(tableNames, ?index=index)
            c

    // - Table API - //
    /// Receive **copy** of table at `index`
    static member getTableAt(index:int) : ArcAssay -> ArcTable =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.GetTableAt(index)

    // - Table API - //
    /// Receive **copy** of table with `name` = `ArcTable.Name`
    static member getTable(name: string) : ArcAssay -> ArcTable =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.GetTable(name)

    // - Table API - //
    static member updateTableAt(index:int, table:ArcTable) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.UpdateTableAt(index, table)
            newAssay

    // - Table API - //
    static member updateTable(name: string, table:ArcTable) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.UpdateTable(name, table)
            newAssay

    // - Table API - //
    static member setTableAt(index:int, table:ArcTable) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.SetTableAt(index, table)
            newAssay

    // - Table API - //
    static member setTable(name: string, table:ArcTable) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.SetTable(name, table)
            newAssay

    // - Table API - //
    static member removeTableAt(index:int) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.RemoveTableAt(index)
            newAssay

    // - Table API - //
    static member removeTable(name: string) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.RemoveTable(name)
            newAssay

    // - Table API - //
    static member mapTableAt(index:int, updateFun: ArcTable -> unit) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()    
            newAssay.MapTableAt(index, updateFun)
            newAssay

    // - Table API - //
    static member updateTable(name: string, updateFun: ArcTable -> unit) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.MapTable(name, updateFun)
            newAssay

    // - Table API - //
    static member renameTableAt(index: int, newName: string) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()    
            newAssay.RenameTableAt(index, newName)
            newAssay

    // - Table API - //
    static member renameTable(name: string, newName: string) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.RenameTable(name, newName)
            newAssay

    // - Column CRUD API - //
    static member addColumnAt(tableIndex:int, header: CompositeHeader, ?cells: CompositeCell [], ?columnIndex: int, ?forceReplace: bool) : ArcAssay -> ArcAssay = 
        fun (assay: ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.AddColumnAt(tableIndex, header, ?cells=cells, ?columnIndex=columnIndex, ?forceReplace=forceReplace)
            newAssay

    // - Column CRUD API - //
    static member addColumn(tableName: string, header: CompositeHeader, ?cells: CompositeCell [], ?columnIndex: int, ?forceReplace: bool) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.AddColumn(tableName, header, ?cells=cells, ?columnIndex=columnIndex, ?forceReplace=forceReplace)
            newAssay

    // - Column CRUD API - //
    static member removeColumnAt(tableIndex: int, columnIndex: int) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.RemoveColumnAt(tableIndex, columnIndex)
            newAssay

    // - Column CRUD API - //
    static member removeColumn(tableName: string, columnIndex: int) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.RemoveColumn(tableName, columnIndex)
            newAssay

    // - Column CRUD API - //
    static member updateColumnAt(tableIndex: int, columnIndex: int, header: CompositeHeader, ?cells: CompositeCell []) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.UpdateColumnAt(tableIndex, columnIndex, header, ?cells=cells)
            newAssay

    // - Column CRUD API - //
    static member updateColumn(tableName: string, columnIndex: int, header: CompositeHeader, ?cells: CompositeCell []) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.UpdateColumn(tableName, columnIndex, header, ?cells=cells)
            newAssay

    // - Column CRUD API - //
    static member getColumnAt(tableIndex: int, columnIndex: int) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.GetColumnAt(tableIndex, columnIndex)

    // - Column CRUD API - //
    static member getColumn(tableName: string, columnIndex: int) =
        fun (assay: ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.GetColumn(tableName, columnIndex)

    // - Row CRUD API - //
    static member addRowAt(tableIndex:int, ?cells: CompositeCell [], ?rowIndex: int) : ArcAssay -> ArcAssay = 
        fun (assay: ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.AddRowAt(tableIndex, ?cells=cells, ?rowIndex=rowIndex)
            newAssay

    // - Row CRUD API - //
    static member addRow(tableName: string, ?cells: CompositeCell [], ?rowIndex: int) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.AddRow(tableName, ?cells=cells, ?rowIndex=rowIndex)
            newAssay

    // - Row CRUD API - //
    static member removeRowAt(tableIndex: int, rowIndex: int) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.RemoveColumnAt(tableIndex, rowIndex)
            newAssay

    // - Row CRUD API - //
    static member removeRow(tableName: string, rowIndex: int) : ArcAssay -> ArcAssay =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.RemoveRow(tableName, rowIndex)
            newAssay

    // - Row CRUD API - //
    static member updateRowAt(tableIndex: int, rowIndex: int, cells: CompositeCell []) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.UpdateRowAt(tableIndex, rowIndex, cells)
            newAssay

    // - Row CRUD API - //
    static member updateRow(tableName: string, rowIndex: int, cells: CompositeCell []) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.UpdateRow(tableName, rowIndex, cells)
            newAssay

    // - Row CRUD API - //
    static member getRowAt(tableIndex: int, rowIndex: int) =
        fun (assay:ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.GetRowAt(tableIndex, rowIndex)

    // - Row CRUD API - //
    static member getRow(tableName: string, rowIndex: int) =
        fun (assay: ArcAssay) ->
            let newAssay = assay.Copy()
            newAssay.GetRow(tableName, rowIndex)

    // - Mutable properties API - //
    static member setPerformers performers (assay: ArcAssay) =
        assay.Performers <- performers
        assay

    member this.Copy() : ArcAssay =
        let nextTables = ResizeArray()
        for table in this.Tables do
            let copy = table.Copy()
            nextTables.Add(copy)
        let nextComments = this.Comments |> Array.map (fun c -> c.Copy())
        let nextPerformers = this.Performers |> Array.map (fun c -> c.Copy())
        ArcAssay.make
            this.Identifier
            this.MeasurementType
            this.TechnologyType
            this.TechnologyPlatform
            nextTables
            nextPerformers
            nextComments

    /// <summary>
    /// Updates given assay with another assay, Identifier will never be updated. By default update is full replace. Optional Parameters can be used to specify update logic.
    /// </summary>
    /// <param name="assay">The assay used for updating this assay.</param>
    /// <param name="onlyReplaceExisting">If true, this will only update fields which are `Some` or non-empty lists. Default: **false**</param>
    /// <param name="appendSequences">If true, this will append lists instead of replacing. Will return only distinct elements. Default: **false**</param>
    member this.UpdateBy(assay:ArcAssay,?onlyReplaceExisting : bool,?appendSequences : bool) =
        let onlyReplaceExisting = defaultArg onlyReplaceExisting false
        let appendSequences = defaultArg appendSequences false
        let updateAlways = onlyReplaceExisting |> not
        if assay.MeasurementType.IsSome || updateAlways then 
            this.MeasurementType <- assay.MeasurementType
        if assay.TechnologyType.IsSome || updateAlways then 
            this.TechnologyType <- assay.TechnologyType
        if assay.TechnologyPlatform.IsSome || updateAlways then 
            this.TechnologyPlatform <- assay.TechnologyPlatform
        if assay.Tables.Count <> 0 || updateAlways then
            let s = ArcTypesAux.updateAppendResizeArray appendSequences this.Tables assay.Tables
            this.Tables <- s
        if assay.Performers.Length <> 0 || updateAlways then
            let s = ArcTypesAux.updateAppendArray appendSequences this.Performers assay.Performers
            this.Performers <- s
        if assay.Comments.Length <> 0 || updateAlways then
            let s = ArcTypesAux.updateAppendArray appendSequences this.Comments assay.Comments
            this.Comments <- s

    // Use this for better print debugging and better unit test output
    override this.ToString() =
        sprintf 
            """ArcAssay({
    Identifier = "%s",
    MeasurementType = %A,
    TechnologyType = %A,
    TechnologyPlatform = %A,
    Tables = %A,
    Performers = %A,
    Comments = %A
})"""
            this.Identifier
            this.MeasurementType
            this.TechnologyType
            this.TechnologyPlatform
            this.Tables
            this.Performers
            this.Comments
    /// This function creates a string containing the name and the ontology short-string of the given ontology annotation term
    ///
    /// TechnologyPlatforms are plain strings in ISA-JSON.
    ///
    /// This function allows us, to parse them as an ontology term.
    static member composeTechnologyPlatform (tp : OntologyAnnotation) = 
        match tp.TANInfo with
        | Some _ ->
            $"{tp.NameText} ({tp.TermAccessionShort})"
        | None ->
            $"{tp.NameText}"

    /// This function parses the given string containing the name and the ontology short-string of the given ontology annotation term
    ///
    /// TechnologyPlatforms are plain strings in ISA-JSON.
    ///
    /// This function allows us, to parse them as an ontology term.
    static member decomposeTechnologyPlatform (name : string) = 
        let pattern = """(?<value>[^\(]+) \((?<ontology>[^(]*:[^)]*)\)"""
        

        match name with 
        | Regex.ActivePatterns.Regex pattern r -> 
            let oa = (r.Groups.Item "ontology").Value   |> OntologyAnnotation.fromTermAnnotation 
            let v =  (r.Groups.Item "value").Value      |> Value.fromString
            {oa with Name = (Some v.Text)}
        | _ ->
            OntologyAnnotation.fromString(termName = name)

    member internal this.AddToInvestigation (investigation: ArcInvestigation) =
        this.Investigation <- Some investigation

    member internal this.RemoveFromInvestigation () =
        this.Investigation <- None

    /// Updates given assay stored in an study or investigation file with values from an assay file.
    member this.UpdateReferenceByAssayFile(assay:ArcAssay,?onlyReplaceExisting : bool) =
        let onlyReplaceExisting = defaultArg onlyReplaceExisting false
        let updateAlways = onlyReplaceExisting |> not
        if assay.MeasurementType.IsSome || updateAlways then 
            this.MeasurementType <- assay.MeasurementType
        if assay.TechnologyPlatform.IsSome || updateAlways then 
            this.TechnologyPlatform <- assay.TechnologyPlatform
        if assay.TechnologyType.IsSome || updateAlways then 
            this.TechnologyType <- assay.TechnologyType
        if assay.Tables.Count <> 0 || updateAlways then          
            this.Tables <- assay.Tables
        if assay.Comments.Length <> 0 || updateAlways then          
            this.Comments <- assay.Comments  
        if assay.Performers.Length <> 0 || updateAlways then          
            this.Performers <- assay.Performers  

    /// Copies ArcAssay object without the pointer to the parent ArcInvestigation
    ///
    /// In order to copy the pointer to the parent ArcInvestigation as well, use the Copy() method of the ArcInvestigation instead.
    member this.ToAssay() : Assay = 
        let processSeq = ArcTables(this.Tables).GetProcesses()
        let assayMaterials =
            AssayMaterials.create(
                ?Samples = (ProcessSequence.getSamples processSeq |> Option.fromValueWithDefault []),
                ?OtherMaterials = (ProcessSequence.getMaterials processSeq |> Option.fromValueWithDefault [])
            )
            |> Option.fromValueWithDefault AssayMaterials.empty
        let fileName = 
            if ARCtrl.ISA.Identifier.isMissingIdentifier this.Identifier then
                None
            else 
                Some (ARCtrl.ISA.Identifier.Assay.fileNameFromIdentifier this.Identifier)
        Assay.create(
            ?FileName = fileName,
            ?MeasurementType = this.MeasurementType,
            ?TechnologyType = this.TechnologyType,
            ?TechnologyPlatform = (this.TechnologyPlatform |> Option.map ArcAssay.composeTechnologyPlatform),
            ?DataFiles = (ProcessSequence.getData processSeq |> Option.fromValueWithDefault []),
            ?Materials = assayMaterials,
            ?CharacteristicCategories = (ProcessSequence.getCharacteristics processSeq |> Option.fromValueWithDefault []),
            ?UnitCategories = (ProcessSequence.getUnits processSeq |> Option.fromValueWithDefault []),
            ?ProcessSequence = (processSeq |> Option.fromValueWithDefault []),
            ?Comments = (this.Comments |> List.ofArray |> Option.fromValueWithDefault [])
            )

    // Create an ArcAssay from an ISA Json Assay.
    static member fromAssay (a : Assay) : ArcAssay = 
        let tables = (a.ProcessSequence |> Option.map (ArcTables.fromProcesses >> fun t -> t.Tables))
        let identifer = 
            match a.FileName with
            | Some fn -> Identifier.Assay.identifierFromFileName fn
            | None -> Identifier.createMissingIdentifier()
        ArcAssay.create(
            identifer,
            ?measurementType = (a.MeasurementType |> Option.map (fun x -> x.Copy())),
            ?technologyType = (a.TechnologyType |> Option.map (fun x -> x.Copy())),
            ?technologyPlatform = (a.TechnologyPlatform |> Option.map ArcAssay.decomposeTechnologyPlatform),
            ?tables = tables,
            ?comments = (a.Comments |> Option.map Array.ofList)
            )

    member this.StructurallyEquals (other: ArcAssay) : bool =
        let i = this.Identifier = other.Identifier
        let mst = this.MeasurementType = other.MeasurementType
        let tt = this.TechnologyType = other.TechnologyType
        let tp = this.TechnologyPlatform = other.TechnologyPlatform
        let tables = Aux.compareSeq this.Tables other.Tables
        let perf = Aux.compareSeq this.Performers other.Performers
        let comments = Aux.compareSeq this.Comments other.Comments
        // Todo maybe add reflection check to prove that all members are compared?
        [|i; mst; tt; tp; tables; perf; comments|] |> Seq.forall (fun x -> x = true)

    /// <summary>
    /// Use this function to check if this ArcAssay and the input ArcAssay refer to the same object.
    ///
    /// If true, updating one will update the other due to mutability.
    /// </summary>
    /// <param name="other">The other ArcAssay to test for reference.</param>
    member this.ReferenceEquals (other: ArcAssay) = System.Object.ReferenceEquals(this,other)

    // custom check
    override this.Equals other =
        match other with
        | :? ArcAssay as assay -> 
            this.StructurallyEquals(assay)
        | _ -> false

    override this.GetHashCode() = 
        [|
            box this.Identifier
            Aux.HashCodes.boxHashOption this.MeasurementType
            Aux.HashCodes.boxHashOption this.TechnologyType
            Aux.HashCodes.boxHashOption this.TechnologyPlatform
            Array.ofSeq >> Aux.HashCodes.boxHashArray <| this.Tables
            Aux.HashCodes.boxHashArray this.Performers
            Aux.HashCodes.boxHashArray this.Comments
        |]
        |> Aux.HashCodes.boxHashArray 
        |> fun x -> x :?> int

[<AttachMembers>]
type ArcStudy(identifier : string, ?title, ?description, ?submissionDate, ?publicReleaseDate, ?publications, ?contacts, ?studyDesignDescriptors, ?tables, ?registeredAssayIdentifiers: ResizeArray<string>, ?factors, ?comments) = 
    inherit ArcTables(defaultArg tables <| ResizeArray())

    let publications = defaultArg publications [||]
    let contacts = defaultArg contacts [||]
    let studyDesignDescriptors = defaultArg studyDesignDescriptors [||]
    let registeredAssayIdentifiers = defaultArg registeredAssayIdentifiers <| ResizeArray()
    let factors = defaultArg factors [||]
    let comments = defaultArg comments [||]

    let mutable identifier = identifier
    let mutable investigation : ArcInvestigation option = None
    let mutable title : string option = title
    let mutable description : string option = description 
    let mutable submissionDate : string option = submissionDate
    let mutable publicReleaseDate : string option = publicReleaseDate
    let mutable publications : Publication [] = publications
    let mutable contacts : Person [] = contacts
    let mutable studyDesignDescriptors : OntologyAnnotation [] = studyDesignDescriptors
    let mutable registeredAssayIdentifiers : ResizeArray<string> = registeredAssayIdentifiers
    let mutable factors : Factor [] = factors
    let mutable comments : Comment [] = comments
    /// Must be unique in one investigation
    member this.Identifier with get() = identifier and internal set(i) = identifier <- i
    // read-only
    member this.Investigation with get() = investigation and internal set(i) = investigation <- i
    member this.Title with get() = title and set(n) = title <- n
    member this.Description with get() = description and set(n) = description <- n
    member this.SubmissionDate with get() = submissionDate and set(n) = submissionDate <- n
    member this.PublicReleaseDate with get() = publicReleaseDate and set(n) = publicReleaseDate <- n
    member this.Publications with get() = publications and set(n) = publications <- n
    member this.Contacts with get() = contacts and set(n) = contacts <- n
    member this.StudyDesignDescriptors with get() = studyDesignDescriptors and set(n) = studyDesignDescriptors <- n
    member this.RegisteredAssayIdentifiers with get() = registeredAssayIdentifiers and set(n) = registeredAssayIdentifiers <- n
    member this.Factors with get() = factors and set(n) = factors <- n
    member this.Comments with get() = comments and set(n) = comments <- n

    static member init(identifier : string) = ArcStudy identifier

    static member create(identifier : string, ?title, ?description, ?submissionDate, ?publicReleaseDate, ?publications, ?contacts, ?studyDesignDescriptors, ?tables, ?registeredAssayIdentifiers, ?factors, ?comments) = 
        ArcStudy(identifier, ?title = title, ?description = description, ?submissionDate =  submissionDate, ?publicReleaseDate = publicReleaseDate, ?publications = publications, ?contacts = contacts, ?studyDesignDescriptors = studyDesignDescriptors, ?tables = tables, ?registeredAssayIdentifiers = registeredAssayIdentifiers, ?factors = factors, ?comments = comments)

    static member make identifier title description submissionDate publicReleaseDate publications contacts studyDesignDescriptors tables registeredAssayIdentifiers factors comments = 
        ArcStudy(identifier, ?title = title, ?description = description, ?submissionDate =  submissionDate, ?publicReleaseDate = publicReleaseDate, publications = publications, contacts = contacts, studyDesignDescriptors = studyDesignDescriptors, tables = tables, registeredAssayIdentifiers = registeredAssayIdentifiers, factors = factors, comments = comments)

    /// <summary>
    /// Returns true if all fields are None/ empty sequences **except** Identifier.
    /// </summary>
    member this.isEmpty 
        with get() =
            (this.Title = None) &&
            (this.Description = None) &&
            (this.SubmissionDate = None) &&
            (this.PublicReleaseDate = None) &&
            (this.Publications = [||]) &&
            (this.Contacts = [||]) &&
            (this.StudyDesignDescriptors = [||]) &&
            (this.Tables.Count = 0) &&
            (this.RegisteredAssayIdentifiers.Count = 0) &&
            (this.Factors = [||]) &&
            (this.Comments = [||])

    // Not sure how to handle this best case.
    static member FileName = ARCtrl.Path.StudyFileName
    //member this.FileName = ArcStudy.FileName

    /// Returns the count of registered assay *identifiers*. This is not necessarily the same as the count of registered assays, as not all identifiers correspond to an existing assay.
    member this.RegisteredAssayIdentifierCount 
        with get() = this.RegisteredAssayIdentifiers.Count

    /// Returns the count of registered assays. This is not necessarily the same as the count of registered assay *identifiers*, as not all identifiers correspond to an existing assay.
    member this.RegisteredAssayCount 
        with get() = this.RegisteredAssays.Count

    /// Returns all assays registered in this study, that correspond to an existing assay object in the associated investigation.
    member this.RegisteredAssays
        with get(): ResizeArray<ArcAssay> = 
            let inv = ArcTypesAux.SanityChecks.validateRegisteredInvestigation this.Investigation
            this.RegisteredAssayIdentifiers 
            |> Seq.choose inv.TryGetAssay
            |> ResizeArray

    /// Returns all registered assay identifiers that do not correspond to an existing assay object in the associated investigation.
    member this.VacantAssayIdentifiers
        with get() = 
            let inv = ArcTypesAux.SanityChecks.validateRegisteredInvestigation this.Investigation
            this.RegisteredAssayIdentifiers 
            |> Seq.filter (inv.ContainsAssay >> not)
            |> ResizeArray

    // - Assay API - CRUD //
    /// <summary>
    /// Add assay to investigation and register it to study.
    /// </summary>
    /// <param name="assay"></param>
    member this.AddRegisteredAssay(assay: ArcAssay) =
        let inv = ArcTypesAux.SanityChecks.validateRegisteredInvestigation this.Investigation 
        inv.AddAssay(assay)
        inv.RegisterAssay(this.Identifier,assay.Identifier)

    static member addRegisteredAssay(assay: ArcAssay) =
        fun (study:ArcStudy) ->
            let newStudy = study.Copy()
            newStudy.AddRegisteredAssay(assay)
            newStudy

    // - Assay API - CRUD //
    member this.InitRegisteredAssay(assayIdentifier: string) =
        let assay = ArcAssay(assayIdentifier)
        this.AddRegisteredAssay(assay)
        assay

    static member initRegisteredAssay(assayIdentifier: string) =
        fun (study:ArcStudy) ->
            let copy = study.Copy()
            copy,copy.InitRegisteredAssay(assayIdentifier)

    // - Assay API - CRUD //
    member this.RegisterAssay(assayIdentifier: string) =
        if Seq.contains assayIdentifier this.RegisteredAssayIdentifiers then failwith $"Assay `{assayIdentifier}` is already registered on the study."
        this.RegisteredAssayIdentifiers.Add(assayIdentifier)

    static member registerAssay(assayIdentifier: string) =
        fun (study: ArcStudy) ->
            let copy = study.Copy()
            copy.RegisterAssay(assayIdentifier)
            copy

    // - Assay API - CRUD //
    member this.DeregisterAssay(assayIdentifier: string) =
        this.RegisteredAssayIdentifiers.Remove(assayIdentifier) |> ignore

    static member deregisterAssay(assayIdentifier: string) =
        fun (study: ArcStudy) ->
            let copy = study.Copy()
            copy.DeregisterAssay(assayIdentifier)
            copy

    // - Assay API - CRUD //
    member this.GetRegisteredAssay(assayIdentifier: string) =
        if Seq.contains assayIdentifier this.RegisteredAssayIdentifiers |> not then failwith $"Assay `{assayIdentifier}` is not registered on the study."
        let inv = ArcTypesAux.SanityChecks.validateRegisteredInvestigation this.Investigation
        inv.GetAssay(assayIdentifier)

    static member getRegisteredAssay(assayIdentifier: string) =
        fun (study: ArcStudy) ->
            let copy = study.Copy()
            copy.GetRegisteredAssay(assayIdentifier)

    // - Assay API - CRUD //
    static member getRegisteredAssays() =
        fun (study: ArcStudy) ->
            let copy = study.Copy()
            copy.RegisteredAssays

    /// <summary>
    /// Returns ArcAssays registered in study, or if no parent exists, initializies new ArcAssay from identifier.
    /// </summary>
    member this.GetRegisteredAssaysOrIdentifier() = 
        // Two Options:
        // 1. Init new assays with only identifier. This is possible without ArcInvestigation parent.
        // 2. Get full assays from ArcInvestigation parent.
        match this.Investigation with
        | Some i -> 
            this.RegisteredAssayIdentifiers
            |> ResizeArray.map (fun identifier -> 
                match i.TryGetAssay(identifier) with
                | Some assay -> assay
                | None -> ArcAssay.init(identifier)
            )
        | None ->
            this.RegisteredAssayIdentifiers 
            |> ResizeArray.map (fun identifier -> ArcAssay.init(identifier))   

    /// <summary>
    /// Returns ArcAssays registered in study, or if no parent exists, initializies new ArcAssay from identifier.
    /// </summary>
    static member getRegisteredAssaysOrIdentifier() =
        fun (study: ArcStudy) ->
            let copy = study.Copy()
            copy.GetRegisteredAssaysOrIdentifier()

    ////////////////////////////////////
    // - Copy & Paste from ArcAssay - //
    ////////////////////////////////////

    // - Table API - //
    // remark should this return ArcTable?
    static member addTable(table:ArcTable, ?index: int) =
        fun (study:ArcStudy) ->
            let c = study.Copy()
            c.AddTable(table, ?index = index)
            c

    // - Table API - //
    static member addTables(tables:seq<ArcTable>, ?index: int) =
        fun (study:ArcStudy) ->
            let c = study.Copy()
            c.AddTables(tables, ?index = index)
            c

    // - Table API - //
    static member initTable(tableName: string, ?index: int) =
        fun (study:ArcStudy) ->
            let c = study.Copy()
            c,c.InitTable(tableName, ?index=index)
            

    // - Table API - //
    static member initTables(tableNames:seq<string>, ?index: int) =
        fun (study:ArcStudy) ->
            let c = study.Copy()
            c.InitTables(tableNames, ?index=index)
            c

    // - Table API - //
    /// Receive **copy** of table at `index`
    static member getTableAt(index:int) : ArcStudy -> ArcTable =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.GetTableAt(index)

    // - Table API - //
    /// Receive **copy** of table with `name` = `ArcTable.Name`
    static member getTable(name: string) : ArcStudy -> ArcTable =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.GetTable(name)

    // - Table API - //
    static member updateTableAt(index:int, table:ArcTable) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.UpdateTableAt(index, table)
            newAssay

    // - Table API - //
    static member updateTable(name: string, table:ArcTable) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.UpdateTable(name, table)
            newAssay


    // - Table API - //
    static member setTableAt(index:int, table:ArcTable) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.SetTableAt(index, table)
            newAssay

    // - Table API - //
    static member setTable(name: string, table:ArcTable) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.SetTable(name, table)
            newAssay

    // - Table API - //
    static member removeTableAt(index:int) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.RemoveTableAt(index)
            newAssay

    // - Table API - //
    static member removeTable(name: string) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.RemoveTable(name)
            newAssay

    // - Table API - //
    // Remark: This must stay `ArcTable -> unit` so name cannot be changed here.
    static member mapTableAt(index:int, updateFun: ArcTable -> unit) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()    
            newAssay.MapTableAt(index, updateFun)
            newAssay

    // - Table API - //
    static member mapTable(name: string, updateFun: ArcTable -> unit) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.MapTable(name, updateFun)
            newAssay

    // - Table API - //
    static member renameTableAt(index: int, newName: string) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()    
            newAssay.RenameTableAt(index, newName)
            newAssay

    // - Table API - //
    static member renameTable(name: string, newName: string) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.RenameTable(name, newName)
            newAssay

    // - Column CRUD API - //
    static member addColumnAt(tableIndex:int, header: CompositeHeader, ?cells: CompositeCell [], ?columnIndex: int, ?forceReplace: bool) : ArcStudy -> ArcStudy = 
        fun (study: ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.AddColumnAt(tableIndex, header, ?cells=cells, ?columnIndex=columnIndex, ?forceReplace=forceReplace)
            newAssay

    // - Column CRUD API - //
    static member addColumn(tableName: string, header: CompositeHeader, ?cells: CompositeCell [], ?columnIndex: int, ?forceReplace: bool) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.AddColumn(tableName, header, ?cells=cells, ?columnIndex=columnIndex, ?forceReplace=forceReplace)
            newAssay

    // - Column CRUD API - //
    static member removeColumnAt(tableIndex: int, columnIndex: int) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.RemoveColumnAt(tableIndex, columnIndex)
            newAssay

    // - Column CRUD API - //
    static member removeColumn(tableName: string, columnIndex: int) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.RemoveColumn(tableName, columnIndex)
            newAssay

    // - Column CRUD API - //
    static member updateColumnAt(tableIndex: int, columnIndex: int, header: CompositeHeader, ?cells: CompositeCell []) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.UpdateColumnAt(tableIndex, columnIndex, header, ?cells=cells)
            newAssay

    // - Column CRUD API - //
    static member updateColumn(tableName: string, columnIndex: int, header: CompositeHeader, ?cells: CompositeCell []) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.UpdateColumn(tableName, columnIndex, header, ?cells=cells)
            newAssay

    // - Column CRUD API - //
    static member getColumnAt(tableIndex: int, columnIndex: int) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.GetColumnAt(tableIndex, columnIndex)

    // - Column CRUD API - //
    static member getColumn(tableName: string, columnIndex: int) =
        fun (study: ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.GetColumn(tableName, columnIndex)

    // - Row CRUD API - //
    static member addRowAt(tableIndex:int, ?cells: CompositeCell [], ?rowIndex: int) : ArcStudy -> ArcStudy = 
        fun (study: ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.AddRowAt(tableIndex, ?cells=cells, ?rowIndex=rowIndex)
            newAssay

    // - Row CRUD API - //
    static member addRow(tableName: string, ?cells: CompositeCell [], ?rowIndex: int) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.AddRow(tableName, ?cells=cells, ?rowIndex=rowIndex)
            newAssay

    // - Row CRUD API - //
    static member removeRowAt(tableIndex: int, rowIndex: int) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.RemoveColumnAt(tableIndex, rowIndex)
            newAssay

    // - Row CRUD API - //
    static member removeRow(tableName: string, rowIndex: int) : ArcStudy -> ArcStudy =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.RemoveRow(tableName, rowIndex)
            newAssay

    // - Row CRUD API - //
    static member updateRowAt(tableIndex: int, rowIndex: int, cells: CompositeCell []) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.UpdateRowAt(tableIndex, rowIndex, cells)
            newAssay

    // - Row CRUD API - //
    static member updateRow(tableName: string, rowIndex: int, cells: CompositeCell []) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.UpdateRow(tableName, rowIndex, cells)
            newAssay

    // - Row CRUD API - //
    static member getRowAt(tableIndex: int, rowIndex: int) =
        fun (study:ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.GetRowAt(tableIndex, rowIndex)

    // - Row CRUD API - //
    static member getRow(tableName: string, rowIndex: int) =
        fun (study: ArcStudy) ->
            let newAssay = study.Copy()
            newAssay.GetRow(tableName, rowIndex)

    member internal this.AddToInvestigation (investigation: ArcInvestigation) =
        this.Investigation <- Some investigation

    member internal this.RemoveFromInvestigation () =
        this.Investigation <- None

    /// Copies ArcStudy object without the pointer to the parent ArcInvestigation
    ///
    /// This copy does only contain the identifiers of the registered ArcAssays and not the actual objects.
    ///
    /// In order to copy the ArcAssays as well, use the Copy() method of the ArcInvestigation.
    member this.Copy() : ArcStudy =
        let nextTables = ResizeArray()
        let nextAssayIdentifiers = ResizeArray(this.RegisteredAssayIdentifiers)
        for table in this.Tables do
            let copy = table.Copy()
            nextTables.Add(copy)
        let nextComments = this.Comments |> Array.map (fun c -> c.Copy())
        let nextFactors = this.Factors |> Array.map (fun c -> c.Copy())
        let nextContacts = this.Contacts |> Array.map (fun c -> c.Copy())
        let nextPublications = this.Publications |> Array.map (fun c -> c.Copy())
        let nextStudyDesignDescriptors = this.StudyDesignDescriptors |> Array.map (fun c -> c.Copy())
        ArcStudy.make
            this.Identifier
            this.Title
            this.Description
            this.SubmissionDate
            this.PublicReleaseDate
            nextPublications
            nextContacts
            nextStudyDesignDescriptors
            nextTables
            nextAssayIdentifiers
            nextFactors
            nextComments

    /// <summary>
    /// Updates given study from an investigation file against a study from a study file. Identifier will never be updated. 
    /// </summary>
    /// <param name="study">The study used for updating this study.</param>
    /// <param name="onlyReplaceExisting">If true, this will only update fields which are `Some` or non-empty lists. Default: **false**</param>
    member this.UpdateReferenceByStudyFile(study:ArcStudy,?onlyReplaceExisting : bool,?keepUnusedRefTables) =
        let onlyReplaceExisting = defaultArg onlyReplaceExisting false
        let updateAlways = onlyReplaceExisting |> not
        if study.Title.IsSome || updateAlways then 
            this.Title <- study.Title
        if study.Description.IsSome || updateAlways then 
            this.Description <- study.Description
        if study.SubmissionDate.IsSome || updateAlways then 
            this.SubmissionDate <- study.SubmissionDate
        if study.PublicReleaseDate.IsSome || updateAlways then 
            this.PublicReleaseDate <- study.PublicReleaseDate
        if study.Publications.Length <> 0 || updateAlways then
            this.Publications <- study.Publications
        if study.Contacts.Length <> 0 || updateAlways then
            this.Contacts <- study.Contacts
        if study.StudyDesignDescriptors.Length <> 0 || updateAlways then
            this.StudyDesignDescriptors <- study.StudyDesignDescriptors
        if study.Tables.Count <> 0 || updateAlways then
            let tables = ArcTables.updateReferenceTablesBySheets(ArcTables(this.Tables),ArcTables(study.Tables),?keepUnusedRefTables = keepUnusedRefTables)
            this.Tables <- tables.Tables
        if study.RegisteredAssayIdentifiers.Count <> 0 || updateAlways then
            this.RegisteredAssayIdentifiers <- study.RegisteredAssayIdentifiers
        if study.Factors.Length <> 0 || updateAlways then            
            this.Factors <- study.Factors
        if study.Comments.Length <> 0 || updateAlways then
            this.Comments <- study.Comments

    /// <summary>
    /// Creates an ISA-Json compatible Study from ArcStudy.
    /// </summary>
    /// <param name="arcAssays">If this parameter is given, will transform these ArcAssays to Assays and include them as children of the Study. If not, tries to get them from the parent ArcInvestigation instead. If ArcStudy has no parent ArcInvestigation either, initializes new ArcAssay from registered Identifiers.</param>
    member this.ToStudy(?arcAssays: ResizeArray<ArcAssay>) : Study = 
        let processSeq = ArcTables(this.Tables).GetProcesses()
        let protocols = ProcessSequence.getProtocols processSeq |> Option.fromValueWithDefault []
        let studyMaterials =
            StudyMaterials.create(
                ?Sources = (ProcessSequence.getSources processSeq |> Option.fromValueWithDefault []),
                ?Samples = (ProcessSequence.getSamples processSeq |> Option.fromValueWithDefault []),
                ?OtherMaterials = (ProcessSequence.getMaterials processSeq |> Option.fromValueWithDefault [])
            )
            |> Option.fromValueWithDefault StudyMaterials.empty
        let identifier,fileName = 
            if ARCtrl.ISA.Identifier.isMissingIdentifier this.Identifier then
                None, None
            else
                Some this.Identifier, Some (Identifier.Study.fileNameFromIdentifier this.Identifier)
        let assays = 
            arcAssays |> Option.defaultValue (this.GetRegisteredAssaysOrIdentifier())
            |> List.ofSeq |> List.map (fun a -> a.ToAssay())
        Study.create(
            ?FileName = fileName,
            ?Identifier = identifier,
            ?Title = this.Title,
            ?Description = this.Description,
            ?SubmissionDate = this.SubmissionDate,
            ?PublicReleaseDate = this.PublicReleaseDate,
            ?Publications = (this.Publications |> List.ofArray |> Option.fromValueWithDefault []),
            ?Contacts = (this.Contacts |> List.ofArray |> Option.fromValueWithDefault []),
            ?StudyDesignDescriptors = (this.StudyDesignDescriptors |> List.ofArray |> Option.fromValueWithDefault []),
            ?Protocols = protocols,
            ?Materials = studyMaterials,
            ?ProcessSequence = (processSeq |> Option.fromValueWithDefault []),
            ?Assays = (assays |> Option.fromValueWithDefault []),
            ?Factors = (this.Factors |> List.ofArray |> Option.fromValueWithDefault []),
            ?CharacteristicCategories = (ProcessSequence.getCharacteristics processSeq |> Option.fromValueWithDefault []),
            ?UnitCategories = (ProcessSequence.getUnits processSeq |> Option.fromValueWithDefault []),
            ?Comments = (this.Comments |> List.ofArray |> Option.fromValueWithDefault [])
            )

    // Create an ArcStudy from an ISA Json Study.
    static member fromStudy (s : Study) : (ArcStudy * ResizeArray<ArcAssay>) = 
        let tables = (s.ProcessSequence |> Option.map (ArcTables.fromProcesses >> fun t -> t.Tables))
        let identifer = 
            match s.FileName with
            | Some fn -> Identifier.Study.identifierFromFileName fn
            | None -> Identifier.createMissingIdentifier()
        let assays = s.Assays |> Option.map (List.map ArcAssay.fromAssay >> ResizeArray) |> Option.defaultValue (ResizeArray())
        let assaysIdentifiers = assays |> Seq.map (fun a -> a.Identifier) |> ResizeArray
        ArcStudy.create(
            identifer,
            ?title = s.Title,
            ?description = s.Description,
            ?submissionDate = s.SubmissionDate,
            ?publicReleaseDate = s.PublicReleaseDate,
            ?publications = (s.Publications |> Option.map Array.ofList),
            ?contacts = (s.Contacts|> Option.map Array.ofList),
            ?studyDesignDescriptors = (s.StudyDesignDescriptors |> Option.map Array.ofList),
            ?tables = tables,
            ?registeredAssayIdentifiers = Some assaysIdentifiers,
            ?factors = (s.Factors |> Option.map Array.ofList),
            ?comments = (s.Comments |> Option.map Array.ofList)
            ),
        assays

    member this.StructurallyEquals (other: ArcStudy) : bool =
        let i = this.Identifier = other.Identifier
        let t = this.Title = other.Title
        let d = this.Description = other.Description
        let sd = this.SubmissionDate = other.SubmissionDate
        let prd = this.PublicReleaseDate = other.PublicReleaseDate 
        let pub = Aux.compareSeq this.Publications other.Publications
        let con = Aux.compareSeq this.Contacts other.Contacts
        let sdd = Aux.compareSeq this.StudyDesignDescriptors other.StudyDesignDescriptors
        let tables = Aux.compareSeq this.Tables other.Tables
        let reg_tables = Aux.compareSeq this.RegisteredAssayIdentifiers other.RegisteredAssayIdentifiers
        let factors = Aux.compareSeq this.Factors other.Factors
        let comments = Aux.compareSeq this.Comments other.Comments
        // Todo maybe add reflection check to prove that all members are compared?
        [|i; t; d; sd; prd; pub; con; sdd; tables; reg_tables; factors; comments|] |> Seq.forall (fun x -> x = true)

    /// <summary>
    /// Use this function to check if this ArcStudy and the input ArcStudy refer to the same object.
    ///
    /// If true, updating one will update the other due to mutability.
    /// </summary>
    /// <param name="other">The other ArcStudy to test for reference.</param>
    member this.ReferenceEquals (other: ArcStudy) = System.Object.ReferenceEquals(this,other)

    // custom check
    override this.Equals other =
        match other with
        | :? ArcStudy as s -> 
            this.StructurallyEquals(s)
        | _ -> false

    override this.GetHashCode() = 
        [|
            box this.Identifier
            Aux.HashCodes.boxHashOption this.Title
            Aux.HashCodes.boxHashOption this.Description
            Aux.HashCodes.boxHashOption this.SubmissionDate
            Aux.HashCodes.boxHashOption this.PublicReleaseDate
            Aux.HashCodes.boxHashArray this.Publications
            Aux.HashCodes.boxHashArray this.Contacts
            Aux.HashCodes.boxHashArray this.StudyDesignDescriptors
            Array.ofSeq >> Aux.HashCodes.boxHashArray <| this.Tables
            Array.ofSeq >> Aux.HashCodes.boxHashArray <| this.RegisteredAssayIdentifiers
            Aux.HashCodes.boxHashArray this.Factors
            Aux.HashCodes.boxHashArray this.Comments
        |]
        |> Aux.HashCodes.boxHashArray 
        |> fun x -> x :?> int

[<AttachMembers>]
type ArcInvestigation(identifier : string, ?title : string, ?description : string, ?submissionDate : string, ?publicReleaseDate : string, ?ontologySourceReferences : OntologySourceReference [], ?publications : Publication [], ?contacts : Person [], ?assays : ResizeArray<ArcAssay>, ?studies : ResizeArray<ArcStudy>, ?registeredStudyIdentifiers : ResizeArray<string>, ?comments : Comment [], ?remarks : Remark []) as this = 

    let ontologySourceReferences = defaultArg ontologySourceReferences [||]
    let publications = defaultArg publications [||]
    let contacts = defaultArg contacts [||]
    let assays = 
        let ass = defaultArg assays (ResizeArray())
        for a in ass do 
            a.Investigation <- Some this
        ass
    let studies = 
        let sss = defaultArg studies (ResizeArray())
        for s in sss do 
            s.Investigation <- Some this
        sss
    let registeredStudyIdentifiers = defaultArg registeredStudyIdentifiers (ResizeArray())
    let comments = defaultArg comments [||]
    let remarks = defaultArg remarks [||]

    let mutable identifier = identifier
    let mutable title : string option = title
    let mutable description : string option = description
    let mutable submissionDate : string option = submissionDate
    let mutable publicReleaseDate : string option = publicReleaseDate
    let mutable ontologySourceReferences : OntologySourceReference [] = ontologySourceReferences
    let mutable publications : Publication [] = publications
    let mutable contacts : Person [] = contacts
    let mutable assays : ResizeArray<ArcAssay> = assays
    let mutable studies : ResizeArray<ArcStudy> = studies
    let mutable registeredStudyIdentifiers : ResizeArray<string> = registeredStudyIdentifiers
    let mutable comments : Comment [] = comments
    let mutable remarks : Remark [] = remarks
    /// Must be unique in one investigation
    member this.Identifier with get() = identifier and internal set(i) = identifier <- i
    member this.Title with get() = title and set(n) = title <- n
    member this.Description with get() = description and set(n) = description <- n
    member this.SubmissionDate with get() = submissionDate and set(n) = submissionDate <- n
    member this.PublicReleaseDate with get() = publicReleaseDate and set(n) = publicReleaseDate <- n
    member this.OntologySourceReferences with get() = ontologySourceReferences and set(n) = ontologySourceReferences <- n
    member this.Publications with get() = publications and set(n) = publications <- n
    member this.Contacts with get() = contacts and set(n) = contacts <- n
    member this.Assays with get() : ResizeArray<ArcAssay> = assays and set(n) = assays <- n
    member this.Studies with get() : ResizeArray<ArcStudy> = studies and set(n) = studies <- n
    member this.RegisteredStudyIdentifiers with get() = registeredStudyIdentifiers and set(n) = registeredStudyIdentifiers <- n
    member this.Comments with get() = comments and set(n) = comments <- n
    member this.Remarks with get() = remarks and set(n) = remarks <- n

    static member FileName = ARCtrl.Path.InvestigationFileName

    static member init(identifier: string) = ArcInvestigation identifier
    static member create(identifier : string, ?title : string, ?description : string, ?submissionDate : string, ?publicReleaseDate : string, ?ontologySourceReferences : OntologySourceReference [], ?publications : Publication [], ?contacts : Person [], ?assays : ResizeArray<ArcAssay>, ?studies : ResizeArray<ArcStudy>,?registeredStudyIdentifiers : ResizeArray<string>, ?comments : Comment [], ?remarks : Remark []) = 
        ArcInvestigation(identifier, ?title = title, ?description = description, ?submissionDate = submissionDate, ?publicReleaseDate = publicReleaseDate, ?ontologySourceReferences = ontologySourceReferences, ?publications = publications, ?contacts = contacts, ?assays = assays, ?studies = studies, ?registeredStudyIdentifiers = registeredStudyIdentifiers, ?comments = comments, ?remarks = remarks)

    static member make (identifier : string) (title : string option) (description : string option) (submissionDate : string option) (publicReleaseDate : string option) (ontologySourceReferences : OntologySourceReference []) (publications : Publication []) (contacts : Person []) (assays: ResizeArray<ArcAssay>) (studies : ResizeArray<ArcStudy>) (registeredStudyIdentifiers : ResizeArray<string>) (comments : Comment []) (remarks : Remark []) : ArcInvestigation =
        ArcInvestigation(identifier, ?title = title, ?description = description, ?submissionDate = submissionDate, ?publicReleaseDate = publicReleaseDate, ontologySourceReferences = ontologySourceReferences, publications = publications, contacts = contacts, assays = assays, studies = studies, registeredStudyIdentifiers = registeredStudyIdentifiers, comments = comments, remarks = remarks)

    member this.AssayCount 
        with get() = this.Assays.Count

    member this.AssayIdentifiers 
        with get(): string [] = this.Assays |> Seq.map (fun (x:ArcAssay) -> x.Identifier) |> Array.ofSeq

    member this.UnregisteredAssays 
        with get(): ResizeArray<ArcAssay> = 
            this.Assays 
            |> ResizeArray.filter (fun a ->
                this.RegisteredStudies
                |> Seq.exists (fun s -> 
                    Seq.exists (fun i -> i = a.Identifier) s.RegisteredAssayIdentifiers
                )
                |> not
            )

    // - Assay API - CRUD //
    member this.AddAssay(assay: ArcAssay) =
        ArcTypesAux.SanityChecks.validateUniqueAssayIdentifier assay.Identifier (this.Assays |> Seq.map (fun x -> x.Identifier))
        assay.Investigation <- Some(this)
        this.Assays.Add(assay)

    static member addAssay(assay: ArcAssay) =
        fun (inv:ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.AddAssay(assay)
            newInvestigation

    // - Assay API - CRUD //
    member this.InitAssay(assayIdentifier: string) =
        let assay = ArcAssay(assayIdentifier)
        this.AddAssay(assay)
        assay

    static member initAssay(assayIdentifier: string) =
        fun (inv:ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.InitAssay(assayIdentifier)

    // - Assay API - CRUD //
    /// <summary>
    /// Removes assay at specified index from ArcInvestigation without deregistering it from studies.
    /// </summary>
    /// <param name="index"></param>
    member this.DeleteAssayAt(index: int) =
        this.Assays.RemoveAt(index)

    // - Assay API - CRUD //
    /// <summary>
    /// Removes assay at specified index from ArcInvestigation without deregistering it from studies.
    /// </summary>
    /// <param name="index"></param>
    static member deleteAssayAt(index: int) =
        fun (inv: ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.DeleteAssayAt(index)
            newInvestigation

    // - Assay API - CRUD //
    /// <summary>
    /// Removes assay with given identifier from ArcInvestigation without deregistering it from studies.
    /// </summary>
    /// <param name="index"></param>
    member this.DeleteAssay(assayIdentifier: string) =
        let index = this.GetAssayIndex(assayIdentifier)
        this.DeleteAssayAt(index)

    // - Assay API - CRUD //
    /// <summary>
    /// Removes assay with given identifier from ArcInvestigation without deregistering it from studies.
    /// </summary>
    /// <param name="index"></param>
    static member deleteAssay(assayIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let newInv = inv.Copy()
            newInv.DeleteAssay(assayIdentifier)
            newInv


    // - Assay API - CRUD //
    /// <summary>
    /// Removes assay at specified index from ArcInvestigation and deregisteres it from all studies.
    /// </summary>
    /// <param name="index"></param>
    member this.RemoveAssayAt(index: int) =
        let ident = this.GetAssayAt(index).Identifier
        this.Assays.RemoveAt(index)
        for study in this.Studies do
            study.DeregisterAssay(ident)

    /// <summary>
    /// Removes assay at specified index from ArcInvestigation and deregisteres it from all studies.
    /// </summary>
    /// <param name="index"></param>
    static member removeAssayAt(index: int) =
        fun (inv: ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.RemoveAssayAt(index)
            newInvestigation

    // - Assay API - CRUD //
    /// <summary>
    /// Removes assay with specified identifier from ArcInvestigation and deregisteres it from all studies.
    /// </summary>
    /// <param name="index"></param>
    member this.RemoveAssay(assayIdentifier: string) =
        let index = this.GetAssayIndex(assayIdentifier)
        this.RemoveAssayAt(index)

    // - Assay API - CRUD //
    /// <summary>
    /// Removes assay with specified identifier from ArcInvestigation and deregisteres it from all studies.
    /// </summary>
    /// <param name="index"></param>
    static member removeAssay(assayIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let newInv = inv.Copy()
            newInv.RemoveAssay(assayIdentifier)
            newInv

    // - Assay API - CRUD //
    member this.SetAssayAt(index: int, assay: ArcAssay) =
        ArcTypesAux.SanityChecks.validateUniqueAssayIdentifier assay.Identifier (this.Assays |> Seq.removeAt index |> Seq.map (fun a -> a.Identifier))
        assay.Investigation <- Some(this)
        this.Assays.[index] <- assay
        this.DeregisterMissingAssays()

    static member setAssayAt(index: int, assay: ArcAssay) =
        fun (inv:ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.SetAssayAt(index, assay)
            newInvestigation

        // - Assay API - CRUD //
    member this.SetAssay(assayIdentifier: string, assay: ArcAssay) =
        let index = this.GetAssayIndex(assayIdentifier)
        this.SetAssayAt(index, assay)

    static member setAssay(assayIdentifier: string, assay: ArcAssay) =
        fun (inv:ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.SetAssay(assayIdentifier, assay)
            newInvestigation

    // - Assay API - CRUD //
    member this.GetAssayIndex(assayIdentifier: string) =
        let index = this.Assays.FindIndex (fun a -> a.Identifier = assayIdentifier)
        if index = -1 then failwith $"Unable to find assay with specified identifier '{assayIdentifier}'!"
        index

    static member getAssayIndex(assayIdentifier: string) : ArcInvestigation -> int =
        fun (inv: ArcInvestigation) -> inv.GetAssayIndex(assayIdentifier)

    // - Assay API - CRUD //
    member this.GetAssayAt(index: int) : ArcAssay =
        this.Assays.[index]

    static member getAssayAt(index: int) : ArcInvestigation -> ArcAssay =
        fun (inv: ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.GetAssayAt(index)

    // - Assay API - CRUD //
    member this.GetAssay(assayIdentifier: string) : ArcAssay =
        match this.TryGetAssay(assayIdentifier) with
        | Some a -> a
        | None -> failwith (ArcTypesAux.ErrorMsgs.unableToFindAssayIdentifier assayIdentifier this.Identifier)

    static member getAssay(assayIdentifier: string) : ArcInvestigation -> ArcAssay =
        fun (inv: ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.GetAssay(assayIdentifier)

    // - Assay API - CRUD //
    member this.TryGetAssay(assayIdentifier: string) : ArcAssay option =
        Seq.tryFind (fun a -> a.Identifier = assayIdentifier) this.Assays

    static member tryGetAssay(assayIdentifier: string) : ArcInvestigation -> ArcAssay option =
        fun (inv: ArcInvestigation) ->
            let newInvestigation = inv.Copy()
            newInvestigation.TryGetAssay(assayIdentifier)
    
    member this.ContainsAssay(assayIdentifier: string) =
        this.Assays
        |> Seq.exists (fun a -> a.Identifier = assayIdentifier)

    static member containsAssay (assayIdentifier: string) : ArcInvestigation -> bool =
        fun (inv: ArcInvestigation) ->            
            inv.ContainsAssay(assayIdentifier)

    /// Returns the count of registered study *identifiers*. This is not necessarily the same as the count of registered studies, as not all identifiers correspond to an existing study object.
    member this.RegisteredStudyIdentifierCount 
        with get() = this.RegisteredStudyIdentifiers.Count

    /// Returns all studies registered in this investigation, that correspond to an existing study object investigation.
    member this.RegisteredStudies 
        with get() : ResizeArray<ArcStudy> = 
            this.RegisteredStudyIdentifiers 
            |> ResizeArray.choose (fun identifier -> this.TryGetStudy identifier)

    /// Returns the count of registered studies. This is not necessarily the same as the count of registered study *identifiers*, as not all identifiers correspond to an existing study object.
    member this.RegisteredStudyCount 
        with get() = this.RegisteredStudies.Count

    /// Returns all registered study identifiers that do not correspond to an existing study object in the investigation.
    member this.VacantStudyIdentifiers
        with get() = 
            this.RegisteredStudyIdentifiers 
            |> ResizeArray.filter (this.ContainsStudy >> not)

    member this.StudyCount 
        with get() = this.Studies.Count

    member this.StudyIdentifiers
        with get() = this.Studies |> Seq.map (fun (x:ArcStudy) -> x.Identifier) |> Seq.toArray

    member this.UnregisteredStudies 
        with get() = 
            this.Studies 
            |> ResizeArray.filter (fun s -> 
                this.RegisteredStudyIdentifiers
                |> Seq.exists ((=) s.Identifier)
                |> not
            )

    // - Study API - CRUD //
    member this.AddStudy(study: ArcStudy) =
        ArcTypesAux.SanityChecks.validateUniqueStudyIdentifier study this.Studies
        study.Investigation <- Some this
        this.Studies.Add(study)

    static member addStudy(study: ArcStudy) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy.AddStudy(study)
            copy

    // - Study API - CRUD //
    member this.InitStudy (studyIdentifier: string) =
        let study = ArcStudy.init(studyIdentifier)
        this.AddStudy(study)
        study

    static member initStudy(studyIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy,copy.InitStudy(studyIdentifier)

    // - Study API - CRUD //
    member this.RegisterStudy (studyIdentifier : string) = 
        ArcTypesAux.SanityChecks.validateExistingStudyRegisterInInvestigation studyIdentifier this.StudyIdentifiers 
        ArcTypesAux.SanityChecks.validateUniqueRegisteredStudyIdentifiers studyIdentifier this.RegisteredStudyIdentifiers       
        this.RegisteredStudyIdentifiers.Add(studyIdentifier)

    static member registerStudy(studyIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy.RegisterStudy(studyIdentifier)
            copy

    // - Study API - CRUD //
    member this.AddRegisteredStudy (study: ArcStudy) = 
        this.AddStudy study
        this.RegisterStudy(study.Identifier)

    static member addRegisteredStudy(study: ArcStudy) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            let study = study.Copy()
            copy.AddRegisteredStudy(study)
            copy

    /// <summary>
    /// Removes study at specified index from ArcInvestigation without deregistering it.
    /// </summary>
    /// <param name="index"></param>
    member this.DeleteStudyAt(index: int) =
        this.Studies.RemoveAt(index)

    /// <summary>
    /// Removes study at specified index from ArcInvestigation without deregistering it.
    /// </summary>
    /// <param name="index"></param>
    static member deleteStudyAt(index: int) =
        fun (i: ArcInvestigation) ->
            let copy = i.Copy()
            copy.DeleteStudyAt(index)
            copy

    /// <summary>
    /// Removes study with specified identifier from ArcInvestigation without deregistering it.
    /// </summary>
    /// <param name="studyIdentifier"></param>
    member this.DeleteStudy(studyIdentifier: string) =
        let index = this.Studies.FindIndex(fun s -> s.Identifier = studyIdentifier)
        this.DeleteStudyAt(index)

    /// <summary>
    /// Removes study with specified identifier from ArcInvestigation without deregistering it.
    /// </summary>
    /// <param name="studyIdentifier"></param>
    static member deleteStudy(studyIdentifier: string) =
        fun (i: ArcInvestigation) ->
            let copy = i.Copy()
            copy.DeleteStudy studyIdentifier
            copy

    // - Study API - CRUD //
    /// <summary>
    /// Removes study at specified index from ArcInvestigation and deregisteres it.
    /// </summary>
    /// <param name="index"></param>
    member this.RemoveStudyAt(index: int) =
        let ident = this.GetStudyAt(index).Identifier
        this.Studies.RemoveAt(index)
        this.DeregisterStudy(ident)

    // - Study API - CRUD //
    /// <summary>
    /// Removes study at specified index from ArcInvestigation and deregisteres it.
    /// </summary>
    /// <param name="index"></param>
    static member removeStudyAt(index: int) =
        fun (inv: ArcInvestigation) ->
            let newInv = inv.Copy()
            newInv.RemoveStudyAt(index)
            newInv

    // - Study API - CRUD //
    /// <summary>
    /// Removes study with specified identifier from ArcInvestigation and deregisteres it.
    /// </summary>
    /// <param name="studyIdentifier"></param>
    member this.RemoveStudy(studyIdentifier: string) =
        let index = this.GetStudyIndex(studyIdentifier)
        this.RemoveStudyAt(index)

    /// <summary>
    /// Removes study with specified identifier from ArcInvestigation and deregisteres it.
    /// </summary>
    /// <param name="studyIdentifier"></param>
    static member removeStudy(studyIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy.RemoveStudy(studyIdentifier)
            copy

    // - Study API - CRUD //
    member this.SetStudyAt(index: int, study: ArcStudy) =
        ArcTypesAux.SanityChecks.validateUniqueStudyIdentifier study (this.Studies |> Seq.removeAt index)
        study.Investigation <- Some this
        this.Studies.[index] <- study

    static member setStudyAt(index: int, study: ArcStudy) =
        fun (inv: ArcInvestigation) ->
            let newInv = inv.Copy()
            newInv.SetStudyAt(index, study)
            newInv

    // - Study API - CRUD //
    member this.SetStudy(studyIdentifier: string, study: ArcStudy) =
        let index = this.GetStudyIndex studyIdentifier
        this.SetStudyAt(index,study)

    static member setStudy(studyIdentifier: string, study: ArcStudy) =
        fun (inv: ArcInvestigation) ->
            let newInv = inv.Copy()
            newInv.SetStudy(studyIdentifier, study)
            newInv

    // - Study API - CRUD //
    member this.GetStudyIndex(studyIdentifier: string) : int =
        let index = this.Studies.FindIndex (fun s -> s.Identifier = studyIdentifier)
        if index = -1 then failwith $"Unable to find study with specified identifier '{studyIdentifier}'!"
        index

    // - Study API - CRUD //
    static member getStudyIndex(studyIdentifier: string) : ArcInvestigation -> int =
        fun (inv: ArcInvestigation) -> inv.GetStudyIndex (studyIdentifier)

    // - Study API - CRUD //
    member this.GetStudyAt(index: int) : ArcStudy =
        this.Studies.[index]

    static member getStudyAt(index: int) : ArcInvestigation -> ArcStudy =
        fun (inv: ArcInvestigation) ->
            let newInv = inv.Copy()
            newInv.GetStudyAt(index)

    // - Study API - CRUD //
    member this.GetStudy(studyIdentifier: string) : ArcStudy =
        match this.TryGetStudy studyIdentifier with
        | Some s -> s
        | None -> failwith (ArcTypesAux.ErrorMsgs.unableToFindStudyIdentifier studyIdentifier this.Identifier)

    static member getStudy(studyIdentifier: string) : ArcInvestigation -> ArcStudy =
        fun (inv: ArcInvestigation) ->
            let newInv = inv.Copy()
            newInv.GetStudy(studyIdentifier)

    member this.TryGetStudy(studyIdentifier: string) : ArcStudy option =
        this.Studies |> Seq.tryFind (fun s -> s.Identifier = studyIdentifier)
        
    static member tryGetStudy(studyIdentifier : string) : ArcInvestigation -> ArcStudy option = 
        fun (inv: ArcInvestigation) -> 
            let newInv = inv.Copy()
            newInv.TryGetStudy(studyIdentifier)

    member this.ContainsStudy(studyIdentifier: string) =
        this.Studies
        |> Seq.exists (fun s -> s.Identifier = studyIdentifier)

    static member containsStudy (studyIdentifier: string) : ArcInvestigation -> bool =
        fun (inv: ArcInvestigation) ->            
            inv.ContainsStudy(studyIdentifier)

    // - Study API - CRUD //
    /// <summary>
    /// Register an existing assay from ArcInvestigation.Assays to a existing study.
    /// </summary>
    /// <param name="studyIdentifier"></param>
    /// <param name="assay"></param>
    member this.RegisterAssayAt(studyIndex: int, assayIdentifier: string) =
        let study = this.GetStudyAt(studyIndex)
        ArcTypesAux.SanityChecks.validateAssayRegisterInInvestigation assayIdentifier (this.Assays |> Seq.map (fun a -> a.Identifier))
        ArcTypesAux.SanityChecks.validateUniqueAssayIdentifier assayIdentifier study.RegisteredAssayIdentifiers
        study.RegisterAssay(assayIdentifier)

    static member registerAssayAt(studyIndex: int, assayIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy.RegisterAssayAt(studyIndex, assayIdentifier)
            copy

    // - Study API - CRUD //
    /// <summary>
    /// Register an existing assay from ArcInvestigation.Assays to a existing study.
    /// </summary>
    /// <param name="studyIdentifier"></param>
    /// <param name="assay"></param>
    member this.RegisterAssay(studyIdentifier: string, assayIdentifier: string) =
        let index = this.GetStudyIndex(studyIdentifier)
        this.RegisterAssayAt(index, assayIdentifier)

    static member registerAssay(studyIdentifier: string, assayIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy.RegisterAssay(studyIdentifier, assayIdentifier)
            copy

    // - Study API - CRUD //
    member this.DeregisterAssayAt(studyIndex: int, assayIdentifier: string) =
        let study = this.GetStudyAt(studyIndex)
        study.DeregisterAssay(assayIdentifier)

    static member deregisterAssayAt(studyIndex: int, assayIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy.DeregisterAssayAt(studyIndex, assayIdentifier)
            copy

    // - Study API - CRUD //
    member this.DeregisterAssay(studyIdentifier: string, assayIdentifier: string) =
        let index = this.GetStudyIndex(studyIdentifier)
        this.DeregisterAssayAt(index, assayIdentifier)

    static member deregisterAssay(studyIdentifier: string, assayIdentifier: string) =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy.DeregisterAssay(studyIdentifier, assayIdentifier)
            copy

    member this.DeregisterStudy(studyIdentifier: string) =
        this.RegisteredStudyIdentifiers.Remove(studyIdentifier) |> ignore

    static member deregisterStudy(studyIdentifier: string) =
        fun (i: ArcInvestigation) ->
            let copy = i.Copy()
            copy.DeregisterStudy(studyIdentifier)
            copy

    /// <summary>
    /// Returns all fully distinct Contacts/Performers from assays/studies/investigation. 
    /// </summary>
    member this.GetAllPersons() : Person [] =
        let persons = ResizeArray()
        for a in this.Assays do
            persons.AddRange(a.Performers)
        for s in this.Studies do
            persons.AddRange(s.Contacts)
        persons.AddRange(this.Contacts)
        persons
        |> Array.ofSeq
        |> Array.distinct

    /// <summary>
    /// Returns all fully distinct Contacts/Performers from assays/studies/investigation unfiltered. 
    /// </summary>
    member this.GetAllPublications() : Publication [] =
        let pubs = ResizeArray()
        for s in this.Studies do
            pubs.AddRange(s.Publications)
        pubs.AddRange(this.Publications)
        pubs
        |> Array.ofSeq
        |> Array.distinct

    // - Study API - CRUD //
    /// <summary>
    /// Deregisters assays not found in ArcInvestigation.Assays from all studies.
    /// </summary>
    member this.DeregisterMissingAssays() =
        ArcTypesAux.removeMissingRegisteredAssays this

    static member deregisterMissingAssays() =
        fun (inv: ArcInvestigation) ->
            let copy = inv.Copy()
            copy.DeregisterMissingAssays()
            copy
    
    /// Updates the IOtypes of the IO columns (Input, Output) across all tables in the investigation if possible.
    ///
    /// If an entity (Row Value of IO Column) with the same name as an entity with a higher IOType specifity is found, the IOType of the entity with the lower IOType specificity is updated.
    ///
    /// E.g. In Table1, there is a column "Output [Sample Name]" with an entity "Sample1". In Table2, there is a column "Input [Source Name]" with the same entity "Sample1". By equality of the entities, the IOType of the Input column in Table2 is inferred to be Sample, resulting in "Input [Sample Name]".
    ///
    /// E.g. RawDataFile is more specific than Source, but less specific than DerivedDataFile.
    ///
    /// E.g. Sample is equally specific to RawDataFile.
    member this.UpdateIOTypeByEntityID() =
        let ioMap = 
            [
                for study in this.Studies do
                    yield! study.Tables
                for assay in this.Assays do
                    yield! assay.Tables
            ]
            |> ResizeArray
            |> ArcTablesAux.getIOMap
        for study in this.Studies do
            ArcTablesAux.applyIOMap ioMap study.Tables
        for assay in this.Assays do
            ArcTablesAux.applyIOMap ioMap assay.Tables          

    member this.Copy() : ArcInvestigation =
        let nextAssays = ResizeArray()
        let nextStudies = ResizeArray()
        for assay in this.Assays do
            let copy = assay.Copy()
            nextAssays.Add(copy)
        for study in this.Studies do
            let copy = study.Copy()
            nextStudies.Add(copy)
        let nextComments = this.Comments |> Array.map (fun c -> c.Copy())
        let nextRemarks = this.Remarks |> Array.map (fun c -> c.Copy())
        let nextContacts = this.Contacts |> Array.map (fun c -> c.Copy())
        let nextPublications = this.Publications |> Array.map (fun c -> c.Copy())
        let nextOntologySourceReferences = this.OntologySourceReferences |> Array.map (fun c -> c.Copy())
        let nextStudyIdentifiers = ResizeArray(this.RegisteredStudyIdentifiers)
        let i = ArcInvestigation(
            this.Identifier,
            ?title = this.Title,
            ?description = this.Description,
            ?submissionDate = this.SubmissionDate,
            ?publicReleaseDate = this.PublicReleaseDate,
            studies = nextStudies,
            assays = nextAssays,
            registeredStudyIdentifiers = nextStudyIdentifiers,
            ontologySourceReferences = nextOntologySourceReferences,
            publications = nextPublications,
            contacts = nextContacts,
            comments = nextComments,
            remarks = nextRemarks
        )
        i

    ///// <summary>
    ///// Updates given investigation with another investigation, Identifier will never be updated. By default update is full replace. Optional Parameters can be used to specify update logic.
    ///// </summary>
    ///// <param name="investigation">The investigation used for updating this investigation.</param>
    ///// <param name="onlyReplaceExisting">If true, this will only update fields which are `Some` or non-empty lists. Default: **false**</param>
    ///// <param name="appendSequences">If true, this will append lists instead of replacing. Will return only distinct elements. Default: **false**</param>
    //member this.UpdateBy(inv:ArcInvestigation,?onlyReplaceExisting : bool,?appendSequences : bool) =
    //    let onlyReplaceExisting = defaultArg onlyReplaceExisting false
    //    let appendSequences = defaultArg appendSequences false
    //    let updateAlways = onlyReplaceExisting |> not
    //    if inv.Title.IsSome || updateAlways then 
    //        this.Title <- inv.Title
    //    if inv.Description.IsSome || updateAlways then 
    //        this.Description <- inv.Description
    //    if inv.SubmissionDate.IsSome || updateAlways then 
    //        this.SubmissionDate <- inv.SubmissionDate
    //    if inv.PublicReleaseDate.IsSome || updateAlways then 
    //        this.PublicReleaseDate <- inv.PublicReleaseDate
    //    if inv.OntologySourceReferences.Length <> 0 || updateAlways then
    //        let s = ArcTypesAux.updateAppendArray appendSequences this.OntologySourceReferences inv.OntologySourceReferences
    //        this.OntologySourceReferences <- s
    //    if inv.Publications.Length <> 0 || updateAlways then
    //        let s = ArcTypesAux.updateAppendArray appendSequences this.Publications inv.Publications
    //        this.Publications <- s
    //    if inv.Contacts.Length <> 0 || updateAlways then
    //        let s = ArcTypesAux.updateAppendArray appendSequences this.Contacts inv.Contacts
    //        this.Contacts <- s
    //    if inv.Assays.Count <> 0 || updateAlways then
    //        let s = ArcTypesAux.updateAppendResizeArray appendSequences this.Assays inv.Assays
    //        this.Assays <- s
    //    if inv.Studies.Count <> 0 || updateAlways then
    //        let s = ArcTypesAux.updateAppendResizeArray appendSequences this.Studies inv.Studies
    //        this.Studies <- s
    //    if inv.RegisteredStudyIdentifiers.Count <> 0 || updateAlways then
    //        let s = ArcTypesAux.updateAppendResizeArray appendSequences this.RegisteredStudyIdentifiers inv.RegisteredStudyIdentifiers
    //        this.RegisteredStudyIdentifiers <- s
    //    if inv.Comments.Length <> 0 || updateAlways then
    //        let s = ArcTypesAux.updateAppendArray appendSequences this.Comments inv.Comments
    //        this.Comments <- s
    //    if inv.Remarks.Length <> 0 || updateAlways then
    //        let s = ArcTypesAux.updateAppendArray appendSequences this.Remarks inv.Remarks
    //        this.Remarks <- s

    /// Transform an ArcInvestigation to an ISA Json Investigation.
    member this.ToInvestigation() : Investigation = 
        let studies = this.RegisteredStudies |> Seq.toList |> List.map (fun a -> a.ToStudy()) |> Option.fromValueWithDefault []
        let identifier =
            if ARCtrl.ISA.Identifier.isMissingIdentifier this.Identifier then None
            else Some this.Identifier
        Investigation.create(
            FileName = ARCtrl.Path.InvestigationFileName,
            ?Identifier = identifier,
            ?Title = this.Title,
            ?Description = this.Description,
            ?SubmissionDate = this.SubmissionDate,
            ?PublicReleaseDate = this.PublicReleaseDate,
            ?Publications = (this.Publications |> List.ofArray |> Option.fromValueWithDefault []),
            ?Contacts = (this.Contacts |> List.ofArray |> Option.fromValueWithDefault []),
            ?Studies = studies,
            ?Comments = (this.Comments |> List.ofArray |> Option.fromValueWithDefault [])
            )

    // Create an ArcInvestigation from an ISA Json Investigation.
    static member fromInvestigation (i : Investigation) : ArcInvestigation = 
        let identifer = 
            match i.Identifier with
            | Some i -> i
            | None -> Identifier.createMissingIdentifier()
        let studiesRaw, assaysRaw = 
            i.Studies 
            |> Option.defaultValue []
            |> List.map ArcStudy.fromStudy
            |> List.unzip
        let studies = ResizeArray(studiesRaw)
        let studyIdentifiers = studiesRaw |> Seq.map (fun a -> a.Identifier) |> ResizeArray
        let assays = assaysRaw |> Seq.concat |> Seq.distinctBy (fun a -> a.Identifier) |> ResizeArray
        let i = ArcInvestigation.create(
            identifer,
            ?title = i.Title,
            ?description = i.Description,
            ?submissionDate = i.SubmissionDate,
            ?publicReleaseDate = i.PublicReleaseDate,
            ?publications = (i.Publications |> Option.map Array.ofList),
            studies = studies,
            assays = assays,
            registeredStudyIdentifiers = studyIdentifiers,
            ?contacts = (i.Contacts |> Option.map Array.ofList),            
            ?comments = (i.Comments |> Option.map Array.ofList)
            )      
        i

    member this.StructurallyEquals (other: ArcInvestigation) : bool =
        let i = this.Identifier = other.Identifier
        let t = this.Title = other.Title
        let d = this.Description = other.Description
        let sd = this.SubmissionDate = other.SubmissionDate
        let prd = this.PublicReleaseDate = other.PublicReleaseDate 
        let pub = Aux.compareSeq this.Publications other.Publications
        let con = Aux.compareSeq this.Contacts other.Contacts
        let osr = Aux.compareSeq this.OntologySourceReferences other.OntologySourceReferences
        let assays = Aux.compareSeq this.Assays other.Assays
        let studies = Aux.compareSeq this.Studies other.Studies
        let reg_studies = Aux.compareSeq this.RegisteredStudyIdentifiers other.RegisteredStudyIdentifiers
        let comments = Aux.compareSeq this.Comments other.Comments
        let remarks = Aux.compareSeq this.Remarks other.Remarks
        // Todo maybe add reflection check to prove that all members are compared?
        [|i; t; d; sd; prd; pub; con; osr; assays; studies; reg_studies; comments; remarks|] |> Seq.forall (fun x -> x = true)

    /// <summary>
    /// Use this function to check if this ArcInvestigation and the input ArcInvestigation refer to the same object.
    ///
    /// If true, updating one will update the other due to mutability.
    /// </summary>
    /// <param name="other">The other ArcInvestigation to test for reference.</param>
    member this.ReferenceEquals (other: ArcStudy) = System.Object.ReferenceEquals(this,other)

    // custom check
    override this.Equals other =
        match other with
        | :? ArcInvestigation as i -> 
            this.StructurallyEquals(i)
        | _ -> false

    override this.GetHashCode() = 
        [|
            box this.Identifier
            Aux.HashCodes.boxHashOption this.Title
            Aux.HashCodes.boxHashOption this.Description
            Aux.HashCodes.boxHashOption this.SubmissionDate
            Aux.HashCodes.boxHashOption this.PublicReleaseDate
            Aux.HashCodes.boxHashArray this.Publications
            Aux.HashCodes.boxHashArray this.Contacts
            Aux.HashCodes.boxHashArray this.OntologySourceReferences
            Array.ofSeq >> Aux.HashCodes.boxHashArray <| this.Assays
            Array.ofSeq >> Aux.HashCodes.boxHashArray <| this.Studies
            Array.ofSeq >> Aux.HashCodes.boxHashArray <| this.RegisteredStudyIdentifiers
            Aux.HashCodes.boxHashArray this.Comments
            Aux.HashCodes.boxHashArray this.Remarks
        |]
        |> Aux.HashCodes.boxHashArray 
        |> fun x -> x :?> int