module Components.Metadata.Study

open Feliz.Bulma
open ARCtrl
open Components
open Components.Forms

let Main(study: ArcStudy, assignedAssays: ArcAssay list, setArcStudy: (ArcStudy * ArcAssay list) -> unit, setDatamap: ArcStudy -> DataMap option -> unit) = 
    Bulma.section [
        Generic.BoxedField
            "Study Metadata"
            None
            [
                FormComponents.TextInput (
                    study.Identifier,
                    "Identifier", 
                    (fun s -> 
                        let nextStudy = IdentifierSetters.setStudyIdentifier s study
                        //(nextStudy, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setArcStudy (nextStudy , assignedAssays)),
                    fullwidth=true
                )
                FormComponents.TextInput (
                    Option.defaultValue "" study.Description,
                    "Description", 
                    (fun s -> 
                        let s = if s = "" then None else Some s
                        study.Description <- s
                        //(study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setArcStudy (study , assignedAssays)),
                    fullwidth=true,
                    isarea=true
                )
                FormComponents.PersonsInput(
                    Array.ofSeq study.Contacts,
                    "Contacts",
                    fun persons ->
                        study.Contacts <- ResizeArray(persons)
                        //(study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                        setArcStudy (study , assignedAssays)
                )
                FormComponents.PublicationsInput (
                    Array.ofSeq study.Publications,
                    "Publications",
                    fun pubs -> 
                        study.Publications <- ResizeArray(pubs)
                        //(study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                        setArcStudy (study , assignedAssays)
                )
                FormComponents.DateTimeInput(
                    Option.defaultValue "" study.SubmissionDate,
                    "Submission Date", 
                    fun s -> 
                        let s = if s = "" then None else Some s
                        study.SubmissionDate <- s
                        //(study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                        setArcStudy (study , assignedAssays)
                )
                FormComponents.DateTimeInput (
                    Option.defaultValue "" study.PublicReleaseDate,
                    "Public ReleaseDate", 
                    fun s -> 
                        let s = if s = "" then None else Some s
                        study.PublicReleaseDate <- s
                        //(study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                        setArcStudy (study , assignedAssays)
                )
                FormComponents.OntologyAnnotationsInput(
                    Array.ofSeq study.StudyDesignDescriptors,
                    "Study Design Descriptors",
                    fun oas ->
                        study.StudyDesignDescriptors <- ResizeArray(oas)
                        //(study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                        setArcStudy (study , assignedAssays)
                )
                //FormComponents.TextInputs(
                //    Array.ofSeq study.RegisteredAssayIdentifiers,
                //    "Registered Assay Identifiers",
                //    fun rais ->
                //        study.RegisteredAssayIdentifiers <- ResizeArray(rais)
                //        (study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                //)
                FormComponents.CommentsInput(
                    Array.ofSeq study.Comments,
                    "Comments",
                    fun comments ->
                        study.Comments <- ResizeArray(comments)
                        //(study, assignedAssays) |> Study |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                        setArcStudy (study , assignedAssays)
                )
            ]
        Datamap.DatamapConfig.Main(
            study.DataMap,
            fun dataMap ->
                //dtm |> SpreadsheetInterface.UpdateDatamap |> InterfaceMsg |> dispatch
                setDatamap study dataMap
        )
    ]