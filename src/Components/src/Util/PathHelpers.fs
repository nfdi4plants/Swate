namespace Swate.Components

[<RequireQualifiedAccess>]
module PathHelpers =

    let normalizeSeparators (path: string) =
        Swate.Components.Shared.PathHelpers.normalizeSeparators path

    let normalizePath (path: string) =
        Swate.Components.Shared.PathHelpers.normalizePath path

    let normalizeRelativePath (path: string) =
        Swate.Components.Shared.PathHelpers.normalizeRelativePath path

    let normalizeForComparison (path: string) =
        Swate.Components.Shared.PathHelpers.normalizeForComparison path

    let pathsEqual (left: string) (right: string) =
        Swate.Components.Shared.PathHelpers.pathsEqual left right

    let getNameFromPath (path: string) =
        Swate.Components.Shared.PathHelpers.getNameFromPath path

    let resolveArcPreviewPath (path: string) =
        Swate.Components.Shared.PathHelpers.resolveArcPreviewPath path
