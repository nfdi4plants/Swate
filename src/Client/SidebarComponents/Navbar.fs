module SidebarComponents.Navbar

open System

open Model
open Messages

open Feliz
open Feliz.Bulma

open Components.QuickAccessButton
open Browser.Types
open ARCtrl
open ARCtrl.Spreadsheet
open Shared
open Components.Metadata

open ExcelJS.Fable
open GlobalBindings

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
    TopLevelMetadata: ArcFiles option
    WorkSheetName: string option
} with
    static member init = {
        TopLevelMetadata = None
        WorkSheetName = None
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
                            WorkSheetName = Some ArcInvestigation.metadataSheetName
                            TopLevelMetadata = Some (ArcFiles.Investigation investigation)
                    }
                    OfficeInterop.CreateTopLevelMetadata(ArcInvestigation.metadataSheetName)
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
                            WorkSheetName = Some ArcStudy.metadataSheetName
                            TopLevelMetadata = Some (ArcFiles.Study (study, []))
                    }
                    OfficeInterop.CreateTopLevelMetadata(ArcStudy.metadataSheetName)                            
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
                            WorkSheetName = Some ArcAssay.metadataSheetName
                            TopLevelMetadata = Some (ArcFiles.Assay assay)
                    }
                    OfficeInterop.CreateTopLevelMetadata(ArcAssay.metadataSheetName)
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
                            WorkSheetName = Some Template.metaDataSheetName
                            TopLevelMetadata = Some (ArcFiles.Template template)
                    }
                    OfficeInterop.CreateTopLevelMetadata(Template.metaDataSheetName)                            
                    |> OfficeInteropMsg
                    |> dispatch
                )
                prop.text "Template"
            ]
        ]
    ]

[<ReactComponent>]
let private CreateMetadataDialog excelMetadataType setExcelMetadataType (ref: IRefValue<HTMLInputElement option>) (closeModal: unit -> unit) (dispatch: Messages.Msg -> unit) =
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

