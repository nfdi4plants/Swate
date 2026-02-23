module Renderer.components.ARCHelper

open Swate.Electron.Shared
open Swate.Electron.Shared.IPCTypes


let tryGetCreatedFilePath appState (target: ExperimentTarget) (identifier: string) =
    match appState with
    | AppState.ARC arcPath ->
        let root = arcPath.Replace("\\", "/").TrimEnd('/')

        match target with
        | ExperimentTarget.Study -> Some $"{root}/studies/{identifier}/isa.study.xlsx"
        | ExperimentTarget.Assay -> Some $"{root}/assays/{identifier}/isa.assay.xlsx"
    | AppState.Init -> None
