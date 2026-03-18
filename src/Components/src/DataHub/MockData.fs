module Swate.Components.MockData.DataHub

open Swate.Components.DataHubTypes
open Swate.Components.Api.GitLabApi


let private ns id name path = {
    id = id
    name = name
    kind = "group"
    full_path = path
}

let groups: GroupDto array = [|
    {
        id = 100
        name = "NFDI4Plants"
        full_path = "nfdi4plants"
        web_url = "https://git.nfdi4plants.org/nfdi4plants"
        avatar_url = Some "https://picsum.photos/48/48?g=1"
    }
    {
        id = 101
        name = "Plant Data"
        full_path = "plant-data"
        web_url = "https://git.nfdi4plants.org/plant-data"
        avatar_url = Some "https://picsum.photos/48/48?g=2"
    }
|]

let yourRepos: ExploreProjectDto array = [|
    {
        id = 1
        name = "metabolomics-arc"
        path_with_namespace = "kevin/metabolomics-arc"
        name_with_namespace = "kevin / metabolomics-arc"
        description = Some "LC-MS metabolomics workflow"
        web_url = "https://git.nfdi4plants.org/kevin/metabolomics-arc"
        avatar_url = Some "https://picsum.photos/40/40?r=1"
        star_count = 7
        created_at = "2026-01-10T09:00:00Z"
        updated_at = "2026-03-10T09:00:00Z"
        last_activity_at = "2026-03-12T11:00:00Z"
        ``namespace`` = ns 1 "kevin" "kevin"
    }
    {
        id = 2
        name = "rnaseq-arc"
        path_with_namespace = "kevin/rnaseq-arc"
        name_with_namespace = "kevin / rnaseq-arc"
        description = Some "RNA-seq analysis templates"
        web_url = "https://git.nfdi4plants.org/kevin/rnaseq-arc"
        avatar_url = Some "https://picsum.photos/40/40?r=2"
        star_count = 11
        created_at = "2025-11-03T12:00:00Z"
        updated_at = "2026-03-01T09:00:00Z"
        last_activity_at = "2026-03-11T08:00:00Z"
        ``namespace`` = ns 1 "kevin" "kevin"
    }
    {
        id = 3
        name = "proteomics-arc"
        path_with_namespace = "kevin/proteomics-arc"
        name_with_namespace = "kevin / proteomics-arc"
        description = Some "Proteomics starter ARC"
        web_url = "https://git.nfdi4plants.org/kevin/proteomics-arc"
        avatar_url = None
        star_count = 3
        created_at = "2025-10-09T10:00:00Z"
        updated_at = "2026-02-27T09:00:00Z"
        last_activity_at = "2026-02-27T09:00:00Z"
        ``namespace`` = ns 1 "kevin" "kevin"
    }
|]

let mostStarred: ExploreProjectDto array = [|
    {
        id = 10
        name = "plant-ontology"
        path_with_namespace = "nfdi4plants/plant-ontology"
        name_with_namespace = "NFDI4Plants / Plant Ontology"
        description = Some "Controlled vocabulary and mappings"
        web_url = "https://git.nfdi4plants.org/nfdi4plants/plant-ontology"
        avatar_url = Some "https://picsum.photos/40/40?r=10"
        star_count = 233
        created_at = "2024-01-01T09:00:00Z"
        updated_at = "2026-03-08T10:00:00Z"
        last_activity_at = "2026-03-12T15:00:00Z"
        ``namespace`` = ns 100 "NFDI4Plants" "nfdi4plants"
    }
    {
        id = 11
        name = "arc-spec"
        path_with_namespace = "nfdi4plants/arc-spec"
        name_with_namespace = "NFDI4Plants / ARC Specification"
        description = Some "ARC specification"
        web_url = "https://git.nfdi4plants.org/nfdi4plants/arc-spec"
        avatar_url = Some "https://picsum.photos/40/40?r=11"
        star_count = 190
        created_at = "2024-02-01T09:00:00Z"
        updated_at = "2026-03-09T10:00:00Z"
        last_activity_at = "2026-03-12T14:00:00Z"
        ``namespace`` = ns 100 "NFDI4Plants" "nfdi4plants"
    }
    {
        id = 12
        name = "arc-templates"
        path_with_namespace = "plant-data/arc-templates"
        name_with_namespace = "Plant Data / ARC Templates"
        description = Some "Reusable ARC templates"
        web_url = "https://git.nfdi4plants.org/plant-data/arc-templates"
        avatar_url = Some "https://picsum.photos/40/40?r=12"
        star_count = 99
        created_at = "2025-01-01T09:00:00Z"
        updated_at = "2026-03-04T10:00:00Z"
        last_activity_at = "2026-03-10T14:00:00Z"
        ``namespace`` = ns 101 "Plant Data" "plant-data"
    }
|]

let orgRepos: Map<int, ExploreProjectDto array> =
    Map.ofArray [|
        100, [| mostStarred[0]; mostStarred[1] |]
        101,
        [|
            mostStarred[2]
            {
                id = 13
                name = "growth-model"
                path_with_namespace = "plant-data/growth-model"
                name_with_namespace = "Plant Data / Growth Model"
                description = Some "Plant growth model experiments"
                web_url = "https://git.nfdi4plants.org/plant-data/growth-model"
                avatar_url = None
                star_count = 27
                created_at = "2025-06-11T09:00:00Z"
                updated_at = "2026-03-01T10:00:00Z"
                last_activity_at = "2026-03-07T10:00:00Z"
                ``namespace`` = ns 101 "Plant Data" "plant-data"
            }
        |]
    |]