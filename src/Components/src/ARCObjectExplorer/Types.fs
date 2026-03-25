namespace Swate.Components


open Fable.Core
open Swate.Components.Shared
open ARCtrl
open Feliz
open Browser.Types
open Swate.Components
open Swate.Components.Metadata


/// Generic form helpers for Electron metadata editing
type FormHelpers =

    /// Debounced text input component
    [<ReactComponent>]
    static member TextInput
        (
            value: string,
            setValue: string -> unit,
            ?label: string,
            ?placeholder: string,
            ?isArea: bool,
            ?disabled: bool,
            ?classes: string,
            ?isJoin: bool,
            ?rmv: MouseEvent -> unit,
            ?validator: string -> Result<unit, string>
        ) =
        let isArea = defaultArg isArea false
        let disabled = defaultArg disabled false
        let isJoin = defaultArg isJoin false
        let startedChange = React.useRef false
        let tempValue, setTempValue = React.useState value
        let debouncedValue = React.useDebounce (tempValue, 300)
        let validationError, setValidationError = React.useState (None: string option)

        // Update parent when debounced value changes
        React.useEffect (
            (fun () ->
                if startedChange.current then
                    // Validate if validator provided
                    match validator with
                    | Some v ->
                        match v debouncedValue with
                        | Result.Ok() ->
                            setValidationError None
                            setValue debouncedValue
                        | Result.Error msg -> setValidationError (Some msg)
                    | None ->
                        setValidationError None
                        setValue debouncedValue

                    startedChange.current <- false
            ),
            [| box debouncedValue |]
        )

        // Sync with external value changes
        React.useEffect ((fun () -> setTempValue value), [| box value |])

        let handleChange =
            fun (s: string) ->
                setTempValue s
                startedChange.current <- true

        let inputClasses = [
            if isJoin then
                "swt:join-item"
            "swt:input swt:input-bordered swt:w-full"
            if validationError.IsSome then
                "swt:input-error"
            if classes.IsSome then
                classes.Value
        ]

        Html.div [
            prop.className (
                if isJoin then
                    "swt:grow swt:join-item"
                else
                    "swt:fieldset swt:grow"
            )
            prop.children [
                if label.IsSome && not isJoin then
                    Generic.FieldTitle label.Value
                if isArea then
                    Html.textarea [
                        prop.className [
                            "swt:textarea swt:textarea-bordered swt:w-full"
                            if validationError.IsSome then
                                "swt:textarea-error"
                            if classes.IsSome then
                                classes.Value
                        ]
                        prop.disabled disabled
                        prop.readOnly disabled
                        prop.valueOrDefault tempValue
                        prop.onChange handleChange
                        if placeholder.IsSome then
                            prop.placeholder placeholder.Value
                    ]
                else
                    Html.div [
                        prop.className "swt:flex swt:gap-2 swt:items-center swt:w-full"
                        prop.children [
                            Html.input [
                                prop.className inputClasses
                                prop.type'.text
                                prop.disabled disabled
                                prop.readOnly disabled
                                prop.valueOrDefault tempValue
                                prop.onChange handleChange
                                if placeholder.IsSome then
                                    prop.placeholder placeholder.Value
                            ]
                            if rmv.IsSome then
                                Helper.deleteButton rmv.Value
                        ]
                    ]
                if validationError.IsSome then
                    Html.p [
                        prop.className "swt:text-error swt:text-sm swt:mt-1"
                        prop.text validationError.Value
                    ]
            ]
        ]

type ARCExplorerServices = {
    openPreview: string -> JS.Promise<Result<unit, string>>
    setStatusMessage: string option -> unit
    runToggleLfsMark: string -> string -> bool -> JS.Promise<Result<unit, string>>
}

[<RequireQualifiedAccess>]
type ArcObjectPreviewState =
    | NoneLoaded
    | Text of string
    | Error of string

[<StringEnum>]
type ArcExplorerNodeKind =
    | Arc
    | Group
    | Study
    | Assay
    | Workflow
    | Run
    | Table
    | DataMap
    | Note
    | Sample

[<RequireQualifiedAccess>]
type ArcExplorerNodePreviewTarget =
    | Default
    | Table of int

type ArcExplorerNode = {
    id: string
    name: string
    kind: ArcExplorerNodeKind
    path: string option
    previewTarget: ArcExplorerNodePreviewTarget
    isSelectable: bool
    isReference: bool
    isLfs: bool option
    children: ArcExplorerNode list
} with

    static member create
        (
            id: string,
            name: string,
            kind: ArcExplorerNodeKind,
            ?path: string option,
            ?previewTarget: ArcExplorerNodePreviewTarget,
            ?isSelectable: bool,
            ?isReference: bool,
            ?isLfs: bool option,
            ?children: ArcExplorerNode list
        ) =
        {
            id = id
            name = name
            kind = kind
            path = defaultArg path None
            previewTarget = defaultArg previewTarget ArcExplorerNodePreviewTarget.Default
            isSelectable = defaultArg isSelectable true
            isReference = defaultArg isReference false
            isLfs = defaultArg isLfs None
            children = defaultArg children []
        }

type ArcObjectExplorerProps = {
    rootRepoPath: string option
    nodes: ArcExplorerNode list
    selectedExplorerItemId: string option
    selectedTreeItemPath: string option
    arcFileState: ArcFiles option
    previewState: ArcObjectPreviewState
    setArcFileState: ArcFiles option -> unit
    setSelectedExplorerItemId: string option -> unit
    setSelectedTreeItemPath: string option -> unit
    services: ARCExplorerServices
}

type NoteTarget =
    | Root
    | Study of string
    | Assay of string
    | Workflow of string
    | Run of string

type NoteEntry = {
    Name: string
    RelativePath: string
    AbsolutePath: string
    Target: NoteTarget
    IsLfs: bool option
}
