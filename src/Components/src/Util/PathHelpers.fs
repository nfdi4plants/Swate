namespace Swate.Components.Shared

[<RequireQualifiedAccess>]
module PathHelpers =

    let normalizeSeparators (path: string) = path.Replace("\\", "/")

    let normalizePath (path: string) =
        normalizeSeparators path |> fun normalized -> normalized.TrimEnd('/')

    let normalizeRelativePath (path: string) =
        normalizeSeparators path |> fun normalized -> normalized.Trim('/').Trim()

    let normalizeForComparison (path: string) =
        normalizeSeparators path
        |> fun normalized -> normalized.Trim().TrimEnd('/').ToLowerInvariant()

    let pathsEqual (left: string) (right: string) =
        normalizeForComparison left = normalizeForComparison right

    let getNameFromPath (path: string) =
        normalizePath path
        |> fun normalized -> normalized.Split('/')
        |> Array.last
