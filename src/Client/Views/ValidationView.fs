module ValidationView

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions

open ExcelColors
open Model
open Messages

open CustomComponents

open OfficeInterop.Types.XmlValidationTypes

let columnListElement ind (columnValidation:ColumnValidation) (model:Model) dispatch =
    let isActive =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            true
        | _ ->
            false
    tr [
        Class "nonSelectText"
        Style [
            Cursor "pointer"
            UserSelect UserSelectOptions.None
            if model.ValidationState.DisplayedOptionsId.IsSome && model.ValidationState.DisplayedOptionsId.Value = ind then
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
                str "X"
        ]
        td [][
            if columnValidation.ValidationFormat.IsSome then
                str columnValidation.ValidationFormat.Value.toReadableString
            else
                str "X"
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
    let isDisabled = (contentType = "Ontology [None]")
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

let checkradioList (ind:int) (hasOntology:string option) colVal model dispatch =
    let ontologyContent = if hasOntology.IsSome then ContentType.OntologyTerm hasOntology.Value |> Some else ContentType.OntologyTerm "None" |> Some
    [
        checkradioElement ind   None                        colVal model dispatch
        
        checkradioElement ind   (Some ContentType.Number)   colVal model dispatch
        checkradioElement ind   (Some ContentType.Int)      colVal model dispatch
        checkradioElement ind   (Some ContentType.Decimal)  colVal model dispatch
        checkradioElement ind   (Some ContentType.Text)     colVal model dispatch
        checkradioElement ind   (Some ContentType.Url)      colVal model dispatch

        checkradioElement ind   ontologyContent             colVal model dispatch
    ]

let findOntology (columnValidation:ColumnValidation) (buildingBlocks:OfficeInterop.Types.BuildingBlockTypes.BuildingBlock []) =
    buildingBlocks
    |> Array.find (fun x -> x.MainColumn.Header.Value.Header = columnValidation.ColumnHeader)
    |> fun x -> x.MainColumn.Header.Value.Ontology

let sliderElements id format dispatch =
    let defaultSliderVal = string (if format.Importance.IsSome then format.Importance.Value else 0)
    let sliderId = sprintf "importanceSlider%i" id
    let outputSliderId = sprintf "outputForImportanceSlider%i" id
    [
        Slider.slider [
            Slider.Props [Id sliderId; Style [Height "40px"]]
            Slider.IsFullWidth
            Slider.IsCircle
            Slider.Max 100.
            Slider.Min 0.
            Slider.Step 1.
            Slider.CustomClass "has-output"
            Slider.OnChange (fun e ->
                // this is used to quickly update the label element to the right of the slider with t he new value
                let sliderEle = Browser.Dom.document.getElementById(outputSliderId)
                let _ = sliderEle.textContent <- (if e.Value = "0" then "None" else e.Value)
                ()
            )
            Slider.Color IsSuccess
            Slider.ValueOrDefault defaultSliderVal
        ]
        output [Props.HtmlFor sliderId; Id outputSliderId; Style [TextOverflow "unset"]] [
            str (if defaultSliderVal = "0" then "None" else defaultSliderVal)
        ]
    ]

open Fable.Core.JsInterop

/// Submit button to apply slider changes to model. If slider.OnChange would dispatch message the app would suffer from lag spikes.
let submitButton ind columnValidation (model:Model) dispatch =
    Button.span [
        Button.Color IsSuccess
        Button.IsOutlined
        Button.OnClick (
            fun e ->
                let sliderId = sprintf "importanceSlider%i" ind
                let sliderEle = Browser.Dom.document.getElementById(sliderId)
                let impoValue = sliderEle?value
                printfn "%s" impoValue
                let nextColumnValidation = {
                    columnValidation with
                        Importance = if impoValue = "0" then None else int impoValue |> Some
                }
                let nextTableValidation =
                    updateTableValidationByColValidation model nextColumnValidation
                UpdateTableValidationScheme nextTableValidation |> Validation |> dispatch
        )
    ][
        str "Submit Importance"
    ]

let optionsElement ind (columnValidation:ColumnValidation) (model:Model) dispatch =
    let hasOntology = findOntology columnValidation model.ValidationState.ActiveTableBuildingBlocks
    let isVisible =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            DisplayOptions.Block
        | _ ->
            DisplayOptions.None
    tr [][
        td [
            ColSpan 4
            Style [Padding "0"]
        ][
            Box.box' [
                Props [
                    Style [
                        Display isVisible
                        Width "100%"
                    ]
                ]
            ][
                Columns.columns [][
                    Column.column [][
                        b [][str "Content Type"]

                        Help.help [Help.Props [Style [MarginBottom "1rem"]]][str "Select the specific type of content for the selected column."]
                    
                        yield! checkradioList ind hasOntology columnValidation model dispatch

                    ]
                    Column.column [][
                        b [][str "Importance"]

                        Help.help [][str "Define how important it is to fill in the column correctly."]

                        yield! sliderElements ind columnValidation dispatch

                        submitButton ind columnValidation model dispatch
                    ]
                ]
            ]
        ]
    ]


let validationComponent model dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Table Validation"]

        //Help.help [Help.Color IsDanger] [
        //    str "This is currently a preview feature and is still missing a lot of features. See "
        //    a [Href "https://github.com/nfdi4plants/Swate/issues/45"; Target "_Blank"][str "here"]
        //    str " for the newst updates on this feature."
        //]

        Field.div [Field.Props [Style [
            Width "100%"
        ]]] [
            Button.a [
                Button.Color Color.IsInfo
                Button.IsFullWidth
                Button.OnClick (fun e -> PipeActiveAnnotationTable GetTableValidationXml |> ExcelInterop |> dispatch )
                Button.Props [Style [MarginBottom "1rem"]]
            ] [
                str "Update Table Representation"
            ]
            // Worksheet - annotationTable name - DateTime of saving
            div [
                Id "TableRepresentationInfoHeader"
                OnTransitionEnd (fun e ->
                    let header = Browser.Dom.document.getElementById("TableRepresentationInfoHeader")
                    header?style?opacity <- 1
                    header?style?transition <- "unset"
                )
            ][
                b [][
                    str model.ValidationState.TableValidationScheme.WorksheetName
                ]
                str " - "
                str model.ValidationState.TableValidationScheme.TableName
                str " - "
                str ( model.ValidationState.TableValidationScheme.DateTime.ToString("yyyy-MM-dd HH:mm") )
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
            // Submit new validation scheme. This will write custom xml into the workbook.
            Button.a [
                Button.Color Color.IsSuccess
                Button.IsFullWidth
                Button.OnClick (fun e ->
                    let header = Browser.Dom.document.getElementById("TableRepresentationInfoHeader")
                    header?style?transition <- "0.3s ease"
                    header?style?opacity <- 0
                    WriteTableValidationToXml (model.ValidationState.TableValidationScheme, model.PersistentStorageState.AppVersion) |> ExcelInterop |> dispatch
                )
                Button.Props [Style [MarginBottom "1rem"]]
            ] [
                str "Add validation to workbook"
            ]
        ]
    ]