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

    let createNextConfig
        (currentConfig: ValidationPackagesConfig)
        (packages: ValidationPackageDTO[])
        (edits: Map<string, option<ValidationPackage>>)
        (removedUnlisted: Set<string>)
        =
        let newPackages =
            Helper.computeNewPackages currentConfig packages edits removedUnlisted

        ValidationPackagesConfig.make (ResizeArray newPackages) currentConfig.ARCSpecification


[<Erase; Mangle(false)>]
type ValidationPackageSelector =

    [<ReactComponent>]
    static member private UnlistedBanner
        (unlistedNames: string[], setRemovedUnlisted: (Set<string> -> Set<string>) -> unit)
        =

        let expanded, setExpanded = React.useState false

        let removeUnlisted (name: string) =
            setRemovedUnlisted (fun set -> Set.add name set)

        if Array.isEmpty unlistedNames then
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
                                            unlistedNames.Length
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
                                        for name in unlistedNames do
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
                                                        prop.onClick (fun _ -> removeUnlisted name)
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
    static member private SubmitBar(isSubmitting: bool, isDirty: bool, submit: unit -> unit) =

        Html.div [
            prop.className "swt:flex swt:justify-end swt:items-center swt:gap-2 swt:p-2 swt:border-t"
            prop.children [
                if isSubmitting then
                    Html.span [
                        prop.key "submitting-spinner"
                        prop.className "swt:loading swt:loading-spinner swt:loading-sm"
                    ]
                Html.button [
                    prop.key "submit-button"
                    prop.type' "button"
                    prop.testId "validation-package-selector-submit"
                    prop.className "swt:btn swt:btn-primary"
                    prop.disabled (not isDirty || isSubmitting)
                    prop.onClick (fun _ -> submit ())
                    prop.text "Submit"
                ]
            ]
        ]

    [<ReactComponent(true)>]
    static member ValidationPackageSelector
        (
            config: ValidationPackagesConfig,
            writeConfig: ValidationPackagesConfig -> JS.Promise<Result<unit, exn>>,
            // https://avpr.nfdi4plants.org/swagger/index.html#/Validation%20Packages/GetAllPackages
            fetchValidationPackages: unit -> JS.Promise<ValidationPackageDTO[]>,
            ?onError: exn -> unit
        ) =
        let state, setState = React.useState (fun () -> SelectorState.Idle)
        let edits, setEdits = React.useState Map.empty<string, ValidationPackage option>

        let removedUnlisted, setRemovedUnlisted =
            React.useStateWithUpdater Set.empty<string>

        let submitting, setSubmitting = React.useState false

        /// This config state is used to compare with the current config state to determine if there are any changes that need to be submitted. It is initialized with the initial config and will only update after the incoming config changes (for example after ``writeConfig``).
        let config_old = React.useMemo ((fun () -> config), [| box config |])

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

        let isDirty =
            React.useMemo (
                (fun () ->

                    let newConfig =
                        ValidationPackageSelectorModel.createNextConfig config packages edits removedUnlisted

                    newConfig <> config_old
                ),
                [| box edits; box removedUnlisted |]
            )

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

        let unlistedNames =
            React.useMemo (
                (fun () ->
                    Helper.unlistedNames config packages
                    |> Array.filter (fun name -> not (removedUnlisted.Contains name))
                ),
                [| box config; box packages; box removedUnlisted |]
            )

        let submit () =
            if isDirty && not submitting then
                setSubmitting true

                let newConfig =
                    ValidationPackageSelectorModel.createNextConfig config packages edits removedUnlisted

                writeConfig newConfig
                |> Promise.map (fun result ->
                    match result with
                    | Ok() ->
                        setEdits Map.empty
                        setRemovedUnlisted (fun _ -> Set.empty)
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
                }),
                [| box state; box packages; box edits; box config |]
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
                        | SelectorState.Loaded _ ->
                            ValidationPackageSelector.UnlistedBanner(unlistedNames, setRemovedUnlisted)
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
                        ValidationPackageSelector.SubmitBar(
                            isSubmitting = submitting,
                            isDirty = isDirty,
                            submit = submit
                        )
                    ]
                ]
            ]
        )
