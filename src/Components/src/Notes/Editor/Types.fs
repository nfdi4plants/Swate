namespace Swate.Components.Notes.Editor

open ARCtrl
open Fable.Core

[<RequireQualifiedAccess>]
type NotesTargetKind =
    | Study
    | Assay

type ExistingTargetRef = {
    Name: string
    Kind: NotesTargetKind
}

[<RequireQualifiedAccess>]
type NotesTarget =
    | ExistingTarget of ExistingTargetRef
    | NewRootNote

type NotesDraft = {
    Title: string
    DateCreated: System.DateTime option
    Tags: ResizeArray<OntologyAnnotation>
    MainText: string
    SelectedExistingTarget: ExistingTargetRef option
} with
    static member init = {
        Title = ""
        DateCreated = None
        Tags = ResizeArray()
        MainText = ""
        SelectedExistingTarget = None
    }

type NotesUiState = {
    Error: string option
    IsSubmitting: bool
    ShowExistingTargetSelector: bool
} with
    static member init = {
        Error = None
        IsSubmitting = false
        ShowExistingTargetSelector = false
    }

type NotesProtocolIntent = {
    RelativePath: string
    Content: string
    Target: NotesTarget
}

type NotesSubmitPayload = {
    Intent: NotesProtocolIntent
    Title: string
    DateCreated: System.DateTime
    Tags: OntologyAnnotation list
}

[<Mangle(false)>]
module Exports =
    let createNotesDraft () = NotesDraft.init
    let createNotesUiState () = NotesUiState.init

    let createExistingTargetRef (name: string) (kind: string) =
        let normalizedKind = kind.Trim().ToLowerInvariant()

        let targetKind =
            match normalizedKind with
            | "assay" -> NotesTargetKind.Assay
            | _ -> NotesTargetKind.Study

        {
            Name = name
            Kind = targetKind
        }

    let createDemoExistingTargets () =
        ResizeArray [
            createExistingTargetRef "MyStudy" "study"
            createExistingTargetRef "MyAssay" "assay"
        ]
