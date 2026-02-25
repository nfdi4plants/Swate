namespace Swate.Components.MarkdownText.Plugins

[<RequireQualifiedAccess>]
module PluginRegistry =

    let defaultPlugins: MarkdownToolbarPlugin list = [ AddStep.plugin ]

    let mergeWithDefaults (customPlugins: MarkdownToolbarPlugin list option) =
        match customPlugins with
        | None -> defaultPlugins
        | Some custom ->
            let customIds = custom |> List.map (fun plugin -> plugin.Id) |> Set.ofList
            let nonOverriddenDefaults = defaultPlugins |> List.filter (fun plugin -> customIds.Contains plugin.Id |> not)
            nonOverriddenDefaults @ custom

    let activeCommands (customPlugins: MarkdownToolbarPlugin list option) =
        mergeWithDefaults customPlugins
        |> List.filter (fun plugin -> plugin.Enabled)
        |> List.map (fun plugin -> plugin.Command)
        |> List.toArray
