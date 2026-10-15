module ElectronRenderer.GitLabCreateProjectTests

open Swate.Components.Api.GitLabApi
open Vitest

let private expectNameRefused (expected: string option) (error: CreateProjectError) =
    match error with
    | CreateProjectError.NameRefused details -> Vitest.expect(details).toEqual (expected)
    | _ -> failwith "Expected NameRefused."

let private expectNotAllowed (expected: string option) (error: CreateProjectError) =
    match error with
    | CreateProjectError.NotAllowed details -> Vitest.expect(details).toEqual (expected)
    | _ -> failwith "Expected NotAllowed."

let private memberGroup id (name: string) (fullPath: string) (level: string option) = {|
    id = id
    name = name
    full_path = fullPath
    project_creation_level = level
|}

let private acceptingGroup id (name: string) (fullPath: string) = {|
    id = id
    name = name
    full_path = fullPath
|}

Vitest.describe (
    "classifyCreateProjectFailure",
    fun () ->
        Vitest.test (
            "reports a namespace error as not allowed whatever the status is",
            fun () ->
                classifyCreateProjectFailure 400 """{"message":{"namespace":["is not valid"]}}"""
                |> expectNotAllowed (Some "is not valid")

                classifyCreateProjectFailure 404 """{"message":{"namespace":["is not valid","is missing"]}}"""
                |> expectNotAllowed (Some "is not valid; is missing")
        )

        Vitest.test (
            "reports a forbidden response as not allowed with the message text when there is one",
            fun () ->
                classifyCreateProjectFailure 403 """{"message":"Project limit reached"}"""
                |> expectNotAllowed (Some "Project limit reached")

                classifyCreateProjectFailure 403 """{"message":{"base":["not permitted"]}}"""
                |> expectNotAllowed None
        )

        Vitest.test (
            "reports a refused name with the flattened field messages",
            fun () ->
                classifyCreateProjectFailure
                    400
                    """{"message":{"name":["has already been taken"],"path":["has already been taken"]}}"""
                |> expectNameRefused (Some "name has already been taken; path has already been taken")

                classifyCreateProjectFailure 409 """{"message":"Conflict"}"""
                |> expectNameRefused (Some "Conflict")

                classifyCreateProjectFailure 400 """{"message":{}}""" |> expectNameRefused None
        )

        Vitest.test (
            "maps every other status to a GitLab error",
            fun () ->
                match classifyCreateProjectFailure 401 """{"message":"401 Unauthorized"}""" with
                | CreateProjectError.Failed GitLabError.Unauthorized -> ()
                | _ -> failwith "Expected Failed Unauthorized."

                match classifyCreateProjectFailure 500 """{"message":"boom"}""" with
                | CreateProjectError.Failed(GitLabError.InvalidRequest text) ->
                    Vitest.expect(text).toContain ("HTTP 500")
                | _ -> failwith "Expected Failed InvalidRequest."
        )

        Vitest.test (
            "falls back to the status when the body is not JSON",
            fun () ->
                classifyCreateProjectFailure 400 "<html>Bad Request</html>"
                |> expectNameRefused None

                classifyCreateProjectFailure 403 "" |> expectNotAllowed None

                match classifyCreateProjectFailure 502 "<html>Bad Gateway</html>" with
                | CreateProjectError.Failed(GitLabError.InvalidRequest text) ->
                    Vitest.expect(text).toContain ("HTTP 502")
                | _ -> failwith "Expected Failed InvalidRequest."
        )
)

Vitest.describe (
    "mergePublishGroups",
    fun () ->
        Vitest.test (
            "flags the member groups that accept projects",
            fun () ->
                let merged =
                    mergePublishGroups [|
                        memberGroup 1 "Alpha" "alpha" (Some "maintainer")
                        memberGroup 2 "Beta" "beta" (Some "owner")
                    |] [| acceptingGroup 2 "Beta" "beta" |]

                Vitest
                    .expect(merged |> Array.map (fun group -> group.id, group.can_create_projects))
                    .toEqual ([| (1, false); (2, true) |])

                Vitest.expect(merged[0].project_creation_level).toEqual (Some "maintainer")
        )

        Vitest.test (
            "adds an accepting group that is missing from the member groups",
            fun () ->
                let merged =
                    mergePublishGroups [| memberGroup 1 "Alpha" "alpha" (Some "owner") |] [|
                        acceptingGroup 1 "Alpha" "alpha"
                        acceptingGroup 7 "Shared" "shared"
                    |]

                Vitest.expect(merged.Length).toBe (2)
                let extra = merged |> Array.find (fun group -> group.id = 7)
                Vitest.expect(extra.can_create_projects).toBe (true)
                Vitest.expect(extra.project_creation_level).toEqual (None)
                Vitest.expect(extra.full_path).toBe ("shared")
        )

        Vitest.test (
            "sorts by full path without regard to case",
            fun () ->
                let merged =
                    mergePublishGroups [|
                        memberGroup 1 "zeta" "Zeta" None
                        memberGroup 2 "alpha" "alpha" None
                        memberGroup 3 "Beta" "Beta" None
                    |] [||]

                Vitest.expect(merged |> Array.map _.full_path).toEqual ([| "alpha"; "Beta"; "Zeta" |])
        )

        Vitest.test (
            "lists a group once even when both lists repeat it",
            fun () ->
                let merged =
                    mergePublishGroups [|
                        memberGroup 1 "Alpha" "alpha" None
                        memberGroup 1 "Alpha" "alpha" None
                    |] [|
                        acceptingGroup 1 "Alpha" "alpha"
                        acceptingGroup 1 "Alpha" "alpha"
                    |]

                Vitest.expect(merged |> Array.map _.id).toEqual ([| 1 |])
        )
)
