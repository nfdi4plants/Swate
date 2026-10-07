namespace Main.ARCtrlExtensions

open ARCtrl
open ARCtrl.Contract
open Main.Bindings.Path
open Main.Bindings.Filesystem
open Swate.Components.Shared
open Swate.Electron.Shared.FileIOHelper

[<AutoOpen>]
module ArcLoadExtensions =

    let private discoverStructuralArcFilePathsAsync (arcPath: string) = promise {
        let paths = ResizeArray<string>()
        let! rootEntries = readdirWithTypesAsync arcPath (ReaddirOptions(withFileTypes = true))

        rootEntries
        |> Array.tryFind (fun entry ->
            entry.isFile ()
            && PathHelpers.pathsEqual entry.name ArcPathHelper.InvestigationFileName
        )
        |> Option.iter (fun entry -> paths.Add entry.name)

        for zone in ArcEntityPathRules.allAddZones do
            let zoneFolder = ArcEntityPathRules.zoneFolderName zone

            match
                rootEntries
                |> Array.tryFind (fun entry -> entry.isDirectory () && PathHelpers.pathsEqual entry.name zoneFolder)
            with
            | None -> ()
            | Some zoneEntry ->
                let zonePath = join [| arcPath; zoneEntry.name |]
                let! entityEntries = readdirWithTypesAsync zonePath (ReaddirOptions(withFileTypes = true))

                for entityEntry in entityEntries do
                    if entityEntry.isDirectory () then
                        let entityPath = join [| zonePath; entityEntry.name |]

                        let! metadataEntries = readdirWithTypesAsync entityPath (ReaddirOptions(withFileTypes = true))

                        for metadataEntry in metadataEntries do
                            if
                                metadataEntry.isFile ()
                                && [|
                                    ArcEntityPathRules.zoneEntityFileName zone
                                    ArcPathHelper.DataMapFileName
                                    LegacyDataMapFileName
                                   |]
                                   |> Array.exists (PathHelpers.pathsEqual metadataEntry.name)
                            then
                                paths.Add $"{zoneEntry.name}/{entityEntry.name}/{metadataEntry.name}"

        return paths.ToArray()
    }

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
