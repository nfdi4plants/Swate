module SidebarComponents.Navbar

open Model
open Messages

open Feliz
open Feliz.DaisyUI

open Components
open ARCtrl
open Swate.Components.Shared
open Components.Metadata
open Swate.Components

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

    static member init() = { Loading = true; Metadata = None }

let AddMetaDataButtons refresh (dispatch: Messages.Msg -> unit) =
    let createMetadata (metadata: ArcFiles) =
        promise {
            let! msgs = OfficeInterop.Core.Main.updateTopLevelMetadata metadata
            GenericInteropLogs(Elmish.Cmd.none, msgs) |> DevMsg |> dispatch
            do! refresh ()
        }
        |> Promise.start

    Html.div [
        prop.className "swt:flex swt:flex-col swt:gap-4"
        prop.children [
            Html.button [
                prop.className "swt:btn"
                prop.text "Investigation"
                prop.onClick (fun _ ->
                    let investigation = ArcInvestigation.init ("New Investigation")
                    let arcfile = ArcFiles.Investigation investigation
                    createMetadata arcfile
                )
            ]
            Html.button [
                prop.className "swt:btn"
                prop.text "Study"
                prop.onClick (fun _ ->
                    let study = ArcStudy.init ("New Study")
                    let arcfile = ArcFiles.Study(study, [])
                    createMetadata arcfile
                )
            ]
            Html.button [
                prop.className "swt:btn"
                prop.text "Assay"
                prop.onClick (fun _ ->
                    let assay = ArcAssay.init ("New Assay")
                    let arcfile = ArcFiles.Assay assay
                    createMetadata arcfile
                )
            ]
            Html.button [
                prop.className "swt:btn"
                prop.text "Template"
                prop.onClick (fun _ ->
                    let template = Template.init ("New Template")
                    template.Version <- "0.0.0"
                    template.LastUpdated <- System.DateTime.Now
                    let arcfile = ArcFiles.Template template
                    createMetadata arcfile
                )
            ]
        ]
    ]

let NoMetadataModalContent refresh (dispatch: Messages.Msg -> unit) =
    Components.Forms.Generic.BoxedField(
        content = [
            Html.h2 "Create Top Level Metadata"
            Html.p "Choose one of the following top level meta data types to create"
            AddMetaDataButtons refresh dispatch
        ]
    )

let UpdateMetadataModalContent
    excelMetadataType
    setExcelMetadataType
    closeModal
    model
    (dispatch: Messages.Msg -> unit)
    =
    React.fragment [
        match excelMetadataType with
        | {
              Metadata = Some(ArcFiles.Assay assay)
          } ->
            let setAssay (assay: ArcAssay) =
                setExcelMetadataType {
                    excelMetadataType with
                        Metadata = Some(ArcFiles.Assay assay)
                }

            let setAssayDataMap (assay: ArcAssay) (dataMap: DataMap option) = assay.DataMap <- dataMap
            Assay.Main(assay, setAssay, setAssayDataMap, model)
        | {
              Metadata = Some(ArcFiles.Study(study, assays))
          } ->
            let setStudy (study: ArcStudy, assays: ArcAssay list) =
                setExcelMetadataType {
                    excelMetadataType with
                        Metadata = Some(ArcFiles.Study(study, assays))
                }

            let setStudyDataMap (study: ArcStudy) (dataMap: DataMap option) = study.DataMap <- dataMap
            Study.Main(study, assays, setStudy, setStudyDataMap, model)
        | {
              Metadata = Some(ArcFiles.Investigation investigation)
          } ->
            let setInvestigation (investigation: ArcInvestigation) =
                setExcelMetadataType {
                    excelMetadataType with
                        Metadata = Some(ArcFiles.Investigation investigation)
                }

            Investigation.Main(investigation, setInvestigation, model)
        | {
              Metadata = Some(ArcFiles.Template template)
          } ->
            let setTemplate (template: Template) =
                setExcelMetadataType {
                    excelMetadataType with
                        Metadata = Some(ArcFiles.Template template)
                }

            Template.Main(template, setTemplate)
        | _ -> Html.none
        Components.Forms.Generic.Section [
            Components.Forms.Generic.BoxedField(
                content = [
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:md:flex-row swt:gap-4"
                        prop.children [
                            //Daisy.button.a [
                            Html.button [
                                prop.className "swt:btn swt:btn-primary"
                                prop.text "Update Metadata Type"
                                prop.onClick (fun _ ->
                                    if excelMetadataType.Metadata.IsSome then
                                        OfficeInterop.UpdateTopLevelMetadata(excelMetadataType.Metadata.Value)
                                        |> OfficeInteropMsg
                                        |> dispatch

                                        closeModal ()
                                    else
                                        console.warn ("Tried updating metadata sheet without given metadata")
                                )
                            ]
                            //Daisy.button.a [
                            Html.button [
                                prop.className "swt:btn swt:btn-error"
                                prop.text "Delete Metadata Type"
                                prop.onClick (fun _ ->
                                    OfficeInterop.DeleteTopLevelMetadata |> OfficeInteropMsg |> dispatch
                                    closeModal ()
                                )
                            ]
                        ]
                    ]
                ]
            )
        ]
    ]

