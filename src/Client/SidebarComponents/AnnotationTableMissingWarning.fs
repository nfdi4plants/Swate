module SidebarComponents.AnnotationTableMissingWarning

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Feliz
open Feliz.DaisyUI


let annotationTableMissingWarningComponent (model:Model) (dispatch: Msg-> unit) =
    Daisy.alert [
        alert.warning
        prop.children [
            Html.div [
                prop.className "justify-end"
                prop.children [
                    Components.Components.DeleteButton(props=[
                        prop.onClick (fun _ -> OfficeInterop.AnnotationTableExists false |> OfficeInteropMsg |> dispatch)
                    ])
                ]
            ]
            Html.div [
                prop.className "prose"
                prop.children [
                    Html.h5 "Warning: No annotation table found in worksheet"
                    Html.p "Your worksheet seems to contain no annotation table. You can create one by pressing the button below."
                    Daisy.button.button [
                        prop.className "grow"
                        prop.onClick (fun e -> SpreadsheetInterface.CreateAnnotationTable e.ctrlKey |> Messages.InterfaceMsg |> dispatch)
                        prop.text "Create Annotation Table"
                    ]
                ]
            ]
        ]
    ]
