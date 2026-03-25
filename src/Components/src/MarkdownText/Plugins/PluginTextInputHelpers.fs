namespace Swate.Components.MarkdownText.Plugins

open System
open Browser.Types
open Fable.Core
open Fable.Core.JsInterop
open Swate.Components.Shared

open Swate.Components.MarkdownText.JsBindings

[<RequireQualifiedAccess>]
module PluginTextInputHelpers =

    type PromptViewModel = {
        Title: string
        Description: string option
        Placeholder: string
        SubmitButtonText: string
        InputMode: MarkdownPromptInputMode
        AcceptTypes: string option
        AllowMultipleFiles: bool
    }

    let activePromptInputMode (activePrompt: MarkdownPromptPlugin option) =
        activePrompt
        |> Option.bind (fun prompt -> prompt.InputMode)
        |> Option.defaultValue MarkdownPromptInputMode.Text

    let activePromptAllowsMultipleFiles (activePrompt: MarkdownPromptPlugin option) =
        activePrompt
        |> Option.bind (fun prompt -> prompt.AllowMultiple)
        |> Option.defaultValue false

    let activePromptAcceptTypes (activePrompt: MarkdownPromptPlugin option) =
        activePrompt
        |> Option.bind (fun prompt -> prompt.Accept)
        |> Option.filter (fun accept -> not (String.IsNullOrWhiteSpace accept))

    let normalizePromptFiles (activePrompt: MarkdownPromptPlugin option) (files: MarkdownPromptFile list) =
        if activePromptAllowsMultipleFiles activePrompt then
            files
        else
            // In single-file mode, always keep the most recently selected file.
            match List.tryLast files with
            | Some lastFile -> [ lastFile ]
            | None -> []

    let private acceptedTypeTokens (activePrompt: MarkdownPromptPlugin option) =
        activePromptAcceptTypes activePrompt
        |> Option.defaultValue ""
        |> fun accept ->
            accept.Split(',')
            |> Array.toList
            |> List.map (fun token -> token.Trim().ToLowerInvariant())
            |> List.filter (fun token -> not (String.IsNullOrWhiteSpace token))

    let private fileMatchesAcceptToken (file: MarkdownPromptFile) (token: string) =
        let fileNameLower =
            if String.IsNullOrWhiteSpace file.Name then
                ""
            else
                file.Name.ToLowerInvariant()

        let mimeLower =
            file.MimeType
            |> Option.defaultValue ""
            |> fun mime -> mime.ToLowerInvariant()

        if token.StartsWith "." then
            not (String.IsNullOrWhiteSpace fileNameLower) && fileNameLower.EndsWith token
        elif token.EndsWith "/*" then
            let mimePrefix = token.Substring(0, token.Length - 1)
            not (String.IsNullOrWhiteSpace mimeLower) && mimeLower.StartsWith mimePrefix
        else
            not (String.IsNullOrWhiteSpace mimeLower) && mimeLower = token

    let partitionFilesByAccept (activePrompt: MarkdownPromptPlugin option) (files: MarkdownPromptFile list) =
        let tokens = acceptedTypeTokens activePrompt

        if List.isEmpty tokens then
            files, []
        else
            files
            |> List.partition (fun file -> tokens |> List.exists (fileMatchesAcceptToken file))

    let rejectedFilesMessage (activePrompt: MarkdownPromptPlugin option) (files: MarkdownPromptFile list) =
        let rejectedNames =
            files
            |> List.map (fun file ->
                if String.IsNullOrWhiteSpace file.Name then
                    "(unnamed file)"
                else
                    file.Name
            )
            |> String.concat ", "

        let allowed =
            activePromptAcceptTypes activePrompt
            |> Option.defaultValue "configured accepted types"

        if List.length files = 1 then
            $"File not allowed: {rejectedNames}. Allowed: {allowed}."
        else
            $"Files not allowed: {rejectedNames}. Allowed: {allowed}."

    let normalizePath = PathHelpers.normalizeSeparators

    let toPromptFile (file: File) : MarkdownPromptFile =
        {
            Name = file.name
            MimeType =
                if String.IsNullOrWhiteSpace file.``type`` then
                    None
                else
                    Some file.``type``
            // Browser fallback cannot reliably resolve host filesystem paths.
            HostPath = None
            BrowserFile = Some file
        }

    let resolvePromptFilePath (filePickerAdapter: MarkdownFilePickerAdapter option) (file: MarkdownPromptFile) =
        promise {
            // Fallback path strategy when no host resolver is provided.
            let fallbackPath =
                match file.HostPath with
                | Some hostPath when not (String.IsNullOrWhiteSpace hostPath) -> normalizePath hostPath
                | _ -> file.Name

            match filePickerAdapter with
            | Some adapter ->
                // Preferred substitution point for runtime-specific link/path mapping.
                let! resolvedPath = adapter.ResolveMarkdownPath file

                if String.IsNullOrWhiteSpace resolvedPath then
                    return fallbackPath
                else
                    return normalizePath resolvedPath
            | None -> return fallbackPath
        }

    let promptViewModel (activePrompt: MarkdownPromptPlugin option) : PromptViewModel =
        {
            Title =
                activePrompt
                |> Option.map (fun prompt -> prompt.Title)
                |> Option.defaultValue "Plugin action"
            Description = activePrompt |> Option.bind (fun prompt -> prompt.Description)
            Placeholder =
                activePrompt
                |> Option.map (fun prompt -> prompt.Placeholder)
                |> Option.defaultValue ""
            SubmitButtonText =
                activePrompt
                |> Option.map (fun prompt -> prompt.SubmitButtonText)
                |> Option.defaultValue "Apply"
            InputMode = activePromptInputMode activePrompt
            AcceptTypes = activePromptAcceptTypes activePrompt
            AllowMultipleFiles = activePromptAllowsMultipleFiles activePrompt
        }

    let tryFindPluginForCommand (activePlugins: MarkdownToolbarPlugin list) (command: ICommand) =
        activePlugins
        |> List.tryFind (fun plugin -> plugin.Command.keyCommand = command.keyCommand)
