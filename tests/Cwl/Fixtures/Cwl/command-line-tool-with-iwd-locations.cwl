cwlVersion: v1.2
class: CommandLineTool
baseCommand: echo
requirements:
- class: InitialWorkDirRequirement
  listing:
  - class: File
    location: file:///data/input.txt
  - class: Directory
    location: file:///data/refs
inputs:
  message:
    type: string
    inputBinding:
      position: 1
outputs:
  out:
    type: stdout
