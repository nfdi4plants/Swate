cwlVersion: v1.2
class: Workflow

inputs:
# Top-level comments between a key and its sequence are valid CWL YAML.
- id: sample_id
  type: string
- id: reads
  type: File

outputs:
# Keep this anonymized fixture shaped like real workflow files with section comments.
- id: report
  type: File
  outputSource: qc/report

steps:
# External runs should remain editable after loading.
- id: qc
  run: tools/qc.cwl
  in:
  - id: sample_id
    source: sample_id
  - id: reads
    source: reads
  out:
  - id: report
