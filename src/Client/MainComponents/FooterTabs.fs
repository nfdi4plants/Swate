module MainComponents.FooterTabs

open Feliz
open Feliz.Bulma

type private FooterTab = {
    IsEditable: bool
    Name: string
} with
    static member init(name) = {
        IsEditable = false
        Name = name
    }

let private popup (x: int, y: int) renameMsg deleteMsg (rmv: _ -> unit) =
    let rmv_element = Html.div [
        prop.onClick rmv
        prop.style [
            style.position.fixedRelativeToWindow
            style.backgroundColor.transparent
            style.left 0
            style.top 0
            style.right 0
            style.bottom 0
            style.display.block
        ]
    ]
    let button (name:string, msg, props) = Html.li [
        Bulma.button.button [
            prop.style [style.borderRadius 0]
            prop.onClick msg
            Bulma.button.isFullWidth
            Bulma.button.isSmall
            yield! props
            prop.text name
        ]
    ]
    Html.div [
        prop.style [
            let height = 53
            style.backgroundColor "white"
            style.position.absolute
            style.left x
            style.top (y - height)
            style.zIndex 20
            style.width 70
            style.height height
        ]
        prop.children [
            rmv_element
            Html.ul [
                button ("Delete", deleteMsg rmv, [Bulma.color.isDanger])
                button ("Rename", renameMsg rmv, [])
            ]
        ]
    ]

[<ReactComponent>]
let Main (input: {|i: int; table: Spreadsheet.SwateTable; model: Messages.Model; dispatch: Messages.Msg -> unit|}) = // (i: int) (table: Spreadsheet.SwateTable) (model: Messages.Model) dispatch =
    let state, setState = React.useState(FooterTab.init(input.table.Name))
    let dispatch = input.dispatch
    let id = $"{input.table.Id}_{input.table.Name}"
    Bulma.tab [
        prop.key id
        if input.model.SpreadsheetModel.ActiveTableIndex = input.i then Bulma.tab.isActive
        prop.onClick (fun _ -> Spreadsheet.UpdateActiveTable input.i |> Messages.SpreadsheetMsg |> dispatch)
        prop.onContextMenu(fun e ->
            e.stopPropagation()
            e.preventDefault()
            let mousePosition = int e.pageX, int e.pageY
            let deleteMsg rmv = fun e -> rmv e; Spreadsheet.RemoveTable input.i |> Messages.SpreadsheetMsg |> dispatch
            let renameMsg rmv = fun e -> rmv e; {state with IsEditable = true} |> setState
            let child = popup mousePosition renameMsg deleteMsg
            let name = $"popup_{mousePosition}"
            Modals.Controller.renderModal(name, child)
        )
        prop.children [
            if state.IsEditable then
                let updateName = fun e ->
                    Spreadsheet.RenameTable (input.i, state.Name) |> Messages.SpreadsheetMsg |> dispatch
                    setState {state with IsEditable = false}
                Bulma.input.text [
                    prop.autoFocus(true)
                    prop.id (id + "input")
                    prop.onChange (fun e ->
                        setState {state with Name = e}
                    )
                    prop.onBlur updateName
                    // .. when pressing "ENTER". "ESCAPE" will negate changes.
                    prop.onKeyDown(fun e ->
                        match e.which with
                        | 13. -> //enter
                            updateName e
                        | 27. -> //escape
                            setState {state with IsEditable = false; Name = input.table.Name}
                        | _ -> ()
                    )
                    prop.defaultValue input.table.Name
                ]
            else
                Html.a [prop.text input.table.Name]
        ]
    ]