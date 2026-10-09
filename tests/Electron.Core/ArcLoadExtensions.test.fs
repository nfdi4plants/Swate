module ElectronCore.ArcLoadExtensionsTests

open System
open Fable.Core
open Main.ARCtrlExtensions
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Components.Shared
open Vitest

let private dirent name isDirectory isFile =
    { new Dirent with
        member _.name = name
        member _.isDirectory() = isDirectory
        member _.isFile() = isFile
        member _.isSymbolicLink() = false
    }

let private directory name = dirent name true false
let private file name = dirent name false true

Vitest.describe (
    "bounded ARC structural discovery",
    fun () ->
        Vitest.test (
            "the worker pool bounds concurrency and preserves input order",
            fun () -> promise {
                let mutable active = 0
                let mutable maximumActive = 0

                let! results =
                    Array.init 32 id
                    |> mapBoundedAsync
                        4
                        (fun value -> promise {
                            active <- active + 1
                            maximumActive <- max maximumActive active

                            let! result =
                                JS.Constructors.Promise.Create(fun resolve _ ->
                                    JS.setTimeout (fun () -> resolve value) (32 - value) |> ignore
                                )

                            active <- active - 1
                            return result
                        })

                Vitest.expect(maximumActive).toBe (4)
                Vitest.expect(results |> Array.toList).toEqual (Array.init 32 id |> Array.toList)
            }
        )

        Vitest.test (
            "discovers many immediate entities deterministically without descending into payload",
            fun () -> promise {
                let assayNames = Array.init 24 (fun index -> $"assay-{index + 1:D2}")
                let mutable readPaths = []

                let readDirectory path _ =
                    let normalizedPath = PathHelpers.normalizePath path
                    readPaths <- normalizedPath :: readPaths

                    if normalizedPath = PathHelpers.normalizePath "C:/arc" then
                        JS.Constructors.Promise.resolve [| directory "assays"; file "isa.investigation.xlsx" |]
                    elif normalizedPath = PathHelpers.normalizePath (join [| "C:/arc"; "assays" |]) then
                        assayNames
                        |> Array.rev
                        |> Array.map directory
                        |> JS.Constructors.Promise.resolve
                    else
                        let entityName = basename normalizedPath

                        JS.Constructors.Promise.Create(fun resolve _ ->
                            JS.setTimeout
                                (fun () ->
                                    resolve [|
                                        directory "dataset"
                                        directory "isa.assay.xlsx"
                                        file "isa.datamap.xlsx"
                                        file "isa.assay.xlsx"
                                        file "notes.txt"
                                    |]
                                )
                                (entityName.Length % 5)
                            |> ignore
                        )

                let! paths = discoverStructuralArcFilePathsWithAsync readDirectory "C:/arc"

                let expected =
                    Array.append
                        [| "isa.investigation.xlsx" |]
                        (assayNames
                         |> Array.collect (fun assayName -> [|
                             $"assays/{assayName}/isa.assay.xlsx"
                             $"assays/{assayName}/isa.datamap.xlsx"
                         |]))
                    |> Array.map PathHelpers.normalizePath
                    |> Array.sort

                Vitest.expect(paths).toEqual (expected)
                Vitest.expect(readPaths.Length).toBe (2 + assayNames.Length)
                Vitest.expect(readPaths |> List.exists (fun path -> path.Contains "dataset")).toBe (false)
            }
        )

        Vitest.test (
            "propagates an entity directory read failure",
            fun () -> promise {
                let entityPath =
                    PathHelpers.normalizePath (join [| "C:/arc"; "assays"; "unreadable" |])

                let readDirectory path _ =
                    let normalizedPath = PathHelpers.normalizePath path

                    if normalizedPath = PathHelpers.normalizePath "C:/arc" then
                        JS.Constructors.Promise.resolve [| directory "assays" |]
                    elif normalizedPath = PathHelpers.normalizePath (join [| "C:/arc"; "assays" |]) then
                        JS.Constructors.Promise.resolve [| directory "unreadable" |]
                    elif normalizedPath = entityPath then
                        JS.Constructors.Promise.Create(fun _ reject -> reject (Exception "permission denied"))
                    else
                        JS.Constructors.Promise.resolve [||]

                try
                    let! _ = discoverStructuralArcFilePathsWithAsync readDirectory "C:/arc"
                    return failwith "Expected structural discovery to fail."
                with error ->
                    Vitest.expect(error.Message).toContain ("permission denied")
            }
        )
)
