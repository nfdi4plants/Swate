[<AutoOpen>]
module Main.ArcVaultTypes

let arcNotOpenError () =
    exn "No ARC is open. Open an ARC and try again."

type ArcVaultFileSystemEvent = {
    EventName: string
    RelativePath: string
    AbsolutePath: string
}
