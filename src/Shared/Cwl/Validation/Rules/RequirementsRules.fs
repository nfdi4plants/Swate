/// Rules for CWL requirements and hints.
module Swate.Components.Shared.Cwl.Validation.Rules.RequirementsRules

open ARCtrl.CWL
open Swate.Components.Shared.Cwl.RequirementMutations
open Swate.Components.Shared.Cwl.Validation.ValidationTypes
open Swate.Components.Shared.Cwl.Validation.ValidationContext
open Swate.Components.Shared.Cwl.Validation.ValidationRule

/// Helper: get requirements from any processing unit.
let private getRequirements (pu: CWLProcessingUnit) : Requirement list =
    CWLProcessingUnit.getRequirements pu |> Seq.toList

/// REQ.001 — DockerRequirement should specify a dockerPull image.
let dockerPullRecommended: ValidationRule =
    let r = {
        Id = RuleId "REQ.001"
        Description = "DockerRequirement should specify dockerPull."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    if includeAdvisoryIssues ctx.Mode then
                        getRequirements ctx.ProcessingUnit
                        |> List.mapi (fun i req ->
                            match req with
                            | Requirement.DockerRequirement dr ->
                                match dr.DockerPull with
                                | None
                                | Some "" ->
                                    Some(
                                        issue
                                            r
                                            Warning
                                            (sprintf "requirements[%d]" i)
                                            "DockerRequirement has no dockerPull image specified."
                                    )
                                | _ -> None
                            | _ -> None
                        )
                        |> List.choose id
                    else
                        []
    }

/// REQ.002 — Warn about duplicate requirement types.
let noDuplicateRequirements: ValidationRule =
    let r = {
        Id = RuleId "REQ.002"
        Description = "Requirements should not have duplicate types."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    if includeAdvisoryIssues ctx.Mode then
                        let reqs = getRequirements ctx.ProcessingUnit
                        let reqNames = reqs |> List.map requirementLabel

                        reqNames
                        |> List.groupBy id
                        |> List.choose (fun (name, items) ->
                            if items.Length > 1 then
                                Some(
                                    issue
                                        r
                                        Warning
                                        "requirements"
                                        (sprintf "Duplicate requirement type: %s (appears %d times)." name items.Length)
                                )
                            else
                                None
                        )
                    else
                        []
    }

/// REQ.003 — Expression-based network/work-reuse requirement values should be reviewed.
let expressionBackedNetworkAndReuseReviewed: ValidationRule =
    let r = {
        Id = RuleId "REQ.003"
        Description = "Expression-based NetworkAccess/WorkReuse values should be reviewed."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    if includeAdvisoryIssues ctx.Mode then
                        getRequirements ctx.ProcessingUnit
                        |> List.mapi (fun i req ->
                            match req with
                            | Requirement.WorkReuseExpressionRequirement expression ->
                                Some(
                                    issue
                                        r
                                        Warning
                                        (sprintf "requirements[%d]" i)
                                        (sprintf
                                            "WorkReuse uses expression value '%s'. Verify target runner support."
                                            expression)
                                )
                            | Requirement.NetworkAccessExpressionRequirement expression ->
                                Some(
                                    issue
                                        r
                                        Warning
                                        (sprintf "requirements[%d]" i)
                                        (sprintf
                                            "NetworkAccess uses expression value '%s'. Verify target runner support."
                                            expression)
                                )
                            | _ -> None
                        )
                        |> List.choose id
                    else
                        []
    }

/// REQ.004 — ToolTimeLimit numeric value must be non-negative.
let toolTimeLimitNonNegative: ValidationRule =
    let r = {
        Id = RuleId "REQ.004"
        Description = "ToolTimeLimit seconds must be non-negative."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    getRequirements ctx.ProcessingUnit
                    |> List.mapi (fun i req ->
                        match req with
                        | Requirement.ToolTimeLimitRequirement(ToolTimeLimitSeconds seconds) when seconds < 0L ->
                            Some(
                                issue
                                    r
                                    Error
                                    (sprintf "requirements[%d]" i)
                                    "ToolTimeLimit seconds must be greater than or equal to zero."
                            )
                        | _ -> None
                    )
                    |> List.choose id
    }

