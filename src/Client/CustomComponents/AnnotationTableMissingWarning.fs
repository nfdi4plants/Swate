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

        ]
    ] [
        Notification.delete [] []
        Heading.h5 [] [str "Warning: No Annotation table found in worksheet"]
        Text.p [] [
            str "Your worksheet seems to contain no annotation table. You can create one by pressing the button below"
        ]
        Button.buttonComponent
            model.SiteStyleState.ColorMode
            true
            "create annotation table"
            (fun _ ->
                (fun (allNames) ->
                    CreateAnnotationTable (allNames,model.SiteStyleState.IsDarkMode))
                    |> PipeCreateAnnotationTableInfo
                    |> ExcelInterop
                    |> dispatch
            )
            //(fun _ -> pipeNameTuple CreateAnnotationTable model.SiteStyleState.IsDarkMode |> ExcelInterop |> dispatch)
    ]
