namespace Main.ARCtrlExtensions

open ARCtrl
open ARCtrl.Contract
open Main.Bindings.Path
open Main.Bindings.Filesystem
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

[<AutoOpen>]
module ArcLoadExtensions =

    // Limit outstanding filesystem reads without introducing threads, Web Workers, or background services.
    // These reads still run through JavaScript promises on the existing event loop.
    let private maxConcurrentStructuralReads = 8

    /// Maps an array through a fixed number of asynchronous promise loops.
    /// Each loop claims the next input only after its current operation completes, while results are
    /// written back to their original indices so promise completion order cannot reorder the output.
    let internal mapBoundedAsync
        (maxConcurrency: int)
        (operation: 'Input -> Fable.Core.JS.Promise<'Output>)
        (inputs: 'Input[])
        : Fable.Core.JS.Promise<'Output[]> =
        promise {
            if maxConcurrency < 1 then
                invalidArg (nameof maxConcurrency) "Concurrency must be at least one."

            let results = Array.zeroCreate<'Output> inputs.Length
            let mutable nextIndex = 0

            let rec runWorker () = promise {
                let index = nextIndex
                nextIndex <- nextIndex + 1

                if index < inputs.Length then
                    let! result = operation inputs.[index]
                    results.[index] <- result
                    return! runWorker ()
            }

            let workers = Array.init (min maxConcurrency inputs.Length) (fun _ -> runWorker ())

            // Await every loop so a read failure is propagated to structural discovery.
            let! _ = Fable.Core.JS.Constructors.Promise.all workers
            return results
        }

    type private StructuralEntityDirectory = {
        Zone: ArcEntityPathRules.AddZone
        ZoneName: string
        ZonePath: string
        EntityName: string
    }

    let internal discoverStructuralArcFilePathsWithAsync
        (readDirectory: string -> ReaddirOptions -> Fable.Core.JS.Promise<Dirent[]>)
        (arcPath: string)
        =
        promise {
            // The root snapshot is authoritative: zones absent here are optional and are never read.
            let! rootEntries = readDirectory arcPath (ReaddirOptions(withFileTypes = true))

            let investigationPath =
                rootEntries
                |> Array.tryFind (fun entry ->
                    entry.isFile ()
                    && PathHelpers.pathsEqual entry.name ArcPathHelper.InvestigationFileName
                )
                |> Option.map _.name
                |> Option.toArray

            let existingZones =
                ArcEntityPathRules.allAddZones
                |> List.choose (fun zone ->
                    let zoneFolder = ArcEntityPathRules.zoneFolderName zone

                    rootEntries
                    |> Array.tryFind (fun entry ->
                        entry.isDirectory () && PathHelpers.pathsEqual entry.name zoneFolder
                    )
                    |> Option.map (fun zoneEntry -> zone, zoneEntry.name, join [| arcPath; zoneEntry.name |])
                )
                |> List.toArray

            let! zoneEntries =
                existingZones
                |> mapBoundedAsync
                    maxConcurrentStructuralReads
                    (fun (zone, zoneName, zonePath) -> promise {
                        let! entries = readDirectory zonePath (ReaddirOptions(withFileTypes = true))
                        return zone, zoneName, zonePath, entries
                    })

            // Zone reads reveal only immediate entity directories; discovery never descends into payload folders.
            let entityDirectories =
                zoneEntries
                |> Array.collect (fun (zone, zoneName, zonePath, entries) ->
                    entries
                    |> Array.choose (fun entry ->
                        if entry.isDirectory () then
                            Some {
                                Zone = zone
                                ZoneName = zoneName
                                ZonePath = zonePath
                                EntityName = entry.name
                            }
                        else
                            None
                    )
                )

            let! entityMetadataPaths =
                entityDirectories
                |> mapBoundedAsync
                    maxConcurrentStructuralReads
                    (fun entity -> promise {
                        let entityPath = join [| entity.ZonePath; entity.EntityName |]
                        let! metadataEntries = readDirectory entityPath (ReaddirOptions(withFileTypes = true))

                        let allowedFileNames = [|
                            ArcEntityPathRules.zoneEntityFileName entity.Zone
                            ArcPathHelper.DataMapFileName
                            LegacyDataMapFileName
                        |]

                        return
                            metadataEntries
                            |> Array.choose (fun metadataEntry ->
                                if
                                    metadataEntry.isFile ()
                                    && allowedFileNames |> Array.exists (PathHelpers.pathsEqual metadataEntry.name)
                                then
                                    Some $"{entity.ZoneName}/{entity.EntityName}/{metadataEntry.name}"
                                else
                                    None
                            )
                    })

            // Normalize and sort after all reads so filesystem enumeration and promise completion order
            // cannot affect the paths passed to ARC.fromFilePaths.
            return
                Array.append investigationPath (Array.concat entityMetadataPaths)
                |> Array.map PathHelpers.normalizePath
                |> Array.sort
        }

    let private discoverStructuralArcFilePathsAsync (arcPath: string) =
        discoverStructuralArcFilePathsWithAsync readdirWithTypesAsync arcPath

    let migrateLegacyDataMapPathsAsync (arcPath: string) (paths: string[]) = promise {
        let migratedPaths = ResizeArray<string>()

        for relativePath in paths do
            if isLegacyDataMapPath relativePath then
                let parentPath = dirname relativePath

                let canonicalRelativePath =
                    if parentPath = "." then
                        ArcPathHelper.DataMapFileName
                    else
                        join [| parentPath; ArcPathHelper.DataMapFileName |]
                    |> PathHelpers.normalizePath

                let legacyAbsolutePath = join [| arcPath; relativePath |]
                let canonicalAbsolutePath = join [| arcPath; canonicalRelativePath |]

                if existsSync canonicalAbsolutePath then
                    Browser.Dom.console.warn (
                        $"Outdated DataMap file '{relativePath}' was ignored because '{canonicalRelativePath}' already exists."
                    )
                else
                    do! renameAsync legacyAbsolutePath canonicalAbsolutePath

                    Browser.Dom.console.warn (
                        $"Outdated DataMap file '{relativePath}' was migrated to '{canonicalRelativePath}'."
                    )

                    migratedPaths.Add canonicalRelativePath
            else
                migratedPaths.Add relativePath

        return migratedPaths.ToArray()
    }

    type private CanonicalArcFileRepairSpec = {
        CollectionFolder: string
        FileName: string
        CreateContracts: string -> Contract[]
    }

    let private createDefaultArcFileContracts (fileType: ArcFilesDiscriminate) (identifier: string) =
        ARCtrlHelper.ArcFileDefaults.createDefaultArcFile fileType identifier
        |> ArcFileCreateContracts.createContracts false

    let private canonicalArcFileRepairSpecs = [|
        {
            CollectionFolder = "assays"
            FileName = "isa.assay.xlsx"
            CreateContracts = createDefaultArcFileContracts ArcFilesDiscriminate.Assay
        }
        {
            CollectionFolder = "studies"
            FileName = "isa.study.xlsx"
            CreateContracts = createDefaultArcFileContracts ArcFilesDiscriminate.Study
        }
        {
            CollectionFolder = "workflows"
            FileName = "isa.workflow.xlsx"
            CreateContracts = createDefaultArcFileContracts ArcFilesDiscriminate.Workflow
        }
        {
            CollectionFolder = "runs"
            FileName = "isa.run.xlsx"
            CreateContracts = createDefaultArcFileContracts ArcFilesDiscriminate.Run
        }
    |]

    let private isZeroByteZipReadError (errors: string[]) =
        errors
        |> Array.exists (fun error ->
            let normalizedError = error.ToLowerInvariant()

            normalizedError.Contains("error reading contract")
            && normalizedError.Contains("data length = 0")
        )

    let private tryReadDirectoryAsync (directoryPath: string) = promise {
        try
            return! readdirAsync directoryPath
        with _ ->
            return [||]
    }

    let private tryGetFileSizeAsync (filePath: string) = promise {
        try
            let! stats = statAsync filePath
            return Some stats.size
        with _ ->
            return None
    }

    let private repairZeroByteCanonicalArcFile
        (arcPath: string)
        (spec: CanonicalArcFileRepairSpec)
        (identifier: string)
        =
        promise {
            let absolutePath =
                join [|
                    arcPath
                    spec.CollectionFolder
                    identifier
                    spec.FileName
                |]

            let! fileSize = tryGetFileSizeAsync absolutePath

            match fileSize with
            | Some size when size = 0.0 ->
                match! fullFillContractBatchAsync arcPath (spec.CreateContracts identifier) with
                | Ok _ -> return true
                | Error _ -> return false
            | _ -> return false
        }

    let private repairZeroByteCanonicalArcFiles (arcPath: string) = promise {
        let mutable repairedAny = false

        for spec in canonicalArcFileRepairSpecs do
            let collectionPath = join [| arcPath; spec.CollectionFolder |]
            let! identifiers = tryReadDirectoryAsync collectionPath

            for identifier in identifiers do
                let! repaired = repairZeroByteCanonicalArcFile arcPath spec identifier
                repairedAny <- repairedAny || repaired

        return repairedAny
    }

    type ARC with

        /// Loads canonical ARC metadata through a bounded traversal of zone roots and immediate entity folders.
        static member LoadAsyncSwate(arcPath: string) = promise {
            let! discoveredPaths = discoverStructuralArcFilePathsAsync arcPath
            let! paths = migrateLegacyDataMapPathsAsync arcPath discoveredPaths
            let arc = ARC.fromFilePaths paths
            let contracts = arc.GetReadContracts()

            match! fullFillContractBatchAsync arcPath contracts with
            | Ok fulfilledContracts ->
                arc.SetISAFromContracts fulfilledContracts
                return Ok arc
            | Error errors -> return Error errors
        }

        /// Hotfix for #620, not fixed in the consumed ARCtrl 3.0.0-beta.12.
        /// Repairs only zero-byte canonical workbooks left by interrupted creates, then retries LoadAsyncSwate.
        static member LoadAsyncSwateZeroByteRepair(arcPath: string) = promise {
            match! ARC.LoadAsyncSwate arcPath with
            | Ok arc ->
                baselineArcStaticHashes arc
                return Ok arc
            | Error errors when isZeroByteZipReadError errors ->
                let! repairedAny = repairZeroByteCanonicalArcFiles arcPath

                if repairedAny then
                    match! ARC.LoadAsyncSwate arcPath with
                    | Ok arc ->
                        baselineArcStaticHashes arc
                        return Ok arc
                    | Error errors -> return Error errors
                else
                    return Error errors
            | Error errors -> return Error errors
        }