let private tryGetResourceNumeric (resource: ResourceRequirementInstance) (fieldName: string) =
    match resource.TryGetInt64(fieldName) with
    | Some intValue -> Some(float intValue)
    | None -> resource.TryGetFloat(fieldName)

/// REQ.005 — ResourceRequirement max values must be greater than or equal to min values.
let resourceMaxNotLessThanMin: ValidationRule =
    let r = {
        Id = RuleId "REQ.005"
        Description = "ResourceRequirement max values must be greater than or equal to min values."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    getRequirements ctx.ProcessingUnit
                    |> List.mapi (fun i req ->
                        match req with
                        | Requirement.ResourceRequirement resource ->
                            [
                                "coresMin", "coresMax"
                                "ramMin", "ramMax"
                                "tmpdirMin", "tmpdirMax"
                                "outdirMin", "outdirMax"
                            ]
                            |> List.choose (fun (minField, maxField) ->
                                match
                                    tryGetResourceNumeric resource minField, tryGetResourceNumeric resource maxField
                                with
                                | Some minValue, Some maxValue when maxValue < minValue ->
                                    Some(
                                        issue
                                            r
                                            Error
                                            (sprintf "requirements[%d].%s" i maxField)
                                            (sprintf "%s must be greater than or equal to %s." maxField minField)
                                    )
                                | _ -> None
                            )
                        | _ -> []
                    )
                    |> List.collect id
    }

/// REQ.006 — EnvVarRequirement envName values should be unique within each requirement.
let envVarNamesUnique: ValidationRule =
    let r = {
        Id = RuleId "REQ.006"
        Description = "EnvVarRequirement envName values should be unique."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    getRequirements ctx.ProcessingUnit
                    |> List.mapi (fun i req ->
                        match req with
                        | Requirement.EnvVarRequirement envDefs ->
                            envDefs
                            |> Seq.map (fun env -> env.EnvName)
                            |> Seq.groupBy id
                            |> Seq.choose (fun (name, matches) ->
                                let count = matches |> Seq.length

                                if count > 1 && System.String.IsNullOrWhiteSpace name |> not then
                                    Some(
                                        issue
                                            r
                                            Error
                                            (sprintf "requirements[%d].envDef" i)
                                            (sprintf "Duplicate envName '%s' appears %d times." name count)
                                    )
                                else
                                    None
                            )
                            |> Seq.toList
                        | _ -> []
                    )
                    |> List.collect id
    }

/// REQ.007 — InplaceUpdate=true with WorkReuse enabled should be reviewed.
let inplaceUpdateWithWorkReuseWarning: ValidationRule =
    let r = {
        Id = RuleId "REQ.007"
        Description = "InplaceUpdate=true with WorkReuse enabled should be reviewed."
        Run = fun _ -> []
    }

    {
        r with
            Run =
                fun ctx ->
                    if includeAdvisoryIssues ctx.Mode then
                        let requirements = getRequirements ctx.ProcessingUnit

                        let hasEnabledInplaceUpdate =
                            requirements
                            |> List.exists (
                                function
                                | Requirement.InplaceUpdateRequirement value -> value.InplaceUpdate
                                | _ -> false
                            )

                        let hasEnabledWorkReuse =
                            requirements
                            |> List.exists (
                                function
                                | Requirement.WorkReuseRequirement value -> value.EnableReuse
                                | Requirement.WorkReuseExpressionRequirement _ -> true
                                | _ -> false
                            )

                        if hasEnabledInplaceUpdate && hasEnabledWorkReuse then
                            [
                                issue
                                    r
                                    Warning
                                    "requirements"
                                    "InplaceUpdate=true with WorkReuse enabled can cause reused outputs to be stale for in-place mutations."
                            ]
                        else
                            []
                    else
                        []
    }

/// All Requirements rules.
let all = [
    dockerPullRecommended
    noDuplicateRequirements
    expressionBackedNetworkAndReuseReviewed
    toolTimeLimitNonNegative
    resourceMaxNotLessThanMin
    envVarNamesUnique
    inplaceUpdateWithWorkReuseWarning
]
