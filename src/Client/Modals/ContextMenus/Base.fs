namespace Modals.ContextMenus

open Feliz
open Feliz.DaisyUI
open Swate.Components

type Base =

    static member Divider() =
        //Daisy.divider [ prop.className "swt:!m-0 swt:pl-1 swt:pr-3 swt:h-2" ]
        Html.div [
            prop.className "swt:divider swt:!m-0 swt:pl-1 swt:pr-3 swt:h-2"
        ]

    static member Item(content: ReactElement, ?onclick, ?icon: ReactElement, ?inactive: bool) =
        Html.li [
            if onclick.IsSome then
                prop.onClick onclick.Value
            prop.className [
                "swt:bg-base-300 swt:text-base-content"
                "swt:flex swt:flex-row swt:justify-between swt:cursor-pointer"
                "swt:text-sm swt:w-full swt:gap-4 swt:px-3 swt:py-1"
                if inactive.IsSome && inactive.Value then
                    "swt:cursor-not-allowed swt:opacity-50"
                else
                    "swt:transition-colors swt:duration-200 swt:hover:bg-base-200"

            ]
            prop.children [
                content
                if icon.IsSome then
                    Html.div [ prop.className "swt:ml-auto"; prop.children icon.Value ]
            ]
        ]

    static member Item(content: ReactElement, onclick, ?icon: ReactElement, ?inactive) =
        let icon = icon |> Option.map (fun i -> i)
        Base.Item(content, onclick, ?icon = icon, ?inactive = inactive)

    static member Item(content: string, onclick, ?icon: ReactElement, ?inactive) =
        let icon = icon |> Option.map (fun i -> i)
        Base.Item(Html.span content, onclick, ?icon = icon, ?inactive = inactive)

    static member Item(content: ReactElement, onclick, ?icons: ReactElement[], ?inactive) =
        let icon =
            icons
            |> Option.map (fun icons ->
                React.fragment [
                    for i in icons do
                        i
                ])

        Base.Item(content, onclick, ?icon = icon, ?inactive = inactive)

    [<ReactComponent>]
    static member Main(mousex: int, mousey: int, children: (unit -> unit) -> ReactElement seq, dispatch) =
        let rmv = Modals.Util.RMV_MODAL dispatch
        let ref = React.useElementRef ()
        React.useListener.onClickAway (ref, fun _ -> rmv ())

        Html.div [
            prop.ref ref
            prop.style [ style.left mousex; style.top mousey ]
            prop.className "swt:fixed swt:z-50 swt:shadow-md swt:rounded-md swt:min-w-fit swt:bg-base-300 swt:text-black"
            prop.children [ Html.ul (children rmv) ]
        ]