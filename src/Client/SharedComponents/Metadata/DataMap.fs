module Components.Metadata.DataMap

open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components
open Components.Forms

let Main (parentId: string option, parent: DataMapParent option, dataMap, setDataMap) =

    let desc = "Set the identifier and type of file your datamap is part of."

    let setParentId parentId =
        setDataMap parentId parent dataMap

    let setParent parent =
        setDataMap parentId parent dataMap

    let content = [
        Html.div [
            prop.className "swt:flex swt:gap-4 swt:flex-col swt:@lg/main:flex-row"
            prop.children [
                FormComponents.ParentId(parentId, setParentId, "Parent Id")
                FormComponents.SelectDataMapParent(parent, setParent)
            ]
        ]
    ]

    Generic.BoxedField("Datamap", desc, content)