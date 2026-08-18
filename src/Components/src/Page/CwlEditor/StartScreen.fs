namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz
open Swate.Components.Shared.Cwl.EditorTypes

[<Erase; Mangle(false)>]
type StartScreen =

    [<ReactComponent>]
    static member StartScreen
        (
            version: int,
            errorMessage: string option,
            isLoading: bool,
            onDismissError: unit -> unit,
            onCreateNew: ProcessingUnitKind -> unit,
            onLoadExisting: unit -> unit
        ) : ReactElement =
        let host =
            Context.useCwlEditorHostCtx ()
            |> Option.defaultWith (fun () -> failwith "StartScreen requires a CwlEditorHost context.")

        Html.div [
            prop.key (sprintf "cwl-start-screen-%d" version)
            prop.className
                "swt:flex swt:flex-col swt:items-center swt:justify-center swt:gap-4 swt:h-full swt:bg-base-100"
            prop.children [
                Html.h1 [
                    prop.className "swt:text-2xl swt:font-semibold"
                    prop.text "CWL Builder"
                ]
                Html.p [
                    prop.className "swt:text-base-content"
                    prop.text "Create or load a Common Workflow Language document."
                ]
                match errorMessage with
                | Some message ->
                    Html.div [
                        prop.className "swt:alert swt:alert-error"
                        prop.children [
                            Html.span [ prop.text message ]
                            Html.button [
                                prop.testId "cwl-dismiss-error"
                                prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                prop.text "Dismiss"
                                prop.onClick (fun _ -> onDismissError ())
                            ]
                        ]
                    ]
                | None -> Html.none
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:justify-center swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-new-command-line-tool"
                            prop.className "swt:btn swt:btn-sm swt:btn-primary"
                            prop.text "New CommandLineTool"
                            prop.onClick (fun _ -> onCreateNew CommandLineTool)
                        ]
                        Html.button [
                            prop.testId "cwl-new-workflow"
                            prop.className "swt:btn swt:btn-sm swt:btn-primary"
                            prop.text "New Workflow"
                            prop.onClick (fun _ -> onCreateNew Workflow)
                        ]
                        Html.button [
                            prop.testId "cwl-new-expression-tool"
                            prop.className "swt:btn swt:btn-sm swt:btn-primary"
                            prop.text "New ExpressionTool"
                            prop.onClick (fun _ -> onCreateNew ExpressionTool)
                        ]
                        Html.button [
                            prop.testId "cwl-new-operation"
                            prop.className "swt:btn swt:btn-sm swt:btn-primary"
                            prop.text "New Operation"
                            prop.onClick (fun _ -> onCreateNew Operation)
                        ]
                        match host.pickOpenFile with
                        | Some _ ->
                            Html.button [
                                prop.testId "cwl-load-existing"
                                prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                prop.text (if isLoading then "Loading..." else "Load existing .cwl")
                                prop.disabled isLoading
                                prop.onClick (fun _ -> onLoadExisting ())
                            ]
                        | None -> Html.none
                    ]
                ]
            ]
        ]
