module Renderer.Components.MainContent.GitHistoryDiffTarget

open Feliz
open Renderer.Context.GitHistoryContext

let private shortRevision (revision: string) =
    revision.Substring(0, min 7 revision.Length)

[<ReactComponent>]
let Main (revision: string, path: string) =
    let history = useGitHistoryCtx ()

    let diff =
        history.state.Diff
        |> Option.filter (fun value -> value.Request.Revision = revision && value.Request.Path = path)

    let error =
        match diff with
        | None -> Some "This comparison is no longer available. Select the file again in the history sidebar."
        | Some pending -> pending.Error

    Html.div [
        prop.className "swt:flex swt:h-full swt:w-full swt:min-h-0 swt:min-w-0 swt:flex-col"
        prop.children [
            Html.div [
                prop.className "swt:flex swt:items-center swt:gap-3 swt:border-b swt:border-base-300 swt:px-4 swt:py-3"
                prop.children [
                    Html.button [
                        prop.type'.button
                        prop.className "swt:btn swt:btn-ghost swt:btn-sm swt:gap-2"
                        prop.onClick (fun _ -> history.closeDiff ())
                        prop.children [
                            Html.i [
                                prop.className "swt:iconify swt:fluent--dismiss-20-regular swt:size-4"
                            ]
                            Html.span "Close comparison"
                        ]
                    ]
                    Html.span [
                        prop.className "swt:min-w-0 swt:flex-1 swt:truncate swt:text-sm swt:text-base-content/60"
                        prop.title path
                        prop.text $"{path} · {shortRevision revision}"
                    ]
                    if diff |> Option.exists (fun pending -> pending.Error.IsSome) then
                        Html.button [
                            prop.type'.button
                            prop.className "swt:btn swt:btn-sm swt:btn-outline"
                            prop.text "Retry"
                            prop.onClick (fun _ -> history.retryDiff ())
                        ]
                ]
            ]
            Html.div [
                prop.className "swt:min-h-0 swt:min-w-0 swt:flex-1 swt:p-4"
                prop.children [
                    match error with
                    | Some message -> Html.p [ prop.role "alert"; prop.text message ]
                    | None ->
                        Html.div [
                            prop.className "swt:flex swt:items-center swt:gap-2"
                            prop.children [
                                Html.span [
                                    prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                                ]
                                Html.span "Opening comparison…"
                            ]
                        ]
                ]
            ]
        ]
    ]
