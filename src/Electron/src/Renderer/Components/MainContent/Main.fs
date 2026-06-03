module Renderer.Components.MainContent.Main

open Feliz
open Renderer.Types
open Swate.Electron.Shared

module private MainHelper =

    open Fable.Core

    let loadTemplates =
        fun () ->
            promise {
                let! json =
                    Swate.Components.Api.SwateApi.SwateTemplateApi.getTemplates ()
                    |> Async.StartAsPromise

                return Ok(ARCtrl.Json.Templates.fromJsonString json)
            }
            |> Promise.catch (fun error ->
                // Handle error, e.g., log it or show a notification
                Error(sprintf "Error loading templates: %s" error.Message)
            )

/// This can be further reduced by using the actual contexts instead of passing down the states and setters as props, but this is good enough for now
[<ReactMemoComponent>]
let Main (appRootPath: ArcRootPath) (pageState: PageState option) =
    Swate.Components.Composite.Template.TemplateCacheProvider.TemplateCacheProvider(
        loadTemplates = MainHelper.loadTemplates,
        children =
            Html.div [
                prop.className "swt:size-full swt:min-w-0 swt:min-h-0 swt:flex swt:justify-center swt:overflow-hidden"
                prop.children [
                    Renderer.Components.MainContent.PageSelector.Main appRootPath pageState
                ]
            ]
    )
