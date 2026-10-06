module Main.VersionControl.VersionControlSettings

open VersionControlService.Abstractions

type VersionControlSettings = {
    /// Whole MiB. Files at or above it use large-object storage automatically.
    AutoTrackThresholdMb: int
    /// Whether clone and update download large objects.
    DownloadLargeFiles: bool
}

module VersionControlSettings =

    [<Literal>]
    let MinAutoTrackThresholdMb = 1

    [<Literal>]
    let MaxAutoTrackThresholdMb = 100

    let defaults = {
        AutoTrackThresholdMb = MinAutoTrackThresholdMb
        DownloadLargeFiles = false
    }

    /// Ok when the threshold is within the allowed range, otherwise the reason it is refused.
    let validate (settings: VersionControlSettings) : Result<VersionControlSettings, string> =
        if settings.AutoTrackThresholdMb < MinAutoTrackThresholdMb then
            Error $"The automatic large-object threshold must be at least {MinAutoTrackThresholdMb} MiB."
        elif settings.AutoTrackThresholdMb > MaxAutoTrackThresholdMb then
            Error $"The automatic large-object threshold must be at most {MaxAutoTrackThresholdMb} MiB."
        else
            Ok settings

    let toStoragePolicySettings (settings: VersionControlSettings) : StoragePolicySettings = {
        AutoPolicyThresholdMb = Some settings.AutoTrackThresholdMb
        MaterializeLargeObjects = settings.DownloadLargeFiles
    }
