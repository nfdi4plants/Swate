namespace Swate.Components.Page.CwlEditor

open System
open System.Globalization
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open ARCtrl.CWL
open Swate.Components.Page.CwlEditor.UiHelpers
open Swate.Components.Shared.Cwl.RequirementMutations

type RequirementBucket =
    | RequirementBucket
    | HintBucket

type RequirementFocus = {
    Bucket: RequirementBucket
    Key: string
}

type SidebarProps = {
    Requirements: ResizeArray<Requirement> option
    Hints: ResizeArray<HintEntry> option
    Focused: RequirementFocus option
    OnFocus: RequirementFocus option -> unit
    OnSetEnabled: RequirementBucket -> string -> bool -> unit
    OnSetField: RequirementBucket -> string -> string -> string -> unit
}

type MainProps = {
    Requirements: ResizeArray<Requirement> option
    Hints: ResizeArray<HintEntry> option
    Focused: RequirementFocus option
    OnFocus: RequirementFocus option -> unit
    OnSetEnabled: RequirementBucket -> string -> bool -> unit
}

[<AutoOpen>]
module private RequirementPickerHelpers =

    let dragPayloadType = "application/x-cwl-requirement-key"

    let bucketLabel bucket =
        match bucket with
        | RequirementBucket -> "Requirement"
        | HintBucket -> "Hint"

    let bucketKey bucket =
        match bucket with
        | RequirementBucket -> "requirement"
        | HintBucket -> "hint"

    let tryGetRequirementByKeyFromItems key (items: ResizeArray<Requirement> option) =
        items
        |> Option.defaultValue (ResizeArray())
        |> Seq.tryFind (fun req -> requirementKey req = Some key)

    let tryGetKnownHintByKeyFromItems key (items: ResizeArray<HintEntry> option) =
        items
        |> Option.defaultValue (ResizeArray())
        |> Seq.tryPick (fun hint ->
            match hint with
            | KnownHint req when requirementKey req = Some key -> Some req
            | _ -> None
        )

    let tryGetFocusedRequirement (props: SidebarProps) =
        match props.Focused with
        | Some focused ->
            let requirement =
                match focused.Bucket with
                | RequirementBucket -> tryGetRequirementByKeyFromItems focused.Key props.Requirements
                | HintBucket -> tryGetKnownHintByKeyFromItems focused.Key props.Hints

            requirement |> Option.map (fun req -> focused, req)
        | None -> None

    let eventTargetValue (ev: obj) =
        let target = ev?target
        if isNull target then "" else string target?value

    let fieldTestId (fieldKey: string) =
        fieldKey.Replace(".", "-").Replace(":", "-").ToLowerInvariant()

    let schemaSaladText (value: SchemaSaladString) =
        match value with
        | SchemaSaladString.Literal text
        | SchemaSaladString.Include text
        | SchemaSaladString.Import text -> text

    let schemaSaladTextOrEmpty (value: SchemaSaladString option) =
        value |> Option.map schemaSaladText |> Option.defaultValue ""

    let schemaSaladMode (value: SchemaSaladString) =
        match value with
        | SchemaSaladString.Literal _ -> "literal"
        | SchemaSaladString.Include _ -> "include"
        | SchemaSaladString.Import _ -> "import"

    let loadListingToString (value: LoadListingEnum) = LoadListingEnum.toCwlString value

    let tryDynamicStringValue (dynamicObj: obj) =
        let tryGet key =
            try
                let value =
                    match dynamicObj with
                    | :? FileInstance as file ->
                        match key with
                        | "location" -> file.Location
                        | "path" -> file.Path
                        | "basename" -> file.Basename
                        | _ -> None
                    | :? DirectoryInstance as directory ->
                        match key with
                        | "location" -> directory.Location
                        | "path" -> directory.Path
                        | "basename" -> directory.Basename
                        | _ -> None
                    | _ -> None

                value |> Option.map string
            with _ ->
                None

        [ "location"; "path"; "basename" ]
        |> List.tryPick tryGet
        |> Option.defaultValue ""

    let resourceScalarText (resource: ResourceRequirementInstance) (fieldName: string) =
        match resource.TryGetInt64(fieldName) with
        | Some intValue -> string intValue
        | None ->
            match resource.TryGetFloat(fieldName) with
            | Some floatValue -> floatValue.ToString(CultureInfo.InvariantCulture)
            | None -> resource.TryGetExpression(fieldName) |> Option.defaultValue ""

    let inlineJavascriptExpressionLibText (value: InlineJavascriptRequirementValue) =
        value.ExpressionLib
        |> Option.defaultValue (ResizeArray())
        |> Seq.toList
        |> String.concat "\n"

    let delimitedListText (value: ResizeArray<string> option) =
        value |> Option.defaultValue (ResizeArray()) |> Seq.toList |> String.concat "\n"

    let schemaDefTypeKey (schemaType: SchemaDefRequirementType) = cwlTypeToKey (Some schemaType.Type_)

    let direntWritableValue (value: bool option) =
        match value with
        | Some true -> "true"
        | Some false -> "false"
        | None -> ""

