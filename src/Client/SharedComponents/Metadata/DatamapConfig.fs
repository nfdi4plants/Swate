namespace Components.Forms

open ARCtrl
open Feliz
open Feliz.DaisyUI

type Datamap =

    static member Main(datamap: DataMap option, setDatamap: DataMap option -> unit) =
        let desc = "Add datamap sheet. This allows detailed annotation of data files."

        let content = [
            Html.div [
                prop.className "swt:flex swt:gap-4 swt:flex-col swt:@lg/main:flex-row"
                prop.children [
                    //Daisy.button.button [
                    Html.button [
                        prop.className [
                            "swt:btn swt:btn-success"
                            if datamap.IsSome then
                                "swt:btn-disabled"
                        ]
                        prop.onClick (fun _ ->
                            let newDtm = DataMap.init ()
                            setDatamap (Some newDtm))
                        prop.children [ Html.i [ prop.className "fa-solid fa-map" ]; Html.span "Add Datamap" ]
                    ]
                    //Daisy.button.button [
                    Html.button [
                        prop.className [
                            "swt:btn swt:btn-error"
                            if datamap.IsNone then
                                "swt:btn-disabled"
                        ]
                        prop.onClick (fun _ -> setDatamap None)
                        prop.children [ Html.i [ prop.className "fa-solid fa-trash" ]; Html.span "Remove Datamap" ]
                    ]
                ]
            ]
        ]

        Generic.BoxedField("Datamap", desc, content)