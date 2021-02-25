module ValidationView

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions

open Shared

open ExcelColors
open Model
open Messages

open CustomComponents

open OfficeInterop.Types.Xml.ValidationTypes

let columnListElement ind (columnValidation:ColumnValidation) (model:Model) dispatch =
    let isActive =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            true
        | _ ->
            false
    tr [
        /// Remove validationTableEle when active to remove on-hover color change to really light grey.
        if isActive then
            Class "nonSelectText"
        else
            Class "nonSelectText validationTableEle"
        Style [
            Cursor "pointer"
            UserSelect UserSelectOptions.None
            if isActive then
                BackgroundColor model.SiteStyleState.ColorMode.ElementBackground
                Color "white"
        ]
        OnClick (fun e ->
            e.preventDefault()
            if isActive then
                UpdateDisplayedOptionsId None |> Validation |> dispatch
            else
                UpdateDisplayedOptionsId (Some ind) |> Validation |> dispatch
        )
    ][
        td [][str columnValidation.ColumnHeader]
        td [][
            if columnValidation.Importance.IsSome then
                str (string columnValidation.Importance)
            else
                str "-"
        ]
        td [][
            if columnValidation.ValidationFormat.IsSome then
                str columnValidation.ValidationFormat.Value.toReadableString
            else
                str "-"
        ]
        td [][
            Icon.icon [][
                Fa.i [Fa.Solid.ChevronDown][]
            ]
        ]
    ]

let updateTableValidationByColValidation (model:Model) (updatedColValidation:ColumnValidation) =
    {
        model.ValidationState.TableValidationScheme with
            ColumnValidations =
                model.ValidationState.TableValidationScheme.ColumnValidations
                |> List.filter (fun x -> x.ColumnHeader <> updatedColValidation.ColumnHeader)
                |> fun filteredList -> updatedColValidation::filteredList
                |> List.sortBy (fun colVal -> colVal.ColumnAdress)
    }

let checkradioElement (id:int) (contentTypeOpt:ContentType option) (columnValidation:ColumnValidation) (model:Model) dispatch =
    let contentType = if contentTypeOpt.IsSome then contentTypeOpt.Value.toReadableString else "None"
    /// See issue #54
    //Checkradio.radio [
    //    //Checkradio.InputProps [Style [Border "1px solid red"]]
    //    Checkradio.Id (sprintf "checkradio%i%s" id contentType)
    //    Checkradio.Disabled (contentType = "Ontology [None]")
    //    Checkradio.Name (sprintf "ContentType%i" id)
    //    Checkradio.OnChange (fun e ->
    //        let newFormat = {
    //            format with
    //                ContentType = contentTypeOpt
    //        }
    //        UpdateValidationFormat (format,newFormat) |> Validation |> dispatch
    //    )
    //    Checkradio.Checked (contentTypeOpt = format.ContentType)
    //    Checkradio.LabelProps [Class "nonSelectText"]
    //    Checkradio.Color IsSuccess
    //][
    //    str contentType
    //]
    let isDisabled = (contentType = "Ontology [None]" || contentType = "Unit [None]")
    div [Style [Position PositionOptions.Relative]] [
        input [
            Type "checkbox";
            Class "checkbox-input"
            Id (sprintf "checkradio%i%s" id contentType)
            Name (sprintf "ContentType%i" id)
            Disabled isDisabled
            OnChange (fun e ->
                let nextColumnValidation = {
                    columnValidation with
                        ValidationFormat = contentTypeOpt
                }
                let nextTableValidation =
                    updateTableValidationByColValidation model nextColumnValidation
                UpdateTableValidationScheme nextTableValidation |> Validation |> dispatch
            )
            Checked (contentTypeOpt = columnValidation.ValidationFormat)

        ]
        label [
            Class "checkbox-label"
            HtmlFor (sprintf "checkradio%i%s" id contentType)
        ][str contentType]
        label [
            Class "checkbox-checkmark";
            HtmlFor (sprintf "checkradio%i%s" id contentType)
        ][]
    ]

let findOntology (columnValidation:ColumnValidation) (buildingBlocks:OfficeInterop.Types.BuildingBlockTypes.BuildingBlock []) =
    buildingBlocks
    |> Array.find (fun x -> x.MainColumn.Header.Value.Header = columnValidation.ColumnHeader)
    |> fun x -> x.MainColumn.Header.Value.Ontology

