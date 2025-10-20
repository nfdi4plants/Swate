module GitHub

open System.IO
open FSharp.Data
open System.Text.Json

type ReleaseResponse = {
    url: string
    assets_url: string
    upload_url: string
    html_url: string
    id: int
    tag_name: string
    name: string
    body: string
}

type UpdateReleaseRequest = {
    tag_name: string option
    name: string option
    body: string option
}

let mkHeaders token = [
    "Authorization", sprintf "Bearer %s" token
    "Accept", "application/vnd.github+json"
    "X-GitHub-Api-Version", "2022-11-28"
    "user-agent", "Swate build script"
]

let toJson (data: (string * obj) list) = JsonSerializer.Serialize(dict data)

let mkRelease (token: string) (version: Changelog.Version) =
    let endpoint =
        $"https://api.github.com/repos/{ProjectInfo.gitOwner}/{ProjectInfo.project}/releases"

    Http.RequestString(
        endpoint,
        httpMethod = "POST",
        headers = mkHeaders token,
        body =
            TextRequest(
                toJson [
                    "tag_name", version.Version.ToString() :> obj
                    "name", version.Version.ToString() :> obj
                    "body", version.Body :> obj
                    "draft", true :> obj
                    "generate_release_notes ", true
                ]
            )
    )
    |> JsonSerializer.Deserialize<ReleaseResponse>

let tryGetRelease (token: string) (version: Changelog.Version) =
    let version = version.Version.ToString()

    let endpoint =
        $"https://api.github.com/repos/{ProjectInfo.gitOwner}/{ProjectInfo.project}/releases/tags/{version}"

    let response =
        Http.Request(endpoint, httpMethod = "GET", headers = mkHeaders token, silentHttpErrors = true)

    match response.StatusCode with
    | 404 ->
        printfn "[GitHub] Release %O not found" version
        None
    | 200 ->
        printfn "[GitHub] Found Release %O" version

        let jsonStr =
            match response.Body with
            | Text text -> text
            | _ -> failwith "Unexpected response body"

        let response = JsonSerializer.Deserialize<ReleaseResponse>(jsonStr)
        Some response
    | code -> failwithf "Error: unexpected status code %d" code

let updateRelease (token: string) (version: Changelog.Version) (fn: ReleaseResponse -> UpdateReleaseRequest) =
    let versionStr = version.Version.ToString()

    let id =
        tryGetRelease token version
        |> Option.defaultWith (fun () -> failwithf "Release %s not found" versionStr)

    let request = fn id

    let endpoint =
        $"https://api.github.com/repos/{ProjectInfo.gitOwner}/{ProjectInfo.project}/releases/{id}"

    Http.Request(
        endpoint,
        httpMethod = "PATCH",
        headers = mkHeaders token,
        body =
            TextRequest(
                toJson [
                    if request.tag_name.IsSome then
                        "tag_name", request.tag_name.Value :> obj
                    if request.name.IsSome then
                        "name", request.name.Value :> obj
                    if request.body.IsSome then
                        "body", request.body.Value :> obj
                ]
            )
    )

let uploadReleaseAsset (token: string) (version: Changelog.Version) (filePath: string) =
    let versionStr = version.Version.ToString()

    let release =
        tryGetRelease token version
        |> Option.defaultWith (fun () -> failwithf "Release %s not found" versionStr)

    let endpoint =
        $"https://uploads.github.com/repos/{ProjectInfo.gitOwner}/{ProjectInfo.project}/releases/{release.id}/assets"

    let fileName = Path.GetFileName(filePath)

    let fileBytes = File.ReadAllBytes(filePath)

    Http.Request(
        endpoint,
        httpMethod = "POST",
        headers = mkHeaders token @ [ "Content-Type", "application/octet-stream" ],
        query = [ "name", fileName; "label", fileName ],
        body = BinaryUpload fileBytes
    )