cwlVersion: v1.2
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
stdout: output.txt
