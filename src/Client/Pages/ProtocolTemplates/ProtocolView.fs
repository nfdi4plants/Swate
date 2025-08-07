namespace Protocol

open Model

open Feliz

type Templates =

    [<ReactComponent>]
    static member Main(model: Model, dispatch) =
        SidebarComponents.SidebarLayout.Container [
            SidebarComponents.SidebarLayout.Header "Templates"

            SidebarComponents.SidebarLayout.Description(
                Html.p [
                    Html.b "Search the database for templates."
                    Html.text " The building blocks from these templates can be inserted into the Swate table. "
                    Html.span [
                        prop.className "swt:text-error"
                        prop.text "Only missing building blocks will be added."
                    ]
                ]
            )

            SidebarComponents.SidebarLayout.LogicContainer [
                if model.ProtocolState.ShowSearch then
                    Protocol.SearchContainer.Main(model, dispatch)
                else
                    Modals.SelectiveTemplateFromDB.Main(model, dispatch, false)
            ]
        ]