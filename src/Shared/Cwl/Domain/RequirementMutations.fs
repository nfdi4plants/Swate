/// Shared requirement/hint mutation helpers across processing-unit editors.
module Swate.Components.Shared.Cwl.RequirementMutations

open System
open System.Globalization
open ARCtrl.CWL
open Swate.Components.Shared.Cwl.EditorMutations

type RequirementTemplate = {
    Key: string
    Label: string
    Create: unit -> Requirement
}

let requirementTemplates: RequirementTemplate list = [
    {
        Key = "inline-javascript"
        Label = "InlineJavascriptRequirement"
        Create = fun () -> Requirement.defaultInlineJavascriptRequirement
    }
    {
        Key = "schema-def"
        Label = "SchemaDefRequirement"
        Create = fun () -> Requirement.SchemaDefRequirement(ResizeArray())
    }
    {
        Key = "docker"
        Label = "DockerRequirement"
        Create = fun () -> Requirement.DockerRequirement(DockerRequirement.create ())
    }
    {
        Key = "network-access"
        Label = "NetworkAccessRequirement"
        Create = fun () -> Requirement.NetworkAccessRequirement(NetworkAccessRequirementValue(true))
    }
    {
        Key = "initial-workdir"
        Label = "InitialWorkDirRequirement"
        Create = fun () -> Requirement.InitialWorkDirRequirement(ResizeArray())
    }
    {
        Key = "resource"
        Label = "ResourceRequirement"
        Create = fun () -> Requirement.ResourceRequirement(ResourceRequirementInstance())
    }
    {
        Key = "env-vars"
        Label = "EnvVarRequirement"
        Create = fun () -> Requirement.EnvVarRequirement(ResizeArray())
    }
    {
        Key = "load-listing"
        Label = "LoadListingRequirement"
        Create = fun () -> Requirement.LoadListingRequirement(LoadListingRequirementValue(LoadListingEnum.NoListing))
    }
    {
        Key = "shell-command"
        Label = "ShellCommandRequirement"
        Create = fun () -> Requirement.ShellCommandRequirement
    }
    {
        Key = "software"
        Label = "SoftwareRequirement"
        Create = fun () -> Requirement.SoftwareRequirement(ResizeArray())
    }
    {
        Key = "work-reuse"
        Label = "WorkReuseRequirement"
        Create = fun () -> Requirement.WorkReuseRequirement(WorkReuseRequirementValue(true))
    }
    {
        Key = "inplace-update"
        Label = "InplaceUpdateRequirement"
        Create = fun () -> Requirement.InplaceUpdateRequirement(InplaceUpdateRequirementValue(true))
    }
    {
        Key = "tool-time-limit"
        Label = "ToolTimeLimitRequirement"
        Create = fun () -> Requirement.ToolTimeLimitRequirement(ToolTimeLimitSeconds 0L)
    }
    {
        Key = "subworkflow-feature"
        Label = "SubworkflowFeatureRequirement"
        Create = fun () -> Requirement.SubworkflowFeatureRequirement
    }
    {
        Key = "scatter-feature"
        Label = "ScatterFeatureRequirement"
        Create = fun () -> Requirement.ScatterFeatureRequirement
    }
    {
        Key = "multiple-input-feature"
        Label = "MultipleInputFeatureRequirement"
        Create = fun () -> Requirement.MultipleInputFeatureRequirement
    }
    {
        Key = "step-input-expression"
        Label = "StepInputExpressionRequirement"
        Create = fun () -> Requirement.StepInputExpressionRequirement
    }
]

let private requirementTemplateByKey =
    requirementTemplates |> List.map (fun item -> item.Key, item) |> Map.ofList

