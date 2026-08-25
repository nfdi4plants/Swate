module Swate.Components.Shared.Cwl.Documents.CommandLineTool

open Swate.Components.Shared.Cwl.Documents.Types

let setBaseCommand (command: string) (model: CommandLineToolModel) =
    let nextBaseCommand =
        if System.String.IsNullOrWhiteSpace command then
            []
        else
            [ command.Trim() ]

    {
        model with
            BaseCommand = nextBaseCommand
    }
