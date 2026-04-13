namespace Swate.Components.ArcFileEditor.EmptyTableView

open Feliz
open ARCtrl
open Fable.Core

open Swate.Components
open Swate.Components.Shared
open Swate.Components.ArcFileEditor.EmptyTableView.Helper

[<RequireQualifiedAccess>]
type private ModalState =
    | BuildingBlock
    | Templates
    | PreviousTableSelect

[<Erase; Mangle(false)>]
type Main =

    [<ReactComponent>]
    static member EmptyTableView
        (
            arcFile: ArcFiles,
            setArcFile: ArcFiles -> unit,
            activeTableIndex: int option,
            templateServices: TemplateWidgetServices
        ) =
        let modal, setModal = React.useState (None: ModalState option)

        let setIsOpen (modal: ModalState) =
            function
            | true -> setModal (Some modal)
            | false -> setModal None

        let isBuildingBlockOpen = modal = Some ModalState.BuildingBlock
        let isTemplatesOpen = modal = Some ModalState.Templates
        let isPreviousTableSelectOpen = modal = Some ModalState.PreviousTableSelect

        let isValidTableIndexForArcFile =
            Helper.tryGetActiveTable arcFile activeTableIndex |> Option.isSome

        let isDisabled = not isValidTableIndexForArcFile

        let canUsePreviousOutput =
            isValidTableIndexForArcFile
            && (Helper.getOutputTables arcFile |> Array.isEmpty |> not)

        Html.div [
            prop.className "swt:flex swt:h-full swt:min-h-0 swt:flex-col swt:gap-4"
            prop.children [
                Html.div [
                    prop.className "swt:flex swt:flex-1 swt:items-center swt:justify-center swt:overflow-auto"
                    prop.children [
                        CardGrid.CardGrid(
                            React.Fragment [
                                CardGrid.CardGridButton(
                                    Icons.Templates(),
                                    "Start with template!",
                                    "Select a full template as a starting point.",
                                    fun _ -> setModal (Some ModalState.Templates)
                                )
                                CardGrid.CardGridButton(
                                    Icons.BuildingBlock(),
                                    "Start from scratch!",
                                    "Select a building block as a starting point.",
                                    fun _ -> setModal (Some ModalState.BuildingBlock)
                                )
                                CardGrid.CardGridButton(
                                    Icons.BasicTable(),
                                    "Create basic table!",
                                    "Create a table with columns: Input, Protocol, Output.",
                                    (fun _ -> Helper.createMinimalTable arcFile activeTableIndex setArcFile),
                                    (isDisabled)
                                )
                                CardGrid.CardGridButton(
                                    Icons.OutputColumn(),
                                    "Utilize prior output!",
                                    "Select an output column of one table as new input column.",
                                    (fun _ -> setModal (Some ModalState.PreviousTableSelect)),
                                    disabled = (not canUsePreviousOutput)
                                )
                            ]
                        )
                    ]
                ]
                Modals.BuildingBlock(
                    arcFile,
                    activeTableIndex,
                    setArcFile,
                    isBuildingBlockOpen,
                    setIsOpen ModalState.BuildingBlock
                )
                Modals.Templates(
                    arcFile,
                    activeTableIndex,
                    setArcFile,
                    templateServices,
                    isTemplatesOpen,
                    setIsOpen ModalState.Templates
                )
                Modals.PreviousTableSelect(
                    arcFile,
                    activeTableIndex,
                    setArcFile,
                    isPreviousTableSelectOpen,
                    setIsOpen ModalState.PreviousTableSelect
                )
            ]
        ]