let requirementKey (req: Requirement) =
    match req with
    | Requirement.InlineJavascriptRequirement _ -> Some "inline-javascript"
    | Requirement.SchemaDefRequirement _ -> Some "schema-def"
    | Requirement.DockerRequirement _ -> Some "docker"
    | Requirement.NetworkAccessRequirement _ -> Some "network-access"
    | Requirement.InitialWorkDirRequirement _ -> Some "initial-workdir"
    | Requirement.ResourceRequirement _ -> Some "resource"
    | Requirement.EnvVarRequirement _ -> Some "env-vars"
    | Requirement.LoadListingRequirement _ -> Some "load-listing"
    | Requirement.ShellCommandRequirement -> Some "shell-command"
    | Requirement.SoftwareRequirement _ -> Some "software"
    | Requirement.WorkReuseRequirement _ -> Some "work-reuse"
    // Expression and boolean variants intentionally share keys so toggle/remove
    // semantics apply consistently to the same logical requirement class.
    | Requirement.WorkReuseExpressionRequirement _ -> Some "work-reuse"
    | Requirement.NetworkAccessExpressionRequirement _ -> Some "network-access"
    | Requirement.InplaceUpdateRequirement _ -> Some "inplace-update"
    | Requirement.ToolTimeLimitRequirement _ -> Some "tool-time-limit"
    | Requirement.SubworkflowFeatureRequirement -> Some "subworkflow-feature"
    | Requirement.ScatterFeatureRequirement -> Some "scatter-feature"
    | Requirement.MultipleInputFeatureRequirement -> Some "multiple-input-feature"
    | Requirement.StepInputExpressionRequirement -> Some "step-input-expression"

let requirementLabel (req: Requirement) =
    match req with
    | Requirement.DockerRequirement _ -> "DockerRequirement"
    | Requirement.InlineJavascriptRequirement _ -> "InlineJavascriptRequirement"
    | Requirement.SoftwareRequirement _ -> "SoftwareRequirement"
    | Requirement.InitialWorkDirRequirement _ -> "InitialWorkDirRequirement"
    | Requirement.EnvVarRequirement _ -> "EnvVarRequirement"
    | Requirement.ResourceRequirement _ -> "ResourceRequirement"
    | Requirement.LoadListingRequirement _ -> "LoadListingRequirement"
    | Requirement.ShellCommandRequirement -> "ShellCommandRequirement"
    | Requirement.NetworkAccessRequirement _ -> "NetworkAccessRequirement"
    | Requirement.ToolTimeLimitRequirement _ -> "ToolTimeLimitRequirement"
    | Requirement.SchemaDefRequirement _ -> "SchemaDefRequirement"
    | Requirement.SubworkflowFeatureRequirement -> "SubworkflowFeatureRequirement"
    | Requirement.ScatterFeatureRequirement -> "ScatterFeatureRequirement"
    | Requirement.MultipleInputFeatureRequirement -> "MultipleInputFeatureRequirement"
    | Requirement.StepInputExpressionRequirement -> "StepInputExpressionRequirement"
    | Requirement.WorkReuseRequirement _ -> "WorkReuseRequirement"
    | Requirement.WorkReuseExpressionRequirement _ -> "WorkReuseRequirement"
    | Requirement.NetworkAccessExpressionRequirement _ -> "NetworkAccessRequirement"
    | Requirement.InplaceUpdateRequirement _ -> "InplaceUpdateRequirement"

let private parseInt64OrNone (value: string) =
    match Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture) with
    | true, parsed -> Some parsed
    | _ -> None

let private parseFloatOrNone (value: string) =
    match Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture) with
    | true, parsed -> Some parsed
    | _ -> None

