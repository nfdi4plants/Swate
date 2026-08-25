module Swate.Components.Composite.ValidationPackageSelector.Context

open Feliz
open Types

type ValidationPackageSelectorContext = {
    FetchState: SelectorState
    Packages: ValidationPackageDTO[]
    RowStateOf: ValidationPackageDTO -> PackageRowState
    Toggle: ValidationPackageDTO -> unit
    UpdateToLatest: ValidationPackageDTO -> unit
}

let ValidationPackageSelectorCtx =
    React.createContext<ValidationPackageSelectorContext> (
        {
            FetchState = SelectorState.Idle
            Packages = [||]
            RowStateOf = fun _ -> PackageRowState.Unchecked
            Toggle = ignore
            UpdateToLatest = ignore
        }
    )

[<Hook>]
let useValidationPackageSelectorCtx () =
    React.useContext ValidationPackageSelectorCtx
