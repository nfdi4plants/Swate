namespace Swate.Components.NoteTypes
open ARCtrl

type Note = {
    RelativePath: string
    Title: string
    Date: System.DateTime
    Tags: ResizeArray<OntologyAnnotation> option
    Content: string
}
