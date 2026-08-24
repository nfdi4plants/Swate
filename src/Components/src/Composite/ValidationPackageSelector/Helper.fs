module Swate.Components.Composite.ValidationPackageSelector.Helper

open ARCtrl.ValidationPackages
open Types

let pageSize = 20

let hasFlag (fields: SearchFields) (flag: SearchFields) = (int fields &&& int flag) = int flag

let toggleFlag (fields: SearchFields) (flag: SearchFields) =
    enum<SearchFields> (int fields ^^^ int flag)

let allSearchFields = [|
    SearchFields.Name
    SearchFields.Summary
    SearchFields.Description
    SearchFields.Tags
    SearchFields.Authors
|]

let searchFieldLabel (field: SearchFields) =
    match field with
    | SearchFields.Name -> "Name"
    | SearchFields.Summary -> "Summary"
    | SearchFields.Description -> "Description"
    | SearchFields.Tags -> "Tags"
    | SearchFields.Authors -> "Authors"
    | _ -> ""

let toVersionString (dto: ValidationPackageDTO) =
    let baseVersion = $"{dto.MajorVersion}.{dto.MinorVersion}.{dto.PatchVersion}"

    [|
        if dto.PreReleaseVersionSuffix <> "" then
            $"-{dto.PreReleaseVersionSuffix}"
        if dto.BuildMetadataVersionSuffix <> "" then
            $"+{dto.BuildMetadataVersionSuffix}"
    |]
    |> String.concat ""
    |> fun suffix -> baseVersion + suffix

let rowState (config: ValidationPackagesConfig) (dto: ValidationPackageDTO) =
    config.ValidationPackages
    |> Seq.tryFind (fun (vp: ValidationPackage) -> vp.Name = dto.Name)
    |> Option.map (fun vp ->
        if vp.Version = Some(toVersionString dto) then
            PackageRowState.Checked
        else
            PackageRowState.HasOlderVersion
    )
    |> Option.defaultValue PackageRowState.Unchecked

let private containsIgnoreCase (haystack: string) (needle: string) =
    haystack.ToLowerInvariant().Contains(needle.ToLowerInvariant())

let filterBySearch (fields: SearchFields) (query: string) (packages: ValidationPackageDTO[]) =
    if query = "" || int fields = 0 then
        packages
    else
        packages
        |> Array.filter (fun dto ->
            let matchesName =
                hasFlag fields SearchFields.Name && containsIgnoreCase dto.Name query

            let matchesSummary =
                hasFlag fields SearchFields.Summary && containsIgnoreCase dto.Summary query

            let matchesDescription =
                hasFlag fields SearchFields.Description
                && containsIgnoreCase dto.Description query

            let matchesTags =
                hasFlag fields SearchFields.Tags
                && dto.Tags
                   |> Array.exists (fun t ->
                       t.Name
                       |> Option.map (fun name -> containsIgnoreCase name query)
                       |> Option.defaultValue false
                   )

            let matchesAuthors =
                hasFlag fields SearchFields.Authors
                && dto.Authors
                   |> Array.exists (fun a ->
                       a.FullName
                       |> Option.map (fun name -> containsIgnoreCase name query)
                       |> Option.defaultValue false
                   )

            matchesName
            || matchesSummary
            || matchesDescription
            || matchesTags
            || matchesAuthors
        )

let filterByTag (tag: string option) (packages: ValidationPackageDTO[]) =
    match tag with
    | None -> packages
    | Some tag ->
        packages
        |> Array.filter (fun dto -> dto.Tags |> Array.exists (fun t -> t.Name = Some tag))

let filterByAuthor (author: string option) (packages: ValidationPackageDTO[]) =
    match author with
    | None -> packages
    | Some author ->
        packages
        |> Array.filter (fun dto -> dto.Authors |> Array.exists (fun a -> a.FullName = Some author))

let distinctTags (packages: ValidationPackageDTO[]) =
    packages
    |> Array.collect (fun dto -> dto.Tags |> Array.choose (fun t -> t.Name))
    |> Array.distinct
    |> Array.sort

let distinctAuthors (packages: ValidationPackageDTO[]) =
    packages
    |> Array.collect (fun dto -> dto.Authors |> Array.choose (fun a -> a.FullName))
    |> Array.distinct
    |> Array.sort

let pageCount (packages: ValidationPackageDTO[]) =
    if packages.Length = 0 then
        0
    else
        (packages.Length + pageSize - 1) / pageSize

let slicePage (packages: ValidationPackageDTO[]) (page: int) =
    let startIndex = page * pageSize

    if startIndex >= packages.Length then
        [||]
    else
        let count = min pageSize (packages.Length - startIndex)
        packages.[startIndex .. startIndex + count - 1]

let unlistedNames (config: ValidationPackagesConfig) (packages: ValidationPackageDTO[]) =
    let tableNames = packages |> Array.map (fun p -> p.Name) |> Set.ofArray

    config.ValidationPackages
    |> Seq.map (fun (vp: ValidationPackage) -> vp.Name)
    |> Seq.distinct
    |> Seq.filter (fun name -> not (tableNames.Contains name))
    |> Seq.toArray

let computeNewPackages
    (config: ValidationPackagesConfig)
    (packages: ValidationPackageDTO[])
    (edits: Map<string, ValidationPackage option>)
    (removedUnlisted: Set<string>)
    =
    let tableNames = packages |> Array.map (fun p -> p.Name) |> Set.ofArray

    let result = ResizeArray<ValidationPackage>()
    let mutable editedNamesSeen = Set.empty<string>

    for vp in config.ValidationPackages do
        if removedUnlisted.Contains vp.Name then
            ()
        elif tableNames.Contains vp.Name && edits.ContainsKey vp.Name then
            if not (editedNamesSeen.Contains vp.Name) then
                editedNamesSeen <- editedNamesSeen.Add vp.Name

                match edits.[vp.Name] with
                | Some p -> result.Add p
                | None -> ()
        else
            result.Add vp

    let resultNames =
        result |> Seq.map (fun (vp: ValidationPackage) -> vp.Name) |> Set.ofSeq

    for dto in packages do
        match edits.TryFind dto.Name with
        | Some(Some p) when not (resultNames.Contains dto.Name) -> result.Add p
        | _ -> ()

    result |> Seq.toArray
