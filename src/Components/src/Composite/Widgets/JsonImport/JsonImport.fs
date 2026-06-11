namespace Swate.Components.Composite.Widgets.JsonImport

open System
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open Browser.Types
open Swate.Components
open Swate.Components.Shared

module private JsonImportHelper =

    
    type TemplateFromFileState = {
        /// User select type to upload
        FileType: ArcFilesDiscriminate
        /// User selects json type to upload
        JsonFormat: JsonExportFormat
        Loading: bool
    } with

        static member init() = {
            FileType = ArcFilesDiscriminate.Assay
            JsonFormat = JsonExportFormat.ROCrate
            Loading = false
        }

open JsonImportHelper

[<Erase; Mangle(false)>]
type JsonImport =

    [<ReactComponent(true)>]
    static member JsonImport(arcFile, setArcFile, ?onError: exn -> unit) =
        let state, setState = React.useState (TemplateFromFileState.init)
        let jsonTranspiledArcFile, setJsonTranspiledArcFile = React.useState (None: ArcFiles option)
        let KEY_SEPARATOR = "@"

        let onError = defaultArg onError (fun exn -> Browser.Dom.console.error exn)

        let keysToString (arcfile: ArcFilesDiscriminate, jsontype: JsonExportFormat) =
            sprintf "%A%s%A" arcfile KEY_SEPARATOR jsontype

        let tryKeysFromString (s: string) =
            let parts = s.Split([|KEY_SEPARATOR|], StringSplitOptions.RemoveEmptyEntries)

            if parts.Length = 2 then
                let arcFile = parts.[0] |> ArcFilesDiscriminate.fromString
                let jsontype = parts.[1] |> JsonExportFormat.fromString

                Some(arcFile, jsontype)
            else
                None

        React.Fragment [
            ImportModal.ImportModal(jsonTranspiledArcFile, setArcFile, (fun b -> setJsonTranspiledArcFile None))
            Html.div [
                prop.className "swt:flex swt:flex-col swt:gap-2"
                prop.children [
                    // currently disabled is missing logic
                    Html.div [
                        prop.className "swt:alert swt:alert-error swt:w-full"
                        prop.children [
                            Html.div [
                                Html.span "Currently disabled, not fully implemented yet!"
                            ]
                        ]
                    ]

                    Html.select [
                        prop.className "swt:select swt:w-full"
                        prop.value (sprintf "%A@%A" state.FileType state.JsonFormat)
                        prop.onChange (fun (ev: string) ->
                            match tryKeysFromString ev with
                            | Some(arcfile, jsontype) ->
                                setState {
                                    state with
                                        FileType = arcfile
                                        JsonFormat = jsontype
                                }
                            | None -> ()
                        )
                        prop.children [
                            for (arcfile, jsontype) in Json.Generic.readFromJsonMap.Keys |> Seq.sortBy fst do
                                Html.option [
                                    prop.value (keysToString (arcfile, jsontype))
                                    prop.text (sprintf "%A - %A" arcfile jsontype)
                                ]
                        ]
                    ]

                    Html.div [
                        Html.input [
                            prop.disabled true
                            prop.type'.file
                            prop.className "swt:file-input swt:file-input-error swt:w-full"
                            prop.onChange (fun (ev: File) ->
                                let reader = Browser.Dom.FileReader.Create()

                                reader.onload <-
                                    fun evt ->
                                        let (r: string) = evt.target?result

                                        promise {
                                            setState { state with Loading = true }

                                            let arcFileFn = Json.Generic.readFromJsonMap.[(state.FileType, state.JsonFormat)]

                                            let arcFile = arcFileFn r

                                            setJsonTranspiledArcFile (Some arcFile)
                                        }
                                        |> Promise.catch (fun e ->
                                            onError e
                                            setState { state with Loading = false }
                                        )
                                        |> Promise.map (fun c ->
                                            setState { state with Loading = false }
                                            c
                                        )
                                        |> Promise.start

                                reader.onerror <- fun evt -> onError (exn evt?value)

                                reader.readAsText (ev)
                                ()
                            )
                            prop.onClick (fun e -> e.target?value <- null)
                        ]
                    ]
                ]
            ]
        ]
