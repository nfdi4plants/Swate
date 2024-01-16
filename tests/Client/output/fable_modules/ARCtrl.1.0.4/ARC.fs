namespace ARCtrl

open ARCtrl.FileSystem
open ARCtrl.Contract
open ARCtrl.ISA
open ARCtrl.ISA.Spreadsheet
open FsSpreadsheet
open Fable.Core

module ARCAux =

    // No idea where to move this
    let getArcAssaysFromContracts (contracts: Contract []) = 
        contracts 
        |> Array.choose ArcAssay.tryFromReadContract
        

    // No idea where to move this
    let getArcStudiesFromContracts (contracts: Contract []) =
        contracts 
        |> Array.choose ArcStudy.tryFromReadContract

    let getArcInvestigationFromContracts (contracts: Contract []) =
        contracts 
        |> Array.choose ArcInvestigation.tryFromReadContract
        |> Array.exactlyOne 

    let updateFSByISA (isa : ArcInvestigation option) (fs : FileSystem) = 
        let (studyNames,assayNames) = 
            match isa with
            | Some inv ->         
                inv.StudyIdentifiers |> Seq.toArray, inv.AssayIdentifiers |> Seq.toArray
            | None -> ([||],[||])
        let assays = FileSystemTree.createAssaysFolder (assayNames |> Array.map FileSystemTree.createAssayFolder)
        let studies = FileSystemTree.createStudiesFolder (studyNames |> Array.map FileSystemTree.createStudyFolder)
        let investigation = FileSystemTree.createInvestigationFile()
        let tree = 
            FileSystemTree.createRootFolder [|investigation;assays;studies|]
            |> FileSystem.create
        fs.Union(tree)

    let updateFSByCWL (cwl : CWL.CWL option) (fs : FileSystem) =       
        let workflows = FileSystemTree.createWorkflowsFolder [||]
        let runs = FileSystemTree.createRunsFolder [||]       
        let tree = 
            FileSystemTree.createRootFolder [|workflows;runs|]
            |> FileSystem.create
        fs.Union(tree)    

