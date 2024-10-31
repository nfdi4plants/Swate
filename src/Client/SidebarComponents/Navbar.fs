module SidebarComponents.Navbar

open System

open Model
open Messages

open Feliz
open Feliz.Bulma

open Components.QuickAccessButton
open ARCtrl
open ARCtrl.Spreadsheet
open Shared
open Components.Metadata

type private NavbarState = {
    BurgerActive: bool
    QuickAccessActive: bool
    ExcelMetadataModalActive: bool
} with
    static member init() = {
        BurgerActive = false
        QuickAccessActive = false
        ExcelMetadataModalActive = false
    }

type ExcelMetadataState = {
    Loading: bool
    Metadata: ArcFiles option
} with
    static member init() = {
        Loading = true
        Metadata = None
    }

let AddMetaDataButtons refresh (dispatch: Messages.Msg -> unit) =
    let createMetadata (metadata: ArcFiles) =
        promise {
            let! msgs = OfficeInterop.Core.updateTopLevelMetadata metadata
            GenericInteropLogs (Elmish.Cmd.none, msgs) |> DevMsg |> dispatch
            do! refresh()
        }
        |> Promise.start
    Html.div [
        prop.className "flex flex-col gap-4"
        prop.children [
            Bulma.button.a [
                prop.onClick(fun _ ->
                    let investigation = ArcInvestigation.init("New Investigation")
                    let arcfile = ArcFiles.Investigation investigation
                    createMetadata arcfile
                )
                prop.text "Investigation"
            ]
            Bulma.button.a [
                prop.onClick(fun _ ->
                    let study = ArcStudy.init("New Study")
                    let arcfile = ArcFiles.Study (study, [])
                    createMetadata arcfile
                )
                prop.text "Study"
            ]
            Bulma.button.a [
                prop.onClick(fun _ ->
                    let assay = ArcAssay.init("New Assay")
                    let arcfile = ArcFiles.Assay assay
                    createMetadata arcfile
                )
                prop.text "Assay"
            ]
            Bulma.button.a [
                prop.onClick(fun _ ->
                    let template = Template.init("New Template")
                    template.Version <- "0.0.0"
                    template.LastUpdated <- System.DateTime.Now
                    let arcfile = ArcFiles.Template template
                    createMetadata arcfile
                )
                prop.text "Template"
            ]
        ]
    ]

let NoMetadataModalContent refresh (dispatch: Messages.Msg -> unit) =
    Bulma.section [
        Components.Generic.BoxedField None None [
            Bulma.title.h2 "Create Top Level Metadata"
            Html.p "Choose one of the following top level meta data types to create"
            AddMetaDataButtons refresh dispatch
        ]
    ]

