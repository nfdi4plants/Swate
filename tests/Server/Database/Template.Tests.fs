module Template.Tests

open Newtonsoft.Json
open Expecto

let authorStr =
    """[{'Last Name': 'Jabeen', 'First Name': 'Hajira', 'Mid Initials': '', 'Email': '', 'Phone': '', 'Fax': '', 'Address': '', 'Affiliation': '', 'ORCID': '', 'Role': '', 'Role Term Accession Number': '', 'Role Term Source REF': ''}, {'Last Name': 'Brilhaus', 'First Name': 'Dominik', 'Mid Initials': '', 'Email': 'brilhaus@example.de', 'Phone': '', 'Fax': '', 'Address': '', 'Affiliation': '', 'ORCID': 'https://orcid.org/0000-0001-9021-3197', 'Role': '', 'Role Term Accession Number': '', 'Role Term Source REF': ''}, {'Last Name': 'Maus', 'First Name': 'Oliver', 'Mid Initials': '', 'Email': '', 'Phone': '', 'Fax': '', 'Address': '', 'Affiliation': '', 'ORCID': '', 'Role': '', 'Role Term Accession Number': '', 'Role Term Source REF': ''}, {'Last Name': 'Zhou', 'First Name': 'Xiaoran', 'Mid Initials': '', 'Email': '', 'Phone': '', 'Fax': '', 'Address': '', 'Affiliation': '', 'ORCID': '', 'Role': '', 'Role Term Accession Number': '', 'Role Term Source REF': ''}]"""

let tagStr = """[{'#': 'Plants', 'Term Accession Number': 'EXMP:000001', 'Term Source REF': 'EXMP'}, {'#': 'Sampling', 'Term Accession Number': '', 'Term Source REF': ''}]"""

open Database.Template

//let tests_templateDB = testList "JsonImport" [
    //testCase "Test json to author parsing." <| fun _ ->
    //    let authors = authorStr |> JsonConvert.DeserializeObject<Author[]>
    //    let oliver = authors |> Array.tryFind (fun x -> x.FirstName = "Oliver")
    //    let dominik = authors |> Array.tryFind (fun x -> x.FirstName = "Dominik")
    //    Expect.equal authors.Length 4 "check length"
    //    Expect.isSome oliver "oliver isSome"
    //    Expect.equal oliver.Value.LastName "Maus" "oliver lastname"
    //    Expect.isSome dominik "dominik isSome"
    //    Expect.equal dominik.Value.LastName "Brilhaus" "dominik lastname"
    //    Expect.equal dominik.Value.ORCID "https://orcid.org/0000-0001-9021-3197" "dominik orcid"
    //    Expect.equal dominik.Value.Email "brilhaus@example.de" "dominik email"       
    //testCase "Test json to tag parsing." <| fun _ ->
    //    let tags = tagStr |> JsonConvert.DeserializeObject<Tag[]>
    //    Expect.equal tags.Length 2 "check length"
    //    let plants = tags.[0]
    //    Expect.equal plants.Term "Plants" "plants.Term"
    //    Expect.equal plants.TermAccessionNumber "EXMP:000001" "plants.TermAccessionNumber"
    //    Expect.equal plants.TermSourceREF "EXMP" "plants.TermSourceREF"
    //    let sampling = tags.[1]
    //    Expect.equal sampling.Term "Sampling" "sampling.Term"
    //    Expect.equal sampling.TermAccessionNumber "" "sampling.TermAccessionNumber"
    //    Expect.equal sampling.TermSourceREF "" "sampling.TermSourceREF"
//]
