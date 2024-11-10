namespace Components.Datamap

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Components

type DatamapConfig =

    static member Main(datamap: DataMap option, setDatamap: DataMap option -> unit) =
        let desc = Some "Add datamap sheet. This allows detailed annotation of data files."
        let content =
            [
                Html.div [
                    Daisy.button.button [
                        button.success
                        if datamap.IsSome then button.disabled
                        prop.onClick (fun _ ->
                            let newDtm = DataMap.init()
                            setDatamap (Some newDtm)
                        )
                        prop.children [
                            Html.i [prop.className "fa-solid fa-map"]
                            Html.span "Add Datamap"
                        ]
                    ]
                    Daisy.button.button [
                        button.error
                        if datamap.IsNone then button.disabled
                        prop.onClick(fun _ ->
                            setDatamap None
                        )
                        prop.children [
                            Html.i [prop.className "fa-solid fa-trash"]
                            Html.span "Remove Datamap"
                        ]
                    ]
                ]
            ]
        Generic.BoxedField
            (Some "Datamap")
            desc
            content