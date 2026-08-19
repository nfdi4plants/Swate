module Swate.Components.Composite.ValidationPackageSelector.Types

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

type OntologyAnnotationDTO = {
    Name: string option
    TermSourceREF: string option
    TermAccessionNumber: string option
}

type AuthorDTO = {
    FullName: string option
    Email: string option
    Affiliation: string option
    AffiliationLink: string option
}

type ValidationPackageDTO = {
    Name: string
    Summary: string
    Description: string
    MajorVersion: int
    MinorVersion: int
    PatchVersion: int
    PreReleaseVersionSuffix: string
    BuildMetadataVersionSuffix: string
    PackageContent: byte []
    ReleaseDate: System.DateTime
    Tags: OntologyAnnotationDTO []
    ReleaseNotes: string
    CQCHookEndpoint: string
    Authors: AuthorDTO []
    ProgrammingLanguage: string
} with 
    static member Create(
        name: string,
        summary: string,
        description: string,
        majorVersion: int,
        minorVersion: int,
        patchVersion: int,
        preReleaseVersionSuffix: string,
        buildMetadataVersionSuffix: string,
        packageContent: byte [],
        releaseDate: System.DateTime,
        tags: OntologyAnnotationDTO [],
        releaseNotes: string,
        cqcHookEndpoint: string,
        authors: AuthorDTO [],
        programmingLanguage: string
    ) : ValidationPackageDTO =
        {
            Name = name
            Summary = summary
            Description = description
            MajorVersion = majorVersion
            MinorVersion = minorVersion
            PatchVersion = patchVersion
            PreReleaseVersionSuffix = preReleaseVersionSuffix
            BuildMetadataVersionSuffix = buildMetadataVersionSuffix
            PackageContent = packageContent
            ReleaseDate = releaseDate
            Tags = tags
            ReleaseNotes = releaseNotes
            CQCHookEndpoint = cqcHookEndpoint
            Authors = authors
            ProgrammingLanguage = programmingLanguage
        }
