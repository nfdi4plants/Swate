namespace Protocol

open Feliz
open Feliz.Bulma
open Messages
open Model
open Shared

type TemplateFromDB =
    
    static member toProtocolSearchElement (model:Model) dispatch =
        Bulma.button.span [
            prop.onClick(fun _ -> UpdatePageState (Some Routing.Route.ProtocolSearch) |> dispatch)
            Bulma.color.isInfo
            Bulma.button.isFullWidth
            prop.style [style.margin (length.rem 1, length.px 0)]
            prop.text "Browse database" ]

    static member addFromDBToTableButton (model:Model) dispatch =
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
                                /// Filter out existing building blocks and keep input/output values.
                                let joinConfig = ARCtrl.TableJoinOptions.WithValues // If changed to anything else we need different logic to keep input/output values
                                let preparedTemplate = Table.selectiveTablePrepare model.SpreadsheetModel.ActiveTable model.ProtocolState.TemplateSelected.Value.Table
                                let index = Spreadsheet.Controller.BuildingBlocks.SidebarControllerAux.getNextColumnIndex model.SpreadsheetModel
                                SpreadsheetInterface.JoinTable (preparedTemplate, Some index, Some joinConfig) |> InterfaceMsg |> dispatch
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

    static member Main(model:Model, dispatch) =
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
