namespace Components.Datamap

open ARCtrl
open Feliz
open Feliz.Bulma
open Components

type DatamapConfig =

    static member Main(datamap: DataMap option, setDatamap: DataMap option -> unit) =
        let desc = Some "Add datamap sheet. This allows detailed annotation of data files."
        let content =
            [
                Bulma.buttons [
                    Bulma.button.button [
                        color.isSuccess
                        if datamap.IsSome then button.isStatic
                        prop.onClick (fun _ ->
                            let newDtm = DataMap.init()
                            setDatamap (Some newDtm)
                        )
                        prop.children [
                            Bulma.icon [
                                Html.i [prop.className "fa-solid fa-map"]
                            ]
                            Html.span "Add Datamap"
                        ]
                    ]
                    Bulma.button.button [
                        color.isDanger
                        if datamap.IsNone then button.isStatic
                        prop.onClick(fun _ ->
                            setDatamap None
                        )
                        prop.children [
                            Bulma.icon [
                                Html.i [prop.className "fa-solid fa-trash"]
                            ]
                            Html.span "Remove Datamap"
                        ]
                    ]
                ]
            ]
        Generic.BoxedField
            "Datamap"
            desc
            content