// Define a modal dialog component
let selectModalDialog (isActive: bool) excelMetadataType setExcelMetadataType (closeModal: unit -> unit) (dispatch: Messages.Msg -> unit) =
    let ref = React.useInputRef()
    Bulma.modal [
        if isActive then
            // Add the "is-active" class to display the modal
            Bulma.modal.isActive
            if excelMetadataType.TopLevelMetadata.IsNone then
                prop.children [
                    CreateMetadataDialog excelMetadataType setExcelMetadataType ref closeModal dispatch
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
                                    if excelMetadataType.TopLevelMetadata.IsSome then
                                        match excelMetadataType.TopLevelMetadata.Value with
                                        | ArcFiles.Assay assay ->
                                            let setAssay (assay: ArcAssay) =
                                                setExcelMetadataType {
                                                    excelMetadataType with
                                                        WorkSheetName = Some ArcAssay.metadataSheetName
                                                        TopLevelMetadata = Some (ArcFiles.Assay assay)
                                                }
                                            let setAssayDataMap (assay: ArcAssay) (dataMap: DataMap option) =
                                                assay.DataMap <- dataMap
                                            Assay.Main(assay, setAssay, setAssayDataMap)
                                        | ArcFiles.Study (study, assays) ->
                                            let setStudy (study: ArcStudy, assays: ArcAssay list) =
                                                setExcelMetadataType {
                                                    excelMetadataType with
                                                        WorkSheetName = Some ArcStudy.metadataSheetName
                                                        TopLevelMetadata = Some (ArcFiles.Study (study, assays))
                                                }
                                            let setStudyDataMap (study: ArcStudy) (dataMap: DataMap option) =
                                                study.DataMap <- dataMap
                                            Study.Main(study, assays, setStudy, setStudyDataMap)
                                        | ArcFiles.Investigation investigation ->
                                            let setInvestigation (investigation: ArcInvestigation) =
                                                setExcelMetadataType {
                                                    excelMetadataType with
                                                        WorkSheetName = Some ArcInvestigation.metadataSheetName
                                                        TopLevelMetadata = Some (ArcFiles.Investigation investigation)
                                                }
                                            Investigation.Main(investigation, setInvestigation)
                                        | ArcFiles.Template template ->
                                            let setTemplate (template: Template) =
                                                setExcelMetadataType {
                                                    excelMetadataType with
                                                        WorkSheetName = Some Template.metaDataSheetName
                                                        TopLevelMetadata = Some (ArcFiles.Template template)
                                                }
                                            Template.Main(template, setTemplate)
                                    else Html.none
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
                                                            OfficeInterop.UpdateTopLevelMetadata(excelMetadataType.TopLevelMetadata.Value)
                                                            |> OfficeInteropMsg
                                                            |> dispatch
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
                                                            OfficeInterop.DeleteTopLevelMetadata excelMetadataType.WorkSheetName
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
                        (fun () -> setModalActive(
                            if
                                excelMetadataType.WorkSheetName.IsNone then NavbarState.init
                            else
                                { isModalActive with SwateExcelHandleMetadataModal = not isModalActive.SwateExcelHandleMetadataModal }))
                        dispatch
                ]
            ],
            (fun _ ->
                setModalActive { isModalActive with SwateExcelHandleMetadataModal = not isModalActive.SwateExcelHandleMetadataModal }
                Excel.run(fun context ->
                    promise {                     
                        let! result = OfficeInterop.Core.tryGetTopLevelMetadataSheetName context
                        if result.IsSome then
                            match result.Value with
                            | assayIdentifier when assayIdentifier.ToLower().Contains("assay") ->
                                let! assay = OfficeInterop.Core.tryGetTopLeveMetadata (assayIdentifier.ToLower()) ArcAssay.fromMetadataCollection
                                setExcelMetadataType {
                                    excelMetadataType with
                                        TopLevelMetadata = if assay.IsSome then Some (ArcFiles.Assay assay.Value) else Some (ArcFiles.Assay (new ArcAssay("New Assay")))
                                        WorkSheetName = Some assayIdentifier
                                }
                            | investigationIdentifier when investigationIdentifier.ToLower().Contains("investigation") ->
                                let! investigation = OfficeInterop.Core.tryGetTopLeveMetadata (investigationIdentifier.ToLower()) ArcInvestigation.fromMetadataCollection
                                setExcelMetadataType {
                                    excelMetadataType with
                                        TopLevelMetadata = if investigation.IsSome then Some (ArcFiles.Investigation investigation.Value) else Some (ArcFiles.Investigation (new ArcInvestigation("New Investigation")))
                                        WorkSheetName = Some investigationIdentifier
                                }
                            | studyIdentifier when studyIdentifier.ToLower().Contains("study") ->
                                let! study = OfficeInterop.Core.tryGetTopLeveMetadata (studyIdentifier.ToLower()) ArcStudy.fromMetadataCollection
                                setExcelMetadataType {
                                    excelMetadataType with
                                        TopLevelMetadata = if study.IsSome then Some (ArcFiles.Study study.Value) else Some (ArcFiles.Study (new ArcStudy("New Study"), []))
                                        WorkSheetName = Some studyIdentifier
                                }
                            | templateIdentifier when templateIdentifier.ToLower().Contains("template") ->
                                let! topLevelTemplateInfo = OfficeInterop.Core.tryGetTopLeveMetadata (templateIdentifier.ToLower()) Template.fromMetadataCollection
                                let template =
                                    if topLevelTemplateInfo.IsSome then 
                                        let templateInfo, ers, tags, authors = topLevelTemplateInfo.Value                                
                                        Some (ArcFiles.Template (Template.fromParts templateInfo ers tags authors (ArcTable.init "New Template") DateTime.Now))
                                    else Some (ArcFiles.Template (new Template(Guid.NewGuid(), (ArcTable.init "New Template"))))
                                setExcelMetadataType {
                                    excelMetadataType with
                                        TopLevelMetadata = template
                                        WorkSheetName = Some templateIdentifier
                                }
                            | _ -> failwith $"No metadata of type {result.Value} has been implemented yet!"
                        else setExcelMetadataType(ExcelMetadataState.init)
                    }
                )  |> ignore
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
