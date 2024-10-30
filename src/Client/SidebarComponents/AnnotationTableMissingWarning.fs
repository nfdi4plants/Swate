module SidebarComponents.AnnotationTableMissingWarning

open Fable.React
open Fable.React.Props
open ExcelColors
open Model
open Messages
open Feliz
open Feliz.Bulma


let annotationTableMissingWarningComponent (model:Model) (dispatch: Msg-> unit) =
    Bulma.notification [
        Bulma.color.isWarning
        prop.children [
            Bulma.delete [
                prop.onClick (fun _ ->
                    OfficeInterop.AnnotationTableExists (Result.Ok "Remove Warning Notification") |> OfficeInteropMsg |> dispatch
                )
            ]
            Html.h5 "Warning: No annotation table found in worksheet"
            Bulma.field.div "Your worksheet seems to contain no annotation table. You can create one by pressing the button below."
            Bulma.field.div [
                Bulma.button.button [
                    Bulma.button.isFullWidth
                    prop.onClick (fun e -> SpreadsheetInterface.CreateAnnotationTable e.ctrlKey |> Messages.InterfaceMsg |> dispatch)
                    prop.text "Create Annotation Table"
                ]                
            ]
        ]
    ]