let UpdateMetadataModalContent excelMetadataType setExcelMetadataType closeModal (dispatch: Messages.Msg -> unit) =
     Bulma.box [
        Bulma.color.hasBackgroundGreyLighter
        prop.children [
            match excelMetadataType with
            | { Metadata = Some (ArcFiles.Assay assay)} ->
                let setAssay (assay: ArcAssay) =
                    setExcelMetadataType {
                        excelMetadataType with
                            Metadata = Some (ArcFiles.Assay assay)
                    }
                let setAssayDataMap (assay: ArcAssay) (dataMap: DataMap option) =
                    assay.DataMap <- dataMap
                Assay.Main(assay, setAssay, setAssayDataMap)
            | { Metadata = Some (ArcFiles.Study (study, assays))} ->
                let setStudy (study: ArcStudy, assays: ArcAssay list) =
                    setExcelMetadataType {
                        excelMetadataType with
                            Metadata = Some (ArcFiles.Study (study, assays))
                    }
                let setStudyDataMap (study: ArcStudy) (dataMap: DataMap option) =
                    study.DataMap <- dataMap
                Study.Main(study, assays, setStudy, setStudyDataMap)
            | { Metadata = Some (ArcFiles.Investigation investigation)} ->
                let setInvestigation (investigation: ArcInvestigation) =
                    setExcelMetadataType {
                        excelMetadataType with
                            Metadata = Some (ArcFiles.Investigation investigation)
                    }
                Investigation.Main(investigation, setInvestigation)
            | { Metadata = Some (ArcFiles.Template template)} ->
                let setTemplate (template: Template) =
                    setExcelMetadataType {
                        excelMetadataType with
                            Metadata = Some (ArcFiles.Template template)
                    }
                Template.Main(template, setTemplate)
            | _ -> Html.none
            Bulma.section [
                prop.className "pt-0"
                prop.children [
                    Components.Generic.BoxedField None None [
                        Bulma.buttons [
                            Bulma.button.a [
                                Bulma.color.isPrimary
                                prop.style [
                                    style.width 250
                                ]
                                prop.text "Update Metadata Type"
                                prop.onClick (fun _ ->
                                    if excelMetadataType.Metadata.IsSome then
                                        OfficeInterop.UpdateTopLevelMetadata(excelMetadataType.Metadata.Value)
                                        |> OfficeInteropMsg
                                        |> dispatch
                                        closeModal()
                                    else
                                        logw ("Tried updating metadata sheet without given metadata")
                                )
                            ]
                            Bulma.button.a [
                                Bulma.color.isDanger
                                prop.style [
                                    style.width 250
                                ]
                                prop.text "Delete Metadata Type"
                                prop.onClick (fun _ ->
                                    OfficeInterop.DeleteTopLevelMetadata
                                    |> OfficeInteropMsg
                                    |> dispatch
                                    closeModal()
                                )
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// Define a modal dialog component
[<ReactComponent>]
let SelectModalDialog (closeModal: unit -> unit) (dispatch: Messages.Msg -> unit) =
    let (excelMetadataType, setExcelMetadataType) = React.useState(ExcelMetadataState.init)
    let refreshMetadataState =
        fun () ->
            promise {
                setExcelMetadataType (ExcelMetadataState.init())
                let! result = OfficeInterop.Core.OfficeInterop.tryParseToArcFile(getTables=false)
                match result with
                | Result.Ok (arcFile) ->
                    setExcelMetadataType {
                        excelMetadataType with
                            Loading = false
                            Metadata = Some arcFile
                    }
                | Result.Error _ ->
                    setExcelMetadataType {
                        excelMetadataType with
                            Loading = false
                    }
            }
    React.useEffectOnce(refreshMetadataState >> Promise.start)
    Bulma.modal [
        // Add the "is-active" class to display the modal
        Bulma.modal.isActive
        prop.children [
            Bulma.modalBackground [
                prop.onClick (fun _ -> closeModal())
            ]
            Bulma.modalContent [
                prop.className "overflow-y-auto"
                prop.onClick (fun ev -> ev.stopPropagation())
                prop.children [
                    match excelMetadataType with
                    | { Loading = true } ->
                        Modals.Loading.loadingComponent
                    | { Metadata = None } ->
                        NoMetadataModalContent refreshMetadataState dispatch
                    | { Metadata = Some metadata } ->
                        UpdateMetadataModalContent excelMetadataType setExcelMetadataType closeModal dispatch
                ]
            ]
            // Close button in the top-right corner
            Bulma.modalClose [
                prop.onClick (fun _ ->
                    closeModal())
            ]
        ]
    ]

let private ShortCutIconList toggleMetdadataModal model (dispatch: Messages.Msg -> unit) =
    [
        QuickAccessButton.create(
            "Create Metadata",
            [
                Html.i [prop.className "fa-solid fa-plus"]
                Html.i [prop.className "fa-solid fa-info"]
            ],
            toggleMetdadataModal
        )

        QuickAccessButton.create(
            "Create Annotation Table",
            [
                Html.i [prop.className "fa-solid fa-plus"]
                Html.i [prop.className "fa-solid fa-table"]
            ],
            (fun e ->
                e.preventDefault()
                let ctrl = e.metaKey || e.ctrlKey
                SpreadsheetInterface.CreateAnnotationTable ctrl |> InterfaceMsg |> dispatch
            )
        )
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel ->
            QuickAccessButton.create(
                "Autoformat Table",
                [
                    Html.i [prop.className "fa-solid fa-rotate"]
                ],
                (fun e ->
                    e.preventDefault()
                    let ctrl = not (e.metaKey || e.ctrlKey)
                    OfficeInterop.AutoFitTable ctrl |> OfficeInteropMsg |> dispatch
                )
            )
        | _ ->
            ()
        QuickAccessButton.create(
            "Validate / Update Ontology Terms",
            [
                Html.i [prop.className "fa-solid fa-spell-check"]
                Html.span model.ExcelState.FillHiddenColsStateStore.toReadableString
                Html.i [prop.className "fa-solid fa-pen"]
            ],
            (fun _ ->
                SpreadsheetInterface.RectifyTermColumns |> InterfaceMsg |> dispatch
            )
        )
        QuickAccessButton.create(
            "Remove Building Block",
            [
                Html.i [prop.className "fa-solid fa-minus pr-1"]
                Html.i [prop.className "fa-solid fa-table-columns"]
            ],
            (fun _ -> SpreadsheetInterface.RemoveBuildingBlock |> InterfaceMsg |> dispatch)
        )
        QuickAccessButton.create(
            "Get Building Block Information",
            [
                Html.i [prop.className "fa-solid fa-question pr-1"]
                Html.span model.BuildingBlockDetailsState.CurrentRequestState.toStringMsg
                Html.i [prop.className "fa-solid fa-table-columns"]
            ],
            (fun _ -> SpreadsheetInterface.EditBuildingBlock |> InterfaceMsg |> dispatch)
        )
    ]
    |> List.map (fun x -> x.toReactElement())
    |> React.fragment


let private quickAccessDropdownElement model dispatch (state: NavbarState) (setState: NavbarState -> unit) (isSndNavbar:bool) =
    Bulma.navbarItem.div [
        prop.onClick (fun _ -> setState { state with QuickAccessActive = not state.QuickAccessActive })
        prop.style [ style.padding 0; if isSndNavbar then style.custom("marginLeft", "auto")]
        prop.title (if state.QuickAccessActive then "Close quick access" else "Open quick access")
        prop.children [
            Html.div [
                prop.style [style.width(length.perc 100); style.height (length.perc 100); style.position.relative]
                prop.children [
                    Bulma.button.a [
                        prop.style [style.backgroundColor "transparent"; style.height(length.perc 100); if state.QuickAccessActive then style.color NFDIColors.Yellow.Base]
                        Bulma.color.isWhite
                        Bulma.button.isInverted
                        prop.children [
                            Html.div [
                                prop.style [ style.display.inlineFlex; style.position.relative; style.justifyContent.center]
                                prop.children [
                                    Html.i [
                                        prop.style [
                                            style.position.absolute
                                            style.display.block
                                            style.custom("transition", "opacity 0.25s, transform 0.25s")
                                            style.opacity (if state.QuickAccessActive then 1 else 0)
                                            style.transform (if state.QuickAccessActive then [transform.rotate -180] else [transform.rotate 0])
                                        ]
                                        prop.className "fa-solid fa-times"
                                    ]
                                    Html.i [
                                        prop.style [
                                            style.position.absolute
                                            style.display.block
                                            style.custom("transition","opacity 0.25s, transform 0.25s")
                                            style.opacity (if state.QuickAccessActive then 0 else 1)
                                        ]
                                        prop.className "fa-solid fa-ellipsis"
                                    ]
                                    // Invis placeholder to create correct space (Height, width, margin, padding, etc.)
                                    Html.i [
                                        prop.style [
                                            style.display.block
                                            style.opacity 0
                                        ]
                                        prop.className "fa-solid fa-ellipsis"
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let private QuickAccessListElement toggleMetdadataModal model dispatch =
    Html.div [
        prop.style [style.display.flex; style.flexDirection.row]
        prop.children (ShortCutIconList toggleMetdadataModal model dispatch)
    ]

[<ReactComponent>]
let NavbarComponent (model : Model) (dispatch : Messages.Msg -> unit) (sidebarsize: Model.WindowSize) =
    let state, setState = React.useState(NavbarState.init)
    let inline toggleMetdadataModal _ = { state with ExcelMetadataModalActive = not state.ExcelMetadataModalActive } |> setState
    Bulma.navbar [
        prop.className "myNavbarSticky"
        prop.id "swate-mainNavbar"; prop.role "navigation"; prop.ariaLabel "main navigation" ;
        prop.style [style.flexWrap.wrap]
        prop.children [
            if state.ExcelMetadataModalActive then
                SelectModalDialog
                    toggleMetdadataModal
                    dispatch
            Html.div [
                prop.style [style.flexBasis (length.percent 100)]
                prop.children [
                    Bulma.navbarBrand.div [
                        prop.style [style.width(length.perc 100)]
                        prop.children [
                            // Logo
                            Bulma.navbarItem.div [
                                prop.id "logo"
                                prop.onClick (fun _ -> Routing.Route.BuildingBlock |> Some |> UpdatePageState |> dispatch)
                                prop.style [style.width 100; style.cursor.pointer; style.padding (0,length.rem 0.4)]
                                let path = if model.PageState.IsExpert then "_e" else ""
                                Bulma.image [
                                    Html.img [
                                        prop.style [style.maxHeight(length.perc 100); style.width 100]
                                        prop.src @$"assets\Swate_logo_for_excel{path}.svg"
                                    ]
                                ]
                                |> prop.children
                            ]

                            // Quick access buttons
                            match sidebarsize, model.PersistentStorageState.Host with
                            | WindowSize.Mini, Some Swatehost.Excel ->
                                quickAccessDropdownElement model dispatch state setState false
                            | _, Some Swatehost.Excel ->
                                QuickAccessListElement toggleMetdadataModal model dispatch
                            | _,_ -> Html.none

                            Bulma.navbarBurger [
                                if state.BurgerActive then Bulma.navbarBurger.isActive
                                prop.onClick (fun _ -> setState { state with BurgerActive = not state.BurgerActive })
                                Bulma.color.hasTextWhite
                                prop.role "button"
                                prop.ariaLabel "menu"
                                prop.ariaExpanded false
                                prop.style [style.display.block]
                                prop.children [
                                    Html.span [prop.ariaHidden true]
                                    Html.span [prop.ariaHidden true]
                                    Html.span [prop.ariaHidden true]
                                    Html.span [prop.ariaHidden true]
                                ]
                            ]
                        ]
                    ]
                    Bulma.navbarMenu [
                        prop.style [if state.BurgerActive then style.display.block]
                        prop.id "navbarMenu"
                        prop.className (if state.BurgerActive then "navbar-menu is-active" else "navbar-menu")
                        Bulma.navbarDropdown.div [
                            prop.style [if state.BurgerActive then style.display.block]
                            prop.children [
                                Bulma.navbarItem.a [
                                    prop.href Shared.URLs.NFDITwitterUrl ;
                                    prop.target "_Blank";
                                    prop.children [
                                        Html.span "News "
                                        Html.i [prop.className "fa-brands fa-twitter"; prop.style [style.color "#1DA1F2"; style.marginLeft 2]]
                                    ]
                                ]
                                Bulma.navbarItem.a [
                                    prop.onClick (fun _ ->
                                        setState { state with BurgerActive = not state.BurgerActive }
                                        UpdatePageState (Some Routing.Route.Info) |> dispatch
                                    )
                                    prop.text Routing.Route.Info.toStringRdbl
                                ]
                                Bulma.navbarItem.a [
                                    prop.onClick (fun _ ->
                                        setState { state with BurgerActive = not state.BurgerActive }
                                        UpdatePageState (Some Routing.Route.PrivacyPolicy) |> dispatch
                                    )
                                    prop.text Routing.Route.PrivacyPolicy.toStringRdbl
                                ]
                                Bulma.navbarItem.a [
                                    prop.href Shared.URLs.SwateWiki ;
                                    prop.target "_Blank";
                                    prop.text "How to use"
                                ]
                                Bulma.navbarItem.a [
                                    prop.href Shared.URLs.Helpdesk.Url;
                                    prop.target "_Blank";
                                    prop.text "Contact us!"
                                ]
                                Bulma.navbarItem.a [
                                    prop.onClick (fun _ ->
                                        setState {state with BurgerActive = not state.BurgerActive}
                                        UpdatePageState (Some Routing.Route.Settings) |> dispatch
                                    )
                                    prop.text "Settings"
                                ]
                                Bulma.navbarItem.a [
                                    prop.onClick (fun e ->
                                        setState { state with BurgerActive = not state.BurgerActive }
                                        UpdatePageState (Some Routing.Route.ActivityLog) |> dispatch
                                    )
                                    prop.text "Activity Log"
                                ]
                            ]
                        ]
                        |> prop.children
                    ]
                ]
            ]
            if state.QuickAccessActive && sidebarsize = WindowSize.Mini then
                Bulma.navbarBrand.div [
                    prop.style [style.flexGrow 1; style.display.flex]
                    ShortCutIconList toggleMetdadataModal model dispatch |> prop.children
                ]
        ]
    ]
