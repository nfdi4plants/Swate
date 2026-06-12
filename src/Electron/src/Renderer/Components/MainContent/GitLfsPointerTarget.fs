module Renderer.Components.MainContent.GitLfsPointerTarget

open Fable.Core
open Feliz
open Renderer.Components.Helper.ArcViewHelper
open Renderer.Components.Helper.GitLfsHelper
open Renderer.Types
open Swate.Components.Shared
open Swate.Components.Primitive.ErrorModal.Context
open Swate.Components.Primitive.ErrorModal.Types

[<ReactComponent>]
let Main (pointerPage: GitLfsPointerPageData) =
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()
    let errorModal = useErrorModalCtx ()
    let isDownloading, setIsDownloading = React.useState false
    let localError, setLocalError = React.useState<string option> None
    let fileName = PathHelpers.getNameFromPath pointerPage.Path

    let downloadAndOpen () =
        if not isDownloading then
            setIsDownloading true
            setLocalError None

            promise {
                try
                    match! runDownloadLfsFile pointerPage.Path with
                    | Error errorMessage -> setLocalError (Some errorMessage)
                    | Ok() ->
                        match! openView pointerPage.Path with
                        | Ok pageState -> pageStateCtx.setState (Some pageState)
                        | Error errorMessage ->
                            errorModal.enqueue (
                                ErrorModalRequest.create (
                                    $"Downloaded '{fileName}', but could not open it: {errorMessage}",
                                    title = "Preview failed"
                                )
                            )
                with exn ->
                    setLocalError (Some exn.Message)

                setIsDownloading false
            }
            |> Promise.start

    Html.div [
        prop.className "swt:size-full swt:flex swt:items-center swt:justify-center swt:p-6"
        prop.children [
            Html.div [
                prop.className
                    "swt:flex swt:max-w-xl swt:flex-col swt:gap-4 swt:rounded-lg swt:border swt:border-base-300 swt:bg-base-100 swt:p-6 swt:shadow-sm"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:items-start swt:gap-3"
                        prop.children [
                            Html.i [
                                prop.className
                                    "swt:iconify swt:fluent--cloud-arrow-down-24-regular swt:mt-1 swt:size-6 swt:text-info"
                            ]
                            Html.div [
                                prop.className "swt:flex swt:flex-col swt:gap-1"
                                prop.children [
                                    Html.h1 [
                                        prop.className "swt:text-lg swt:font-semibold"
                                        prop.text "Git LFS pointer"
                                    ]
                                    Html.p [
                                        prop.className "swt:text-sm swt:text-base-content/70"
                                        prop.text
                                            $"'{fileName}' is tracked by Git LFS and only the small pointer file is available locally."
                                    ]
                                    match pointerPage.SizeFormatted with
                                    | Some size ->
                                        Html.p [
                                            prop.className "swt:text-xs swt:text-base-content/60"
                                            prop.text $"Full file size: {size}"
                                        ]
                                    | None -> Html.none
                                ]
                            ]
                        ]
                    ]

                    match localError with
                    | Some errorMessage ->
                        Html.div [
                            prop.className "swt:alert swt:alert-error swt:text-sm"
                            prop.text errorMessage
                        ]
                    | None -> Html.none

                    Html.div [
                        prop.className "swt:flex swt:justify-end"
                        prop.children [
                            Html.button [
                                prop.type'.button
                                prop.className "swt:btn swt:btn-primary swt:btn-sm"
                                prop.disabled isDownloading
                                prop.onClick (fun _ -> downloadAndOpen ())
                                prop.children [
                                    if isDownloading then
                                        Html.span [
                                            prop.className "swt:loading swt:loading-spinner swt:loading-xs"
                                        ]
                                    else
                                        Html.i [
                                            prop.className
                                                "swt:iconify swt:fluent--cloud-arrow-down-24-regular swt:size-4"
                                        ]

                                    Html.span [
                                        prop.text (if isDownloading then "Downloading" else "Download")
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
