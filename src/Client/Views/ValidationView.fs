module ValidationView

open Fable.React
open Fable.React.Props
open Fulma
open ExcelColors
open Model
open Messages
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions

open CustomComponents

open Fulma.Extensions.Wikiki
open Fable.FontAwesome

let columnListElement ind (format:ValidationFormat) (model:Model) dispatch =
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
        td [][str format.ColumnHeader]
        td [][
            if format.Importance.IsSome then
                str (string format.Importance.Value)
            else
                str "X"
        ]
        td [][
            if format.ContentType.IsSome then
                str format.ContentType.Value.toString
            else
                str "X"
        ]
        td [][
            Icon.icon [][
                Fa.i [Fa.Solid.ChevronDown][]
            ]
        ]
    ]

let checkradioElement (id:int) (contentTypeOpt:ContentType option) (format:ValidationFormat) dispatch =
    let contentType = if contentTypeOpt.IsSome then contentTypeOpt.Value.toString else "None"
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
                let newFormat = {
                    format with
                        ContentType = contentTypeOpt
                }
                UpdateValidationFormat (format,newFormat) |> Validation |> dispatch
            )
            Checked (contentTypeOpt = format.ContentType)

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

let checkradioList (ind:int) (hasOntology:string option) format dispatch=
    [
        checkradioElement ind None format dispatch
        
        checkradioElement ind (Some ContentType.Number) format dispatch
        checkradioElement ind (Some ContentType.Int) format dispatch
        checkradioElement ind (Some ContentType.Decimal) format dispatch
        checkradioElement ind (Some ContentType.Text) format dispatch
        checkradioElement ind (Some ContentType.Url) format dispatch
        checkradioElement ind (
            if hasOntology.IsSome then ContentType.OntologyTerm hasOntology.Value |> Some else ContentType.OntologyTerm "None" |> Some
            )
            format
            dispatch
    ]

let findOntology (format:ValidationFormat) (colReps:OfficeInterop.ColumnRepresentation []) =
    colReps
    |> Array.find (fun x -> x.Header = format.ColumnHeader)
    |> fun x -> x.ParentOntology

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
let submitButton ind format dispatch =
    Button.span [
        Button.Color IsSuccess
        Button.IsOutlined
        Button.OnClick (
            fun e ->
                let sliderId = sprintf "importanceSlider%i" ind
                let sliderEle = Browser.Dom.document.getElementById(sliderId)
                let impoValue = sliderEle?value
                printfn "%s" impoValue
                let newFormat = {
                    format with
                        Importance = if impoValue = "0" then None else int impoValue |> Some
                }
                UpdateValidationFormat (format,newFormat) |> Validation |> dispatch
        )
    ][
        str "Submit Importance"
    ]

let optionsElement ind (format:ValidationFormat) (model:Model) dispatch =
    let hasOntology = findOntology format model.ValidationState.TableRepresentation
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
                    
                        yield! checkradioList ind hasOntology format dispatch

                    ]
                    Column.column [][
                        b [][str "Importance"]

                        Help.help [][str "Define how important it is to fill in the column correctly."]

                        yield! sliderElements ind format dispatch

                        submitButton ind format dispatch
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

        Help.help [Help.Color IsDanger] [
            str "This is currently a preview feature and is still missing a lot of features. See "
            a [Href "https://github.com/nfdi4plants/Swate/issues/45"; Target "_Blank"][str "here"]
            str " for the newst updates on this feature."
        ]

        Field.div [Field.Props [Style [
            Width "100%"
        ]]] [
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
                    for i in 0 .. model.ValidationState.TableValidationScheme.Length-1 do
                        let f = model.ValidationState.TableValidationScheme.[i]
                        yield! [
                            columnListElement i f model dispatch
                            optionsElement i f model dispatch
                        ]
                ]
            ]
        ]
    ]