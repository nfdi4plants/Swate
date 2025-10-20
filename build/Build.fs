open System
open ProjectInfo

[<EntryPoint>]
let main args =
    let argv = args |> Array.map (fun x -> x.ToLower()) |> Array.toList

    match argv with
    | "bundle" :: a ->
        run "npm" [ "install" ] "."

        match a with
        | "client" :: a ->
            match a with
            | "standalone" :: _ ->
                Bundle.Client(false)
                0
            | _ ->
                Bundle.Client(true)
                0
        | _ ->
            Bundle.All()
            0
    | "run" :: a ->
        match a with
        | "db" :: a ->
            Run.All(true)
            0
        | "client" :: a ->
            run "dotnet" Run.ClientArgs ProjectPaths.clientPath
            0
        | _ ->
            Run.All(false)
            0
    | "test" :: a ->
        Test.disableUserData ()
        Test.buildSharedTests ()

        match a with
        | "watch" :: _ ->
            Test.Watch()
            0
        | "js" :: _ ->
            Test.WatchJs()
            0
        | _ ->
            Test.Run()
            0
    | "release" :: target :: otherArgs ->
        let latestVersion = Changelog.getLatestVersion ()
        let isDryRun = otherArgs |> List.contains "--dry-run"
        let isCi = otherArgs |> List.contains "--ci"

        if not isCi then
            printRedfn "Currently the worklow only supports CI releases!"
            exit 1

        match target with
        | "nuget" ->
            let key = getEnvironementVariableOrFail "NUGET_KEY"

            Release.nuget key latestVersion isDryRun

            printGreenfn ("Release nuget!")
            0
        | "npm" ->
            let key = getEnvironementVariableOrFail "NPM_KEY"

            printGreenfn ("Starting NPM release!")

            Release.npm key latestVersion isDryRun

            printGreenfn "Released npm package version %O" latestVersion.Version
            0
        | "docker" ->
            let key = getEnvironementVariableOrFail "DOCKER_KEY"

            let user =
                otherArgs
                |> List.tryFind (fun x -> x.StartsWith "--user=")
                |> Option.map (fun x -> x.Substring 7)

            if user.IsNone then
                printRedfn "No docker user set! Please pass user in the format --user=yourusername"
                exit 1

            Release.docker user.Value key latestVersion isDryRun

            printGreenfn ("Release docker!")
            0
        | "electron" ->
            let GithubToken = getEnvironementVariableOrFail "GITHUB_TOKEN"
            Release.electron latestVersion GithubToken isDryRun
            printGreenfn ("Release electron!")
            0
        | "storybook" ->
            printfn "Starting Storybook release!"
            Release.storybook ()
            0
        | _ ->
            printRedfn ("No valid release target provided!")
            1
    | "prerelease" :: _ ->

        let GitHubToken = getEnvironementVariableOrFail "GITHUB_TOKEN"

        let latestVersion = Changelog.getLatestVersion ()
        let tags = Git.getTags () |> Array.toList
        let nextTag = latestVersion.Version.ToString()

        if tags |> List.contains (nextTag) && nextTag <> "1.0.0-rc.9" then
            printGreenfn
                "The latest version %O from CHANGELOG.md is already tagged in git. No release needed."
                latestVersion.Version

            GHActions.setShouldSkip ()
            0
        else
            printGreenfn "The latest version %O from CHANGELOG.md is not yet tagged in git." latestVersion.Version
            Git.createTagAndPush (nextTag)

            match GitHub.tryGetLatestRelease GitHubToken latestVersion with
            | Some _ -> printGreenfn "Release for version %O already exists on GitHub." latestVersion.Version
            | None -> GitHub.mkRelease GitHubToken latestVersion |> ignore

            0
    | "postrelease" :: _ ->

        let GitHubToken = getEnvironementVariableOrFail "GITHUB_TOKEN"
        let latestVersion = Changelog.getLatestVersion ()
        let isPrerelease = latestVersion.Version.IsPrerelease

        GitHub.updateRelease
            GitHubToken
            latestVersion
            (fun _ -> {
                tag_name = None
                target_commitish = None
                name = None
                body = None
                draft = Some false
                prerelease = Some isPrerelease
                make_latest = Some "true"
            })
        |> ignore

        0
    | "dev" :: a ->
        let token = ""
        let version = Changelog.getLatestVersion ()
        GitHub.tryGetLatestRelease token version |> printfn "%A"
        0
    | _ ->
        Console.WriteLine("No valid argument provided. Please provide a valid target.")
        1