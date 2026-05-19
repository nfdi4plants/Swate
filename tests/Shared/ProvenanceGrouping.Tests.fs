module ProvenanceGroupingTests

#if FABLE_COMPILER
open Fable.Mocha
#else
open Expecto
#endif

open Swate.Components.Shared.ProvenanceGrouping.Types

let private term name =
    {
        Name = name
        TermSource = None
        TermAccession = None
    }

let private ioHeader kind text =
    {
        Kind = kind
        Text = text
    }

let private propertyHeader kind name =
    {
        Kind = kind
        Category = term name
    }

let typeTests =
    testList "Types" [
        testCase "loaded input set carries the actual input name" <| fun _ ->
            let species = propertyHeader ProvenancePropertyKind.Characteristic "Species"
            let inputSet =
                {
                    Id = "input-a"
                    TableName = "assay-table"
                    Header = ioHeader ProvenanceIOKind.Sample "Input [Sample Name]"
                    Name = "Input A"
                    PropertyValueIds = [ "pv-species-a" ]
                }

            let propertyValue =
                {
                    Id = "pv-species-a"
                    Header = species
                    Value = ProvenanceValue.Text "Arabidopsis"
                    Unit = None
                    Source =
                        Some
                            {
                                TableName = "previous-table"
                                ProcessName = Some "previous-process"
                                Header = species
                                InputNames = [ "Ancestor A" ]
                                OutputNames = []
                            }
                }

            Expect.equal inputSet.Name "Input A" "Loaded input name should live on the set."
            Expect.equal inputSet.PropertyValueIds [ propertyValue.Id ] "Set should point to property value occurrence."

            match propertyValue.Source with
            | Some source ->
                Expect.equal source.TableName "previous-table" "Collapsed value should keep writeback table metadata."
            | None ->
                failwith "Expected collapsed value source."
    ]

let tests =
    testList "ProvenanceGrouping" [
        typeTests
    ]
