namespace Renderer.Components.LeftSidebar

open Fable.Core
open Feliz

type EmptyStateAction = {
    Label: string
    IconClassName: string
    Disabled: bool
    OnClick: unit -> unit
}

[<Erase; Mangle(false)>]
type GitSidebarEmptyState =

    [<ReactComponent>]
    static member Main
        (
            title: string,
            description: string,
            primaryAction: EmptyStateAction,
            ?secondaryAction: EmptyStateAction,
            ?infoText: string,
            ?extraContent: ReactElement
        ) =
        Html.div [
            prop.testId "GitSidebarEmptyState"
            prop.className "swt:flex swt:h-full swt:flex-col swt:justify-center swt:p-4"
            prop.children [
                Html.div [
                    prop.className "swt:rounded-box swt:border swt:border-base-content/10 swt:bg-base-100 swt:p-4"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-base swt:font-semibold"
                            prop.text title
                        ]
                        Html.p [
                            prop.className "swt:mt-2 swt:text-sm swt:text-base-content/70"
                            prop.text description
                        ]
                        match extraContent with
                        | Some content ->
                            Html.div [
                                prop.className "swt:mt-4"
                                prop.children [ content ]
                            ]
                        | None -> Html.none
                        Html.div [
                            prop.className "swt:mt-4 swt:flex swt:flex-col swt:gap-2"
                            prop.children [
                                Html.button [
                                    prop.className "swt:btn swt:btn-primary swt:w-full swt:justify-start swt:gap-2"
                                    prop.disabled primaryAction.Disabled
                                    prop.onClick (fun _ -> primaryAction.OnClick())
                                    prop.children [
                                        Html.span [
                                            prop.className $"swt:iconify {primaryAction.IconClassName} swt:size-4"
                                        ]
                                        Html.span primaryAction.Label
                                    ]
                                ]
                                match secondaryAction with
                                | Some action ->
                                    Html.button [
                                        prop.className "swt:btn swt:btn-outline swt:w-full swt:justify-start swt:gap-2"
                                        prop.disabled action.Disabled
                                        prop.onClick (fun _ -> action.OnClick())
                                        prop.children [
                                            Html.span [
                                                prop.className $"swt:iconify {action.IconClassName} swt:size-4"
                                            ]
                                            Html.span action.Label
                                        ]
                                    ]
                                | None -> Html.none
                            ]
                        ]
                        match infoText with
                        | Some info ->
                            Html.div [
                                prop.className "swt:mt-4 swt:alert swt:alert-warning swt:px-3 swt:py-2 swt:text-sm"
                                prop.children [
                                    Html.span [
                                        prop.className "swt:iconify swt:fluent--warning-shield-24-regular swt:size-4"
                                    ]
                                    Html.span info
                                ]
                            ]
                        | None -> Html.none
                    ]
                ]
            ]
        ]
