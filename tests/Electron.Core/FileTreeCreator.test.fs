module ElectronCore.FileTreeCreatorTests

open System
open Fable.Core
open Fable.Core.JsInterop
open Main.Bindings.Path
open Vitest

module FileTreeCreator = Main.FileTreeCreator
module GitProvisioningService = Main.Git.GitProvisioningService
module GitService = Main.Git.GitService

let private fsPromisesDynamic: obj = importAll "fs/promises"
let private osDynamic: obj = importAll "os"
let private childProcessDynamic: obj = importAll "node:child_process"

let private fileTreeCreatorTestOptions = TestOptions(timeout = 20000)

let private normalizeSlashes (path: string) = path.Replace("\\", "/")

type private TempRepositoryContext = { RootPath: string; RepoPath: string }

let private createTempDirectoryAsync () : Fable.Core.JS.Promise<string> =
    let prefix =
        join [|
            osDynamic?tmpdir () |> unbox<string>
            "swate-electron-file-tree-"
        |]

    fsPromisesDynamic?mkdtemp (prefix) |> unbox<Fable.Core.JS.Promise<string>>

let private removeDirectoryAsync (path: string) : Fable.Core.JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?rm (path, createObj [ "recursive" ==> true; "force" ==> true ])
        |> unbox<Fable.Core.JS.Promise<obj>>

    return ()
}

let private writeUtf8FileAsync (path: string) (content: string) : Fable.Core.JS.Promise<unit> = promise {
    let! _ =
        fsPromisesDynamic?writeFile (path, content, "utf8")
        |> unbox<Fable.Core.JS.Promise<obj>>

    return ()
}

let private runGitAsync (repoPath: string) (args: string[]) : Fable.Core.JS.Promise<string> = promise {
    let! output =
        Fable.Core.JS.Constructors.Promise.Create(fun resolve reject ->
            childProcessDynamic?execFile (
                "git",
                args,
                createObj [
                    "cwd" ==> repoPath
                    "encoding" ==> "utf8"
                    "shell" ==> false
                ],
                fun (error: obj) (stdout: obj) (stderr: obj) ->
                    if error |> Option.ofObj |> Option.isSome then
                        let stderrText =
                            stderr |> Option.ofObj |> Option.map string |> Option.defaultValue String.Empty

                        let argsText = String.concat " " args
                        reject (exn $"git {argsText} failed: {stderrText}")
                    else
                        stdout
                        |> Option.ofObj
                        |> Option.map string
                        |> Option.defaultValue String.Empty
                        |> resolve
            )
            |> ignore
        )

    return output
}

let private expectGitOk<'T> (operationName: string) (result: GitService.GitResult<'T>) : 'T =
    match result with
    | Ok value -> value
    | Error failure -> failwith $"{operationName} failed ({failure.Kind}): {failure.Message}"

let private withTempRepository
    (testBody: TempRepositoryContext -> Fable.Core.JS.Promise<unit>)
    : Fable.Core.JS.Promise<unit> =
    promise {
        let! rootPath = createTempDirectoryAsync ()

        try
            let repoPath = join [| rootPath; "repo" |]
            let! initResult = GitProvisioningService.initRepository repoPath
            let normalizedRepoPath = expectGitOk "git init" initResult

            do!
                testBody {
                    RootPath = rootPath
                    RepoPath = normalizedRepoPath
                }

            do! removeDirectoryAsync rootPath
        with error ->
            do! removeDirectoryAsync rootPath
            return raise error
    }

