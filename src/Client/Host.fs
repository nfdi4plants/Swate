[<AutoOpen>]
module Host

[<RequireQualifiedAccess>]
type Swatehost =
| Browser
| Excel of host:string * platform: string
| Electron //WIP
| None