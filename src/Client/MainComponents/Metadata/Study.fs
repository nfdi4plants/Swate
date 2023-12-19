module MainComponents.Metadata.Study

open Feliz
open Feliz.Bulma
open Messages
open ARCtrl.ISA
open Shared

let Main(study: ArcStudy, assignedAssays: ArcAssay list, model: Messages.Model, dispatch: Msg -> unit) = 
    Bulma.section [ 
        FormComponents.TextInput (
            study.Identifier,
            "Identifier", 
            fun s -> 
                let nextAssay = IdentifierSetters.setStudyIdentifier s study
                (nextAssay, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.TextInput (
            Option.defaultValue "" study.Description,
            "Description", 
            fun s -> 
                let s = if s = "" then None else Some s
                study.Description <- s
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
        )
        FormComponents.PersonsInput(
            study.Contacts,
            "Contacts",
            fun persons ->
                study.Contacts <- persons
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
        FormComponents.CommentsInput(
            study.Comments,
            "Comments",
            fun comments ->
                study.Comments <- comments
                (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch 
        )
    ]