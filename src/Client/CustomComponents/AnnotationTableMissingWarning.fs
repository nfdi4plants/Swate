module CustomComponents.AnnotationTableMissingWarning

open Fable.React
open Fable.React.Props
open Fulma
open ExcelColors
open Model
open Messages

let annotationTableMissingWarningComponent (model:Model) (dispatch: Msg-> unit) =
    Notification.notification [
        Notification.Color IsWarning
        Notification.Props [
            Style [
                BackgroundColor (if model.SiteStyleState.IsDarkMode = false then NFDIColors.Yellow.Base else model.SiteStyleState.ColorMode.ControlBackground)
                Color model.SiteStyleState.ColorMode.Text
                //yield! colorControlInArray model.SiteStyleState.ColorMode
            ]
        ]
    ] [
        Notification.delete [ Props [
            OnClick (fun _ ->
                OfficeInterop.AnnotationTableExists (Shared.OfficeInteropTypes.TryFindAnnoTableResult.Success "Remove Warning Notification") |> OfficeInteropMsg |> dispatch
            )
        ]] [ ]
        Heading.h5 [] [str "Warning: No annotation table found in worksheet"]
        Field.div [] [
            str "Your worksheet seems to contain no annotation table. You can create one by pressing the button below."
        ]
        Field.div [] [
            Button.button [
                if model.SiteStyleState.IsDarkMode then
                    Button.Color IsWarning
                    Button.IsOutlined
                else
                    Button.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.BodyForeground; Color model.SiteStyleState.ColorMode.Text]]
                Button.IsFullWidth
                Button.OnClick (fun e -> OfficeInterop.CreateAnnotationTable e.ctrlKey |> OfficeInteropMsg |> dispatch )
                ] [
                str "create annotation table"
            ]                
        ]
    ]
