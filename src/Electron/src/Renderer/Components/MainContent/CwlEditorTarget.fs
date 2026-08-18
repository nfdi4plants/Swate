module Renderer.Components.MainContent.CwlEditorTarget

open Feliz
open Swate.Components.Page.CwlEditor
open Swate.Components.Page.CwlEditor.Types
open Swate.Components.Shared.Cwl.HostTypes
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes

let private loadResponse (path: string) (raw: string) (resolved: string option) : LoadCwlResponse = {
    Success = true
    Yaml = Some raw
    ResolvedYaml = resolved
    FilePath = path
    Error = None
}

let private failedLoadResponse (path: string) (error: exn) : LoadCwlResponse = {
    Success = false
    Yaml = None
    ResolvedYaml = None
    FilePath = path
    Error = Some error.Message
}

let private saveResponse (path: string) (result: Result<unit, exn>) : SaveCwlResponse =
    match result with
    | Ok() -> {
        Success = true
        FilePath = path
        Error = None
      }
    | Error error -> {
        Success = false
        FilePath = path
        Error = Some error.Message
      }

[<ReactComponent>]
let CwlEditorTarget (path: string) (raw: string) (resolved: string option) =
    let initialFile, setInitialFile = React.useState<LoadCwlResponse option> None
    let isDirty, setIsDirty = React.useState false

    let fallbackInitialFile = loadResponse path raw resolved

    React.useEffect (
        (fun () ->
            promise {
                let! result = Api.ipcArcVaultApi.openCwlFile path

                match result with
                | Ok cwlFile -> setInitialFile (Some(loadResponse path cwlFile.raw cwlFile.resolved))
                | Error _ -> setInitialFile (Some fallbackInitialFile)
            }
            |> Promise.catch (fun _ -> setInitialFile (Some fallbackInitialFile))
            |> Promise.start
        ),
        [||]
    )

    let host: CwlEditorHost = {
        loadCwlFile =
            fun filePath -> promise {
                let! result = Api.ipcArcVaultApi.openCwlFile filePath

                match result with
                | Ok cwlFile -> return loadResponse filePath cwlFile.raw cwlFile.resolved
                | Error error -> return failedLoadResponse filePath error
            }
        saveCwlFile =
            fun filePath yaml -> promise {
                let! result =
                    FileContentDTO.create FileContentType.CWL yaml filePath
                    |> Api.ipcArcVaultApi.writeFile

                return saveResponse filePath result
            }
        pickOpenFile = None
        pickSavePath = None
    }

    match initialFile with
    | Some file -> CwlEditor.CwlEditor(initialFile = file, host = host, onDirtyChange = setIsDirty)
    | None ->
        Html.div [
            prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:flex swt:items-center swt:justify-center"
            prop.text "Loading CWL editor..."
        ]
