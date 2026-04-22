module Renderer.RendererStoreState

[<RequireQualifiedAccess>]
type LoadStatus =
    | NotRequested
    | Loading
    | Ready

type IPCSnapshot<'T> = {
    Value: 'T
    Status: LoadStatus
}