let private parseDelimitedStringsOrNone (value: string) =
    value.Split([| ','; '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun token -> token.Trim())
    |> Array.filter (String.IsNullOrWhiteSpace >> not)
    |> ResizeArray
    |> fun values -> if values.Count = 0 then None else Some values

let private parseBoolOrNone (value: string) =
    match value.Trim().ToLowerInvariant() with
    | "true"
    | "yes"
    | "1"
    | "on" -> Some true
    | "false"
    | "no"
    | "0"
    | "off" -> Some false
    | _ -> None

let private parseResourceScalar (value: string) =
    let trimmed = value.Trim()

    if String.IsNullOrWhiteSpace trimmed then
        None
    else
        match parseInt64OrNone trimmed with
        | Some intValue -> Some(box intValue)
        | None ->
            match parseFloatOrNone trimmed with
            | Some floatValue -> Some(box floatValue)
            | None -> Some(box trimmed)

let private schemaSaladText (value: SchemaSaladString) =
    match value with
    | SchemaSaladString.Literal text
    | SchemaSaladString.Include text
    | SchemaSaladString.Import text -> text

let private schemaSaladMode (value: SchemaSaladString) =
    match value with
    | SchemaSaladString.Literal _ -> "literal"
    | SchemaSaladString.Include _ -> "include"
    | SchemaSaladString.Import _ -> "import"

let private schemaSaladFromMode (mode: string) (text: string) =
    match mode.Trim().ToLowerInvariant() with
    | "include" -> SchemaSaladString.Include text
    | "import" -> SchemaSaladString.Import text
    | _ -> SchemaSaladString.Literal text

let private schemaSaladWithMode (mode: string) (currentValue: SchemaSaladString) =
    schemaSaladFromMode mode (schemaSaladText currentValue)

let private schemaSaladWithText (text: string) (currentValue: SchemaSaladString) =
    schemaSaladFromMode (schemaSaladMode currentValue) text

let private cwlTypeFromSchemaTypeKey (value: string) =
    match value.Trim().ToLowerInvariant() with
    | "string" -> Some CWLType.String
    | "int" -> Some CWLType.Int
    | "long" -> Some CWLType.Long
    | "float" -> Some CWLType.Float
    | "double" -> Some CWLType.Double
    | "boolean" -> Some CWLType.Boolean
    | "stdout" -> Some CWLType.Stdout
    | "null" -> Some CWLType.Null
    | "file" -> Some(CWLType.file ())
    | "directory" -> Some(CWLType.directory ())
    | _ -> None

let private tryParseIndexedFieldKey (prefix: string) (fieldKey: string) =
    if fieldKey.StartsWith(prefix, StringComparison.Ordinal) then
        let indexText = fieldKey.Substring(prefix.Length)

        match Int32.TryParse indexText with
        | true, index -> Some index
        | _ -> None
    else
        None

let toggleRequirement (key: string) (enabled: bool) (items: ResizeArray<Requirement> option) =
    let filtered =
        items
        |> Option.defaultValue (ResizeArray())
        |> Seq.filter (fun req -> requirementKey req <> Some key)
        |> ResizeArray

    if enabled then
        match requirementTemplateByKey |> Map.tryFind key with
        | Some template -> filtered.Add(template.Create())
        | None -> ()

    if filtered.Count = 0 then None else Some filtered

let toggleHint (key: string) (enabled: bool) (items: ResizeArray<HintEntry> option) =
    let filtered =
        items
        |> Option.defaultValue (ResizeArray())
        |> Seq.filter (fun hint ->
            match hint with
            | KnownHint req -> requirementKey req <> Some key
            | UnknownHint _ -> true
        )
        |> ResizeArray

    if enabled then
        match requirementTemplateByKey |> Map.tryFind key with
        | Some template -> filtered.Add(KnownHint(template.Create()))
        | None -> ()

    if filtered.Count = 0 then None else Some filtered

let private setDockerRequirementField (docker: DockerRequirement) (fieldKey: string) (value: string) =
    let nextValue = nonEmptyOrNone value

    match fieldKey with
    | "dockerPull" ->
        docker.DockerPull <- nextValue
        docker
    | "dockerImageId" ->
        docker.DockerImageId <- nextValue
        docker
    | "dockerLoad" ->
        docker.DockerLoad <- nextValue
        docker
    | "dockerImport" ->
        docker.DockerImport <- nextValue
        docker
    | "dockerFile" ->
        let currentMode =
            docker.DockerFile |> Option.map schemaSaladMode |> Option.defaultValue "literal"

        docker.DockerFile <- nextValue |> Option.map (schemaSaladFromMode currentMode)
        docker
    | "dockerFileMode" ->
        docker.DockerFile <- docker.DockerFile |> Option.map (schemaSaladWithMode value)
        docker
    | "dockerOutputDirectory" ->
        docker.DockerOutputDirectory <- nextValue
        docker
    | "dockerRunOptions" ->
        docker.DockerRunOptions <- parseDelimitedStringsOrNone value
        docker
    | _ -> docker

let private setRequirementFieldValue (requirement: Requirement) (fieldKey: string) (value: string) =
    match requirement with
    | Requirement.InlineJavascriptRequirement inlineJavascript ->
        match fieldKey with
        | "expressionLib" ->
            inlineJavascript.ExpressionLib <- parseDelimitedStringsOrNone value
            requirement
        | _ -> requirement

    | Requirement.SchemaDefRequirement schemaTypes ->
        match fieldKey with
        | "schema.add" ->
            let nextName =
                nextName "type" (schemaTypes |> Seq.map (fun schemaType -> schemaType.Name))

            schemaTypes.Add(SchemaDefRequirementType(nextName, CWLType.String))

            Requirement.SchemaDefRequirement schemaTypes
        | _ ->
            match tryParseIndexedFieldKey "schema.remove:" fieldKey with
            | Some index when index >= 0 && index < schemaTypes.Count ->
                schemaTypes.RemoveAt(index)
                Requirement.SchemaDefRequirement schemaTypes
            | _ ->
                match tryParseIndexedFieldKey "schema.name:" fieldKey with
                | Some index when index >= 0 && index < schemaTypes.Count ->
                    let trimmed = value.Trim()
                    let current = schemaTypes.[index]

                    let nextName =
                        if String.IsNullOrWhiteSpace trimmed then
                            current.Name
                        else
                            trimmed

                    current.Name <- nextName
                    Requirement.SchemaDefRequirement schemaTypes
                | _ ->
                    match tryParseIndexedFieldKey "schema.type:" fieldKey with
                    | Some index when index >= 0 && index < schemaTypes.Count ->
                        match cwlTypeFromSchemaTypeKey value with
                        | Some cwlType ->
                            let current = schemaTypes.[index]
                            current.Type_ <- cwlType
                            Requirement.SchemaDefRequirement schemaTypes
                        | None -> requirement
                    | _ -> requirement

    | Requirement.DockerRequirement docker ->
        Requirement.DockerRequirement(setDockerRequirementField docker fieldKey value)

    | Requirement.SoftwareRequirement packages ->
        match fieldKey with
        | "software.add" ->
            packages.Add(SoftwarePackage(""))

            Requirement.SoftwareRequirement packages
        | _ ->
            match tryParseIndexedFieldKey "software.remove:" fieldKey with
            | Some index when index >= 0 && index < packages.Count ->
                packages.RemoveAt(index)
                Requirement.SoftwareRequirement packages
            | _ ->
                match tryParseIndexedFieldKey "software.package:" fieldKey with
                | Some index when index >= 0 && index < packages.Count ->
                    let current = packages.[index]
                    current.Package <- value.Trim()
                    Requirement.SoftwareRequirement packages
                | _ ->
                    match tryParseIndexedFieldKey "software.version:" fieldKey with
                    | Some index when index >= 0 && index < packages.Count ->
                        let current = packages.[index]

                        current.Version <- parseDelimitedStringsOrNone value

                        Requirement.SoftwareRequirement packages
                    | _ ->
                        match tryParseIndexedFieldKey "software.specs:" fieldKey with
                        | Some index when index >= 0 && index < packages.Count ->
                            let current = packages.[index]

                            current.Specs <- parseDelimitedStringsOrNone value

                            Requirement.SoftwareRequirement packages
                        | _ -> requirement

    | Requirement.LoadListingRequirement loadListing ->
        match fieldKey with
        | "loadListing" ->
            match LoadListingEnum.tryParse value with
            | Some parsed ->
                loadListing.LoadListing <- parsed
                Requirement.LoadListingRequirement loadListing
            | None -> requirement
        | _ -> requirement

    | Requirement.EnvVarRequirement envDefs ->
        match fieldKey with
        | "env.add" ->
            envDefs.Add(EnvironmentDef("", ""))
            Requirement.EnvVarRequirement envDefs
        | _ ->
            match tryParseIndexedFieldKey "env.remove:" fieldKey with
            | Some index when index >= 0 && index < envDefs.Count ->
                envDefs.RemoveAt(index)
                Requirement.EnvVarRequirement envDefs
            | _ ->
                match tryParseIndexedFieldKey "env.name:" fieldKey with
                | Some index when index >= 0 && index < envDefs.Count ->
                    let current = envDefs.[index]
                    current.EnvName <- value.Trim()
                    Requirement.EnvVarRequirement envDefs
                | _ ->
                    match tryParseIndexedFieldKey "env.value:" fieldKey with
                    | Some index when index >= 0 && index < envDefs.Count ->
                        let current = envDefs.[index]
                        current.EnvValue <- value
                        Requirement.EnvVarRequirement envDefs
                    | _ -> requirement

    | Requirement.ToolTimeLimitRequirement timeLimit ->
        match fieldKey with
        | "timelimitMode" ->
            let nextTimeLimit =
                match value.Trim().ToLowerInvariant(), timeLimit with
                | "seconds", ToolTimeLimitSeconds seconds -> ToolTimeLimitSeconds seconds
                | "seconds", ToolTimeLimitExpression expression ->
                    match parseInt64OrNone expression with
                    | Some seconds -> ToolTimeLimitSeconds seconds
                    | None -> ToolTimeLimitSeconds 0L
                | "expression", ToolTimeLimitExpression expression -> ToolTimeLimitExpression expression
                | "expression", ToolTimeLimitSeconds seconds -> ToolTimeLimitExpression(string seconds)
                | _ -> timeLimit

            Requirement.ToolTimeLimitRequirement nextTimeLimit
        | "timelimitValue" ->
            let nextTimeLimit =
                match timeLimit with
                | ToolTimeLimitSeconds seconds ->
                    match parseInt64OrNone value with
                    | Some parsed -> ToolTimeLimitSeconds parsed
                    | None when String.IsNullOrWhiteSpace value -> ToolTimeLimitSeconds 0L
                    | None -> ToolTimeLimitSeconds seconds
                | ToolTimeLimitExpression _ -> ToolTimeLimitExpression value

            Requirement.ToolTimeLimitRequirement nextTimeLimit
        | _ -> requirement

    | Requirement.ResourceRequirement resource ->
        let editableFields =
            set [
                "coresMin"
                "coresMax"
                "ramMin"
                "ramMax"
                "tmpdirMin"
                "tmpdirMax"
                "outdirMin"
                "outdirMax"
            ]

        if editableFields.Contains fieldKey then
            let parsed = parseResourceScalar value

            match fieldKey with
            | "coresMin" -> resource.CoresMin <- parsed
            | "coresMax" -> resource.CoresMax <- parsed
            | "ramMin" -> resource.RamMin <- parsed
            | "ramMax" -> resource.RamMax <- parsed
            | "tmpdirMin" -> resource.TmpdirMin <- parsed
            | "tmpdirMax" -> resource.TmpdirMax <- parsed
            | "outdirMin" -> resource.OutdirMin <- parsed
            | "outdirMax" -> resource.OutdirMax <- parsed
            | _ -> ()

            Requirement.ResourceRequirement resource
        else
            requirement

    | Requirement.InitialWorkDirRequirement listing ->
        match fieldKey with
        | "iwd.addString" ->
            listing.Add(StringEntry(SchemaSaladString.Literal ""))
            Requirement.InitialWorkDirRequirement listing
        | "iwd.addDirent" ->
            listing.Add(DirentEntry(DirentInstance(SchemaSaladString.Literal "")))

            Requirement.InitialWorkDirRequirement listing
        | "iwd.addFile" ->
            listing.Add(FileEntry(FileInstance()))
            Requirement.InitialWorkDirRequirement listing
        | "iwd.addDirectory" ->
            listing.Add(DirectoryEntry(DirectoryInstance()))
            Requirement.InitialWorkDirRequirement listing
        | _ ->
            match tryParseIndexedFieldKey "iwd.remove:" fieldKey with
            | Some index when index >= 0 && index < listing.Count ->
                listing.RemoveAt(index)
                Requirement.InitialWorkDirRequirement listing
            | _ ->
                match tryParseIndexedFieldKey "iwd.value:" fieldKey with
                | Some index when index >= 0 && index < listing.Count ->
                    let updatedEntry =
                        match listing.[index] with
                        | StringEntry entry -> StringEntry(schemaSaladWithText value entry)
                        | DirentEntry dirent ->
                            dirent.Entry <- schemaSaladWithText value dirent.Entry
                            DirentEntry dirent
                        | FileEntry file ->
                            match nonEmptyOrNone value with
                            | Some location -> file.Location <- Some location
                            | None -> file.Location <- None

                            FileEntry file
                        | DirectoryEntry directory ->
                            match nonEmptyOrNone value with
                            | Some location -> directory.Location <- Some location
                            | None -> directory.Location <- None

                            DirectoryEntry directory

                    listing.[index] <- updatedEntry
                    Requirement.InitialWorkDirRequirement listing
                | _ ->
                    match tryParseIndexedFieldKey "iwd.entryMode:" fieldKey with
                    | Some index when index >= 0 && index < listing.Count ->
                        let updatedEntry =
                            match listing.[index] with
                            | StringEntry entry -> StringEntry(schemaSaladWithMode value entry)
                            | DirentEntry dirent ->
                                dirent.Entry <- schemaSaladWithMode value dirent.Entry
                                DirentEntry dirent
                            | _ -> listing.[index]

                        listing.[index] <- updatedEntry
                        Requirement.InitialWorkDirRequirement listing
                    | _ ->
                        match tryParseIndexedFieldKey "iwd.entryname:" fieldKey with
                        | Some index when index >= 0 && index < listing.Count ->
                            let updatedEntry =
                                match listing.[index] with
                                | DirentEntry dirent ->
                                    let nextEntryName =
                                        match nonEmptyOrNone value with
                                        | Some nextValue ->
                                            match dirent.Entryname with
                                            | Some existing -> Some(schemaSaladWithText nextValue existing)
                                            | None -> Some(SchemaSaladString.Literal nextValue)
                                        | None -> None

                                    dirent.Entryname <- nextEntryName
                                    DirentEntry dirent
                                | _ -> listing.[index]

                            listing.[index] <- updatedEntry
                            Requirement.InitialWorkDirRequirement listing
                        | _ ->
                            match tryParseIndexedFieldKey "iwd.entrynameMode:" fieldKey with
                            | Some index when index >= 0 && index < listing.Count ->
                                let updatedEntry =
                                    match listing.[index] with
                                    | DirentEntry dirent ->
                                        let nextEntryName =
                                            match dirent.Entryname with
                                            | Some existing -> Some(schemaSaladWithMode value existing)
                                            | None -> Some(schemaSaladFromMode value "")

                                        dirent.Entryname <- nextEntryName
                                        DirentEntry dirent
                                    | _ -> listing.[index]

                                listing.[index] <- updatedEntry
                                Requirement.InitialWorkDirRequirement listing
                            | _ ->
                                match tryParseIndexedFieldKey "iwd.writable:" fieldKey with
                                | Some index when index >= 0 && index < listing.Count ->
                                    let updatedEntry =
                                        match listing.[index] with
                                        | DirentEntry dirent ->
                                            let nextWritable =
                                                if String.IsNullOrWhiteSpace value then
                                                    None
                                                else
                                                    match parseBoolOrNone value with
                                                    | Some writable -> Some writable
                                                    | None -> dirent.Writable

                                            dirent.Writable <- nextWritable
                                            DirentEntry dirent
                                        | _ -> listing.[index]

                                    listing.[index] <- updatedEntry
                                    Requirement.InitialWorkDirRequirement listing
                                | _ -> requirement

    | Requirement.WorkReuseRequirement workReuse ->
        match fieldKey with
        | "workReuseMode" when value = "expression" -> Requirement.WorkReuseExpressionRequirement "true"
        | "workReuseValue" ->
            match parseBoolOrNone value with
            | Some parsed ->
                workReuse.EnableReuse <- parsed
                Requirement.WorkReuseRequirement workReuse
            | None -> requirement
        | _ -> requirement

    | Requirement.WorkReuseExpressionRequirement expression ->
        match fieldKey with
        | "workReuseMode" when value = "bool" ->
            match parseBoolOrNone expression with
            | Some parsed -> Requirement.WorkReuseRequirement(WorkReuseRequirementValue(parsed))
            | None -> Requirement.WorkReuseRequirement(WorkReuseRequirementValue(true))
        | "workReuseValue" -> Requirement.WorkReuseExpressionRequirement value
        | _ -> requirement

    | Requirement.NetworkAccessRequirement networkAccess ->
        match fieldKey with
        | "networkAccessMode" when value = "expression" -> Requirement.NetworkAccessExpressionRequirement "true"
        | "networkAccessValue" ->
            match parseBoolOrNone value with
            | Some parsed ->
                networkAccess.NetworkAccess <- parsed
                Requirement.NetworkAccessRequirement networkAccess
            | None -> requirement
        | _ -> requirement

    | Requirement.NetworkAccessExpressionRequirement expression ->
        match fieldKey with
        | "networkAccessMode" when value = "bool" ->
            match parseBoolOrNone expression with
            | Some parsed -> Requirement.NetworkAccessRequirement(NetworkAccessRequirementValue(parsed))
            | None -> Requirement.NetworkAccessRequirement(NetworkAccessRequirementValue(true))
        | "networkAccessValue" -> Requirement.NetworkAccessExpressionRequirement value
        | _ -> requirement

    | Requirement.InplaceUpdateRequirement inplaceUpdate ->
        match fieldKey with
        | "inplaceUpdate" ->
            match parseBoolOrNone value with
            | Some parsed ->
                inplaceUpdate.InplaceUpdate <- parsed
                Requirement.InplaceUpdateRequirement inplaceUpdate
            | None -> requirement
        | _ -> requirement

    | _ -> requirement

let setRequirementFieldByKey (items: ResizeArray<Requirement> option) (key: string) (fieldKey: string) (value: string) =
    items
    |> Option.iter (fun requirements ->
        for index in 0 .. requirements.Count - 1 do
            let requirement = requirements.[index]

            match requirementKey requirement with
            | Some requirementKey when requirementKey = key ->
                requirements.[index] <- setRequirementFieldValue requirement fieldKey value
            | _ -> ()
    )

let setHintFieldByKey (items: ResizeArray<HintEntry> option) (key: string) (fieldKey: string) (value: string) =
    items
    |> Option.iter (fun hints ->
        for index in 0 .. hints.Count - 1 do
            let hint = hints.[index]

            match hint with
            | KnownHint requirement when requirementKey requirement = Some key ->
                hints.[index] <- KnownHint(setRequirementFieldValue requirement fieldKey value)
            | _ -> ()
    )
