namespace Swate.Components.NoteTypes


open ARCtrl

type NoteSearch = {
    Title: string
    Date: System.DateTime
    Tags:ResizeArray<OntologyAnnotation> option
    Content: string
}