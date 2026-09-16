module ElectronRenderer.ValidationPackagesTargetHelperTests

open ARCtrl.ValidationPackages
open Renderer.Components.MainContent.ValidationPackagesTargetHelper
open Vitest

let private expectOk (result: Result<'T, exn>) =
    match result with
    | Ok value -> value
    | Error error -> failwith error.Message

let private arcitectYaml =
    "arc_specification: 2.0.0-draft\nvalidation_packages:\n  - name: invenio\n    version: 1.0.0\n"

Vitest.describe (
    "ValidationPackagesTargetHelper",
    fun () ->
        Vitest.test (
            "a missing config file starts with an empty config",
            fun () ->
                let config = parseConfigOrDefault None |> expectOk
                Vitest.expect(config.ValidationPackages.Count).toBe (0)
                Vitest.expect(config.ARCSpecification).toEqual (None)
        )

        Vitest.test (
            "a blank config file starts with an empty config",
            fun () ->
                let config = parseConfigOrDefault (Some "  \n") |> expectOk
                Vitest.expect(config.ValidationPackages.Count).toBe (0)
        )

        Vitest.test (
            "parses the YAML layout written by ARCitect",
            fun () ->
                let config = parseConfigOrDefault (Some arcitectYaml) |> expectOk
                Vitest.expect(config.ARCSpecification).toEqual (Some "2.0.0-draft")
                Vitest.expect(config.ValidationPackages.Count).toBe (1)
                Vitest.expect(config.ValidationPackages.[0].Name).toBe ("invenio")
                Vitest.expect(config.ValidationPackages.[0].Version).toEqual (Some "1.0.0")
        )

        Vitest.test (
            "serializes and parses back to a structurally equal config",
            fun () ->
                let config =
                    ValidationPackagesConfig.make
                        (ResizeArray [
                            ValidationPackage("invenio", ?version = Some "1.2.0")
                            ValidationPackage("custom")
                        ])
                        (Some "2.0.0-draft")

                let roundTripped = parseConfigOrDefault (Some(serializeConfig config)) |> expectOk
                Vitest.expect(roundTripped.StructurallyEquals config).toBe (true)
        )

        Vitest.test (
            "serializes in the compact layout ARCitect writes",
            fun () ->
                let config =
                    ValidationPackagesConfig.make
                        (ResizeArray [ ValidationPackage("invenio", ?version = Some "1.0.0") ])
                        (Some "2.0.0-draft")

                Vitest.expect((serializeConfig config).TrimEnd()).toBe (arcitectYaml.TrimEnd())
        )

        Vitest.test (
            "reports YAML that does not match the schema",
            fun () ->
                match parseConfigOrDefault (Some "just_a_key: without packages\n") with
                | Ok _ -> failwith "Expected the schema validation to fail."
                | Error _ -> ()
        )

        Vitest.test (
            "decodes AVPR JSON to the latest version per package",
            fun () ->
                let json =
                    """[
                        {"Name":"invenio","MajorVersion":1,"MinorVersion":0,"PatchVersion":0},
                        {"Name":"invenio","MajorVersion":1,"MinorVersion":1,"PatchVersion":0}
                    ]"""

                let packages = decodePackages json |> expectOk
                Vitest.expect(packages.Length).toBe (1)
                Vitest.expect(packages.[0].MinorVersion).toBe (1)
        )
)
