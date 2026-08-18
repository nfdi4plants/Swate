namespace Swate.Components.Page.CwlEditor

open Fable.Core
open Feliz
open Swate.Components.Shared.Cwl.Validation.ValidationTypes

module private ValidationPanelHelpers =

    let issueClass (severity: Severity) =
        match severity with
        | Severity.Error -> "swt:text-error"
        | Severity.Warning -> "swt:text-warning"
        | Severity.Info -> "swt:text-info"

[<Erase; Mangle(false)>]
type ValidationPanel =

    [<ReactComponent>]
    static member ValidationPanel(version: int, result: ValidationResult) : ReactElement =
        Html.section [
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.h3 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text "Validation"
                ]
                Html.p [
                    prop.className (
                        if result.IsValid then
                            "swt:text-success"
                        else
                            "swt:text-error"
                    )
                    prop.text (
                        if result.IsValid then
                            "No blocking errors."
                        else
                            "Document contains blocking errors."
                    )
                ]
                if result.Issues.IsEmpty then
                    Html.p [
                        prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                        prop.text "No validation messages."
                    ]
                else
                    Html.ul [
                        prop.className "swt:flex swt:flex-col swt:gap-1"
                        prop.children [
                            for issue in result.Issues do
                                let (RuleId ruleIdText) = issue.RuleId

                                Html.li [
                                    prop.key (sprintf "%s:%s:%s" ruleIdText issue.Path issue.Message)
                                    prop.className (ValidationPanelHelpers.issueClass issue.Severity)
                                    prop.text (sprintf "[%s] %s" ruleIdText issue.Message)
                                ]
                        ]
                    ]
            ]
        ]