[<AttachMembers>]
type ARC(?isa : ISA.ArcInvestigation, ?cwl : CWL.CWL, ?fs : FileSystem.FileSystem) =

    let mutable _isa = isa
    let mutable _cwl = cwl
    let mutable _fs = 
        fs
        |> Option.defaultValue (FileSystem.create(FileSystemTree.Folder ("",[||])))
        |> ARCAux.updateFSByISA isa
        |> ARCAux.updateFSByCWL cwl

    member this.ISA 
        with get() = _isa
        and set(newISA : ArcInvestigation option) =
            _isa <- newISA
            this.UpdateFileSystem()

    member this.CWL 
        with get() = cwl

    member this.FileSystem 
        with get() = _fs
        and set(fs) = _fs <- fs

    member this.RemoveAssay(assayIdentifier: string) =
        let isa = 
            match this.ISA with
            | Some i -> i
            | None -> failwith "Cannot remove assay from null ISA value."
        let assay = isa.GetAssay(assayIdentifier)
        let studies = assay.StudiesRegisteredIn
        isa.RemoveAssay(assayIdentifier)
        let paths = this.FileSystem.Tree.ToFilePaths()
        let assayFolderPath = Path.getAssayFolderPath(assayIdentifier)
        let filteredPaths = paths |> Array.filter (fun p -> p.StartsWith(assayFolderPath) |> not)
        this.SetFilePaths(filteredPaths)      
        [
            assay.ToDeleteContract()
            isa.ToUpdateContract()
            for s in studies do
                s.ToUpdateContract()
        ]
        |> ResizeArray

    member this.RemoveStudy(studyIdentifier: string) =
        let isa = 
            match this.ISA with
            | Some i -> i
            | None -> failwith "Cannot remove study from null ISA value."
        isa.RemoveStudy(studyIdentifier)
        let paths = this.FileSystem.Tree.ToFilePaths()
        let studyFolderPath = Path.getStudyFolderPath(studyIdentifier)
        let filteredPaths = paths |> Array.filter (fun p -> p.StartsWith(studyFolderPath) |> not)
        this.SetFilePaths(filteredPaths)
        [
            Contract.createDelete(studyFolderPath) // isa.GetStudy(studyIdentifier).ToDeleteContract()
            isa.ToUpdateContract()
        ]
        |> ResizeArray

    //static member updateISA (isa : ISA.Investigation) (arc : ARC) : ARC =
    //    raise (System.NotImplementedException())

    //static member updateCWL (cwl : CWL.CWL) (arc : ARC) : ARC =
    //    raise (System.NotImplementedException())

    //static member updateFileSystem (fileSystem : FileSystem.FileSystem) (arc : ARC) : ARC =
    //    raise (System.NotImplementedException())

    //static member addFile (path : string) (arc : ARC) : ARC =
    //    FileSystem.addFile |> ignore
    //    ARC.updateFileSystem |> ignore
    //    raise (System.NotImplementedException())

    //static member addFolder (path : string) (arc : ARC) : ARC =
    //    FileSystem.addFolder |> ignore
    //    ARC.updateFileSystem |> ignore
    //    raise (System.NotImplementedException())

    //static member addFolders (paths : string array) (arc : ARC) : ARC =
    //    paths
    //    |> Array.fold (fun arc path -> ARC.addFolder path arc) arc |> ignore
    //    raise (System.NotImplementedException())

    ///// Add folder to ARC.FileSystem and add .gitkeep file to the folder
    //static member addEmptyFolder (path : string) (arc : ARC) : ARC =
    //    FileSystem.addFolder  |> ignore   
    //    FileSystem.addFile (Path.combine path Path.gitKeepFileName) |> ignore
    //    ARC.updateFileSystem |> ignore
    //    raise (System.NotImplementedException())

    //static member addEmptyFolders (paths : string array) (arc : ARC) : ARC =
    //    paths
    //    |> Array.fold (fun arc path -> ARC.addEmptyFolder path arc) arc |> ignore
    //    raise (System.NotImplementedException())


    /// Add assay folder to the ARC.FileSystem and update the ARC.ISA with the new assay metadata
    //static member addAssay (assay : ISA.ArcAssay) (studyIdentifier : string) (arc : ARC) : ARC = 
  
        // - Contracts - //
        // create spreadsheet assays/AssayName/isa.assay.xlsx  
        // create text assays/AssayName/dataset/.gitkeep 
        // create text assays/AssayName/dataset/Readme.md
        // create text assays/AssayName/protocols/.gitkeep 
        // update spreadsheet isa.investigation.xlsx

        // - ISA - //
        //let assayFolderPath = Path.combineAssayFolderPath assay
        //let assaySubFolderPaths = Path.combineAssaySubfolderPaths assay
        //let assayReadmeFilePath = Path.combine assayFolderPath Path.assayReadmeFileName
        //let updatedInvestigation = ArcInvestigation.addAssay assay studyIdentifier arc.ISA
        //arc
        // - FileSystem - //
        //// Create assay root folder in ARC.FileSystem
        //ARC.addFolder assayFolderPath arc 
        //// Create assay subfolders in ARC.FileSystem
        //|> ARC.addEmptyFolders assaySubFolderPaths 
        // Create assay readme file in ARC.FileSystem
        //ARC.addFile assayReadmeFilePath 
        //// Update ARC.ISA with the updated investigation
        //|> ARC.updateISA updatedInvestigation 

    // to-do: we need a function that generates only create contracts from a ARC data model. 
    // reason: contracts are initially designed to sync disk with in-memory model while working on the arc.
    // but we need a way to create an arc programmatically and then write it to disk.

    static member fromFilePaths (filePaths : string array) : ARC = 
        let fs : FileSystem = FileSystem.fromFilePaths filePaths
        ARC(fs=fs)

    member this.SetFilePaths (filePaths : string array) =
        let tree = FileSystemTree.fromFilePaths filePaths
        _fs <- {_fs with Tree = tree}

    //// Maybe add forceReplace flag?
    //member this.SetFSFromFilePaths (filePaths : string array) = 
    //    let newFS : FileSystem = FileSystem.fromFilePaths filePaths
    //    fs <- Some newFS

    // to-do: function that returns read contracts based on a list of paths.
    member this.GetReadContracts () =
        _fs.Tree.ToFilePaths() |> Array.choose Contract.ARC.tryISAReadContractFromPath 

    /// <summary>
    /// This function creates the ARC-model from fullfilled READ contracts. The necessary READ contracts can be created with `ARC.getReadContracts`.
    /// </summary>
    /// <param name="cArr">The fullfilled READ contracts.</param>
    /// <param name="enableLogging">If this flag is set true, the function will print any missing/found assays/studies to the console. *Default* = false</param>
    member this.SetISAFromContracts (contracts: Contract []) =
        /// get investigation from xlsx
        let investigation = ARCAux.getArcInvestigationFromContracts contracts
        /// get studies from xlsx
        let studies = ARCAux.getArcStudiesFromContracts contracts |> Array.map fst
        /// get assays from xlsx
        let assays = ARCAux.getArcAssaysFromContracts contracts

        // Remove Assay metadata objects read from investigation file from investigation object, if no assosiated assay file exists
        investigation.AssayIdentifiers
        |> Array.iter (fun ai -> 
            if assays |> Array.exists (fun a -> a.Identifier = ai) |> not then
                investigation.DeleteAssay(ai)      
        )

        // Remove Study metadata objects read from investigation file from investigation object, if no assosiated study file exists
        investigation.StudyIdentifiers
        |> Array.iter (fun si -> 
            if studies |> Array.exists (fun s -> s.Identifier = si) |> not then
                investigation.DeleteStudy(si)
        )

        studies |> Array.iter (fun study ->
            /// Try find registered study in parsed READ contracts
            let registeredStudyOpt = investigation.Studies |> Seq.tryFind (fun s -> s.Identifier = study.Identifier)
            match registeredStudyOpt with
            | Some registeredStudy -> 
                registeredStudy.UpdateReferenceByStudyFile(study,true)
            | None -> 
                investigation.AddStudy(study)
        )
        assays |> Array.iter (fun assay ->
            /// Try find registered study in parsed READ contracts
            let registeredAssayOpt = investigation.Assays |> Seq.tryFind (fun a -> a.Identifier = assay.Identifier)
            match registeredAssayOpt with
            | Some registeredAssay -> // This study element is parsed from FsWorkbook and has no regsitered assays, yet
                registeredAssay.UpdateReferenceByAssayFile(assay,true)
            | None -> 
                investigation.AddAssay(assay)
            let assay = investigation.Assays |> Seq.find (fun a -> a.Identifier = assay.Identifier)
            let updatedTables = 
                assay.StudiesRegisteredIn
                |> Array.fold (fun tables study -> 
                    ArcTables.updateReferenceTablesBySheets(ArcTables(study.Tables),tables,false)
                ) (ArcTables(assay.Tables))
            assay.Tables <- updatedTables.Tables
        )
        this.ISA <- Some investigation

    member this.UpdateFileSystem() =   
        let newFS = 
            ARCAux.updateFSByISA _isa _fs
            |> ARCAux.updateFSByCWL _cwl
        _fs <- newFS        

    /// <summary>
    /// This function returns the all write Contracts for the current state of the ARC. ISA contracts do contain the object data as spreadsheets, while the other contracts only contain the path.
    /// </summary>  
    member this.GetWriteContracts (?isLight: bool) =
        let isLight = defaultArg isLight true
        /// Map containing the DTOTypes and objects for the ISA objects.
        let workbooks = System.Collections.Generic.Dictionary<string, DTOType*FsWorkbook>()
        match this.ISA with
        | Some inv -> 
            let investigationConverter = if isLight then ISA.Spreadsheet.ArcInvestigation.toLightFsWorkbook else ISA.Spreadsheet.ArcInvestigation.toFsWorkbook
            workbooks.Add (Path.InvestigationFileName, (DTOType.ISA_Investigation, investigationConverter inv))
            inv.Studies
            |> Seq.iter (fun s ->
                workbooks.Add (
                    Identifier.Study.fileNameFromIdentifier s.Identifier,
                    (DTOType.ISA_Study, ArcStudy.toFsWorkbook s)
                )
            )
            inv.Assays
            |> Seq.iter (fun a ->
                workbooks.Add (
                    Identifier.Assay.fileNameFromIdentifier a.Identifier,
                    (DTOType.ISA_Assay, ISA.Spreadsheet.ArcAssay.toFsWorkbook a))                
            )
            
        | None -> 
            //printfn "ARC contains no ISA part."
            workbooks.Add (Path.InvestigationFileName, (DTOType.ISA_Investigation, ISA.Spreadsheet.ArcInvestigation.toLightFsWorkbook (ArcInvestigation.create(Identifier.MISSING_IDENTIFIER))))

        // Iterates over filesystem and creates a write contract for every file. If possible, include DTO.       
        _fs.Tree.ToFilePaths(true)
        |> Array.map (fun fp ->
            match Dictionary.tryGet fp workbooks with
            | Some (dto,wb) -> Contract.createCreate(fp,dto,DTO.Spreadsheet wb)
            | None -> Contract.createCreate(fp, DTOType.PlainText)
        )

    member this.GetGitInitContracts(?branch : string,?repositoryAddress : string,?defaultGitignore : bool) = 
        let defaultGitignore = defaultArg defaultGitignore false
        [|
            Contract.Git.Init.createInitContract(?branch = branch)
            if defaultGitignore then Contract.Git.gitignoreContract
            if repositoryAddress.IsSome then Contract.Git.Init.createAddRemoteContract repositoryAddress.Value
        |]

    static member getCloneContract(remoteUrl : string,?merge : bool ,?branch : string,?token : string*string,?nolfs : bool) =
        Contract.Git.Clone.createCloneContract(remoteUrl,?merge = merge,?branch = branch,?token = token,?nolfs = nolfs)

    member this.Copy() = 
        let isaCopy = _isa |> Option.map (fun i -> i.Copy())
        let fsCopy = _fs.Copy()
        new ARC(?isa = isaCopy, ?cwl = _cwl, fs = fsCopy)
        
    /// <summary>
    /// Returns the FileSystemTree of the ARC with only the registered files and folders included.
    /// </summary>
    /// <param name="IgnoreHidden">Wether or not to ignore hidden files and folders starting with '.'. If true, no hidden files are included in the result. (default: true)</param>
    member this.GetRegisteredPayload(?IgnoreHidden:bool) =

        let isaCopy = _isa |> Option.map (fun i -> i.Copy()) // not sure if needed, but let's be safe

        let registeredStudies =     
            isaCopy
            |> Option.map (fun isa -> isa.Studies.ToArray()) // to-do: isa.RegisteredStudies
            |> Option.defaultValue [||]
        
        let registeredAssays =
            registeredStudies
            |> Array.map (fun s -> s.RegisteredAssays.ToArray()) // to-do: s.RegisteredAssays
            |> Array.concat

        let includeRootFiles : Set<string> = 
            set [
                Path.InvestigationFileName
                Path.READMEFileName
            ]

        let includeStudyFiles = 
            registeredStudies
            |> Array.map (fun s -> 
                let studyFoldername = $"{Path.StudiesFolderName}/{s.Identifier}"

                set [
                    yield $"{studyFoldername}/{Path.StudyFileName}"
                    yield $"{studyFoldername}/{Path.READMEFileName}"

                    //just allow any constructed path from cell values. there may be occasions where this includes wrong files, but its good enough for now.
                    for table in s.Tables do
                        for kv in table.Values do
                            let textValue = kv.Value.ToFreeTextCell().AsFreeText
                            yield textValue // from arc root
                            yield $"{studyFoldername}/{Path.StudiesResourcesFolderName}/{textValue}" // from study root > resources
                            yield $"{studyFoldername}/{Path.StudiesProtocolsFolderName}/{textValue}" // from study root > protocols
                ]
            )
            |> Set.unionMany

        let includeAssayFiles = 
            registeredAssays
            |> Array.map (fun a -> 
                let assayFoldername = $"{Path.AssaysFolderName}/{a.Identifier}"

                set [
                    yield $"{assayFoldername}/{Path.AssayFileName}"
                    yield $"{assayFoldername}/{Path.READMEFileName}"

                    //just allow any constructed path from cell values. there may be occasions where this includes wrong files, but its good enough for now.
                    for table in a.Tables do
                        for kv in table.Values do
                            let textValue = kv.Value.ToFreeTextCell().AsFreeText
                            yield textValue // from arc root
                            yield $"{assayFoldername}/{Path.AssayDatasetFolderName}/{textValue}" // from assay root > dataset
                            yield $"{assayFoldername}/{Path.AssayProtocolsFolderName}/{textValue}" // from assay root > protocols
                ]
            )
            |> Set.unionMany


        let includeFiles = Set.unionMany [includeRootFiles; includeStudyFiles; includeAssayFiles]

        let ignoreHidden = defaultArg IgnoreHidden true
        let fsCopy = _fs.Copy() // not sure if needed, but let's be safe

        fsCopy.Tree
        |> FileSystemTree.toFilePaths()
        |> Array.filter (fun p -> 
            p.StartsWith(Path.WorkflowsFolderName) 
            || p.StartsWith(Path.RunsFolderName) 
            || includeFiles.Contains(p)
        )
        |> FileSystemTree.fromFilePaths
        |> fun tree -> if ignoreHidden then tree |> FileSystemTree.filterFiles (fun n -> not (n.StartsWith("."))) else Some tree
        |> Option.bind (fun tree -> if ignoreHidden then tree |> FileSystemTree.filterFolders (fun n -> not (n.StartsWith("."))) else Some tree)
        |> Option.defaultValue (FileSystemTree.fromFilePaths [||])

    /// <summary>
    /// Returns the FileSystemTree of the ARC with only and folders included that are considered additional payload.
    /// </summary>
    /// <param name="IgnoreHidden">Wether or not to ignore hidden files and folders starting with '.'. If true, no hidden files are included in the result. (default: true)</param>

    member this.GetAdditionalPayload(?IgnoreHidden:bool) =
        let ignoreHidden = defaultArg IgnoreHidden true
        let registeredPayload = 
            this.GetRegisteredPayload()
            |> FileSystemTree.toFilePaths()
            |> set

        _fs.Copy().Tree
        |> FileSystemTree.toFilePaths()
        |> Array.filter (fun p -> not (registeredPayload.Contains(p)))
        |> FileSystemTree.fromFilePaths
        |> fun tree -> if ignoreHidden then tree |> FileSystemTree.filterFiles (fun n -> not (n.StartsWith("."))) else Some tree
        |> Option.bind (fun tree -> if ignoreHidden then tree |> FileSystemTree.filterFolders (fun n -> not (n.StartsWith("."))) else Some tree)
        |> Option.defaultValue (FileSystemTree.fromFilePaths [||])
        
    static member DefaultContracts = Map<string,Contract> [|
        ARCtrl.Contract.Git.gitignoreFileName, ARCtrl.Contract.Git.gitignoreContract
    |]

//-Pseudo code-//
//// Option 1
//let fs, readcontracts = ARC.FSFromFilePaths filepaths
//let isa = ARC.ISAFromContracts fullfilled_readcontracts
//let cwl = ARC.CWLFromXXX XXX
//ARC.create(fs, isa, cwl)

//// Option 2
//let arc = ARC.fromFilePaths // returned ARC
//let contracts = arc.getREADContracts // retunred READ
//arc.updateFromContracts fullfilled_contracts //returned ARC
