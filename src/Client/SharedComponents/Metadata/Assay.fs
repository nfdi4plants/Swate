module Components.Metadata.Assay

open Feliz
open Feliz.DaisyUI
open ARCtrl
open Shared
open Components
open Components.Forms

[<ReactComponent>]
let Main(assay: ArcAssay, setArcAssay: ArcAssay -> unit, setDatamap: ArcAssay -> DataMap option -> unit) =
    Generic.Section [
        Generic.BoxedField(
            "Assay Metadata",
            content = [
                FormComponents.TextInput (
                    assay.Identifier,
                    (fun v ->
                        let nextAssay = IdentifierSetters.setAssayIdentifier v assay
                        setArcAssay nextAssay
                    ),
                    "Identifier",
                    validator = {| fn = (fun s -> ARCtrl.Helper.Identifier.tryCheckValidCharacters s); msg = "Invalid Identifier" |}
                )

                FormComponents.OntologyAnnotationInput(
                    assay.MeasurementType |> Option.defaultValue (OntologyAnnotation()),
                    (fun oa ->
                        assay.MeasurementType <- oa |> Option.whereNot _.isEmpty()
                        setArcAssay <| assay
                    ),
                    "Measurement Type"
                )
                FormComponents.OntologyAnnotationInput(
                    assay.TechnologyType |> Option.defaultValue (OntologyAnnotation.empty()),
                    (fun oa ->
                        assay.TechnologyType <- oa |> Option.whereNot _.isEmpty()
                        setArcAssay <| assay
                    ),
                    "Technology Type"
                )
                FormComponents.OntologyAnnotationInput(
                    assay.TechnologyPlatform |> Option.defaultValue (OntologyAnnotation.empty()),
                    (fun oa ->
                        assay.TechnologyPlatform <- oa |> Option.whereNot _.isEmpty()
                        setArcAssay <| assay
                    ),
                    "Technology Platform"
                )
                FormComponents.PersonsInput(
                    assay.Performers,
                    (fun persons ->
                        assay.Performers <- persons
                        setArcAssay assay),
                    "Performers"
                )
                FormComponents.CommentsInput(
                    assay.Comments,
                    (fun comments ->
                        assay.Comments <- ResizeArray comments
                        setArcAssay assay),
                    "Comments"
                )
            ]
        )
        Datamap.Main(
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