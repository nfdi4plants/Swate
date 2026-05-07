namespace Main.ArcMerge

[<RequireQualifiedAccess>]
type EventName =
    | Add
    | Unlink
    | Change

type FileEvent = { EventName: EventName; Path: string }

[<RequireQualifiedAccess>]
type ArcEntityRef =
    | Investigation
    | Assay of string
    | AssayDataMap of string
    | Study of string
    | StudyDataMap of string
    | Run of string
    | RunDataMap of string
    | Workflow of string
    | WorkflowDataMap of string
    | Unknown of string
