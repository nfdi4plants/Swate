module Renderer.Components.MainContent.ValidationPackagesTarget

open Fable.Core
open Feliz
open ARCtrl.ValidationPackages
open Swate.Components.Primitive
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types
open Swate.Components.Composite.ValidationPackageSelector
open Swate.Components.Composite.ValidationPackageSelector.Types
open Renderer.Components.MainContent.ErrorViewTarget

[<RequireQualifiedAccess>]
type private LoadState =
    | Loading
    | Loaded of ValidationPackagesConfig
    | Failed of string

[<ReactComponent>]
let ValidationPackagesTarget () =
    let errorModal = useErrorModalCtx ()
    let gitStateCtx = Renderer.Context.GitStateContext.useGitStateCtx ()
    let loadState, setLoadState = React.useState LoadState.Loading

    React.useEffect (
        (fun () ->
            let mutable isDisposed = false

            promise {
                let! result = Api.ipcArcVaultApi.readValidationPackagesConfig ()

                if not isDisposed then
                    match result |> Result.bind ValidationPackagesTargetHelper.parseConfigOrDefault with
                    | Ok config -> setLoadState (LoadState.Loaded config)
                    | Error error ->
                        setLoadState (
                            LoadState.Failed $"Could not read the validation packages config: {error.Message}"
                        )
            }
            |> Promise.catch (fun error ->
                if not isDisposed then
                    setLoadState (LoadState.Failed error.Message)
            )
            |> Promise.start

            fun () -> isDisposed <- true
        ),
        [||]
    )

    let writeConfig (nextConfig: ValidationPackagesConfig) : JS.Promise<Result<unit, exn>> = promise {
        let yaml = ValidationPackagesTargetHelper.serializeConfig nextConfig

        match! Api.ipcArcVaultApi.writeValidationPackagesConfig yaml with
        | Ok() ->
            setLoadState (LoadState.Loaded nextConfig)
            // The git sidebar only polls on mount and after git operations, so ask for a fresh status
            // right away. Otherwise the new .arc/validation_packages.yml stays invisible until a remount.
            gitStateCtx.refresh ()
            return Ok()
        | Error error -> return Error error
    }

    let fetchPackages () : JS.Promise<ValidationPackageDTO[]> = promise {
        match! Api.ipcAvprApi.getPackages () with
        | Error error -> return raise error
        | Ok json ->
            match ValidationPackagesTargetHelper.decodePackages json with
            | Ok packages -> return packages
            | Error error -> return raise error
    }

    let onError (error: exn) =
        errorModal.enqueue (ErrorModalRequest.create (error.Message, title = "Validation packages"))

    Html.div [
        prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:overflow-y-auto"
        prop.testId "main-content-validation-packages-page"
        prop.children [
            match loadState with
            | LoadState.Loading ->
                Html.div [
                    prop.className "swt:size-full swt:flex swt:justify-center swt:items-center"
                    prop.children [
                        LoadingSpinner.LoadingSpinner.LoadingSpinner(
                            size = DaisyuiSize.XL,
                            color = DaisyuiColors.Primary,
                            text = "Loading validation packages..."
                        )
                    ]
                ]
            | LoadState.Failed message -> ErrorViewTarget message
            | LoadState.Loaded config ->
                ValidationPackageSelector.ValidationPackageSelector(
                    config = config,
                    writeConfig = writeConfig,
                    fetchValidationPackages = fetchPackages,
                    onError = onError
                )
        ]
    ]
