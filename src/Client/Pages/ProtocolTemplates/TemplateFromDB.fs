namespace Protocol

open Feliz
open Feliz.Bulma
open Messages

type TemplateFromDB =
    
    static member toProtocolSearchElement (model:Model) dispatch =
        Bulma.button.span [
            prop.onClick(fun _ -> UpdatePageState (Some Routing.Route.ProtocolSearch) |> dispatch)
            Bulma.color.isInfo
            Bulma.button.isFullWidth
            prop.style [style.margin (length.rem 1, length.px 0)]
            prop.text "Browse database" ]

    static member addFromDBToTableButton (model:Messages.Model) dispatch =
        Bulma.columns [
            Bulma.columns.isMobile
            prop.children [
                Bulma.column [
                    prop.children [
                        Bulma.button.a [
                            Bulma.color.isSuccess
                            if model.ProtocolState.TemplateSelected.IsSome then
                                Bulma.button.isActive
                            else
                                Bulma.color.isDanger
                                prop.disabled true
                            Bulma.button.isFullWidth
                            prop.onClick (fun _ ->
                                if model.ProtocolState.TemplateSelected.IsNone then
                                    failwith "No template selected!"
                                // Remove existing columns
                                let mutable columnsToRemove = []
                                // find duplicate columns
                                let tablecopy = model.ProtocolState.TemplateSelected.Value.Table.Copy()
                                for header in model.SpreadsheetModel.ActiveTable.Headers do
                                    let containsAtIndex = tablecopy.Headers |> Seq.tryFindIndex (fun h -> h = header)
                                    if containsAtIndex.IsSome then
                                        columnsToRemove <- containsAtIndex.Value::columnsToRemove
                                log columnsToRemove
                                tablecopy.RemoveColumns (Array.ofList columnsToRemove)
                                tablecopy.IteriColumns(fun i c0 ->
                                    let c1 = {c0 with Cells = [||]}
                                    let c2 =
                                        if c1.Header.isInput then
                                            match model.SpreadsheetModel.ActiveTable.TryGetInputColumn() with
                                            | Some ic ->
                                                {c1 with Cells = ic.Cells}
                                            | _ -> c1
                                        elif c1.Header.isOutput then
                                            match model.SpreadsheetModel.ActiveTable.TryGetOutputColumn() with
                                            | Some oc ->
                                                {c1 with Cells = oc.Cells}
                                            | _ -> c1
                                        else
                                            c1
                                    tablecopy.UpdateColumn(i, c2.Header, c2.Cells)
                                )
                                log(tablecopy.RowCount, tablecopy.ColumnCount)
                                let index = Spreadsheet.BuildingBlocks.Controller.SidebarControllerAux.getNextColumnIndex model.SpreadsheetModel
                                let joinConfig = Some ARCtrl.TableJoinOptions.WithValues // If changed to anything else we need different logic to keep input/output values
                                SpreadsheetInterface.JoinTable (tablecopy, Some index, joinConfig ) |> InterfaceMsg |> dispatch
                            )
                            prop.text "Add template"
                        ]
                    ]
                ]
                if model.ProtocolState.TemplateSelected.IsSome then
                    Bulma.column [
                        Bulma.column.isNarrow
                        Bulma.button.a [
                            prop.onClick (fun e -> Protocol.RemoveSelectedProtocol |> ProtocolMsg |> dispatch)
                            Bulma.color.isDanger
                            Html.i [prop.className "fa-solid fa-times"] |> prop.children
                        ] |> prop.children
                    ]
            ]
        ]

    static member displaySelectedProtocolEle (model:Model) dispatch =
        Html.div [
            prop.style [style.overflowX.auto; style.marginBottom (length.rem 1)]
            prop.children [
                Bulma.table [
                    Bulma.table.isFullWidth;
                    Bulma.table.isBordered
                    prop.children [
                        Html.thead [
                            Html.tr [
                                Html.th "Column"
                                Html.th "Column TAN"
                                //Html.th "Unit"
                                //Html.th "Unit TAN"
                            ]
                        ]
                        Html.tbody [
                            for column in model.ProtocolState.TemplateSelected.Value.Table.Columns do
                                //let unitOption = column.TryGetColumnUnits()
                                yield
                                    Html.tr [
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

    static member Main(model:Messages.Model, dispatch) =
        mainFunctionContainer [
            Bulma.field.div [
                Bulma.help [
                    Html.b "Search the database for templates."
                    Html.text " The building blocks from these templates can be inserted into the Swate table. "
                    Html.span [
                        color.hasTextDanger
                        prop.text "Only missing building blocks will be added."
                    ]
                ]
            ]
            Bulma.field.div [
                TemplateFromDB.toProtocolSearchElement model dispatch
            ]

            Bulma.field.div [
                TemplateFromDB.addFromDBToTableButton model dispatch
            ]
            if model.ProtocolState.TemplateSelected.IsSome then
                Bulma.field.div [
                    TemplateFromDB.displaySelectedProtocolEle model dispatch
                ]
                Bulma.field.div [
                    TemplateFromDB.addFromDBToTableButton model dispatch
                ]
        ]
