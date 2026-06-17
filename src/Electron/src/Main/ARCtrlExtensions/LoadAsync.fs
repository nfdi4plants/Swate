namespace Main.ARCtrlExtensions

open System
open ARCtrl
open ARCtrl.Contract
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Swate.Electron.Shared.FileIOHelper

[<AutoOpen>]
module ArcLoadExtensions =

    /// Returns true when a path addresses Git's private repository metadata.
    /// `.gitignore`, `.gitattributes`, and similarly named files remain ordinary ARC payload.
    let isGitMetadataPath (pathValue: string) =
        pathValue
        |> getNonEmptyPathParts
        |> Array.exists (fun segment -> String.Equals(segment, ".git", StringComparison.OrdinalIgnoreCase))

    let private getAllArcFilePathsAsync (arcPath: string) =
        let rec collectFiles (absoluteDirectoryPath: string) (relativeDirectoryPath: string) = promise {
            let! entries = readdirWithTypesAsync absoluteDirectoryPath (ReaddirOptions(withFileTypes = true))
            let files = ResizeArray<string>()

            for entry in entries do
                let relativePath =
                    if relativeDirectoryPath = "" then
                        entry.name
                    else
                        $"{relativeDirectoryPath}/{entry.name}"

                if not (isGitMetadataPath relativePath) then
                    if entry.isDirectory () then
                        let absolutePath = join [| absoluteDirectoryPath; entry.name |]
                        let! nestedFiles = collectFiles absolutePath relativePath
                        files.AddRange nestedFiles
                    elif entry.isFile () then
                        files.Add relativePath

            return files.ToArray()
        }

        collectFiles arcPath ""

    type private CanonicalArcFileRepairSpec = {
        CollectionFolder: string
        FileName: string
        CreateContracts: string -> Contract[]
    }

    let private canonicalArcFileRepairSpecs = [|
        {
            CollectionFolder = "assays"
            FileName = "isa.assay.xlsx"
            CreateContracts = fun identifier -> (ArcAssay.init identifier).ToCreateContract(false)
        }
        {
            CollectionFolder = "studies"
            FileName = "isa.study.xlsx"
            CreateContracts = fun identifier -> (ArcStudy.init identifier).ToCreateContract(false)
        }
        {
            CollectionFolder = "workflows"
            FileName = "isa.workflow.xlsx"
            CreateContracts = fun identifier -> (ArcWorkflow.init identifier).ToCreateContract(false)
        }
        {
            CollectionFolder = "runs"
            FileName = "isa.run.xlsx"
            CreateContracts = fun identifier -> (ArcRun.init identifier).ToCreateContract(false)
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

        /// Hotfix for #619, not fixed in the consumed ARCtrl 3.0.0-beta.12.
        /// Mirrors ARC.tryLoadAsync, changing only filesystem traversal so `.git` directories are never enumerated.
        static member LoadAsyncSwate(arcPath: string) = promise {
            let! paths = getAllArcFilePathsAsync arcPath
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
