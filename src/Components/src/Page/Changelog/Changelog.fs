namespace Swate.Components.Page.Changelog

open Fable.Core
open Feliz
open Swate.Components.Api.GitHubReleases
open Swate.Components.Composite.MarkdownText.JsBindings

[<Erase; Mangle(false)>]
type Changelog =

    /// currentRelease accepts an exact tag or a semantic version (with or without v).
    /// Starts at the matching release, or the first release when no match exists.
    /// Navigation preserves the order returned by fetchReleases.
    [<ReactComponent(true)>]
    static member Changelog(fetchReleases: unit -> JS.Promise<Release[]>, currentRelease: string) =
        let releases, setReleases = React.useState<Release[]> ([||])
        let selectedIndex, setSelectedIndex = React.useState 0
        let loading, setLoading = React.useState true
        let error, setError = React.useState<string option> None
        let retry, setRetry = React.useState 0

        React.useEffect (
            (fun () ->
                let mutable active = true
                setLoading true
                setError None
                setReleases [||]
                setSelectedIndex 0

                promise {
                    try
                        let! loaded = fetchReleases ()

                        if active then
                            setReleases loaded

                            loaded
                            |> Array.tryFindIndex (matchesVersion currentRelease)
                            |> Option.defaultValue 0
                            |> setSelectedIndex

                            setLoading false
                    with ex ->
                        if active then
                            setError (Some ex.Message)
                            setLoading false
                }
                |> Promise.start

                FsReact.createDisposable (fun () -> active <- false)
            ),
            [| box fetchReleases; box currentRelease; box retry |]
        )

        let current = releases |> Array.tryItem selectedIndex

        Html.section [
            prop.className "swt:w-full swt:overflow-auto swt:p-6 swt:flex swt:flex-col swt:grow"
            prop.ariaLabel "Changelog"
            prop.children [
                Html.h1 [
                    prop.className "swt:text-2xl swt:font-bold swt:mb-4"
                    prop.text "Changelog"
                ]
                if loading then
                    Html.p [ prop.role "status"; prop.text "Loading release notes..." ]
                else
                    match error with
                    | Some message ->
                        Html.div [
                            prop.role.alert
                            prop.className "swt:alert swt:alert-error swt:alert-vertical swt:sm:alert-horizontal"
                            prop.children [
                                Html.i [
                                    prop.className "swt:iconify swt:fluent--error-circle-20-regular swt:size-8"
                                ]
                                Html.div [
                                    Html.h3 [ prop.className "swt:font-bold"; prop.text "Error" ]
                                    Html.div [ prop.className "swt:text-xs"; prop.text message ]
                                ]
                                Html.button [
                                    prop.className "swt:btn swt:btn-sm"
                                    prop.text "Retry"
                                    prop.onClick (fun _ -> setRetry (retry + 1))
                                ]
                            ]
                        ]
                    | None ->
                        match current with
                        | None -> Html.p "No published releases found."
                        | Some release ->
                            Html.select [
                                prop.name "release-version-selector"
                                prop.className "swt:select swt:select-bordered swt:mb-6"
                                prop.ariaLabel "Release version"
                                prop.value (string selectedIndex)
                                prop.onChange (fun (value: string) -> setSelectedIndex (int value))
                                prop.children [
                                    for index, release in Array.indexed releases do
                                        Html.option [
                                            prop.key index
                                            prop.value (string index)
                                            prop.text release.tag_name
                                        ]
                                ]
                            ]

                            Html.div [
                                prop.className "swt:grow swt:overflow-auto"
                                prop.children [
                                    ReactMDEditor.MarkdownPreview(
                                        (match release.body with
                                         | Some body when not (System.String.IsNullOrWhiteSpace body) -> body
                                         | _ -> "No release notes provided."),
                                        rehypePlugins = [| ReactMDEditor.rehypeSanitize |],
                                        style = {| padding = 16; borderRadius = 8 |},
                                        className = "swt:prose"
                                    )
                                ]
                            ]

                            Html.nav [
                                prop.className "swt:flex swt:items-center swt:justify-center swt:gap-3 swt:mt-6"
                                prop.ariaLabel "Changelog pagination"
                                prop.children [
                                    Html.button [
                                        prop.type'.button
                                        prop.className "swt:btn swt:btn-sm"
                                        prop.text "Previous"
                                        prop.disabled (selectedIndex <= 0)
                                        prop.onClick (fun _ -> setSelectedIndex (max 0 (selectedIndex - 1)))
                                    ]
                                    Html.span $"{selectedIndex + 1} / {releases.Length}"
                                    Html.button [
                                        prop.type'.button
                                        prop.className "swt:btn swt:btn-sm"
                                        prop.text "Next"
                                        prop.disabled (selectedIndex >= releases.Length - 1)
                                        prop.onClick (fun _ ->
                                            setSelectedIndex (min (releases.Length - 1) (selectedIndex + 1))
                                        )
                                    ]
                                ]
                            ]
            ]
        ]
