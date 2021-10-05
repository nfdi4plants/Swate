module CustomComponents.AnnotationTableMissingWarning

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
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
            OnClick (fun e ->
                OfficeInterop.AnnotationTableExists (Shared.OfficeInteropTypes.TryFindAnnoTableResult.Success "Remove Warning Notification") |> OfficeInteropMsg |> dispatch
            )
        ]] [ ]
        Heading.h5 [] [str "Warning: No Annotation table found in worksheet"]
        Field.div [] [
            str "Your worksheet seems to contain no annotation table. You can create one by pressing the button below"
        ]
        Field.div [][
            Button.buttonComponent
                model.SiteStyleState.ColorMode
                model.SiteStyleState.IsDarkMode
                "create annotation table"
                (fun _ -> OfficeInterop.CreateAnnotationTable model.SiteStyleState.IsDarkMode |> OfficeInteropMsg |> dispatch )
        ]
    ]