let checkradioList (ind:int) colVal model dispatch =
    let hasOntology = findOntology colVal model.ValidationState.ActiveTableBuildingBlocks

    let unitContent =
        if colVal.Unit.IsSome then ContentType.UnitTerm colVal.Unit.Value |> Some else ContentType.UnitTerm "None" |> Some
        
    let ontologyContent =
        if hasOntology.IsSome then ContentType.OntologyTerm hasOntology.Value.Name |> Some else ContentType.OntologyTerm "None" |> Some

    [
        checkradioElement ind   None                        colVal model dispatch
        
        checkradioElement ind   (Some ContentType.Number)   colVal model dispatch
        checkradioElement ind   (Some ContentType.Int)      colVal model dispatch
        checkradioElement ind   (Some ContentType.Decimal)  colVal model dispatch
        checkradioElement ind   (Some ContentType.Text)     colVal model dispatch
        checkradioElement ind   (Some ContentType.Url)      colVal model dispatch

        checkradioElement ind   ontologyContent             colVal model dispatch
        checkradioElement ind   unitContent                 colVal model dispatch
    ]


let sliderElements id columnValidation model dispatch =
    //let defaultSliderVal = string (if columnValidation.Importance.IsSome then columnValidation.Importance.Value else 0)
    //let sliderId = sprintf "importanceSlider%i" id
    //let outputSliderId = sprintf "outputForImportanceSlider%i" id
    //[
    //    Slider.slider [
    //        Slider.Props [Id sliderId; Style [Height "40px"]]
    //        Slider.IsFullWidth
    //        Slider.IsCircle
    //        Slider.Max 10.
    //        Slider.Min 0.
    //        Slider.Step 1.
    //        Slider.CustomClass "has-output"
    //        Slider.OnChange (fun e ->
    //            // this is used to quickly update the label element to the right of the slider with the new value
    //            let sliderEle = Browser.Dom.document.getElementById(outputSliderId)
    //            let _ = sliderEle.textContent <- (if e.Value = "0" then "None" else e.Value)
    //            /// Previously this appeared rather laggy, but now it seems to work fine.
    //            let nextColumnValidation = {
    //                columnValidation with
    //                    Importance = if e.Value = "0" then None else int e.Value |> Some
    //            }
    //            let nextTableValidation =
    //                updateTableValidationByColValidation model nextColumnValidation
    //            UpdateTableValidationScheme nextTableValidation |> Validation |> dispatch
    //            //()
    //        )
    //        Slider.Color IsPrimary
    //        Slider.ValueOrDefault defaultSliderVal
    //    ]
    //    output [Props.HtmlFor sliderId; Id outputSliderId; Style [TextOverflow "unset"]] [
    //        str (if defaultSliderVal = "0" then "None" else defaultSliderVal)
    //    ]
    //]
    div [][
        for i in 1 .. 5 do
            yield
                Button.a [
                    Button.Color IsWarning
                    Button.Props [Style [Padding "0rem"]]
                    Button.IsLight
                    Button.OnClick (fun e ->
                        let nextColumnValidation = {
                            columnValidation with
                                Importance = i |> Some
                        }
                        let nextTableValidation =
                            updateTableValidationByColValidation model nextColumnValidation
                        UpdateTableValidationScheme nextTableValidation |> Validation |> dispatch
                        )
                ][
                    Fa.span [
                        Fa.Size Fa.FaLarge
                        if columnValidation.Importance.IsSome && columnValidation.Importance.Value >= i then
                            Fa.Solid.Star
                        else
                            Fa.Regular.Star
                        Fa.Props [Style [Color NFDIColors.Yellow.Base]]
                    ][]
                ]
        yield Button.a [
            Button.Color IsDanger
            Button.IsLight
            Button.OnClick (fun e ->
                let nextColumnValidation = {
                    columnValidation with
                        Importance = None
                }
                let nextTableValidation =
                    updateTableValidationByColValidation model nextColumnValidation
                UpdateTableValidationScheme nextTableValidation |> Validation |> dispatch
                )
        ][
            Fa.span [
                Fa.Size Fa.FaLarge
                Fa.Solid.Backspace
            ][]
        ]
    ]

open Fable.Core.JsInterop


///// Submit button to apply slider changes to model. If slider.OnChange would dispatch message the app would suffer from lag spikes.
///// EDIT: This problem appearently disappeared. For now we will test it with only slider
//let submitButton ind (columnValidation:ColumnValidation) (model:Model) dispatch =
//    Button.span [
//        Button.IsFullWidth
//        Button.Color IsPrimary
//        //Button.IsStatic isSame
//        Button.OnClick (
//            fun e ->
//                let sliderId = sprintf "importanceSlider%i" ind
//                let sliderEle = Browser.Dom.document.getElementById(sliderId)
//                let impoValue = sliderEle?value
//                let nextColumnValidation = {
//                    columnValidation with
//                        Importance = if impoValue = "0" then None else int impoValue |> Some
//                }
//                let nextTableValidation =
//                    updateTableValidationByColValidation model nextColumnValidation
//                UpdateTableValidationScheme nextTableValidation |> Validation |> dispatch
//        )
//    ][
//        str "Submit Importance"
//    ]

