namespace Protocol

open Feliz
open Feliz.DaisyUI
open Messages
open Model
open Shared

type SelectedColumns = {
    Columns: bool []
}
with
    static member init(length) =
        {
            Columns = Array.init length (fun _ -> true)
        }

type TemplateFromDB =

    static member toProtocolSearchElement (model:Model) dispatch =
        Daisy.button.button [
            prop.onClick(fun _ -> UpdateModel {model with Model.PageState.SidebarPage = Routing.SidebarPage.ProtocolSearch} |> dispatch)
            button.primary
            button.block
            prop.text "Browse database"
        ]

    static member addFromDBToTableButton (model:Model) selectionInformation dispatch =
        Html.div [
            prop.className "join flex flex-row justify-center gap-2"
            prop.children [
                Daisy.button.a [
                    button.success
                    button.wide
                    if model.ProtocolState.TemplateSelected.IsNone then
                        button.error
                        prop.disabled true
                    prop.onClick (fun _ ->
                        if model.ProtocolState.TemplateSelected.IsNone then
                            failwith "No template selected!"

                        SpreadsheetInterface.AddTemplate(model.ProtocolState.TemplateSelected.Value.Table, selectionInformation.Columns) |> InterfaceMsg |> dispatch
                    )
                    prop.text "Add template"
                ]
                if model.ProtocolState.TemplateSelected.IsSome then
                    Daisy.button.a [
                        button.outline
                        prop.onClick (fun _ -> Protocol.RemoveSelectedProtocol |> ProtocolMsg |> dispatch)
                        button.error
                        Html.i [prop.className "fa-solid fa-times"] |> prop.children
                    ]
            ]
        ]

    [<ReactComponent>]
    static member displaySelectedProtocolEle (model:Model) (selectionInformation:SelectedColumns) (setSelectedColumns:SelectedColumns -> unit) dispatch =
        Html.div [
            prop.style [style.overflowX.auto; style.marginBottom (length.rem 1)]
            prop.children [
                Daisy.table [
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th "Selection"
                                Html.th "Column"
                                Html.th "Column TAN"
                                //Html.th "Unit"
                                //Html.th "Unit TAN"
                            ]
                        ]
                        Html.tbody [
                            for i in 0..model.ProtocolState.TemplateSelected.Value.Table.Columns.Length-1 do
                                let column = model.ProtocolState.TemplateSelected.Value.Table.Columns.[i]
                                //let unitOption = column.TryGetColumnUnits()
                                yield
                                    Html.tr [
                                        Html.div [
                                            prop.style [style.display.flex; style.justifyContent.center]
                                            prop.children [
                                                Daisy.checkbox [
                                                    prop.type'.checkbox
                                                    prop.isChecked selectionInformation.Columns.[i]
                                                    prop.onChange (fun (b: bool) ->
                                                        let selectedData = selectionInformation.Columns
                                                        selectedData.[i] <- b
                                                        {selectionInformation with Columns = selectedData} |> setSelectedColumns)
                                                ]
                                            ]
                                        ]
                                        Html.td (column.Header.ToString())
                                        Html.td (if column.Header.IsTermColumn then column.Header.ToTerm().TermAccessionShort else "-")
                                        //td [] [str (if unitOption.IsSome then insertBB.UnitTerm.Value.Name else "-")]
                                        //td [] [str (if insertBB.HasUnit then insertBB.UnitTerm.Value.TermAccession else "-")]
                                    ]
                        ]
                    ]
                ]
            ]
        ]

    static member Main(model:Model, dispatch) =
        let length = if model.ProtocolState.TemplateSelected.IsSome then model.ProtocolState.TemplateSelected.Value.Table.Columns.Length else 0
        let selectedColumns, setSelectedColumns = React.useState(SelectedColumns.init length)
        SidebarComponents.SidebarLayout.LogicContainer [
            Html.div [
                TemplateFromDB.toProtocolSearchElement model dispatch
            ]

            Html.div [
                TemplateFromDB.addFromDBToTableButton model selectedColumns dispatch
            ]
            if model.ProtocolState.TemplateSelected.IsSome then
                Html.div [
                    TemplateFromDB.displaySelectedProtocolEle model selectedColumns setSelectedColumns dispatch
                ]
                Html.div [
                    TemplateFromDB.addFromDBToTableButton model selectedColumns dispatch
                ]
        ]
