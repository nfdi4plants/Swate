module Preload

open Browser.Types
open Fable.Electron.Renderer
open Fable.Electron.Remoting.Preload
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

type private SwateElectronFileApi = { getPathForFile: File -> string }

contextBridge.exposeInMainWorld (
    "SwateElectronFileApi",
    {
        getPathForFile = fun (file: File) -> webUtils.getPathForFile file
    }
)

Remoting.createIpc () |> Remoting.buildTwoWayBridge<IArcVaultsApi>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<IGitApi>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<IGitLabApi>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<IAuthApi>

Remoting.createIpc () |> Remoting.buildBridge<IPathChangeRendererApi>
Remoting.createIpc () |> Remoting.buildBridge<IRecentArcsRendererApi>
Remoting.createIpc () |> Remoting.buildBridge<IAuthAccountsRendererApi>
Remoting.createIpc () |> Remoting.buildBridge<IFileTreeRendererApi>
Remoting.createIpc () |> Remoting.buildBridge<IGitProgressRendererApi>
Remoting.createIpc () |> Remoting.buildBridge<IGitRepositoryRendererApi>
Remoting.createIpc () |> Remoting.buildBridge<IGitLfsProgressRendererApi>
Remoting.createIpc () |> Remoting.buildBridge<IHasUnsavedArcChangesRendererApi>

Remoting.createIpc () |> Remoting.buildBridge<IArcFileWatcherApi>
Remoting.createIpc () |> Remoting.buildBridge<IMainSaveBeforeQuitApi>
