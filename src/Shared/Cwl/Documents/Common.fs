module Swate.Components.Shared.Cwl.Documents.Common

open System

type DocumentId = DocumentId of Guid
type Revision = Revision of int
type InputId = InputId of Guid
type OutputId = OutputId of Guid
type StepId = StepId of Guid
type StepInputId = StepInputId of Guid
type StepOutputId = StepOutputId of Guid
type RequirementNodeId = RequirementNodeId of Guid

type StringMap = Map<string, string>

let emptyStringMap: StringMap = Map.empty

let newDocumentId () = DocumentId(Guid.NewGuid())
let newRevision value = Revision value
let newInputId () = InputId(Guid.NewGuid())
let newOutputId () = OutputId(Guid.NewGuid())
let newStepId () = StepId(Guid.NewGuid())
let newStepInputId () = StepInputId(Guid.NewGuid())
let newStepOutputId () = StepOutputId(Guid.NewGuid())
let newRequirementNodeId () = RequirementNodeId(Guid.NewGuid())
