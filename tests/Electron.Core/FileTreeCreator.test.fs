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

let private fileTreeCreatorTestOptions = TestOptions(timeout = 20000)

[<Emit("Buffer.byteLength($0, 'utf8')")>]
let private utf8ByteLength (text: string) : int = jsNative

let private normalizeSlashes (path: string) = path.Replace("\\", "/")

type private TempRepositoryContext = {
    RootPath: string
    RepoPath: string
}

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
    let! _ = fsPromisesDynamic?writeFile (path, content, "utf8") |> unbox<Fable.Core.JS.Promise<obj>>
    return ()
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

Vitest.describe("FileTreeCreator LFS metadata", fun () ->
    Vitest.test (
        "getFileEntries marks LFS pointer and downloaded files with size metadata",
        fileTreeCreatorTestOptions,
        fun () -> promise {
            do!
                withTempRepository (fun context -> promise {
                    let pointerOid = String.replicate 64 "a"
                    let pointerObjectSizeBytes = 2_097_152
                    let pointerContent =
                        $"version https://git-lfs.github.com/spec/v1\noid sha256:{pointerOid}\nsize {pointerObjectSizeBytes}\n"
                    let downloadedContent = "Hydrated LFS content from working tree.\n"
                    let downloadedSizeBytes = utf8ByteLength downloadedContent |> float

                    let gitattributesPath = join [| context.RepoPath; ".gitattributes" |]
                    let pointerFilePath = join [| context.RepoPath; "pointer.psd" |]
                    let downloadedFilePath = join [| context.RepoPath; "downloaded.psd" |]

                    do! writeUtf8FileAsync gitattributesPath "*.psd filter=lfs diff=lfs merge=lfs -text\n"
                    do! writeUtf8FileAsync pointerFilePath pointerContent
                    do! writeUtf8FileAsync downloadedFilePath downloadedContent

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

                    Vitest.expect(pointerEntry.isLfs).toEqual(Some true)
                    Vitest.expect(pointerEntry.isLfsPointer).toEqual(Some true)
                    Vitest.expect(pointerEntry.downloaded).toEqual(Some false)
                    Vitest.expect(pointerEntry.lfsSizeBytes).toEqual(Some(float pointerObjectSizeBytes))

                    Vitest.expect(downloadedEntry.isLfs).toEqual(Some true)
                    Vitest.expect(downloadedEntry.isLfsPointer).toEqual(Some false)
                    Vitest.expect(downloadedEntry.downloaded).toEqual(Some true)
                    Vitest.expect(downloadedEntry.lfsSizeBytes).toEqual(Some downloadedSizeBytes)
                })
        }
    )

    Vitest.test (
        "getFileEntryWithLfsMetadata enriches a single tracked pointer file",
        fileTreeCreatorTestOptions,
        fun () -> promise {
            do!
                withTempRepository (fun context -> promise {
                    let pointerOid = String.replicate 64 "b"
                    let pointerObjectSizeBytes = 6_291_456
                    let pointerContent =
                        $"version https://git-lfs.github.com/spec/v1\noid sha256:{pointerOid}\nsize {pointerObjectSizeBytes}\n"

                    let gitattributesPath = join [| context.RepoPath; ".gitattributes" |]
                    let pointerFilePath = join [| context.RepoPath; "single-pointer.psd" |]

                    do! writeUtf8FileAsync gitattributesPath "*.psd filter=lfs diff=lfs merge=lfs -text\n"
                    do! writeUtf8FileAsync pointerFilePath pointerContent

                    let! enrichedEntry = FileTreeCreator.getFileEntryWithLfsMetadata context.RepoPath pointerFilePath

                    Vitest.expect(enrichedEntry.isLfs).toEqual(Some true)
                    Vitest.expect(enrichedEntry.isLfsPointer).toEqual(Some true)
                    Vitest.expect(enrichedEntry.downloaded).toEqual(Some false)
                    Vitest.expect(enrichedEntry.lfsSizeBytes).toEqual(Some(float pointerObjectSizeBytes))
                })
        }
    )
)
