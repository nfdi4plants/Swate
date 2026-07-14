module ProcessCoreWritebackTests

open Expecto
open ProcessCoreProvenanceFixtures
open Swate.Components.Shared.ProvenanceGrouping.Types
open Swate.Components.Shared.ProvenanceGrouping.Session
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreConverter
open Swate.Components.Shared.ProvenanceGrouping.ProcessCoreWriteback

let private propertyByName name model =
    model.PropertyValues
    |> Map.toList
    |> List.find (fun (_, value) -> value.Header.Category.Name = name)

let private update propertyId value unit session =
    Session.updatePropertyValue propertyId value unit session |> expectOk |> fst

let tests =
    testList "ProcessCore writeback" [
        testCase "updates every indexed duplicate annotation in memory"
        <| fun _ ->
            let arc, parameterOne, parameterTwo = annotated ()
            let converted = fromArc loadedTable arc |> expectOk
            let propertyId, _ = propertyByName "parameter-neutral" converted.Model

            let session =
                Session.init converted.Model
                |> update propertyId (ProvenanceValue.Integer 9) None

            let summary = writeBack converted.Index session arc |> expectOk

            Expect.equal summary.UpdatedAnnotations 2 "Both occurrences must be updated."
            Expect.equal parameterOne.Value (Some "9") "First annotation must contain the invariant integer."
            Expect.equal parameterTwo.Value (Some "9") "Second annotation must contain the invariant integer."

        testCase "writes floats with invariant culture"
        <| fun _ ->
            let arc, parameterOne, parameterTwo = annotated ()
            let converted = fromArc loadedTable arc |> expectOk
            let propertyId, _ = propertyByName "parameter-neutral" converted.Model

            let session =
                Session.init converted.Model
                |> update propertyId (ProvenanceValue.Float 1.5) None

            let originalCulture = System.Globalization.CultureInfo.CurrentCulture

            try
                System.Globalization.CultureInfo.CurrentCulture <- System.Globalization.CultureInfo("de-DE")
                writeBack converted.Index session arc |> expectOk |> ignore
                Expect.equal parameterOne.Value (Some "1.5") "Float must use an invariant decimal separator."
                Expect.equal parameterTwo.Value (Some "1.5") "Every duplicate must use invariant formatting."
            finally
                System.Globalization.CultureInfo.CurrentCulture <- originalCulture

        testCase "writes term and unit accessions and clears them for text"
        <| fun _ ->
            let arc, _, _ = annotated ()
            let converted = fromArc loadedTable arc |> expectOk
            let propertyId, _ = propertyByName "category-neutral" converted.Model

            let term = {
                Name = "changed-neutral"
                TermSource = None
                TermAccession = Some "term:changed"
            }

            let unit = {
                Name = "changed-unit"
                TermSource = None
                TermAccession = Some "term:changed-unit"
            }

            let first =
                Session.init converted.Model
                |> update propertyId (ProvenanceValue.Term term) (Some unit)

            writeBack converted.Index first arc |> expectOk |> ignore
            let reconverted = fromArc loadedTable arc |> expectOk
            let nextId, _ = propertyByName "category-neutral" reconverted.Model

            let second =
                Session.init reconverted.Model
                |> update nextId (ProvenanceValue.Text "plain-neutral") None

            writeBack reconverted.Index second arc |> expectOk |> ignore

            let annotation = arc.AllProcesses().[0].Inputs.[0].AsSample().AdditionalProperty.[0]
            Expect.equal annotation.Value (Some "plain-neutral") "Text value must be written."
            Expect.isNone annotation.ValueTAN "Text write must clear the value accession."
            Expect.isNone annotation.Unit "Removing the unit must clear its text."
            Expect.isNone annotation.UnitTAN "Removing the unit must clear its accession."

        testCase "updates an upstream value at its original location"
        <| fun _ ->
            let arc, previousAnnotation = withPreviousContext ()
            let converted = fromArc loadedTable arc |> expectOk
            let propertyId, _ = propertyByName "previous-parameter" converted.Model

            let session =
                Session.init converted.Model
                |> update propertyId (ProvenanceValue.Text "changed-upstream") None

            writeBack converted.Index session arc |> expectOk |> ignore

            Expect.equal
                previousAnnotation.Value
                (Some "changed-upstream")
                "Writer must mutate the upstream annotation."
    ]
