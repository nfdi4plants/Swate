module ElectronCore.ValidationPackagesConfigIOTests

open Fable.Core
open ARCtrl.ValidationPackages
open ARCtrl.Yaml
open Main.Bindings.Filesystem
open Main.Bindings.Path
open Main.ValidationPackages.ValidationPackagesConfigIO
open Vitest

let private withTempArcFolder (body: string -> JS.Promise<unit>) : JS.Promise<unit> = promise {
    let! rootPath = TestHelpers.createTempDirectoryAsync "swate-validation-packages-"
    let arcPath = join [| rootPath; "arc" |]
    let! _ = mkdirAsync arcPath (MkdirOptions(recursive = true))

    try
        do! body arcPath
        do! TestHelpers.removeDirectoryAsync rootPath
    with error ->
        do! TestHelpers.removeDirectoryAsync rootPath
        raise error
}

let private expectOk (result: Result<'T, exn>) =
    match result with
    | Ok value -> value
    | Error error -> failwith error.Message

let private sampleConfig () =
    ValidationPackagesConfig.make
        (ResizeArray [
            ValidationPackage("invenio", ?version = Some "1.0.0")
            ValidationPackage("custom")
        ])
        (Some "2.0.0-draft")

/// The layout ARCitect writes by hand. Swate must read it so both tools can edit the same ARC.
let private arcitectYaml =
    "arc_specification: 2.0.0-draft\nvalidation_packages:\n  - name: invenio\n    version: 1.0.0\n"

Vitest.describe (
    "ValidationPackagesConfigIO",
    fun () ->
        Vitest.test (
            "resolves the config below the .arc folder",
            fun () ->
                Vitest.expect(configRelativePath).toBe (".arc/validation_packages.yml")

                let absolutePath = configPathAtArcPath "C:/arcs/demo"
                Vitest.expect(absolutePath.EndsWith("validation_packages.yml")).toBe (true)
                Vitest.expect(absolutePath.Contains(".arc")).toBe (true)
        )

        Vitest.test (
            "reading a missing config yields None",
            fun () ->
                withTempArcFolder (fun arcPath -> promise {
                    let! result = readConfigYamlAtArcPath arcPath
                    Vitest.expect(expectOk result).toEqual (None)
                })
        )

        Vitest.test (
            "round-trips a config through the ARCtrl YAML codec",
            fun () ->
                withTempArcFolder (fun arcPath -> promise {
                    let config = sampleConfig ()
                    let! writeResult = writeConfigYamlAtArcPath arcPath (config.toYamlString ())
                    expectOk writeResult

                    let! exists = TestHelpers.pathExistsAsync (configPathAtArcPath arcPath)
                    Vitest.expect(exists).toBe (true)

                    let! readResult = readConfigYamlAtArcPath arcPath

                    match expectOk readResult with
                    | None -> failwith "Expected the written config to be readable."
                    | Some yaml ->
                        let parsed = tryParseConfigYaml yaml |> expectOk
                        Vitest.expect(parsed.StructurallyEquals config).toBe (true)
                        Vitest.expect(parsed.ARCSpecification).toEqual (Some "2.0.0-draft")
                        Vitest.expect(parsed.ValidationPackages.Count).toBe (2)
                })
        )

        Vitest.test (
            "parses the YAML layout written by ARCitect",
            fun () ->
                let parsed = tryParseConfigYaml arcitectYaml |> expectOk
                Vitest.expect(parsed.ARCSpecification).toEqual (Some "2.0.0-draft")
                Vitest.expect(parsed.ValidationPackages.Count).toBe (1)
                Vitest.expect(parsed.ValidationPackages.[0].Name).toBe ("invenio")
                Vitest.expect(parsed.ValidationPackages.[0].Version).toEqual (Some "1.0.0")
        )

        Vitest.test (
            "rejects YAML that does not match the config schema and keeps the previous file",
            fun () ->
                withTempArcFolder (fun arcPath -> promise {
                    let! _ = writeConfigYamlAtArcPath arcPath arcitectYaml

                    let! rejected = writeConfigYamlAtArcPath arcPath "just_a_key: without packages\n"

                    match rejected with
                    | Ok() -> failwith "Expected schema validation to reject the YAML."
                    | Error error -> Vitest.expect(error.Message).toContain ("not valid")

                    let! readResult = readConfigYamlAtArcPath arcPath
                    Vitest.expect(expectOk readResult).toEqual (Some arcitectYaml)
                })
        )

        Vitest.test (
            "rejects an empty ARC path",
            fun () -> promise {
                let! readResult = readConfigYamlAtArcPath ""
                let! writeResult = writeConfigYamlAtArcPath "   " arcitectYaml

                match readResult, writeResult with
                | Error _, Error _ -> ()
                | _ -> failwith "Expected both operations to fail for an empty ARC path."
            }
        )
)
