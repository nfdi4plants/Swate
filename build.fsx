#r "paket:
nuget BlackFox.Fake.BuildTask
nuget Fake.Core.Target
nuget Fake.Core.Process
nuget Fake.Core.ReleaseNotes
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Paket
nuget Fake.DotNet.FSFormatting
nuget Fake.DotNet.Fsi
nuget Fake.DotNet.NuGet
nuget Fake.Api.Github
nuget Fake.DotNet.Testing.Expecto 
nuget Fake.Extensions.Release
nuget Fake.Tools.Git //
"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/dotnet/fsharp/issues/5216
#endif

open System
open System.Text
open Fake
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.Api
open Fake.Tools.Git

Target.initEnvironment ()

let sharedPath = Path.getFullName "./src/Shared"
let serverPath = Path.getFullName "./src/Server"
let deployDir = Path.getFullName "./deploy"
let clientPath = Path.getFullName "./src/Client"
let clientDeployPath = Path.combine clientPath "deploy"
let sharedTestsPath = Path.getFullName "./tests/Shared"
let serverTestsPath = Path.getFullName "./tests/Server"

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let npmTool = platformTool "npm" "npm.cmd"
let npxTool = platformTool "npx" "npx.cmd"
let dockerComposeTool = platformTool "docker-compose" "docker-compose.exe"

