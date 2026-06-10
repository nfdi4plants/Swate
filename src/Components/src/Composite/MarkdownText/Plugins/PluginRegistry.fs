namespace Swate.Components.Composite.MarkdownText.Plugins

[<RequireQualifiedAccess>]
module PluginRegistry =

    let defaultPlugins: MarkdownToolbarPlugin list = [
        AddStep.plugin
        AddImage.plugin
        AddOntologyReference.plugin
    ]

    let mergeWithDefaults (customPlugins: MarkdownToolbarPlugin list option) =
        match customPlugins with
        | None -> defaultPlugins
        | Some custom ->
            let customIds = custom |> List.map (fun plugin -> plugin.Id) |> Set.ofList

            let nonOverriddenDefaults =
                defaultPlugins
                |> List.filter (fun plugin -> customIds.Contains plugin.Id |> not)

            nonOverriddenDefaults @ custom

    let activePlugins (customPlugins: MarkdownToolbarPlugin list option) =
        mergeWithDefaults customPlugins |> List.filter (fun plugin -> plugin.Enabled)

    let activeCommands (customPlugins: MarkdownToolbarPlugin list option) =
        activePlugins customPlugins
        |> List.map (fun plugin -> plugin.Command)
        |> List.toArray
