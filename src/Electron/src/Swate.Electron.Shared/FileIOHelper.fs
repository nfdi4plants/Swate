module Swate.Electron.Shared.FileIOHelper

open Swate.Components.Shared

let getNameFromPath (path: string) = PathHelpers.getNameFromPath path

let normalizePath (path: string) = PathHelpers.normalizeForComparison path

let pathsEqual (left: string) (right: string) =
    PathHelpers.pathsEqual left right
