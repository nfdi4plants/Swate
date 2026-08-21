namespace Swate.Components.Composite.ValidationPackageSelector

open Fable.Core
open Feliz


// [
//   {
//     "Name": "MyPackage",
//     "Summary": "MyPackage does the thing",
//     "Description": "MyPackage does the thing.\nIt does it very good, it does it very well.\nIt does it very fast, it does it very swell.",
//     "MajorVersion": 1,
//     "MinorVersion": 0,
//     "PatchVersion": 0,
//     "PreReleaseVersionSuffix": "alpha.1",
//     "BuildMetadataVersionSuffix": "0",
//     "PackageContent": "aHR0cHM6Ly93d3cueW91dHViZS5jb20vd2F0Y2g/dj1kUXc0dzlXZ1hjUQ==",
//     "ReleaseDate": "2026-08-19",
//     "Tags": [
//       {
//         "Name": "string",
//         "TermSourceREF": "string",
//         "TermAccessionNumber": "string"
//       }
//     ],
//     "ReleaseNotes": "string",
//     "CQCHookEndpoint": "string",
//     "Authors": [
//       {
//         "FullName": "string",
//         "Email": "string",
//         "Affiliation": "string",
//         "AffiliationLink": "string"
//       }
//     ],
//     "ProgrammingLanguage": "string"
//   }
// ]

[<Erase; Mangle(false)>]
type ValidationPackageSelectorFixture = 

    [<ReactComponent(true)>]
    static member Main () =
        ValidationPackageSelector.ValidationPackageSelector()
