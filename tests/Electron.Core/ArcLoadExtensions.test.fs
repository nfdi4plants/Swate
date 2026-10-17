module ElectronCore.ArcLoadExtensionsTests

open ARCtrl
open System
open Fable.Core
open Main.ARCtrlExtensions
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper
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
            "discovers the root license files that ARCtrl reads into the ARC",
            fun () -> promise {
                let readDirectory path _ =
                    if PathHelpers.normalizePath path = PathHelpers.normalizePath "C:/arc" then
                        JS.Constructors.Promise.resolve [|
                            file "isa.investigation.xlsx"
                            file "LICENSE.md"
                            file "README.md"
                            directory "LICENSE"
                        |]
                    else
                        JS.Constructors.Promise.resolve [||]

                let! paths = discoverStructuralArcFilePathsWithAsync readDirectory "C:/arc"

                Vitest.expect(paths).toEqual ([| "LICENSE.md"; "isa.investigation.xlsx" |])
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

Vitest.describe (
    "bounded ARC loading",
    fun () ->
        Vitest.test (
            "bounded ARC loading reconstructs every entity type",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-bounded-arc-load-"
                    "BoundedArc"
                    TestHelpers.addDataMapToAllEntityTypes
                    (fun arcPath -> promise {
                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult

                        Vitest.expect(loadedArc.Identifier).toBe ("BoundedArc")
                        Vitest.expect(loadedArc.GetStudy("Study With DataMap").DataMap.IsSome).toBe (true)
                        Vitest.expect(loadedArc.GetAssay("Assay With DataMap").DataMap.IsSome).toBe (true)
                        Vitest.expect(loadedArc.GetWorkflow("Workflow With DataMap").DataMap.IsSome).toBe (true)
                        Vitest.expect(loadedArc.GetRun("Run With DataMap").DataMap.IsSome).toBe (true)
                    })
        )

        Vitest.test (
            "bounded ARC loading allows missing optional structural zones",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-bounded-missing-zones-"
                    "MissingZonesArc"
                    ignore
                    (fun arcPath -> promise {
                        for zone in ArcEntityPathRules.allAddZones do
                            let zonePath = join [| arcPath; ArcEntityPathRules.zoneFolderName zone |]
                            do! rmAsync zonePath (RmOptions(recursive = true, force = true))

                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult

                        Vitest.expect(loadedArc.Identifier).toBe ("MissingZonesArc")
                    })
        )

        Vitest.test (
            "bounded ARC loading migrates a legacy entity DataMap",
            fun () ->
                TestHelpers.withTempArcWith
                    "swate-bounded-legacy-datamap-"
                    "LegacyDataMapArc"
                    (fun arc ->
                        let assay = ArcAssay("Legacy Assay")
                        assay.DataMap <- Some(DataMap.init ())
                        arc.AddAssay assay
                    )
                    (fun arcPath -> promise {
                        let assayFolder =
                            join [|
                                arcPath
                                ArcPathHelper.AssaysFolderName
                                "Legacy Assay"
                            |]

                        let canonicalPath = join [| assayFolder; ArcPathHelper.DataMapFileName |]
                        let legacyPath = join [| assayFolder; LegacyDataMapFileName |]
                        do! renameAsync canonicalPath legacyPath

                        let! loadResult = ARC.LoadAsyncSwate arcPath
                        let loadedArc = TestHelpers.expectLoadedArc loadResult

                        Vitest.expect(loadedArc.GetAssay("Legacy Assay").DataMap.IsSome).toBe (true)
                        Vitest.expect(existsSync legacyPath).toBe (false)
                        Vitest.expect(existsSync canonicalPath).toBe (true)
                    })
        )
)
