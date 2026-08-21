namespace Swate.Components.Composite.ValidationPackageSelector

open Fable.Core
open Fable.Core.JS
open Feliz
open ARCtrl
open ARCtrl.ValidationPackages

[<Erase; Mangle(false)>]
type ValidationPackageSelector =

    [<ReactComponent(true)>]
    static member ValidationPackageSelector
        (
            fetchValidationPackages: unit -> Promise<ValidationPackage list>,
            writeConfig: ValidationPackagesConfig -> Promise<Result<unit, exn>>
        ) =
        Html.div "ValidationPackageSelector"
