namespace Components

open Feliz
open Feliz.DaisyUI

type BaseContextMenu =

    static member Divider() =
        Daisy.divider [prop.className "!m-0 pl-1 pr-3 h-2"]

    static member Item(content: ReactElement, onclick, ?icon: ReactElement, ?inactive: bool) =
        Html.li [
            prop.onClick onclick
            prop.className [
                "bg-base-300 text-base-content"
                "flex flex-row justify-between cursor-pointer"
                "text-sm w-full gap-4 px-3 py-1"
                if inactive.IsSome && inactive.Value then
                    "cursor-not-allowed opacity-50"
                else
                    "transition-colors duration-200 hover:bg-base-200"

            ]
            prop.children [
                content
                if icon.IsSome then
                    Html.div [
                        prop.className "ml-auto"
                        prop.children icon.Value
                    ]
            ]
        ]

    static member Item(content: ReactElement, onclick, ?icon: string, ?inactive) =
        let icon = icon |> Option.map (fun i -> Html.i [prop.className i])
        BaseContextMenu.Item(content, onclick, ?icon = icon, ?inactive=inactive)

    static member Item(content: string, onclick, ?icon: string, ?inactive) =
        let icon = icon |> Option.map (fun i -> Html.i [prop.className i])
        BaseContextMenu.Item(Html.span content, onclick, ?icon = icon, ?inactive=inactive)

    static member Item(content: ReactElement, onclick, ?icons: string [], ?inactive) =
        let icon = icons |> Option.map (fun icons ->
            React.fragment [
                for i in icons do
                    Html.i [prop.className i]
            ]
        )
        BaseContextMenu.Item(content, onclick, ?icon = icon, ?inactive=inactive)

    [<ReactComponent>]
    static member Main(mousex: int, mousey: int, rmv: unit -> unit, children: ReactElement seq) =
        let ref = React.useElementRef()
        React.useListener.onClickAway(ref, fun _ -> rmv())
        Html.div [
            prop.ref ref
            prop.style [
                style.left mousex
                style.top (mousey - 40)
            ]
            prop.className "fixed z-50 shadow-md rounded-md min-w-fit bg-base-300 text-black"
            prop.children [
                Html.ul children
            ]
        ]