// Define a modal dialog component
[<ReactComponent>]
let SelectModalDialog (closeModal: unit -> unit) model (dispatch: Messages.Msg -> unit) =
    let (excelMetadataType, setExcelMetadataType) =
        React.useState (ExcelMetadataState.init)

    let refreshMetadataState =
        fun () -> promise {
            setExcelMetadataType (ExcelMetadataState.init ())
            let! result = OfficeInterop.Core.Main.tryParseToArcFile (getTables = false)

            match result with
            | Result.Ok arcFile ->
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

    React.useLayoutEffectOnce (refreshMetadataState >> Promise.start)

    //Daisy.modal.div [
    Html.div [
        // Add the "is-active" class to display the modal
        prop.className "swt:modal swt:modal-open"
        prop.children [
            //Daisy.modalBackdrop [ prop.onClick (fun _ -> closeModal ()) ]
            Html.div [ prop.className "swt:modal-backdrop"; prop.onClick (fun _ -> closeModal ()) ]
            //Daisy.modalBox.div [
            Html.div [
                prop.className "swt:modal-box swt:overflow-y-auto swt:h-[100%]"
                prop.children [
                    match excelMetadataType with
                    | { Loading = true } -> Modals.Loading.Component
                    | { Metadata = None } -> NoMetadataModalContent refreshMetadataState dispatch
                    | { Metadata = Some metadata } ->
                        UpdateMetadataModalContent excelMetadataType setExcelMetadataType closeModal model dispatch
                ]
            ]
        ]
    ]

let private QuickAccessList toggleMetdadataModal model (dispatch: Messages.Msg -> unit) =
    [
        QuickAccessButton.QuickAccessButton(
            "Create Metadata",
            React.fragment [ Icons.CreateMetadata() ],
            toggleMetdadataModal
        )

        QuickAccessButton.QuickAccessButton(
            "Create Annotation Table",
            React.fragment [ Icons.CreateAnnotationTable() ],
            (fun e ->
                e.preventDefault ()
                let e = e :?> Browser.Types.MouseEvent
                let ctrl = e.metaKey || e.ctrlKey
                SpreadsheetInterface.CreateAnnotationTable ctrl |> InterfaceMsg |> dispatch
            )
        )
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel ->
            QuickAccessButton.QuickAccessButton(
                "Autoformat Table",
                React.fragment [ Icons.AutoformatTable() ],
                (fun e ->
                    e.preventDefault ()
                    let e = e :?> Browser.Types.MouseEvent
                    let ctrl = not (e.metaKey || e.ctrlKey)
                    OfficeInterop.AutoFitTable ctrl |> OfficeInteropMsg |> dispatch
                )
            )
        | _ -> ()
        QuickAccessButton.QuickAccessButton(
            "Rectify Ontology Terms",
            React.fragment [
                Icons.RectifyOntologyTerms(Html.span model.ExcelState.FillHiddenColsStateStore.toReadableString)
            ],
            (fun _ -> SpreadsheetInterface.RectifyTermColumns |> InterfaceMsg |> dispatch)
        )
        QuickAccessButton.QuickAccessButton(
            "Remove Building Block",
            React.fragment [ Icons.RemoveBuildingBlock() ],
            (fun _ -> SpreadsheetInterface.RemoveBuildingBlock None |> InterfaceMsg |> dispatch)
        )
        QuickAccessButton.QuickAccessButton(
            "Get Building Block Information",
            React.fragment [ Icons.BuildingBlockInformation() ],
            (fun _ ->
                promise {
                    let! ontologyAnnotationRes = OfficeInterop.Core.Main.getCompositeColumnDetails ()

                    match ontologyAnnotationRes with
                    | Result.Error msgs -> GenericInteropLogs(Elmish.Cmd.none, msgs) |> DevMsg |> dispatch
                    | Result.Ok term ->
                        let ontologyAnnotation = OntologyAnnotation.fromDBTerm term

                        Model.ModalState.TableModals.TermDetails ontologyAnnotation
                        |> Model.ModalState.ModalTypes.TableModal
                        |> Some
                        |> Messages.UpdateModal
                        |> dispatch
                }
                |> Promise.start
            )
        )
    ]
    |> React.fragment

[<ReactComponent>]
let NavbarComponent (model: Model) (dispatch: Messages.Msg -> unit) =
    let state, setState = React.useState (NavbarState.init)

    let inline toggleMetdadataModal _ =
        {
            state with
                ExcelMetadataModalActive = not state.ExcelMetadataModalActive
        }
        |> setState

    Components.BaseNavbar.Glow [
        if state.ExcelMetadataModalActive then
            SelectModalDialog toggleMetdadataModal model dispatch
        Components.Logo.Main()
        match model.PersistentStorageState.Host with
        | Some Swatehost.Excel ->
            Html.div [
                prop.className "swt:navbar swt:flex swt:gap-4"
                prop.children [ QuickAccessList toggleMetdadataModal model dispatch ]
            ]

            Html.div [
                prop.className "swt:ml-auto"
                prop.children [ NavbarBurger.Main(model, dispatch) ]
            ]
        | _ ->
            Html.div [
                prop.className "swt:ml-auto"
                prop.children [
                    Components.DeleteButton(
                        className = "swt:btn-sm swt:btn-error",
                        props = [
                            prop.onClick (fun _ ->
                                Messages.PageState.UpdateShowSidebar(not model.PageState.ShowSideBar)
                                |> Messages.PageStateMsg
                                |> dispatch
                            )
                        ]
                    )
                ]
            ]
    ]