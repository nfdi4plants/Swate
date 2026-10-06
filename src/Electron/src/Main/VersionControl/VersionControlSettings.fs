module Main.VersionControl.VersionControlSettings

open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions

type VersionControlSettings = {
    /// Whole MiB. Files at or above it use large-object storage automatically.
    AutoTrackThresholdMb: int
    /// Whether clone and update download large objects.
    DownloadLargeFiles: bool
    /// Whole MiB. A value from 1 to 63 keeps the diff in memory and is its memory budget. A value
    /// from 64 up lets the diff use temp files, and its background indexing stops once the pages it
    /// read add up to this size.
    DiffIndexingLimitMb: int
    /// Whole MiB. A diff that would use temp files keeps its data in memory instead when the temp
    /// drive has less free space than this reserve plus 5 %.
    DiffFreeSpaceReserveMb: int
}

module VersionControlSettings =

    [<Literal>]
    let MinAutoTrackThresholdMb = 1

    [<Literal>]
    let MaxAutoTrackThresholdMb = 100

    /// The limit ranges from 1 MiB to 1 TiB. A value up to MaxMemoryDiffLimitMb is the memory budget of
    /// a diff held in memory. A larger value is the indexing limit of a diff that uses temp files.
    [<Literal>]
    let MinDiffIndexingLimitMb = 1

    [<Literal>]
    let MaxMemoryDiffLimitMb = 63

    [<Literal>]
    let MaxDiffIndexingLimitMb = 1048576

    [<Literal>]
    let DefaultDiffIndexingLimitMb = 1024

    /// The reserve of free temp space is 0 MiB to 1 TiB.
    [<Literal>]
    let MinDiffFreeSpaceReserveMb = 0

    [<Literal>]
    let MaxDiffFreeSpaceReserveMb = 1048576

    [<Literal>]
    let DefaultDiffFreeSpaceReserveMb = 1024

    /// The library compares the free space against the reserve times this factor.
    [<Literal>]
    let EffectiveReserveFactor = 1.05

    /// The memory budget of a diff that falls back to memory because the temp drive is low on space.
    [<Literal>]
    let FallbackMemoryBudgetBytes = 67108864L

    let defaults = {
        AutoTrackThresholdMb = MinAutoTrackThresholdMb
        DownloadLargeFiles = false
        DiffIndexingLimitMb = DefaultDiffIndexingLimitMb
        DiffFreeSpaceReserveMb = DefaultDiffFreeSpaceReserveMb
    }

    /// Ok when the values are within the allowed ranges, otherwise the failure code and the reason
    /// the settings are refused.
    let validate (settings: VersionControlSettings) : Result<VersionControlSettings, string * string> =
        if settings.AutoTrackThresholdMb < MinAutoTrackThresholdMb then
            Error(
                VersionControlCodes.InvalidLfsThreshold,
                $"The automatic large-object threshold must be at least {MinAutoTrackThresholdMb} MiB."
            )
        elif settings.AutoTrackThresholdMb > MaxAutoTrackThresholdMb then
            Error(
                VersionControlCodes.InvalidLfsThreshold,
                $"The automatic large-object threshold must be at most {MaxAutoTrackThresholdMb} MiB."
            )
        elif settings.DiffIndexingLimitMb < MinDiffIndexingLimitMb then
            Error(
                VersionControlCodes.InvalidDiffIndexingLimit,
                $"The background indexing limit must be at least {MinDiffIndexingLimitMb} MiB."
            )
        elif settings.DiffIndexingLimitMb > MaxDiffIndexingLimitMb then
            Error(
                VersionControlCodes.InvalidDiffIndexingLimit,
                $"The background indexing limit must be at most {MaxDiffIndexingLimitMb} MiB."
            )
        elif settings.DiffFreeSpaceReserveMb < MinDiffFreeSpaceReserveMb then
            Error(
                VersionControlCodes.InvalidDiffFreeSpaceReserve,
                $"The free-space reserve must be at least {MinDiffFreeSpaceReserveMb} MiB."
            )
        elif settings.DiffFreeSpaceReserveMb > MaxDiffFreeSpaceReserveMb then
            Error(
                VersionControlCodes.InvalidDiffFreeSpaceReserve,
                $"The free-space reserve must be at most {MaxDiffFreeSpaceReserveMb} MiB."
            )
        else
            Ok settings

    /// The storage policy of a new diff. A limit from 1 to 63 MiB keeps the diff in memory with that
    /// budget. Any other limit prefers temp files and asks the library to fall back to memory when the
    /// temp drive has less free space than the reserve times the effective factor, rounded up.
    let diffStoragePolicy (settings: VersionControlSettings) : DiffStoragePolicy =
        let mib = 1048576L

        if settings.DiffIndexingLimitMb <= MaxMemoryDiffLimitMb then
            DiffStoragePolicy.MemoryOnly(int64 settings.DiffIndexingLimitMb * mib)
        else
            let minimumFreeBytes =
                int64 (System.Math.Ceiling(float settings.DiffFreeSpaceReserveMb * EffectiveReserveFactor * float mib))

            DiffStoragePolicy.PreferDisk(minimumFreeBytes, FallbackMemoryBudgetBytes)

    let toStoragePolicySettings (settings: VersionControlSettings) : StoragePolicySettings = {
        AutoPolicyThresholdMb = Some settings.AutoTrackThresholdMb
        MaterializeLargeObjects = settings.DownloadLargeFiles
    }
