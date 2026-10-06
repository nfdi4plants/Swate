module Main.VersionControl.VersionControlSettings

open Swate.Electron.Shared.VersionControlTypes
open VersionControlService.Abstractions

type VersionControlSettings = {
    /// Whole MiB. Files at or above it use large-object storage automatically.
    AutoTrackThresholdMb: int
    /// Whether clone and update download large objects.
    DownloadLargeFiles: bool
    /// Whole MiB. The background indexing of a diff stops once the pages it read add up to this size.
    DiffIndexingLimitMb: int
}

module VersionControlSettings =

    [<Literal>]
    let MinAutoTrackThresholdMb = 1

    [<Literal>]
    let MaxAutoTrackThresholdMb = 100

    /// The smallest limit holds a few pages of a dense diff. The largest is 1 TiB.
    [<Literal>]
    let MinDiffIndexingLimitMb = 64

    [<Literal>]
    let MaxDiffIndexingLimitMb = 1048576

    [<Literal>]
    let DefaultDiffIndexingLimitMb = 1024

    let defaults = {
        AutoTrackThresholdMb = MinAutoTrackThresholdMb
        DownloadLargeFiles = false
        DiffIndexingLimitMb = DefaultDiffIndexingLimitMb
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
        else
            Ok settings

    let toStoragePolicySettings (settings: VersionControlSettings) : StoragePolicySettings = {
        AutoPolicyThresholdMb = Some settings.AutoTrackThresholdMb
        MaterializeLargeObjects = settings.DownloadLargeFiles
    }
