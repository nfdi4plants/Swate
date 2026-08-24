namespace Swate.Components.Composite.ValidationPackageSelector

open Fable.Core
open Feliz
open ARCtrl.ValidationPackages
open Swate.Components
open Swate.Components.Composite.ValidationPackageSelector.Context
open Types

module private ValidationPackageSelectorModel =

    let createLatestPackage (dto: ValidationPackageDTO) =
        ValidationPackage(dto.Name, ?version = Some(Helper.toVersionString dto))

[<Erase; Mangle(false)>]
type ValidationPackageSelector =

    [<ReactComponent>]
    static member private UnlistedBanner() =
        let ctx = useValidationPackageSelectorCtx ()

        let expanded, setExpanded = React.useState false

        if Array.isEmpty ctx.UnlistedNames then
            Html.none
        else
            Html.div [
                prop.className "swt:alert swt:alert-warning"
                prop.testId "validation-package-selector-unlisted-banner"
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:w-full"
                        prop.children [
                            Html.div [
                                prop.className "swt:flex swt:items-center swt:gap-2"
                                prop.children [
                                    Html.span [
                                        prop.className "swt:iconify swt:fluent--warning-20-regular"
                                    ]
                                    Html.span (
                                        sprintf
                                            "%d Packages in current config not available online"
                                            ctx.UnlistedNames.Length
                                    )
                                    Html.button [
                                        prop.type' "button"
                                        prop.className "swt:btn swt:btn-xs"
                                        prop.text (if expanded then "Hide" else "Show")
                                        prop.onClick (fun _ -> setExpanded (not expanded))
                                    ]
                                ]
                            ]
                            if expanded then
                                Html.ul [
                                    prop.children [
                                        for name in ctx.UnlistedNames do
                                            Html.li [
                                                prop.className "swt:flex swt:items-center swt:gap-2 swt:py-1"
                                                prop.children [
                                                    Html.span [ prop.text name ]
                                                    Html.button [
                                                        prop.type' "button"
                                                        prop.testId (
                                                            "validation-package-selector-remove-unlisted-" + name
                                                        )
                                                        prop.className "swt:btn swt:btn-xs swt:btn-error"
                                                        prop.text "Remove"
                                                        prop.onClick (fun _ -> ctx.RemoveUnlisted name)
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]

    [<ReactComponent>]
    static member private SubmitBar() =
        let ctx = useValidationPackageSelectorCtx ()

        Html.div [
            prop.className "swt:flex swt:justify-end swt:items-center swt:gap-2 swt:p-2 swt:border-t"
            prop.children [
                if ctx.Submitting then
                    Html.span [
                        prop.key "submitting-spinner"
                        prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                    ]
                Html.button [
                    prop.key "submit-button"
                    prop.type' "button"
                    prop.testId "validation-package-selector-submit"
                    prop.className "swt:btn swt:btn-primary"
                    prop.disabled (not ctx.Dirty || ctx.Submitting)
                    prop.onClick (fun _ -> ctx.Submit())
                    prop.text "Submit"
                ]
            ]
        ]

    [<ReactComponent(true)>]
    static member ValidationPackageSelector
        (
            config: ValidationPackagesConfig,
            writeConfig: ValidationPackagesConfig -> JS.Promise<Result<unit, exn>>,
            fetchValidationPackages: unit -> JS.Promise<ValidationPackageDTO[]>,
            ?onError: exn -> unit
        ) =
        let state, setState = React.useState (fun () -> SelectorState.Idle)
        let edits, setEdits = React.useState Map.empty<string, ValidationPackage option>
        let removedUnlisted, setRemovedUnlisted = React.useState Set.empty<string>
        let submitting, setSubmitting = React.useState false

        React.useEffectOnce (fun () ->
            setState SelectorState.Loading

            fetchValidationPackages ()
            |> Promise.map (fun packages -> setState (SelectorState.Loaded packages))
            |> Promise.catch (fun ex ->
                console.error ("Error fetching validation packages:", ex)
                setState (SelectorState.Error ex)
                onError |> Option.iter (fun f -> f ex)
            )
            |> Promise.start
        )

        let packages =
            match state with
            | SelectorState.Loaded packages -> packages
            | _ -> [||]

        let rowStateOf (dto: ValidationPackageDTO) =
            match Map.tryFind dto.Name edits with
            | Some None -> PackageRowState.Unchecked
            | Some(Some p) ->
                if p.Version = Some(Helper.toVersionString dto) then
                    PackageRowState.Checked
                else
                    PackageRowState.HasOlderVersion
            | None -> Helper.rowState config dto

        let toggle (dto: ValidationPackageDTO) =
            let latest = ValidationPackageSelectorModel.createLatestPackage dto

            match rowStateOf dto with
            | PackageRowState.Unchecked
            | PackageRowState.HasOlderVersion -> setEdits (Map.add dto.Name (Some latest) edits)
            | PackageRowState.Checked -> setEdits (Map.add dto.Name None edits)

        let updateToLatest (dto: ValidationPackageDTO) =
            setEdits (Map.add dto.Name (Some(ValidationPackageSelectorModel.createLatestPackage dto)) edits)

        let removeUnlisted (name: string) =
            setRemovedUnlisted (Set.add name removedUnlisted)

        let unlistedNames =
            React.useMemo (
                (fun () ->
                    Helper.unlistedNames config packages
                    |> Array.filter (fun name -> not (removedUnlisted.Contains name))
                ),
                [| box config; box packages; box removedUnlisted |]
            )

        let dirty = not edits.IsEmpty || not removedUnlisted.IsEmpty

        let submit () =
            if dirty && not submitting then
                setSubmitting true

                let newPackages = Helper.computeNewPackages config packages edits removedUnlisted

                let newConfig =
                    ValidationPackagesConfig.make (ResizeArray newPackages) config.ARCSpecification

                writeConfig newConfig
                |> Promise.map (fun result ->
                    match result with
                    | Ok() ->
                        setEdits Map.empty
                        setRemovedUnlisted Set.empty
                        setSubmitting false
                    | Error ex ->
                        setSubmitting false
                        onError |> Option.iter (fun f -> f ex)
                )
                |> Promise.catch (fun ex ->
                    setSubmitting false
                    onError |> Option.iter (fun f -> f ex)
                )
                |> Promise.start

        let ctxValue =
            React.useMemo (
                (fun () -> {
                    FetchState = state
                    Packages = packages
                    RowStateOf = rowStateOf
                    Toggle = toggle
                    UpdateToLatest = updateToLatest
                    UnlistedNames = unlistedNames
                    RemoveUnlisted = removeUnlisted
                    Dirty = dirty
                    Submitting = submitting
                    Submit = submit
                }),
                [|
                    box state
                    box packages
                    box edits
                    box removedUnlisted
                    box submitting
                    box config
                |]
            )

        ValidationPackageSelectorCtx.Provider(
            ctxValue,
            React.Fragment [
                Html.div [
                    prop.className "swt:flex swt:flex-col swt:gap-2 swt:p-2 swt:min-h-0"
                    prop.children [
                        Html.h2 [
                            prop.className "swt:text-2xl swt:font-bold"
                            prop.text "Validation Packages"
                        ]
                        match state with
                        | SelectorState.Loaded _ -> ValidationPackageSelector.UnlistedBanner()
                        | _ -> Html.none
                        SearchField.SearchField(fun searchFiltered ->
                            PackageTable.PackageTable(
                                searchFiltered,
                                renderBody =
                                    (fun filtered ->
                                        PackagePagination.PackagePagination(
                                            filtered,
                                            renderPage =
                                                (fun pageItems ->
                                                    React.Fragment [
                                                        for pkg in pageItems do
                                                            PackageRow.PackageRow(pkg, key = pkg.Name)
                                                    ]
                                                )
                                        )
                                    )
                            )
                        )
                        ValidationPackageSelector.SubmitBar()
                    ]
                ]
            ]
        )
