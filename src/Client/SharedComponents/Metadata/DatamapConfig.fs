namespace Components.Forms

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components

type Datamap =

    static member Main(datamap: DataMap option, setDatamap: DataMap option -> unit) =
        let desc = "Add datamap sheet. This allows detailed annotation of data files."

        let content = [
            Html.div [
                prop.className "swt:flex swt:gap-4 swt:flex-col swt:@lg/main:flex-row"
                prop.children [
                    Html.button [
                        prop.className [
                            "swt:btn swt:btn-success"
                            if datamap.IsSome then
                                "swt:btn-disabled"
                        ]
                        prop.onClick (fun _ ->
                            let newDtm = DataMap.init ()
                            setDatamap (Some newDtm))
                        prop.children [ Icons.Map(); Html.span "Add Datamap" ]
                    ]
                    Html.button [
                        prop.className [
                            "swt:btn swt:btn-error"
                            if datamap.IsNone then
                                "swt:btn-disabled"
                        ]
                        prop.onClick (fun _ -> setDatamap None)
                        prop.children [ Icons.Delete() ; Html.span "Remove Datamap" ]
                    ]
                ]
            ]
        ]

        Generic.BoxedField("Datamap", desc, content)