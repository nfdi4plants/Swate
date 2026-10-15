module internal Swate.Components.Page.Metadata.FormComponents.PersonsInput.LookupTests

open ARCtrl
open Fable.Core
open Vitest
open Swate.Components.Page.Metadata.FormComponents.PersonsInput.Types
open Swate.Components.Page.Metadata.FormComponents.PersonsInput.Lookup

let private json<'T> text : 'T = JS.JSON.parse text |> unbox

let private emptyResult = {
    orcid = "0000-0002-1825-0097"
    firstName = None
    lastName = None
    displayName = None
    email = None
    affiliation = None
}

Vitest.describe (
    "Person ORCID lookup",
    fun () ->
        Vitest.test (
            "normalizes canonical ORCID identifiers and links including checksum X",
            fun () ->
                for query in
                    [
                        "0000-0002-1825-0097"
                        " 0000-0002-1825-0097 "
                        "https://orcid.org/0000-0002-1825-0097"
                        "http://orcid.org/0000-0002-1825-0097/?source=test#section"
                    ] do
                    Vitest.expect(normalizeOrcid query).toEqual (Ok "0000-0002-1825-0097")

                Vitest.expect(normalizeOrcid "0000-0002-1694-233x").toEqual (Ok "0000-0002-1694-233X")

                Vitest
                    .expect(recordUrl "https://orcid.org/0000-0002-1825-0097/")
                    .toEqual (Ok "https://pub.orcid.org/v3.0/0000-0002-1825-0097/record")
        )

        Vitest.test (
            "rejects malformed identifiers, foreign URLs, DOI values and invalid checksums",
            fun () ->
                for query in
                    [
                        ""
                        "10.1000/example"
                        "0000000218250097"
                        "0000-0002-1825-0098"
                        "https://example.org/0000-0002-1825-0097"
                        "https://orcid.org.evil.test/0000-0002-1825-0097"
                    ] do
                    Vitest.expect(Result.isError (normalizeOrcid query)).toBe true
        )

        Vitest.test (
            "quotes and escapes name tokens without inferring first and last names",
            fun () ->
                Vitest.expect(nameQuery "  Ada   Lovelace  ").toBe "given-and-family-names:(\"Ada\" AND \"Lovelace\")"
                Vitest.expect(nameQuery "A\"B C\\D").toBe "given-and-family-names:(\"A\\\"B\" AND \"C\\\\D\")"

                Vitest.expect(nameQuery "Smith OR Jones").toBe
                    "given-and-family-names:(\"Smith\" AND \"OR\" AND \"Jones\")"

                Vitest
                    .expect(nameSearchUrl "Ada & Lovelace" 10)
                    .toBe (
                        "https://pub.orcid.org/v3.0/expanded-search/?q="
                        + JS.encodeURIComponent "given-and-family-names:(\"Ada\" AND \"&\" AND \"Lovelace\")"
                        + "&rows=10&start=10"
                    )
        )

        Vitest.test (
            "maps only returned public record names, email and employment affiliations",
            fun () ->
                let response =
                    json<OrcidRecord>
                        """{"person":{"name":{"given-names":{"value":" Ada "},"family-name":{"value":"Lovelace"},"credit-name":{"value":"A. Lovelace"}},"emails":{"email":[{"email":""},{"email":"ada@example.org"}]}},"activities-summary":{"employments":{"affiliation-group":[{"summaries":[{"employment-summary":{"organization":{"name":"Example University"},"department-name":"Computing"}}]}]}}}"""

                let result = mapRecord emptyResult.orcid response
                Vitest.expect(result.orcid).toBe emptyResult.orcid
                Vitest.expect(result.firstName).toEqual (Some "Ada")
                Vitest.expect(result.lastName).toEqual (Some "Lovelace")
                Vitest.expect(result.email).toEqual (Some "ada@example.org")
                Vitest.expect(result.affiliation).toEqual (Some "Example University; Computing")
                Vitest.expect(fullName result).toBe "A. Lovelace"
        )

        Vitest.test (
            "handles private or missing record fields without inferring metadata",
            fun () ->
                let result = json<OrcidRecord> "{}" |> mapRecord emptyResult.orcid
                Vitest.expect(result.firstName.IsNone).toBe true
                Vitest.expect(result.lastName.IsNone).toBe true
                Vitest.expect(result.email.IsNone).toBe true
                Vitest.expect(result.affiliation.IsNone).toBe true
                Vitest.expect(fullName result).toBe emptyResult.orcid

                Vitest
                    .expect(
                        fullName {
                            result with
                                firstName = Some "Ada"
                                lastName = Some "Lovelace"
                        }
                    )
                    .toBe
                    "Ada Lovelace"
        )

        Vitest.test (
            "maps expanded search metadata directly and skips missing or invalid identifiers",
            fun () ->
                let raw =
                    json<OrcidExpandedResult>
                        """{"orcid-id":"0000-0002-1825-0097","given-names":"Ada","family-names":"Lovelace","credit-name":"A. Lovelace","email":["","ada@example.org"],"institution-name":["Example University","Example University","Institute"]}"""

                let result = (mapExpandedResult raw).Value
                Vitest.expect(result.firstName).toEqual (Some "Ada")
                Vitest.expect(result.lastName).toEqual (Some "Lovelace")
                Vitest.expect(result.displayName).toEqual (Some "A. Lovelace")
                Vitest.expect(result.email).toEqual (Some "ada@example.org")
                Vitest.expect(result.affiliation).toEqual (Some "Example University; Institute")
                Vitest.expect((mapExpandedResult (json<OrcidExpandedResult> "{}")).IsNone).toBe true

                Vitest.expect((mapExpandedResult (json<OrcidExpandedResult> """{"orcid-id":"invalid"}""")).IsNone).toBe
                    true
        )

        Vitest.test (
            "paginates expanded search by raw result count and stops at the public search limit",
            fun () ->
                let rawResults =
                    Array.create 10 """{"orcid-id":"0000-0002-1825-0097"}""" |> String.concat ","

                let response =
                    json<OrcidExpandedResponse> ("""{"expanded-result":[""" + rawResults + """],"num-found":25}""")

                Vitest.expect((mapExpandedPage 0 response).nextStart).toEqual (Some 10)
                Vitest.expect((mapExpandedPage 10 response).nextStart).toEqual (Some 20)
                Vitest.expect((mapExpandedPage 20 response).nextStart.IsNone).toBe true
                let empty = mapExpandedPage 0 (json<OrcidExpandedResponse> "{}")
                Vitest.expect(empty.results.Length).toBe 0
                Vitest.expect(empty.nextStart.IsNone).toBe true

                let limitResponse =
                    json<OrcidExpandedResponse> ("""{"expanded-result":[""" + rawResults + """],"num-found":20000}""")

                Vitest.expect((mapExpandedPage 9990 limitResponse).nextStart.IsNone).toBe true
        )

        Vitest.test (
            "preserves manually entered fields, roles and comments when applying public metadata",
            fun () ->
                let person =
                    Person(
                        firstName = "Manual",
                        lastName = "Name",
                        midInitials = "M",
                        email = "manual@example.org",
                        affiliation = "Manual affiliation",
                        phone = "123",
                        fax = "456",
                        address = "Address",
                        roles = ResizeArray [ OntologyAnnotation("Researcher") ],
                        comments = ResizeArray [ Comment("Note", "Keep") ]
                    )

                let result = {
                    emptyResult with
                        firstName = Some "Ada"
                        lastName = Some " "
                        affiliation = Some "University"
                }

                let merged = mergePerson person result
                Vitest.expect(merged.FirstName).toEqual (Some "Ada")
                Vitest.expect(merged.LastName).toEqual person.LastName
                Vitest.expect(merged.ORCID).toEqual (Some emptyResult.orcid)
                Vitest.expect(merged.Affiliation).toEqual (Some "University")
                Vitest.expect(merged.EMail).toEqual person.EMail
                Vitest.expect(merged.MidInitials).toEqual person.MidInitials
                Vitest.expect(merged.Phone).toEqual person.Phone
                Vitest.expect(merged.Fax).toEqual person.Fax
                Vitest.expect(merged.Address).toEqual person.Address
                Vitest.expect(merged.Roles.[0]).toEqual person.Roles.[0]
                Vitest.expect(merged.Comments.[0].Value).toEqual (Some "Keep")
                merged.Comments.[0].Value <- Some "Changed"
                Vitest.expect(person.Comments.[0].Value).toEqual (Some "Keep")
                Vitest.expect(person.FirstName).toEqual (Some "Manual")
        )

        Vitest.test (
            "distinguishes blocked anonymous access and retry-later errors",
            fun () ->
                for status in [ 401; 403 ] do
                    Vitest.expect((httpError status).StartsWith("Blocked: ORCID")).toBe true
                    Vitest.expect((httpError status).Contains("requires an access token or login")).toBe true

                Vitest.expect((httpError 429).Contains("try again later")).toBe true
                Vitest.expect((httpError 500).Contains("HTTP 500")).toBe true
        )
)
