module SidebarComponents.Navbar

open System
open System.Collections.Generic

open Model
open Messages

open Feliz
open Feliz.Bulma

open Components.QuickAccessButton
open Browser.Types
open ARCtrl
open Shared
open Components.Metadata

type private NavbarState = {
    BurgerActive: bool
    QuickAccessActive: bool
    SwateExcelHandleMetadataModal: bool
} with
    static member init = {
        BurgerActive = false
        QuickAccessActive = false
        SwateExcelHandleMetadataModal = false
    }

type ExcelMetadataState = {
    MetadataType: ArcFilesDiscriminate option
    Identifier: string option
    Assay: ArcAssay option
    Investigation: ArcInvestigation option
    Study: (ArcStudy * ArcAssay list) option
    Template: Template option
} with
    static member init = {
        MetadataType = None
        Identifier = None
        Assay = None
        Investigation = None
        Study = None
        Template = None
    }

let createMetaDataTypeButtons excelMetadataType setExcelMetadataType (dispatch: Messages.Msg -> unit) =
    Html.div [
        prop.style [
            style.display.flex
            style.flexDirection.column  // Stack buttons vertically
            style.gap 20                // Optional: add space between buttons
        ]
        prop.children [
            Bulma.button.a [
                prop.onClick(fun _ ->
                    let investigation = ArcInvestigation.init("New Investigation")
                    setExcelMetadataType {
                        excelMetadataType with
                            Identifier = Some investigation.Identifier
                            MetadataType = Some ArcFilesDiscriminate.Investigation
                            Investigation = Some investigation
                    }
                    OfficeInterop.CreateTopLevelMetadata(ArcFilesDiscriminate.Investigation)
                    |> OfficeInteropMsg
                    |> dispatch
                )
                prop.text "Investigation"
            ]
            Bulma.button.a [
                prop.onClick(fun _ ->
                    let study = ArcStudy.init("New Study")
                    let table = study.InitTable("New Study Table")
                    study.Tables.Add(table)
                    setExcelMetadataType {
                        excelMetadataType with
                            Identifier = Some study.Identifier
                            MetadataType = Some ArcFilesDiscriminate.Study
                            Study = Some (study, [])
                    }
                    OfficeInterop.CreateTopLevelMetadata(ArcFilesDiscriminate.Study)                            
                    |> OfficeInteropMsg
                    |> dispatch
                )
                prop.text "Study"
            ]
            Bulma.button.a [
                prop.onClick(fun _ ->
                    let assay = ArcAssay.init("New Assay")
                    let table = assay.InitTable("New Assay Table")
                    assay.Tables.Add(table)
                    setExcelMetadataType {
                        excelMetadataType with
                            Identifier = Some assay.Identifier
                            MetadataType = Some ArcFilesDiscriminate.Assay
                            Assay = Some assay
                    }
                    OfficeInterop.CreateTopLevelMetadata(ArcFilesDiscriminate.Assay)
                    |> OfficeInteropMsg
                    |> dispatch
                )
                prop.text "Assay"
            ]
            Bulma.button.a [
                prop.onClick(fun _ ->
                    let template = Template.init("New Template")
                    let table = ArcTable.init("New Table")
                    template.Table <- table
                    template.Version <- "0.0.0"
                    template.Id <- System.Guid.NewGuid()
                    template.LastUpdated <- System.DateTime.Now
                    setExcelMetadataType {
                        excelMetadataType with
                            Identifier = Some template.Name
                            MetadataType = Some ArcFilesDiscriminate.Template
                            Template = Some template
                    }
                    OfficeInterop.CreateTopLevelMetadata(ArcFilesDiscriminate.Template)                            
                    |> OfficeInteropMsg
                    |> dispatch
                )
                prop.text "Template"
            ]
        ]
    ]

[<ReactComponent>]
let createMetadataDialog excelMetadataType setExcelMetadataType (ref: IRefValue<HTMLInputElement option>) (closeModal: unit -> unit) (dispatch: Messages.Msg -> unit) =
    Html.div [
        prop.children [            
            // Modal background to close the dialog
            Bulma.modalBackground [
                prop.ref ref
            ]
            // Modal content
            Bulma.modalContent [
                prop.onClick (fun ev -> ev.stopPropagation())
                prop.children [
                    Bulma.box [
                        prop.style [style.height 350]
                        prop.children [
                            Bulma.title.h2 "Create Top Level Metadata"
                            Html.p "Choose one of the following top level meta data types to create"
                            createMetaDataTypeButtons excelMetadataType setExcelMetadataType dispatch
                        ]
                    ]
                ]
            ]
            // Close button in the top-right corner
            Bulma.modalClose [                
                prop.onClick (fun _ ->
                    closeModal())
            ]
        ]
    ]

