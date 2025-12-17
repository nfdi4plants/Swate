namespace Swate.Electron.Shared

[<RequireQualifiedAccess>]
type AppState =
    | Init
    | ARC of path: string