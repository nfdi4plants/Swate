namespace Swate.Components.Composite.Widgets.JsonImport

open Swate.Components.Shared
open System
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open ARCtrl
open Swate.Components.Composite.Template
open Swate.Components.Composite.Template.Types
open Swate.Components.Primitive.BaseModal

module private JsonImportHelper =

    type SelectiveImportConfig = {
        ImportType: ARCtrl.TableJoinOptions
        /// List of tables to import, with their index in the original file
        ImportTableIndices: int list
    } with

        static member init() = {
            ImportType = ARCtrl.TableJoinOptions.Headers
            ImportTableIndices = []
        }

[<Erase; Mangle(false)>]
type ImportModal =

    // [<ReactComponent>]
    // static member ImportActionOption(id, label: string, isActive: bool, setTemplateImportAction) =
    //     let radioGroupName = $"json-import-action-{id}"
    //     Html.label [
    //         prop.className "swt:inline-flex swt:items-center swt:gap-2 swt:cursor-pointer"
    //         prop.children [
    //             Html.input [
    //                 prop.type'.radio
    //                 prop.name radioGroupName
    //                 prop.className "swt:radio swt:radio-xs"
    //                 prop.isChecked isActive
    //                 prop.onChange (fun (b: bool) ->
    //                     if b then setTemplateImportAction ()
    //                 )
    //             ]
    //             Html.span [ prop.className "swt:text-xs"; prop.text label ]
    //         ]
    //     ]

    // [<ReactComponent>]
    // static member ImportView(table: ArcTable, importConfig: SelectiveImportConfig, setImportConfig: SelectiveImportConfig -> unit, ?key) =
    //     Html.div [
    //         prop.className
    //             "swt:border swt:border-base-300 swt:rounded-box swt:p-2 swt:flex swt:flex-col swt:gap-2"
    //         prop.children [
    //             Html.div [
    //                 prop.className "swt:text-sm swt:font-medium swt:truncate"
    //                 prop.title table.Name
    //                 prop.text table.Name
    //             ]
    //             Html.div [
    //                 prop.className "swt:flex swt:flex-col swt:gap-1"
    //                 prop.children [
    //                     ImportModal.ImportActionOption(
    //                         table.Name,
    //                         "Import (new table)",
    //                         (importConfig.ImportType = TemplateImportAction.ImportAsNewTable),
    //                         setTemplateImportAction = callbacks.SetTemplateImportAction
    //                     )
    //                     ImportModal.ImportActionOption(
    //                         template.Id,
    //                         TemplateTypes.TemplateImportAction.AppendToActiveTable,
    //                         "Append to active table",
    //                         (importAction = TemplateTypes.TemplateImportAction.AppendToActiveTable),
    //                         setTemplateImportAction = callbacks.SetTemplateImportAction
    //                     )
    //                     ImportModal.ImportActionOption(
    //                         template.Id,
    //                         TemplateTypes.TemplateImportAction.NoImport,
    //                         "No import",
    //                         (importAction = TemplateTypes.TemplateImportAction.NoImport),
    //                         setTemplateImportAction = callbacks.SetTemplateImportAction
    //                     )
    //                 ]
    //             ]
    //             Html.details [
    //                 prop.className "swt:border swt:border-base-300 swt:rounded-box swt:px-2 swt:py-1"
    //                 prop.children [
    //                     Html.summary [
    //                         prop.className "swt:cursor-pointer swt:text-xs swt:font-medium"
    //                         prop.textf "Columns: %d/%d selected" selectedColumnsCount columns.Length
    //                     ]
    //                     Html.div [
    //                         prop.className "swt:flex swt:flex-col swt:gap-2 swt:mt-2"
    //                         prop.children [
    //                             if columns.Length = 0 then
    //                                 Html.div [
    //                                     prop.className "swt:text-xs swt:opacity-70"
    //                                     prop.text "Template has no columns."
    //                                 ]
    //                             else
    //                                 React.Fragment [
    //                                     Html.div [
    //                                         prop.className "swt:flex swt:items-center swt:gap-2"
    //                                         prop.children [
    //                                             Html.button [
    //                                                 prop.className
    //                                                     "swt:btn swt:btn-ghost swt:btn-xs swt:ml-auto"
    //                                                 prop.text "Select all"
    //                                                 prop.disabled (not canEditColumns)
    //                                                 prop.onClick (fun _ ->
    //                                                     callbacks.SelectAllTemplateColumns template.Id
    //                                                 )
    //                                             ]
    //                                             Html.button [
    //                                                 prop.className "swt:btn swt:btn-ghost swt:btn-xs"
    //                                                 prop.text "Unselect all"
    //                                                 prop.disabled (not canEditColumns)
    //                                                 prop.onClick (fun _ ->
    //                                                     callbacks.UnselectAllTemplateColumns template
    //                                                 )
    //                                             ]
    //                                         ]
    //                                     ]
    //                                     Html.div [
    //                                         prop.className "swt:overflow-x-auto"
    //                                         prop.children [
    //                                             Html.table [
    //                                                 prop.className
    //                                                     "swt:table swt:table-xs swt:table-fixed swt:w-max"
    //                                                 prop.children [
    //                                                     Html.tbody [
    //                                                         Html.tr [
    //                                                             prop.children [
    //                                                                 for columnIndex in
    //                                                                     0 .. columns.Length - 1 do
    //                                                                     let header =
    //                                                                         columns.[columnIndex].Header
    //                                                                             .ToString()

    //                                                                     Html.td [
    //                                                                         prop.key
    //                                                                             $"{template.Id}_{columnIndex}_header_card"
    //                                                                         prop.className
    //                                                                             "swt:w-52 swt:min-w-52 swt:px-2 swt:py-1"
    //                                                                         prop.children [
    //                                                                             Html.label [
    //                                                                                 prop.className
    //                                                                                     "swt:flex swt:items-center swt:gap-2 swt:cursor-pointer swt:border swt:border-base-300 swt:rounded-box swt:px-2 swt:py-1"
    //                                                                                 prop.children [
    //                                                                                     Html.input [
    //                                                                                         prop.type'.checkbox
    //                                                                                         prop.className
    //                                                                                             "swt:checkbox swt:checkbox-xs"
    //                                                                                         prop
    //                                                                                             .isChecked (
    //                                                                                                 callbacks.IsColumnSelected
    //                                                                                                     template.Id
    //                                                                                                     columnIndex
    //                                                                                             )
    //                                                                                         prop
    //                                                                                             .disabled (
    //                                                                                                 not
    //                                                                                                     canEditColumns
    //                                                                                             )
    //                                                                                         prop.onChange (fun
    //                                                                                                             (_:
    //                                                                                                                 bool) ->
    //                                                                                             callbacks.ToggleColumnSelection
    //                                                                                                 template.Id
    //                                                                                                 columnIndex
    //                                                                                         )
    //                                                                                     ]
    //                                                                                     Html.span [
    //                                                                                         prop.className
    //                                                                                             "swt:text-xs swt:font-medium swt:truncate"
    //                                                                                         prop.title
    //                                                                                             header
    //                                                                                         prop.text
    //                                                                                             header
    //                                                                                     ]
    //                                                                                 ]
    //                                                                             ]
    //                                                                         ]
    //                                                                     ]
    //                                                             ]
    //                                                         ]
    //                                                         Html.tr [
    //                                                             prop.children [
    //                                                                 for columnIndex in
    //                                                                     0 .. columns.Length - 1 do
    //                                                                     let valuePreviewText =
    //                                                                         TemplateHelper.templateColumnValuePreview
    //                                                                             template.Table
    //                                                                             columnIndex

    //                                                                     Html.td [
    //                                                                         prop.key
    //                                                                             $"{template.Id}_{columnIndex}_values_card"
    //                                                                         prop.className
    //                                                                             "swt:w-52 swt:min-w-52 swt:px-2 swt:pt-0 swt:pb-1"
    //                                                                         prop.children [
    //                                                                             Html.span [
    //                                                                                 prop.className
    //                                                                                     "swt:block swt:text-[10px] swt:opacity-70 swt:truncate"
    //                                                                                 prop.title
    //                                                                                     valuePreviewText
    //                                                                                 prop.text
    //                                                                                     valuePreviewText
    //                                                                             ]
    //                                                                         ]
    //                                                                     ]
    //                                                             ]
    //                                                         ]
    //                                                     ]
    //                                                 ]
    //                                             ]
    //                                         ]
    //                                     ]
    //                                 ]
    //                         ]
    //                     ]
    //                 ]
    //             ]
    //         ]
    //     ]

    [<ReactComponent(true)>]
    static member ImportModal(jsonTranspiledArcFile: ArcFiles option, setArcFile, close) =

        // let importConfig, setImportConfig = React.useState (SelectiveImportConfig.init())

        // let tables = jsonTranspiledArcFile |> Option.map (fun arcFile -> arcFile.Tables())

        // BaseModal.Modal(
        //     isOpen = jsonTranspiledArcFile.IsSome,
        //     setIsOpen = close,
        //     header = Html.text "Import Json",
        //     description = Html.text "Select an import mode before importing the selected JSON file.",
        //     children =
        //         Html.div [
        //             prop.className "swt:flex swt:flex-col swt:gap-2"
        //             prop.children [
        //                 Html.div [
        //                     prop.className "swt:text-xs swt:opacity-70"
        //                     prop.text "Import mode"
        //                 ]
        //                 for importMode, _, label in Helper.TemplateImportMode.options do
        //                     let isChecked = importConfig.ImportType = importMode

        //                     Html.label [
        //                         prop.className "swt:label swt:cursor-pointer swt:justify-start swt:gap-2"
        //                         prop.children [
        //                             Html.input [
        //                                 prop.type'.radio
        //                                 prop.name "template-import-mode"
        //                                 prop.className "swt:radio swt:radio-sm"
        //                                 prop.isChecked isChecked
        //                                 prop.onChange (fun (_: bool) -> setImportConfig { importConfig with ImportType = importMode })
        //                             ]
        //                             Html.span [ prop.className "swt:text-sm"; prop.text label ]
        //                         ]
        //                     ]
        //                 Html.div [ prop.className "swt:divider swt:my-1" ]
        //                 Html.div [
        //                     prop.className "swt:max-h-64 swt:overflow-y-auto"
        //                     prop.children [
        //                         TemplateImportModalPreview.TemplateImportModalPreview(
        //                             selectedTemplates,
        //                             previewCallbacks
        //                         )
        //                     ]
        //                 ]
        //             ]
        //         ],
        //     footer =
        //         Html.div [
        //             prop.className "swt:flex swt:w-full swt:gap-2"
        //             prop.children [
        //                 Html.button [
        //                     prop.className "swt:btn swt:btn-outline"
        //                     prop.text "Cancel"
        //                     prop.onClick (fun _ -> close true)
        //                 ]
        //                 Html.button [
        //                     prop.className "swt:btn swt:btn-primary swt:ml-auto"
        //                     prop.disabled (not canConfirmImport)
        //                     prop.text "Import"
        //                     prop.onClick (fun _ -> confirmImport ())
        //                 ]
        //             ]
        //         ]
        // )
        Html.none
