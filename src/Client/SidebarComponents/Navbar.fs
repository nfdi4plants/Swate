module SidebarComponents.Navbar

open System

open Model
open Messages

open Feliz
open Feliz.DaisyUI

open Components
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
            Daisy.button.a [
                prop.onClick(fun _ ->
                    let investigation = ArcInvestigation.init("New Investigation")
                    let arcfile = ArcFiles.Investigation investigation
                    createMetadata arcfile
                )
                prop.text "Investigation"
            ]
            Daisy.button.a [
                prop.onClick(fun _ ->
                    let study = ArcStudy.init("New Study")
                    let arcfile = ArcFiles.Study (study, [])
                    createMetadata arcfile
                )
                prop.text "Study"
            ]
            Daisy.button.a [
                prop.onClick(fun _ ->
                    let assay = ArcAssay.init("New Assay")
                    let arcfile = ArcFiles.Assay assay
                    createMetadata arcfile
                )
                prop.text "Assay"
            ]
            Daisy.button.a [
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
    Html.section [
        Components.Forms.Generic.BoxedField [
            Html.h2 "Create Top Level Metadata"
            Html.p "Choose one of the following top level meta data types to create"
            AddMetaDataButtons refresh dispatch
        ]
    ]

let UpdateMetadataModalContent excelMetadataType setExcelMetadataType closeModal (dispatch: Messages.Msg -> unit) =
    Html.div [
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
            Html.section [
                prop.className "pt-0"
                prop.children [
                    Components.Forms.Generic.BoxedField [
                        Html.div [
                            Daisy.button.a [
                                button.primary
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
                            Daisy.button.a [
                                button.error
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
                let! result = OfficeInterop.Core.Main.tryParseToArcFile(getTables=false)
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
    Daisy.modal.div [
        // Add the "is-active" class to display the modal
        modal.active
        prop.children [
            Daisy.modalBackdrop [
                prop.onClick (fun _ -> closeModal())
            ]
            Daisy.modalBox.form [
                prop.className "overflow-y-auto"
                prop.children [
                    match excelMetadataType with
                    | { Loading = true } ->
                        Modals.Loading.Modal()
                    | { Metadata = None } ->
                        NoMetadataModalContent refreshMetadataState dispatch
                    | { Metadata = Some metadata } ->
                        UpdateMetadataModalContent excelMetadataType setExcelMetadataType closeModal dispatch
                ]
            ]
        ]
    ]

let private QuickAccessList toggleMetdadataModal model (dispatch: Messages.Msg -> unit) =
    [
        QuickAccessButton.Main(
            "Create Metadata",
            React.fragment [
                Html.i [prop.className "fa-solid fa-plus"]
                Html.i [prop.className "fa-solid fa-info"]
            ],
            toggleMetdadataModal
        )

        QuickAccessButton.Main(
            "Create Annotation Table",
            React.fragment [
                Html.i [prop.className "fa-solid fa-plus"]
                Html.i [prop.className "fa-solid fa-table"]
            ],
            (fun e ->
                e.preventDefault()
                let e = e :?> Browser.Types.MouseEvent
                let ctrl = e.metaKey || e.ctrlKey
                SpreadsheetInterface.CreateAnnotationTable ctrl |> InterfaceMsg |> dispatch
            )
        )
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel ->
            QuickAccessButton.Main(
                "Autoformat Table",
                React.fragment [
                    Html.i [prop.className "fa-solid fa-rotate"]
                ],
                (fun e ->
                    e.preventDefault()
                    let e = e :?> Browser.Types.MouseEvent
                    let ctrl = not (e.metaKey || e.ctrlKey)
                    OfficeInterop.AutoFitTable ctrl |> OfficeInteropMsg |> dispatch
                )
            )
        | _ ->
            ()
        QuickAccessButton.Main(
            "Rectify Ontology Terms",
            React.fragment [
                Html.i [prop.className "fa-solid fa-spell-check"]
                Html.span model.ExcelState.FillHiddenColsStateStore.toReadableString
                Html.i [prop.className "fa-solid fa-pen"]
            ],
            (fun _ ->
                SpreadsheetInterface.RectifyTermColumns |> InterfaceMsg |> dispatch
            )
        )
        QuickAccessButton.Main(
            "Remove Building Block",
            React.fragment [
                Html.i [prop.className "fa-solid fa-minus pr-1"]
                Html.i [prop.className "fa-solid fa-table-columns"]
            ],
            (fun _ -> SpreadsheetInterface.RemoveBuildingBlock |> InterfaceMsg |> dispatch)
        )
        //QuickAccessButton.create(
        //    "Get Building Block Information",
        //    [
        //        Html.i [prop.className "fa-solid fa-question pr-1"]
        //        Html.span model.BuildingBlockDetailsState.CurrentRequestState.toStringMsg
        //        Html.i [prop.className "fa-solid fa-table-columns"]
        //    ],
        //    (fun _ -> SpreadsheetInterface.EditBuildingBlock |> InterfaceMsg |> dispatch)
        //)
    ]
    |> React.fragment

[<ReactComponent>]
let NavbarComponent (model : Model) (dispatch : Messages.Msg -> unit) =
    let state, setState = React.useState(NavbarState.init)
    let inline toggleMetdadataModal _ = { state with ExcelMetadataModalActive = not state.ExcelMetadataModalActive } |> setState
    Components.BaseNavbar.Glow [
        if state.ExcelMetadataModalActive then
            SelectModalDialog
                toggleMetdadataModal
                dispatch
        Html.div [
            prop.ariaLabel "logo"
            prop.children [
                Html.img [
                    prop.style [style.maxHeight(length.perc 100); style.width 100]
                    prop.src @"assets/Swate_logo_for_excel.svg"
                ]
            ]
        ]
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel ->
            Daisy.navbarCenter [
                QuickAccessList toggleMetdadataModal model dispatch
            ]
            Html.div [
                prop.className "ml-auto"
                prop.children [
                    NavbarBurger.Main(model, dispatch)
                ]
            ]
        | _ ->
            Html.div [
                prop.className "ml-auto"
                prop.children [
                    Components.DeleteButton(props = [
                        prop.onClick (fun _ ->
                            Messages.PersistentStorage.UpdateShowSidebar (not model.PersistentStorageState.ShowSideBar)
                            |> Messages.PersistentStorageMsg
                            |> dispatch
                        )
                        button.sm
                        button.glass
                    ])
                ]
            ]
    ]