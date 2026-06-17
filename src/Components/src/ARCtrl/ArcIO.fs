module ARCtrl.ArcIO

open Swate.Components.Shared

let getArcRootPath (rootPath: string) =
    let normalizedPath = PathHelpers.normalizePath rootPath

    ARCtrl.ArcPathHelper.getFileName normalizedPath
