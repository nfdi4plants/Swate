namespace Swate.Components.NoteTypes

open Feliz
open Fable.Core

type NoteSearch = {
    Id: int
    Title: string
    Date: System.DateTime
    Content: string
}