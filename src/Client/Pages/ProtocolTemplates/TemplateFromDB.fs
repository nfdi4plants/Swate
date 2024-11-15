namespace Protocol

open Feliz
open Feliz.DaisyUI
open Messages
open Model
open Shared

type TemplateFromDB =

    static member toProtocolSearchElement (model:Model) dispatch =
        Daisy.button.button [
            prop.onClick(fun _ -> UpdatePageState (Some Routing.Route.ProtocolSearch) |> dispatch)
            button.info
            button.block
            prop.style [style.margin (length.rem 1, length.px 0)]
            prop.text "Browse database" ]

    static member addFromDBToTableButton (model:Model) dispatch =
        Html.div [
            prop.className "flex flex-row"
            prop.children [
                Html.div [
                    prop.children [
                        Daisy.button.a [
                            button.success
                            button.block
                            if model.ProtocolState.TemplateSelected.IsNone then
                                button.error
                                prop.disabled true
                            prop.onClick (fun _ ->
                                if model.ProtocolState.TemplateSelected.IsNone then
                                    failwith "No template selected!"

                                SpreadsheetInterface.AddTemplate(model.ProtocolState.TemplateSelected.Value.Table) |> InterfaceMsg |> dispatch
                            )
                            prop.text "Add template"
                        ]
                    ]
                ]
                if model.ProtocolState.TemplateSelected.IsSome then
                    Html.div [
                        prop.className "grow-0"
                        Daisy.button.a [
                            prop.onClick (fun e -> Protocol.RemoveSelectedProtocol |> ProtocolMsg |> dispatch)
                            button.error
                            Html.i [prop.className "fa-solid fa-times"] |> prop.children
                        ] |> prop.children
                    ]
            ]
        ]

    static member displaySelectedProtocolEle (model:Model) dispatch =
        Html.div [
            prop.style [style.overflowX.auto; style.marginBottom (length.rem 1)]
            prop.children [
                Daisy.table [
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
        SidebarComponents.SidebarLayout.LogicContainer [
            Html.div [
                Html.p [
                    Html.b "Search the database for templates."
                    Html.text " The building blocks from these templates can be inserted into the Swate table. "
                    Html.span [
                        prop.className "text-error"
                        prop.text "Only missing building blocks will be added."
                    ]
                ]
            ]
            Html.div [
                TemplateFromDB.toProtocolSearchElement model dispatch
            ]

            Html.div [
                TemplateFromDB.addFromDBToTableButton model dispatch
            ]
            if model.ProtocolState.TemplateSelected.IsSome then
                Html.div [
                    TemplateFromDB.displaySelectedProtocolEle model dispatch
                ]
                Html.div [
                    TemplateFromDB.addFromDBToTableButton model dispatch
                ]
        ]
