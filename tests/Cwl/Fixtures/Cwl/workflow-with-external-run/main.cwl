cwlVersion: v1.2
class: Workflow
inputs:
  message:
    type: string
outputs:
  final:
    type: string
    outputSource: echo_step/out
steps:
  echo_step:
    run: tools/echo.cwl
    in:
      message: message
    out: [out]
