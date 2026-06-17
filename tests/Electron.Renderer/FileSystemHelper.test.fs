module ElectronRenderer.FileSystemHelperTests

open Renderer.Components.Helper.FileSystemHelper
open Vitest

Vitest.describe (
    "FileSystemHelper",
    fun () ->
        Vitest.test (
            "checkTargetAvailability normalizes the target and returns Empty when it does not exist",
            fun () -> promise {
                let requestedPaths = ResizeArray<string>()

                let pathExists path = promise {
                    requestedPaths.Add path
                    return Ok false
                }

                let! result = checkTargetAvailability pathExists "notes\\2026-06-15\\draft.md/"

                match result with
                | Ok TargetAvailability.Empty -> ()
                | _ -> failwith $"Expected empty target availability, got {result}."

                Vitest.expect(requestedPaths.ToArray()).toEqual ([| "notes/2026-06-15/draft.md" |])
            }
        )

        Vitest.test (
            "checkTargetAvailability returns Exists when the target exists",
            fun () -> promise {
                let pathExists _ = promise { return Ok true }

                let! result = checkTargetAvailability pathExists "notes/2026-06-15/draft.md"

                match result with
                | Ok TargetAvailability.Exists -> ()
                | _ -> failwith $"Expected existing target availability, got {result}."
            }
        )

        Vitest.test (
            "checkTargetAvailability propagates path check errors",
            fun () -> promise {
                let expectedError = exn "path check failed"
                let pathExists _ = promise { return Error expectedError }

                let! result = checkTargetAvailability pathExists "notes/2026-06-15/draft.md"

                match result with
                | Error actualError -> Vitest.expect(actualError.Message).toBe ("path check failed")
                | Ok availability -> failwith $"Expected path check failure, got {availability}."
            }
        )
)
