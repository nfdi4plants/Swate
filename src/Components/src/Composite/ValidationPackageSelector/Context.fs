module Swate.Components.Composite.ValidationPackageSelector.Context

open Feliz
open Types

type ValidationPackageSelectorContext = {
    RowStateMap: Map<string, PackageRowState>
    Toggle: ValidationPackageDTO -> unit
    UpdateToLatest: ValidationPackageDTO -> unit
}

let ValidationPackageSelectorCtx =
    React.createContext<ValidationPackageSelectorContext> (
        {
            RowStateMap = Map.empty
            Toggle = ignore
            UpdateToLatest = ignore
        }
    )

[<Hook>]
let useValidationPackageSelectorCtx () =
    React.useContext ValidationPackageSelectorCtx
