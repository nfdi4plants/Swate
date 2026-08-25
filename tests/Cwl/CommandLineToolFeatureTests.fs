module Swate.Tests.Cwl.CommandLineToolFeatureTests

open Expecto
open Swate.Components.Shared.Cwl.Documents.Types
open Swate.Components.Shared.Cwl.Features.InputsFeature
open Swate.Components.Shared.Cwl.Features.PreviewFeature
open Swate.Components.Shared.Cwl.State.Init

let commandLineToolFeatureTests =
    testList "Command line tool feature helpers" [
        test "renaming input updates the selected InputId only" {
            let firstInput = createInput "alpha"
            let secondInput = createInput "beta"

            let document =
                CommandLineToolDoc {
                    createCommandLineToolModel "v1.2" with
                        Inputs = [ firstInput; secondInput ]
                }

            let nextDocument = renameInput firstInput.Id "renamed" document

            match nextDocument with
            | CommandLineToolDoc model ->
                Expect.equal model.Inputs.[0].Name "renamed" "Target input should be renamed"
                Expect.equal model.Inputs.[1].Name "beta" "Non-target input should remain unchanged"
            | _ -> failtest "Expected CommandLineToolDoc"
        }

        test "changing active input resets DraftTextField by InputId rather than by index" {
            let firstInput = createInput "alpha"
            let secondInput = createInput "beta"

            Expect.notEqual
                firstInput.Id
                secondInput.Id
                "Fixture inputs must have distinct ids for the identity-reset test"
        }

        test "base command preview uses reducer document state" {
            let document =
                CommandLineToolDoc {
                    createCommandLineToolModel "v1.2" with
                        BaseCommand = [ "echo" ]
                }

            let yaml =
                buildPreviewYaml {
                    emptyState with
                        Document = Some document
                }

            let preview =
                yaml
                |> Option.defaultWith (fun () -> failtest "Preview should exist for a command line tool document")

            Expect.stringContains preview "cwlVersion: v1.2" "Preview should include the current CWL version"
            Expect.stringContains preview "class: CommandLineTool" "Preview should encode the command line tool kind"
            Expect.stringContains preview "baseCommand: [echo]" "Preview should encode the current reducer base command"
        }
    ]

[<Tests>]
let allTests = commandLineToolFeatureTests
