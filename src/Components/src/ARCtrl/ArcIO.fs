module ARCtrl.ArcIO

open ARCitISA.Components.Shared

let getArcRootPath (rootPath: string) =
    let normalizedPath = PathHelpers.normalizePath rootPath

    ARCtrl.ArcPathHelper.getFileName normalizedPath
