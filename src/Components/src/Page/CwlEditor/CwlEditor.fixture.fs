module Swate.Components.Page.CwlEditor.CwlEditorFixture

open Fable.Core
open Feliz
open Swate.Components.Shared.Cwl.HostTypes

[<ReactComponent>]
let Entry () =
    let files = React.useRef (Map.empty<string, string>)
    let fixturePath = "minimal-command-line-tool.cwl"

    let mockHost: Types.CwlEditorHost = {
        pickOpenFile =
            Some(fun () ->
                Promise.lift {
                    Canceled = false
                    FilePath = Some fixturePath
                }
            )
        loadCwlFile =
            fun filePath ->
                Promise.lift {
                    Success = true
                    Yaml =
                        Some(
                            """cwlVersion: v1.2
class: CommandLineTool
baseCommand: echo
inputs:
  message:
    type: string
    inputBinding:
      position: 1
outputs:
  out:
    type: stdout
"""
                        )
                    ResolvedYaml = None
                    FilePath = filePath
                    Error = None
                }
        pickSavePath =
            Some(fun () ->
                Promise.lift {
                    Canceled = false
                    FilePath = Some fixturePath
                }
            )
        saveCwlFile =
            fun filePath yaml ->
                files.current <- files.current.Add(filePath, yaml)

                Promise.lift {
                    Success = true
                    FilePath = filePath
                    Error = None
                }
    }

    CwlEditor.CwlEditor(host = mockHost)
