module ARCtrl.FileSystemTree

open ARCtrl.FileSystem

let createGitKeepFile() = 
    FileSystemTree.createFile ARCtrl.Path.GitKeepFileName

let createReadmeFile() = 
    FileSystemTree.createFile ARCtrl.Path.READMEFileName

let createEmptyFolder (name : string) = 
    FileSystemTree.createFolder(name, [|createGitKeepFile()|])

let createAssayFolder(assayName : string) = 
    let dataset = createEmptyFolder ARCtrl.Path.AssayDatasetFolderName
    let protocols = createEmptyFolder ARCtrl.Path.AssayProtocolsFolderName
    let readme = createReadmeFile()
    let assayFile = FileSystemTree.createFile ARCtrl.Path.AssayFileName
    FileSystemTree.createFolder(assayName, [|dataset; protocols; assayFile; readme|])

let createStudyFolder(studyName : string) = 
    let resources = createEmptyFolder ARCtrl.Path.StudiesResourcesFolderName
    let protocols = createEmptyFolder ARCtrl.Path.StudiesProtocolsFolderName
    let readme = createReadmeFile()
    let studyFile = FileSystemTree.createFile ARCtrl.Path.StudyFileName
    FileSystemTree.createFolder(studyName, [|resources; protocols; studyFile; readme|])

let createInvestigationFile() = 
    FileSystemTree.createFile ARCtrl.Path.InvestigationFileName

let createAssaysFolder(assays : FileSystemTree array) =
    FileSystemTree.createFolder(
        ARCtrl.Path.AssaysFolderName, 
        Array.append [|createGitKeepFile()|] assays
    )

let createStudiesFolder(studies : FileSystemTree array) =
    FileSystemTree.createFolder(
        ARCtrl.Path.StudiesFolderName,
        Array.append [|createGitKeepFile()|] studies
    )

let createWorkflowsFolder(workflows : FileSystemTree array) =
    FileSystemTree.createFolder(
        ARCtrl.Path.WorkflowsFolderName, 
        Array.append [|createGitKeepFile()|] workflows
    )

let createRunsFolder(runs : FileSystemTree array) = 
    FileSystemTree.createFolder(
        ARCtrl.Path.RunsFolderName, 
        Array.append [|createGitKeepFile()|] runs
    )


