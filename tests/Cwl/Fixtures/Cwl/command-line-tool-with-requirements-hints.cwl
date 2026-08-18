cwlVersion: v1.2
class: CommandLineTool
baseCommand: echo
requirements:
  - class: InlineJavascriptRequirement
hints:
  - class: DockerRequirement
    dockerPull: alpine:3.20
inputs:
  message:
    type: string
    inputBinding:
      position: 1
outputs:
  out:
    type: stdout
