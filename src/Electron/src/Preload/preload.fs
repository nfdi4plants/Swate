module Preload

open Fable.Electron.Remoting.Preload
open Swate.Electron.Shared.IPCTypes

Remoting.init |> Remoting.buildTwoWayBridge<IArcVaultsApi>
Remoting.init |> Remoting.buildTwoWayBridge<IGitApi>
Remoting.init |> Remoting.buildTwoWayBridge<IGitLabApi>
Remoting.init |> Remoting.buildTwoWayBridge<IAuthApi>

Remoting.init |> Remoting.buildBridge<IPathChangeApi>
Remoting.init |> Remoting.buildBridge<IRecentARCsUpdateApi>
Remoting.init |> Remoting.buildBridge<IAuthAccountsUpdateApi>
Remoting.init |> Remoting.buildBridge<IFileTreeUpdateApi>
Remoting.init |> Remoting.buildBridge<IGitProgressUpdateApi>

Remoting.init |> Remoting.buildBridge<IArcFileWatcherApi>
Remoting.init |> Remoting.buildBridge<IMainSaveBeforeQuitApi>