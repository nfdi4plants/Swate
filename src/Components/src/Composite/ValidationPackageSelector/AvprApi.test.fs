module internal Swate.Components.Tests.ValidationPackageSelector.AvprApi

open Swate.Components.Composite.ValidationPackageSelector.AvprApi
open Swate.Components.Composite.ValidationPackageSelector.Helper
open Vitest

let private sampleJson =
    """
[
  {
    "Name": "invenio",
    "Summary": "Invenio checks",
    "Description": "Checks invenio metadata.",
    "MajorVersion": 1,
    "MinorVersion": 0,
    "PatchVersion": 0,
    "PreReleaseVersionSuffix": "",
    "BuildMetadataVersionSuffix": "",
    "PackageContent": "aGVsbG8=",
    "ReleaseDate": "2026-08-19",
    "Tags": [ { "Name": "Publication", "TermSourceREF": null, "TermAccessionNumber": null } ],
    "ReleaseNotes": "",
    "CQCHookEndpoint": "",
    "Authors": [ { "FullName": "Kevin Frey", "Email": null, "Affiliation": null, "AffiliationLink": null } ],
    "ProgrammingLanguage": "fsharp"
  },
  {
    "Name": "invenio",
    "Summary": "Invenio checks",
    "Description": "Checks invenio metadata.",
    "MajorVersion": 1,
    "MinorVersion": 2,
    "PatchVersion": 0,
    "PreReleaseVersionSuffix": "",
    "BuildMetadataVersionSuffix": "",
    "PackageContent": "aGVsbG8=",
    "ReleaseDate": "2026-09-01",
    "Tags": [],
    "ReleaseNotes": "",
    "CQCHookEndpoint": "",
    "Authors": [],
    "ProgrammingLanguage": "fsharp"
  },
  {
    "Name": "invenio",
    "Summary": "Invenio checks",
    "Description": "Checks invenio metadata.",
    "MajorVersion": 1,
    "MinorVersion": 2,
    "PatchVersion": 0,
    "PreReleaseVersionSuffix": "alpha.1",
    "BuildMetadataVersionSuffix": "",
    "PackageContent": "aGVsbG8=",
    "ReleaseDate": "2026-08-25",
    "Tags": [],
    "ReleaseNotes": "",
    "CQCHookEndpoint": "",
    "Authors": [],
    "ProgrammingLanguage": "fsharp"
  },
  {
    "Name": "test",
    "Summary": null,
    "Description": null,
    "MajorVersion": 0,
    "MinorVersion": 1,
    "PatchVersion": 0,
    "PreReleaseVersionSuffix": null,
    "BuildMetadataVersionSuffix": null,
    "PackageContent": null,
    "ReleaseDate": null,
    "Tags": null,
    "ReleaseNotes": null,
    "CQCHookEndpoint": null,
    "Authors": null,
    "ProgrammingLanguage": null
  },
  {
    "Summary": "An entry without a name is dropped"
  }
]
"""

let private expectOk (result: Result<'T, exn>) =
    match result with
    | Ok value -> value
    | Error error -> failwith error.Message

Vitest.describe (
    "AvprApi.decodePackages",
    fun () ->
        Vitest.test (
            "decodes every named entry and tolerates null fields",
            fun () ->
                let packages = decodePackages sampleJson |> expectOk

                Vitest.expect(packages.Length).toBe (4)

                let first = packages.[0]
                Vitest.expect(first.Name).toBe ("invenio")
                Vitest.expect(toVersionString first).toBe ("1.0.0")
                Vitest.expect(first.ReleaseDate.ToString("yyyy-MM-dd")).toBe ("2026-08-19")
                Vitest.expect(first.Tags.[0].Name).toEqual (Some "Publication")
                Vitest.expect(first.Tags.[0].TermSourceREF).toEqual (None)
                Vitest.expect(first.Authors.[0].FullName).toEqual (Some "Kevin Frey")

                let nullHeavy = packages.[3]
                Vitest.expect(nullHeavy.Name).toBe ("test")
                Vitest.expect(nullHeavy.Summary).toBe ("")
                Vitest.expect(nullHeavy.PreReleaseVersionSuffix).toBe ("")
                Vitest.expect(nullHeavy.Tags.Length).toBe (0)
                Vitest.expect(nullHeavy.Authors.Length).toBe (0)
                Vitest.expect(toVersionString nullHeavy).toBe ("0.1.0")
        )

        Vitest.test (
            "rejects payloads that are not a JSON array",
            fun () ->
                match decodePackages """{"Name":"invenio"}""" with
                | Ok _ -> failwith "Expected an error for a non-array payload."
                | Error error -> Vitest.expect(error.Message).toContain ("JSON array")
        )

        Vitest.test (
            "rejects malformed JSON",
            fun () ->
                match decodePackages "not json" with
                | Ok _ -> failwith "Expected an error for malformed JSON."
                | Error _ -> ()
        )
)

Vitest.describe (
    "AvprApi.latestPerPackage",
    fun () ->
        Vitest.test (
            "keeps one row per package with the newest stable release winning over pre-releases",
            fun () ->
                let latest = decodeLatestPackages sampleJson |> expectOk

                Vitest.expect(latest.Length).toBe (2)

                let invenio = latest |> Array.find (fun dto -> dto.Name = "invenio")
                Vitest.expect(toVersionString invenio).toBe ("1.2.0")

                let test = latest |> Array.find (fun dto -> dto.Name = "test")
                Vitest.expect(toVersionString test).toBe ("0.1.0")
        )

        Vitest.test (
            "returns an empty array for an empty listing",
            fun () -> Vitest.expect((latestPerPackage [||]).Length).toBe (0)
        )
)