[<Erase; Mangle(false)>]
type RequirementPicker =

    static member private FieldInput
        (props: SidebarProps, focused: RequirementFocus, label: string, fieldKey: string, value: string)
        : ReactElement =
        Html.label [
            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
            prop.children [
                Html.span [ prop.className "swt:text-sm"; prop.text label ]
                Html.input [
                    prop.testId (sprintf "cwl-requirement-field-%s" (fieldTestId fieldKey))
                    prop.key (sprintf "%A:%s:%s" focused.Bucket focused.Key fieldKey)
                    prop.className "swt:input swt:input-sm swt:w-full"
                    prop.defaultValue value
                    prop.onBlur (fun ev -> props.OnSetField focused.Bucket focused.Key fieldKey (eventTargetValue ev))
                ]
            ]
        ]

    static member private FieldSelect
        (
            props: SidebarProps,
            focused: RequirementFocus,
            label: string,
            fieldKey: string,
            value: string,
            options: (string * string) list
        ) : ReactElement =
        Html.label [
            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
            prop.children [
                Html.span [ prop.className "swt:text-sm"; prop.text label ]
                Html.select [
                    prop.testId (sprintf "cwl-requirement-select-%s" (fieldTestId fieldKey))
                    prop.className "swt:select swt:select-sm swt:w-full"
                    prop.value value
                    prop.onChange (fun nextValue -> props.OnSetField focused.Bucket focused.Key fieldKey nextValue)
                    prop.children [
                        for optionValue, optionLabel in options do
                            Html.option [
                                prop.key optionValue
                                prop.value optionValue
                                prop.text optionLabel
                            ]
                    ]
                ]
            ]
        ]

    static member private FieldTextArea
        (props: SidebarProps, focused: RequirementFocus, label: string, fieldKey: string, value: string)
        : ReactElement =
        Html.label [
            prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
            prop.children [
                Html.span [ prop.className "swt:text-sm"; prop.text label ]
                Html.textarea [
                    prop.testId (sprintf "cwl-requirement-textarea-%s" (fieldTestId fieldKey))
                    prop.key (sprintf "%A:%s:%s" focused.Bucket focused.Key fieldKey)
                    prop.className "swt:textarea swt:w-full"
                    prop.defaultValue value
                    prop.rows 4
                    prop.onBlur (fun ev -> props.OnSetField focused.Bucket focused.Key fieldKey (eventTargetValue ev))
                ]
            ]
        ]

    static member private RemoveButton (props: SidebarProps) (focused: RequirementFocus) : ReactElement =
        Html.button [
            prop.testId "cwl-requirement-remove"
            prop.className "swt:btn swt:btn-sm swt:btn-error"
            prop.text (sprintf "Remove %s" (bucketLabel focused.Bucket))
            prop.onClick (fun _ ->
                props.OnSetEnabled focused.Bucket focused.Key false
                props.OnFocus None
            )
        ]

    [<ReactComponent>]
    static member private InitialWorkDirEditor
        (version: int, props: SidebarProps, focused: RequirementFocus, listing: ResizeArray<InitialWorkDirEntry>)
        : ReactElement =
        let schemaSaladModeOptions = [
            "literal", "Literal"
            "include", "$include"
            "import", "$import"
        ]

        let renderStringEntry (index: int) (entryValue: SchemaSaladString) =
            Html.li [
                prop.testId (sprintf "cwl-requirement-iwd-string-%d" index)
                prop.key (sprintf "iwd-string-%d" index)
                prop.children [
                    RequirementPicker.FieldSelect(
                        props,
                        focused,
                        "String mode",
                        $"iwd.entryMode:{index}",
                        schemaSaladMode entryValue,
                        schemaSaladModeOptions
                    )
                    RequirementPicker.FieldInput(
                        props,
                        focused,
                        "String value",
                        $"iwd.value:{index}",
                        schemaSaladText entryValue
                    )
                    Html.button [
                        prop.testId "cwl-requirement-iwd-remove"
                        prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                        prop.text "Remove"
                        prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key $"iwd.remove:{index}" "")
                    ]
                ]
            ]

        let renderDirentEntry (index: int) (dirent: DirentInstance) =
            let entryNameValue =
                dirent.Entryname |> Option.map schemaSaladText |> Option.defaultValue ""

            let entryNameMode =
                dirent.Entryname |> Option.map schemaSaladMode |> Option.defaultValue "literal"

            Html.li [
                prop.testId (sprintf "cwl-requirement-iwd-dirent-%d" index)
                prop.key (sprintf "iwd-dirent-%d" index)
                prop.children [
                    RequirementPicker.FieldSelect(
                        props,
                        focused,
                        "Dirent.entry mode",
                        $"iwd.entryMode:{index}",
                        schemaSaladMode dirent.Entry,
                        schemaSaladModeOptions
                    )
                    RequirementPicker.FieldInput(
                        props,
                        focused,
                        "Dirent.entry",
                        $"iwd.value:{index}",
                        schemaSaladText dirent.Entry
                    )
                    RequirementPicker.FieldSelect(
                        props,
                        focused,
                        "Dirent.entryname mode",
                        $"iwd.entrynameMode:{index}",
                        entryNameMode,
                        schemaSaladModeOptions
                    )
                    RequirementPicker.FieldInput(
                        props,
                        focused,
                        "Dirent.entryname",
                        $"iwd.entryname:{index}",
                        entryNameValue
                    )
                    RequirementPicker.FieldSelect(
                        props,
                        focused,
                        "Dirent.writable",
                        $"iwd.writable:{index}",
                        direntWritableValue dirent.Writable,
                        [ "", "default"; "true", "true"; "false", "false" ]
                    )
                    Html.button [
                        prop.testId "cwl-requirement-iwd-remove"
                        prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                        prop.text "Remove"
                        prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key $"iwd.remove:{index}" "")
                    ]
                ]
            ]

        let renderFileOrDirectoryEntry (index: int) (entryLabel: string) (value: string) =
            Html.li [
                prop.testId (sprintf "cwl-requirement-iwd-file-directory-%d" index)
                prop.key (sprintf "iwd-filedir-%d" index)
                prop.children [
                    RequirementPicker.FieldInput(props, focused, entryLabel, $"iwd.value:{index}", value)
                    Html.button [
                        prop.testId "cwl-requirement-iwd-remove"
                        prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                        prop.text "Remove"
                        prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key $"iwd.remove:{index}" "")
                    ]
                ]
            ]

        let renderEntry (index: int) (entry: InitialWorkDirEntry) =
            match entry with
            | StringEntry textValue -> renderStringEntry index textValue
            | DirentEntry dirent -> renderDirentEntry index dirent
            | FileEntry file -> renderFileOrDirectoryEntry index "File.location" (tryDynamicStringValue file)
            | DirectoryEntry directory ->
                renderFileOrDirectoryEntry index "Directory.location" (tryDynamicStringValue directory)

        Html.div [
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text "InitialWorkDir listing"
                ]
                if listing.Count = 0 then
                    Html.p [
                        prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                        prop.text "No entries yet."
                    ]
                else
                    Html.ul [
                        prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                        prop.children [
                            for index, entry in listing |> Seq.indexed do
                                renderEntry index entry
                        ]
                    ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-requirement-iwd-add-string"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Add String"
                            prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key "iwd.addString" "")
                        ]
                        Html.button [
                            prop.testId "cwl-requirement-iwd-add-dirent"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Add Dirent"
                            prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key "iwd.addDirent" "")
                        ]
                        Html.button [
                            prop.testId "cwl-requirement-iwd-add-file"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Add File"
                            prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key "iwd.addFile" "")
                        ]
                        Html.button [
                            prop.testId "cwl-requirement-iwd-add-directory"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Add Directory"
                            prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key "iwd.addDirectory" "")
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private DockerForm
        (version: int, props: SidebarProps, focused: RequirementFocus, docker: DockerRequirement)
        : ReactElement =
        let dockerFileMode =
            docker.DockerFile |> Option.map schemaSaladMode |> Option.defaultValue "literal"

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: DockerRequirement" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldInput(
                    props,
                    focused,
                    "dockerPull",
                    "dockerPull",
                    (docker.DockerPull |> Option.defaultValue "")
                )
                RequirementPicker.FieldInput(
                    props,
                    focused,
                    "dockerImageId",
                    "dockerImageId",
                    (docker.DockerImageId |> Option.defaultValue "")
                )
                RequirementPicker.FieldInput(
                    props,
                    focused,
                    "dockerLoad",
                    "dockerLoad",
                    (docker.DockerLoad |> Option.defaultValue "")
                )
                RequirementPicker.FieldInput(
                    props,
                    focused,
                    "dockerImport",
                    "dockerImport",
                    (docker.DockerImport |> Option.defaultValue "")
                )
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "dockerFile mode",
                    "dockerFileMode",
                    dockerFileMode,
                    [
                        "literal", "Literal"
                        "include", "$include"
                        "import", "$import"
                    ]
                )
                RequirementPicker.FieldInput(
                    props,
                    focused,
                    "dockerFile",
                    "dockerFile",
                    schemaSaladTextOrEmpty docker.DockerFile
                )
                RequirementPicker.FieldInput(
                    props,
                    focused,
                    "dockerOutputDirectory",
                    "dockerOutputDirectory",
                    (docker.DockerOutputDirectory |> Option.defaultValue "")
                )
                RequirementPicker.FieldTextArea(
                    props,
                    focused,
                    "dockerRunOptions (one entry per line)",
                    "dockerRunOptions",
                    delimitedListText docker.DockerRunOptions
                )
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private InlineJavascriptForm
        (
            version: int,
            props: SidebarProps,
            focused: RequirementFocus,
            inlineJavascript: InlineJavascriptRequirementValue
        ) : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: InlineJavascriptRequirement" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldTextArea(
                    props,
                    focused,
                    "expressionLib (one entry per line)",
                    "expressionLib",
                    inlineJavascriptExpressionLibText inlineJavascript
                )
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private SchemaDefForm
        (
            version: int,
            props: SidebarProps,
            focused: RequirementFocus,
            schemaTypes: ResizeArray<SchemaDefRequirementType>
        ) : ReactElement =
        let schemaTypeOptions =
            cwlTypeSelectOptions
            |> List.filter (fun (value, _) -> String.IsNullOrWhiteSpace value |> not)

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: SchemaDefRequirement" (bucketLabel focused.Bucket))
                ]
                if schemaTypes.Count = 0 then
                    Html.p [
                        prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                        prop.text "No schema definitions yet."
                    ]
                else
                    Html.ul [
                        prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                        prop.children [
                            for index, schemaType in schemaTypes |> Seq.indexed do
                                let currentTypeKey = schemaDefTypeKey schemaType

                                let typeOptions =
                                    if currentTypeKey = "custom" then
                                        ("custom", "custom (loaded)") :: schemaTypeOptions
                                    else
                                        schemaTypeOptions

                                Html.li [
                                    prop.testId (sprintf "cwl-requirement-schema-type-%d" index)
                                    prop.key $"schema-type-{index}"
                                    prop.children [
                                        RequirementPicker.FieldInput(
                                            props,
                                            focused,
                                            "name",
                                            $"schema.name:{index}",
                                            schemaType.Name
                                        )
                                        RequirementPicker.FieldSelect(
                                            props,
                                            focused,
                                            "type",
                                            $"schema.type:{index}",
                                            currentTypeKey,
                                            typeOptions
                                        )
                                        if currentTypeKey = "custom" then
                                            Html.p [
                                                prop.className
                                                    "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                                prop.text
                                                    "Loaded type is complex/custom. Selecting a value replaces it with a simple type."
                                            ]
                                        Html.button [
                                            prop.testId "cwl-requirement-schema-remove"
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Remove entry"
                                            prop.onClick (fun _ ->
                                                props.OnSetField focused.Bucket focused.Key $"schema.remove:{index}" ""
                                            )
                                        ]
                                    ]
                                ]
                        ]
                    ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-requirement-schema-add"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Add schema type"
                            prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key "schema.add" "")
                        ]
                    ]
                ]
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private LoadListingForm
        (version: int, props: SidebarProps, focused: RequirementFocus, loadListing: LoadListingRequirementValue)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: LoadListingRequirement" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "loadListing",
                    "loadListing",
                    loadListingToString loadListing.LoadListing,
                    [
                        "no_listing", "no_listing"
                        "shallow_listing", "shallow_listing"
                        "deep_listing", "deep_listing"
                    ]
                )
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private SoftwareForm
        (version: int, props: SidebarProps, focused: RequirementFocus, packages: ResizeArray<SoftwarePackage>)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: SoftwareRequirement" (bucketLabel focused.Bucket))
                ]
                if packages.Count = 0 then
                    Html.p [
                        prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                        prop.text "No software packages yet."
                    ]
                else
                    Html.ul [
                        prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                        prop.children [
                            for index, pkg in packages |> Seq.indexed do
                                Html.li [
                                    prop.testId (sprintf "cwl-requirement-software-package-%d" index)
                                    prop.key $"software-package-{index}"
                                    prop.children [
                                        RequirementPicker.FieldInput(
                                            props,
                                            focused,
                                            "package",
                                            $"software.package:{index}",
                                            pkg.Package
                                        )
                                        RequirementPicker.FieldTextArea(
                                            props,
                                            focused,
                                            "version (one entry per line)",
                                            $"software.version:{index}",
                                            delimitedListText pkg.Version
                                        )
                                        RequirementPicker.FieldTextArea(
                                            props,
                                            focused,
                                            "specs (one entry per line)",
                                            $"software.specs:{index}",
                                            delimitedListText pkg.Specs
                                        )
                                        Html.button [
                                            prop.testId "cwl-requirement-software-remove"
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Remove package"
                                            prop.onClick (fun _ ->
                                                props.OnSetField
                                                    focused.Bucket
                                                    focused.Key
                                                    $"software.remove:{index}"
                                                    ""
                                            )
                                        ]
                                    ]
                                ]
                        ]
                    ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-requirement-software-add"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Add package"
                            prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key "software.add" "")
                        ]
                    ]
                ]
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private EnvVarForm
        (version: int, props: SidebarProps, focused: RequirementFocus, envDefs: ResizeArray<EnvironmentDef>)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: EnvVarRequirement" (bucketLabel focused.Bucket))
                ]
                if envDefs.Count = 0 then
                    Html.p [
                        prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                        prop.text "No environment variables yet."
                    ]
                else
                    Html.ul [
                        prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                        prop.children [
                            for index, envDef in envDefs |> Seq.indexed do
                                Html.li [
                                    prop.testId (sprintf "cwl-requirement-environment-variable-%d" index)
                                    prop.key $"envdef-{index}"
                                    prop.children [
                                        RequirementPicker.FieldInput(
                                            props,
                                            focused,
                                            "envName",
                                            $"env.name:{index}",
                                            envDef.EnvName
                                        )
                                        RequirementPicker.FieldInput(
                                            props,
                                            focused,
                                            "envValue",
                                            $"env.value:{index}",
                                            envDef.EnvValue
                                        )
                                        Html.button [
                                            prop.testId "cwl-requirement-environment-remove"
                                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                                            prop.text "Remove variable"
                                            prop.onClick (fun _ ->
                                                props.OnSetField focused.Bucket focused.Key $"env.remove:{index}" ""
                                            )
                                        ]
                                    ]
                                ]
                        ]
                    ]
                Html.div [
                    prop.className "swt:flex swt:flex-wrap swt:gap-2"
                    prop.children [
                        Html.button [
                            prop.testId "cwl-requirement-environment-add"
                            prop.className "swt:btn swt:btn-sm swt:btn-ghost"
                            prop.text "Add variable"
                            prop.onClick (fun _ -> props.OnSetField focused.Bucket focused.Key "env.add" "")
                        ]
                    ]
                ]
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private ToolTimeLimitForm
        (version: int, props: SidebarProps, focused: RequirementFocus, timeLimit: ToolTimeLimitValue)
        : ReactElement =
        let mode, valueText =
            match timeLimit with
            | ToolTimeLimitSeconds seconds -> "seconds", string seconds
            | ToolTimeLimitExpression expression -> "expression", expression

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: ToolTimeLimitRequirement" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "Mode",
                    "timelimitMode",
                    mode,
                    [ "seconds", "Seconds"; "expression", "Expression" ]
                )
                Html.label [
                    prop.className "swt:label swt:flex-col swt:items-start swt:gap-1"
                    prop.children [
                        Html.span [ prop.className "swt:text-sm"; prop.text "timelimit" ]
                        Html.input [
                            prop.testId "cwl-requirement-timelimit-value"
                            prop.key (sprintf "%A:%s:timelimitValue:%s" focused.Bucket focused.Key mode)
                            prop.className "swt:input swt:input-sm swt:w-full"
                            if mode = "seconds" then
                                prop.type'.number
                            prop.defaultValue valueText
                            prop.onBlur (fun ev ->
                                props.OnSetField focused.Bucket focused.Key "timelimitValue" (eventTargetValue ev)
                            )
                        ]
                    ]
                ]
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private ResourceForm
        (version: int, props: SidebarProps, focused: RequirementFocus, resource: ResourceRequirementInstance)
        : ReactElement =
        let fields = [
            "coresMin"
            "coresMax"
            "ramMin"
            "ramMax"
            "tmpdirMin"
            "tmpdirMax"
            "outdirMin"
            "outdirMax"
        ]

        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: ResourceRequirement" (bucketLabel focused.Bucket))
                ]
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text "Values accept integers, floats, or expressions."
                ]
                for fieldName in fields do
                    RequirementPicker.FieldInput(
                        props,
                        focused,
                        fieldName,
                        fieldName,
                        resourceScalarText resource fieldName
                    )
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private InitialWorkDirForm
        (version: int, props: SidebarProps, focused: RequirementFocus, listing: ResizeArray<InitialWorkDirEntry>)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: InitialWorkDirRequirement" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.InitialWorkDirEditor(version, props, focused, listing)
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private WorkReuseForm
        (version: int, props: SidebarProps, focused: RequirementFocus, workReuse: WorkReuseRequirementValue)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: WorkReuseRequirement" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "Mode",
                    "workReuseMode",
                    "bool",
                    [ "bool", "Boolean"; "expression", "Expression" ]
                )
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "enableReuse",
                    "workReuseValue",
                    (if workReuse.EnableReuse then "true" else "false"),
                    [ "true", "true"; "false", "false" ]
                )
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private WorkReuseExpressionForm
        (version: int, props: SidebarProps, focused: RequirementFocus, expression: string)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: WorkReuseRequirement (expression)" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "Mode",
                    "workReuseMode",
                    "expression",
                    [ "bool", "Boolean"; "expression", "Expression" ]
                )
                RequirementPicker.FieldInput(props, focused, "enableReuse expression", "workReuseValue", expression)
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private NetworkAccessForm
        (version: int, props: SidebarProps, focused: RequirementFocus, networkAccess: NetworkAccessRequirementValue)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: NetworkAccessRequirement" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "Mode",
                    "networkAccessMode",
                    "bool",
                    [ "bool", "Boolean"; "expression", "Expression" ]
                )
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "networkAccess",
                    "networkAccessValue",
                    (if networkAccess.NetworkAccess then "true" else "false"),
                    [ "true", "true"; "false", "false" ]
                )
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private NetworkAccessExpressionForm
        (version: int, props: SidebarProps, focused: RequirementFocus, expression: string)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: NetworkAccessRequirement (expression)" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "Mode",
                    "networkAccessMode",
                    "expression",
                    [ "bool", "Boolean"; "expression", "Expression" ]
                )
                RequirementPicker.FieldInput(
                    props,
                    focused,
                    "networkAccess expression",
                    "networkAccessValue",
                    expression
                )
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private InplaceUpdateForm
        (version: int, props: SidebarProps, focused: RequirementFocus, inplaceUpdate: InplaceUpdateRequirementValue)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: InplaceUpdateRequirement" (bucketLabel focused.Bucket))
                ]
                RequirementPicker.FieldSelect(
                    props,
                    focused,
                    "inplaceUpdate",
                    "inplaceUpdate",
                    (if inplaceUpdate.InplaceUpdate then "true" else "false"),
                    [ "true", "true"; "false", "false" ]
                )
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private FallbackForm
        (version: int, props: SidebarProps, focused: RequirementFocus, requirement: Requirement)
        : ReactElement =
        Html.div [
            prop.className "swt:flex swt:flex-col swt:gap-2"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text (sprintf "%s: %s" (bucketLabel focused.Bucket) (requirementLabel requirement))
                ]
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text "No specialized fields are available for this requirement type yet."
                ]
                RequirementPicker.RemoveButton props focused
            ]
        ]

    [<ReactComponent>]
    static member private DetailForm
        (version: int, props: SidebarProps, focused: RequirementFocus, requirement: Requirement)
        : ReactElement =
        match requirement with
        | Requirement.DockerRequirement docker -> RequirementPicker.DockerForm(version, props, focused, docker)
        | Requirement.InlineJavascriptRequirement inlineJavascript ->
            RequirementPicker.InlineJavascriptForm(version, props, focused, inlineJavascript)
        | Requirement.SchemaDefRequirement schemaTypes ->
            RequirementPicker.SchemaDefForm(version, props, focused, schemaTypes)
        | Requirement.LoadListingRequirement loadListing ->
            RequirementPicker.LoadListingForm(version, props, focused, loadListing)
        | Requirement.SoftwareRequirement packages -> RequirementPicker.SoftwareForm(version, props, focused, packages)
        | Requirement.EnvVarRequirement envDefs -> RequirementPicker.EnvVarForm(version, props, focused, envDefs)
        | Requirement.ToolTimeLimitRequirement timeLimit ->
            RequirementPicker.ToolTimeLimitForm(version, props, focused, timeLimit)
        | Requirement.ResourceRequirement resource -> RequirementPicker.ResourceForm(version, props, focused, resource)
        | Requirement.InitialWorkDirRequirement listing ->
            RequirementPicker.InitialWorkDirForm(version, props, focused, listing)
        | Requirement.WorkReuseRequirement workReuse ->
            RequirementPicker.WorkReuseForm(version, props, focused, workReuse)
        | Requirement.WorkReuseExpressionRequirement expression ->
            RequirementPicker.WorkReuseExpressionForm(version, props, focused, expression)
        | Requirement.NetworkAccessRequirement networkAccess ->
            RequirementPicker.NetworkAccessForm(version, props, focused, networkAccess)
        | Requirement.NetworkAccessExpressionRequirement expression ->
            RequirementPicker.NetworkAccessExpressionForm(version, props, focused, expression)
        | Requirement.InplaceUpdateRequirement inplaceUpdate ->
            RequirementPicker.InplaceUpdateForm(version, props, focused, inplaceUpdate)
        | _ -> RequirementPicker.FallbackForm(version, props, focused, requirement)

    [<ReactComponent>]
    static member RequirementSidebarPanel(version: int, props: SidebarProps) : ReactElement =
        let focusedEditor =
            match tryGetFocusedRequirement props with
            | Some(focused, requirement) -> RequirementPicker.DetailForm(version, props, focused, requirement)
            | None ->
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text "Select a requirement or hint from the main section to edit specialized fields."
                ]

        Html.section [
            prop.testId "cwl-requirement-sidebar-panel"
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.h3 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text "Requirements & Hints"
                ]
                Html.p [
                    prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                    prop.text "Drag a requirement type into either Requirements or Hints on the right."
                ]
                Html.ul [
                    prop.className "swt:menu swt:bg-base-100 swt:rounded-box"
                    prop.children [
                        for template in requirementTemplates do
                            Html.li [
                                prop.testId (sprintf "cwl-requirement-template-%s" template.Key)
                                prop.key template.Key
                                prop.draggable true
                                prop.onDragStart (fun e ->
                                    e.dataTransfer.setData (dragPayloadType, template.Key) |> ignore
                                    e.dataTransfer.setData ("text/plain", template.Key) |> ignore
                                )
                                prop.text template.Label
                            ]
                    ]
                ]
                focusedEditor
            ]
        ]

    [<ReactComponent>]
    static member private RequirementsList
        (
            version: int,
            bucket: RequirementBucket,
            title: string,
            items: ResizeArray<Requirement> option,
            focused: RequirementFocus option,
            onFocus: RequirementFocus option -> unit,
            onSetEnabled: RequirementBucket -> string -> bool -> unit
        ) : ReactElement =
        let values = items |> Option.defaultValue (ResizeArray())
        let isDragActive, setIsDragActive = React.useState (false)

        Html.div [
            prop.testId (sprintf "cwl-requirement-list-%s" (bucketKey bucket))
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text title
                ]
                Html.ul [
                    prop.className [
                        "swt:menu swt:bg-base-100 swt:rounded-box"
                        if isDragActive then
                            "swt:ring-2 swt:ring-primary"
                    ]
                    prop.onDragEnter (fun e ->
                        e.preventDefault ()
                        setIsDragActive true
                    )
                    prop.onDragOver (fun e ->
                        e.preventDefault ()
                        setIsDragActive true
                    )
                    prop.onDragLeave (fun _ -> setIsDragActive false)
                    prop.onDrop (fun e ->
                        e.preventDefault ()
                        setIsDragActive false
                        let key = e.dataTransfer.getData (dragPayloadType)

                        if System.String.IsNullOrWhiteSpace key |> not then
                            onSetEnabled bucket key true
                            onFocus (Some { Bucket = bucket; Key = key })
                    )
                    prop.children [
                        if values.Count = 0 then
                            Html.li [
                                prop.testId (sprintf "cwl-requirement-empty-%s" (bucketKey bucket))
                                prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                prop.text "Drop a requirement type here."
                            ]
                        else
                            for index, req in values |> Seq.indexed do
                                let reqKey = requirementKey req

                                let isSelected =
                                    match focused, reqKey with
                                    | Some current, Some key -> current.Bucket = bucket && current.Key = key
                                    | _ -> false

                                Html.li [
                                    prop.testId (sprintf "cwl-requirement-item-%s-%d" (bucketKey bucket) index)
                                    prop.key (sprintf "%A-%d-%s" bucket index (requirementLabel req))
                                    prop.className [
                                        if isSelected then
                                            "swt:menu-active"
                                    ]
                                    match reqKey with
                                    | Some key -> prop.onClick (fun _ -> onFocus (Some { Bucket = bucket; Key = key }))
                                    | None -> ()
                                    prop.text (requirementLabel req)
                                ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member private HintsList
        (
            version: int,
            bucket: RequirementBucket,
            title: string,
            items: ResizeArray<HintEntry> option,
            focused: RequirementFocus option,
            onFocus: RequirementFocus option -> unit,
            onSetEnabled: RequirementBucket -> string -> bool -> unit
        ) : ReactElement =
        let values = items |> Option.defaultValue (ResizeArray())
        let isDragActive, setIsDragActive = React.useState (false)

        Html.div [
            prop.testId "cwl-requirement-hints-list"
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.h4 [
                    prop.className "swt:font-semibold swt:text-base-content"
                    prop.text title
                ]
                Html.ul [
                    prop.className [
                        "swt:menu swt:bg-base-100 swt:rounded-box"
                        if isDragActive then
                            "swt:ring-2 swt:ring-primary"
                    ]
                    prop.onDragEnter (fun e ->
                        e.preventDefault ()
                        setIsDragActive true
                    )
                    prop.onDragOver (fun e ->
                        e.preventDefault ()
                        setIsDragActive true
                    )
                    prop.onDragLeave (fun _ -> setIsDragActive false)
                    prop.onDrop (fun e ->
                        e.preventDefault ()
                        setIsDragActive false
                        let key = e.dataTransfer.getData (dragPayloadType)

                        if System.String.IsNullOrWhiteSpace key |> not then
                            onSetEnabled bucket key true
                            onFocus (Some { Bucket = bucket; Key = key })
                    )
                    prop.children [
                        if values.Count = 0 then
                            Html.li [
                                prop.testId "cwl-requirement-empty-hint"
                                prop.className "swt:text-base-content/60 swt:italic swt:p-4 swt:text-center"
                                prop.text "Drop a requirement type here."
                            ]
                        else
                            for index, hint in values |> Seq.indexed do
                                let label, hintKey =
                                    match hint with
                                    | KnownHint req -> requirementLabel req, requirementKey req
                                    | UnknownHint unknown ->
                                        let className = unknown.Class |> Option.defaultValue "unknown class"
                                        $"UnknownHint ({className})", None

                                let isSelected =
                                    match focused, hintKey with
                                    | Some current, Some key -> current.Bucket = bucket && current.Key = key
                                    | _ -> false

                                Html.li [
                                    prop.testId (sprintf "cwl-requirement-hint-item-%d" index)
                                    prop.key (sprintf "%A-%d-%s" bucket index label)
                                    prop.className [
                                        if isSelected then
                                            "swt:menu-active"
                                    ]
                                    match hintKey with
                                    | Some key -> prop.onClick (fun _ -> onFocus (Some { Bucket = bucket; Key = key }))
                                    | None -> ()
                                    prop.text label
                                ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member RequirementMainPanel(version: int, props: MainProps) : ReactElement =
        Html.section [
            prop.testId "cwl-requirement-main-panel"
            prop.className "swt:card swt:bg-base-200 swt:p-4"
            prop.children [
                Html.div [
                    prop.className "swt:mb-2"
                    prop.children [
                        Html.h3 [
                            prop.className "swt:font-semibold swt:text-base-content"
                            prop.text "Requirements & Hints"
                        ]
                    ]
                ]
                RequirementPicker.RequirementsList(
                    version,
                    RequirementBucket,
                    "Requirements",
                    props.Requirements,
                    props.Focused,
                    props.OnFocus,
                    props.OnSetEnabled
                )
                RequirementPicker.HintsList(
                    version,
                    HintBucket,
                    "Hints",
                    props.Hints,
                    props.Focused,
                    props.OnFocus,
                    props.OnSetEnabled
                )
            ]
        ]
