module Preload

open Browser.Types
open Browser.Dom
open Fable.Electron.Renderer
open Fable.Electron.Remoting.Preload
open Swate.Electron.Shared.IPCTypes
open Swate.Electron.Shared.IPCTypes.MainToRendererIpc

/// Captures absolute paths for dropped browser Files in preload before renderer code handles the drop.
let private registerDroppedFiles (event: Event) =
    let event = event :?> DragEvent

    if not (isNull event.dataTransfer) && not (isNull event.dataTransfer.files) then
        let registrations = ResizeArray<obj>()

        for index in 0 .. int event.dataTransfer.files.length - 1 do
            let file = event.dataTransfer.files.item index

            if not (isNull file) then
                try
                    let absolutePath = webUtils.getPathForFile file

                    if not (System.String.IsNullOrWhiteSpace absolutePath) then
                        registrations.Add(
                            box {|
                                key = createDroppedFilePathKey file.name file.size file.lastModified file.``type``
                                absolutePath = absolutePath
                            |}
                        )
                with _ ->
                    ()

        if registrations.Count > 0 then
            ipcRenderer.send (DroppedFilePathsRegisteredChannel, registrations.ToArray())

window.addEventListener ("drop", registerDroppedFiles, true)

Remoting.createIpc () |> Remoting.buildTwoWayBridge<IArcVaultsApi>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<IGitApi>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<IGitLabApi>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<IAuthApi>
Remoting.createIpc () |> Remoting.buildTwoWayBridge<IFilePickerApi>

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
