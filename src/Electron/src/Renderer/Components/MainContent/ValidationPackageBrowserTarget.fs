module Renderer.Components.MainContent.ValidationPackageBrowserTarget

open Fable.Core
open Feliz
open ARCtrl.ValidationPackages
open ARCtrl.Yaml
open Swate.Components
open Swate.Components.Page.ValidationPackageBrowser
open Swate.Components.Page.ValidationPackageBrowser.Helper
open Swate.Components.Page.ValidationPackageBrowser.Types
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Electron.Shared.FileIOHelper
open Swate.Electron.Shared.FileIOTypes
open Renderer.Components.Helper.ArcVaultHelper

module private ValidationPackageBrowserHelper =

    let configFilePath =
        ARCtrl.ArcPathHelper.combine
            ARCtrl.ArcPathHelper.ARCConfigFolderName
            ARCtrl.ArcPathHelper.ValidationPackagesYamlFileName

    let emptyConfig () =
        ValidationPackagesConfig.make (ResizeArray<ValidationPackage>()) None

    let loadConfig (onError: string -> unit) (setConfig: ValidationPackagesConfig -> unit) : JS.Promise<unit> = promise {
        let! existsResult = Api.ipcArcVaultApi.pathExists configFilePath

        match existsResult with
        | Error error -> onError $"Could not check validation packages file: {error.Message}"
        | Ok false -> () // Missing file: keep the empty config, it will be written on first submit.
        | Ok true ->
            let! fileResult = Api.ipcArcVaultApi.openFile configFilePath

            match fileResult with
            | Error error -> onError $"Could not read validation packages file: {error.Message}"
            | Ok fileDto ->
                try
                    let parsedConfig = ValidationPackagesConfig.fromYamlString fileDto.content
                    setConfig parsedConfig
                with parseError ->
                    onError $"Could not parse validation packages file: {parseError.Message}"
    }

    let writeConfig
        (setConfig: ValidationPackagesConfig -> unit)
        (config: ValidationPackagesConfig)
        : JS.Promise<Result<unit, exn>> =
        promise {
            try
                let! folderExists = Api.ipcArcVaultApi.pathExists ARCtrl.ArcPathHelper.ARCConfigFolderName

                match folderExists with
                | Ok false ->
                    // Ensure the .arc folder exists before the first write. The folder creation is best effort,
                    // the file write below also creates missing parent directories.
                    let! _ =
                        Api.ipcArcVaultApi.createFileSystemItem {
                            parentPath = ""
                            name = ARCtrl.ArcPathHelper.ARCConfigFolderName
                            kind = FileSystemItemKind.Folder
                        }

                    ()
                | _ -> ()

                let yaml = config.toYamlString ()

                let! writeResult =
                    Api.ipcArcVaultApi.writeFile (FileContentDTO.create FileContentType.YAML yaml configFilePath)

                match writeResult with
                | Ok() ->
                    setConfig config
                    return Ok()
                | Error error -> return Error error
            with error ->
                return Error error
        }


[<ReactComponent>]
let private ParsingErrorWarning (parsingError: string, setParsingError: string option -> unit) =
    Html.div [
        prop.role.alertDialog
        prop.className "swt:alert swt:alert-error"
        prop.children [
            Html.div [
                prop.text
                    "Parsing error occurred. If you dismiss this error, without resolving the issue, you might override existing validation package configuration."
            ]
            Html.div [
                Html.span parsingError
                Html.button [
                    prop.text "Dismiss"
                    prop.onClick (fun _ -> setParsingError None)
                ]
            ]
        ]
    ]

[<ReactComponent(true)>]
let ValidationPackageBrowserTarget () =
    let errorModal = useErrorModalCtx ()
    let appStateCtx = Renderer.Context.AppStateContext.useAppStateCtx ()
    /// If parsing error exists, we display a warning for the user, that if they proceed, they might loose existing validation package configuration.
    let parsingError, setParsingError = React.useState None

    let onError =
        fun (msg: string) ->
            setParsingError (Some msg)
            createErrorModalCallback errorModal.enqueue "Validation packages" appStateCtx msg

    let config, setConfig =
        React.useState (fun () -> ValidationPackageBrowserHelper.emptyConfig ())

    React.useEffectOnce (fun () -> ValidationPackageBrowserHelper.loadConfig onError setConfig |> Promise.start)

    let fetchValidationPackages () : JS.Promise<ValidationPackageDTO[]> = promise {
        let! result = Api.ipcValidationPackageApi.getAllPackages ()

        match result with
        | Ok packages ->
            // The registry returns all versions of each package, the selector only supports one row per package name.
            return latestVersions packages
        | Error error -> return raise error
    }

    Html.div [
        prop.className "swt:size-full swt:flex swt:flex-col swt:overflow-hidden"
        prop.testId "main-content-validation-package-browser"
        prop.children [
            match parsingError with
            | Some error -> ParsingErrorWarning(error, setParsingError)
            | None ->
                ValidationPackageBrowser.ValidationPackageBrowser(
                    config = config,
                    writeConfig = ValidationPackageBrowserHelper.writeConfig setConfig,
                    fetchValidationPackages = fetchValidationPackages,
                    onError = fun error -> onError error.Message
                )
        ]
    ]
