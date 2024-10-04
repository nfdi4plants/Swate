module Components.Metadata.Assay

open Feliz
open Feliz.Bulma
open ARCtrl
open Shared
open Components
open Components.Forms

[<ReactComponent>]
let Main(assay: ArcAssay, setArcAssay: ArcAssay -> unit, setDatamap: ArcAssay -> DataMap option -> unit) = 
    Bulma.section [
        Generic.BoxedField
            "Assay Metadata"
            None
            [
                FormComponents.TextInput (
                    assay.Identifier,
                    "Identifier", 
                    (fun s -> 
                        let nextAssay = IdentifierSetters.setAssayIdentifier s assay
                        //nextAssay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setArcAssay nextAssay),
                    fullwidth=true
                )
                FormComponents.OntologyAnnotationInput(
                    assay.MeasurementType |> Option.defaultValue (OntologyAnnotation.empty()),
                    (fun oa -> 
                        let oa = if oa = (OntologyAnnotation.empty()) then None else Some oa
                        assay.MeasurementType <- oa
                        //assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setArcAssay assay),
                    "Measurement Type"
                )
                FormComponents.OntologyAnnotationInput(
                    assay.TechnologyType |> Option.defaultValue (OntologyAnnotation.empty()),
                    (fun oa -> 
                        let oa = if oa = (OntologyAnnotation.empty()) then None else Some oa
                        assay.TechnologyType <- oa
                        //assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setArcAssay assay),
                    "Technology Type"
                )
                FormComponents.OntologyAnnotationInput(
                    assay.TechnologyPlatform |> Option.defaultValue (OntologyAnnotation.empty()),
                    (fun oa -> 
                        let oa = if oa = (OntologyAnnotation.empty()) then None else Some oa
                        assay.TechnologyPlatform <- oa
                        //assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch),
                        setArcAssay assay),
                    "Technology Platform"
                )
                FormComponents.PersonsInput(
                    Array.ofSeq assay.Performers,
                    "Performers",
                    fun persons ->
                        assay.Performers <- ResizeArray persons
                        //assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                        setArcAssay assay
                )
                FormComponents.CommentsInput(
                    Array.ofSeq assay.Comments,
                    "Comments",
                    fun comments ->
                        assay.Comments <- ResizeArray comments
                        //assay |> Assay |> Spreadsheet.UpdateArcFile |> SpreadsheetMsg |> dispatch
                        setArcAssay assay
                )
            ]
        Datamap.DatamapConfig.Main(
            assay.DataMap,
            fun dataMap ->
                //logw "HARDCODED DTM EXTENSION!"
                //let create_Datacontext (i:int) =
                //    DataContext(
                //        $"id_string_{i}",
                //        "My Name",
                //        DataFile.DerivedDataFile,
                //        "My Format",
                //        "My Selector Format",
                //        OntologyAnnotation("Explication", "MS", "MS:123456"),
                //        OntologyAnnotation("Unit", "MS", "MS:123456"),
                //        OntologyAnnotation("ObjectType", "MS", "MS:123456"),
                //        "My Label",
                //        "My Description",
                //        "KevinF.exe",
                //        (ResizeArray [Comment.create("Hello", "World")])
                //    )
                //dtm |> Option.iter (fun dtm ->
                //    for i in 0 .. 5 do
                //        dtm.DataContexts.Add (create_Datacontext i)
                //)

                //dtm |> SpreadsheetInterface.UpdateDatamap |> InterfaceMsg |> dispatch
                setDatamap assay dataMap
        )
    ]