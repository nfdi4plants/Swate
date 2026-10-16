namespace Swate.Components.Page.GitSidebarTypes

/// The git settings a user chooses once and every ARC opened afterwards starts with.
type SettingsGitDefaults = {
    LfsAutoTrackThresholdMb: int
    DownloadLargeFiles: bool
    DiffIndexingLimitMb: int
    UploadRightAway: bool
}

/// The allowed range of the numeric git settings in MB. The git sidebar, the settings page and the main
/// process accept the same values.
module GitSettingRanges =

    [<Literal>]
    let MinLfsAutoTrackThresholdMb = 1

    [<Literal>]
    let MaxLfsAutoTrackThresholdMb = 100

    /// A limit below 64 is the memory budget of a diff held in memory.
    [<Literal>]
    let MinDiffIndexingLimitMb = 1

    [<Literal>]
    let MaxDiffIndexingLimitMb = 1048576