let updateTopLevelMetadata (metadataType: ExcelMetadataState) dispatch =
    match metadataType.MetadataType with
    | Some ArcFilesDiscriminate.Assay           -> OfficeInterop.UpdateTopLevelAssay(metadataType.Assay)                    |> OfficeInteropMsg |> dispatch
    | Some ArcFilesDiscriminate.Investigation   -> OfficeInterop.UpdateTopLevelInvestigation(metadataType.Investigation)    |> OfficeInteropMsg |> dispatch
    | Some ArcFilesDiscriminate.Study           -> OfficeInterop.UpdateTopLevelStudy(metadataType.Study)                    |> OfficeInteropMsg |> dispatch
    | Some ArcFilesDiscriminate.Template        -> OfficeInterop.UpdateTopLevelTemplate(metadataType.Template)              |> OfficeInteropMsg |> dispatch
    | None                                      -> failwith "No top level metadata type has been selected"

// Define a modal dialog component
let selectModalDialog (isActive: bool) excelMetadataType setExcelMetadataType (closeModal: unit -> unit) (dispatch: Messages.Msg -> unit) =
    let ref = React.useInputRef()
    Bulma.modal [
        if isActive then
            // Add the "is-active" class to display the modal
            Bulma.modal.isActive
            if excelMetadataType.MetadataType.IsNone then
                prop.children [
                    createMetadataDialog excelMetadataType setExcelMetadataType ref closeModal dispatch
                ]
            else
                prop.children [
                    Bulma.modalBackground [
                        prop.ref ref
                    ]
                    Bulma.modalContent [
                        prop.className "overflow-y-auto h-full"
                        prop.onClick (fun ev -> ev.stopPropagation())
                        prop.children [
                            Bulma.box [
                                Bulma.color.hasBackgroundGreyLighter
                                prop.children [
                                    match excelMetadataType.MetadataType with
                                    | Some ArcFilesDiscriminate.Assay ->
                                        let setAssay (assay: ArcAssay) =
                                            setExcelMetadataType {
                                                excelMetadataType with
                                                    Identifier = Some assay.Identifier
                                                    MetadataType = Some ArcFilesDiscriminate.Assay
                                                    Assay = Some assay
                                            }
                                        let setAssayDataMap (assay: ArcAssay) (dataMap: DataMap option) =
                                            assay.DataMap <- dataMap
                                        Assay.Main(excelMetadataType.Assay.Value, setAssay, setAssayDataMap)
                                    | Some ArcFilesDiscriminate.Study ->
                                        let (study, arcAssays) = excelMetadataType.Study.Value
                                        let setStudy (study: ArcStudy, assays: ArcAssay list) =
                                            setExcelMetadataType {
                                                excelMetadataType with
                                                    Identifier = Some study.Identifier
                                                    MetadataType = Some ArcFilesDiscriminate.Study
                                                    Study = Some (study, assays)
                                            }
                                        let setStudyDataMap (study: ArcStudy) (dataMap: DataMap option) =
                                            study.DataMap <- dataMap
                                        Study.Main(study, arcAssays, setStudy, setStudyDataMap)
                                    | Some ArcFilesDiscriminate.Investigation ->
                                        let setInvestigation (investigation: ArcInvestigation) =
                                            setExcelMetadataType {
                                                excelMetadataType with
                                                    Identifier = Some investigation.Identifier
                                                    MetadataType = Some ArcFilesDiscriminate.Investigation
                                                    Investigation = Some investigation
                                            }
                                        Investigation.Main(excelMetadataType.Investigation.Value, setInvestigation)
                                    | Some ArcFilesDiscriminate.Template ->
                                        let setTemplate (template: Template) =
                                            setExcelMetadataType {
                                                excelMetadataType with
                                                    Identifier = Some (template.Name.ToString())
                                                    MetadataType = Some ArcFilesDiscriminate.Template
                                                    Template = Some template
                                            }
                                        Template.Main(excelMetadataType.Template.Value, setTemplate)
                                    | None -> Html.none
                                    Html.div [
                                        prop.style [
                                            style.display.flex
                                            style.justifyContent.center
                                            style.alignItems.center
                                        ]
                                        prop.children [
                                            Bulma.box [
                                                prop.style [
                                                    style.display.flex
                                                    style.justifyContent.center
                                                    style.alignItems.center
                                                    style.flexDirection.column  // Stack buttons vertically
                                                    style.gap 20                // Optional: add space between buttons                                                
                                                    style.width 480                                                
                                                ]
                                                prop.children [
                                                    Bulma.button.a [
                                                        Bulma.color.isPrimary
                                                        prop.style [
                                                            style.width 250
                                                        ]
                                                        prop.text "Update Metadata Type"
                                                        prop.onClick (fun _ ->                                    
                                                            updateTopLevelMetadata excelMetadataType dispatch
                                                            closeModal()
                                                        )
                                                    ]
                                                    Bulma.button.a [
                                                        Bulma.color.isDanger
                                                        prop.style [
                                                            style.width 250
                                                        ]
                                                        prop.text "Delete Metadata Type"
                                                        prop.onClick (fun _ ->
                                                            OfficeInterop.DeleteTopLevelMetadata excelMetadataType.Identifier
                                                            |> OfficeInteropMsg
                                                            |> dispatch
                                                            setExcelMetadataType(ExcelMetadataState.init)
                                                            closeModal()
                                                        )
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    // Close button in the top-right corner
                    Bulma.modalClose [
                        prop.onClick (fun _ ->
                            closeModal())
                    ]
                ]
    ]

let private shortCutIconList model (dispatch: Messages.Msg -> unit) =
    [
        let (isModalActive, setModalActive) = React.useState(NavbarState.init)
        let (excelMetadataType, setExcelMetadataType) = React.useState(ExcelMetadataState.init)
        QuickAccessButton.create(
            "Create Metadata",
            [
                Html.i [prop.className "fa-solid fa-plus"]
                Html.i [prop.className "fa-solid fa-info"]
                Html.div [
                    selectModalDialog
                        isModalActive.SwateExcelHandleMetadataModal
                        excelMetadataType
                        setExcelMetadataType
                        (fun () -> setModalActive(if excelMetadataType.Identifier.IsNone then NavbarState.init else { isModalActive with SwateExcelHandleMetadataModal = not isModalActive.SwateExcelHandleMetadataModal }))
                        dispatch
                ]
            ],
            (fun _ ->
                setModalActive { isModalActive with SwateExcelHandleMetadataModal = not isModalActive.SwateExcelHandleMetadataModal }
                promise {
                    let! result = OfficeInterop.Core.tryGetTopLevelMetadata()
                    if result.IsSome then
                        match result.Value with
                        | assayIdentifier when assayIdentifier.ToLower().Contains("assay") ->
                            setExcelMetadataType {
                                excelMetadataType with
                                    MetadataType = Some ArcFilesDiscriminate.Assay
                                    Identifier = Some assayIdentifier
                                    Assay = Some (new ArcAssay(assayIdentifier))
                            }
                        | investigationIdentifier when investigationIdentifier.ToLower().Contains("investigation") ->
                            setExcelMetadataType {
                                excelMetadataType with
                                    MetadataType = Some ArcFilesDiscriminate.Investigation
                                    Identifier = Some investigationIdentifier
                                    Investigation = Some (new ArcInvestigation(investigationIdentifier))
                            }
                        | studyIdentifier when studyIdentifier.ToLower().Contains("study") ->
                            setExcelMetadataType {
                                excelMetadataType with
                                    MetadataType = Some ArcFilesDiscriminate.Study
                                    Identifier = Some studyIdentifier
                                    Study = Some (new ArcStudy(studyIdentifier), [])
                            }
                        | templateIdentifier when templateIdentifier.ToLower().Contains("template") ->
                            setExcelMetadataType {
                                excelMetadataType with
                                    MetadataType = Some ArcFilesDiscriminate.Template
                                    Identifier = Some templateIdentifier
                                    Template =
                                        Some (
                                            new Template(Guid.NewGuid(),
                                            new ArcTable("new Table",
                                                new ResizeArray<CompositeHeader>(), new Dictionary<(int * int), CompositeCell>()
                                            ),
                                            templateIdentifier)
                                        )
                            }
                        | _ -> failwith $"No metadata of type {result.Value} has been implemented yet!"
                } |> ignore
            )
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
                //SpreadsheetInterface.ValidateAnnotationTable |> InterfaceMsg |> dispatch
                SpreadsheetInterface.UpdateTermColumns |> InterfaceMsg |> dispatch                
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

let private navbarShortCutIconList model dispatch =
    [
        for icon in shortCutIconList model dispatch do
            yield
                icon.toReactElement()
    ]

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

let private quickAccessListElement model dispatch =
    Html.div [
        prop.style [style.display.flex; style.flexDirection.row]
        prop.children (navbarShortCutIconList model dispatch)
    ]

[<ReactComponent>]
let NavbarComponent (model : Model) (dispatch : Messages.Msg -> unit) (sidebarsize: Model.WindowSize) =
    let state, setState = React.useState(NavbarState.init)
    Bulma.navbar [
        prop.className "myNavbarSticky"
        prop.id "swate-mainNavbar"; prop.role "navigation"; prop.ariaLabel "main navigation" ;
        prop.style [style.flexWrap.wrap]
        prop.children [
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
                                quickAccessListElement model dispatch
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
                    navbarShortCutIconList model dispatch |> prop.children
                ]
        ]
    ]
