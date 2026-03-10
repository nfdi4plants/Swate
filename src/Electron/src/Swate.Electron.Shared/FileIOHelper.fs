module Swate.Electron.Shared.FileIOHelper


let getNameFromPath (path: string) =
    path
    |> (fun p -> p.Replace("\\", "/"))
    |> (fun p -> p.TrimEnd('/'))
    |> (fun p -> p.Split("/"))
    |> Array.last

let normalizePath (path: string) =
    path.Replace("\\", "/").Trim().TrimEnd('/').ToLowerInvariant()

let pathsEqual (left: string) (right: string) =
    normalizePath left = normalizePath right