let optionsElement ind (columnValidation:ColumnValidation) (model:Model) dispatch =
    let isVisible =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            true
        | _ ->
            false
    tr [][
        td [
            ColSpan 4
            Style [
                Padding "0";
                if isVisible then BorderBottom (sprintf "2px solid %s" ExcelColors.colorfullMode.Accent)
            ]
        ][
            Box.box' [
                Props [
                    Style [
                        Display (if isVisible then DisplayOptions.Block else DisplayOptions.None)
                        Width "100%"
                    ]
                ]
            ][
                Columns.columns [][
                    Column.column [][
                        b [][str "Content Type"]

                        Help.help [Help.Props [Style [MarginBottom "1rem"]]][str "Select the specific type of content for the selected column."]
                    
                        yield! checkradioList ind columnValidation model dispatch

                    ]
                    Column.column [][
                        b [][str "Importance"]

                        Help.help [][str "Define how important it is to fill in the column correctly."]

                        sliderElements ind columnValidation model dispatch

                        //submitButton ind columnValidation model dispatch
                    ]
                ]
            ]
        ]
    ]

let validationElements (model:Model) dispatch =
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            //BorderRadius "15px 15px 0 0"
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ][
        Field.div [Field.Props [Style [
            Width "100%"
        ]]] [
            // Worksheet - annotationTable name - DateTime of saving
            div [
                Id "TableRepresentationInfoHeader"
                OnTransitionEnd (fun e ->
                    let header = Browser.Dom.document.getElementById("TableRepresentationInfoHeader")
                    header?style?opacity <- 1
                    header?style?transition <- "unset"
                )
            ][
                b [][ str model.ValidationState.TableValidationScheme.WorksheetName ]
                str " - "
                str model.ValidationState.TableValidationScheme.TableName
                str " - "
                str ( model.ValidationState.TableValidationScheme.DateTime.ToString("yyyy-MM-dd HH:mm") )
                str " - "
                str (
                    sprintf "Swate %s" (
                        if model.ValidationState.TableValidationScheme.SwateVersion = "" then
                            model.PersistentStorageState.AppVersion
                        else
                            model.ValidationState.TableValidationScheme.SwateVersion
                    )
                )
            ]
            Table.table [ Table.IsHoverable; Table.IsFullWidth ] [
                thead [ ] [
                    tr [ ] [
                        th [ ] [ str "Column Header" ]
                        th [ ] [ str "Importance" ]
                        th [ ] [ str "Content Type" ]
                        th [][]
                    ]
                ]
                tbody [ ] [
                    for i in 0 .. model.ValidationState.TableValidationScheme.ColumnValidations.Length-1 do
                        let colVal = model.ValidationState.TableValidationScheme.ColumnValidations.[i]
                        yield! [
                            columnListElement i colVal model dispatch
                            optionsElement i colVal model dispatch
                        ]
                ]
            ]

            /// Show warning if no validation format was found
            if model.ValidationState.TableValidationScheme.SwateVersion = "" then
                Label.label [Label.Size Size.IsSmall; Label.Props [Style [Color NFDIColors.Red.Lighter10]]][
                    str """No checklist for this table found! Hit "Add checklist to workbook" to add a checklist for the active annotation table."""
                ]

            // Submit new checklist scheme. This will write custom xml into the workbook.
            Button.a [
                Button.Color Color.IsSuccess
                Button.IsFullWidth
                Button.OnClick (fun e ->
                    let header = Browser.Dom.document.getElementById("TableRepresentationInfoHeader")
                    header?style?transition <- "0.3s ease"
                    header?style?opacity <- 0
                    WriteTableValidationToXml (model.ValidationState.TableValidationScheme, model.PersistentStorageState.AppVersion) |> ExcelInterop |> dispatch
                )
                Button.Props [Tooltip.dataTooltip "Write checklist info to excel worksheet."]
                Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            ] [
                str "Add checklist to workbook"
            ]
        ]
    ]

let validationComponent model dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Checklist Editor"]

        Help.help [][
            str "Display a table representation and add information to later validate values in the table according to their respective column."
        ]

        Button.a [
            Button.Color Color.IsInfo
            Button.IsFullWidth
            Button.OnClick (fun e -> GetTableValidationXml |> ExcelInterop |> dispatch )
            Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            Button.Props [Style [Margin "1rem 0"]; Tooltip.dataTooltip "Get checklist info for currently shown annotation table."]
        ] [
            str "Update table representation"
        ]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [
            str """Adjust current Swate table checklist. """
            span [
                Class (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
                Tooltip.dataTooltip """When hitting "Add checklist to workbook" this information will be saved as part of the workbook."""
                Style [Color NFDIColors.LightBlue.Base; MarginLeft ".5rem"]
            ][
                Fa.i [ Fa.Solid.InfoCircle ][]
            ]
        ]
        
        validationElements model dispatch
            
    ]