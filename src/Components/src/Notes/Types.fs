namespace Swate.Components.NoteTypes

open ARCtrl

type NoteSearch = {
    RelativePath: string
    Title: string
    Date: System.DateTime
    Tags: string[]
    Content: string
}
