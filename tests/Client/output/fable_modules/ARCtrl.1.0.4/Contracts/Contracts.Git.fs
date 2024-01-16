module ARCtrl.Contract.Git

open ARCtrl.Contract

let [<Literal>] git = @"git"  
let [<Literal>] defaultBranch = @"main" 
let [<Literal>] gitignoreFileName = @".gitignore" 

let gitWithArgs(arguments : string []) = CLITool.create(git,arguments)

let createGitContractAt path arguments = Contract.createExecute(gitWithArgs(arguments),path)

let createGitContract(arguments) = Contract.createExecute(gitWithArgs(arguments))

let gitignoreContract = Contract.createCreate(gitignoreFileName,DTOType.PlainText,DTO.Text ARCtrl.FileSystem.DefaultGitignore.dgi)

type Init = 

    static member init = "init" 
    static member branchFlag = "-b"

    static member remote = @"remote"
    static member add = @"add"
    static member origin = @"origin"

    static member createInitContract(?branch : string) =
        let branch = Option.defaultValue defaultBranch branch
        createGitContract([|Init.init;Init.branchFlag;branch|])

    static member createAddRemoteContract(remoteUrl : string) =
        createGitContract([|Init.remote;Init.add;Init.origin;remoteUrl|])

and Clone = 

    static member clone = "clone" 

    static member branchFlag = "-b"

    static member noLFSConfig = "-c \"filter.lfs.smudge = git-lfs smudge --skip -- %f\" -c \"filter.lfs.process = git-lfs filter-process --skip\""

    static member formatRepoString username pass (url : string) = 
        let comb = username + ":" + pass + "@"
        url.Replace("https://","https://" + comb)

    static member createCloneContract(remoteUrl : string,?merge : bool ,?branch : string,?token : string*string,?nolfs : bool) =
        let nolfs = Option.defaultValue false nolfs
        let merge = Option.defaultValue false merge
        let remoteUrl = 
            match token with
            | Some (username,pass) -> Clone.formatRepoString username pass remoteUrl
            | None -> remoteUrl
        createGitContract([|
            Clone.clone
            if nolfs then Clone.noLFSConfig
            if branch.IsSome then Clone.branchFlag
            if branch.IsSome then branch.Value
            remoteUrl
            if merge then "."
        |])