let currentDateString =
    let n = System.DateTime.Now
    sprintf "%i-%i-%i" n.Year n.Month n.Day

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let npm args workingDir =
    let npmPath =
        match ProcessUtils.tryFindFileOnPath "npm" with
        | Some path -> path
        | None ->
            "npm was not found in path. Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
            |> failwith

    let arguments = args |> String.split ' ' |> Arguments.OfArgs

    Command.RawCommand (npmPath, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let openBrowser url =
    //https://github.com/dotnet/corefx/issues/10361
    Command.ShellCommand url
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result = Fake.DotNet.DotNet.exec (Fake.DotNet.DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

Target.create "Clean" (fun _ ->
    [ deployDir
      clientDeployPath ]
    |> Shell.cleanDirs
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Npm version:"
    runTool npmTool "--version"  __SOURCE_DIRECTORY__
    runTool npmTool "install" __SOURCE_DIRECTORY__
)

Target.create "Build" (fun _ ->
    let release = ReleaseNotes.load "RELEASE_NOTES.md"
    runDotNet "build" serverPath
    Shell.regexReplaceInFileWithEncoding
        "let app = \".+\""
       ("let app = \"" + release.NugetVersion + "\"")
        System.Text.Encoding.UTF8
        (Path.combine clientPath "Version.fs")
    runTool npxTool "webpack-cli -p" __SOURCE_DIRECTORY__
)

Target.create "Run" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let client = async {
        runTool npxTool "webpack-dev-server" __SOURCE_DIRECTORY__
    }
    let browser = async {
        do! Async.Sleep 5000
        openBrowser "https://localhost:5000"
    }

    let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let tasks =
        [ if not safeClientOnly then yield server
          yield client
          if not vsCodeSession then yield browser ]

    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "StartMySqlDocker" (fun _ ->
    runTool dockerComposeTool "-f .db\docker-compose.yml up" __SOURCE_DIRECTORY__
)


Target.create "OfficeDebug" (fun _ ->
    let server = async {
        runDotNet "watch run" serverPath
    }
    let officeDebug = async {
         runTool npxTool "office-addin-debugging start manifest.xml desktop --debug-method web" __SOURCE_DIRECTORY__
    }
    let client = async {
        runTool npxTool "webpack-dev-server" __SOURCE_DIRECTORY__
    }

    let mySqlDocker = async {
        Trace.trace "Start MySql+Adminer Docker"
        runTool dockerComposeTool "-f .db\docker-compose.yml up" __SOURCE_DIRECTORY__
    }

    //let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let tasks =
        [
          yield officeDebug 
          yield client
          yield mySqlDocker
          if not safeClientOnly then yield server
          ]
    tasks
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "OfficeDebugRemote" (fun _ ->
   
    let officeDebug = async {
         runTool npxTool "office-addin-debugging start tests/manifest.xml desktop --debug-method web" __SOURCE_DIRECTORY__
    }

    officeDebug 
    |> Async.RunSynchronously
    |> ignore
)

Target.create "InstallOfficeAddinTooling" (fun _ ->

    printfn "Installing office addin tooling"

    runTool npmTool "install -g office-addin-dev-certs" __SOURCE_DIRECTORY__
    runTool npmTool "install -g office-addin-debugging" __SOURCE_DIRECTORY__
    runTool npmTool "install -g office-addin-manifest" __SOURCE_DIRECTORY__
)

Target.create "WebpackConfigSetup" (fun _ ->
    let userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

    Shell.replaceInFiles
        [
            "{USERFOLDER}",userPath.Replace("\\","/")
        ]
        [
            (Path.combine __SOURCE_DIRECTORY__ "webpack.config.js")
        ]

)

// https://fake.build/core-targets.html#Targets-with-arguments
Target.create "LocalConnectionStringSetup" (fun conf ->

    let pwOpt =
        conf.Context.Arguments
        |> List.tryFind (fun x -> x.StartsWith "pw:")

    let msg =
        if pwOpt.IsNone then "Default MySql password set" else "Custom MySql password set."

    let pw =
        if pwOpt.IsNone then "example" else pwOpt.Value.Replace("pw:","").Trim()

    Shell.replaceInFiles
        [
            "{PASSWORD}",pw
        ]
        [
            (Path.combine __SOURCE_DIRECTORY__ ".db/docker-compose.yml")
            (Path.combine __SOURCE_DIRECTORY__ "src/Server/dev.json")
        ]
    Trace.trace msg
)

Target.create "SetLoopbackExempt" (fun _ ->
    Command.RawCommand("CheckNetIsolation.exe",Arguments.ofList [
        "LoopbackExempt"
        "-a"
        "-n=\"microsoft.win32webviewhost_cw5n1h2txyewy\""
    ])
    |> CreateProcess.fromCommand
    |> Proc.run
    |> ignore
)


Target.create "CreateDevCerts" (fun _ ->
    runTool npxTool "office-addin-dev-certs install --days 365" __SOURCE_DIRECTORY__

    let certPath =
        Path.combine
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            ".office-addin-dev-certs/ca.crt"
        

    let psi = new System.Diagnostics.ProcessStartInfo(FileName = certPath, UseShellExecute = true)
    System.Diagnostics.Process.Start(psi) |> ignore
)

type SemVerRelease =
| Major
| Minor
| Patch
| WIP

type ReleaseNotesDescriptors =
| Additions
| Deletions
| Bugfixes

    /// | Additions -> "Additions:" | Deletions -> "Deletions:" | Bugfixes  -> "Bugfixes:"
    member this.toString =
        match this with
        | Additions -> "Additions:"
        | Deletions -> "Deletions:"
        | Bugfixes  -> "Bugfixes:"

    static member DescriptorList =
        [Additions.toString; Deletions.toString; Bugfixes.toString]

let createNewSemVer (semVerReleaseType:SemVerRelease) (newestCommitHash:string) (previousSemVer:SemVerInfo)=
    match semVerReleaseType with
    | Major ->
        sprintf "%i.0.0+%s" (previousSemVer.Major+1u) newestCommitHash
    | Minor ->
        sprintf "%i.%i.0+%s" (previousSemVer.Major) (previousSemVer.Minor+1u) newestCommitHash
    | Patch ->
        sprintf "%i.%i.%i+%s" (previousSemVer.Major) (previousSemVer.Minor) (previousSemVer.Patch+1u) newestCommitHash
    | WIP ->
        sprintf "%i.%i.%i+%s" (previousSemVer.Major) (previousSemVer.Minor) (previousSemVer.Patch) newestCommitHash

// This is later used to try and sort the commit messages to the three fields additions, bugs and deletions.
let rec sortCommitsByKeyWords (all:string list) (additions:string list) (deletions:string list) (bugs:string list) =
    let bugKeyWords = [|"bug"; "problem"; "fix"|] |> Array.map String.toLower
    let deleteKeyWords = [|"delete"; "remove"|] |> Array.map String.toLower
    let isHeadBugKeyWord (head:string) = Array.exists (fun (x:string) -> (String.toLower head).Contains x) bugKeyWords
    let isHeadDeleteKeyWord (head:string) = Array.exists (fun (x:string) -> (String.toLower head).Contains x) deleteKeyWords
    match all with
    | head::rest when isHeadBugKeyWord head
            -> sortCommitsByKeyWords rest additions deletions (head::bugs)
    | head::rest when isHeadDeleteKeyWord head
            -> sortCommitsByKeyWords rest additions (head::deletions) bugs
    | head::rest -> sortCommitsByKeyWords rest (head::additions) deletions bugs
    | head::[] when isHeadBugKeyWord head
        -> additions, deletions, (head::bugs)
    | head::[] when isHeadDeleteKeyWord head
        -> additions, (head::deletions), bugs
    | head::[]
        -> (head::additions), deletions, bugs
    | []
        -> additions, deletions, bugs
    |> fun (x,y,z) -> List.rev x, List.rev y, List.rev z  


let splitPreviousReleaseNotes releaseNotes =
    let addOpt = releaseNotes |> List.tryFindIndex (fun x -> x = Additions.toString)
    let deleteOpt = releaseNotes |> List.tryFindIndex (fun x -> x = Deletions.toString)
    let bugOpt = releaseNotes |> List.tryFindIndex (fun x -> x = Bugfixes.toString)
    let indList = [addOpt,Additions;deleteOpt,Deletions;bugOpt,Bugfixes] |> List.choose (fun (x,y) -> if x.IsSome then Some (x.Value, y) else None)
    let addedDescriptors =
        releaseNotes
        |> List.mapi (fun i x ->
            let descriptor = indList |> List.tryFindBack (fun (descInd,_) -> descInd <= i && ReleaseNotesDescriptors.DescriptorList |> List.contains x |> not)
            if descriptor.IsNone then None else Some (snd descriptor.Value,x)
        )
    let findCommitsByDescriptor descriptor (commitOptionList:(ReleaseNotesDescriptors*string) option list) =
        commitOptionList
        |> List.choose (fun x -> 
            if x.IsSome && fst x.Value = descriptor then Some (snd x.Value) else None
        )
        |> List.map (fun x -> sprintf "    * %s" x)
    let prevAdditions = 
        findCommitsByDescriptor Additions addedDescriptors
        // REMOVE this line as soon as parsing of semver metadata is fixed.
        |> List.filter (fun x -> x.StartsWith "    * latest commit #" |> not)
    let prevDeletions = findCommitsByDescriptor Deletions addedDescriptors
    let prevBugs = findCommitsByDescriptor Bugfixes addedDescriptors
    prevAdditions, prevDeletions, prevBugs

Target.create "IsExistingReleaseNotes" (fun _ ->
    let isExisting = Fake.IO.File.exists "RELEASE_NOTES.md"
    if isExisting = false then
        Fake.IO.File.create "RELEASE_NOTES.md"
        Fake.IO.File.write
            true
            "RELEASE_NOTES.md"
            [
                sprintf "### 0.0.0 (Released %s)" (currentDateString)
                "* Additions:"
                "    * Initial set up for RELEASE_Notes.md"
            ]
        Trace.traceImportant "RELEASE_Notes.md created"
    else
        Trace.trace "RELEASE_Notes.md found"
)   

Target.create "Release" (fun config ->

    let semVer =
        let opt =
            config.Context.Arguments
            |> List.map (fun x -> x.ToLower())
            |> List.tryFind (fun x -> x.StartsWith "semver:")
        match opt with
        | Some "semver:major"->
            Trace.trace "Increase major for next release notes."
            Major
        | Some "semver:minor" ->
            Trace.trace "Increase minor for next release notes."
            Minor
        | Some "semver:patch" ->
            Trace.trace "Increase patch for next release notes."
            Patch
        | Some "semver:wip" ->
            Trace.trace "Add new commits to current release."
            WIP
        | Some x ->
            Trace.traceError (sprintf "Unrecognized argument: \"%s\". Default to \"semver:wip\"." x)
            WIP
        | None ->
            Trace.trace "Add new commits to current release."
            WIP

    let nOfLastCommitsToCheck =
        let opt =
            config.Context.Arguments
            |> List.tryFind (fun x -> x.StartsWith "n:")
        if opt.IsSome then opt.Value.Replace("n:","") else "30"

    let prevReleaseNotes =
        Fake.IO.File.read "RELEASE_NOTES.md"

    let release = ReleaseNotes.load "RELEASE_NOTES.md"

    // REMOVE this line as soon as parsing of semver metadata is fixed.
    // This should be in release.SemVer.MetaData
    let (tryFindPreviousReleaseCommitHash: string option) =
        release.Notes
        |> List.tryFind (fun x -> x.TrimStart([|' ';'*'|]).StartsWith "latest commit #")
        |> fun x ->
            if x.IsSome then x.Value.Replace("latest commit ","") |> Some else None

    if tryFindPreviousReleaseCommitHash.IsSome then
        Trace.trace (sprintf "Found PreviousCommit: %s" tryFindPreviousReleaseCommitHash.Value)
    else
        Trace.traceError "Did not find previous Commit!"

    //https://git-scm.com/book/en/v2/Git-Basics-Viewing-the-Commit-History#pretty_format
    let allGitCommits =
        Fake.Tools.Git.CommandHelper.runGitCommand "" ("log -" + nOfLastCommitsToCheck + " --pretty=format:\"%H;%h;%s\"" )

    let cutCommitsAtPreviousReleaseCommit =
        allGitCommits
        |> fun (_,gitCommits,_) ->
            if tryFindPreviousReleaseCommitHash.IsSome then
                let indOpt =
                    gitCommits |> List.tryFindIndex (fun y -> y.Contains tryFindPreviousReleaseCommitHash.Value.[1..])
                let ind =
                    if indOpt.IsSome then
                        indOpt.Value
                    else
                        failwithf
                            "Could not find last version git hash: %s in the last %s commits.
                            You can increase the number of searched commits by passing a argument
                            as such \"dotnet fake build -t release n:50\""
                            tryFindPreviousReleaseCommitHash.Value nOfLastCommitsToCheck
                gitCommits
                |> List.take (ind)
            else
                gitCommits

    Trace.trace "Update RELEASE_NOTES.md"

    let writeNewReleaseNotes() =

        let commitNoteArr = cutCommitsAtPreviousReleaseCommit |> Array.ofList |> Array.map (fun x -> x.Split([|";"|],StringSplitOptions.None))
        // REMOVE this line as soon as parsing of semver metadata is fixed.
        // This should be in release.SemVer.MetaData
        let latestCommitHash =
            let newCommit = if tryFindPreviousReleaseCommitHash.IsSome then tryFindPreviousReleaseCommitHash.Value else ""
            if Array.isEmpty commitNoteArr then newCommit else sprintf "#%s" commitNoteArr.[0].[1]
        let newSemVer =
            createNewSemVer semVer latestCommitHash.[1..] release.SemVer
        /// This will be used to directly create the release notes
        let formattedCommitNoteList =
            commitNoteArr
            |> Array.filter (fun x ->
                match (String.toLower x.[2]) with
                | x when x.Contains "update release_notes.md" || x.Contains "update release notes" ->
                    false
                | _ -> true
            )
            |> Array.map (fun x ->
                sprintf "    * [[#%s](https://github.com/nfdi4plants/Swate/commit/%s)] %s" x.[1] x.[0] x.[2]
            )
            |> List.ofArray
        let additions, deletions, bugs = sortCommitsByKeyWords formattedCommitNoteList [] [] []

        let newNotes =
            if semVer <> WIP then
                [
                    sprintf "### %s (Released %s)" newSemVer currentDateString
                    // Additions will not need to be checked, as in the current version the latest commit hash needs to be th first entry here.
                    "* Additions:"
                    // REMOVE this line as soon as parsing of semver metadata is fixed.
                    sprintf "    * latest commit %s" latestCommitHash
                    yield! additions
                    if List.isEmpty deletions |> not then
                        "* Deletions:"
                        yield! deletions
                    if List.isEmpty bugs |> not then
                        "* Bugfixes:"
                        yield! bugs
                    ""
                    yield! prevReleaseNotes
                ]
            else
                let prevAdditions, prevDeletions, prevBugs =
                    splitPreviousReleaseNotes release.Notes
                let appendAdditions, appendDeletions, appendBugfixes =
                    additions@prevAdditions,deletions@prevDeletions,bugs@prevBugs
                let skipPrevVersionOfReleaseNotes =
                    let findInd =
                        prevReleaseNotes
                        |> Seq.indexed
                        |> Seq.choose (fun (i,x) -> if x.StartsWith "###" then Some i else None)
                        |> Seq.skip 1
                    if Seq.isEmpty findInd then 0 else Seq.head findInd
                [
                    sprintf "### %s (Released %s)" newSemVer currentDateString
                    if List.isEmpty appendAdditions |> not then
                        "* Additions:"
                        // REMOVE this line as soon as parsing of semver metadata is fixed.
                        sprintf "    * latest commit %s" latestCommitHash
                        yield! appendAdditions
                    if List.isEmpty appendDeletions |> not then
                        "* Deletions:"
                        yield! appendDeletions
                    if List.isEmpty appendBugfixes |> not then
                        "* Bugfixes:"
                        yield! appendBugfixes
                    ""
                    yield! (Seq.skip skipPrevVersionOfReleaseNotes prevReleaseNotes)
                ]


        Fake.IO.File.write
            false
            "RELEASE_NOTES.md"
            newNotes

    writeNewReleaseNotes()

    Trace.trace "Update RELEASE_NOTES.md done!"

    Trace.trace "Update Version.fs"

    let newRelease = ReleaseNotes.load "RELEASE_NOTES.md"

    let releaseDate =
        if newRelease.Date.IsSome then newRelease.Date.Value.ToShortDateString() else "WIP"

    Fake.DotNet.AssemblyInfoFile.createFSharp  "src/Server/Version.fs"
        [   Fake.DotNet.AssemblyInfo.Title "SWATE"
            Fake.DotNet.AssemblyInfo.Version newRelease.AssemblyVersion
            Fake.DotNet.AssemblyInfo.Metadata ("ReleaseDate",releaseDate)
        ]

    Trace.trace "Update Version.fs done!"

    /// Update maniefest.xmls

    Trace.trace "Update manifest.xml"

    let _ =
        let newVer = sprintf "<Version>%i.%i.%i</Version>" newRelease.SemVer.Major newRelease.SemVer.Minor newRelease.SemVer.Patch
        Shell.regexReplaceInFilesWithEncoding
            "<Version>[0-9]+.[0-9]+.[0-9]+</Version>"
            newVer
            Encoding.UTF8
            [
                (Path.combine __SOURCE_DIRECTORY__ @".assets\assets\manifest.xml")
                (Path.combine __SOURCE_DIRECTORY__ "manifest.xml")
            ]

    Trace.trace "Update manifest.xml done!"
)

Target.create "ZipAssets" (fun _ ->

    let assetPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__,@".assets\assets")
    let assetDir = Fake.IO.DirectoryInfo.ofPath assetPath

    let files =
        let assetsPaths = Fake.IO.DirectoryInfo.getFiles assetDir
        assetsPaths |> Array.map (fun x -> x.FullName)

    Zip.zip assetDir.FullName ".assets\swate.zip" files
)

Target.create "GithubDraft" (fun config ->

    let prevReleaseNotes =
        Fake.IO.File.read "RELEASE_NOTES.md"
    let takeLastOfReleaseNotes =
        let findInd =
            prevReleaseNotes
            |> Seq.indexed
            |> Seq.choose (fun (i,x) -> if x.StartsWith "###" then Some i else None)
        match Seq.length findInd with
        | 1 ->
            prevReleaseNotes
        | x when x >= 2 ->
            let indOfSecondLastRN = findInd|> Seq.item 1
            Seq.take (indOfSecondLastRN - 1) prevReleaseNotes
        | _ ->
            failwith "Previous RELEASE_NOTES.md not found or in wrong formatting"

    let bodyText =
        [
            ""
            "The latest release features:"
            yield! takeLastOfReleaseNotes
            ""
            "You can check our [release notes](https://github.com/nfdi4plants/Swate/blob/developer/RELEASE_NOTES.md) to see a list of all new features."
            "If you decide to test Swate in the current state, please take the time to set up a Github account to report your issues and suggestions here."
            ""
            "You can also search existing issues for solutions for your questions and/or discussions about your suggestions."
            ""
            "Here are the necessary steps to use SWATE:"
            ""
            "#### Using the Swate installer"
            "    - Documentation can be found here: [Swate installer](https://github.com/omaus/Swate_Install#swate-installer)"
            ""
            "#### If you use the excel desktop application locally:"
            "    - Install node.js LTS (needed for office add-in related tooling)"
            "    - Download the release archive (.zip file) below and extract it"
            "    - Execute the swate.cmd (windows) or swate.sh (macOS, you will need to make it executable via chmod a+x) script."
            ""
            "#### If you use Excel in the browser:"
            "    - Download the release archive (.zip file) below and extract it"
            "    - Launch Excel online, open a (blank) workbook"
            "    - Under the Insert tab, select Add-Ins"
            "    - Go to Manage my Add-Ins and select Upload my Add-In"
            "    - select and upload the manifest.xml file contained in the archive."
            ""
            ""
        ]

    let tokenOpt =
        config.Context.Arguments
        |> List.tryFind (fun x -> x.StartsWith "token:")

    let release = ReleaseNotes.load "RELEASE_NOTES.md"
    let semVer = (sprintf "v%i.%i.%i" release.SemVer.Major release.SemVer.Minor release.SemVer.Patch)

    let token =
        match Environment.environVarOrDefault "github_token" "", tokenOpt with
        | s, None when System.String.IsNullOrWhiteSpace s |> not -> s
        | s, Some token when System.String.IsNullOrWhiteSpace s |> not ->
            Trace.traceImportant "Environment variable for token and token argument found. Will proceed with token passed as argument 'token:my-github-token'"
            token.Replace("token:","")
        | _, Some token ->
            token.Replace("token:","")
        | _, None ->
            failwith "please set the github_token environment variable to a github personal access token with repro access or pass the github personal access token as argument as in 'dotnet fake build -target githubdraft token:my-github-token'."

    let files =
        let assetPath = System.IO.Path.Combine(__SOURCE_DIRECTORY__,@".assets")
        let assetDir = Fake.IO.DirectoryInfo.ofPath assetPath
        /// This only accesses files and not folders. So in this case it will only access the .zip file created by "ZipAssets"
        let assetsPaths = Fake.IO.DirectoryInfo.getFiles assetDir
        assetsPaths |> Array.map (fun x -> x.FullName)

    let gitOwner = "nfdi4plants"

    let gitName = "Swate"

    let _ =
        GitHub.createClientWithToken token
        |> GitHub.draftNewRelease gitOwner gitName semVer (release.SemVer.PreRelease <> None) bodyText
        |> GitHub.uploadFiles files
        |> Async.RunSynchronously

    Trace.trace "Draft successfully created!"
)


Target.create "Bundle" (fun _ ->
    runDotNet (sprintf "publish -c Release -o \"%s\"" deployDir) serverPath
    npm "run build" "."
)

Target.create "Bundle-Linux" (fun _ ->
    runDotNet (sprintf "publish -c Release -r linux-x64 -o \"%s\"" deployDir) serverPath
    npm "run build" "."
)

Target.create "Setup" ignore 

Target.create "RunTests" (fun _ ->
    runDotNet "build" sharedTestsPath
    [ async { runDotNet "watch run" serverTestsPath }
      async { npm "run test:live" "." } ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)

Target.create "testfake" (fun _ ->
    Trace.trace "Finish test"
)

open Fake.Core.TargetOperators

"Clean"
    ==> "InstallClient"
    ==> "Build"

"Clean"
    ==> "InstallClient"
    ==> "Build"
    ==> "Bundle"

"Clean"
    ==> "InstallClient"
    ==> "Build"
    ==> "Bundle-Linux"

"Clean"
    ==> "InstallClient"
    ==> "Run"

"Clean"
    ==> "InstallClient"
    ==> "OfficeDebug"

"IsExistingReleaseNotes"
    ==> "Release"

"ZipAssets"
    ==> "GithubDraft"

"InstallOfficeAddinTooling"
    ==> "WebpackConfigSetup"
    ==> "LocalConnectionStringSetup"
    ==> "CreateDevCerts"
    ==> "SetLoopbackExempt"
    ==> "Setup"

"Clean"
    ==> "InstallClient"
    ==> "RunTests"

Target.runOrDefaultWithArguments "Bundle"
