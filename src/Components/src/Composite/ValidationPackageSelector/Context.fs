module Swate.Components.Composite.ValidationPackageSelector.Context

open Feliz
open Types

type ValidationPackageSelectorContext = {
    RowStateOf: ValidationPackageDTO -> PackageRowState
    Toggle: ValidationPackageDTO -> unit
    UpdateToLatest: ValidationPackageDTO -> unit
}

let ValidationPackageSelectorCtx =
    React.createContext<ValidationPackageSelectorContext> (
        {
            RowStateOf = fun _ -> PackageRowState.Unchecked
            Toggle = ignore
            UpdateToLatest = ignore
        }
    )

[<Hook>]
let useValidationPackageSelectorCtx () =
    React.useContext ValidationPackageSelectorCtx