Vitest.describe (
    "FileTreeCreator LFS metadata",
    fun () ->
        Vitest.test (
            "getFileEntries annotates staged LFS files from git lfs ls-files -j",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let gitattributesPath = join [| context.RepoPath; ".gitattributes" |]
                        let pointerFilePath = join [| context.RepoPath; "pointer.psd" |]
                        let downloadedFilePath = join [| context.RepoPath; "downloaded.psd" |]

                        do! writeUtf8FileAsync gitattributesPath "*.psd filter=lfs diff=lfs merge=lfs -text\n"
                        do! writeUtf8FileAsync pointerFilePath "Pointer-like content.\n"
                        do! writeUtf8FileAsync downloadedFilePath "Hydrated-like content.\n"

                        let! _ =
                            runGitAsync context.RepoPath [|
                                "add"
                                ".gitattributes"
                                "pointer.psd"
                                "downloaded.psd"
                            |]

                        ()

                        let! entries = FileTreeCreator.getFileEntries context.RepoPath

                        let pointerEntry =
                            entries
                            |> Microsoft.FSharp.Collections.Array.find (fun entry ->
                                normalizeSlashes entry.path = normalizeSlashes pointerFilePath
                            )

                        let downloadedEntry =
                            entries
                            |> Microsoft.FSharp.Collections.Array.find (fun entry ->
                                normalizeSlashes entry.path = normalizeSlashes downloadedFilePath
                            )

                        Vitest.expect(pointerEntry.lfs.IsSome).toBe (true)
                        Vitest.expect(downloadedEntry.lfs.IsSome).toBe (true)

                        let pointerLfs = pointerEntry.lfs |> Option.get
                        let downloadedLfs = downloadedEntry.lfs |> Option.get

                        Vitest.expect(pointerLfs.name).toBe ("pointer.psd")
                        Vitest.expect(pointerLfs.size).toBeGreaterThan (0)
                        Vitest.expect(pointerLfs.checkout).toBe (true)
                        Vitest.expect(pointerLfs.downloaded).toBe (true)
                        Vitest.expect(pointerLfs.``oid_type``).toBe ("sha256")
                        Vitest.expect(pointerLfs.oid.Length).toBe (64)
                        Vitest.expect(pointerLfs.version).toBe ("https://git-lfs.github.com/spec/v1")

                        Vitest.expect(downloadedLfs.name).toBe ("downloaded.psd")
                        Vitest.expect(downloadedLfs.size).toBeGreaterThan (0)
                        Vitest.expect(downloadedLfs.checkout).toBe (true)
                        Vitest.expect(downloadedLfs.downloaded).toBe (true)
                        Vitest.expect(downloadedLfs.``oid_type``).toBe ("sha256")
                        Vitest.expect(downloadedLfs.oid.Length).toBe (64)
                        Vitest.expect(downloadedLfs.version).toBe ("https://git-lfs.github.com/spec/v1")
                    })
            }
        )

        Vitest.test (
            "getFileEntryWithLfsMetadata enriches a single staged LFS file",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let gitattributesPath = join [| context.RepoPath; ".gitattributes" |]
                        let pointerFilePath = join [| context.RepoPath; "single-pointer.psd" |]

                        do! writeUtf8FileAsync gitattributesPath "*.psd filter=lfs diff=lfs merge=lfs -text\n"
                        do! writeUtf8FileAsync pointerFilePath "Single tracked content.\n"
                        let! _ = runGitAsync context.RepoPath [| "add"; ".gitattributes"; "single-pointer.psd" |]
                        ()

                        let! enrichedEntry =
                            FileTreeCreator.getFileEntryWithLfsMetadata context.RepoPath pointerFilePath

                        Vitest.expect(enrichedEntry.lfs.IsSome).toBe (true)
                        let lfsInfo = enrichedEntry.lfs |> Option.get

                        Vitest.expect(lfsInfo.name).toBe ("single-pointer.psd")
                        Vitest.expect(lfsInfo.checkout).toBe (true)
                        Vitest.expect(lfsInfo.``oid_type``).toBe ("sha256")
                    })
            }
        )

        Vitest.test (
            "files absent from ls-files -j keep lfs metadata None",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let gitattributesPath = join [| context.RepoPath; ".gitattributes" |]
                        let untrackedLfsPath = join [| context.RepoPath; "untracked.psd" |]

                        do! writeUtf8FileAsync gitattributesPath "*.psd filter=lfs diff=lfs merge=lfs -text\n"
                        do! writeUtf8FileAsync untrackedLfsPath "Untracked file content.\n"
                        let! _ = runGitAsync context.RepoPath [| "add"; ".gitattributes" |]
                        ()

                        let! enrichedEntry =
                            FileTreeCreator.getFileEntryWithLfsMetadata context.RepoPath untrackedLfsPath

                        Vitest.expect(enrichedEntry.lfs).toEqual (None)
                    })
            }
        )

        Vitest.test (
            "no LFS files (files=null) keeps entries without lfs metadata",
            fileTreeCreatorTestOptions,
            fun () -> promise {
                do!
                    withTempRepository (fun context -> promise {
                        let plainFilePath = join [| context.RepoPath; "plain.txt" |]
                        do! writeUtf8FileAsync plainFilePath "Plain text.\n"

                        let! entries = FileTreeCreator.getFileEntries context.RepoPath

                        let plainEntry =
                            entries
                            |> Array.find (fun entry -> normalizeSlashes entry.path = normalizeSlashes plainFilePath)

                        Vitest.expect(plainEntry.lfs).toEqual (None)
                    })
            }
        )
)
