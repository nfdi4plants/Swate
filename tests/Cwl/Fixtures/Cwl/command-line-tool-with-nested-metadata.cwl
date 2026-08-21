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
customMetadata:
  nested:
    enabled: true
    thresholds: [1, 2.5]
    labels:
      primary: alpha
      secondary: beta
