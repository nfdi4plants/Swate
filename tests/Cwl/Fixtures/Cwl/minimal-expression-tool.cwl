cwlVersion: v1.2
class: ExpressionTool
requirements:
  - class: InlineJavascriptRequirement
inputs:
  input_val:
    type: int
outputs:
  output_val:
    type: int
expression: "${return {'output_val': inputs.input_val + 1};}"
